using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using log4net;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FollowBot.Tasks
{
    public class LevelGemsTask : ITask
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private readonly WaitTimer _levelWait = WaitTimer.FiveSeconds;

        public string Name { get { return "LevelGemsTask"; } }
        public string Description { get { return "This task will Level gems."; } }
        public string Author { get { return "Alcor75"; } }
        public string Version { get { return "0.0.0.1"; } }


        public void Start()
        {

        }
        public void Stop()
        {

        }
        public void Tick()
        {

        }

        public async Task<bool> Run()
        {
            // Don't update while we are not in the game.
            if (!LokiPoe.IsInGame)
            {
                return false;
            }
            // Don't try to do anything when the escape state is active.
            if (LokiPoe.StateManager.IsEscapeStateActive)
            {
                return false;
            }

            // Don't level skill gems if we're dead.
            if (LokiPoe.Me.IsDead)
            {
                return false;
            }
            // Can't level skill gems under this scenario either.
            if (LokiPoe.InGameState.SkillsUi.IsOpened)
            {
                return false;
            }
            // Can't level gems when favor Ui is open
            if (LokiPoe.InGameState.RitualFavorsUi.IsOpened)
            {
                return false;
            }

            // Check if feature is enabled
            if (!FollowBotSettings.Instance.LevelGems)
            {
                return false;
            }

            // Only check for skillgem leveling at a fixed interval.
            if (!_levelWait.IsFinished)
            {
                return false;
            }

            // If we have icons on the hud to process.
            if (LokiPoe.InGameState.SkillGemHud.AreIconsDisplayed)
            {
                // If the InventoryUi is already opened, skip this logic and let the next set run.
                if (!LokiPoe.InGameState.InventoryUi.IsOpened)
                {
                    // EARLY CHECK: Do we have ANY actionable gems?
                    // This prevents FinishCurrentAction() spam when only greyed-out gems are visible
                    var prependingGems = LokiPoe.InGameState.SkillGemHud.ListOfPendingSkillGems;

                    if (prependingGems == null || prependingGems.Count == 0)
                    {
                        if (FollowBotSettings.Instance.GemDebugStatements)
                        {
                            Log.DebugFormat("[LevelGemsTask] No pending gems on HUD.");
                        }
                        return false;
                    }

                    // Count gems that can actually be leveled OR need to be dismissed
                    bool hasActionableGem = false;
                    foreach (var gem in prependingGems)
                    {
                        // Gem can be leveled
                        if (gem.GemCanLevelUp)
                        {
                            hasActionableGem = true;
                            break;
                        }
                        
                        // Gem needs to be dismissed (in ignore list)
                        if (ContainsHelper(gem.Item.Name, gem.Item.SkillGemLevel))
                        {
                            hasActionableGem = true;
                            break;
                        }
                    }

                    // Exit early if only greyed-out gems (no action needed)
                    if (!hasActionableGem)
                    {
                        if (FollowBotSettings.Instance.GemDebugStatements)
                        {
                            Log.DebugFormat("[LevelGemsTask] All {0} gems are greyed out, skipping until requirements met.", prependingGems.Count);
                        }
                        return false;
                    }

                    // NOW proceed with blocking operations (we have work to do)
                    await Coroutines.CloseBlockingWindows();

                    // We need to let skills finish casting, because of 2.6 changes.
                    await Coroutines.FinishCurrentAction();
                    await Coroutines.LatencyWait();

                    // RE-FETCH both lists - state may have changed during waits (combat finished, player leveled, gem gained XP, etc.)
                    var pendingElements = LokiPoe.InGameState.SkillGemHud.ListOfPendingSkillElements;
                    var pendingGems = LokiPoe.InGameState.SkillGemHud.ListOfPendingSkillGems;

                    // Safety check for both lists
                    if (pendingElements == null || pendingElements.Count == 0 || pendingGems == null || pendingGems.Count == 0)
                    {
                        if (FollowBotSettings.Instance.GemDebugStatements)
                        {
                            Log.DebugFormat("[LevelGemsTask] No pending elements (UI issue?)");
                        }
                        return false;
                    }

                    // PHASE 1: Dismiss ignored gems (one at a time)
                    for (int i = 0; i < pendingElements.Count && i < pendingGems.Count; i++)
                    {
                        var element = pendingElements[i];
                        var gem = pendingGems[i];

                        // Check if gem is in the ignore list
                        if (ContainsHelper(gem.Item.Name, gem.Item.SkillGemLevel))
                        {
                            if (FollowBotSettings.Instance.GemDebugStatements)
                            {
                                Log.DebugFormat("[LevelGemsTask] Dismissing ignored gem: {0} [Level: {1}]", gem.Item.Name, gem.Item.SkillGemLevel);
                            }

                            // Right-click Child[1] (the button) to dismiss
                            if (element.Children != null && element.Children.Count > 1)
                            {
                                var buttonElement = element.Children[1];
                                var clickPos = buttonElement.CenterClickLocation();
                                
                                MouseManager.SetMousePosition(clickPos, useRandomPos: false);
                                Thread.Sleep(LokiPoe.Random.Next(25, 55));
                                MouseManager.ClickRMB();
                                Thread.Sleep(LokiPoe.Random.Next(25, 55));

                                if (FollowBotSettings.Instance.GemDebugStatements)
                                {
                                    Log.DebugFormat("[LevelGemsTask] Dismissed gem at position {0}", clickPos);
                                }

                                // Re-check list on next run
                                return false;
                            }
                        }
                    }

                    // PHASE 2: Count levelable gems
                    int levelableCount = 0;
                    int firstLevelableIndex = -1;

                    // Primary approach: Use GemCanLevelUp property (more reliable)
                    for (int i = 0; i < pendingGems.Count; i++)
                    {
                        var gem = pendingGems[i];

                        if (gem.GemCanLevelUp)
                        {
                            if (firstLevelableIndex == -1)
                            {
                                firstLevelableIndex = i;
                            }
                            levelableCount++;
                        }
                    }

                    // Fallback approach: Check Child[3] for "Click to level up" text
                    // Uncomment this block and comment out the GemCanLevelUp loop above if API breaks
                    /*
                    for (int i = 0; i < pendingElements.Count; i++)
                    {
                        var element = pendingElements[i];

                        // Check Child[3] for "Click to level up" text
                        if (element.Children != null && element.Children.Count > 3)
                        {
                            var textElement = element.Children[3];
                            if (textElement != null &&
                                textElement.Text != null &&
                                textElement.Text.Contains("Click to level up"))
                            {
                                if (firstLevelableIndex == -1)
                                {
                                    firstLevelableIndex = i;
                                }
                                levelableCount++;
                            }
                        }
                    }
                    */

                    if (FollowBotSettings.Instance.GemDebugStatements)
                    {
                        Log.DebugFormat("[LevelGemsTask] Found {0} levelable gems", levelableCount);
                    }

                    // PHASE 3: Level gems
                    if (levelableCount >= 2 && FollowBotSettings.Instance.UseLevelAllButton)
                    {
                        // Use LevelAll button if enabled and 2+ gems ready
                        Log.InfoFormat("[LevelGemsTask] Using LevelAll() for {0} gems", levelableCount);
                        LokiPoe.InGameState.SkillGemHud.LevelAll();
                    }
                    else if (levelableCount >= 1)
                    {
                        // Level one gem at a time
                        if (firstLevelableIndex >= 0 && firstLevelableIndex < pendingElements.Count)
                        {
                            var element = pendingElements[firstLevelableIndex];
                            
                            if (element.Children != null && element.Children.Count > 1)
                            {
                                var buttonElement = element.Children[1];
                                var clickPos = buttonElement.CenterClickLocation();
                                
                                if (FollowBotSettings.Instance.GemDebugStatements)
                                {
                                    Log.DebugFormat("[LevelGemsTask] Leveling single gem at index {0}", firstLevelableIndex);
                                }

                                MouseManager.SetMousePosition(clickPos, useRandomPos: false);
                                Thread.Sleep(LokiPoe.Random.Next(90, 150));
                                MouseManager.ClickLMB();
                                Thread.Sleep(LokiPoe.Random.Next(90, 150));

                                Log.InfoFormat("[LevelGemsTask] Leveled gem at position {0}", clickPos);
                            }
                        }
                    }

                    return false;
                }
            }

            // Just wait 5-10s between checks.
            _levelWait.Reset(TimeSpan.FromMilliseconds(LokiPoe.Random.Next(5000, 10000)));

            return false;
        }


        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }


        private static bool ContainsHelper(string name, int level)
        {
            foreach (string entry in FollowBotSettings.Instance.GlobalNameIgnoreList)
            {
                string[] ignoreArray = entry.Split(',');
                if (ignoreArray.Length == 1)
                {
                    if (ignoreArray[0].Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
                else
                {
                    if (ignoreArray[0].Equals(name, StringComparison.OrdinalIgnoreCase) && level >= Convert.ToInt32(ignoreArray[1]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

    }
}