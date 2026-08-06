using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki;
using FollowBot.Class;
using FollowBot.SimpleEXtensions;
using FollowBot.SimpleEXtensions.Global;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static DreamPoeBot.Loki.Game.LokiPoe;
using static FollowBot.Helpers.StateHelper;


namespace FollowBot.Tasks
{
    class FollowTask : ITask
    {

        public string Name { get { return "FollowTask"; } }
        public string Description { get { return "This task will Follow a Leader."; } }
        public string Author { get { return "NotYourFriend, origial code from Unknown, Rushtothesun"; } }
        public string Version { get { return "0.0.0.1"; } }
        private const int MaxInteractionAttempts = 4;
        private const int InteractionDistance = 40;
        private const int MercenaryOptInAttempts = 2;
        private const int MercenaryInteractionDistance = 20;
        private const int MercenaryWaitingForDuelTimeoutMs = 1500;
        private const int MercenaryUiCloseTimeoutMs = 1000;
        private const int MercenaryUiStabilizationMs = 150;
        private const string DeepwaterLanternMetadata = "Metadata/Terrain/Leagues/Deepwater/Objects/Lantern";
        private const int DeepwaterChestSafetyRadius = 105;
        private const int DeepwaterChestSafetyRadiusSqr = DeepwaterChestSafetyRadius * DeepwaterChestSafetyRadius;
        private const int DeepwaterGoldenLanternTouchRadius = 10;
        private const int InvalidPathsBeforeRecovery = 3;
        private const int PathingRecoveryCooldownMs = 30000;
        public const int NewInstanceWaitMs = 7000;
        private Vector2i _lastSeenMasterPosition;
        private Stopwatch _leaderzoningSw;
        private Stopwatch _pathingRecoveryCooldown;
        private bool _postAreaChangePathCheckPending;
        private bool _pathingRecoveryAwaitingValidation;
        private int _consecutiveInvalidPaths;
        private HashSet<int> _failedObjectIds = new HashSet<int>();
        private HashSet<int> _failedMercenaryIds = new HashSet<int>();
        private HashSet<int> _touchedSpawnerIds = new HashSet<int>();
        private HashSet<int> _touchedGoldenLanternIds = new HashSet<int>();
        public static bool ShouldCreateNewInstance = false;
        public static bool WaitingForNewInstance = false;
        public static Stopwatch NewInstanceWaitSw = new Stopwatch();

        public void Start()
        {
            GlobalLog.Info($"[{Name}] Task Loaded.");
            FollowBot.Leader = null;
            _lastSeenMasterPosition = Vector2i.Zero;
            _leaderzoningSw = new Stopwatch();
            _pathingRecoveryCooldown = new Stopwatch();
            _postAreaChangePathCheckPending = true;
            _pathingRecoveryAwaitingValidation = false;
            _consecutiveInvalidPaths = 0;
            _failedObjectIds.Clear();
            _failedMercenaryIds.Clear();
            _touchedSpawnerIds.Clear();
            _touchedGoldenLanternIds.Clear();
            NewInstanceWaitSw = new Stopwatch();
        }
        public void Stop()
        {

        }
        public void Tick()
        {

        }

