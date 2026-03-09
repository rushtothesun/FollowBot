using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.Game.NativeWrappers;
using FollowBot.SimpleEXtensions;

namespace FollowBot.Helpers
{
    /// <summary>
    /// Helper class for interacting with the stash system.
    /// Provides methods to deposit and withdraw items from stash tabs.
    /// </summary>
    public static class StashHelper
    {
        private const int TAB_SWITCH_DELAY = 500; // milliseconds to wait for tab to load

        /// <summary>
        /// Specifies which type of stash to interact with.
        /// </summary>
        public enum StashType
        {
            Regular,
            Guild
        }

        #region Core Stash Operations

        /// <summary>
        /// Navigates to a stash tab by name.
        /// </summary>
        /// <param name="tabName">Tab name (e.g., "Currency")</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>True if navigation was successful</returns>
        public static bool SwitchToTab(string tabName, StashType stashType = StashType.Regular)
        {
            if (!IsStashOpened(stashType))
            {
                GlobalLog.Error($"[StashHelper] {stashType} stash UI is not open");
                return false;
            }

            var tabControl = GetStashTabControl(stashType);

            // Fast path: Check if we are already on the correct tab
            if (tabControl.CurrentTabName == tabName)
            {
                GlobalLog.Debug($"[StashHelper] Already on {stashType} stash tab: {tabName}");
                return true;
            }

            // Sanity check: Ensure the requested tab actually exists
            var allTabs = tabControl.TabNames;
            if (allTabs != null && !allTabs.Contains(tabName))
            {
                GlobalLog.Error($"[StashHelper] Fatal Error: Cannot switch to tab '{tabName}'. It does not exist in your {stashType} stash!");
                return false;
            }

            var result = tabControl.SwitchToTabMouse(tabName);

            // SwitchToTabResult.None = SUCCESS (counter-intuitive but verified)
            bool success = result == SwitchToTabResult.None;

            if (success)
            {
                Thread.Sleep(TAB_SWITCH_DELAY);
                GlobalLog.Info($"[StashHelper] Switched to {stashType} stash tab: {tabName}");
            }
            else
            {
                GlobalLog.Error($"[StashHelper] Failed to switch to {stashType} stash tab '{tabName}': {result}");
            }

            return success;
        }

        /// <summary>
        /// Navigates to a stash tab by display index.
        /// </summary>
        /// <param name="displayIndex">The visual position of the tab (0-based)</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>True if navigation was successful</returns>
        public static bool SwitchToTab(int displayIndex, StashType stashType = StashType.Regular)
        {
            if (!IsStashOpened(stashType))
            {
                GlobalLog.Error($"[StashHelper] {stashType} stash UI is not open");
                return false;
            }

            var tabControl = GetStashTabControl(stashType);

            // Fast path: Check if we are already on the correct tab index
            if (tabControl.CurrentTabIndex == displayIndex)
            {
                GlobalLog.Debug($"[StashHelper] Already on {stashType} stash tab index: {displayIndex}");
                return true;
            }

            // Sanity check: Ensure index is within bounds
            var allTabs = tabControl.TabNames;
            if (allTabs != null && (displayIndex < 0 || displayIndex >= allTabs.Count))
            {
                GlobalLog.Error($"[StashHelper] Fatal Error: Cannot switch to tab index {displayIndex}. Valid indices are 0 to {allTabs.Count - 1}.");
                return false;
            }

            var result = tabControl.SwitchToTabMouse(displayIndex);

            bool success = result == SwitchToTabResult.None;

            if (success)
            {
                Thread.Sleep(TAB_SWITCH_DELAY);
                GlobalLog.Info($"[StashHelper] Switched to {stashType} stash tab index: {displayIndex}");
            }
            else
            {
                GlobalLog.Error($"[StashHelper] Failed to switch to {stashType} stash tab index {displayIndex}: {result}");
            }

            return success;
        }

