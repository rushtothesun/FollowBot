﻿using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using log4net;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using static DreamPoeBot.Loki.Game.LokiPoe;


namespace FollowBot.Tasks
{
    class FollowTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();

        public string Name { get { return "FollowTask"; } }
        public string Description { get { return "This task will Follow a Leader."; } }
        public string Author { get { return "NotYourFriend, origial code from Unknown"; } }
        public string Version { get { return "0.0.0.1"; } }
        private Vector2i _lastSeenMasterPosition;
        private Stopwatch _leaderzoningSw;

        public void Start()
        {
            Log.InfoFormat("[{0}] Task Loaded.", Name);
            FollowBot.Leader = null;
            _lastSeenMasterPosition = Vector2i.Zero;
            _leaderzoningSw = new Stopwatch();
        }
        public void Stop()
        {

        }
        public void Tick()
        {

        }

        public async Task<bool> Run()
        {
            if (!FollowBotSettings.Instance.ShouldFollow)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return false;
            }
            if (!IsInGame || Me.IsDead || Me.IsInTown || Me.IsInHideout)
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

            var labportaltest = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Labyrinth/Objects/LabyrinthTrialReturnPortal");
            if (labportaltest != null)
            {
                Log.DebugFormat("[{0}] Found walkable lab portal.", Name);
                if (LokiPoe.Me.Position.Distance(labportaltest.Position) > 15 && LokiPoe.Me.Position.Distance(labportaltest.Position) > 20)
                {
                    var walkablePosition = ExilePather.FastWalkablePositionFor(labportaltest, 13);

                    // Cast Phase run if we have it.
                    FollowBot.PhaseRun();

                    Move.Towards(walkablePosition, "moving to lab portal");
                    return true;
                }

                var tele = await Coroutines.InteractWith(labportaltest);

                if (!tele)
                {
                    Log.DebugFormat("[{0}] lab portal error.", Name);
                }

                FollowBot.Leader = null;
                return true;
            }

            var distance = leaderPos.Distance(mypos);

            if (ExilePather.PathExistsBetween(mypos, ExilePather.FastWalkablePositionFor(leaderPos)))
                _lastSeenMasterPosition = leaderPos;


            if (distance > FollowBotSettings.Instance.MaxFollowDistance || leader?.HasCurrentAction == true && leader?.CurrentAction?.Skill?.InternalId == "Move")
            {

                var pos = ExilePather.FastWalkablePositionFor(mypos.GetPointAtDistanceBeforeEnd(
                    leaderPos,
                    Random.Next(FollowBotSettings.Instance.FollowDistance,
                        FollowBotSettings.Instance.MaxFollowDistance)));
                if (pos == Vector2i.Zero || !ExilePather.PathExistsBetween(mypos, pos))
                {
                    KeyManager.ClearAllKeyStates();
                    // First check for Grace period, that mean we have just zoned, and the leader position might be incorrect.
                    if (Me.HasAura("Grace Period"))
                    {
                        if (!_leaderzoningSw.IsRunning)
                        {
                            Log.DebugFormat($"Grace period detected, this mean we just zoned and are waiting for the leader to finish loading.");
                            _leaderzoningSw.Start();
                        }
                        if (_leaderzoningSw.IsRunning && _leaderzoningSw.ElapsedMilliseconds < 10000)
                            return true;
                    }

                    var labportal = LokiPoe.ObjectManager.GetObjectByMetadata("Metadata/Terrain/Labyrinth/Objects/LabyrinthTrialReturnPortal");
                    if (labportal != null)
                    {
                        Log.DebugFormat("[{0}] Found walkable lab portal.", Name);
                        if (LokiPoe.Me.Position.Distance(labportal.Position) > 15)
                        {
                            var walkablePosition = ExilePather.FastWalkablePositionFor(labportal, 13);

                            // Cast Phase run if we have it.
                            FollowBot.PhaseRun();

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

                    //Then check for Delve portals:
                    var delveportal = ObjectManager.GetObjectsByType<AreaTransition>().FirstOrDefault(x => x.Name == "Azurite Mine" && x.Metadata == "Metadata/MiscellaneousObject/PortalTransition");
                    if (delveportal != null)
                    {
                        Log.DebugFormat("[{0}] Found walkable delve portal.", Name);
                    RepeatBehavior1:
                        if (Me.Position.Distance(delveportal.Position) > 20)
                        {
                            if (Me.IsDead) { return true; }
                            var walkablePosition = ExilePather.FastWalkablePositionFor(delveportal, 20);

                            // Cast Phase run if we have it.
                            FollowBot.PhaseRun();

                            if (Move.Towards(walkablePosition, "moving to delve portal"))
                                goto RepeatBehavior1;
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

                    AreaTransition areatransition = null;
                    if (_lastSeenMasterPosition != Vector2i.Zero)
                        areatransition = ObjectManager.GetObjectsByType<AreaTransition>().OrderBy(x => x.Position.Distance(_lastSeenMasterPosition)).FirstOrDefault(x => ExilePather.PathExistsBetween(mypos, ExilePather.FastWalkablePositionFor(x.Position, 20)));
                    if (areatransition == null)
                    {
                        var teleport = ObjectManager.GetObjectsByName("Portal").OrderBy(x => x.Position.Distance(_lastSeenMasterPosition)).FirstOrDefault(x => ExilePather.PathExistsBetween(Me.Position, ExilePather.FastWalkablePositionFor(x.Position, 20)));
                        if (teleport == null)
                            return false;
                        Log.DebugFormat("[{0}] Found walkable Teleport.", Name);
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
                            FollowBot.PhaseRun();

                            if (Move.Towards(walkablePosition, "moving to Teleport"))
                            {
                                goto RepeatBehavior2;
                            }
                            return true;
                        }

                        var tele = await Coroutines.InteractWith(teleport);

                        if (!tele)
                        {
                            Log.DebugFormat("[{0}] Teleport error.", Name);
                        }

                        FollowBot.Leader = null;
                        return true;
                    }

                    Log.DebugFormat("[{0}] Found walkable Area Transition [{1}].", Name, areatransition.Name);

                    if (Me.Position.Distance(areatransition.Position) > 20)
                    {
                        if (Me.IsDead) { return true; }
                        var walkablePosition = ExilePather.FastWalkablePositionFor(areatransition, 20);

                        // Cast Phase run if we have it.
                        FollowBot.PhaseRun();

                        Move.Towards(walkablePosition, "moving to area transition");

                        return true;
                    }
                    var trans = await PlayerAction.TakeTransition(areatransition);

                    if (!trans)
                    {
                        Log.DebugFormat("[{0}] Areatransition error.", Name);
                    }

                    //FollowBot.Leader = null;
                    return true;
                }

                // Cast Phase run if we have it.
                FollowBot.PhaseRun();

                if (ExilePather.PathDistance(mypos, pos) < 45)
                {
                    InGameState.SkillBarHud.UseAt(FollowBot.LastBoundMoveSkillSlot, false, pos, false);
                }
                else
                    Move.Towards(pos, $"{leader.Name}");
                return true;
            }
            // Clear the move key
            ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
            //KeyManager.ClearAllKeyStates();
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
            }
            return MessageResult.Unprocessed;
        }
    }
}