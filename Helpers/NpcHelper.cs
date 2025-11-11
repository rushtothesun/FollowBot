using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using System.Threading.Tasks;
using System.Windows.Forms;
using static DreamPoeBot.Loki.Game.LokiPoe;
using static DreamPoeBot.Loki.Game.LokiPoe.InGameState;

namespace FollowBot.Helpers
{
    class NpcHelper
    {
        public static async Task<bool> MoveToAndTalk(NetworkObject npcObj)
        {
            if (npcObj == null) return false;
            if (!npcObj.IsTargetable) return false;
            if (NpcDialogUi.IsOpened && RewardUi.IsOpened) return true;


            if (Me.Position.Distance(npcObj.Position) > 30)
            {
                GlobalLog.Debug($"[NcpHelper] try come to npc [{npcObj.Name}]");
                await npcObj.WalkablePosition().TryComeAtOnce();
            }
            await Coroutines.CloseBlockingWindows();

            return await PlayerAction.Interact(npcObj, () => NpcDialogUi.IsOpened || RewardUi.IsOpened, "Dialog open");
        }

        public static async Task<bool> TalkAndSkipDialog(NetworkObject npcObj)
        {
            if (!await MoveToAndTalk(npcObj)) return false;
            if (!await SkipDialog(npcObj)) return false;
            return true;
        }

        public static async Task<bool> SkipDialog(NetworkObject obj)
        {
            if ((NpcDialogUi.DialogDepth != 1 && NpcDialogUi.IsOpened))
            {
                for (int i = 0; i < 10; i++)
                {
                    Input.SimulateKeyEvent(Keys.Escape, true, false, false, Keys.None);
                    await Wait.SleepSafe(250, 500);
                    if (!obj.IsTargetable) return false;
                    if (NpcDialogUi.DialogDepth == 1 && NpcDialogUi.IsOpened) return true;
                    if (!NpcDialogUi.IsOpened && !RewardUi.IsOpened) return false;
                }
                return false;
            }
            return true;
        }

        public static bool SelectDialog(string dialogName)
        {
            if (!NpcDialogUi.IsOpened) return false;
            var dialog = NpcDialogUi.DialogEntries.Find((x) => x.Text.ContainsIgnorecase(dialogName));
            if (dialog == null)
            {
                GlobalLog.Error($"[NpcHelper]: cannot find dialog : [{dialogName}]");
                return false;
            }

            if (NpcDialogUi.Converse(dialog.Text) != ConverseResult.None)
            {
                GlobalLog.Error($"[NpcHelper]: cannot converse with dialog : [{dialog.Text}]");
                return false;

            }
            return true;
        }



        public static async Task<bool> BanditKillSelect(NetworkObject bandit)
        {
            if (bandit == null)
            {
                return false;
            }

            await bandit.WalkablePosition().ComeAtOnce();

            if (await OpenBanditPanel(bandit))
            {
                if (BanditPanel.KillBandit(true) != TalkToBanditResult.None)
                {
                    return false;
                }
            }
            return true;
        }
        private static async Task<bool> OpenBanditPanel(NetworkObject bandit)
        {
            // If the panel is not open, do nothing. The leader must initiate.
            if (!BanditPanel.IsOpened)
            {
                return false;
            }

            // If the panel IS open, this bot must still interact to enable its own buttons.
            if (!await PlayerAction.Interact(bandit, () => NpcDialogUi.IsOpened, "Bandit dialog open"))
            {
                // If interaction fails, check if the panel is open anyway before failing completely.
                if (!BanditPanel.IsOpened)
                {
                    return false;
                }
            }

            await SkipDialog(bandit);
            
            await Wait.SleepSafe(100, 200); // Brief wait for UI to update.

            // Final confirmation that the panel is still open.
            return BanditPanel.IsOpened;
        }
        public static async Task<bool> TakeReward(NetworkObject obj, string dialogName)
        {
            if (!await MoveToAndTalk(obj))
            {
                return false;
            }
            if (!await SkipDialog(obj)) return false;

            if (!SelectDialog(dialogName))
            {
                return false;
            }
            await Wait.Sleep(250);

            if (!RewardUi.IsOpened) return false;

            var rewardInvenoryControls = RewardUi.InventoryControls;
            if (rewardInvenoryControls.Count == 0)
            {
                GlobalLog.Error("[NpcHelper] Reward count says 0.");
            }
            var rewardControl = rewardInvenoryControls[0];
            var reward = rewardControl.Inventory.Items[0];
            if (reward == null) return false;
            int expectedItemCount = Inventories.InventoryItems.Count + 1;
            var result = rewardControl.FastMoveReward(reward.LocalId, true);
            if (result != FastMoveResult.None)
            {
                GlobalLog.Error($"[NpcHelper][TakeReward]  cannot take reward [{reward.FullName}]\n ERROR: {result}");
                return false;
            }
            await Wait.Sleep(500);
            if (Inventories.InventoryItems.Count != expectedItemCount)
            {
                GlobalLog.Error("[NpcHelper][TakeReward] some error try more");
                return false;
            }
            GlobalLog.Debug($"[NpcHelper][TakeReward] succes taken reward [{reward.FullName}]");
            return true;
        }