        /// <summary>
        /// Gets the currently opened stash inventory.
        /// </summary>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>The current stash inventory, or null if stash is not open</returns>
        private static Inventory GetCurrentStashInventory(StashType stashType = StashType.Regular)
        {
            if (!IsStashOpened(stashType))
            {
                GlobalLog.Error($"[StashHelper] {stashType} stash UI is not open");
                return null;
            }

            // Always get fresh inventory reference
            var stashControl = GetStashInventoryControl(stashType);
            if (stashControl == null)
            {
                GlobalLog.Error($"[StashHelper] Could not get {stashType} stash control");
                return null;
            }

            var inventory = stashControl.Inventory;
            if (inventory == null)
            {
                GlobalLog.Error($"[StashHelper] Could not get {stashType} stash inventory");
                return null;
            }

            return inventory;
        }

        #endregion

        #region Deposit Items to Stash

        /// <summary>
        /// Deposits an item from player inventory to the currently open stash tab.
        /// </summary>
        /// <param name="itemLocalId">The LocalId of the item in player inventory</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>True if deposit was successful</returns>
        public static bool DepositItem(int itemLocalId, StashType stashType = StashType.Regular)
        {
            if (!IsStashOpened(stashType))
            {
                GlobalLog.Error($"[StashHelper] {stashType} stash UI is not open");
                return false;
            }

            var playerControl = LokiPoe.InGameState.InventoryUi.InventoryControl_Main;
            if (playerControl == null)
            {
                GlobalLog.Error("[StashHelper] Could not get player inventory control");
                return false;
            }

            var result = playerControl.FastMove(itemLocalId);

            // FastMoveResult.None = SUCCESS
            bool success = result == FastMoveResult.None;

            if (success)
            {
                // Wait for item to register in stash
                Thread.Sleep(LokiPoe.Random.Next(50, 100));
            }
            else
            {
                GlobalLog.Error($"[StashHelper] Failed to deposit item {itemLocalId} to {stashType} stash: {result}");
            }

            return success;
        }

        /// <summary>
        /// Deposits an item from player inventory to a specific stash tab.
        /// </summary>
        /// <param name="itemLocalId">The LocalId of the item in player inventory</param>
        /// <param name="tabName">Target stash tab name</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>True if deposit was successful</returns>
        public static bool DepositItemToTab(int itemLocalId, string tabName, StashType stashType = StashType.Regular)
        {
            if (!SwitchToTab(tabName, stashType))
                return false;

            return DepositItem(itemLocalId, stashType);
        }

        /// <summary>
        /// Deposits an item from player inventory to a specific stash tab by index.
        /// </summary>
        /// <param name="itemLocalId">The LocalId of the item in player inventory</param>
        /// <param name="displayIndex">Target stash tab display index</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>True if deposit was successful</returns>
        public static bool DepositItemToTab(int itemLocalId, int displayIndex, StashType stashType = StashType.Regular)
        {
            if (!SwitchToTab(displayIndex, stashType))
                return false;

            return DepositItem(itemLocalId, stashType);
        }

        #endregion

        #region Withdraw Items from Stash

        /// <summary>
        /// Withdraws the first item matching the predicate from the currently open stash tab.
        /// This is the core method - all other withdraw methods use this internally.
        ///
        /// Examples:
        ///   // Withdraw by name from regular stash
        ///   WithdrawItem(i => i.Name == "Scroll of Wisdom");
        ///
        ///   // Withdraw from guild stash
        ///   WithdrawItem(i => i.Name == "Chaos Orb", StashType.Guild);
        ///
        ///   // Withdraw any red gem with 20% quality
        ///   WithdrawItem(i => i.SkillGemLevel > 0 && i.SocketColor == SocketColor.Red && i.Quality == 20);
        ///
        ///   // Withdraw item level 86+ Elder ring
        ///   WithdrawItem(i => i.Class == "Ring" && i.ItemLevel >= 86 && i.IsElderItem);
        ///
        ///   // Withdraw 6-link unique chest
        ///   WithdrawItem(i => i.Class == "Body Armour" && i.MaxLinkCount == 6 && i.Rarity == Rarity.Unique);
        /// </summary>
        /// <param name="predicate">Function to test each item</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>True if a matching item was found and withdrawn</returns>
        public static bool WithdrawItem(Func<Item, bool> predicate, StashType stashType = StashType.Regular)
        {
            var inventory = GetCurrentStashInventory(stashType);
            if (inventory == null)
                return false;

            var item = inventory.Items.FirstOrDefault(predicate);
            if (item == null)
            {
                GlobalLog.Debug("[StashHelper] No item found matching criteria");
                return false;
            }

            var stashControl = GetStashInventoryControl(stashType);
            var result = stashControl.FastMove(item.LocalId);

            bool success = result == FastMoveResult.None;

            if (success)
            {
                Thread.Sleep(200);
                GlobalLog.Info($"[StashHelper] Withdrew item {item.LocalId} from {stashType} stash");
            }
            else
            {
                GlobalLog.Error($"[StashHelper] Failed to withdraw item {item.LocalId} from {stashType} stash: {result}");
            }

            return success;
        }

