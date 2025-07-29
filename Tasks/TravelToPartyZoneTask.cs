using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.Class;
using FollowBot.Helpers;
using FollowBot.SimpleEXtensions;
using log4net;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;


namespace FollowBot.Tasks
{
    class TravelToPartyZoneTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        private bool _enabled = true;
        private Stopwatch _portalRequestStopwatch = Stopwatch.StartNew();
        private static int _zoneCheckRetry = 0;
        public static Stopwatch PortOutStopwatch = new Stopwatch();

        public string Name { get { return "TravelToPartyZone"; } }
        public string Description { get { return "This task will travel to party grind zone."; } }
        public string Author { get { return "NotYourFriend original from Unknown"; } }
        public string Version { get { return "0.0.0.1"; } }

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

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead)
            {
                return false;
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
                //if (LokiPoe.CurrentWorldArea.IsMap || LokiPoe.CurrentWorldArea.Id.Contains("AfflictionTown") || LokiPoe.CurrentWorldArea.Id.Contains("Delve_"))
                //{
                //    if (FollowBotSettings.Instance.DontPortOutofMap) return false;
                //}
                if (PortOutStopwatch.IsRunning && PortOutStopwatch.ElapsedMilliseconds < FollowBotSettings.Instance.PortOutThreshold * 1000)
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
            
            //if (!curZone.IsTown && !curZone.IsHideoutArea && FollowBotSettings.Instance.DontPortOutofMap) return false;

            
            #region Delve
            //Then check for Delve portals:
            var delveportal = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>().FirstOrDefault(x => x.Name == "Azurite Mine" && (x.Metadata == "Metadata/MiscellaneousObject/PortalTransition" || x.Metadata == "Metadata/MiscellaneousObjects/PortalTransition"));
            if (delveportal != null)
            {
                Log.DebugFormat("[{0}] Found walkable delve portal.", Name);
                if (LokiPoe.Me.Position.Distance(delveportal.Position) > 15)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(delveportal, 13);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to delve portal");
                    return true;
                }

                var tele = await Coroutines.InteractWith(delveportal);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] delve portal error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }
            #endregion
            #region Heist Portals
            //Next check for Heist portals:
            var heistportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Leagues/Heist/Objects/MissionEntryPortal");

            //Check for exit port if already in a heist
            if (heistportal == null)
            {
                heistportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Leagues/Heist/Objects/MissionExitPortal");
            }

            if (heistportal != null && heistportal.Components.TargetableComponent.CanTarget)
            {
                Log.DebugFormat("[{0}] Found walkable heist portal.", Name);
                if (LokiPoe.Me.Position.Distance(heistportal.Position) > 20)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(heistportal, 20);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to heist portal");
                    return true;
                }

                var tele = await Coroutines.InteractWith(heistportal);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] heist portal error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }
            #endregion
            #region Affliction
            //King of the mist portal
            var kingportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/PortalToggleable");
            if (kingportal != null)
            {
                Log.DebugFormat("[{0}] Found king of the mist portal.", Name);
                if (LokiPoe.Me.Position.Distance(kingportal.Position) > 15)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(kingportal, 13);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to king of the mist portal");
                    return true;
                }

                var tele = await Coroutines.InteractWith(kingportal);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] king of the mist portal error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }

            var kingreturnportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/PortalToggleableReverse");
            if (kingreturnportal != null)
            {
                Log.DebugFormat("[{0}] Found king of the mist return portal.", Name);
                if (LokiPoe.Me.Position.Distance(kingreturnportal.Position) > 15)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(kingreturnportal, 13);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to king of the mist return portal");
                    return true;
                }

                var tele = await Coroutines.InteractWith(kingreturnportal);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] king of the mist return portal error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }

            var afflictiontransition = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Leagues/Azmeri/WoodsEntranceTransition");
            if (afflictiontransition != null && afflictiontransition.Components.TargetableComponent.CanTarget)
            {
                Log.DebugFormat("[{0}] Found walkable sanctum transition.", Name);
                if (LokiPoe.Me.Position.Distance(afflictiontransition.Position) > 20 && LokiPoe.Me.Position.Distance(afflictiontransition.Position) < 40)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(afflictiontransition, 20);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to affliction transition");
                    return true;
                }

                var tele = await Coroutines.InteractWith(afflictiontransition);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] affliction transition error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }
            #endregion
            #region Lab Trial Portals
            //Next check for Lab portals:
            var labportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/QuestObjects/Labyrinth/LabyrinthTrialPortal");
            if (labportal != null && labportal.Components.TargetableComponent.CanTarget)
            {
                Log.DebugFormat("[{0}] Found walkable lab portal.", Name);
                if (LokiPoe.Me.Position.Distance(labportal.Position) > 20 && LokiPoe.Me.Position.Distance(labportal.Position) < 50)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(labportal, 20);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to lab portal");
                    return true;
                }

                var tele = await Coroutines.InteractWith(labportal);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] lab portal error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }

            //Next check for Lab return portals:
            var labreturnportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Labyrinth/Objects/MapLabyrinthTrialReturnPortal");
            if (labreturnportal != null && labreturnportal.Components.TargetableComponent.CanTarget)
            {
                Log.DebugFormat("[{0}] Found walkable lab return portal.", Name);
                if (LokiPoe.Me.Position.Distance(labreturnportal.Position) > 20)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(labreturnportal, 20);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to lab return portal");
                    return true;
                }

                var tele = await Coroutines.InteractWith(labreturnportal);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] lab return portal error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }

            var campaignLabPortal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Labyrinth/Objects/LabyrinthTrialReturnPortal");
            if (campaignLabPortal != null && campaignLabPortal.IsTargetable)
            {
                var portalDistance = LokiPoe.Me.Position.Distance(campaignLabPortal.Position);

                // Move closer if not in interaction range
                if (portalDistance > 15)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(campaignLabPortal, 13);

                    // Cast Phase Run if available
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to Labyrinth Trial Return Portal");
                    return true;
                }

                // Interact with the portal if close enough
                var tele = await Coroutines.InteractWith(campaignLabPortal);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] Labyrinth Trial Return Portal interaction error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }
            #endregion
            #region Abyss Portals
            //Next check for Abyss portals:
            var abyssportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/Abyss/AbyssSubAreaTransition");
            if (abyssportal != null && abyssportal.Components.TargetableComponent.CanTarget)
            {
                Log.DebugFormat("[{0}] Found walkable abyss portal.", Name);
                if (LokiPoe.Me.Position.Distance(abyssportal.Position) > 20 && LokiPoe.Me.Position.Distance(abyssportal.Position) < 40)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(abyssportal, 20);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to abyss portal");
                    return true;
                }

                var tele = await Coroutines.InteractWith(abyssportal);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] abyss portal error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }
            #endregion
            #region Vaal Side Areas
            //Next check for Corrupted portals: 
            var corruptportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/PortalTransition");
            if (corruptportal != null && corruptportal.Components.TargetableComponent.CanTarget && corruptportal.Components.AreaTransitionComponent.TransitionType.ToString() == "NormalToCorrupted")
            {
                Log.DebugFormat("[{0}] Found walkable corrupt portal.", Name);
                if (LokiPoe.Me.Position.Distance(corruptportal.Position) > 20 && LokiPoe.Me.Position.Distance(corruptportal.Position) < 40)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(corruptportal, 20);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to corrupt portal");
                    return true;
                }

                var tele = await Coroutines.InteractWith(corruptportal);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] corrupt portal error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }

            //Next check for Corrupted areas: 
            var corruptarea = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/AreaTransition");
            if (corruptarea != null && corruptarea.Components.TargetableComponent.CanTarget && (corruptarea.Components.AreaTransitionComponent.TransitionType.ToString() == "NormalToCorrupted" || corruptarea.Components.AreaTransitionComponent.TransitionType.ToString() == "CorruptedToNormal"))
            {
                Log.DebugFormat("[{0}] Found walkable corrupt area.", Name);
                if (LokiPoe.Me.Position.Distance(corruptarea.Position) > 20 && LokiPoe.Me.Position.Distance(corruptarea.Position) < 40)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(corruptarea, 20);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to corrupt area");
                    return true;
                }

                var tele = await Coroutines.InteractWith(corruptarea);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] corrupt area error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }

            //Next check for Corrupted toggleable areas: 
            var corruptareatoggle = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/AreaTransitionToggleable");
            if (corruptareatoggle != null && corruptareatoggle.Components.TargetableComponent.CanTarget && (corruptareatoggle.Components.AreaTransitionComponent.TransitionType.ToString() == "NormalToCorrupted" || corruptareatoggle.Components.AreaTransitionComponent.TransitionType.ToString() == "CorruptedToNormal"))
            {
                Log.DebugFormat("[{0}] Found walkable corrupt toggle area.", Name);
                if (LokiPoe.Me.Position.Distance(corruptareatoggle.Position) > 20 && LokiPoe.Me.Position.Distance(corruptareatoggle.Position) < 40)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(corruptareatoggle, 20);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to corrupt toggle area");
                    return true;
                }

                var tele = await Coroutines.InteractWith(corruptareatoggle);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] corrupt toggle area error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }

            //Next check for Corrupted return portals:
            var corruptreturnportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/MiscellaneousObjects/VaalSideAreaReturnPortal");
            if (corruptreturnportal != null && corruptreturnportal.Components.TargetableComponent.CanTarget)
            {
                Log.DebugFormat("[{0}] Found walkable corrupt return portal.", Name);
                if (LokiPoe.Me.Position.Distance(corruptreturnportal.Position) > 20)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(corruptreturnportal, 20);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to corrupt return portal");
                    return true;
                }

                var tele = await Coroutines.InteractWith(corruptreturnportal);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] corrupt return portal error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }
            #endregion
            #region Sanctum Transition
            //Next check for the Sanctum transition by the waypoint
            var sanctumtransition = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Leagues/Sanctum/Objects/SanctumAirlockTransition");
            if (sanctumtransition != null && sanctumtransition.Components.TargetableComponent.CanTarget)
            {
                Log.DebugFormat("[{0}] Found walkable sanctum transition.", Name);
                if (LokiPoe.Me.Position.Distance(sanctumtransition.Position) > 20)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(sanctumtransition, 20);

                    // Cast Phase run if we have it.
                    CustomSkills.PhaseRun();

                    Move.Towards(walkablePosition, "moving to sanctum transition");
                    return true;
                }

                var tele = await Coroutines.InteractWith(sanctumtransition);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] sanctum transition error.", Name);
                }

                FollowBot.Leader = null;
                return true;
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

                    if (LokiPoe.Me.Position.Distance(trans.Position) > 20)
                    {
                        var loc = ExilePather.FastWalkablePositionFor(trans.Position, 20);
                        Move.Towards(loc, $"{trans.Name}");
                        return true;
                    }

                    await PlayerAction.Interact(trans);
                    return true;
                }
                else if (World.CurrentArea.IsLabyrinthArea)
                {
                    AreaTransition areatransition = null;
                    areatransition = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>().OrderBy(x => x.Distance).FirstOrDefault(x => ExilePather.PathExistsBetween(LokiPoe.Me.Position, ExilePather.FastWalkablePositionFor(x.Position, 20)));
                    if (areatransition != null)
                    {
                        Log.DebugFormat("[{0}] Found walkable Area Transition [{1}].", Name, areatransition.Name);
                        if (LokiPoe.Me.Position.Distance(areatransition.Position) > 20)
                        {
                            var walkablePosition = ExilePather.FastWalkablePositionFor(areatransition, 20);

                            // Cast Phase run if we have it.
                            CustomSkills.PhaseRun();

                            Move.Towards(walkablePosition, "moving to area transition");
                            return true;
                        }

                        var trans = await PlayerAction.TakeTransition(areatransition);

                        if (!trans)
                        {
                            Log.DebugFormat("[{0}] Areatransition error.", Name);
                        }

                        FollowBot.Leader = null;
                        return true;
                    }
                }
                GlobalLog.Warn($"[TravelToPartyZoneTask] Cant follow the leader in the Labirynt when the lab is already started.");
                return false;
            }
			var curZone = World.CurrentArea;
            if (curZone.IsCombatArea && FollowBotSettings.Instance.PortOutThreshold > 0)
            {
                if (!PortOutStopwatch.IsRunning)
                {
                    GlobalLog.Warn($"[TravelToPartyZoneTask] Party leader is in a diffrerent zone waiting {FollowBotSettings.Instance.PortOutThreshold} seconds to see if it come back.");
                    PortOutStopwatch.Restart();
                    await Coroutines.LatencyWait();
                    return true;
                }
                if (PortOutStopwatch.IsRunning && PortOutStopwatch.ElapsedMilliseconds >= FollowBotSettings.Instance.PortOutThreshold * 1000)
                {
                    PortOutStopwatch.Reset();
                    GlobalLog.Warn($"[TravelToPartyZoneTask] {FollowBotSettings.Instance.PortOutThreshold} seconds elapsed and Party leader is in still a diffrerent zone porting!.");
                    await PartyHelper.FastGotoPartyZone(leadername);
                    return true;
                }

                await Coroutines.LatencyWait();
                return true;
            }
            else
            {
				GlobalLog.Warn($"Trying to tp");
                await PartyHelper.FastGotoPartyZone(leadername);
                await Coroutines.LatencyWait();
            }
            await Coroutines.LatencyWait();
            return true;
        }
        private async Task<bool> GoToPartyLeaderZone()
        {
            var leader = LokiPoe.InstanceInfo.PartyMembers.FirstOrDefault(x => x.MemberStatus == PartyStatus.PartyLeader);
            if (leader == null) return false;
            var leaderPlayerEntry = leader.PlayerEntry;
            if (leaderPlayerEntry == null) return false;

            var leaderArea = leaderPlayerEntry?.Area;
            var zoneTransition = LokiPoe.ObjectManager.GetObjectsByType<AreaTransition>().OrderBy(x => x.Distance).FirstOrDefault(x => ExilePather.PathExistsBetween(LokiPoe.Me.Position, ExilePather.FastWalkablePositionFor(x.Position, 20)));
            if (zoneTransition != null && leaderArea != null && leaderArea.Id != World.CurrentArea.Id)
            {
                if (zoneTransition.Position.Distance(LokiPoe.Me.Position) > 15)
                    await Move.AtOnce(zoneTransition.Position, "Move to Move to leader zone");
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
                if (portal.Position.Distance(LokiPoe.Me.Position) > 18)
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
                Log.DebugFormat("[{0}] Failed to find portals.", Name);
                return false;
            }
        }

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
            if (message.Id == "Enable")
            {
                _enabled = true;
                return MessageResult.Processed;
            }
            if (message.Id == "Disable")
            {
                _enabled = false;
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }
    }
}