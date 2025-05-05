using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;

namespace FollowBot.Tasks
{
    class TradeTask : ITask
    {
        private readonly ILog Log = Logger.GetLoggerInstanceForType();
        public string Author => string.Empty;

        public string Description => string.Empty;

        public string Name => "TradeTask";

        public string Version => string.Empty;



        public void Start()
        {
            Log.InfoFormat("[{0}] Task Loaded.", Name);

        }

        public void Stop()
        {
        }

        public void Tick()
        {
        }
        public async Task<bool> Run()
        {

            if (NotificationHud.IsOpened && LokiPoe.InstanceInfo.PartyStatus == PartyStatus.PartyMember)
            {
                await HandleTradeRequest();

            }
            else
            {
                return false;
            }


            var currentArea = World.CurrentArea;

            if (TradeUi.IsOpened)
            {

                if (!currentArea.IsHideoutArea && !currentArea.IsTown)
                {
                    Log.InfoFormat("[TradeTask] Start Trade in map");
                    //  Map trade logic
                    try
                    {

                        while (TradeUi.IsOpened && BotManager.IsRunning)
                        {
                            await Coroutines.LatencyWait();

                            TradeControlWrapper tradeControl1 = TradeUi.TradeControl;

                            if (tradeControl1 == null)
                            {
                                GlobalLog.Debug("[TradeTask] TradeControl is null");
                                break;
                            }

                            List<Item> allItems = TradeUi.TradeControl.InventoryControl_OtherOffer.Inventory.Items;

                            if (allItems == null) break;

                            var transparentItems = allItems?.Where(
                                                          (item) => TradeUi.TradeControl.InventoryControl_OtherOffer.IsItemTransparent(item.LocalId));
                            Log.DebugFormat($"[TradeTask] Find {transparentItems.Count()} transparent items.");


                            foreach (Item item in transparentItems)
                            {
                                if (!(TradeUi.TradeControl.AcceptButtonText != "accept") || !(TradeUi.TradeControl.AcceptButtonText != "accept (0)"))
                                {
                                    int itemId = item.LocalId;
                                    TradeControlWrapper tradeControl = TradeUi.TradeControl;
                                    tradeControl?.InventoryControl_OtherOffer.ViewItemsInInventory((inventory, invenoryItem) => invenoryItem.LocalId == itemId, () => TradeUi.IsOpened);
                                    continue;
                                }
                                await Coroutines.LatencyWait();
                                int rand = LokiPoe.Random.Next(1000, 2000);
                                await Coroutine.Sleep(rand);
                            }
                            if (TradeUi.TradeControl.AcceptButtonText == "accept" && TradeUi.TradeControl.OtherAcceptedTheOffert)
                            {
                                TradeUi.TradeControl.Accept(true);
                                Log.InfoFormat("[TradeTask] Accepting trade");
                                await Coroutines.CloseBlockingWindows();
                                await Coroutines.LatencyWait();
                            }

                        }
                    }
                    catch (Exception)
                    {
                        GlobalLog.Debug("[TradeTask] Some error in the trade");
                        await Coroutines.ReactionWait();
                        await Coroutines.LatencyWait();
                    }
                }
                if (currentArea.IsHideoutArea || currentArea.IsTown)
                {
                    Log.InfoFormat("[TradeTask] Start Trade in Hideout");

                    try
                    {

                        while (TradeUi.IsOpened && BotManager.IsRunning)
                        {
                            await Coroutines.LatencyWait();

                            TradeControlWrapper tradeControl1 = TradeUi.TradeControl;

                            if (tradeControl1 == null)
                            {
                                GlobalLog.Debug("[TradeTask] TradeControl is null");
                                break;
                            }


                            if (TradeUi.TradeControl.MeAcceptedTheOffert)
                            {
                                Log.InfoFormat("[TradeTask] Wait accept trade");
                                continue;
                            }

                            var mainInventoryItems = InventoryUi.InventoryControl_Main.Inventory.Items;
                            var sortedInventoryItems = mainInventoryItems.OrderByDescending(item => item.Size).ToList();


                            foreach (Item item in sortedInventoryItems)
                            {
                                List<Item> yourOfferInventoryItems = TradeUi.TradeControl.InventoryControl_YourOffer.Inventory.Items;

                                if (yourOfferInventoryItems.Any(e => e.LocalId == item.LocalId))
                                {
                                    continue;
                                }

                                InventoryUi.InventoryControl_Main.FastMove(item.LocalId, true, false);

                                await Wait.SleepSafe(LokiPoe.Random.Next(30, 70));

                            }

                            var tradeItemsFromYourInventory = TradeUi.TradeControl.InventoryControl_YourOffer.Inventory.Items;

                            if (tradeItemsFromYourInventory.Count == mainInventoryItems.Count && TradeUi.TradeControl.AcceptButtonText == "accept")
                            {
                                TradeUi.TradeControl.Accept(true);
                                Log.InfoFormat("[TradeTask] Accepting trade");
                                await Coroutines.LatencyWait();
                            }

                        }

                    }
                    catch (Exception)
                    {
                        GlobalLog.Debug("[TradeTask] Some error in the trade");
                        await Coroutines.ReactionWait();
                        await Coroutines.LatencyWait();
                    }



                }

                return true;
            }
            return true;

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
            if (NotificationHud.NotificationList.Where(x => x.IsVisible).ToList().Count > 0)
            {
                FollowBot.Log.WarnFormat($"[FollowBot] Visible Notifications: {NotificationHud.NotificationList.Where(x => x.IsVisible).ToList().Count}");
                ProcessNotificationEx isTradeRequestToBeAccepted = (x, y) =>
                {
                    var res = y == NotificationType.Trade && (string.IsNullOrEmpty(FollowBotSettings.Instance.InviteWhiteList) || FollowBotSettings.Instance.InviteWhiteList.Contains(x.CharacterName));
                    FollowBot.Log.WarnFormat($"[FollowBot] Detected {y} request from char: {x.CharacterName} [AccountName: {x.AccountName}] Accepting? {res}");
                    return res;
                };

                var anyVis = NotificationHud.NotificationList.Any(x => x.IsVisible);
                if (anyVis)
                {
                    await Wait.Sleep(500);
                }
                var ret = NotificationHud.HandleNotificationEx(isTradeRequestToBeAccepted);
                FollowBot.Log.WarnFormat($"[HandleTradeRequest] Result: {ret}");
                await Coroutines.LatencyWait();
                if (ret == HandleNotificationResult.Accepted) return true;
            }
            return false;

        }

    }


}

