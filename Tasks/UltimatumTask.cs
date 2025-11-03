using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using System.Threading.Tasks;
using System.Linq;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using System.Diagnostics;

namespace FollowBot.Tasks
{
    public class UltimatumTask : ITask
    {
        public static bool ShouldEnterPortal;
        public static string[] TownNames = { "Lioneye's Watch", "The Forest Encampment", "The Sarn Encampment", "Highgate", "Overseer's Tower", "The Bridge Encampment", "Oriath Docks", "Karui Shores" };
        private static bool _ultimatumCompletedInThisInstance;
        private static int _lastMapHash;
        private readonly Stopwatch _ultimatumStopwatch = new Stopwatch();

        public async Task<bool> Run()
        {
            //Ultimatum Handling
            if (LokiPoe.InGameState.UltimatumTrialRewardUi.IsOpened)
            {
                if (LokiPoe.InGameState.UltimatumTrialRewardUi.GetVotedForReward() >= 1)
                {
                    LokiPoe.InGameState.UltimatumTrialRewardUi.TakeReward();
                }
                foreach (var opt in LokiPoe.InGameState.UltimatumTrialRewardUi.Options)
                {
                    if (LokiPoe.InGameState.UltimatumTrialRewardUi.GetVotedForOption(opt) >= 1)
                    {
                        LokiPoe.InGameState.UltimatumTrialRewardUi.SelectOption(opt);
                        LokiPoe.InGameState.UltimatumTrialRewardUi.AcceptTrial();
                    }

                }
            }

            var ultimatum = LokiPoe.ObjectManager.Objects.FirstOrDefault<UltimatumChallengeInteractable>();
            if (ultimatum != null && ultimatum.IsTrialCompleted && !_ultimatumCompletedInThisInstance)
            {
                _ultimatumCompletedInThisInstance = true;
                _lastMapHash = LokiPoe.CurrentWorldArea.GetHashCode(); // Store the hash of the current map
                if (FollowBotSettings.Instance.ShouldLootUltimatum)
                {
                    _ultimatumStopwatch.Start();
                }
            }

            if (_ultimatumStopwatch.IsRunning)
            {
                if (_ultimatumStopwatch.Elapsed.TotalSeconds > FollowBotSettings.Instance.UltimatumLootTimer)
                {
                    _ultimatumStopwatch.Reset();
                }
                else
                {
                    // Directly attempt to find and enter a portal. The function will handle the check.
                    return await EnterPortal();
                }
            }

            if (ShouldEnterPortal)
            {
                return await EnterPortal();
            }

            return false;
        }

        private async Task<bool> EnterPortal()
        {
            if (!LokiPoe.IsInGame)
                return false;

            var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>().FirstOrDefault(x =>
                x.IsTargetable &&
                x.Distance < 40 &&
                (TownNames.Contains(x.Name) || x.Name.Contains("Hideout"))
            );

            if (portal == null)
            {
                ShouldEnterPortal = false;
                return false;
            }

            if (portal.Distance > 18)
            {
                await Move.AtOnce(portal.Position, "Move to portal");
                return true;
            }

            if (await Coroutines.InteractWith<Portal>(portal))
            {
                await Coroutines.ReactionWait();
                ShouldEnterPortal = false;
                return true;
            }

            return false;
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Events.Messages.AreaChanged)
            {
                var area = LokiPoe.CurrentWorldArea;
                if (!area.IsTown && !area.IsHideoutArea) // We are in a map
                {
                    // Only perform the hash check if we have a completed ultimatum to reset.
                    if (_ultimatumCompletedInThisInstance)
                    {
                        // If we are in a new map instance, reset the flag.
                        if (area.GetHashCode() != _lastMapHash)
                        {
                            _ultimatumCompletedInThisInstance = false;
                            _ultimatumStopwatch.Reset();
                            ShouldEnterPortal = false;
                        }
                    }
                }
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return LogicResult.Unprovided;
        }

        public void Start()
        {
        }

        public void Stop()
        {
        }

        public void Tick()
        {
        }

        public string Name => "UltimatumTask";
        public string Description => "This task will handle Ultimatum related logic.";
        public string Author => "Rushtothesun";
        public string Version => "1.0";
    }
}