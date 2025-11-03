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
            if (BanditPanel.IsOpened) return true;

            await PlayerAction.Interact(bandit);
            await Wait.SleepSafe(250, 500);

            for (int i = 0; i < 5; i++)
            {
                if (BanditPanel.IsOpened) return true;
                Input.SimulateKeyEvent(Keys.Escape, true, false, false, Keys.None);
                await Wait.SleepSafe(250, 500);
            }

            return false;
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
                GlobalLog.Error("[NpcHelper] you see this error becaus DPB suck");
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
    }
}
