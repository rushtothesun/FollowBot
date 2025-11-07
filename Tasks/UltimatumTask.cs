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
        private static uint _lastMapHash;
        private readonly Stopwatch _ultimatumStopwatch = new Stopwatch();
        private static bool _voteSent;

        public async Task<bool> Run()
        {
            //Ultimatum Handling
            if (!LokiPoe.InGameState.UltimatumTrialRewardUi.IsOpened)
            {
                _voteSent = false;
            }

            if (LokiPoe.InGameState.UltimatumTrialRewardUi.IsOpened)
            {
                if (_voteSent)
                {
                    GlobalLog.Debug("[UltimatumTask] I already Voted, waiting for the rest of the party to vote.");
                    return true;
                }

                if (LokiPoe.InGameState.UltimatumTrialRewardUi.GetVotedForReward() >= 1)
                {
                    var takeResult = LokiPoe.InGameState.UltimatumTrialRewardUi.TakeReward();
                    if (takeResult == LokiPoe.InGameState.UltimatumTrialRewardUi.TakeRewardResult.None)
                    {
                        GlobalLog.Debug("[UltimatumTask] TakeReward successful. Setting _voteSent to true.");
                        _voteSent = true;
                    }
                    else
                    {
                        GlobalLog.Error($"[UltimatumTask] TakeReward failed with result: {takeResult}. Will retry on next tick.");
                    }
                    return true;
                }

                foreach (var opt in LokiPoe.InGameState.UltimatumTrialRewardUi.Options)
                {
                    if (LokiPoe.InGameState.UltimatumTrialRewardUi.GetVotedForOption(opt) >= 1)
                    {
                        LokiPoe.InGameState.UltimatumTrialRewardUi.SelectOption(opt);

                        var result = LokiPoe.InGameState.UltimatumTrialRewardUi.AcceptTrial();
                        if (result == LokiPoe.InGameState.UltimatumTrialRewardUi.AcceptTrialResult.None)
                        {
                            GlobalLog.Debug("[UltimatumTask] AcceptTrial returned successful. Setting _voteSent to true.");
                            GlobalLog.Debug("[UltimatumTask] If the bot is stuck not voting, then DPB failed to lock in the vote.");
                            _voteSent = true;
                        }
                        else
                        {
                            GlobalLog.Error($"[UltimatumTask] AcceptTrial failed with result: {result}. Will retry on next tick.");
                        }

                        return true;
                    }
                }
                // If we are here, the UI is open but no one has voted yet.
                // Return true to ensure the task keeps running and doesn't fall through to other logic.
                return true;
            }

            var ultimatum = LokiPoe.ObjectManager.Objects.FirstOrDefault<UltimatumChallengeInteractable>();
            if (ultimatum != null && ultimatum.IsTrialCompleted && !_ultimatumCompletedInThisInstance)
            {
                _ultimatumCompletedInThisInstance = true;
                _lastMapHash = LokiPoe.LocalData.AreaHash; // Store the hash of the current map
                GlobalLog.Debug($"[UltimatumTask] Trial completed, Setting _lastMapHash to: {_lastMapHash} from LokiPoe.LocalData.AreaHash");
                if (FollowBotSettings.Instance.ShouldLootUltimatum)
                {
                    GlobalLog.Debug("[UltimatumTask] Portal After Ultimatum is on in settings. Starting portal search.");
                    _ultimatumStopwatch.Start();
                }
            }

            if (_ultimatumStopwatch.IsRunning)
            {
                GlobalLog.Debug($"[UltimatumTask] Ultimatum stopwatch is running. Elapsed time: {_ultimatumStopwatch.Elapsed.TotalSeconds} seconds.");

                if (_ultimatumStopwatch.Elapsed.TotalSeconds > FollowBotSettings.Instance.UltimatumLootTimer)
                {
                    GlobalLog.Debug($"[UltimatumTask] Resetting stopwatch.");

                    _ultimatumStopwatch.Reset();
                }
                else
                {
                    // Directly attempt to find and enter a portal. The function will handle the check.
                    GlobalLog.Debug($"[UltimatumTask] Attempting to find a portal.");
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
            {
                GlobalLog.Debug("[UltimatumTask] Not ingame.");
                return false;
            }

            GlobalLog.Debug("[UltimatumTask] Checking for portals.");

            var portal = LokiPoe.ObjectManager.GetObjectsByType<Portal>().FirstOrDefault(x =>
                x.IsTargetable &&
                x.Distance <= 80 &&
                (TownNames.Contains(x.Name) || x.Name.Contains("Hideout"))
            );

            if (portal == null)
            {
                GlobalLog.Debug("[UltimatumTask] No portal found.");
                ShouldEnterPortal = false;
                return false;
            }

            if (portal.Distance > 30)
            {
                GlobalLog.Debug("[UltimatumTask] Found portal, moving.");
                await Move.AtOnce(portal.Position, "Move to portal");
                return true;
            }

            GlobalLog.Debug($"[UltimatumTask] Attempting to enter portal: {portal.Name}");
            if (await Coroutines.InteractWith<Portal>(portal))
            {
                await Coroutines.ReactionWait();
                GlobalLog.Debug($"[UltimatumTask] Turning off shouldEnterPortal.");
                ShouldEnterPortal = false;
                _ultimatumStopwatch.Reset();
                return true;
            }

            return false;
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Events.Messages.AreaChanged)
            {
                var area = LokiPoe.CurrentWorldArea;
                if (!area.IsTown && !area.IsHideoutArea && !area.IsCorruptedArea) // We are in a map
                {
                    // Only perform the hash check if we have a completed ultimatum to reset.
                    if (_ultimatumCompletedInThisInstance)
                    {
                        // If we are in a new map instance, reset the flag.
                        GlobalLog.Debug($"[UltimatumTask] Comparing Hashes: Current Area Hash ({LokiPoe.LocalData.AreaHash}) != Last Map Hash ({_lastMapHash})");
                        if (LokiPoe.LocalData.AreaHash != _lastMapHash)
                        {
                            GlobalLog.Debug($"[UltimatumTask] New map, resetting Ultimatum checks.");
                            _ultimatumCompletedInThisInstance = false;
                            _ultimatumStopwatch.Reset();
                            ShouldEnterPortal = false;
                        }
                        else
                        {
                            GlobalLog.Debug($"[UltimatumTask] Hashes are the same. Not resetting.");
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