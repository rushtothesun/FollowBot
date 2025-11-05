using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using FollowBot.Class;
using FollowBot.SimpleEXtensions;
using System.Threading.Tasks;

namespace FollowBot.Tasks
{
    public class AsyncCustomSkillsTask : ITask
    {
        public string Name => "AsyncCustomSkillsTask";
        public string Description => "Handles custom async skill usage based on settings.";
        public string Author => "Rushtothesun";
        public string Version => "1.0.0";

        public void Start() { }
        public void Stop() { }
        public void Tick() { }

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame) return false;
            if (FollowBot.Leader == null) return false;
            if (LokiPoe.Me.IsDead) return false;
            if (LokiPoe.CurrentWorldArea.IsTown) return false;
            if (LokiPoe.CurrentWorldArea.Id == "HeistHub") return false;
            if (!LokiPoe.CurrentWorldArea.IsCombatArea) return false;

            if (LokiPoe.Me.HasAura("Grace Period"))
            {
                GlobalLog.Debug("[AsyncCustomSkillsTask] Find grace period, wait player moves.");
                return false;
            }

            var settings = FollowBotSettings.Instance;

            if (settings.EnableSummonRagingSpirits) await AsyncCustomSkills.SummonRagingSpirits();
            if (settings.EnableSummonSkeletons) await AsyncCustomSkills.SummonSkeletons();
            if (settings.EnableBreachGraft1) await AsyncCustomSkills.BreachGraft1();

            return false;
        }

        public Task<LogicResult> Logic(Logic logic) => Task.FromResult(LogicResult.Unprovided);
        public MessageResult Message(Message message) => MessageResult.Unprocessed;
    }
}