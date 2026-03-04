using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using FollowBot.Settings;
using FollowBot.Helpers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;

namespace FollowBot.Tasks
{
    #region PoeNinjaClasses
    public class PoeNinjaItem
    {
        public string Name { get; set; }
        public int GemLevel { get; set; }
        public int GemQuality { get; set; } = 0;
        public double ChaosValue { get; set; }
        public PoeNinjaTradeFilter TradeFilter { get; set; }
    }

    public class PoeNinjaTradeFilter
    {
        public PoeNinjaQuery Query { get; set; }
    }

    public class PoeNinjaQuery
    {
        public PoeNinjaType Type { get; set; }
    }

    public class PoeNinjaType
    {
        public string Discriminator { get; set; }
    }

    public class PoeNinjaResponse
    {
        public List<PoeNinjaItem> Lines { get; set; }
    }
    #endregion

    public class DivineFontTask : ITask
    {
        private static Dictionary<string, double> _gemPrices = new Dictionary<string, double>();
        private static DateTime _lastPriceUpdateTime = DateTime.MinValue;
        private const string PriceCacheFileName = "divine_font_gem_prices.json";

        public string Name => "DivineFontTask";
        public string Description => "Divine Font Gem Handling";
        public string Author => "Rushtothesun";
        public string Version => "1.0.0";

        private bool _hasExecuted = false;
        private HashSet<DivineFontOptionType> _failedOptions = new HashSet<DivineFontOptionType>();

        public void Start() { }
        public void Stop() { }
        public void Tick() { }

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead || !World.CurrentArea.IsLabyrinthArea || !FollowBotSettings.Instance.Lab.EnableDivineFontHandling)
            {
                return false;
            }

            await UpdateGemPricesIfNeeded();

            // Check if Divine Font UI is open
            var isOpened = LokiPoe.InGameState.DivineFontUi.IsOpened;

            if (!isOpened && !_hasExecuted)
            {
                // Try to find and interact with Divine Font
                var divineFont = FindDivineFont();
                if (divineFont == null)
                {
                    return false;
                }

                if (divineFont.Distance > 40)
                {
                    GlobalLog.Info("[DivineFontTask] Divine Font too far away");
                    return false;
                }

                GlobalLog.Info("[DivineFontTask] Interacting with Divine Font");
                var interactResult = await PlayerAction.Interact(divineFont);
                if (interactResult)
                {
                    await Wait.Sleep(500);
                    // Return true to maintain control and prevent other tasks from closing the UI
                    return true;
                }

                return false;
            }

            // Only execute the main logic once though
            if (_hasExecuted)
            {
                return false;
            }

