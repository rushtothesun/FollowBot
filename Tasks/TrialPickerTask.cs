using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using System.Linq;
using System.Threading.Tasks;

namespace FollowBot.Tasks
{
    public class TrialPickerTask : ITask
    {
        public string Author => "Letale, Rushtothesun";

        public string Description => "Trial picker task";

        public string Name => "TrialPicker";

        public string Version => "1.0.0.0";

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {

            return MessageResult.Unprocessed;
        }

        public async Task<bool> Run()
        {
            if (!LokiPoe.LabyrinthTrialAreaIds.Contains(LokiPoe.CurrentWorldArea.Id))
            {
                return false;
            }

            var me = LokiPoe.Me;
            if (me.IsAscendencyTrialCompleted(LokiPoe.CurrentWorldArea.Id))
            {
                return false;
            }

            NetworkObject trial = LokiPoe.ObjectManager.Objects.FirstOrDefault(x => x.Metadata.Contains("LabyrinthTrialPlaque"));
            if (trial != null && trial.PathExists() && me.Position.Distance(trial.Position) < 30)
            {
                GlobalLog.Debug($"[{Name}] Find trial : [{trial.Name}]");

                await trial.WalkablePosition().TryComeAtOnce();
                if (await PlayerAction.Interact(trial))
                {
                    await Coroutines.FinishCurrentAction(true);
                    await Wait.SleepSafe(300, 500);
                    return true;
                }
            }
            return false;
        }

        public void Start()
        {
            GlobalLog.Info($"[{Name}] Task Loaded.");
        }

        public void Stop()
        {
        }

        public void Tick()
        {
        }
    }
}