using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.NativeWrappers;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot;
using FollowBot.Class;
using FollowBot.SimpleEXtensions;
using FollowBot.SimpleEXtensions.CommonTasks;
using FollowBot.SimpleEXtensions.Global;
using log4net;
using Message = DreamPoeBot.Loki.Bot.Message;


namespace FollowBot.SimpleEXtensions.CommonTasks
{
    public class CombatTask : ITask
    {
        private readonly int _leashRange;

        public CombatTask(int leashRange)
        {
            _leashRange = leashRange;
        }

        public async Task<bool> Run()
        {
            if (!FollowBotSettings.Instance.ShouldKill) return false;
            if (!World.CurrentArea.IsCombatArea)
                return false;
			if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead)
				return false;
			
			var lead = LokiPoe.InstanceInfo.PartyMembers.FirstOrDefault(x => x.MemberStatus == PartyStatus.PartyLeader);
			var leadname = lead.PlayerEntry.Name;
			//var lead = LokiPoe.ObjectManager.GetObjectsByType<Player>().FirstOrDefault(x => x.Name != LokiPoe.Me.Name);
			//var leadSmite = FollowBot.leader;
			//var aura = FollowBot.Leader.Auras.All(x => x.Name == "Smite Aura");
			//GlobalLog.Debug($"test ",aura);
			//var aura = LokiPoe.Me.Auras.FirstOrDefault(x => x.Name == "Smite Aura");
			if (!LokiPoe.InGameState.PartyHud.IsInSameZone(leadname) || FollowBot.Leader == null || FollowBot.Leader.HasBuff("Smite Aura"))
				return false;

			
				

            var routine = RoutineManager.Current;

            routine.Message(new Message("SetLeash", this, _leashRange));

            var res = await routine.Logic(new Logic("hook_combat", this));
            return res == LogicResult.Provided;
        }

        #region Unused interface methods

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public void Start()
        {
        }

        public void Tick()
        {
        }

        public void Stop()
        {
        }

        public string Name => "CombatTask (Leash " + _leashRange + ")";

        public string Description => "This task executes routine logic for combat.";

        public string Author => "NotYourFriend original from EXVault";

        public string Version => "1.0";

        #endregion
    }
}
