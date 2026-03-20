using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.Class;
using FollowBot.Helpers;
using FollowBot.SimpleEXtensions;
using FollowBot.SimpleEXtensions.Global;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace FollowBot.Tasks
{
    class TravelToPartyZoneTask : ITask
    {
        #region Fields and Constants

        // Portal interaction distances - standardized to 20
        private const int PortalMoveDistance = 20;
        private const int PortalWalkableDistance = 20;

        // Max search distances (prevents running across entire map)
        private const int StandardMaxDistance = 40;
        private const int LabTrialMaxDistance = 50;
        private const int MaligaroMaxDistance = 70;
        private const int NearbyTransitionMaxDistance = 100;
        private const int MirageReturnMaxDistance = 50;

        // State management
        private Stopwatch _portalRequestStopwatch = Stopwatch.StartNew();
        private static int _zoneCheckRetry = 0;
        private static int _maligaroPortalRetry = 0;
        public static Stopwatch PortOutStopwatch = new Stopwatch();

        #endregion

        #region ITask Properties

        public string Name { get { return "TravelToPartyZone"; } }
        public string Description { get { return "This task will travel to party grind zone."; } }
        public string Author { get { return "NotYourFriend, Rushtothesun"; } }
        public string Version { get { return "0.0.0.2"; } }

        #endregion

        #region ITask Lifecycle Methods

        public void Start()
        {
            PortOutStopwatch.Reset();
        }
        public void Stop()
        {
            PortOutStopwatch.Reset();
        }
        public void Tick()
        {
        }

        #endregion

        #region Main Task Logic

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead)
            {
                return false;
            }

            if (FollowTask.WaitingForNewInstance || FollowTask.NewInstanceWaitSw.IsRunning)
            {
                if (FollowTask.NewInstanceWaitSw.ElapsedMilliseconds < FollowTask.NewInstanceWaitMs)
                {
                    GlobalLog.Debug($"[{Name}] Waiting for leader after creating new instance...");
                    return false;
                }
            }

            await Coroutines.CloseBlockingWindows();

            var leader = LokiPoe.InstanceInfo.PartyMembers.FirstOrDefault(x => x.MemberStatus == PartyStatus.PartyLeader);
            if (leader == null) return false;
            var leaderPlayerEntry = leader.PlayerEntry;
            if (leaderPlayerEntry == null) return false;
            if (leaderPlayerEntry?.IsOnline != true)
            {
                GlobalLog.Warn($"Leader is not Online, probably loading.");
                return false;
            }

            var leadername = leaderPlayerEntry?.Name;
            var leaderArea = leaderPlayerEntry?.Area;
            if (string.IsNullOrEmpty(leadername) || leaderArea == null) return false;
            if (LokiPoe.InGameState.PartyHud.IsInSameZone(leadername))
            {
                _zoneCheckRetry = 0;
                PortOutStopwatch.Reset();
                return false;
            }
            else
            {
                if (LokiPoe.CurrentWorldArea.IsMap || LokiPoe.CurrentWorldArea.Id.Contains("AfflictionTown") || LokiPoe.CurrentWorldArea.Id.Contains("Delve_"))
                {
                    if (FollowBotSettings.Instance.Follow.DontPortOutofMap) return false;
                }

                if (PortOutStopwatch.IsRunning && PortOutStopwatch.ElapsedMilliseconds < FollowBotSettings.Instance.Follow.PortOutThreshold * 1000)
                {

                }
                else
                {
                    _zoneCheckRetry++;
                    if (_zoneCheckRetry < 2)
                    {
                        await Coroutines.LatencyWait();
                        GlobalLog.Warn($"IsInSameZone returned false for {leadername} retry [{_zoneCheckRetry}/2]");
                        return true;
                    }
                }
            }

            //First check the DontPortOutofMap
            var whereAmI = World.CurrentArea;
            if (!whereAmI.IsTown && !whereAmI.IsHideoutArea && FollowBotSettings.Instance.Follow.DontPortOutofMap) return false;

            #region Delve
            var delveportal = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>()
                .FirstOrDefault(x => x.Name == "Azurite Mine" &&
                    (x.Metadata == "Metadata/MiscellaneousObject/PortalTransition" ||
                     x.Metadata == "Metadata/MiscellaneousObjects/PortalTransition"));

            if (await TryInteractWithPortal(delveportal, "delve"))
                return true;
            #endregion

            #region Heist Portals
            var heistportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Leagues/Heist/Objects/MissionEntryPortal");
            if (heistportal == null)
                heistportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Leagues/Heist/Objects/MissionExitPortal");

            if (await TryInteractWithPortal(heistportal, "heist"))
                return true;
            #endregion

            #region Affliction
            var kingportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/PortalToggleable");
            if (await TryInteractWithPortal(kingportal, "king of the mist"))
                return true;

            var kingreturnportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/PortalToggleableReverse");
            if (await TryInteractWithPortal(kingreturnportal, "king of the mist return"))
                return true;

            var afflictiontransition = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Leagues/Azmeri/WoodsEntranceTransition");
            if (await TryInteractWithPortal(afflictiontransition, "affliction transition", StandardMaxDistance))
                return true;
            #endregion

            #region Lab Trial Portals
            var labportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/QuestObjects/Labyrinth/LabyrinthTrialPortal");
            if (await TryInteractWithPortal(labportal, "lab", LabTrialMaxDistance))
                return true;

            var labreturnportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Labyrinth/Objects/MapLabyrinthTrialReturnPortal");
            if (await TryInteractWithPortal(labreturnportal, "lab return"))
                return true;
            #endregion

            #region Abyss Portals
            var abyssportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/Abyss/AbyssSubAreaTransition");
            if (await TryInteractWithPortal(abyssportal, "abyss", StandardMaxDistance))
                return true;
            #endregion

            #region Vaal Side Areas
            var corruptportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/PortalTransition");
            if (corruptportal != null && corruptportal.Components.AreaTransitionComponent.TransitionType.ToString() == "NormalToCorrupted")
            {
                if (await TryInteractWithPortal(corruptportal, "corrupt portal", StandardMaxDistance))
                    return true;
            }

            var corruptarea = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/AreaTransition");
            if (corruptarea != null && (corruptarea.Components.AreaTransitionComponent.TransitionType.ToString() == "NormalToCorrupted" ||
                corruptarea.Components.AreaTransitionComponent.TransitionType.ToString() == "CorruptedToNormal"))
            {
                if (await TryInteractWithPortal(corruptarea, "corrupt area", StandardMaxDistance))
                    return true;
            }

            var corruptareatoggle = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/AreaTransitionToggleable");
            if (corruptareatoggle != null && (corruptareatoggle.Components.AreaTransitionComponent.TransitionType.ToString() == "NormalToCorrupted" ||
                corruptareatoggle.Components.AreaTransitionComponent.TransitionType.ToString() == "CorruptedToNormal"))
            {
                if (await TryInteractWithPortal(corruptareatoggle, "corrupt toggle area", StandardMaxDistance))
                    return true;
            }

            var corruptreturnportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/VaalSideAreaReturnPortal");
            if (await TryInteractWithPortal(corruptreturnportal, "corrupt return"))
                return true;
            #endregion

            #region Sanctum Transition
            var sanctumtransition = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Leagues/Sanctum/Objects/SanctumAirlockTransition");
            if (await TryInteractWithPortal(sanctumtransition, "sanctum transition"))
                return true;
            #endregion

            #region Mirage Portals
            if (leaderArea.Id == LokiPoe.CurrentWorldArea.Id)
            {
                // 1. Entry Portal (We are OUTSIDE the Mirage, button is not visible)
                var mirageEntry = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/Faridun/DjinnPortal");
                if (mirageEntry != null && await TryInteractWithPortal(mirageEntry, "mirage entry", StandardMaxDistance, interactDistance: 12))
                    return true;

                // 2. Return Portal / Button (We are INSIDE the Mirage)
                // The presence of the Mirage return UI button is the only 100% guarantee we are actually inside a Mirage instance,
                // and prevents the bot from clicking a player's Sekhema Portal MTX in a regular map.
                var mirageButton = FindMirageReturnButton();
                if (mirageButton != null)
                {
                    var mirageReturn = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Effects/Microtransactions/Town_Portals/SekhemaPortal/SekhemaPortal");

                    if (mirageReturn != null && LokiPoe.Me.Position.Distance(mirageReturn.Position) <= MirageReturnMaxDistance)
                    {
                        // Portal is visible and close enough to walk to
                        if (await TryInteractWithPortal(mirageReturn, "mirage return", MirageReturnMaxDistance, interactDistance: 12))
                            return true;
                    }
                    else
                    {
                        // Portal is too far or not visible yet — click the UI button to teleport to it
                        if (await ClickMirageReturnButton(mirageButton))
                            return true;
                    }
                }
            }
            #endregion

            #region Maligaro's Sanctum Portal
            if (LokiPoe.CurrentWorldArea.Id == "2_7_5_1")
            {
                var maligaroPortal = LokiPoe.ObjectManager.GetObjectsByType<Portal>()
                    .FirstOrDefault(x => x.Name == "Maligaro's Sanctum" && x.Distance <= MaligaroMaxDistance);

                if (maligaroPortal != null && maligaroPortal.IsTargetable)
                {
                    GlobalLog.Debug($"[{Name}] Found Maligaro's Sanctum portal at distance {maligaroPortal.Distance}.");

                    if (LokiPoe.Me.Position.Distance(maligaroPortal.Position) > PortalMoveDistance)
                    {
                        var walkablePosition = ExilePather.FastWalkablePositionFor(maligaroPortal, PortalWalkableDistance);
                        CustomSkills.PhaseRun();
                        Move.Towards(walkablePosition, "moving to Maligaro's Sanctum portal");
                        return true;
                    }

                    var tele = await Coroutines.InteractWith(maligaroPortal);

                    if (!tele)
                    {
                        _maligaroPortalRetry++;
                        if (_maligaroPortalRetry < 5)
                        {
                            GlobalLog.Debug($"[{Name}] Maligaro's Sanctum portal interaction failed, retry [{_maligaroPortalRetry}/5].");
                            await Coroutines.LatencyWait();
                            return true;
                        }
                        else
                        {
                            GlobalLog.Debug($"[{Name}] Maligaro's Sanctum portal error after 5 attempts.");
                            _maligaroPortalRetry = 0;
                        }
                    }
                    else
                    {
                        _maligaroPortalRetry = 0;
                    }

                    FollowBot.Leader = null;
                    return true;
                }
            }
            #endregion

            #region Hideout Portal
            // portal
            /*var hoportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/MultiplexPortal");
            if (hoportal != null)
            {
                Log.DebugFormat("[{0}] Found ho portal.", Name);
                if (LokiPoe.Me.Position.Distance(hoportal.Position) > 20 && LokiPoe.Me.Position.Distance(hoportal.Position) < 40)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(hoportal, 13);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to ho portal");
                    return true;
                }

                var tele = await Coroutines.InteractWith(hoportal);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] ho portal error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            } */
            #endregion

            if (leaderArea.IsMap || leaderArea.IsTempleOfAtzoatl || leaderArea.Id.Contains("Expedition"))
            {
                if (!await TakePortal())
                    await Coroutines.ReactionWait();
                return true;
            }
            else if (leaderArea.IsLabyrinthArea)
            {
                if (leaderArea.Name == "Aspirants' Plaza")
                {
                    await PartyHelper.FastGotoPartyZone(leader.PlayerEntry.Name);
                    return true;
                }

                if (World.CurrentArea.Name == "Aspirants' Plaza")
                {
                    var trans = LokiPoe.ObjectManager.GetObjectByType<AreaTransition>();
                    if (trans == null)
                    {
                        var loc = ExilePather.FastWalkablePositionFor(new Vector2i(363, 423));
                        if (loc != Vector2i.Zero)
                        {
                            Move.Towards(loc, "Bronze Plaque");
                            return true;
                        }
                        else
                        {
                            GlobalLog.Warn($"[TravelToPartyZoneTask] Cant find Bronze Plaque location.");
                            return false;
                        }
                    }

                    if (LokiPoe.Me.Position.Distance(trans.Position) > PortalMoveDistance)
                    {
                        var loc = ExilePather.FastWalkablePositionFor(trans.Position, PortalWalkableDistance);
                        Move.Towards(loc, $"{trans.Name}");
                        return true;
                    }

                    await PlayerAction.Interact(trans);
                    return true;
                }
                else if (World.CurrentArea.IsLabyrinthArea)
                {
                    AreaTransition areatransition = null;
                    areatransition = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>()
                        .OrderBy(x => x.Distance)
                        .FirstOrDefault(x => ExilePather.PathExistsBetween(LokiPoe.Me.Position,
                            ExilePather.FastWalkablePositionFor(x.Position, PortalWalkableDistance)));

                    if (areatransition != null)
                    {
                        GlobalLog.Debug($"[{Name}] Found walkable Area Transition [{areatransition.Name}].");
                        if (LokiPoe.Me.Position.Distance(areatransition.Position) > PortalMoveDistance)
                        {
                            var walkablePosition = ExilePather.FastWalkablePositionFor(areatransition, PortalWalkableDistance);
                            CustomSkills.PhaseRun();
                            Move.Towards(walkablePosition, "moving to area transition");
                            return true;
                        }

                        var trans = await PlayerAction.TakeTransition(areatransition);

                        if (!trans)
                        {
                            GlobalLog.Debug($"[{Name}] Areatransition error.");
                        }

                        FollowBot.Leader = null;
                        return true;
                    }
                }
                GlobalLog.Warn($"[TravelToPartyZoneTask] Cant follow the leader in the Labyrinth when the lab is already started.");
                return false;
            }
            var curZone = World.CurrentArea;
            if (curZone.IsCombatArea && FollowBotSettings.Instance.Follow.PortOutThreshold > 0)
            {
                if (!PortOutStopwatch.IsRunning)
                {
                    GlobalLog.Warn($"[TravelToPartyZoneTask] Party leader is in a diffrerent zone waiting {FollowBotSettings.Instance.Follow.PortOutThreshold} seconds to see if it come back.");
                    PortOutStopwatch.Restart();
                    await Coroutines.LatencyWait();
                    return true;
                }
                if (PortOutStopwatch.IsRunning && PortOutStopwatch.ElapsedMilliseconds >= FollowBotSettings.Instance.Follow.PortOutThreshold * 1000)
                {
                    PortOutStopwatch.Reset();
                    GlobalLog.Warn($"[TravelToPartyZoneTask] {FollowBotSettings.Instance.Follow.PortOutThreshold} seconds elapsed and Party leader is in still a diffrerent zone porting!.");

                    // Try to use nearby area transition first
                    if (await TryUseNearbyTransitionToLeader(leaderArea.Id))
                        return true;

                    await PartyHelper.FastGotoPartyZone(leadername);
                    return true;
                }

                await Coroutines.LatencyWait();
                return true;
            }
            else
            {
                GlobalLog.Warn($"Trying to tp");

                // Try to use nearby area transition first
                if (!await TryUseNearbyTransitionToLeader(leaderArea.Id))
                {
                    await PartyHelper.FastGotoPartyZone(leadername);
                }
                await Coroutines.LatencyWait();
            }
            await Coroutines.LatencyWait();
            return true;
        }

        #endregion

        #region Portal Helper Methods

        /// <summary>
        /// Common helper method for interacting with portals and transitions
        /// </summary>
        private async Task<bool> TryInteractWithPortal(NetworkObject portal, string portalType, int? maxDistance = null, int interactDistance = PortalMoveDistance)
        {
            if (portal == null || !portal.Components.TargetableComponent.CanTarget)
                return false;

            var distance = LokiPoe.Me.Position.Distance(portal.Position);

            // Check if portal is within max search distance
            if (maxDistance.HasValue && distance > maxDistance.Value)
                return false;

            GlobalLog.Debug($"[{Name}] Found walkable {portalType} portal.");

            // Move to portal if too far
            if (distance > interactDistance)
            {
                var walkablePosition = ExilePather.FastWalkablePositionFor(portal, PortalWalkableDistance);
                CustomSkills.PhaseRun();
                Move.Towards(walkablePosition, $"moving to {portalType} portal");
                return true;
            }

            // Interact with portal
            var tele = await Coroutines.InteractWith(portal);
            if (!tele)
            {
                GlobalLog.Debug($"[{Name}] {portalType} portal error.");
            }

            FollowBot.Leader = null;
            return true;
        }

        #endregion

        #region Mirage Return Button

        private const string MirageButtonTooltip = "Teleports you back to the entrance of this Mirage.";

        /// <summary>
        /// Finds the mirage return button by searching the action button container for the
        /// element whose tooltip matches. Resilient to index shifts from other mechanic buttons.
        /// </summary>
        private Element FindMirageReturnButton()
        {
            var container = FindElementByLabels("HUD", "HUDRight", "skip_button_layout");
            if (container?.Children == null)
                return null;

            foreach (var child in container.Children)
            {
                if (child == null || !child.IsVisible)
                    continue;

                try
                {
                    var tooltip = child.Tooltip;
                    if (tooltip?.Text?.Contains(MirageButtonTooltip) == true)
                        return child;

                    // Some tooltips have text in children
                    if (tooltip?.Children != null && tooltip.Children.Count > 0)
                    {
                        var text = tooltip.Children[0]?.Text;
                        if (text != null && text.Contains(MirageButtonTooltip))
                            return child;
                    }
                }
                catch
                {
                    // Tooltip access can throw on stale elements
                }
            }

            return null;
        }

        /// <summary>
        /// Clicks the in-game "Return to Mirage Portal" UI button to teleport near the exit portal.
        /// Uses a position guard to verify the teleport actually occurred.
        /// </summary>
        private async Task<bool> ClickMirageReturnButton(Element mirageButton)
        {
            if (mirageButton == null)
                return false;

            GlobalLog.Debug($"[{Name}] Clicking mirage return button to teleport to exit portal.");

            // Record position before click
            var posBefore = LokiPoe.Me.Position;

            // Click the button
            var clickPos = mirageButton.CenterClickLocation();
            MouseManager.SetMousePosition(clickPos, useRandomPos: false);
            await Wait.SleepSafe(25, 55);
            MouseManager.ClickLMB();

            // Poll for teleport completion — the teleport animation can take a while
            for (int i = 0; i < 20; i++)
            {
                await Wait.SleepSafe(70, 100);

                var posNow = LokiPoe.Me.Position;
                var distanceMoved = posBefore.Distance(posNow);

                if (distanceMoved > 30)
                {
                    GlobalLog.Debug($"[{Name}] Mirage return teleport successful, moved {(int)distanceMoved} units.");
                    FollowBot.Leader = null;
                    return true;
                }
            }

            // Final check
            var posAfter = LokiPoe.Me.Position;
            var finalDistance = posBefore.Distance(posAfter);
            GlobalLog.Warn($"[{Name}] Mirage return button click did not teleport after 2s (moved {(int)finalDistance} units), falling through.");
            return false;
        }



        #endregion

        #region Helper Methods

        /// <summary>
        /// Navigates the UI element tree by IdLabel instead of hardcoded indices.
        /// Starts from root.Children[1] and walks down matching each label in order.
        /// </summary>
        private static Element FindElementByLabels(params string[] labels)
        {
            var allElements = LokiPoe.GetGuiElements();
            var root = Enumerable.FirstOrDefault(allElements, e => e.IdLabel == "root");
            if (root?.Children == null || root.Children.Count < 2)
                return null;

            Element current = root.Children[1];
            foreach (var label in labels)
            {
                if (current?.Children == null)
                    return null;
                current = Enumerable.FirstOrDefault(current.Children, c => c?.IdLabel == label);
                if (current == null)
                    return null;
            }
            return current;
        }


        private async Task<bool> GoToPartyLeaderZone()
        {
            var leader = LokiPoe.InstanceInfo.PartyMembers.FirstOrDefault(x => x.MemberStatus == PartyStatus.PartyLeader);
            if (leader == null) return false;
            var leaderPlayerEntry = leader.PlayerEntry;
            if (leaderPlayerEntry == null) return false;

            var leaderArea = leaderPlayerEntry?.Area;
            var zoneTransition = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>()
                .OrderBy(x => x.Distance)
                .FirstOrDefault(x => ExilePather.PathExistsBetween(LokiPoe.Me.Position, ExilePather.FastWalkablePositionFor(x.Position, PortalWalkableDistance)));

            if (zoneTransition != null && leaderArea != null && leaderArea.Id != World.CurrentArea.Id)
            {
                if (zoneTransition.Position.Distance(LokiPoe.Me.Position) > PortalMoveDistance)
                    await Move.AtOnce(zoneTransition.Position, "Move to leader zone");

                if (await Coroutines.InteractWith<AreaTransition>(zoneTransition))
                    return true;
                else
                    return false;
            }
            return false;
        }

        private async Task<bool> TakePortal()
        {
            var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>().FirstOrDefault(x => x.IsTargetable);
            if (portal != null)
            {
                if (portal.Position.Distance(LokiPoe.Me.Position) > PortalMoveDistance)
                    await Move.AtOnce(portal.Position, "Move to portal");

                if (await Coroutines.InteractWith<Portal>(portal))
                    return true;
                else
                    return false;
            }
            else
            {
                if (await GoToPartyLeaderZone())
                {
                    await Coroutines.ReactionWait();
                    return true;
                }
                GlobalLog.Debug($"[{Name}] Failed to find portals.");
                return false;
            }
        }

        private async Task<bool> TryUseNearbyTransitionToLeader(string leaderAreaId)
        {
            var cache = CombatAreaCache.Current;
            var nearbyTransition = cache.AreaTransitions
                .Where(t => t.Destination != null && t.Destination.Id == leaderAreaId)
                .OrderBy(t => t.Position.Distance)
                .FirstOrDefault(t => t.Position.Distance < NearbyTransitionMaxDistance);

            if (nearbyTransition == null)
                return false;

            var transition = nearbyTransition.Object;
            if (transition == null || !transition.IsTargetable)
            {
                cache.AreaTransitions.Remove(nearbyTransition);
                return false;
            }

            GlobalLog.Debug($"[{Name}] Found nearby transition to leader's zone: {transition.Name} at distance {nearbyTransition.Position.Distance}");

            if (nearbyTransition.Position.Distance > PortalMoveDistance)
            {
                // Snap to a walkable position first — the raw transition position may be off-navmesh,
                // which would cause Move.AtOnce to loop on pathfinding failures and crash the bot.
                var walkablePos = ExilePather.FastWalkablePositionFor(transition.Position, PortalWalkableDistance);
                if (!ExilePather.PathExistsBetween(LokiPoe.Me.Position, walkablePos))
                {
                    GlobalLog.Warn($"[{Name}] No walkable path to transition {transition.Name}, falling back to teleport.");
                    return false;
                }
                await Move.AtOnce(walkablePos, "moving to area transition");
            }

            var success = await PlayerAction.TakeTransition(transition);
            if (success)
            {
                GlobalLog.Info($"[{Name}] Used area transition to reach leader instead of teleport button.");
                return true;
            }

            return false;
        }

        #endregion

        #region ITask Interface Implementation

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Events.Messages.AreaChanged)
            {
                _zoneCheckRetry = 0;
                PortOutStopwatch.Reset();
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        #endregion
    }
}