        public static async Task<bool> TakeReward(NetworkObject obj, string dialogName, string rewardItemName)
        {
            if (!await PrepareRewardDialog(obj, dialogName))
                return false;

            var rewardInventoryControls = RewardUi.InventoryControls;
            GlobalLog.Debug($"[NpcHelper][TakeReward] RewardUi has {rewardInventoryControls.Count} inventory control(s)");
            
            if (rewardInventoryControls.Count == 0)
            {
                GlobalLog.Error("[NpcHelper][TakeReward] Reward inventory control count is 0.");
                return false;
            }

            // Find the correct inventory control using PlacementGraph
            var rewardControl = FindRewardControlByPlacementGraph(rewardInventoryControls, rewardItemName);
            if (rewardControl == null)
            {
                GlobalLog.Error($"[NpcHelper][TakeReward] Cannot find reward '{rewardItemName}' in PlacementGraph");
                return false;
            }

            // Get the actual item from the inventory
            var rewardItem = rewardControl.Inventory.Items.Find(i => i.Name == rewardItemName || i.FullName == rewardItemName);
            if (rewardItem == null)
            {
                GlobalLog.Error($"[NpcHelper][TakeReward] Found control but reward '{rewardItemName}' not in Inventory.Items");
                return false;
            }

            return await TakeRewardItem(rewardControl, rewardItem);
        }

        private static async Task<bool> PrepareRewardDialog(NetworkObject obj, string dialogName)
        {
            if (!await MoveToAndTalk(obj))
                return false;
            
            if (!await SkipDialog(obj))
                return false;

            if (!SelectDialog(dialogName))
                return false;

            await Wait.Sleep(250);

            if (!RewardUi.IsOpened)
            {
                GlobalLog.Error("[NpcHelper] RewardUi is not opened after selecting dialog");
                return false;
            }

            return true;
        }

        private static InventoryControlWrapper FindRewardControlByPlacementGraph(System.Collections.Generic.List<InventoryControlWrapper> controls, string itemName)
        {
            foreach (var control in controls)
            {
                var graphValues = control.InventorySlotUiElement.PlacementGraph.Values;
                foreach (var graphItem in graphValues)
                {
                    if (graphItem == null)
                        continue;
                    
                    if (graphItem.Name == itemName || graphItem.FullName == itemName)
                    {
                        GlobalLog.Debug($"[NpcHelper][FindRewardControl] Found '{graphItem.FullName}' (LocalId: {graphItem.LocalId}) in PlacementGraph");
                        return control;
                    }
                }
            }
            return null;
        }

        private static async Task<bool> TakeRewardItem(InventoryControlWrapper control, Item item)
        {
            GlobalLog.Debug($"[NpcHelper][TakeRewardItem] Using FastMove for '{item.FullName}' (LocalId: {item.LocalId})");
            
            int expectedItemCount = Inventories.InventoryItems.Count + 1;
            var result = control.FastMove(item.LocalId, true, false);
            
            GlobalLog.Debug($"[NpcHelper][TakeRewardItem] FastMove returned: {result}");
            
            if (result != FastMoveResult.None)
            {
                GlobalLog.Error($"[NpcHelper][TakeRewardItem] FastMove failed for [{item.FullName}]\n ERROR: {result}");
                return false;
            }
            
            await Wait.Sleep(500);
            
            if (Inventories.InventoryItems.Count != expectedItemCount)
            {
                GlobalLog.Error("[NpcHelper][TakeRewardItem] Inventory count did not increase as expected");
                return false;
            }
            
            GlobalLog.Debug($"[NpcHelper][TakeRewardItem] Successfully taken [{item.FullName}]");
            return true;
        }

        public static async Task<bool> TakeRewardAndUseBook(NetworkObject obj, string dialogName)
        {
            if (!await TakeReward(obj, dialogName))
            {
                return false;
            }

            // Wait a moment for the item to appear in inventory
            await Wait.Sleep(300);

            // Find ALL Books of Skill and Books of Regrets
            var books = Inventories.InventoryItems.FindAll(x => x.Name == "Book of Skill" || x.Name == "Book of Regrets");
            
            if (books == null || books.Count == 0)
            {
                GlobalLog.Warn("[NpcHelper][TakeRewardAndUseBook] No books found in inventory after taking reward");
                return true; // Still return true since we took the reward successfully
            }

            GlobalLog.Debug($"[NpcHelper][TakeRewardAndUseBook] Found {books.Count} book(s) in inventory, now using all of them");

            // Make sure inventory is open
            if (!await Inventories.OpenInventory())
            {
                GlobalLog.Error("[NpcHelper][TakeRewardAndUseBook] Failed to open inventory");
                return false;
            }

            await Wait.Sleep(200);

            // Use all books in inventory
            int usedCount = 0;
            foreach (var book in books)
            {
                GlobalLog.Debug($"[NpcHelper][TakeRewardAndUseBook] Using {book.Name} (ID: {book.LocalId})");
                
                var err = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.UseItem(book.LocalId);
                if (err != UseItemResult.None)
                {
                    GlobalLog.Error($"[NpcHelper][TakeRewardAndUseBook] Failed to use {book.Name}. Error: {err}");
                    continue; // Continue to try using other books
                }
                
                usedCount++;
                await Wait.Sleep(300); // Wait between using each book
            }

            GlobalLog.Debug($"[NpcHelper][TakeRewardAndUseBook] Successfully used {usedCount} out of {books.Count} book(s)");
            
            // Wait for the last book to be consumed
            await Wait.Sleep(300);
            
            // Close inventory
            await Coroutines.CloseBlockingWindows();
            
            return usedCount > 0; // Return true if at least one book was used
        }
    }
}