        /// <summary>
        /// Withdraws an item from a specific stash tab using a predicate.
        /// </summary>
        /// <param name="tabName">The stash tab name</param>
        /// <param name="predicate">Function to test each item</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>True if the item was found and withdrawn</returns>
        public static bool WithdrawItemFromTab(string tabName, Func<Item, bool> predicate, StashType stashType = StashType.Regular)
        {
            if (!SwitchToTab(tabName, stashType))
                return false;

            return WithdrawItem(predicate, stashType);
        }

        /// <summary>
        /// Withdraws an item from a specific stash tab by index using a predicate.
        /// </summary>
        /// <param name="displayIndex">The stash tab display index</param>
        /// <param name="predicate">Function to test each item</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>True if the item was found and withdrawn</returns>
        public static bool WithdrawItemFromTab(int displayIndex, Func<Item, bool> predicate, StashType stashType = StashType.Regular)
        {
            if (!SwitchToTab(displayIndex, stashType))
                return false;

            return WithdrawItem(predicate, stashType);
        }

        #endregion

        #region Query Stash Items

        /// <summary>
        /// Gets all items from the currently open stash tab.
        /// </summary>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>List of items in the current stash tab</returns>
        public static List<Item> GetCurrentTabItems(StashType stashType = StashType.Regular)
        {
            var inventory = GetCurrentStashInventory(stashType);
            return inventory?.Items ?? new List<Item>();
        }

        /// <summary>
        /// Finds the first item matching the predicate in the currently open stash tab.
        /// This is the core method - all other find methods use this internally.
        ///
        /// Examples:
        ///   // Find by name in regular stash
        ///   var item = FindItem(i => i.Name == "Chaos Orb");
        ///
        ///   // Find in guild stash
        ///   var item = FindItem(i => i.Name == "Divine Orb", StashType.Guild);
        ///
        ///   // Find level 20 gem
        ///   var gem = FindItem(i => i.SkillGemLevel == 20);
        ///
        ///   // Find unique amulet with item level 85+
        ///   var amulet = FindItem(i => i.Class == "Amulet" && i.Rarity == Rarity.Unique && i.ItemLevel >= 85);
        /// </summary>
        /// <param name="predicate">Function to test each item</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>The item if found, or null</returns>
        public static Item FindItem(Func<Item, bool> predicate, StashType stashType = StashType.Regular)
        {
            var inventory = GetCurrentStashInventory(stashType);
            if (inventory == null)
                return null;

            return inventory.Items.FirstOrDefault(predicate);
        }

        /// <summary>
        /// Finds all items matching the predicate in the currently open stash tab.
        /// </summary>
        /// <param name="predicate">Function to test each item</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>List of matching items</returns>
        public static List<Item> FindAllItems(Func<Item, bool> predicate, StashType stashType = StashType.Regular)
        {
            var inventory = GetCurrentStashInventory(stashType);
            if (inventory == null)
                return new List<Item>();

            return inventory.Items.Where(predicate).ToList();
        }

