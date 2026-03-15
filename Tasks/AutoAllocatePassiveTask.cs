using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.Helpers;
using FollowBot.SimpleEXtensions;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace FollowBot.Tasks
{
    public class AutoAllocatePassiveTask : ITask
    {
        private static bool _forceRunOnce = false;
        private static readonly Interval _runCooldown = new Interval(5000);
        private static readonly Dictionary<string, List<TargetPassiveNode>> _urlCache = new Dictionary<string, List<TargetPassiveNode>>();

        public string Name => "AutoAllocatePassiveTask";
        public string Description => "Task to automatically allocate passive skill points.";
        public string Author => "Rushtothesun, inspiration from https://github.com/exApiTools/PassiveSkillTreePlanter";
        public string Version => "1.0.0.0";

        public static void ForceTrigger()
        {
            _forceRunOnce = true;
            GlobalLog.Info("[AutoAllocatePassiveTask] Force trigger requested via command.");
        }

        public void Start()
        {
            _urlCache.Clear();
            GlobalLog.Debug("[AutoAllocatePassiveTask] Cache cleared on start.");
        }
        public void Stop() { }
        public void Tick() { }

        public async Task<bool> Run()
        {
            if (!FollowBotSettings.Instance.PassiveTree.EnableAutoAllocation && !_forceRunOnce)
                return false;

            if (!LokiPoe.IsInGame)
                return false;

            // 1. Points Available?
            int points = LokiPoe.InstanceInfo.PassiveSkillPointsAvailable;
            if (points <= 0)
            {
                _forceRunOnce = false;
                return false;
            }

            // 2. Cooldown & Safety Checks (Skip if forced)
            if (!_forceRunOnce)
            {
                if (!_runCooldown.Elapsed)
                    return false;

                // Safe Zone Check
                if (FollowBotSettings.Instance.PassiveTree.OnlyInSafeZone && !LokiPoe.Me.IsInTown && !LokiPoe.Me.IsInHideout)
                    return false;

                // Combat Check
                if (PassiveTreeHelper.IsInCombat(FollowBotSettings.Instance.PassiveTree.SafeMonsterDistance))
                    return false;

                // Leader Stationary Check
                if (FollowBotSettings.Instance.PassiveTree.CheckLeaderStationary && !LokiPoe.Me.IsInTown && !LokiPoe.Me.IsInHideout)
                {
                    var leader = FollowBot.Leader;
                    if (leader != null && leader.IsMoving)
                        return false;
                }
            }

            // 3. Pre-check: Do any URLs actually need allocation? (Avoid opening tree if not needed)
            var allocatedIds = LokiPoe.InstanceInfo.PassiveSkillIds.ToHashSet();
            var urlList = FollowBotSettings.Instance.PassiveTree.PassiveTreeUrls;
            bool needsAny = false;

            if (urlList != null)
            {
                foreach (var urlEntry in urlList)
                {
                    if (urlEntry == null || string.IsNullOrWhiteSpace(urlEntry.Url)) continue;

                    if (!_urlCache.TryGetValue(urlEntry.Url, out var targetNodes))
                    {
                        targetNodes = PassiveTreeHelper.GetTargetNodesFromUrl(urlEntry.Url);
                        _urlCache[urlEntry.Url] = targetNodes;
                    }

                    if (targetNodes.Any(n => !allocatedIds.Contains(n.Id)))
                    {
                        needsAny = true;
                        break;
                    }
                }
            }

            if (!needsAny && !_forceRunOnce)
            {
                return false; // All nodes in all URLs are already allocated
            }

            GlobalLog.Info($"[AutoAllocatePassiveTask] Starting Passives allocation. Character has {points} points available.");
            _forceRunOnce = false;

            // 4. Execution (Atomic)
            try
            {
                // Close other windows first to avoid conflicts
                await Coroutines.CloseBlockingWindows();
                await Wait.SleepSafe(LokiPoe.Random.Next(200, 400));

                // Open Tree via Simulation
                if (!await EnsureTreeOpened())
                {
                    return true;
                }

                bool allocatedAny = false;
                int pointsSpent = 0;

                // Iterate through the URLs in order
                if (urlList == null)
                {
                    GlobalLog.Warn("[AutoAllocatePassiveTask] PassiveTreeUrls collection is null.");
                    return true;
                }

                foreach (var urlEntry in urlList)
                {
                    if (urlEntry == null || string.IsNullOrWhiteSpace(urlEntry.Url)) continue;

                    if (!_urlCache.TryGetValue(urlEntry.Url, out var targetNodes))
                    {
                        targetNodes = PassiveTreeHelper.GetTargetNodesFromUrl(urlEntry.Url);
                        _urlCache[urlEntry.Url] = targetNodes;
                    }

                    if (!targetNodes.Any()) continue;

                    // Calculate how many of these are NOT yet allocated using the robust InstanceInfo check
                    var unallocatedTargets = targetNodes.Where(n => !allocatedIds.Contains(n.Id)).ToList();
                    if (!unallocatedTargets.Any())
                    {
                        GlobalLog.Debug($"[AutoAllocatePassiveTask] All {targetNodes.Count} nodes from URL are already allocated.");
                        continue;
                    }

                    GlobalLog.Info($"[AutoAllocatePassiveTask] Parsed {targetNodes.Count} nodes from URL. {unallocatedTargets.Count} require allocation. Points available: {points - pointsSpent}.");

                    // Keep allocating until this URL's targets are exhausted or we run out of points
                    while (pointsSpent < points)
                    {
                        var reachableTargets = PassiveTreeHelper.GetReachableTargetNodes(unallocatedTargets);
                        if (!reachableTargets.Any()) break;

                        var nextNode = reachableTargets.First();

                        LokiPoe.InGameState.ChoosePassiveError result;
                        if (LokiPoe.InGameState.SkillsUi.IsMastery((int)nextNode.Id) && nextNode.MasteryHash != 0)
                        {
                            GlobalLog.Info($"[AutoAllocatePassiveTask] Allocating Mastery node {nextNode.Id} with hash {nextNode.MasteryHash}.");
                            result = LokiPoe.InGameState.SkillsUi.ChoosePassiveMastery((int)nextNode.Id, new List<int> { (int)nextNode.MasteryHash });
                        }
                        else
                        {
                            GlobalLog.Info($"[AutoAllocatePassiveTask] Allocating node {nextNode.Id}.");
                            result = LokiPoe.InGameState.SkillsUi.ChoosePassive((int)nextNode.Id);
                        }

                        if (result == LokiPoe.InGameState.ChoosePassiveError.None)
                        {
                            allocatedAny = true;
                            pointsSpent++;
                            unallocatedTargets.Remove(nextNode);
                            await Wait.SleepSafe(LokiPoe.Random.Next(200, 500));
                        }
                        else
                        {
                            GlobalLog.Error($"[AutoAllocatePassiveTask] Failed to choose passive {nextNode.Id}: {result}");
                            break;
                        }
                    }

                    // Strict termination: if we finished this URL and still have points, but no targets left in this URL, we stop or move to next URL.
                    // The 'while' loop already handles pointsSpent < points.
                    if (unallocatedTargets.Any())
                    {
                        GlobalLog.Warn($"[AutoAllocatePassiveTask] Stop reached for this URL. {unallocatedTargets.Count} targets remains but none are reachable or points exhausted.");
                        break;
                    }
                }

                if (allocatedAny)
                {
                    GlobalLog.Info("[AutoAllocatePassiveTask] Confirming allocation.");
                    LokiPoe.InGameState.SkillsUi.ConfirmOperation();
                    await Wait.SleepSafe(LokiPoe.Random.Next(700, 1000));
                }
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[AutoAllocatePassiveTask] Exception during execution: {ex.Message}");
            }
            finally
            {
                // Close tree when done via Simulation
                await EnsureTreeClosed();
            }

            return true; // We handled the tick
        }

        private async Task<bool> EnsureTreeOpened()
        {
            if (LokiPoe.InGameState.SkillsUi.IsOpened) return true;

            var combo = LokiPoe.Input.Binding.open_passive_skills_panel_combo;
            GlobalLog.Debug($"[AutoAllocatePassiveTask] Opening passive tree via {combo.Key}.");
            LokiPoe.Input.SimulateKeyEvent(combo.Key, true, false, false, combo.Modifier);

            bool opened = await Wait.For(() => LokiPoe.InGameState.SkillsUi.IsOpened, "Passive Tree opening", 100, 3000);
            if (opened)
            {
                await Wait.SleepSafe(LokiPoe.Random.Next(400, 800));
            }
            return opened;
        }

        private async Task EnsureTreeClosed()
        {
            if (!LokiPoe.InGameState.SkillsUi.IsOpened) return;

            var combo = LokiPoe.Input.Binding.open_passive_skills_panel_combo;
            GlobalLog.Debug($"[AutoAllocatePassiveTask] Closing passive tree via {combo.Key}.");
            LokiPoe.Input.SimulateKeyEvent(combo.Key, true, false, false, combo.Modifier);

            await Wait.For(() => !LokiPoe.InGameState.SkillsUi.IsOpened, "Passive Tree closing", 100, 3000);
            await Wait.SleepSafe(LokiPoe.Random.Next(300, 600));
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