            return await ExecutePriorityLogic();
        }

        private async Task<bool> ExecutePriorityLogic()
        {
            var availableTypes = ReadAvailableOptionsFromUi();

            // 1. Check Inventory First (Fastest)
            var userOptions = FollowBotSettings.Instance.Lab.DivineFontOptions
                .Where(o => o.IsEnabled)
                .OrderBy(o => o.Priority)
                .ToList();

            foreach (var option in userOptions)
            {
                if (!availableTypes.Contains(option.Type)) continue;
                if (_failedOptions.Contains(option.Type)) continue;

                if (CanFulfillFromInventory(option))
                {
                    return await ExecuteOption(option);
                }
            }

            // 2. If we are here, we need *something* from stash.
            // Calculate requirements based on what we are missing.
            var requirements = DetermineGemRequirements(availableTypes);

            if (requirements.Any())
            {
                GlobalLog.Info("[DivineFont] Inventory check failed. Initiating Smart Stash Retrieval.");

                // Close Font UI to go to stash
                if (LokiPoe.InGameState.DivineFontUi.IsOpened)
                    await Coroutines.CloseBlockingWindows();

                // This function handles the "Check A -> Fail -> Check B -> Success" logic internally
                // and ensures we leave the stash with the best possible gem (or nothing if empty).
                await PerformSmartStashRetrieval(requirements);

                // Return true to loop back.
                // Next tick, "Check Inventory" will pass for whatever gem we managed to grab.
                return true;
            }

            GlobalLog.Info("[DivineFont] No options could be fulfilled.");
            _hasExecuted = true;
            return false;
        }

        private bool CanFulfillFromInventory(DivineFontOption option)
        {
            if (option.Type == DivineFontOptionType.TransformSpecificGem)
            {
                return FindSpecificGemInInventory(option.GemName) != null;
            }
            else if (option.Type == DivineFontOptionType.TransformRandomSameColor)
            {
                return FindGemToEnchant() != null;
            }
            return false;
        }

        private Item FindSpecificGemInInventory(string gemName)
        {
            var mainInventoryItems = InventoryUi.InventoryControl_Main.Inventory.Items;
            return mainInventoryItems?.FirstOrDefault(i =>
                i.Class == "Active Skill Gem" &&
                !i.IsCorrupted &&
                i.Name.Equals(gemName, StringComparison.OrdinalIgnoreCase));
        }

        private async Task<bool> ExecuteOption(DivineFontOption option)
        {
            Item gem = null;
            if (option.Type == DivineFontOptionType.TransformSpecificGem)
            {
                gem = FindSpecificGemInInventory(option.GemName);
            }
            else if (option.Type == DivineFontOptionType.TransformRandomSameColor)
            {
                gem = FindGemToEnchant();
            }

            if (gem == null)
            {
                GlobalLog.Error($"[DivineFontTask] Failed to find gem for option {option.Name} despite check passing.");
                return false;
            }

            return await ExecuteTransformation(gem, option.Type);
        }

        private async Task<bool> ExecuteTransformation(Item gem, DivineFontOptionType optionType)
        {
            GlobalLog.Info($"[DivineFontTask] Starting Divine Font sequence with gem: {gem.Name}");

            if (!LokiPoe.InGameState.DivineFontUi.IsOpened)
            {
                GlobalLog.Error("[DivineFontTask] Divine Font UI closed unexpectedly");
                return false;
            }

            // Place gem
            GlobalLog.Info($"[DivineFontTask] Placing gem: {gem.Name}");
            bool placed = await PlaceGemInSlot(gem);
            if (!placed) return false;

            await Wait.Sleep(300);

            // Select Transform option
            GlobalLog.Info("[DivineFontTask] Selecting Transform option");
            bool optionSelected = await SelectTransformOption(optionType);
            if (!optionSelected) return false;

            await Wait.Sleep(300);

            // Click craft button
            GlobalLog.Info("[DivineFontTask] Clicking craft button");
            bool crafted = await ClickCraftButton();
            if (!crafted) return false;

            await Wait.Sleep(2000);

            // Get gem choices
            string gem1 = GetGemNameFromTooltip(0);
            string gem2 = GetGemNameFromTooltip(1);
            string gem3 = GetGemNameFromTooltip(2);

            GlobalLog.Info($"[DivineFontTask] Gem choices: '{gem1}', '{gem2}', '{gem3}'");

            // Choose best gem
            bool gemSelected = await ChooseMostValuableGem(new[] { gem1, gem2, gem3 });
            if (!gemSelected) return false;

            await Wait.Sleep(300);

            // Confirm
            GlobalLog.Info("[DivineFontTask] Clicking confirm button");
            bool confirmed = await ClickConfirmButton();
            if (!confirmed) return false;

            await Wait.Sleep(1000);

            // Remove gem
            if (IsGemInSlot())
            {
                string gemName = GetGemNameFromInputSlot();
                GlobalLog.Info($"[DivineFontTask] Removing gem '{gemName}' from input slot");
                bool removed = await RemoveGemFromSlot();
                if (!removed) return false;
                GlobalLog.Info("[DivineFontTask] Task complete - gem successfully transformed!");
                _hasExecuted = true;
                return true;
            }

            _hasExecuted = true;
            return true;
        }

        private class GemRequirement
        {
            public DivineFontOptionType OptionType;
            public string SpecificGemName; // For specific option
            public LabSettings.GemColor ColorPreference; // For random option
        }

        private List<GemRequirement> DetermineGemRequirements(List<DivineFontOptionType> availableOptions)
        {
            var requirements = new List<GemRequirement>();
            var userOptions = FollowBotSettings.Instance.Lab.DivineFontOptions
                .Where(o => o.IsEnabled)
                .OrderBy(o => o.Priority);

            foreach (var option in userOptions)
            {
                // Only consider options actually offered by the Font
                if (!availableOptions.Contains(option.Type)) continue;
                if (_failedOptions.Contains(option.Type)) continue;

                if (option.Type == DivineFontOptionType.TransformSpecificGem)
                {
                    requirements.Add(new GemRequirement
                    {
                        OptionType = option.Type,
                        SpecificGemName = option.GemName
                    });
                }
                else if (option.Type == DivineFontOptionType.TransformRandomSameColor)
                {
                    requirements.Add(new GemRequirement
                    {
                        OptionType = option.Type,
                        ColorPreference = FollowBotSettings.Instance.Lab.Color
                    });
                }
            }
            return requirements;
        }

        private async Task<bool> PerformSmartStashRetrieval(List<GemRequirement> requirements)
        {
            // Find the stash
            var stash = LokiPoe.ObjectManager.Stash;
            if (stash == null)
            {
                GlobalLog.Debug("[DivineFontTask] No stash found near Divine Font");
                return false;
            }
            // Move to stash if too far away
            if (LokiPoe.Me.Position.Distance(stash.Position) > 20)
            {
                GlobalLog.Info("[DivineFontTask] Moving closer to stash");
                await Move.AtOnce(stash.Position, "Stash", 15);
            }

            GlobalLog.Info("[DivineFontTask] Opening stash to search for gems");

            // Interact with stash using PlayerAction
            var interactResult = await PlayerAction.Interact(stash);
            if (!interactResult)
            {
                GlobalLog.Error("[DivineFontTask] Failed to interact with stash");
                return false;
            }

            await Wait.Sleep(800); // Wait for stash UI to populate

            if (!LokiPoe.InGameState.StashUi.IsOpened)
            {
                GlobalLog.Error("[DivineFontTask] Stash UI did not open");
                return false;
            }

            // Switch to configured tab
            if (!SwitchToConfiguredStashTab())
            {
                GlobalLog.Error("[DivineFontTask] Failed to switch to configured stash tab");
                await Coroutines.CloseBlockingWindows();
                return false;
            }

            foreach (var req in requirements)
            {
                bool success = false;

                if (req.OptionType == DivineFontOptionType.TransformSpecificGem)
                {
                    // Withdraw specific gem by name (any level/quality, not corrupted)
                    success = StashHelper.WithdrawItem(i =>
                        (i.Name == req.SpecificGemName || i.FullName == req.SpecificGemName) &&
                        i.Class == "Active Skill Gem" &&
                        !i.IsCorrupted
                    );
                    if (success)
                    {
                        GlobalLog.Info($"[DivineFont] Withdrew specific gem '{req.SpecificGemName}'.");
                        break; // Done!
                    }
                    else
                    {
                        GlobalLog.Info($"[DivineFont] Specific gem '{req.SpecificGemName}' missing. Trying next option...");
                        _failedOptions.Add(req.OptionType);
                    }
                }
                else if (req.OptionType == DivineFontOptionType.TransformRandomSameColor)
                {
                    if (req.ColorPreference == LabSettings.GemColor.Smart)
                    {
                        var colorPriority = GetColorPriorityList();
                        foreach (var color in colorPriority)
                        {
                            GlobalLog.Info($"[DivineFont] Smart Stash: Checking for {color} gems...");
                            if (TryWithdrawGemByColor(color))
                            {
                                success = true;
                                GlobalLog.Info($"[DivineFont] Withdrew {color} gem (Smart Choice).");
                                break;
                            }
                        }
                    }
                    else
                    {
                        GlobalLog.Info($"[DivineFont] Checking stash for {req.ColorPreference} gems...");
                        if (TryWithdrawGemByColor(req.ColorPreference))
                        {
                            success = true;
                            GlobalLog.Info($"[DivineFont] Withdrew {req.ColorPreference} gem.");
                        }
                    }

                    if (success) break; // Done!
                    else _failedOptions.Add(req.OptionType);
                }
            }

            await Coroutines.CloseBlockingWindows();
            return true;
        }

        private List<DivineFontOptionType> ReadAvailableOptionsFromUi()
        {
            var available = new List<DivineFontOptionType>();
            var optionsContainer = GetElementByPath(70, 0, 2, 2);

            if (optionsContainer == null || optionsContainer.Children == null)
                return available;

            foreach (var option in optionsContainer.Children)
            {
                if (option.Children != null && option.Children.Count > 1)
                {
                    var textElement = option.Children[1];
                    string text = textElement.Text;
                    var type = IdentifyOptionType(text);
                    if (type.HasValue)
                        available.Add(type.Value);
                }
            }
            return available;
        }

        private DivineFontOptionType? IdentifyOptionType(string text)
        {
            // "Transform specific gem to Transfigured version"
            if (text.Contains("Transfigured") && text.Contains("version"))
                return DivineFontOptionType.TransformSpecificGem;

            // "Transform a Skill Gem to be a random Transfigured Gem of the same colour"
            if (text.Contains("random") && text.Contains("Transfigured") && text.Contains("same colour"))
                return DivineFontOptionType.TransformRandomSameColor;

            return null;
        }
        private async Task<bool> ChooseMostValuableGem(string[] gemNames)
        {
            double bestPrice = 0;
            string bestGemName = null;
            int bestGemIndex = -1;

            for (int i = 0; i < gemNames.Length; i++)
            {
                string gemName = gemNames[i];
                if (string.IsNullOrEmpty(gemName)) continue;

                if (_gemPrices.TryGetValue(gemName, out double price))
                {
                    GlobalLog.Info($"[DivineFontTask] Price for '{gemName}': {price}c");
                    if (price > bestPrice)
                    {
                        bestPrice = price;
                        bestGemName = gemName;
                        bestGemIndex = i;
                    }
                }
                else
                {
                    GlobalLog.Info($"[DivineFontTask] No price found for '{gemName}'. Assuming 0c.");
                }
            }

            if (bestGemIndex == -1)
            {
                GlobalLog.Info("[DivineFontTask] No valuable gems found or no prices available. Picking first gem by default.");
                bestGemIndex = 0;
                bestGemName = gemNames[0];
            }

            GlobalLog.Info($"[DivineFontTask] Best gem is '{bestGemName}' at {bestPrice}c (Index: {bestGemIndex}).");

            // Click the selected gem
            return await ClickGemChoice(bestGemIndex);
        }

        private async Task UpdateGemPricesIfNeeded()
        {
            if (!World.CurrentArea.IsLabyrinthArea && !World.CurrentArea.IsHideoutArea && !World.CurrentArea.IsTown)
            {
                //Log.Info($"[DivineFontTask] Not in a valid area to update prices. IsLabyrinth: {World.CurrentArea.IsLabyrinthArea}, IsHideout: {World.CurrentArea.IsHideoutArea}, IsTown: {World.CurrentArea.IsTown}");
                return;
            }

            string priceFilePath = Path.Combine(Configuration.Instance.Path, PriceCacheFileName);

            if (File.Exists(priceFilePath) && (DateTime.Now - File.GetLastWriteTime(priceFilePath)).TotalHours < 1)
            {
                if (_gemPrices.Count == 0)
                {
                    GlobalLog.Info("[DivineFontTask] Loading gem prices from cache.");
                    string cachedJson = File.ReadAllText(priceFilePath);
                    _gemPrices = JsonConvert.DeserializeObject<Dictionary<string, double>>(cachedJson);
                }
                return;
            }

            GlobalLog.Info("[DivineFontTask] Price cache is outdated or missing. Fetching new prices from poe.ninja.");

            try
            {
                using (var client = new WebClient())
                {
                    client.Encoding = System.Text.Encoding.UTF8;
                    string url = "https://poe.ninja/poe1/api/economy/stash/current/item/overview?league=Keepers&type=SkillGem";
                    string json = await Coroutine.ExternalTask(client.DownloadStringTaskAsync(new Uri(url)));
                    var response = JsonConvert.DeserializeObject<PoeNinjaResponse>(json);

                    _gemPrices.Clear();
                    foreach (var item in response.Lines)
                    {
                        if (item.TradeFilter?.Query?.Type?.Discriminator == "alt_x" ||
                            item.TradeFilter?.Query?.Type?.Discriminator == "alt_y")
                        {
                            if (item.GemLevel == 1 && item.GemQuality == 0)
                            {
                                // Exclude Trarthus gems
                                if (item.Name.IndexOf("Trarthus", StringComparison.OrdinalIgnoreCase) < 0)
                                {
                                    if (!_gemPrices.ContainsKey(item.Name))
                                    {
                                        _gemPrices.Add(item.Name, item.ChaosValue);
                                    }
                                }
                            }
                        }
                    }

                    string newJsonCache = JsonConvert.SerializeObject(_gemPrices, Formatting.Indented);
                    File.WriteAllText(priceFilePath, newJsonCache);
                    _lastPriceUpdateTime = DateTime.Now;
                    GlobalLog.Info($"[DivineFontTask] Successfully updated and cached prices for {_gemPrices.Count} transfigured gems.");
                }
            }
            catch (Exception ex)
            {
                GlobalLog.Error("[DivineFontTask] Error updating gem prices. " + ex);
            }
        }

        private NetworkObject FindDivineFont()
        {
            return LokiPoe.ObjectManager.Objects
                .FirstOrDefault(o => o.Metadata == "Metadata/Terrain/Labyrinth/Objects/LabyrinthBlessingBench");
        }

        private Element GetElementByPath(params int[] childIndices)
        {
            var allElements = LokiPoe.GetGuiElements();
            var root = allElements.FirstOrDefault(e => e.IdLabel == "root");

            if (root == null || root.Children == null || root.Children.Count < 2)
                return null;

            Element current = root.Children[1];

            foreach (var index in childIndices)
            {
                if (current.Children == null || current.Children.Count <= index)
                    return null;
                current = current.Children[index];
            }

            return current;
        }

        private async Task<bool> ClickElement(Element element)
        {
            if (element == null)
                return false;

            var clickPos = element.CenterClickLocation();
            MouseManager.SetMousePosition(clickPos, useRandomPos: false);
            Thread.Sleep(LokiPoe.Random.Next(25, 55));
            MouseManager.ClickLMB();
            Thread.Sleep(LokiPoe.Random.Next(90, 150));

            return true;
        }



        private async Task<bool> SelectTransformOption(DivineFontOptionType targetType)
        {
            var optionsContainer = GetElementByPath(70, 0, 2, 2);
            if (optionsContainer == null || optionsContainer.Children == null)
            {
                GlobalLog.Error("[DivineFontTask] Options container not found");
                return false;
            }

            foreach (var option in optionsContainer.Children)
            {
                if (option.Children != null && option.Children.Count > 1)
                {
                    var textElement = option.Children[1];
                    string text = textElement.Text;
                    var type = IdentifyOptionType(text);

                    if (type == targetType)
                    {
                        // The clickable part is usually the first child (radio button/checkbox area)
                        var clickable = option.Children[0];
                        await ClickElement(clickable);
                        await Coroutines.LatencyWait();
                        return true;
                    }
                }
            }

            GlobalLog.Error($"[DivineFontTask] Could not find option of type {targetType}");
            return false;
        }

        private async Task<bool> PlaceGemInSlot(Item gem)
        {
            // Use FastMove to place the gem (gem parameter is already the item at position 0,0)
            var result = InventoryUi.InventoryControl_Main.FastMove(gem.LocalId, true, false);

            if (result != FastMoveResult.None)
            {
                GlobalLog.Error($"[DivineFontTask] Failed to place gem using FastMove: {result}");
                return false;
            }

            await Coroutines.LatencyWait();

            return true;
        }

        private async Task<bool> ClickCraftButton()
        {
            // Path: root.Children[1].Children[70].Children[0].Children[3].Children[0]
            var craftButton = GetElementByPath(70, 0, 3, 0);

            if (craftButton == null || !craftButton.IsVisible)
            {
                GlobalLog.Error("[DivineFontTask] Craft button not found");
                return false;
            }

            await ClickElement(craftButton);

            // Wait for gem choices to appear
            await Wait.Sleep(500);
            await Coroutines.LatencyWait();

            return true;
        }

        private string GetGemNameFromTooltip(int gemIndex)
        {
            try
            {
                // Navigate to gems container parent: root[1][70][4][0][0]
                var gemsParent = GetElementByPath(70, 4, 0, 0);
                if (gemsParent == null || gemsParent.Children == null || gemsParent.Children.Count == 0)
                {
                    GlobalLog.Error($"[DivineFontTask] Gems parent not found or has no children");
                    return null;
                }

                // Get the actual container with 3 gem children
                var gemsContainer = gemsParent.Children[0];
                if (gemsContainer == null || gemsContainer.Children == null || gemsContainer.Children.Count <= gemIndex)
                {
                    GlobalLog.Error($"[DivineFontTask] Gem {gemIndex} not found. Available: {gemsContainer?.Children?.Count ?? 0}");
                    return null;
                }

                // Get the specific gem element
                var gemElement = gemsContainer.Children[gemIndex];
                if (gemElement == null)
                {
                    GlobalLog.Error($"[DivineFontTask] Gem element {gemIndex} is null");
                    return null;
                }

                // Hover over the gem to populate tooltip
                var gemPos = gemElement.CenterClickLocation();
                MouseManager.SetMousePosition(gemPos, useRandomPos: false);
                Thread.Sleep(250); // Wait for tooltip to populate

                // Tooltip is directly on the gem element
                var tooltip = gemElement.Tooltip;
                if (tooltip == null)
                {
                    GlobalLog.Error($"[DivineFontTask] Tooltip not found for gem {gemIndex} after hover");
                    return null;
                }

                // Navigate tooltip structure: Tooltip.Children[0].Children[0].Text
                if (tooltip.Children == null || tooltip.Children.Count == 0)
                {
                    GlobalLog.Error($"[DivineFontTask] Tooltip has no children for gem {gemIndex}");
                    return null;
                }

                var tooltipChild = tooltip.Children[0];
                if (tooltipChild == null || tooltipChild.Children == null || tooltipChild.Children.Count == 0)
                {
                    GlobalLog.Error($"[DivineFontTask] Tooltip child has no children for gem {gemIndex}");
                    return null;
                }

                var textElement = tooltipChild.Children[0];
                return textElement?.Text;
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[DivineFontTask] Exception reading gem {gemIndex}: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> ClickGemChoice(int gemIndex)
        {
            try
            {
                // Navigate to gems container: root[1][70][4][0][0]
                var gemsParent = GetElementByPath(70, 4, 0, 0);
                if (gemsParent == null || gemsParent.Children == null || gemsParent.Children.Count == 0)
                {
                    GlobalLog.Error("[DivineFontTask] Gems parent not found for clicking");
                    return false;
                }

                var gemsContainer = gemsParent.Children[0];
                if (gemsContainer == null || gemsContainer.Children == null || gemsContainer.Children.Count <= gemIndex)
                {
                    GlobalLog.Error($"[DivineFontTask] Gem {gemIndex} not found for clicking");
                    return false;
                }

                var gemElement = gemsContainer.Children[gemIndex];
                if (gemElement == null)
                {
                    GlobalLog.Error($"[DivineFontTask] Gem element {gemIndex} is null");
                    return false;
                }

                GlobalLog.Info($"[DivineFontTask] Clicking gem at index {gemIndex}");
                await ClickElement(gemElement);
                await Coroutines.LatencyWait();

                return true;
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[DivineFontTask] Exception clicking gem {gemIndex}: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ClickConfirmButton()
        {
            // Path: root.Children[1].Children[70].Children[4].Children[0].Children[1].Children[0]
            var confirmButton = GetElementByPath(70, 4, 0, 1, 0);

            if (confirmButton == null || !confirmButton.IsVisible)
            {
                GlobalLog.Error("[DivineFontTask] Confirm button not found");
                return false;
            }

            // Verify button text is "confirm"
            if (confirmButton.Text == "confirm")
            {
                GlobalLog.Info("[DivineFontTask] Clicking confirm button");
                await ClickElement(confirmButton);

                await Coroutines.LatencyWait();
                return true;
            }

            GlobalLog.Error($"[DivineFontTask] Unexpected confirm button text: '{confirmButton.Text}'");
            return false;
        }

        private Element GetGemInputSlot()
        {
            // Based on findgeminput.cs dump:
            // Gem container is at [70][0][3][3]
            // Within that container, child[1] has the tooltip (the gem)
            var container = GetElementByPath(70, 0, 3, 3);

            if (container == null)
            {
                GlobalLog.Warn("[DivineFontTask] GetGemInputSlot: Container not found");
                return null;
            }

            if (container.Children == null || container.Children.Count < 2)
            {
                return null;
            }

            // The gem is at child[1] of the container
            return container.Children[1];
        }

        private bool IsGemInSlot()
        {
            // If there's a gem in the slot, the container at [70][0][3][3] will have 2 children
            // If empty, it only has 1 child ([0])
            var container = GetElementByPath(70, 0, 3, 3);

            if (container == null)
            {
                GlobalLog.Warn("[DivineFontTask] IsGemInSlot: Container not found");
                return false;
            }

            int childCount = container.Children?.Count ?? 0;
            bool hasGem = childCount >= 2;

            GlobalLog.Info($"[DivineFontTask] IsGemInSlot: {hasGem} (container has {childCount} children)");
            return hasGem;
        }

        private string GetGemNameFromInputSlot()
        {
            try
            {
                var gemSlot = GetGemInputSlot();
                if (gemSlot == null || gemSlot.Tooltip == null)
                {
                    GlobalLog.Warn("[DivineFontTask] GetGemNameFromInputSlot: No gem in slot");
                    return null;
                }

                // Navigate tooltip structure: Tooltip -> [0] -> [0] -> [0] -> Text
                var tooltip = gemSlot.Tooltip;

                if (tooltip.Children == null || tooltip.Children.Count == 0)
                {
                    GlobalLog.Error("[DivineFontTask] GetGemNameFromInputSlot: Tooltip has no children");
                    return null;
                }

                var tooltipChild1 = tooltip.Children[0];
                if (tooltipChild1 == null || tooltipChild1.Children == null || tooltipChild1.Children.Count == 0)
                {
                    GlobalLog.Error("[DivineFontTask] GetGemNameFromInputSlot: Tooltip[0] has no children");
                    return null;
                }

                var tooltipChild2 = tooltipChild1.Children[0];
                if (tooltipChild2 == null || tooltipChild2.Children == null || tooltipChild2.Children.Count == 0)
                {
                    GlobalLog.Error("[DivineFontTask] GetGemNameFromInputSlot: Tooltip[0][0] has no children");
                    return null;
                }

                var textElement = tooltipChild2.Children[0];
                string gemName = textElement?.Text;

                //Log.Info($"[DivineFontTask] GetGemNameFromInputSlot: Found gem '{gemName}'");
                return gemName;
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[DivineFontTask] Exception reading gem name from input slot: {ex.Message}");
                return null;
            }
        }

        private async Task<bool> RemoveGemFromSlot()
        {
            try
            {

                var gemSlot = GetGemInputSlot();
                if (gemSlot == null)
                {
                    GlobalLog.Error("[DivineFontTask] Gem input slot not found");
                    return false;
                }

                if (!IsGemInSlot())
                {
                    GlobalLog.Info("[DivineFontTask] No gem in slot to remove");
                    return true;
                }

                GlobalLog.Info("[DivineFontTask] Hovering over gem slot to remove gem");

                // Hover over the gem slot
                var slotPos = gemSlot.CenterClickLocation();
                MouseManager.SetMousePosition(slotPos, useRandomPos: false);
                Thread.Sleep(LokiPoe.Random.Next(25, 55));

                GlobalLog.Info("[DivineFontTask] Performing Ctrl + Left Click to remove gem");

                // Ctrl + Left Click to remove the gem
                LokiPoe.ProcessHookManager.SetKeyState(Keys.ControlKey, -32768);
                Thread.Sleep(LokiPoe.Random.Next(25, 55));
                MouseManager.ClickLMB();
                Thread.Sleep(LokiPoe.Random.Next(90, 150));
                LokiPoe.ProcessHookManager.SetKeyState(Keys.ControlKey, 0);


                await Coroutines.LatencyWait();

                GlobalLog.Info("[DivineFontTask] Gem removed from slot");
                return true;
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[DivineFontTask] Exception removing gem from slot: {ex.Message}");
                return false;
            }
        }

        private Item FindGemToEnchant()
        {
            var mainInventoryItems = InventoryUi.InventoryControl_Main.Inventory.Items;
            if (mainInventoryItems == null)
            {
                GlobalLog.Error("[DivineFontTask] Main inventory is not available.");
                return null;
            }

            // Check settings for which strategy to use
            if (FollowBotSettings.Instance.Lab.UseFirstInventorySlot)
            {
                // Original behavior: use gem at position (0, 0)
                return mainInventoryItems.FirstOrDefault(i =>
                    i.LocationTopLeft.X == 0 &&
                    i.LocationTopLeft.Y == 0);
            }
            else if (FollowBotSettings.Instance.Lab.SearchForSuitableGem)
            {
                // Intelligent selection with safety checks
                return FindSuitableGemForTransformation(mainInventoryItems);
            }

            return null;
        }

        private Item FindSuitableGemForTransformation(List<Item> inventoryItems)
        {
            var desiredColor = FollowBotSettings.Instance.Lab.Color;

            // If Smart is selected, try colors in priority order
            if (desiredColor == LabSettings.GemColor.Smart)
            {
                var colorPriority = GetColorPriorityList();
                foreach (var color in colorPriority)
                {
                    var gem = FindGemByColor(inventoryItems, color);
                    if (gem != null)
                    {
                        GlobalLog.Info($"[DivineFontTask] Smart choice found {color} gem in inventory: {gem.Name}");
                        return gem;
                    }
                }
                return null;
            }

            // For specific color selection or Any, search directly
            return FindGemByColor(inventoryItems, desiredColor);
        }

        private Item FindGemByColor(List<Item> inventoryItems, LabSettings.GemColor desiredColor)
        {
            foreach (var item in inventoryItems)
            {
                // Must be an active skill gem
                if (item.Class != "Active Skill Gem")
                    continue;

                // Must not be corrupted
                if (item.IsCorrupted)
                    continue;

                // Color Check (skip if Any)
                if (desiredColor != LabSettings.GemColor.Any)
                {
                    var skillGemComponent = item.Components?.SkillGemComponent;
                    if (skillGemComponent == null) continue;

                    bool colorMatch = false;
                    switch (desiredColor)
                    {
                        case LabSettings.GemColor.Red:
                            colorMatch = skillGemComponent.SocketColor.ToString() == "Red";
                            break;
                        case LabSettings.GemColor.Green:
                            colorMatch = skillGemComponent.SocketColor.ToString() == "Green";
                            break;
                        case LabSettings.GemColor.Blue:
                            colorMatch = skillGemComponent.SocketColor.ToString() == "Blue";
                            break;
                    }
                    if (!colorMatch) continue;
                }

                // Check if it's a standard gem (Superior quality)
                if (item.SkillGemQualityType == DreamPoeBot.Loki.Game.GameData.GemQualityType.Superior)
                {
                    // This is a standard gem - safe to use
                    GlobalLog.Info($"[DivineFontTask] Found standard gem '{item.Name}' for transformation.");
                    return item;
                }
                else
                {
                    // This is a special quality gem (Anomalous, Divergent, Phantasmal)
                    // Check if any of its potential transfigured versions are high value
                    var potentialMatches = _gemPrices
                        .Where(kvp => kvp.Key.Contains(item.Name))
                        .ToList();

                    if (potentialMatches.Any())
                    {
                        double maxPotentialValue = potentialMatches.Max(kvp => kvp.Value);

                        if (maxPotentialValue <= FollowBotSettings.Instance.Lab.GemValueSafetyThreshold)
                        {
                            // All potential versions are low value - safe to reuse
                            GlobalLog.Info($"[DivineFontTask] Found low-value special gem '{item.Name}' (max potential: {maxPotentialValue}c).");
                            return item;
                        }
                        else
                        {
                            // Could be valuable - skip it
                            GlobalLog.Info($"[DivineFontTask] Skipping '{item.Name}' - could be worth {maxPotentialValue}c.");
                        }
                    }
                    else
                    {
                        // Unknown special gem - be conservative and skip it
                        GlobalLog.Debug($"[DivineFontTask] Skipping unknown special gem '{item.Name}'.");
                    }
                }
            }

            return null;
        }

        private async Task<bool> TryGetGemFromStash()
        {
            // Find the stash
            var stash = LokiPoe.ObjectManager.Stash;
            if (stash == null)
            {
                GlobalLog.Debug("[DivineFontTask] No stash found near Divine Font");
                return false;
            }
            // Move to stash if too far away
            if (LokiPoe.Me.Position.Distance(stash.Position) > 20)
            {
                GlobalLog.Info("[DivineFontTask] Moving closer to stash");
                await Move.AtOnce(stash.Position, "Stash", 15);
            }

            GlobalLog.Info("[DivineFontTask] Opening stash to search for gems");

            // Interact with stash using PlayerAction
            var interactResult = await PlayerAction.Interact(stash);
            if (!interactResult)
            {
                GlobalLog.Error("[DivineFontTask] Failed to interact with stash");
                return false;
            }

            await Wait.Sleep(800); // Wait for stash UI to populate

            if (!LokiPoe.InGameState.StashUi.IsOpened)
            {
                GlobalLog.Error("[DivineFontTask] Stash UI did not open");
                return false;
            }

            bool gemWithdrawn = false;
            var desiredColor = FollowBotSettings.Instance.Lab.Color;

            // Switch to configured tab
            if (!SwitchToConfiguredStashTab())
            {
                GlobalLog.Error("[DivineFontTask] Failed to switch to configured stash tab");
                await Coroutines.CloseBlockingWindows();
                return false;
            }

            if (desiredColor == LabSettings.GemColor.Smart)
            {
                // Smart mode: try colors in priority order
                var colorPriority = GetColorPriorityList();
                foreach (var color in colorPriority)
                {
                    GlobalLog.Info($"[DivineFontTask] Checking stash for {color} gems");
                    if (TryWithdrawGemByColor(color))
                    {
                        gemWithdrawn = true;
                        GlobalLog.Info($"[DivineFontTask] Successfully withdrew {color} gem from stash");
                        break;
                    }
                }
            }
            else
            {
                // Specific color or Any mode
                if (TryWithdrawGemByColor(desiredColor))
                {
                    gemWithdrawn = true;
                    GlobalLog.Info($"[DivineFontTask] Successfully withdrew gem from stash");
                }
            }

            // Close stash
            await Coroutines.CloseBlockingWindows();
            await Wait.Sleep(200);

            return gemWithdrawn;
        }

        private bool TryWithdrawGemByColor(LabSettings.GemColor color)
        {
            var socketColor = ColorToSocketColor(color);

            // Use predicate-based StashHelper to find and withdraw gem
            return StashHelper.WithdrawItem(i =>
                i.SkillGemLevel == 1 &&
                i.Quality == 0 &&
                (socketColor == DreamPoeBot.Loki.Game.GameData.SocketColor.None || i.SocketColor == socketColor) &&
                i.SkillGemQualityType == DreamPoeBot.Loki.Game.GameData.GemQualityType.Superior &&
                !i.IsCorrupted
            );
        }

        private DreamPoeBot.Loki.Game.GameData.SocketColor ColorToSocketColor(LabSettings.GemColor color)
        {
            switch (color)
            {
                case LabSettings.GemColor.Red:
                    return DreamPoeBot.Loki.Game.GameData.SocketColor.Red;
                case LabSettings.GemColor.Green:
                    return DreamPoeBot.Loki.Game.GameData.SocketColor.Green;
                case LabSettings.GemColor.Blue:
                    return DreamPoeBot.Loki.Game.GameData.SocketColor.Blue;
                default:
                    return DreamPoeBot.Loki.Game.GameData.SocketColor.None;
            }
        }

        private List<LabSettings.GemColor> GetColorPriorityList()
        {
            if (_gemPrices.Count == 0)
            {
                GlobalLog.Warn("[DivineFontTask] No gem price data available for smart choice. Using default priority: Red, Green, Blue");
                return new List<LabSettings.GemColor> { LabSettings.GemColor.Red, LabSettings.GemColor.Green, LabSettings.GemColor.Blue };
            }

            // Categorize gems by color
            var redGems = new List<string>();
            var greenGems = new List<string>();
            var blueGems = new List<string>();

            foreach (var gemName in _gemPrices.Keys)
            {
                if (IsRedGem(gemName))
                    redGems.Add(gemName);
                else if (IsGreenGem(gemName))
                    greenGems.Add(gemName);
                else if (IsBlueGem(gemName))
                    blueGems.Add(gemName);
            }

            // Calculate average prices
            var redAvg = redGems.Count > 0 ? redGems.Average(g => _gemPrices[g]) : 0;
            var greenAvg = greenGems.Count > 0 ? greenGems.Average(g => _gemPrices[g]) : 0;
            var blueAvg = blueGems.Count > 0 ? blueGems.Average(g => _gemPrices[g]) : 0;

            // Create priority list sorted by average price (highest to lowest)
            var colorValues = new List<(LabSettings.GemColor color, double avg)>
            {
                (LabSettings.GemColor.Red, redAvg),
                (LabSettings.GemColor.Green, greenAvg),
                (LabSettings.GemColor.Blue, blueAvg)
            };

            var sortedColors = colorValues
                .OrderByDescending(cv => cv.avg)
                .Select(cv => cv.color)
                .ToList();

            GlobalLog.Info($"[DivineFontTask] Color priority (by avg price): {string.Join(", ", sortedColors.Select(c => $"{c}({colorValues.First(cv => cv.color == c).avg:F2}c)"))}");

            return sortedColors;
        }

        public static string CalculateSmartGemChoice()
        {
            if (_gemPrices.Count == 0)
            {
                return "No gem price data available. Make sure the bot is started, has a leader, and you are in the labyrinth, hideout, or town.";
            }

            // Get gem colors from gemcolors.md data
            var redGems = new List<string>();
            var greenGems = new List<string>();
            var blueGems = new List<string>();
            var uncategorizedGems = new List<string>();

            // Categorize gems by color based on gemcolors.md
            foreach (var gemName in _gemPrices.Keys)
            {
                if (IsRedGem(gemName))
                    redGems.Add(gemName);
                else if (IsGreenGem(gemName))
                    greenGems.Add(gemName);
                else if (IsBlueGem(gemName))
                    blueGems.Add(gemName);
                else
                    uncategorizedGems.Add(gemName);
            }

            if (uncategorizedGems.Any())
            {
                GlobalLog.Warn("[DivineFontTask] The following gems were not categorized: " + string.Join(", ", uncategorizedGems));
            }

            // Calculate average prices
            var redAvg = redGems.Count > 0 ? redGems.Average(g => _gemPrices[g]) : 0;
            var greenAvg = greenGems.Count > 0 ? greenGems.Average(g => _gemPrices[g]) : 0;
            var blueAvg = blueGems.Count > 0 ? blueGems.Average(g => _gemPrices[g]) : 0;

            var result = $"Smart Gem Choice Analysis:\n";
            result += $"================================\n";
            result += $"Red Gems: {redGems.Count} gems, Average: {redAvg:F2}c\n";
            result += $"Green Gems: {greenGems.Count} gems, Average: {greenAvg:F2}c\n";
            result += $"Blue Gems: {blueGems.Count} gems, Average: {blueAvg:F2}c\n";
            result += $"================================\n";

            // Determine best choice
            string bestColor = "Red";
            double bestAvg = redAvg;

            if (greenAvg > bestAvg)
            {
                bestColor = "Green";
                bestAvg = greenAvg;
            }
            if (blueAvg > bestAvg)
            {
                bestColor = "Blue";
                bestAvg = blueAvg;
            }

            result += $"Best Choice: {bestColor} (Average: {bestAvg:F2}c)\n";
            result += $"\nTop 5 {bestColor} gems by value:\n";

            var bestColorGems = bestColor == "Red" ? redGems : (bestColor == "Green" ? greenGems : blueGems);
            var topGems = bestColorGems.OrderByDescending(g => _gemPrices[g]).Take(5).ToList();

            foreach (var gem in topGems)
            {
                result += $"  - {gem}: {_gemPrices[gem]:F2}c\n";
            }

            return result;
        }

        private static bool IsRedGem(string gemName)
        {
            var redGems = new[] {
                "Absolution of Inspiring", "Animate Guardian of Smiting", "Bladestorm of Uncertainty",
                "Boneshatter of Carnage", "Boneshatter of Complex Trauma", "Cleave of Rage",
                "Consecrated Path of Endurance", "Dominating Blow of Inspiring", "Earthquake of Amplification",
                "Earthshatter of Fragility", "Earthshatter of Prominence", "Exsanguinate of Transmission",
                "Frozen Legion of Rallying", "Glacial Hammer of Shattering", "Ground Slam of Earthshaking",
                "Holy Flame Totem of Ire", "Ice Crash of Cadence", "Infernal Blow of Immolation",
                "Leap Slam of Groundbreaking", "Molten Strike of the Zenith", "Perforate of Bloodshed",
                "Perforate of Duality", "Rage Vortex of Berserking", "Shield Crush of the Chieftain", "Shockwave Totem of Authority",
                "Smite of Divine Judgement", "Static Strike of Gathering Lightning", "Summon Flame Golem of Hordes", "Summon Flame Golem of the Meteor",
                "Summon Stone Golem of Hordes", "Summon Stone Golem of Safeguarding", "Sunder of Earthbreaking",
                "Tectonic Slam of Cataclysm", "Volcanic Fissure of Snaking"
            };
            return redGems.Any(g => g.Equals(gemName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsGreenGem(string gemName)
        {
            var greenGems = new[] {
                "Animate Weapon of Ranged Arms", "Animate Weapon of Self Reflection", "Artillery Ballista of Cross Strafe",
                "Artillery Ballista of Focus Fire", "Barrage of Volley Fire", "Bear Trap of Skewers",
                "Blade Blast of Dagger Detonation", "Blade Blast of Unloading", "Blade Flurry of Incision",
                "Blade Trap of Greatswords", "Blade Trap of Laceration", "Blade Vortex of the Scythe",
                "Bladefall of Impaling", "Bladefall of Volleys", "Blink Arrow of Bombarding Clones",
                "Blink Arrow of Prismatic Clones", "Burning Arrow of Vigour", "Caustic Arrow of Poison",
                "Charged Dash of Projection", "Cremation of Exhuming", "Cremation of the Volcano", "Cyclone of Tumult",
                "Detonate Dead of Chain Reaction", "Detonate Dead of Scavenging", "Double Strike of Impaling",
                "Double Strike of Momentum", "Dual Strike of Ambidexterity", "Elemental Hit of the Spectrum",
                "Ethereal Knives of Lingering Blades", "Ethereal Knives of the Massacre", "Explosive Concoction of Destruction",
                "Explosive Trap of Magnitude", "Explosive Trap of Shrapnel", "Fire Trap of Blasting",
                "Flamethrower Trap of Stability", "Flicker Strike of Power", "Frenzy of Onslaught", "Frost Blades of Katabasis",
                "Galvanic Arrow of Energy", "Galvanic Arrow of Surging", "Ice Shot of Penetration",
                "Ice Trap of Hollowness", "Lacerate of Butchering", "Lacerate of Haemorrhage",
                "Lancing Steel of Spraying", "Lightning Arrow of Electrocution", "Lightning Strike of Arcing",
                "Mirror Arrow of Bombarding Clones", "Mirror Arrow of Prismatic Clones", "Poisonous Concoction of Bouncing",
                "Puncture of Shanking", "Rain of Arrows of Artillery", "Rain of Arrows of Saturation",
                "Reave of Refraction", "Scourge Arrow of Menace", "Seismic Trap of Swells",
                "Shattering Steel of Ammunition", "Shrapnel Ballista of Steel", "Siege Ballista of Splintering",
                "Spectral Shield Throw of Shattering", "Spectral Throw of Materialising", "Split Arrow of Splitting",
                "Splitting Steel of Ammunition", "Storm Rain of the Conduit", "Storm Rain of the Fence",
                "Summon Ice Golem of Hordes", "Summon Ice Golem of Shattering", "Tornado of Elemental Turbulence",
                "Tornado Shot of Cloudburst", "Toxic Rain of Sporeburst", "Toxic Rain of Withering",
                "Viper Strike of the Mamba", "Volatile Dead of Confinement", "Volatile Dead of Seething",
                "Wild Strike of Extremes"
            };
            return greenGems.Any(g => g.Equals(gemName, StringComparison.OrdinalIgnoreCase));
        }

        private static bool IsBlueGem(string gemName)
        {
            var blueGems = new[] {
                "Arc of Oscillating", "Arc of Surging", "Armageddon Brand of Recall",
                "Armageddon Brand of Volatility", "Ball Lightning of Orbiting", "Ball Lightning of Static",
                "Bane of Condemnation", "Blight of Atrophy", "Blight of Contagion",
                "Bodyswap of Sacrifice", "Cold Snap of Power", "Contagion of Subsiding",
                "Contagion of Transference", "Crackling Lance of Branching", "Crackling Lance of Disintegration",
                "Discharge of Misery", "Divine Ire of Disintegration", "Divine Ire of Holy Lightning",
                "Essence Drain of Desperation", "Essence Drain of Wickedness", "Eye of Winter of Finality",
                "Eye of Winter of Transience", "Firestorm of Meteors", "Firestorm of Pelting",
                "Flame Dash of Return", "Flame Surge of Combusting", "Flameblast of Celerity",
                "Flameblast of Contraction", "Forbidden Rite of Soul Sacrifice", "Frost Bomb of Forthcoming",
                "Frost Bomb of Instability", "Frostblink of Wintry Blast", "Galvanic Field of Intensity",
                "Glacial Cascade of the Fissure", "Hexblast of Contradiction", "Hexblast of Havoc",
                "Ice Nova of Deep Freeze", "Ice Nova of Frostbolts", "Ice Spear of Splitting",
                "Icicle Mine of Fanning", "Icicle Mine of Sabotage", "Incinerate of Expanse",
                "Incinerate of Venting", "Kinetic Blast of Clustering", "Kinetic Bolt of Fragmentation",
                "Kinetic Fusillade of Detonation", "Kinetic Rain of Impact", "Lightning Conduit of the Heavens", "Lightning Spire Trap of Overloading",
                "Lightning Spire Trap of Zapping", "Lightning Tendrils of Eccentricity", "Lightning Tendrils of Escalation",
                "Lightning Trap of Sparking", "Orb of Storms of Squalls", "Penance Brand of Conduction", "Penance Brand of Dissipation",
                "Power Siphon of the Archmage", "Purifying Flame of Revelations", "Pyroclast Mine of Sabotage",
                "Raise Spectre of Transience", "Raise Zombie of Falling", "Raise Zombie of Slamming",
                "Righteous Fire of Arcane Devotion", "Scorching Ray of Immolation", "Shock Nova of Procession",
                "Siphoning Trap of Pain", "Soulrend of Reaping",
                "Soulrend of the Spiral", "Spark of the Nova", "Spark of Unpredictability",
                "Storm Brand of Indecision", "Storm Burst of Repulsion", "Stormbind of Teleportation", "Summon Carrion Golem of Hordes",
                "Summon Carrion Golem of Scavenging", "Summon Chaos Golem of Hordes", "Summon Chaos Golem of the Maelström",
                "Summon Holy Relic of Conviction", "Summon Lightning Golem of Hordes", "Summon Raging Spirit of Enormity",
                "Summon Reaper of Eviscerating", "Summon Reaper of Revenants", "Summon Skeletons of Archers",
                "Summon Skeletons of Mages", "Void Sphere of Rending", "Vortex of Projection"
            };
            return blueGems.Any(g => g.Equals(gemName, StringComparison.OrdinalIgnoreCase));
        }

        private static LabSettings.GemColor DetermineSmartGemColor()
        {
            if (_gemPrices.Count == 0)
            {
                GlobalLog.Warn("[DivineFontTask] No gem price data available for smart choice. Defaulting to Red.");
                return LabSettings.GemColor.Red;
            }

            // Categorize gems by color
            var redGems = new List<string>();
            var greenGems = new List<string>();
            var blueGems = new List<string>();

            foreach (var gemName in _gemPrices.Keys)
            {
                if (IsRedGem(gemName))
                    redGems.Add(gemName);
                else if (IsGreenGem(gemName))
                    greenGems.Add(gemName);
                else if (IsBlueGem(gemName))
                    blueGems.Add(gemName);
            }

            // Calculate average prices
            var redAvg = redGems.Count > 0 ? redGems.Average(g => _gemPrices[g]) : 0;
            var greenAvg = greenGems.Count > 0 ? greenGems.Average(g => _gemPrices[g]) : 0;
            var blueAvg = blueGems.Count > 0 ? blueGems.Average(g => _gemPrices[g]) : 0;

            GlobalLog.Info($"[DivineFontTask] Smart choice averages - Red: {redAvg:F2}c, Green: {greenAvg:F2}c, Blue: {blueAvg:F2}c");

            // Determine best choice
            if (redAvg >= greenAvg && redAvg >= blueAvg)
            {
                return LabSettings.GemColor.Red;
            }
            else if (greenAvg >= redAvg && greenAvg >= blueAvg)
            {
                return LabSettings.GemColor.Green;
            }
            else
            {
                return LabSettings.GemColor.Blue;
            }
        }

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(DreamPoeBot.Loki.Bot.Message message)
        {
            if (message.Id == Events.Messages.AreaChanged)
            {
                _hasExecuted = false;
                _failedOptions.Clear();
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        private int GetRemainingCrafts()
        {
            var craftsElement = GetElementByPath(70, 0, 3, 1, 0);
            if (craftsElement == null)
            {
                GlobalLog.Error("[DivineFontTask] Could not find crafts remaining element.");
                return 0;
            }

            string text = craftsElement.Text;
            if (string.IsNullOrEmpty(text))
            {
                GlobalLog.Error("[DivineFontTask] Crafts remaining text is empty.");
                return 0;
            }

            var parts = text.Split(' ');
            if (parts.Length < 3 || !int.TryParse(parts[2], out int count))
            {
                GlobalLog.Error($"[DivineFontTask] Could not parse crafts remaining from text: {text}");
                return 0;
            }

            GlobalLog.Info($"[DivineFontTask] Crafts Remaining: {count}");
            return count;
        }

        private async Task ReadLabOptions()
        {
            GlobalLog.Info("[DivineFontTask] Reading Lab options...");

            int remainingCrafts = GetRemainingCrafts();
            if (remainingCrafts == 0)
            {
                GlobalLog.Info("[DivineFontTask] No crafts remaining.");
                return;
            }

            var optionsContainer = GetElementByPath(70, 0, 2, 2);
            if (optionsContainer == null)
            {
                GlobalLog.Error("[DivineFontTask] Could not find options container.");
                return;
            }

            if (optionsContainer.Children == null || optionsContainer.Children.Count == 0)
            {
                GlobalLog.Error("[DivineFontTask] Options container has no children.");
                return;
            }

            GlobalLog.Info($"[DivineFontTask] Found {optionsContainer.Children.Count} options.");

            for (int i = 0; i < optionsContainer.Children.Count; i++)
            {
                var option = optionsContainer.Children[i];
                if (option.Children != null && option.Children.Count > 1)
                {
                    var textElement = option.Children[1];
                    string text = textElement.Text;
                    GlobalLog.Info($"[DivineFontTask] Option {i + 1}: {text}");
                }
                else
                {
                    GlobalLog.Warn($"[DivineFontTask] Could not read text for option {i + 1}.");
                }
            }
        }

        private bool SwitchToConfiguredStashTab()
        {
            var settings = FollowBotSettings.Instance.Lab;
            if (settings.TabMode == LabSettings.StashTabMode.Index)
            {
                return StashHelper.SwitchToTab(settings.StashTabIndex);
            }
            else
            {
                if (string.IsNullOrEmpty(settings.StashTabName))
                {
                    GlobalLog.Error("[DivineFontTask] Stash Tab Name is empty but mode is set to Name.");
                    return false;
                }
                return StashHelper.SwitchToTab(settings.StashTabName);
            }
        }
    }
}