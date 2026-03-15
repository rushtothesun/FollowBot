using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.Class;
using FollowBot.SimpleEXtensions;
using FollowBot.SimpleEXtensions.Global;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static DreamPoeBot.Loki.Game.LokiPoe;


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
        public const int NewInstanceWaitMs = 7000;
        private Vector2i _lastSeenMasterPosition;
        private Stopwatch _leaderzoningSw;
        private HashSet<int> _failedObjectIds = new HashSet<int>();
        private HashSet<int> _touchedSpawnerIds = new HashSet<int>();
        public static bool ShouldCreateNewInstance = false;
        public static bool WaitingForNewInstance = false;
        public static Stopwatch NewInstanceWaitSw = new Stopwatch();

        public void Start()
        {
            GlobalLog.Info($"[{Name}] Task Loaded.");
            FollowBot.Leader = null;
            _lastSeenMasterPosition = Vector2i.Zero;
            _leaderzoningSw = new Stopwatch();
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

            if (ExilePather.PathExistsBetween(mypos, ExilePather.FastWalkablePositionFor(leaderPos)))
                _lastSeenMasterPosition = leaderPos;

            // Handle specific area transitions when leader is far away
            if (await TryUseAreaSpecificTransition(distance))
                return true;

            // Try to interact with nearby crafting recipes (always enabled, no setting needed)
            if (await TryInteractWithNearbyRecipe())
                return true;

            // Try to click nearby shrines
            if (FollowBotSettings.Instance.Follow.ClickShrines && await TryClickNearbyShrine())
                return true;

            // Try to open nearby chests (only if leader is close)
            if (FollowBotSettings.Instance.Loot.ShouldOpenChests && distance <= 60 && await TryOpenNearbyChest())
                return true;

            // Try to activate Mirage spawners
            if (FollowBotSettings.Instance.Follow.ActivateMirageSpawners && await TryActivateNearbySpawner())
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
                    if (Me.HasAura("Grace Period"))
                    {
                        if (!_leaderzoningSw.IsRunning)
                        {
                            GlobalLog.Debug($"Grace period detected, this mean we just zoned and are waiting for the leader to finish loading.");
                            _leaderzoningSw.Start();
                        }
                        if (_leaderzoningSw.IsRunning && _leaderzoningSw.ElapsedMilliseconds < 10000)
                            return true;
                    }

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

        private async Task<bool> TryOpenNearbyChest()
        {
            var cache = CombatAreaCache.Current;

            // Combine regular chests, special chests, and unique strongboxes from cache
            var cachedObjects = cache.Chests
                .Concat(cache.SpecialChests)
                .Concat(cache.Strongboxes.Where(s => s.Rarity == Rarity.Unique));

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
            var success = await PlayerAction.InteractWithoutDelay(obj, MaxInteractionAttempts);

            if (success)
            {
                removeFromCacheAction(cachedObject);
            }
            else
            {
                // Mark as failed to prevent retry
                _failedObjectIds.Add(cachedObject.Id);
            }

            return success;
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
                _failedObjectIds.Clear();
                _touchedSpawnerIds.Clear();

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