        public async Task<bool> Run()
        {
            if (ShouldCreateNewInstance)
            {
                var transition = ObjectManager.GetObjectsByType<AreaTransition>()
                    .OrderBy(t => t.Distance)
                    .FirstOrDefault(t => t.Distance <= 30);

                if (transition != null)
                {
                    var pos = transition.WalkablePosition();
                    if (pos.Distance > 10)
                    {
                        await pos.ComeAtOnce();
                    }

                    // Set flag BEFORE the await — it will survive the loading screen
                    // and be converted to a running stopwatch in the AreaChanged handler.
                    WaitingForNewInstance = true;
                    await PlayerAction.CreateNewInstance(transition);
                }

                ShouldCreateNewInstance = false;
                return true;
            }

            if (NewInstanceWaitSw.IsRunning)
            {
                if (NewInstanceWaitSw.ElapsedMilliseconds < NewInstanceWaitMs)
                {
                    if (FollowBot.Leader != null && LokiPoe.InGameState.PartyHud.IsInSameZone(FollowBot.Leader.Name))
                    {
                        GlobalLog.Debug($"[{Name}] Leader is in the same zone, stopping wait.");
                        NewInstanceWaitSw.Reset();
                    }
                    else
                    {
                        GlobalLog.Debug($"[{Name}] Waiting for leader after creating new instance...");
                        return true;
                    }
                }
                else
                {
                    NewInstanceWaitSw.Reset();
                }
            }

            if (!FollowBotSettings.Instance.Follow.ShouldFollow)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return false;
            }
            if (!IsInGame || Me.IsDead)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return false;
            }
            if (Me.IsInTown && !FollowBotSettings.Instance.Follow.FollowInTown)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return false;
            }
            if (Me.IsInHideout && !FollowBotSettings.Instance.Follow.FollowInHideout)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return false;
            }
            if (World.CurrentArea.Id == "HeistHub" && !FollowBotSettings.Instance.Follow.FollowInHeistHub)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return false;
            }

            if (FollowBot.Leader == null)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return false;
            }

            var leader = FollowBot.Leader;

            var leaderPos = leader.Position;
            var mypos = Me.Position;
            if (leaderPos == Vector2i.Zero || mypos == Vector2i.Zero)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return false;
            }



            var distance = leaderPos.Distance(mypos);

            // Mercenaries are discovered by CombatAreaCache's existing object scan.
            // Keep the interaction state live because CanOpt_In and the duel UI can change.
            if (await TryOptInToNearbyMercenary(leader))
                return true;

            if (ExilePather.PathExistsBetween(mypos, ExilePather.FastWalkablePositionFor(leaderPos)))
            {
                _lastSeenMasterPosition = leaderPos;
                MarkPathingHealthy();
            }

            // Handle specific area transitions when leader is far away
            if (await TryUseAreaSpecificTransition(distance))
                return true;

            // Take trial return portal if nearby and leader is far
            if (await TryUseTrialReturnPortal(distance))
                return true;

            // Try to interact with nearby crafting recipes (always enabled, no setting needed)
            if (await TryInteractWithNearbyRecipe())
                return true;

            // Try to open nearby doors
            if (FollowBotSettings.Instance.Follow.OpenDoors && await TryOpenNearbyDoor())
                return true;

            if (!IsDeepwaterDrowning())
            {
                // Try to click nearby shrines
                if (FollowBotSettings.Instance.Follow.ClickShrines && await TryClickNearbyShrine())
                    return true;

                // Try to open nearby chests (only if leader is close)
                if (FollowBotSettings.Instance.Loot.ShouldOpenChests && distance <= 60 && await TryOpenNearbyChest())
                    return true;

                // Golden lanterns grant a Deepwater buff when walked over. Only detour when the follower is close to the leader.
                if (IsDeepwaterEncounter() && await TryActivateNearbyGoldenLantern(distance))
                    return true;
            }

            // Try to activate Mirage spawners
            if (LeagueFeatureFlags.MirageEnabled && FollowBotSettings.Instance.Follow.ActivateMirageSpawners && await TryActivateNearbySpawner())
                return true;

            if (distance > FollowBotSettings.Instance.Follow.MaxFollowDistance || leader?.HasCurrentAction == true && leader?.CurrentAction?.Skill?.InternalId == "Move")
            {

                var pos = ExilePather.FastWalkablePositionFor(mypos.GetPointAtDistanceBeforeEnd(
                    leaderPos,
                    Random.Next(FollowBotSettings.Instance.Follow.FollowDistance,
                        FollowBotSettings.Instance.Follow.MaxFollowDistance)));
                if (pos == Vector2i.Zero || !ExilePather.PathExistsBetween(mypos, pos))
                {
                    KeyManager.ClearAllKeyStates();
                    // First check for Grace period, that mean we have just zoned, and the leader position might be incorrect.
                    if (ClassExtensions.IsUnderGracePeriod)
                    {
                        if (!_leaderzoningSw.IsRunning)
                        {
                            GlobalLog.Debug($"Grace period detected, this mean we just zoned and are waiting for the leader to finish loading.");
                            _leaderzoningSw.Start();
                        }
                        if (_leaderzoningSw.IsRunning && _leaderzoningSw.ElapsedMilliseconds < 10000)
                            return true;
                    }

                    _consecutiveInvalidPaths++;
                    if (_pathingRecoveryAwaitingValidation)
                    {
                        _pathingRecoveryAwaitingValidation = false;
                        GlobalLog.Warn($"[{Name}] Follow path is still invalid after ExilePather reload " +
                                       $"(me: {mypos}, leader: {leaderPos}, target: {pos}).");
                    }
                    else if (_consecutiveInvalidPaths == 1)
                    {
                        GlobalLog.Warn($"[{Name}] Invalid follow path detected " +
                                       $"(me: {mypos}, leader: {leaderPos}, target: {pos}).");
                    }

                    if (TryRecoverPathing(mypos, leaderPos, pos))
                        return true;

                    //Then check for Delve portals:
                    var delveportal = ObjectManager.GetObjectsByType<AreaTransition>().FirstOrDefault(x => x.Name == "Azurite Mine" && x.Metadata == "Metadata/MiscellaneousObject/PortalTransition");
                    if (delveportal != null)
                    {
                        GlobalLog.Debug($"[{Name}] Found walkable delve portal.");
                    RepeatBehavior1:
                        if (Me.Position.Distance(delveportal.Position) > 20)
                        {
                            if (Me.IsDead) { return true; }
                            var walkablePosition = ExilePather.FastWalkablePositionFor(delveportal, 20);

                            // Cast Phase run if we have it.
                            CustomSkills.PhaseRun();

                            if (Move.Towards(walkablePosition, "moving to delve portal"))
                                goto RepeatBehavior1;
                            return true;
                        }

                        var tele = await Coroutines.InteractWith(delveportal);

                        if (!tele)
                        {
                            GlobalLog.Debug($"[{Name}] delve portal error.");
                        }

                        FollowBot.Leader = null;
                        return true;
                    }

                    AreaTransition areatransition = null;
                    if (_lastSeenMasterPosition != Vector2i.Zero)
                        areatransition = ObjectManager.GetObjectsByType<AreaTransition>().OrderBy(x => x.Position.Distance(_lastSeenMasterPosition)).FirstOrDefault(x => ExilePather.PathExistsBetween(mypos, ExilePather.FastWalkablePositionFor(x.Position, 20)));
                    if (areatransition == null)
                    {
                        var teleport = ObjectManager.GetObjectsByName("Portal").OrderBy(x => x.Position.Distance(_lastSeenMasterPosition)).FirstOrDefault(x => ExilePather.PathExistsBetween(Me.Position, ExilePather.FastWalkablePositionFor(x.Position, 20)));
                        if (teleport == null)
                            return false;
                        GlobalLog.Debug($"[{Name}] Found walkable Teleport.");
                    RepeatBehavior2:
                        if (Me.Position.Distance(teleport.Position) > 20)
                        {

                            var leader2 = FollowBot.Leader;

                            var leaderPos2 = leader.Position;
                            var mypos2 = Me.Position;
                            if (!ExilePather.PathExistsBetween(leaderPos2, mypos2))
                            {
                                return false;
                            }
                            var walkablePosition = ExilePather.FastWalkablePositionFor(teleport, 20);
                            // Cast Phase run if we have it.
                            CustomSkills.PhaseRun();

                            if (Move.Towards(walkablePosition, "moving to Teleport"))
                            {
                                goto RepeatBehavior2;
                            }
                            return true;
                        }

                        var tele = await Coroutines.InteractWith(teleport);

                        if (!tele)
                        {
                            GlobalLog.Debug($"[{Name}] Teleport error.");
                        }

                        FollowBot.Leader = null;
                        return true;
                    }

                    GlobalLog.Debug($"[{Name}] Found walkable Area Transition [{areatransition.Name}].");

                    if (Me.Position.Distance(areatransition.Position) > 20)
                    {
                        if (Me.IsDead) { return true; }
                        var walkablePosition = ExilePather.FastWalkablePositionFor(areatransition, 20);

                        // Cast Phase run if we have it.
                        CustomSkills.PhaseRun();

                        Move.Towards(walkablePosition, "moving to area transition");

                        return true;
                    }
                    var trans = await PlayerAction.TakeTransition(areatransition);

                    if (!trans)
                    {
                        GlobalLog.Debug($"[{Name}] Areatransition error.");
                    }

                    //FollowBot.Leader = null;
                    return true;
                }

                MarkPathingHealthy();

                // Cast Phase run if we have it.
                CustomSkills.PhaseRun();

                /*if (ExilePather.PathDistance(mypos, pos) < 45) //effects skillplayermover
                {
                    InGameState.SkillBarHud.UseAt(FollowBot.LastBoundMoveSkillSlot, false, pos, false);
                }
                else*/
                Move.Towards(pos, $"{leader.Name}");
                return true;
            }
            // Clear the move key
            ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
            //KeyManager.ClearAllKeyStates();
            return false;
        }

        private bool TryRecoverPathing(Vector2i mypos, Vector2i leaderPos, Vector2i followPos)
        {
            if (!ExilePather.IsReady)
                return false;

            var isPostAreaChangeRecovery = _postAreaChangePathCheckPending;
            var cooldownElapsed = !_pathingRecoveryCooldown.IsRunning ||
                                  _pathingRecoveryCooldown.ElapsedMilliseconds >= PathingRecoveryCooldownMs;
            var isInAreaRecovery = _consecutiveInvalidPaths >= InvalidPathsBeforeRecovery && cooldownElapsed;

            if (!isPostAreaChangeRecovery && !isInAreaRecovery)
                return false;

            _postAreaChangePathCheckPending = false;
            _pathingRecoveryAwaitingValidation = true;
            _consecutiveInvalidPaths = 0;
            _pathingRecoveryCooldown.Restart();

            var recoveryKind = isPostAreaChangeRecovery ? "post-area-change" : "in-area watchdog";
            GlobalLog.Info($"[{Name}] Invalid follow path (me: {mypos}, leader: {leaderPos}, target: {followPos}). " +
                           $"Reloading ExilePather ({recoveryKind}).");
            ExilePather.Reload(true);
            return true;
        }

        private void MarkPathingHealthy()
        {
            if (_pathingRecoveryAwaitingValidation)
            {
                GlobalLog.Info($"[{Name}] Follow path restored after ExilePather reload.");
                _pathingRecoveryAwaitingValidation = false;
            }

            _postAreaChangePathCheckPending = false;
            _consecutiveInvalidPaths = 0;
        }

        private async Task<bool> TryOpenNearbyDoor()
        {
            var cache = CombatAreaCache.Current;

            return await TryInteractWithNearbyObject(
                cache.Blockages,
                obj =>
                {
                    var blockObj = obj.Object;
                    return blockObj != null && blockObj.IsTargetable && 
                           ((blockObj as TriggerableBlockage)?.IsOpened != true);
                },
                obj => cache.Blockages.Remove(obj),
                obj => $"Opening mechanism: {obj.Object.Name}"
            );
        }

        private async Task<bool> TryOpenNearbyChest()
        {
            var cache = CombatAreaCache.Current;
            var isDeepwaterEncounter = IsDeepwaterEncounter();

            // Combine regular chests, special chests, and unique strongboxes from cache
            // Filter out Izaro treasure chests to avoid interacting with them
            var cachedObjects = cache.Chests
                .Concat(cache.SpecialChests)
                .Concat(cache.Strongboxes.Where(s => s.Rarity == Rarity.Unique))
                .Where(obj => obj.Object?.Metadata != null && !obj.Object.Metadata.Contains("Metadata/Chests/Labyrinth/Izaro"));

            // In Deepwater, do not select a chest unless its position is safely covered by a lantern.
            // Keep uncovered chests in the cache so they can become eligible after the leader places a lantern.
            if (isDeepwaterEncounter)
            {
                cachedObjects = cachedObjects.Where(obj =>
                {
                    var chest = obj.Object as Chest;
                    return chest != null && IsInsideDeepwaterLanternSafetyRadius(chest.Position);
                });
            }

            return await TryInteractWithNearbyObject(
                cachedObjects,
                obj =>
                {
                    var chest = obj.Object as Chest;
                    return chest != null && !chest.IsOpened && chest.IsTargetable;
                },
                obj =>
                {
                    cache.Chests.Remove(obj);
                    cache.SpecialChests.Remove(obj);
                    if (obj is CachedStrongbox)
                        cache.Strongboxes.Remove(obj as CachedStrongbox);
                },
                obj => $"Opening chest: {obj.Object.Name}"
            );
        }

        private static bool IsInsideDeepwaterLanternSafetyRadius(Vector2i position)
        {
            return ObjectManager.Objects.Any(obj =>
            {
                if (obj == null || !obj.IsValid || obj.Metadata != DeepwaterLanternMetadata)
                    return false;

                var dx = position.X - obj.Position.X;
                var dy = position.Y - obj.Position.Y;
                return dx * dx + dy * dy <= DeepwaterChestSafetyRadiusSqr;
            });
        }

        private async Task<bool> TryInteractWithNearbyRecipe()
        {
            var cache = CombatAreaCache.Current;

            return await TryInteractWithNearbyObject(
                cache.CraftingRecipe,
                obj =>
                {
                    var recipe = obj.Object as CraftingRecipe;
                    return recipe != null && !recipe.IsOpened && recipe.IsTargetable;
                },
                obj => cache.CraftingRecipe.Remove(obj),
                obj => "Interacting with crafting recipe"
            );
        }

        private async Task<bool> TryOptInToNearbyMercenary(Player leader)
        {
            var settings = FollowBotSettings.Instance.Follow;
            if (!settings.MercenaryOptIn)
                return false;

            if (World.CurrentArea == null || !World.CurrentArea.IsCombatArea)
                return false;

            var cache = CombatAreaCache.Current;
            var mercenaryCandidate = cache.Mercenaries
                .Where(m => !_failedMercenaryIds.Contains(m.Id))
                .Select(m => new
                {
                    Cached = m,
                    Mercenary = m.Object as Mercenary
                })
                .Where(m => m.Mercenary != null)
                .OrderBy(m => m.Mercenary.Position.Distance(leader.Position))
                .FirstOrDefault(m => m.Mercenary.Position.Distance(leader.Position) <= settings.MercenaryLeaderDistance);

            if (mercenaryCandidate == null)
                return false;

            var cachedMercenary = mercenaryCandidate.Cached;
            var mercenary = mercenaryCandidate.Mercenary;
            if (mercenary == null || mercenary.IsFriendly)
            {
                cache.Mercenaries.Remove(cachedMercenary);
                return false;
            }

            // The leader-distance gate prevents every follower from independently
            // searching the area. The player-distance gate keeps Opt_In in range.
            if (mercenary.Distance > settings.MercenaryFollowerDistance)
                return false;

            if (IsWaitingForDuelVisible(mercenary))
            {
                cache.Mercenaries.Remove(cachedMercenary);
                return true;
            }

            if (!mercenary.CanOpt_In)
                return false;

            GlobalLog.Info($"[{Name}] Opting in to mercenary encounter: {mercenary.MercenaryName}");

            for (var attempt = 1; attempt <= MercenaryOptInAttempts; attempt++)
            {
                mercenary = cachedMercenary.Object as Mercenary;
                if (mercenary == null || mercenary.IsFriendly)
                {
                    cache.Mercenaries.Remove(cachedMercenary);
                    return true;
                }

                if (IsWaitingForDuelVisible(mercenary))
                {
                    cache.Mercenaries.Remove(cachedMercenary);
                    return true;
                }

                if (mercenary.Position.Distance(leader.Position) > settings.MercenaryLeaderDistance ||
                    mercenary.Distance > settings.MercenaryFollowerDistance)
                {
                    GlobalLog.Debug($"[{Name}] Mercenary moved outside the configured opt-in ranges.");
                    return false;
                }

                if (!await CloseMercenaryEncounterUi())
                {
                    GlobalLog.Warn($"[{Name}] Unable to close the mercenary encounter window.");
                    break;
                }

                KeyManager.ClearAllKeyStates();

                if (mercenary.Distance > MercenaryInteractionDistance)
                {
                    GlobalLog.Debug($"[{Name}] Moving closer to mercenary before opt-in. Distance: {(int)mercenary.Distance}.");
                    await mercenary.WalkablePosition().ComeAtOnce(MercenaryInteractionDistance);
                }

                await StopMovementForMercenaryOptIn();
                await Wait.SleepSafe(MercenaryUiStabilizationMs);

                mercenary = cachedMercenary.Object as Mercenary;
                if (mercenary == null || mercenary.IsFriendly)
                {
                    cache.Mercenaries.Remove(cachedMercenary);
                    return true;
                }

                if (IsWaitingForDuelVisible(mercenary))
                {
                    cache.Mercenaries.Remove(cachedMercenary);
                    return true;
                }

                if (mercenary.Position.Distance(leader.Position) > settings.MercenaryLeaderDistance ||
                    mercenary.Distance > settings.MercenaryFollowerDistance)
                {
                    GlobalLog.Debug($"[{Name}] Mercenary moved outside the configured opt-in ranges before clicking.");
                    return false;
                }

                if (!mercenary.CanOpt_In)
                    return false;

                if (!IsMercenaryOptInVisible(mercenary))
                {
                    GlobalLog.Debug($"[{Name}] Mercenary opt-in element is not visible.");
                    return false;
                }

                GlobalLog.Debug($"[{Name}] Clicking mercenary opt-in. Attempt: {attempt}/{MercenaryOptInAttempts}.");
                if (!await ClickMercenaryOptIn(mercenary))
                {
                    GlobalLog.Warn($"[{Name}] Mercenary opt-in cursor verification failed.");
                    if (attempt < MercenaryOptInAttempts)
                        await Wait.SleepSafe(150, 250);
                    continue;
                }

                await Wait.For(
                    () =>
                    {
                        var liveMercenary = cachedMercenary.Object as Mercenary;
                        return liveMercenary == null ||
                               liveMercenary.IsFriendly ||
                               IsWaitingForDuelVisible(liveMercenary) ||
                               LokiPoe.InGameState.MercenaryEncounterUi.IsOpened;
                    },
                    "mercenary opt-in result",
                    100,
                    MercenaryWaitingForDuelTimeoutMs);

                mercenary = cachedMercenary.Object as Mercenary;
                if (mercenary == null || mercenary.IsFriendly || IsWaitingForDuelVisible(mercenary))
                {
                    cache.Mercenaries.Remove(cachedMercenary);
                    return true;
                }

                if (LokiPoe.InGameState.MercenaryEncounterUi.IsOpened)
                {
                    GlobalLog.Warn($"[{Name}] Mercenary encounter window opened instead of opting in. Closing it before retry.");
                    if (!await CloseMercenaryEncounterUi())
                        break;
                }

                if (attempt < MercenaryOptInAttempts)
                    await Wait.SleepSafe(150, 250);
            }

            _failedMercenaryIds.Add(cachedMercenary.Id);
            GlobalLog.Warn($"[{Name}] Mercenary opt-in did not reach the Waiting for Duel state: {mercenary.MercenaryName}");
            return true;
        }

        private static async Task<bool> ClickMercenaryOptIn(Mercenary mercenary)
        {
            var element = mercenary?.Ui?.Opt_InElement;
            if (element == null || !element.IsVisible || !element.IsEnable)
                return false;

            // The two-argument overload uses normalized positions within the
            // element. 0.5, 0.5 is its deterministic geometric center.
            var clickPosition = element.CenterClickLocation(0.5, 0.5);
            MouseManager.SetMousePosition(clickPosition, false);
            await Wait.SleepSafe(50);

            // Refresh the element after moving the cursor in case the overhead UI shifted.
            element = mercenary.Ui?.Opt_InElement;
            if (element == null || !element.IsVisible || !element.IsEnable)
                return false;

            var rect = element.GetClientRect();
            var mousePosition = MouseManager.GetMousePosition();
            if (!ContainsScreenPosition(rect, clickPosition) || !ContainsScreenPosition(rect, mousePosition))
            {
                GlobalLog.Warn($"[FollowTask] Mercenary opt-in moved before click. Target: {clickPosition}, cursor: {mousePosition}, bounds: {rect}.");
                return false;
            }

            GlobalLog.Debug($"[FollowTask] Mercenary opt-in cursor verified. Target: {clickPosition}, cursor: {mousePosition}, bounds: {rect}.");
            MouseManager.ClickLMB(clickPosition.X, clickPosition.Y);
            await StopMovementForMercenaryOptIn();
            return true;
        }

        private static bool ContainsScreenPosition(SharpDX.RectangleF rect, Vector2i position)
        {
            return position.X >= rect.X &&
                   position.X <= rect.X + rect.Width &&
                   position.Y >= rect.Y &&
                   position.Y <= rect.Y + rect.Height;
        }

        private static async Task StopMovementForMercenaryOptIn()
        {
            PlayerMoverManager.MoveTowards(LokiPoe.MyPosition);
            await Coroutines.FinishCurrentAction(true);
            KeyManager.ClearAllKeyStates();
        }

        private static async Task<bool> CloseMercenaryEncounterUi()
        {
            if (!LokiPoe.InGameState.MercenaryEncounterUi.IsOpened)
                return true;

            await Coroutines.CloseBlockingWindows();
            return await Wait.For(
                () => !LokiPoe.InGameState.MercenaryEncounterUi.IsOpened,
                "mercenary encounter window closing",
                50,
                MercenaryUiCloseTimeoutMs);
        }

        private static bool IsMercenaryOptInVisible(Mercenary mercenary)
        {
            return mercenary?.Ui?.Opt_InElement?.IsVisible == true;
        }

        private static bool IsWaitingForDuelVisible(Mercenary mercenary)
        {
            return mercenary?.Ui?.Opt_InElement != null &&
                   ContainsVisibleText(mercenary.Ui.Opt_InElement, "Waiting for Duel");
        }

        private static bool ContainsVisibleText(Element element, string text)
        {
            if (element == null || !element.IsVisible)
                return false;

            if (string.Equals(element.Text, text, System.StringComparison.OrdinalIgnoreCase))
                return true;

            if (element.Children == null)
                return false;

            return element.Children.Any(child => ContainsVisibleText(child, text));
        }

        private async Task<bool> TryClickNearbyShrine()
        {
            var cache = CombatAreaCache.Current;

            return await TryInteractWithNearbyObject(
                cache.Shrines,
                obj =>
                {
                    var shrine = obj.Object as Shrine;
                    return shrine != null && !shrine.IsDeactivated && shrine.IsTargetable;
                },
                obj => cache.Shrines.Remove(obj),
                obj => $"Clicking shrine: {obj.Object.Name}"
            );
        }

        private async Task<bool> TryActivateNearbySpawner()
        {
            const int TouchRadius = 10;
            var maxDist = FollowBotSettings.Instance.Follow.MirageSpawnerDistance;
            var cache = CombatAreaCache.Current;

            var spawner = cache.MirageSpawners
                .Where(o => !_touchedSpawnerIds.Contains(o.Id) && o.Position.Distance <= maxDist)
                .OrderBy(o => o.Position.Distance)
                .FirstOrDefault(o => o.Position.Distance <= maxDist &&
                                     ExilePather.PathDistance(Me.Position, o.Position, true, true) <= maxDist);

            if (spawner == null)
                return false;

            // Already close enough — mark as touched and move on
            if (spawner.Position.Distance <= TouchRadius)
            {
                _touchedSpawnerIds.Add(spawner.Id);
                GlobalLog.Debug($"[FollowTask] Activated Mirage spawner #{spawner.Id} at distance {(int)spawner.Position.Distance}.");
                return false; // Return false so the bot immediately continues to follow
            }

            // Move toward the spawner
            CustomSkills.PhaseRun();
            Move.Towards(spawner.Position, "activating Mirage spawner");
            return true;
        }

        private async Task<bool> TryActivateNearbyGoldenLantern(double leaderDistance)
        {
            var settings = FollowBotSettings.Instance.Follow;
            if (!settings.ActivateGoldenLanterns || leaderDistance > settings.GoldenLanternDistance)
                return false;

            var cache = CombatAreaCache.Current;
            var lantern = cache.GoldenLanterns
                .Where(o => !_touchedGoldenLanternIds.Contains(o.Id))
                .Select(o => new
                {
                    Cached = o,
                    Object = o.Object
                })
                .Where(o => o.Object != null && o.Object.IsValid && o.Object.IsTargetable)
                .Where(o => o.Cached.Position.Distance <= settings.GoldenLanternDistance)
                .Where(o => ExilePather.PathDistance(Me.Position, o.Cached.Position, true, true) <= settings.GoldenLanternDistance)
                .Where(o => IsInsideDeepwaterLanternSafetyRadius(o.Object.Position))
                .OrderBy(o => o.Cached.Position.Distance)
                .FirstOrDefault();

            if (lantern == null)
                return false;

            if (lantern.Cached.Position.Distance <= DeepwaterGoldenLanternTouchRadius)
            {
                _touchedGoldenLanternIds.Add(lantern.Cached.Id);
                cache.GoldenLanterns.Remove(lantern.Cached);
                GlobalLog.Debug($"[FollowTask] Activated Deepwater golden lantern #{lantern.Cached.Id} at distance {(int)lantern.Cached.Position.Distance}.");
                return false;
            }

            Move.Towards(lantern.Cached.Position, "activating Deepwater golden lantern");
            return true;
        }

        private async Task<bool> TryUseAreaSpecificTransition(double leaderDistance)
        {
            var areaId = World.CurrentArea.Id;

            // Define transition requirements per area: (transitionNames[], maxTransitionDistance, minLeaderDistance)
            string[] transitionNames;
            int maxTransitionDistance;
            int minLeaderDistance;

            switch (areaId)
            {
                case "1_4_6_2": // The Belly of the Beast Level 2
                    transitionNames = new[] { "The Bowels of the Beast" };
                    maxTransitionDistance = 30;
                    minLeaderDistance = 100;
                    break;

                case "1_4_6_3": // The Bowels of the Beast
                case "2_9_10_2": // The Bowels of the Beast (Act 9)
                    transitionNames = new[] { "Shavronne's Arena", "Maligaro's Arena", "Doedre's Arena" };
                    maxTransitionDistance = 30;
                    minLeaderDistance = 100;
                    break;

                default:
                    return false;
            }

            if (leaderDistance <= minLeaderDistance)
                return false;

            var cache = CombatAreaCache.Current;
            var transition = cache.AreaTransitions
                .FirstOrDefault(t => transitionNames.Contains(t.Name) && t.Position.Distance < maxTransitionDistance);

            if (transition == null)
                return false;

            var areaTransition = transition.Object;
            if (areaTransition == null || !areaTransition.IsTargetable)
                return false;

            GlobalLog.Debug($"[FollowTask] Leader is far ({leaderDistance}), using transition: {transition.Name}");
            await Coroutines.InteractWith(areaTransition);
            cache.AreaTransitions.Remove(transition);
            return true;
        }

        private async Task<bool> TryUseTrialReturnPortal(double leaderDistance)
        {
            if (!LokiPoe.LabyrinthTrialAreaIds.Contains(World.CurrentArea.Id))
                return false;

            if (leaderDistance <= 80)
                return false;

            var portal = LokiPoe.ObjectManager.Objects
                .FirstOrDefault(x => x.Metadata == "Metadata/Terrain/Labyrinth/Objects/LabyrinthTrialReturnPortal");

            if (portal == null || !portal.IsTargetable || portal.Distance > 90)
                return false;

            GlobalLog.Debug($"[FollowTask] Leader is far ({leaderDistance}), using trial return portal.");
            await portal.WalkablePosition().ComeAtOnce();
            await PlayerAction.Interact(portal);
            await Wait.SleepSafe(300, 500);
            return true;
        }

        private async Task<bool> TryInteractWithNearbyObject(
            IEnumerable<CachedObject> objects,
            System.Func<CachedObject, bool> isValidFunc,
            System.Action<CachedObject> removeFromCacheAction,
            System.Func<CachedObject, string> logMessageFunc)
        {
            var cachedObject = objects
                .Where(o => !_failedObjectIds.Contains(o.Id))
                .OrderBy(o => o.Position.Distance)
                .FirstOrDefault(o => o.Position.Distance < InteractionDistance &&
                                     ExilePather.PathDistance(Me.Position, o.Position, true, true) < InteractionDistance);

            if (cachedObject == null)
                return false;

            if (!isValidFunc(cachedObject))
            {
                removeFromCacheAction(cachedObject);
                return false;
            }

            var obj = cachedObject.Object;
            var pos = obj.WalkablePosition();

            // Move close to the object if needed
            if (pos.Distance > 20)
            {
                await pos.ComeAtOnce();
            }

            GlobalLog.Debug($"[FollowTask] {logMessageFunc(cachedObject)}");

            for (int i = 1; i <= MaxInteractionAttempts; i++)
            {
                if (!isValidFunc(cachedObject))
                {
                    GlobalLog.Debug($"[FollowTask] {obj.Name} is no longer valid, aborting retries.");
                    removeFromCacheAction(cachedObject);
                    return false;
                }

                if (await PlayerAction.Interact(obj))
                {
                    removeFromCacheAction(cachedObject);
                    return true;
                }

                GlobalLog.Debug($"[FollowTask] Failed to interact with {obj.Name}. Attempt: {i}/{MaxInteractionAttempts}.");
            }

            _failedObjectIds.Add(cachedObject.Id);
            return false;
        }

        private AreaTransition GetRottingCoreTransition(Player leaderPlayerEntry)
        {
            var leaderPosition = leaderPlayerEntry.Position;
            var areatransition = ObjectManager.GetObjectsByType<AreaTransition>()
                .FirstOrDefault(x => x.Name == "The Black Core");
            if (areatransition == null)
                areatransition = ObjectManager.GetObjectsByType<AreaTransition>()
                    .FirstOrDefault(x => x.Name == "The Black Heart" && x.Distance < 140);
            if (areatransition == null && leaderPosition.X < 900)
            {
                areatransition =
                    ObjectManager.GetObjectsByType<AreaTransition>()
                        .FirstOrDefault(x => x.Name == "Shavronne's Sorrow" && x.Distance < 120);
            }
            else if (areatransition == null && leaderPosition.X < 1325)
            {
                areatransition =
                    ObjectManager.GetObjectsByType<AreaTransition>()
                        .FirstOrDefault(x => x.Name == "Maligaro's Misery" && x.Distance < 140);
            }
            else if (areatransition == null && leaderPosition.X < 2103)
            {
                areatransition =
                    ObjectManager.GetObjectsByType<AreaTransition>()
                        .FirstOrDefault(x => x.Name == "Doedre's Despair" && x.Distance < 140);
            }
            return areatransition;
        }

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Events.Messages.AreaChanged)
            {
                _leaderzoningSw.Reset();
                _pathingRecoveryCooldown.Reset();
                _postAreaChangePathCheckPending = true;
                _pathingRecoveryAwaitingValidation = false;
                _consecutiveInvalidPaths = 0;
                _failedObjectIds.Clear();
                _failedMercenaryIds.Clear();
                _touchedSpawnerIds.Clear();
                _touchedGoldenLanternIds.Clear();

                // Convert WaitingForNewInstance flag into a running stopwatch.
                // This fires AFTER loading completes, so the 5s wait starts in the new zone.
                if (WaitingForNewInstance)
                {
                    WaitingForNewInstance = false;
                    NewInstanceWaitSw.Restart();
                }
            }
            return MessageResult.Unprocessed;
        }
    }
}
