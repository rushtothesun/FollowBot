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
        private string _lastCraftedGemName = null;

        public void Start() { }
        public void Stop() { }
        public void Tick() { }

        public async Task<bool> Run()
        {
            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead || !World.CurrentArea.IsLabyrinthArea || !FollowBotSettings.Instance.Lab.EnableDivineFontHandling)
                return false;

            if (_hasExecuted)
                return false;

            await UpdateGemPricesIfNeeded();

            if (!LokiPoe.InGameState.DivineFontUi.IsOpened)
            {
                if (!await OpenDivineFont())
                    return false;
            }

            return await ExecutePriorityLogic();
        }

        private async Task<bool> OpenDivineFont()
        {
            var divineFont = FindDivineFont();
            if (divineFont == null || divineFont.Distance > 55)
                return false;

            GlobalLog.Info("[DivineFontTask] Interacting with Divine Font");
            var interactResult = await PlayerAction.Interact(divineFont);
            if (!interactResult)
                return false;

            return await Wait.For(() => LokiPoe.InGameState.DivineFontUi.IsOpened, "Divine Font UI opening", 200, 2000);
        }

        private async Task<bool> ExecutePriorityLogic()
        {
            while (true)
            {
                await Wait.Sleep(LokiPoe.Random.Next(200, 500));
                // 1. Read what options the Divine Font is offering
                var availableTypes = ReadAvailableOptionsFromUi();

                // 2. Pick highest-priority enabled option that's available on the UI
                var userOptions = FollowBotSettings.Instance.Lab.DivineFontOptions
                    .Where(o => o.IsEnabled)
                    .OrderBy(o => o.Priority)
                    .ToList();

                var chosenOption = userOptions.FirstOrDefault(o => availableTypes.Contains(o.Type));
                if (chosenOption == null)
                {
                    GlobalLog.Info("[DivineFontTask] No enabled options match what the Divine Font is offering.");
                    BotManager.Stop(false);
                    return false;
                }

                GlobalLog.Info($"[DivineFontTask] Chosen option: {chosenOption.Name} (Priority {chosenOption.Priority})");
                await Wait.Sleep(LokiPoe.Random.Next(200, 500));

                // 3. Execute the chosen option
                _lastCraftedGemName = null;
                bool success;
                switch (chosenOption.Type)
                {
                    case DivineFontOptionType.TransformSpecificGem:
                        success = await ExecuteTransformSpecific(chosenOption);
                        break;
                    case DivineFontOptionType.ExchangeForExceptional:
                        success = await ExecuteExchangeForExceptional();
                        break;
                    case DivineFontOptionType.TransformRandomSameColor:
                        success = await ExecuteTransformRandom();
                        break;
                    default:
                        GlobalLog.Error($"[DivineFontTask] Unknown option type: {chosenOption.Type}");
                        BotManager.Stop(false);
                        return false;
                }

                if (!success)
                {
                    BotManager.Stop(false);
                    return false;
                }

                // 4. Drop valuable gems on the ground so they don't get re-used
                if (_lastCraftedGemName != null)
                {
                    double price = _gemPrices.ContainsKey(_lastCraftedGemName) ? _gemPrices[_lastCraftedGemName] : 0;
                    if (price > FollowBotSettings.Instance.Lab.GemValueSafetyThreshold)
                    {
                        GlobalLog.Info($"[DivineFontTask] '{_lastCraftedGemName}' ({price:F0}c) exceeds safety threshold ({FollowBotSettings.Instance.Lab.GemValueSafetyThreshold}c). Dropping on ground.");
                        if (!await DropCraftedGem(_lastCraftedGemName))
                        {
                            BotManager.Stop(false);
                            return false;
                        }
                    }
                    else
                    {
                        GlobalLog.Info($"[DivineFontTask] '{_lastCraftedGemName}' ({price:F0}c) below safety threshold ({FollowBotSettings.Instance.Lab.GemValueSafetyThreshold}c). Keeping in inventory.");
                    }
                }

                // 5. Reopen Divine Font if it was closed (e.g. after dropping gem)
                if (!LokiPoe.InGameState.DivineFontUi.IsOpened)
                {
                    if (!await OpenDivineFont())
                    {
                        GlobalLog.Error("[DivineFontTask] Failed to reopen Divine Font after drop.");
                        BotManager.Stop(false);
                        return false;
                    }
                }

                // 6. Check if crafts remain
                int remaining = GetRemainingCrafts();
                if (remaining <= 0)
                {
                    GlobalLog.Info("[DivineFontTask] No crafts remaining. Task complete.");
                    _hasExecuted = true;
                    return true;
                }

                GlobalLog.Info($"[DivineFontTask] {remaining} craft(s) remaining. Re-evaluating options.");
                // Loop back to re-read available options
            }
        }

        #region Execution Methods

        private async Task<bool> ExecuteTransformSpecific(DivineFontOption option)
        {
            var gem = FindSpecificGemInInventory(option.GemName);
            if (gem == null)
            {
                GlobalLog.Info($"[DivineFontTask] Specific gem '{option.GemName}' not in inventory. Checking stash...");
                gem = await WithdrawFromStash(option);
            }
            if (gem == null)
            {
                GlobalLog.Error($"[DivineFontTask] Cannot find '{option.GemName}' in inventory or stash. Stopping for leader intervention.");
                return false;
            }

            return await PlaceCraftAndPickResult(gem, DivineFontOptionType.TransformSpecificGem);
        }

        private async Task<bool> ExecuteExchangeForExceptional()
        {
            var gem = FindSupportGemInInventory();
            if (gem == null)
            {
                GlobalLog.Info("[DivineFontTask] No support gem in inventory. Checking stash...");
                gem = await WithdrawFromStash(DivineFontOptionType.ExchangeForExceptional);
            }
            if (gem == null)
            {
                GlobalLog.Error("[DivineFontTask] Cannot find support gem in inventory or stash. Stopping for leader intervention.");
                return false;
            }

            return await PlaceCraftAndPickResult(gem, DivineFontOptionType.ExchangeForExceptional);
        }

        private async Task<bool> ExecuteTransformRandom()
        {
            var gem = FindGemToEnchant();
            if (gem == null)
            {
                GlobalLog.Info("[DivineFontTask] No suitable gem in inventory. Checking stash...");
                gem = await WithdrawFromStash(DivineFontOptionType.TransformRandomSameColor);
            }
            if (gem == null)
            {
                GlobalLog.Error("[DivineFontTask] Cannot find suitable gem in inventory or stash. Stopping for leader intervention.");
                return false;
            }

            return await PlaceCraftAndPickResult(gem, DivineFontOptionType.TransformRandomSameColor);
        }

        #endregion

        #region Shared Craft Execution

        private async Task<bool> PlaceCraftAndPickResult(Item gem, DivineFontOptionType optionType)
        {
            GlobalLog.Info($"[DivineFontTask] Starting Divine Font sequence with gem: {gem.Name}");

            if (!LokiPoe.InGameState.DivineFontUi.IsOpened)
            {
                GlobalLog.Error("[DivineFontTask] Divine Font UI not open");
                return false;
            }

            // Place gem
            GlobalLog.Info($"[DivineFontTask] Placing gem: {gem.Name}");
            if (!await PlaceGemInSlot(gem)) return false;
            await Wait.Sleep(300);

            // Select option
            GlobalLog.Info($"[DivineFontTask] Selecting option: {optionType}");
            if (!await SelectTransformOption(optionType)) return false;
            await Wait.Sleep(300);

            // Click craft
            GlobalLog.Info("[DivineFontTask] Clicking craft button");
            if (!await ClickCraftButton()) return false;
            await Wait.Sleep(2000);

            // Handle result
            if (optionType == DivineFontOptionType.ExchangeForExceptional)
            {
                GlobalLog.Info("[DivineFontTask] Exchange craft complete. Taking result.");
            }
            else
            {
                if (!await PickBestGemChoice()) return false;
            }

            // Take result from slot
            return await TakeResultFromSlot();
        }

        private async Task<bool> PickBestGemChoice()
        {
            string gem1 = GetGemNameFromTooltip(0);
            string gem2 = GetGemNameFromTooltip(1);
            string gem3 = GetGemNameFromTooltip(2);

            GlobalLog.Info($"[DivineFontTask] Gem choices: '{gem1}', '{gem2}', '{gem3}'");

            if (!await ChooseMostValuableGem(new[] { gem1, gem2, gem3 })) return false;
            await Wait.Sleep(300);

            GlobalLog.Info("[DivineFontTask] Clicking confirm button");
            if (!await ClickConfirmButton()) return false;
            await Wait.Sleep(1000);

            return true;
        }

        private async Task<bool> TakeResultFromSlot()
        {
            if (!IsGemInSlot())
            {
                GlobalLog.Error("[DivineFontTask] No gem in slot after craft");
                return false;
            }

            string gemName = GetGemNameFromInputSlot();
            GlobalLog.Info($"[DivineFontTask] Removing gem '{gemName}' from input slot");
            if (!await RemoveGemFromSlot()) return false;
            _lastCraftedGemName = gemName;
            GlobalLog.Info("[DivineFontTask] Gem successfully transformed!");
            return true;
        }

        private async Task<bool> DropCraftedGem(string gemName)
        {
            GlobalLog.Info($"[DivineFontTask] Dropping '{gemName}' on the ground.");

            // Close Divine Font UI for more room
            await Coroutines.CloseBlockingWindows();
            await Wait.Sleep(300);

            // Open inventory (required to drop items)
            if (!LokiPoe.InGameState.InventoryUi.IsOpened)
            {
                if (!await Inventories.OpenInventory())
                {
                    GlobalLog.Error("[DivineFontTask] Failed to open inventory for gem drop.");
                    return false;
                }
                await Wait.Sleep(900); // Wait for viewport shift
            }

            // Find the gem in inventory
            var inv = InventoryUi.InventoryControl_Main;
            var gem = inv.Inventory.Items?.FirstOrDefault(i =>
                i.Name.Equals(gemName, StringComparison.OrdinalIgnoreCase));

            if (gem == null)
            {
                GlobalLog.Error($"[DivineFontTask] Could not find '{gemName}' in inventory to drop.");
                return false;
            }

            // Pick up the gem (attach to cursor)
            var pickupResult = inv.Pickup(gem.LocalId, true);
            if (pickupResult != PickupResult.None)
            {
                GlobalLog.Error($"[DivineFontTask] Failed to pick up '{gemName}': {pickupResult}");
                return false;
            }
            await Wait.Sleep(300);

            // Wait for cursor attach
            var sw = System.Diagnostics.Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 3000)
            {
                if (LokiPoe.InGameState.CursorItemOverlay.Item != null)
                    break;
                await Wait.Sleep(50);
            }

            if (LokiPoe.InGameState.CursorItemOverlay.Item == null)
            {
                GlobalLog.Error("[DivineFontTask] Gem did not attach to cursor.");
                return false;
            }

            // Drop at character's feet
            int cx, cy;
            LokiPoe.ClientFunctions.WorldToScreen(LokiPoe.Me.InteractCenterWorld, out cx, out cy);
            var dropX = cx;
            var dropY = cy + 50;

            await Wait.Sleep(200);
            MouseManager.SetMousePosition(new DreamPoeBot.Common.Vector2i(dropX, dropY), false);
            await Wait.Sleep(200);
            MouseManager.ClickLMB(dropX, dropY);
            await Wait.Sleep(300);

            // Wait for cursor to clear
            sw.Restart();
            while (sw.ElapsedMilliseconds < 3000)
            {
                if (LokiPoe.InGameState.CursorItemOverlay.Item == null)
                    break;
                await Wait.Sleep(50);
            }

            if (LokiPoe.InGameState.CursorItemOverlay.Item != null)
            {
                GlobalLog.Error("[DivineFontTask] Item stuck on cursor after drop attempt.");
                return false;
            }

            GlobalLog.Info($"[DivineFontTask] '{gemName}' dropped on ground successfully.");

            // Close inventory
            if (LokiPoe.InGameState.InventoryUi.IsOpened)
            {
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.open_inventory_panel, true, false, false);
                await Wait.Sleep(300);
            }

            return true;
        }

        #endregion

        #region Gem Finding

        private Item FindSpecificGemInInventory(string gemName)
        {
            var mainInventoryItems = InventoryUi.InventoryControl_Main.Inventory.Items;
            return mainInventoryItems?.FirstOrDefault(i =>
                i.Class == "Active Skill Gem" &&
                !i.IsCorrupted &&
                i.Name.Equals(gemName, StringComparison.OrdinalIgnoreCase));
        }

        private static readonly HashSet<string> ExceptionalGemNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "Empower Support", "Enlighten Support", "Enhance Support"
        };

        private Item FindSupportGemInInventory()
        {
            var mainInventoryItems = InventoryUi.InventoryControl_Main.Inventory.Items;
            return mainInventoryItems?.FirstOrDefault(i =>
                i.Class == "Support Skill Gem" &&
                !i.IsCorrupted &&
                i.SkillGemLevel == 1 &&
                i.Quality == 0 &&
                !ExceptionalGemNames.Contains(i.Name));
        }

        #endregion

        #region Stash Interaction

        private async Task<Item> WithdrawFromStash(DivineFontOption option)
        {
            return await WithdrawFromStash(option.Type, option.GemName);
        }

        private async Task<Item> WithdrawFromStash(DivineFontOptionType optionType, string specificGemName = null)
        {
            // Close Divine Font UI to access stash
            if (LokiPoe.InGameState.DivineFontUi.IsOpened)
                await Coroutines.CloseBlockingWindows();

            if (!await OpenStash())
                return null;

            bool withdrew = false;

            switch (optionType)
            {
                case DivineFontOptionType.TransformSpecificGem:
                    withdrew = await StashHelper.WithdrawItem(i =>
                        (i.Name == specificGemName || i.FullName == specificGemName) &&
                        i.Class == "Active Skill Gem" &&
                        !i.IsCorrupted);
                    break;

                case DivineFontOptionType.ExchangeForExceptional:
                    withdrew = await StashHelper.WithdrawItem(i =>
                        i.Class == "Support Skill Gem" &&
                        !i.IsCorrupted &&
                        i.SkillGemLevel == 1 &&
                        i.Quality == 0 &&
                        !ExceptionalGemNames.Contains(i.Name));
                    break;

                case DivineFontOptionType.TransformRandomSameColor:
                    withdrew = await WithdrawRandomGemFromStash();
                    break;
            }

            await Coroutines.CloseBlockingWindows();

            if (!withdrew)
            {
                GlobalLog.Info($"[DivineFontTask] No suitable gem found in stash for {optionType}.");
                return null;
            }

            GlobalLog.Info($"[DivineFontTask] Withdrew gem from stash for {optionType}.");

            // Reopen Divine Font
            if (!await OpenDivineFont())
            {
                GlobalLog.Error("[DivineFontTask] Failed to reopen Divine Font after stash trip.");
                return null;
            }

            // Find the withdrawn gem in inventory
            switch (optionType)
            {
                case DivineFontOptionType.TransformSpecificGem:
                    return FindSpecificGemInInventory(specificGemName);
                case DivineFontOptionType.ExchangeForExceptional:
                    return FindSupportGemInInventory();
                case DivineFontOptionType.TransformRandomSameColor:
                    return FindGemToEnchant();
                default:
                    return null;
            }
        }

        private async Task<bool> WithdrawRandomGemFromStash()
        {
            var desiredColor = FollowBotSettings.Instance.Lab.Color;

            if (desiredColor == LabSettings.GemColor.Smart)
            {
                var colorPriority = GetColorPriorityList();
                foreach (var color in colorPriority)
                {
                    GlobalLog.Info($"[DivineFontTask] Smart Stash: Checking for {color} gems...");
                    if (await TryWithdrawGemByColor(color))
                    {
                        GlobalLog.Info($"[DivineFontTask] Withdrew {color} gem (Smart Choice).");
                        return true;
                    }
                }
                return false;
            }

            return await TryWithdrawGemByColor(desiredColor);
        }

        private async Task<bool> OpenStash()
        {
            var stash = LokiPoe.ObjectManager.Stash;
            if (stash == null)
            {
                GlobalLog.Error("[DivineFontTask] No stash found near Divine Font");
                return false;
            }

            if (LokiPoe.Me.Position.Distance(stash.Position) > 20)
            {
                GlobalLog.Info("[DivineFontTask] Moving closer to stash");
                await Move.AtOnce(stash.Position, "Stash", 15);
            }

            var interactResult = await PlayerAction.Interact(stash);
            if (!interactResult)
            {
                GlobalLog.Error("[DivineFontTask] Failed to interact with stash");
                return false;
            }

            if (!await Wait.For(() => LokiPoe.InGameState.StashUi.IsOpened, "Stash UI opening", 200, 2000))
            {
                GlobalLog.Error("[DivineFontTask] Stash UI did not open");
                return false;
            }

            if (!await SwitchToConfiguredStashTab())
            {
                GlobalLog.Error("[DivineFontTask] Failed to switch to configured stash tab");
                await Coroutines.CloseBlockingWindows();
                return false;
            }

            return true;
        }

        #endregion

        private List<DivineFontOptionType> ReadAvailableOptionsFromUi()
        {
            var available = new List<DivineFontOptionType>();
            var craftOptions = DivineFontUi.CraftOptions;

            if (craftOptions == null)
                return available;

            foreach (var option in craftOptions)
            {
                var type = IdentifyOptionType(option.CraftOptionText);
                if (type.HasValue)
                    available.Add(type.Value);
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

            // "Exchange a Support Gem for a random Exceptional Gem" (or Empower/Enlighten/Enhance variant)
            if (text.IndexOf("support", StringComparison.OrdinalIgnoreCase) >= 0)
                return DivineFontOptionType.ExchangeForExceptional;

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

        private static async Task UpdateGemPricesIfNeeded(bool requireSafeArea = true, bool useBotCoroutine = true)
        {
            if (requireSafeArea && !World.CurrentArea.IsLabyrinthArea && !World.CurrentArea.IsHideoutArea && !World.CurrentArea.IsTown)
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
                    string url = "https://poe.ninja/poe1/api/economy/stash/current/item/overview?league=Allflame&type=SkillGem";
                    var downloadTask = client.DownloadStringTaskAsync(new Uri(url));
                    string json = useBotCoroutine
                        ? await Coroutine.ExternalTask(downloadTask)
                        : await downloadTask;
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

        public static async Task<string> RefreshPricesAndCalculateSmartGemChoice()
        {
            await UpdateGemPricesIfNeeded(requireSafeArea: false, useBotCoroutine: false);
            return CalculateSmartGemChoice();
        }

        private NetworkObject FindDivineFont()
        {
            return LokiPoe.ObjectManager.Objects
                .FirstOrDefault(o => o.Metadata == "Metadata/Terrain/Labyrinth/Objects/LabyrinthBlessingBench");
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
            var craftOptions = DivineFontUi.CraftOptions;
            if (craftOptions == null)
            {
                GlobalLog.Error("[DivineFontTask] Craft options not found");
                return false;
            }

            foreach (var option in craftOptions)
            {
                var type = IdentifyOptionType(option.CraftOptionText);
                if (type == targetType)
                {
                    option.Select();
                    await Coroutines.LatencyWait();
                    return true;
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
            DivineFontUi.Craft();

            // Wait for gem choices to appear
            await Wait.Sleep(500);
            await Coroutines.LatencyWait();

            return true;
        }

        private string GetGemNameFromTooltip(int gemIndex)
        {
            try
            {
                // Navigate to gems container parent: root[1][68][4][0][0]
                var gemsParent = ClassExtensions.GetElementByPath(68, 4, 0, 0);
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
                // Navigate to gems container: root[1][68][4][0][0]
                var gemsParent = ClassExtensions.GetElementByPath(68, 4, 0, 0);
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
            // Path: root.Children[1].Children[68].Children[4].Children[0].Children[1].Children[0]
            var confirmButton = ClassExtensions.GetElementByPath(68, 4, 0, 1, 0);

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

        private bool IsGemInSlot()
        {
            var items = DivineFontUi.InventoryControl?.Inventory?.Items;
            bool hasGem = items != null && items.Any();
            GlobalLog.Info($"[DivineFontTask] IsGemInSlot: {hasGem}");
            return hasGem;
        }

        private string GetGemNameFromInputSlot()
        {
            var item = DivineFontUi.InventoryControl?.Inventory?.Items?.FirstOrDefault();
            if (item == null)
            {
                GlobalLog.Warn("[DivineFontTask] GetGemNameFromInputSlot: No gem in slot");
                return null;
            }
            return item.Name;
        }

        private async Task<bool> RemoveGemFromSlot()
        {
            var inv = DivineFontUi.InventoryControl;
            var item = inv?.Inventory?.Items?.FirstOrDefault();

            if (item == null)
            {
                GlobalLog.Info("[DivineFontTask] No gem in slot to remove");
                return true;
            }

            var result = inv.FastMove(item.LocalId, true, false);
            if (result != FastMoveResult.None)
            {
                GlobalLog.Error($"[DivineFontTask] Failed to remove gem from slot: {result}");
                return false;
            }

            await Coroutines.LatencyWait();
            GlobalLog.Info("[DivineFontTask] Gem removed from slot");
            return true;
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

                // Standard gems (Superior quality) are always safe to use
                if (item.SkillGemQualityType == DreamPoeBot.Loki.Game.GameData.GemQualityType.Superior)
                {
                    GlobalLog.Info($"[DivineFontTask] Found standard gem '{item.Name}' for transformation.");
                    return item;
                }

                // Transfigured gem - check price by exact name
                if (_gemPrices.TryGetValue(item.Name, out double price))
                {
                    if (price <= FollowBotSettings.Instance.Lab.GemValueSafetyThreshold)
                    {
                        GlobalLog.Info($"[DivineFontTask] Found low-value transfigured gem '{item.Name}' ({price:F2}c).");
                        return item;
                    }
                    else
                    {
                        GlobalLog.Info($"[DivineFontTask] Skipping '{item.Name}' - worth {price:F2}c.");
                    }
                }
                else
                {
                    // Non-Superior but not in price list — name field may be broken or gem is new/unknown. Skip to be safe.
                    GlobalLog.Warn($"[DivineFontTask] Skipping transfigured gem '{item.Name}' - not found in price list (name bug or new gem).");
                }
            }

            return null;
        }




        private async Task<bool> TryWithdrawGemByColor(LabSettings.GemColor color)
        {
            var socketColor = ColorToSocketColor(color);

            // Use predicate-based StashHelper to find and withdraw gem
            return await StashHelper.WithdrawItem(i =>
                i.Class == "Active Skill Gem" &&
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

            // Calculate average and max prices per color
            var redAvg = redGems.Count > 0 ? redGems.Average(g => _gemPrices[g]) : 0;
            var greenAvg = greenGems.Count > 0 ? greenGems.Average(g => _gemPrices[g]) : 0;
            var blueAvg = blueGems.Count > 0 ? blueGems.Average(g => _gemPrices[g]) : 0;

            var redMax = redGems.Count > 0 ? redGems.Max(g => _gemPrices[g]) : 0;
            var greenMax = greenGems.Count > 0 ? greenGems.Max(g => _gemPrices[g]) : 0;
            var blueMax = blueGems.Count > 0 ? blueGems.Max(g => _gemPrices[g]) : 0;

            // Sort by average price first
            var colorValues = new List<(LabSettings.GemColor color, double avg, double max)>
            {
                (LabSettings.GemColor.Red, redAvg, redMax),
                (LabSettings.GemColor.Green, greenAvg, greenMax),
                (LabSettings.GemColor.Blue, blueAvg, blueMax)
            };

            var sorted = colorValues.OrderByDescending(cv => cv.avg).ToList();

            // Jackpot tiebreaker: among all colors within 10c average of the top,
            // prefer the one with the highest max gem IF that max is >= 100c
            const double averageThreshold = 10.0;
            const double jackpotFloor = 100.0;

            var topAvg = sorted[0].avg;
            var contenders = sorted.Where(cv => topAvg - cv.avg <= averageThreshold).ToList();

            if (contenders.Count >= 2)
            {
                var bestJackpot = contenders.OrderByDescending(cv => cv.max).First();

                if (bestJackpot.max >= jackpotFloor && bestJackpot.color != sorted[0].color)
                {
                    var oldFirst = sorted[0];
                    int jackpotIdx = sorted.FindIndex(cv => cv.color == bestJackpot.color);
                    GlobalLog.Info($"[DivineFontTask] Jackpot tiebreaker: {bestJackpot.color} (max {bestJackpot.max:F0}c) beats {oldFirst.color} (max {oldFirst.max:F0}c) — averages within {topAvg - bestJackpot.avg:F2}c");
                    sorted.RemoveAt(jackpotIdx);
                    sorted.Insert(0, bestJackpot);
                }
            }

            var sortedColors = sorted.Select(cv => cv.color).ToList();

            GlobalLog.Info($"[DivineFontTask] Color priority (by avg price): {string.Join(", ", sorted.Select(cv => $"{cv.color}(avg:{cv.avg:F2}c, max:{cv.max:F0}c)"))}");

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

            // Calculate average and max prices per color
            var redAvg = redGems.Count > 0 ? redGems.Average(g => _gemPrices[g]) : 0;
            var greenAvg = greenGems.Count > 0 ? greenGems.Average(g => _gemPrices[g]) : 0;
            var blueAvg = blueGems.Count > 0 ? blueGems.Average(g => _gemPrices[g]) : 0;

            var redMax = redGems.Count > 0 ? redGems.Max(g => _gemPrices[g]) : 0;
            var greenMax = greenGems.Count > 0 ? greenGems.Max(g => _gemPrices[g]) : 0;
            var blueMax = blueGems.Count > 0 ? blueGems.Max(g => _gemPrices[g]) : 0;

            var result = $"Smart Gem Choice Analysis:\n";
            result += $"================================\n";
            result += $"Red Gems: {redGems.Count} gems, Average: {redAvg:F2}c, Max: {redMax:F2}c\n";
            result += $"Green Gems: {greenGems.Count} gems, Average: {greenAvg:F2}c, Max: {greenMax:F2}c\n";
            result += $"Blue Gems: {blueGems.Count} gems, Average: {blueAvg:F2}c, Max: {blueMax:F2}c\n";
            result += $"================================\n";

            // Sort by average first
            var colorValues = new[]
            {
                (name: "Red", avg: redAvg, max: redMax, gems: redGems),
                (name: "Green", avg: greenAvg, max: greenMax, gems: greenGems),
                (name: "Blue", avg: blueAvg, max: blueMax, gems: blueGems)
            };

            var sorted = colorValues.OrderByDescending(cv => cv.avg).ToList();

            // Jackpot tiebreaker: among all colors within 10c average of the top,
            // prefer the one with the highest max gem IF that max is >= 100c
            const double averageThreshold = 10.0;
            const double jackpotFloor = 100.0;
            var tiebreakApplied = false;

            var topAvg = sorted[0].avg;
            var contenders = sorted.Where(cv => topAvg - cv.avg <= averageThreshold).ToList();

            if (contenders.Count >= 2)
            {
                var bestJackpot = contenders.OrderByDescending(cv => cv.max).First();

                if (bestJackpot.max >= jackpotFloor && bestJackpot.name != sorted[0].name)
                {
                    var oldFirst = sorted[0];
                    int jackpotIdx = sorted.FindIndex(cv => cv.name == bestJackpot.name);
                    sorted.RemoveAt(jackpotIdx);
                    sorted.Insert(0, bestJackpot);
                    tiebreakApplied = true;
                }
            }

            if (tiebreakApplied)
                result += $"Best Choice: {sorted[0].name} (Jackpot tiebreaker: max {sorted[0].max:F0}c beats {sorted[1].name} max {sorted[1].max:F0}c — averages within {topAvg - sorted[0].avg:F2}c)\n";
            else
                result += $"Best Choice: {sorted[0].name} (Average: {sorted[0].avg:F2}c)\n";

            result += $"\nTop 5 {sorted[0].name} gems by value:\n";

            var topGems = sorted[0].gems.OrderByDescending(g => _gemPrices[g]).Take(5).ToList();

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
                "Consecrated Path of Endurance", "Divine Blast of Radiance", "Dominating Blow of Inspiring", "Earthquake of Amplification",
                "Earthshatter of Fragility", "Earthshatter of Prominence", "Exsanguinate of Transmission",
                "Frozen Legion of Rallying", "Glacial Hammer of Shattering", "Ground Slam of Earthshaking",
                "Holy Flame Totem of Ire", "Holy Hammers of Spirals", "Holy Sweep of Hammerfalls", "Ice Crash of Cadence", "Infernal Blow of Immolation",
                "Leap Slam of Groundbreaking", "Molten Strike of the Zenith", "Perforate of Bloodshed",
                "Perforate of Duality", "Rage Vortex of Berserking", "Reap of Butchery", "Shield Crush of the Chieftain", "Shockwave Totem of Authority",
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

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(DreamPoeBot.Loki.Bot.Message message)
        {
            if (message.Id == Events.Messages.AreaChanged)
            {
                _hasExecuted = false;
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        private int GetRemainingCrafts()
        {
            var craftsElement = ClassExtensions.GetElementByPath(68, 0, 3, 1, 0);
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

        private async Task<bool> SwitchToConfiguredStashTab()
        {
            var settings = FollowBotSettings.Instance.Lab;
            if (settings.TabMode == LabSettings.StashTabMode.Index)
            {
                return await StashHelper.SwitchToTab(settings.StashTabIndex);
            }
            else
            {
                if (string.IsNullOrEmpty(settings.StashTabName))
                {
                    GlobalLog.Error("[DivineFontTask] Stash Tab Name is empty but mode is set to Name.");
                    return false;
                }
                return await StashHelper.SwitchToTab(settings.StashTabName);
            }
        }
    }
}
