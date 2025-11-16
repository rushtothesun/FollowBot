using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using FollowBot.SimpleEXtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;

namespace FollowBot.Helpers
{
    public static class PartyHelper
    {

        public static List<string> PartyPortal = new List<string>()
        {
            "portal pls",
            "Portal ples",
            "new portal pls",
            "can i have a portal?",
            "new gate pls",
            "bro give me a port",
            "door pls",
            "port",
            "Port pls",
            "1 to teleport",
        };

        public static async Task<bool> HandlePartyInvite()
        {
      bool hasVisiblePartyNotification = NotificationHud.NotificationList
       .Any(n => n.IsVisible && n.NotificationTypeEnum == NotificationType.Party);
            if (hasVisiblePartyNotification && LokiPoe.InGameState.NotificationHud.NotificationList.Where(x => x.IsVisible).ToList().Count > 0)
            {
                FollowBot.Log.WarnFormat($"[FollowBot] Visible Notifications: {LokiPoe.InGameState.NotificationHud.NotificationList.Where(x => x.IsVisible).ToList().Count}");
                LokiPoe.InGameState.ProcessNotificationEx isPartyRequestToBeAccepted = (x, y) =>
                {
                    var res = y == LokiPoe.InGameState.NotificationType.Party && IsNameInWhiteList(x.CharacterName, x.AccountName);
                    FollowBot.Log.WarnFormat($"[FollowBot] Detected {y.ToString()} request from char: {x.CharacterName} [AccountName: {x.AccountName}] Accepting? {res}");
                    return res;
                };

                var anyVis = LokiPoe.InGameState.NotificationHud.NotificationList.Any(x => x.IsVisible);
                if (anyVis)
                {
                    await Wait.Sleep(500);
                }
                var ret = LokiPoe.InGameState.NotificationHud.HandleNotificationEx(isPartyRequestToBeAccepted);
                FollowBot.Log.WarnFormat($"[HandlePartyInvite] Result: {ret}");
                await Coroutines.LatencyWait();
                if (ret == LokiPoe.InGameState.HandleNotificationResult.Accepted) return true;
            }
            return false;
        }

        public static async Task<bool> LeaveParty()
        {

            if (!LokiPoe.InGameState.ChatPanel.IsOpened)
                LokiPoe.InGameState.ChatPanel.ToggleChat();

            if (!LokiPoe.InGameState.ChatPanel.IsOpened) return false;
            LokiPoe.InGameState.ChatPanel.Chat("/kick " + LokiPoe.Me.Name);
            await Coroutines.LatencyWait();

            if (LokiPoe.InGameState.ChatPanel.IsOpened)
                LokiPoe.InGameState.ChatPanel.ToggleChat();

            return true;
        }

        public static async Task<bool> GoToPartyHideOut(string name)
        {
            await Coroutines.CloseBlockingWindows();
            await Coroutines.LatencyWait();

            LokiPoe.InGameState.PartyHud.OpenContextMenu(name);
            var ret = LokiPoe.InGameState.ContextMenu.VisitHideout();
            await Coroutines.LatencyWait();
            await Coroutines.ReactionWait();
            if (ret != LokiPoe.InGameState.ContextMenuResult.None)
            {
                return false;
            }
            return true;
        }

        public static async Task<bool> FastGotoPartyZone(string name)
        {
            await Coroutines.CloseBlockingWindows();
            await Coroutines.LatencyWait();

            var ret = LokiPoe.InGameState.PartyHud.FastGoToZone(name);
            await Coroutines.LatencyWait();
            await Coroutines.ReactionWait();
            if (ret != LokiPoe.InGameState.FastGoToZoneResult.None)
            {
                GlobalLog.Error($"[FastGotoPartyZone] Returned Error: {ret}");
                return false;
            }
            if (LokiPoe.InGameState.GlobalWarningDialog.IsOpened)
                LokiPoe.InGameState.GlobalWarningDialog.ConfirmDialog();
            return true;
        }

        /// <summary>
        /// Checks if a character name or account name is in the whitelist.
        /// Whitelist entries with "#" are treated as account names, otherwise as character names.
        /// </summary>
        public static bool IsNameInWhiteList(string characterName, string accountName)
        {
            var whiteListCollection = FollowBotSettings.Instance.InviteTradeWhiteList;
            
            // If whitelist is empty, allow all
            if (whiteListCollection == null || whiteListCollection.Count == 0)
                return true;

            foreach (var entry in whiteListCollection)
            {
                if (string.IsNullOrEmpty(entry))
                    continue;

                // If entry contains "#", it's an account name
                if (entry.Contains("#"))
                {
                    if (!string.IsNullOrEmpty(accountName) && entry.Equals(accountName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
                else
                {
                    // Otherwise, it's a character name
                    if (!string.IsNullOrEmpty(characterName) && entry.Equals(characterName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
            
            return false;
        }
    }
}
