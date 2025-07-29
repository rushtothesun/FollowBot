﻿using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using FollowBot.SimpleEXtensions;
using FollowBot.Class;
using log4net;
using System.Threading.Tasks;
using static DreamPoeBot.Loki.Game.LokiPoe;

namespace FollowBot.Tasks
{
    class PreCombatFollowTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        private int FollowFailCounter = 0;

        public string Name { get { return "PreCombatFollowTask"; } }
        public string Description { get { return "This task will keep the bot under a specific distance from the leader, in combat situation."; } }
        public string Author { get { return "NotYourFriend, origial code from Unknown"; } }
        public string Version { get { return "0.0.0.1"; } }


        public void Start()
        {
            Log.InfoFormat("[{0}] Task Loaded.", Name);
        }
        public void Stop()
        {

        }
        public void Tick()
        {

        }

        public Task<bool> Run()
        {
            if (!FollowBotSettings.Instance.ShouldKill) return Task.FromResult(false);
            if (!FollowBotSettings.Instance.ShouldFollow) return Task.FromResult(false);
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead || LokiPoe.Me.IsInTown || LokiPoe.Me.IsInHideout)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return Task.FromResult(false);
            }

            var leader = FollowBot.Leader;

            if (leader == null)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return Task.FromResult(false);
            }

            var leaderPos = leader.Position;
            var mypos = LokiPoe.Me.Position;
            if (leaderPos == Vector2i.Zero || mypos == Vector2i.Zero)
            {
                ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                return Task.FromResult(false);
            }

            var distance = leaderPos.Distance(mypos);

            if (distance > FollowBotSettings.Instance.MaxCombatDistance)
            {
                var pos = ExilePather.FastWalkablePositionFor(mypos.GetPointAtDistanceBeforeEnd(
                    leaderPos,
                    LokiPoe.Random.Next(FollowBotSettings.Instance.FollowDistance,
                        FollowBotSettings.Instance.MaxFollowDistance)));
                if (pos == Vector2i.Zero || !ExilePather.PathExistsBetween(mypos, pos))
                {
                    ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
                    return Task.FromResult(false);
                }

                // Cast Phase run if we have it.
                CustomSkills.PhaseRun();

                if (ExilePather.PathDistance(mypos, pos) < 45)
                    LokiPoe.InGameState.SkillBarHud.UseAt(FollowBot.LastBoundMoveSkillSlot, false, pos, false);
                else
                    Move.Towards(pos, $"{FollowBot.Leader.Name}");
                return Task.FromResult(true);
            }
            ProcessHookManager.SetKeyState(FollowBot.LastBoundMoveSkillKey, 0);
            ////KeyManager.ClearAllKeyStates();
            return Task.FromResult(false);
        }

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }
    }
}
