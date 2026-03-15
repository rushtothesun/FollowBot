using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.Helpers;
using FollowBot.SimpleEXtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;

namespace FollowBot.Tasks
{
    class TradeTask : ITask
    {

        private const string AcceptButtonText = "accept";
        private const string AcceptButtonTextCountdown = "accept (0)";

        public string Author => string.Empty;
        public string Description => string.Empty;
        public string Name => "TradeTask";
        public string Version => string.Empty;

        public void Start()
        {
            GlobalLog.Info($"[{Name}] Task Loaded.");
        }

        public void Stop() { }

        public void Tick() { }

        private bool IsReturnModeArea(DatWorldAreaWrapper area)
        {
            return area.IsHideoutArea ||
                   area.IsTown ||
                   area.Id == "HeistHub" ||
                   area.Name == "Monastery of the Keepers";
        }

        public async Task<bool> Run()
        {
            bool hasVisibleTradeNotification = NotificationHud.NotificationList
                .Any(n => n.IsVisible && n.NotificationTypeEnum == NotificationType.Trade);

            if (hasVisibleTradeNotification && LokiPoe.InstanceInfo.PartyStatus == PartyStatus.PartyMember)
            {
                await HandleTradeRequest();
            }
            else
            {
                return false;
            }

            if (TradeUi.IsOpened)
            {
                var currentArea = World.CurrentArea;

                if (IsReturnModeArea(currentArea))
                {
                    // Safe area - wait and decide which mode
                    await Wait.SleepSafe(1500); // Give leader time to add items

                    // Check if leader is giving items
                    var leaderOffer = TradeUi.TradeControl?.InventoryControl_OtherOffer.Inventory.Items;
                    if (leaderOffer != null && leaderOffer.Any())
                    {
                        if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                            GlobalLog.Info("[TradeTask] Leader has items - executing receive mode");
                        await ExecuteReceiveMode();
                    }
                    else
                    {
                        if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                            GlobalLog.Info("[TradeTask] Leader has no items - executing return mode");
                        await ExecuteReturnMode();
                    }
                }
                else
                {
                    // Map/other areas - always receive mode
                    await ExecuteReceiveMode();
                }

                return true;
            }
            return true;
        }

        private async Task ExecuteReceiveMode()
        {
            if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                GlobalLog.Info("[TradeTask] Start Trade in map (Receive Mode)");

            try
            {
                while (TradeUi.IsOpened && BotManager.IsRunning)
                {
                    await Coroutines.LatencyWait();

                    if (TradeUi.TradeControl == null)
                    {
                        if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                            GlobalLog.Debug("[TradeTask] TradeControl is null");
                        break;
                    }

                    await ViewAllTransparentItems();

                    if (TradeUi.TradeControl.AcceptButtonText == AcceptButtonText && TradeUi.TradeControl.OtherAcceptedTheOffert)
                    {
                        TradeUi.TradeControl.Accept(true);
                        if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                            GlobalLog.Info("[TradeTask] Accepting trade");
                        await Coroutines.CloseBlockingWindows();
                        await Coroutines.LatencyWait();
                    }
                }
            }
            catch (Exception)
            {
                if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                    GlobalLog.Debug("[TradeTask] Some error in the trade");
                await Coroutines.ReactionWait();
                await Coroutines.LatencyWait();
            }
        }

        private async Task ExecuteReturnMode()
        {
            if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                GlobalLog.Info("[TradeTask] Start Trade in Hideout (Return Mode)");

            try
            {
                while (TradeUi.IsOpened && BotManager.IsRunning)
                {
                    await Coroutines.LatencyWait();

                    if (TradeUi.TradeControl == null)
                    {
                        if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                            GlobalLog.Debug("[TradeTask] TradeControl is null");
                        break;
                    }

                    if (TradeUi.TradeControl.MeAcceptedTheOffert)
                    {
                        if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                            GlobalLog.Info("[TradeTask] Waiting for other player to accept.");
                        continue;
                    }

                    await TransferAllTradableItems();

                    var mainInventoryItems = InventoryUi.InventoryControl_Main.Inventory.Items;
                    var tradeItemsFromYourInventory = TradeUi.TradeControl.InventoryControl_YourOffer.Inventory.Items;
                    int tradableItemCount = mainInventoryItems.Count(i =>
                        i.Class != ItemClasses.QuestItem &&
                        !FollowBotSettings.Instance.Trade.IsSlotExcluded(i.LocationTopLeft.X, i.LocationTopLeft.Y));

                    if (tradeItemsFromYourInventory.Count == tradableItemCount && TradeUi.TradeControl.AcceptButtonText == AcceptButtonText)
                    {
                        TradeUi.TradeControl.Accept(true);
                        if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                            GlobalLog.Info("[TradeTask] Accepting trade");
                        await Coroutines.LatencyWait();
                    }
                }
            }
            catch (Exception)
            {
                if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                    GlobalLog.Debug("[TradeTask] Some error in the trade");
                await Coroutines.ReactionWait();
                await Coroutines.LatencyWait();
            }
        }