        /// <summary>
        /// Gets the total stack count of items matching the predicate in the currently open stash tab.
        ///
        /// Examples:
        ///   // Count Chaos Orbs in regular stash
        ///   int chaosCount = GetItemCount(i => i.Name == "Chaos Orb");
        ///
        ///   // Count Divine Orbs in guild stash
        ///   int divineCount = GetItemCount(i => i.Name == "Divine Orb", StashType.Guild);
        ///
        ///   // Count all corrupted gems
        ///   int corruptedGems = GetItemCount(i => i.SkillGemLevel > 0 && i.IsCorrupted);
        /// </summary>
        /// <param name="predicate">Function to test each item</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>Total stack count of matching items</returns>
        public static int GetItemCount(Func<Item, bool> predicate, StashType stashType = StashType.Regular)
        {
            var inventory = GetCurrentStashInventory(stashType);
            if (inventory == null)
                return 0;

            return inventory.Items
                .Where(predicate)
                .Sum(i => i.StackCount);
        }

        #endregion

        #region Stash Tab Information

        /// <summary>
        /// Gets all available stash tabs.
        /// </summary>
        /// <returns>List of stash tab information</returns>
        public static List<StashTabInfo> GetAllStashTabs()
        {
            return LokiPoe.InstanceInfo.StashTabs ?? new List<StashTabInfo>();
        }

        /// <summary>
        /// Finds a stash tab by name.
        /// </summary>
        /// <param name="tabName">The display name of the tab</param>
        /// <returns>StashTabInfo if found, or null</returns>
        public static StashTabInfo FindTabByName(string tabName)
        {
            return GetAllStashTabs().FirstOrDefault(t => t.DisplayName == tabName);
        }

        /// <summary>
        /// Gets the currently open stash tab information.
        /// </summary>
        /// <returns>Current stash tab info, or null if stash is not open</returns>
        public static StashTabInfo GetCurrentTabInfo()
        {
            if (!LokiPoe.InGameState.StashUi.IsOpened)
                return null;

            return LokiPoe.InGameState.StashUi.StashTabInfo;
        }

        /// <summary>
        /// Logs detailed information about all available stash tabs for debugging purposes.
        /// </summary>
        public static void LogStashTabDetails()
        {
            try
            {
                var tabs = LokiPoe.InstanceInfo.StashTabs;
                GlobalLog.Debug($"[StashHelper] Dumping StashTabInfo for {tabs.Count} tabs:");
                foreach (var tab in tabs.OrderBy(t => t.DisplayIndex))
                {
                    GlobalLog.Debug($"[StashHelper] Name: '{tab.DisplayName}', DisplayIndex: {tab.DisplayIndex}, InventoryId: {tab.InventoryId}, Type: {tab.TabType}, IsHidden: {tab.IsHiddenFlagged}");
                }
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[StashHelper] Error logging stash tab details: {ex}");
            }
        }

        #endregion

        #region Private Helper Methods for StashType

        /// <summary>
        /// Checks if the specified stash type is currently opened.
        /// </summary>
        private static bool IsStashOpened(StashType stashType)
        {
            return stashType == StashType.Guild
                ? LokiPoe.InGameState.GuildStashUi.IsOpened
                : LokiPoe.InGameState.StashUi.IsOpened;
        }

        /// <summary>
        /// Gets the inventory control for the specified stash type.
        /// </summary>
        private static InventoryControlWrapper GetStashInventoryControl(StashType stashType)
        {
            return stashType == StashType.Guild
                ? LokiPoe.InGameState.GuildStashUi.InventoryControl
                : LokiPoe.InGameState.StashUi.InventoryControl;
        }

        /// <summary>
        /// Gets the tab control for the specified stash type.
        /// </summary>
        private static TabControlWrapper GetStashTabControl(StashType stashType)
        {
            return stashType == StashType.Guild
                ? LokiPoe.InGameState.GuildStashUi.TabControl
                : LokiPoe.InGameState.StashUi.TabControl;
        }

        #endregion
    }
}