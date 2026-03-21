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
        private static int _lastTreeOpenLevel = 0;
        private static readonly Interval _runCooldown = new Interval(5000);
        private static readonly Dictionary<string, List<TargetPassiveNode>> _urlCache = new Dictionary<string, List<TargetPassiveNode>>();
        private static readonly Stopwatch _leaderStationarySw = Stopwatch.StartNew();

        public string Name => "AutoAllocatePassiveTask";
        public string Description => "Task to automatically allocate passive skill points.";
        public string Author => "Rushtothesun, inspiration from https://github.com/exApiTools/PassiveSkillTreePlanter";
        public string Version => "1.0.0.0";

        public static void ForceTrigger()
        {
            _forceRunOnce = true;
            _runCooldown.Restart(0);
            GlobalLog.Info("[AutoAllocatePassiveTask] Force trigger requested via command.");
        }

        public static void InvalidateCache()
        {
            _lastTreeOpenLevel = 0;
            _runCooldown.Restart(0);
            GlobalLog.Debug("[AutoAllocatePassiveTask] Reachability cache invalidated.");
        }

        public void Start()
        {
            _urlCache.Clear();
            _lastTreeOpenLevel = 0;
        }
        public void Stop() { }
        public void Tick() { }

        public async Task<bool> Run()
        {
            if (!FollowBotSettings.Instance.PassiveTree.EnableAutoAllocation && !_forceRunOnce)
            {
                // Silent, no log needed for disabled
                return false;
            }

            if (!LokiPoe.IsInGame)
            {
                return false;
            }

            if (!_forceRunOnce && !_runCooldown.Elapsed)
            {
                return false;
            }

            _runCooldown.Restart(LokiPoe.Random.Next(4000, 5500));

            var dictP = LokiPoe.InGameState.SkillsUi.Dictionary_Passive;
            var dictA = LokiPoe.InGameState.SkillsUi.Dictionary_Ascend;

            // 1. Points Available? (Regular or Ascendancy)
            int regularPoints = LokiPoe.InstanceInfo.PassiveSkillPointsAvailable;
            int ascendancyPoints = LokiPoe.InstanceInfo.AscendencySkillPointsAvailable;

            if (regularPoints <= 0 && ascendancyPoints <= 0)
            {
                _forceRunOnce = false;
                return false;
            }

            // Prime the passive tree cache if the level has changed since last open.
            // The game only populates CanBeAllocate reachability after the tree panel has been opened at least once,
            // and this cache invalidates on level-up.
            int currentLevel = LokiPoe.Me.Level;
            if (currentLevel != _lastTreeOpenLevel)
            {
                GlobalLog.Debug($"[AutoAllocatePassiveTask] Level changed ({_lastTreeOpenLevel} -> {currentLevel}). Priming reachability cache.");
                await PrimeReachabilityCache();
                _lastTreeOpenLevel = currentLevel;
            }

            // 2. Safety Checks (Skip if forced)
            if (!_forceRunOnce)
            {
                // Safe Zone Check
                if (FollowBotSettings.Instance.PassiveTree.OnlyInSafeZone && !LokiPoe.Me.IsInTown && !LokiPoe.Me.IsInHideout)
                {
                    return false;
                }

                // Combat Check
                if (PassiveTreeHelper.IsInCombat(FollowBotSettings.Instance.PassiveTree.SafeMonsterDistance))
                {
                    return false;
                }

                // Leader Stationary Check — leader must be still for at least 5 seconds
                if (FollowBotSettings.Instance.PassiveTree.CheckLeaderStationary && !LokiPoe.Me.IsInTown && !LokiPoe.Me.IsInHideout)
                {
                    var leader = FollowBot.Leader;
                    if (leader != null)
                    {
                        if (leader.IsMoving)
                        {
                            _leaderStationarySw.Restart();
                            return false;
                        }

                        if (_leaderStationarySw.ElapsedMilliseconds < 5000)
                        {
                            return false;
                        }
                    }
                }
            }

            // 3. Pre-check: Do any URLs actually need allocation OR have reachable nodes?
            var allocatedIds = LokiPoe.InstanceInfo.PassiveSkillIds.Select(id => (int)id).ToHashSet();
            var urlList = FollowBotSettings.Instance.PassiveTree.PassiveTreeUrls;
            bool anyUnallocatedFound = false;
            bool anyReachableFound = false;

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

                    var unallocated = targetNodes.Where(n => !allocatedIds.Contains(n.Id) && (dictP.ContainsKey(n.Id) || dictA.ContainsKey(n.Id))).ToList();
                    if (unallocated.Any())
                    {
                        anyUnallocatedFound = true;
                        // Check if any of these are reachable WITHOUT opening the tree
                        var reachable = PassiveTreeHelper.GetReachableTargetNodes(unallocated);
                        if (reachable.Any())
                        {
                            // Point-Type Awareness: Only open if we have the right points for these specific reachable nodes
                            bool canSpendOnRegular = regularPoints > 0 && reachable.Any(n => !LokiPoe.InGameState.SkillsUi.IsAscendPassive(n.Id));
                            bool canSpendOnAscend = ascendancyPoints > 0 && reachable.Any(n => LokiPoe.InGameState.SkillsUi.IsAscendPassive(n.Id)) && FollowBotSettings.Instance.PassiveTree.EnableAscendancyAllocation;

                            if (canSpendOnRegular || canSpendOnAscend)
                            {
                                anyReachableFound = true;
                            }
                        }
                        // URL Pathing: This URL is not finished. We stop here and don't check subsequent URLs.
                        break;
                    }
                }
            }

            if (!anyUnallocatedFound && !_forceRunOnce)
            {
                return false;
            }

            if (!anyReachableFound && !_forceRunOnce)
            {
                return false;
            }

            GlobalLog.Info($"[AutoAllocatePassiveTask] Starting Passives allocation. Regular points: {regularPoints}, Ascendancy points: {ascendancyPoints}.");
            _forceRunOnce = false;

            // 4. Execution (Atomic)
            // Virtual Allocation List to track clicks in this session for pathfinding
            var sessionAllocatedIds = LokiPoe.InstanceInfo.PassiveSkillIds.Select(id => (int)id).ToHashSet();

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

                // Main Allocation Loop - stay in here until we are actually stuck or done
                while (true)
                {
                    bool allocatedSomethingInThisPass = false;
                    var currentAllocatedIds = LokiPoe.InstanceInfo.PassiveSkillIds.Select(id => (int)id).ToHashSet();
                    sessionAllocatedIds = new HashSet<int>(currentAllocatedIds);

                    // Iterate through the URLs in order
                    if (urlList == null) break;
                    foreach (var urlEntry in urlList)
                    {
                        bool urlAllocatedAny = false;
                        if (urlEntry == null || string.IsNullOrWhiteSpace(urlEntry.Url)) continue;

                        if (!_urlCache.TryGetValue(urlEntry.Url, out var targetNodes))
                        {
                            targetNodes = PassiveTreeHelper.GetTargetNodesFromUrl(urlEntry.Url);
                            _urlCache[urlEntry.Url] = targetNodes;
                        }
                        if (!targetNodes.Any()) continue;

                        // Keep allocating until this URL's targets are exhausted or we run out of all points
                        while (true)
                        {
                            var unallocatedTargets = targetNodes.Where(n => !sessionAllocatedIds.Contains(n.Id) && (dictP.ContainsKey(n.Id) || dictA.ContainsKey(n.Id))).ToList();

                            if (!unallocatedTargets.Any()) break;

                            // Use virtual list for reachability check
                            var reachableTargets = PassiveTreeHelper.GetReachableTargetNodes(unallocatedTargets, sessionAllocatedIds);
                            if (!reachableTargets.Any()) break;

                            var nextNode = reachableTargets.First();

                            // Check point availability for this specific node type
                            bool isAscendancy = LokiPoe.InGameState.SkillsUi.IsAscendPassive(nextNode.Id);
                            if (isAscendancy)
                            {
                                if (!FollowBotSettings.Instance.PassiveTree.EnableAscendancyAllocation) break;
                                if (LokiPoe.InstanceInfo.AscendencySkillPointsAvailable <= 0) break;

                                if (!LokiPoe.InGameState.SkillsUi.AscendencyUi.IsOpened)
                                {
                                    GlobalLog.Info("[AutoAllocatePassiveTask] Opening Ascendancy panel.");
                                    LokiPoe.InGameState.SkillsUi.AscendencyUi.Toggle();
                                    await Wait.SleepSafe(LokiPoe.Random.Next(600, 900));
                                }
                            }
                            else
                            {
                                if (LokiPoe.InstanceInfo.PassiveSkillPointsAvailable <= 0) break;

                                // Close ascendancy panel if it was open — it can block regular nodes behind it
                                if (LokiPoe.InGameState.SkillsUi.AscendencyUi.IsOpened)
                                {
                                    GlobalLog.Info("[AutoAllocatePassiveTask] Closing Ascendancy panel before allocating regular node.");
                                    LokiPoe.InGameState.SkillsUi.AscendencyUi.Toggle();
                                    await Wait.SleepSafe(LokiPoe.Random.Next(600, 900));
                                }
                            }

                            LokiPoe.InGameState.ChoosePassiveError result;
                            if (LokiPoe.InGameState.SkillsUi.IsMastery(nextNode.Id) && nextNode.MasteryHash != 0)
                            {
                                GlobalLog.Info($"[AutoAllocatePassiveTask] Allocating Mastery node {nextNode.Id} with hash {nextNode.MasteryHash}.");
                                result = LokiPoe.InGameState.SkillsUi.ChoosePassiveMastery(nextNode.Id, new List<int> { (int)nextNode.MasteryHash });
                            }
                            else if (isAscendancy)
                            {
                                GlobalLog.Info($"[AutoAllocatePassiveTask] Allocating Ascendancy node {nextNode.Id}.");
                                result = LokiPoe.InGameState.SkillsUi.AscendencyUi.ChooseAscendancyPassive(nextNode.Id);
                            }
                            else
                            {
                                GlobalLog.Info($"[AutoAllocatePassiveTask] Allocating node {nextNode.Id}.");
                                result = LokiPoe.InGameState.SkillsUi.ChoosePassive(nextNode.Id);
                            }

                            if (result == LokiPoe.InGameState.ChoosePassiveError.None)
                            {
                                urlAllocatedAny = true;
                                allocatedSomethingInThisPass = true;
                                sessionAllocatedIds.Add(nextNode.Id);
                                await Wait.SleepSafe(LokiPoe.Random.Next(200, 500));
                            }
                            else
                            {
                                GlobalLog.Error($"[AutoAllocatePassiveTask] Failed to choose passive {nextNode.Id}: {result}");
                                break;
                            }
                        }

                        if (urlAllocatedAny)
                        {
                            GlobalLog.Info("[AutoAllocatePassiveTask] Confirming allocation for current URL.");
                            LokiPoe.InGameState.SkillsUi.ConfirmOperation();

                            // Wait for server to sync so the next pass/URL can see the new reachable path
                            await Wait.SleepSafe(LokiPoe.Random.Next(1200, 1800));
                        }

                        var remainingUnallocated = targetNodes.Where(n => !sessionAllocatedIds.Contains(n.Id) && (dictP.ContainsKey(n.Id) || dictA.ContainsKey(n.Id))).ToList();
                        if (remainingUnallocated.Any())
                        {
                            break;
                        }
                    }

                    if (!allocatedSomethingInThisPass) break;

                    if (LokiPoe.InstanceInfo.PassiveSkillPointsAvailable <= 0 && LokiPoe.InstanceInfo.AscendencySkillPointsAvailable <= 0) break;

                    GlobalLog.Debug("[AutoAllocatePassiveTask] Points remaining, starting another allocation pass.");
                }

            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[AutoAllocatePassiveTask] Exception during execution: {ex.Message}");
            }
            finally
            {
                await EnsureTreeClosed();
            }

            return true;
        }

        private async Task PrimeReachabilityCache()
        {
            var combo = LokiPoe.Input.Binding.open_passive_skills_panel_combo;
            LokiPoe.Input.SimulateKeyEvent(combo.Key, true, false, false, combo.Modifier);
            await Coroutines.CloseBlockingWindows();
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