        private async Task ViewAllTransparentItems()
        {
            List<Item> allItems = TradeUi.TradeControl.InventoryControl_OtherOffer.Inventory.Items;
            if (allItems == null) return;

            var transparentItems = allItems.Where(item => TradeUi.TradeControl.InventoryControl_OtherOffer.IsItemTransparent(item.LocalId));
            if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                GlobalLog.Debug($"[TradeTask] Found {transparentItems.Count()} transparent items.");

            foreach (Item item in transparentItems)
            {
                if (TradeUi.TradeControl.AcceptButtonText == AcceptButtonText || TradeUi.TradeControl.AcceptButtonText == AcceptButtonTextCountdown)
                {
                    int itemId = item.LocalId;
                    TradeUi.TradeControl?.InventoryControl_OtherOffer.ViewItemsInInventory((inventory, inventoryItem) => inventoryItem.LocalId == itemId, () => TradeUi.IsOpened);
                    continue;
                }
                await Coroutines.LatencyWait();
                int rand = LokiPoe.Random.Next(1000, 2000);
                await Wait.SleepSafe(rand);
            }
        }

        private async Task TransferAllTradableItems()
        {
            var mainInventoryItems = InventoryUi.InventoryControl_Main.Inventory.Items;
            for (int y = 0; y < 5; y++) // rows
            {
                for (int x = 0; x < 12; x++) // columns
                {
                    var item = mainInventoryItems.FirstOrDefault(i =>
                        i.LocationTopLeft.X == x &&
                        i.LocationTopLeft.Y == y);

                    if (item == null)
                        continue;

                    if (item.Class == ItemClasses.QuestItem)
                    {
                        if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                            GlobalLog.Debug($"[TradeTask] Skipping quest item: {item.Name}");
                        continue;
                    }

                    if (FollowBotSettings.Instance.Trade.IsSlotExcluded(x, y))
                    {
                        if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                            GlobalLog.Debug($"[TradeTask] Skipping excluded slot ({x}, {y}): {item.Name}");
                        continue;
                    }

                    // Human-like pause (10% chance)
                    int randomChance = LokiPoe.Random.Next(1, 100);
                    if (randomChance > 90)
                    {
                        int pauseDuration = LokiPoe.Random.Next(100, 500);
                        if (FollowBotSettings.Instance.Trade.EnableTradeDebugLog)
                            GlobalLog.Debug($"[TradeTask] Random human-like pause: {pauseDuration}ms");
                        await Wait.SleepSafe(pauseDuration);
                    }

                    InventoryUi.InventoryControl_Main.FastMove(item.LocalId, true, false);
                    await Wait.SleepSafe(LokiPoe.Random.Next(30, 70));
                }
            }
        }

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        private static async Task<bool> HandleTradeRequest()
        {
            var visibleNotifications = NotificationHud.NotificationList.Where(n => n.IsVisible).ToList();
            bool hasVisibleTradeNotification = visibleNotifications.Any(n => n.NotificationTypeEnum == NotificationType.Trade);

            if (hasVisibleTradeNotification)
            {
                GlobalLog.Warn($"[FollowBot] Visible Notifications: {visibleNotifications.Count}");
                ProcessNotificationEx isTradeRequestToBeAccepted = (x, y) =>
                {
                    var res = y == NotificationType.Trade && PartyHelper.IsNameInWhiteList(x.CharacterName, x.AccountName);
                    GlobalLog.Warn($"[FollowBot] Detected {y} request from char: {x.CharacterName} [AccountName: {x.AccountName}] Accepting? {res}");
                    return res;
                };

                // Human-like delay
                await Wait.SleepSafe(400, 550);

                var ret = NotificationHud.HandleNotificationEx(isTradeRequestToBeAccepted);
                GlobalLog.Warn($"[HandleTradeRequest] Result: {ret}");

                await Coroutines.LatencyWait();
                return ret == HandleNotificationResult.Accepted;
            }
            return false;
        }
    }
}
