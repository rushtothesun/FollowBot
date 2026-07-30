using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace FollowBot.Tasks
{
    public class TradeBuyoutTask : ITask
    {
        public static volatile bool HasPendingBuyout;
        public static volatile int TargetX;
        public static volatile int TargetY;
        public static volatile string ExpectedFullName;
        public static volatile string ExpectedBaseName;
        public static volatile string ExpectedCost;

        public string Name => "TradeBuyoutTask";
        public string Description => "Buys a specific item from a merchant tab after receiving coordinates from the trade site.";
        public string Author => "Rushtothesun";
        public string Version => "1.0.0.0";

        public void Start() { }
        public void Stop() { ClearState(); }
        public void Tick() { }

        public async Task<bool> Run()
        {
            if (!HasPendingBuyout)
                return false;

            if (!LokiPoe.IsInGame)
                return true;

            try
            {
                // 1. Wait for PurchaseUi to open
                if (!LokiPoe.InGameState.PurchaseUi.IsOpened)
                {
                    GlobalLog.Info("[TradeBuyoutTask] Waiting for PurchaseUi to open...");
                    var opened = await Wait.For(() => LokiPoe.InGameState.PurchaseUi.IsOpened, "PurchaseUi opening", 100, 15000);
                    if (!opened)
                    {
                        GlobalLog.Error("[TradeBuyoutTask] PurchaseUi did not open within 15s. Item may already be sold. Aborting.");
                        return true;
                    }
                }

                await Wait.SleepSafe(100, 200);

                // 2. Find item at target coordinates
                var items = LokiPoe.InGameState.PurchaseUi.InventoryControl.Inventory.Items;
                var item = items.FirstOrDefault(i => i.LocationTopLeft.X == TargetX && i.LocationTopLeft.Y == TargetY);
                if (item == null)
                {
                    GlobalLog.Error($"[TradeBuyoutTask] No item found at ({TargetX},{TargetY}). Aborting.");
                    return true;
                }

                GlobalLog.Info($"[TradeBuyoutTask] Found item at ({TargetX},{TargetY}): '{item.FullName}' / '{item.Name}' note='{item.DisplayNote}'");

                // 3. Hover to load tooltip data
                LokiPoe.InGameState.PurchaseUi.InventoryControl.OpenDisplayNote(item.LocalId);
                await Wait.SleepSafe(200, 250);

                // Re-read item after hover
                items = LokiPoe.InGameState.PurchaseUi.InventoryControl.Inventory.Items;
                item = items.FirstOrDefault(i => i.LocationTopLeft.X == TargetX && i.LocationTopLeft.Y == TargetY);
                if (item == null)
                {
                    GlobalLog.Error($"[TradeBuyoutTask] Item disappeared after hover at ({TargetX},{TargetY}). Aborting.");
                    return true;
                }

                // 4. Verify identity
                if (!VerifyItem(item))
                    return true;

                // 5. Check affordability (if enabled)
                if (FollowBotSettings.Instance.TradeBuyout.CheckAffordability)
                {
                    List<KeyValuePair<string, double>> costInfo;
                    bool canAfford;
                    LokiPoe.InGameState.PurchaseUi.InventoryControl.GetItemCostAsDouble(item.LocalId, out costInfo, out canAfford);

                    // canAfford checks gold but not currency orbs
                    if (!canAfford)
                    {
                        GlobalLog.Error($"[TradeBuyoutTask] Cannot afford item at ({TargetX},{TargetY}) — not enough gold. Aborting.");
                        return true;
                    }

                    // Manually verify currency orbs in inventory
                    if (costInfo != null)
                    {
                        var inventoryItems = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.Inventory.Items;
                        foreach (var cost in costInfo)
                        {
                            if (cost.Key.Equals("Gold", StringComparison.OrdinalIgnoreCase))
                                continue;

                            int required = (int)Math.Ceiling(cost.Value);
                            int have = inventoryItems
                                .Where(i => i.Name.Equals(cost.Key, StringComparison.OrdinalIgnoreCase))
                                .Sum(i => i.StackCount);

                            if (have < required)
                            {
                                GlobalLog.Error($"[TradeBuyoutTask] Cannot afford item at ({TargetX},{TargetY}) — need {required}x {cost.Key}, have {have}. Aborting.");
                                return true;
                            }
                        }
                    }

                    GlobalLog.Info("[TradeBuyoutTask] Affordability check passed.");
                }

                // 6. FastMove (buy) — up to 3 attempts
                for (int attempt = 0; attempt < 3; attempt++)
                {
                    if (attempt > 0)
                    {
                        // Re-hover and re-verify before retry
                        LokiPoe.InGameState.PurchaseUi.InventoryControl.OpenDisplayNote(item.LocalId);
                        await Wait.SleepSafe(300, 500);

                        items = LokiPoe.InGameState.PurchaseUi.InventoryControl.Inventory.Items;
                        item = items.FirstOrDefault(i => i.LocationTopLeft.X == TargetX && i.LocationTopLeft.Y == TargetY);
                        if (item == null)
                        {
                            GlobalLog.Error($"[TradeBuyoutTask] Item disappeared before retry {attempt + 1}. Aborting.");
                            return true;
                        }

                        if (!VerifyItem(item))
                            return true;
                    }

                    var result = LokiPoe.InGameState.PurchaseUi.InventoryControl.FastMove(item.LocalId, true, false);

                    if (result == FastMoveResult.None)
                    {
                        GlobalLog.Info($"[TradeBuyoutTask] Successfully purchased item at ({TargetX},{TargetY})!");
                        break;
                    }

                    GlobalLog.Warn($"[TradeBuyoutTask] FastMove attempt {attempt + 1} failed: {result}");

                    if (attempt < 2)
                        await Wait.SleepSafe(200, 400);
                    else
                        GlobalLog.Error($"[TradeBuyoutTask] All 3 FastMove attempts failed for item at ({TargetX},{TargetY}).");
                }
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[TradeBuyoutTask] Exception: {ex.Message}");
            }
            finally
            {
                await Coroutines.CloseBlockingWindows();
                ClearState();
            }

            return true;
        }

        private bool VerifyItem(Item item)
        {
            bool mismatch = false;

            if (!string.IsNullOrEmpty(ExpectedFullName) && !string.IsNullOrEmpty(item.FullName))
            {
                if (item.FullName != ExpectedFullName)
                {
                    GlobalLog.Warn($"[TradeBuyoutTask] FullName mismatch: expected='{ExpectedFullName}' actual='{item.FullName}'");

                    // Foulborn uniques: the game API never exposes the "Foulborn" prefix in item.FullName.
                    // If the expected name starts with "Foulborn ", strip it and re-verify against the
                    // actual name + confirm the item is Unique (rares cannot be Foulborn uniques).
                    if (ExpectedFullName.StartsWith("Foulborn ", StringComparison.OrdinalIgnoreCase))
                    {
                        var stripped = ExpectedFullName.Substring("Foulborn ".Length);
                        if (stripped == item.FullName && item.Rarity == Rarity.Unique)
                        {
                            GlobalLog.Info($"[TradeBuyoutTask] Foulborn fallback: stripped='{stripped}' matches actual='{item.FullName}', item is Unique — treating as match.");
                        }
                        else
                        {
                            GlobalLog.Error($"[TradeBuyoutTask] Foulborn fallback failed: stripped='{stripped}', actual='{item.FullName}', rarity='{item.Rarity}'. Aborting.");
                            mismatch = true;
                        }
                    }
                    else
                    {
                        GlobalLog.Error($"[TradeBuyoutTask] FullName mismatch is not Foulborn-related. Aborting.");
                        mismatch = true;
                    }
                }
            }

            if (!string.IsNullOrEmpty(ExpectedBaseName))
            {
                if (item.Name != ExpectedBaseName)
                {
                    GlobalLog.Error($"[TradeBuyoutTask] BaseName mismatch: expected='{ExpectedBaseName}' actual='{item.Name}'");
                    mismatch = true;
                }
            }

            if (!string.IsNullOrEmpty(ExpectedCost))
            {
                if (item.DisplayNote != ExpectedCost)
                {
                    GlobalLog.Error($"[TradeBuyoutTask] Cost mismatch: expected='{ExpectedCost}' actual='{item.DisplayNote}'");
                    mismatch = true;
                }
            }

            if (mismatch)
            {
                GlobalLog.Error($"[TradeBuyoutTask] Item verification failed at ({TargetX},{TargetY}). Aborting.");
            }

            return !mismatch;
        }

        private static void ClearState()
        {
            HasPendingBuyout = false;
            TargetX = 0;
            TargetY = 0;
            ExpectedFullName = null;
            ExpectedBaseName = null;
            ExpectedCost = null;
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
