using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using log4net;
using System.Linq;
using System.Threading.Tasks;

namespace FollowBot.Tasks
{
    public class TrialPickerTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Author => "Letale";

        public string Description => "Trial picker task";

        public string Name => "TrialPicker";

        public string Version => "0.0.0.0";

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

            if (LokiPoe.LabyrinthTrialAreaIds.Contains(LokiPoe.CurrentWorldArea.Id))
            {
                var me = LokiPoe.Me;
                if (me.IsAscendencyTrialCompleted(LokiPoe.CurrentWorldArea.Id)) return true;

                NetworkObject trial = LokiPoe.ObjectManager.Objects.FirstOrDefault(x => x.Metadata.Contains("LabyrinthTrialPlaque"));
                if (trial != null && trial.PathExists() && me.Position.Distance(trial.Position) < 30)
                {
                    Log.Debug($"[{Name}] Find trial : [{trial.Name}]");

                    await trial.WalkablePosition().ComeAtOnce();
                    await PlayerAction.Interact(trial);

                }
            }
            return true;
        }

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
    }
}