using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using System;
using System.Threading.Tasks;
using System.Linq;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using System.Diagnostics;

namespace FollowBot.Tasks
{
    public class UltimatumTask : ITask
    {
        #region Fields and Constants

        // Constants
        private const int MaxPortalSearchDistance = 80;
        private const int MinPortalInteractionDistance = 30;

        // Static state
        public static bool ShouldEnterPortal;
        public static readonly string[] TownNames = { "Lioneye's Watch", "The Forest Encampment", "The Sarn Encampment", "Highgate", "Overseer's Tower", "The Bridge Encampment", "Oriath Docks", "Karui Shores" };
        private static bool _ultimatumCompletedInThisInstance;
        private static uint _lastMapHash;
        private static bool _voteSent;

        // Instance state
        private readonly Stopwatch _portalLootingTimer = new Stopwatch();

        #endregion

        #region Main Task Logic

        public async Task<bool> Run()
        {
            // Reset vote state when UI is closed
            _voteSent = _voteSent && LokiPoe.InGameState.UltimatumTrialRewardUi.IsOpened;

            if (LokiPoe.InGameState.UltimatumTrialRewardUi.IsOpened)
            {
                if (_voteSent)
                {
                    bool isStuck = false;
                    try
                    {
                        if (LokiPoe.InGameState.UltimatumTrialRewardUi.SelectedOption == null)
                        {
                            isStuck = true;
                        }
                    }
                    catch (Exception)
                    {
                        // Exception indicates stuck state - option selection failed
                        isStuck = true;
                    }

                    if (isStuck)
                    {
                        GlobalLog.Error("[UltimatumTask] Stuck state detected! Resetting vote.");
                        _voteSent = false;
                    }

                    GlobalLog.Debug("[UltimatumTask] I already Voted, waiting for the rest of the party to vote.");
                    return true;
                }

                if (LokiPoe.InGameState.UltimatumTrialRewardUi.GetVotedForReward() >= 1)
                {
                    return TryVoteForReward();
                }

                foreach (var opt in LokiPoe.InGameState.UltimatumTrialRewardUi.Options)
                {
                    if (LokiPoe.InGameState.UltimatumTrialRewardUi.GetVotedForOption(opt) >= 1)
                    {
                        return TryAcceptTrial(opt);
                    }
                }

                // If we are here, the UI is open but no one has voted yet.
                // Return true to ensure the task keeps running and doesn't fall through to other logic.
                return true;
            }
            

            var ultimatum = LokiPoe.ObjectManager.Objects.FirstOrDefault<UltimatumChallengeInteractable>();
            if (ultimatum != null && ultimatum.IsTrialCompleted && !_ultimatumCompletedInThisInstance)
            {
                HandleUltimatumCompletion();
            }

            if (_portalLootingTimer.IsRunning)
            {
                GlobalLog.Debug($"[UltimatumTask] Portal looting timer is running. Elapsed time: {_portalLootingTimer.Elapsed.TotalSeconds:F1} seconds.");

                if (_portalLootingTimer.Elapsed.TotalSeconds > FollowBotSettings.Instance.Loot.UltimatumLootTimer)
                {
                    GlobalLog.Debug($"[UltimatumTask] Looting timer expired. Resetting.");
                    _portalLootingTimer.Reset();
                }
                else
                {
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

        #endregion

        #region Portal Handling

        private async Task<bool> EnterPortal()
        {
            if (!LokiPoe.IsInGame)
            {
                GlobalLog.Debug("[UltimatumTask] Not ingame.");
                return false;
            }

            GlobalLog.Debug("[UltimatumTask] Checking for portals.");

            var portal = FindNearbyPortal();

            if (portal == null)
            {
                GlobalLog.Debug("[UltimatumTask] No portal found.");
                ShouldEnterPortal = false;
                return false;
            }

            if (portal.Distance > MinPortalInteractionDistance)
            {
                GlobalLog.Debug($"[UltimatumTask] Portal distance: {portal.Distance:F1}. Moving closer.");
                await Move.AtOnce(portal.Position, "Move to portal");
                return true;
            }

            GlobalLog.Debug($"[UltimatumTask] Attempting to enter portal: {portal.Name}");
            if (await Coroutines.InteractWith<Portal>(portal))
            {
                await Coroutines.ReactionWait();
                GlobalLog.Debug($"[UltimatumTask] Successfully entered portal. Resetting state.");
                ShouldEnterPortal = false;
                _portalLootingTimer.Reset();
                return true;
            }

            return false;
        }

        private Portal FindNearbyPortal()
        {
            return LokiPoe.ObjectManager.GetObjectsByType<Portal>().FirstOrDefault(x =>
                x.IsTargetable &&
                x.Distance <= MaxPortalSearchDistance &&
                (TownNames.Contains(x.Name) || x.Name.Contains("Hideout"))
            );
        }

        #endregion

        #region Vote Handling

        private bool TryVoteForReward()
        {
            var voteResult = LokiPoe.InGameState.UltimatumTrialRewardUi.TakeReward();
            return HandleVoteResult(
                voteResult == LokiPoe.InGameState.UltimatumTrialRewardUi.TakeRewardResult.None,
                "TakeReward",
                voteResult);
        }

        private bool TryAcceptTrial(DreamPoeBot.Loki.Game.GameData.DatUltimatumModifiersWrapper option)
        {
            LokiPoe.InGameState.UltimatumTrialRewardUi.SelectOption(option);
            var result = LokiPoe.InGameState.UltimatumTrialRewardUi.AcceptTrial();
            return HandleVoteResult(
                result == LokiPoe.InGameState.UltimatumTrialRewardUi.AcceptTrialResult.None,
                "AcceptTrial",
                result);
        }

        private bool HandleVoteResult(bool success, string action, object result)
        {
            if (success)
            {
                GlobalLog.Debug($"[UltimatumTask] {action} successful. Setting _voteSent to true.");
                _voteSent = true;
            }
            else
            {
                GlobalLog.Error($"[UltimatumTask] {action} failed with result: {result}. Will retry on next tick.");
            }
            return true;
        }

        #endregion

        #region Ultimatum Completion Handling

        private void HandleUltimatumCompletion()
        {
            _ultimatumCompletedInThisInstance = true;
            _lastMapHash = LokiPoe.LocalData.AreaHash;
            GlobalLog.Debug($"[UltimatumTask] Trial completed. Setting _lastMapHash to: {_lastMapHash}");

            if (FollowBotSettings.Instance.Loot.ShouldLootUltimatum)
            {
                GlobalLog.Debug("[UltimatumTask] Portal After Ultimatum is enabled. Starting portal search.");
                _portalLootingTimer.Start();
            }
        }

        #endregion

        #region ITask Interface Implementation

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
                        GlobalLog.Debug($"[UltimatumTask] Comparing Hashes: Current ({LokiPoe.LocalData.AreaHash}) vs Last ({_lastMapHash})");
                        if (LokiPoe.LocalData.AreaHash != _lastMapHash)
                        {
                            GlobalLog.Debug($"[UltimatumTask] New map detected. Resetting Ultimatum state.");
                            _ultimatumCompletedInThisInstance = false;
                            _portalLootingTimer.Reset();
                            ShouldEnterPortal = false;
                        }
                        else
                        {
                            GlobalLog.Debug($"[UltimatumTask] Same map instance. Not resetting.");
                        }
                    }
                }
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
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

        #endregion
    }
}