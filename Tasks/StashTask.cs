using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
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
    /// <summary>
    /// Task for managing stash operations.
    /// Provides methods to deposit items from inventory to stash with filtering.
    /// </summary>
    public class StashTask : ITask
    {

        // Track if we were in a map before the area change
        private bool _wasInMap = false;
        // Flag to trigger deposit when entering hideout from a map
        private bool _shouldDepositToStash = false;
        // Flag to trigger deposit from chat command
        public static bool ShouldDepositFromChat = false;

        public string Name { get { return "StashTask"; } }
        public string Description { get { return "This task manages stash operations."; } }
        public string Author { get { return "Rushtothesun"; } }
        public string Version { get { return "0.0.0.1"; } }

        private const int MaxRetries = 3;
        private const int RetryDelayMs = 500;

        public void Start()
        {
            _wasInMap = false;
            _shouldDepositToStash = false;
            ShouldDepositFromChat = false;
        }

        public void Stop()
        {
            _wasInMap = false;
            _shouldDepositToStash = false;
            ShouldDepositFromChat = false;
        }

        public void Tick()
        {

        }

        public async Task<bool> Run()
        {
            // Don't run if not in game
            if (!LokiPoe.IsInGame)
            {
                return false;
            }

            // Don't run if escape state is active
            if (LokiPoe.StateManager.IsEscapeStateActive)
            {
                return false;
            }

            // Don't run if dead
            if (LokiPoe.Me.IsDead)
            {
                return false;
            }

            // Auto-deposit when entering hideout from a map
            if (_shouldDepositToStash)
            {
                var area = LokiPoe.CurrentWorldArea;
                if (area.IsHideoutArea)
                {
                    GlobalLog.Info("[StashTask] Entered hideout from map, depositing inventory to stash.");
                    _shouldDepositToStash = false;

                    var stashType = GetConfiguredStashType();
                    var tabName = FollowBotSettings.Instance.Stash.TargetStashTab;

                    bool success = await DepositInventoryToTab(tabName, stashType);
                    if (success)
                    {
                        GlobalLog.Info("[StashTask] Auto-deposit completed successfully.");
                    }
                    else
                    {
                        GlobalLog.Warn("[StashTask] Auto-deposit failed.");
                    }

                    return true;
                }
                else
                {
                    // Not in hideout yet, clear the flag since we ended up somewhere else
                    _shouldDepositToStash = false;
                }
            }

            // Deposit from chat command
            if (ShouldDepositFromChat)
            {
                var area = LokiPoe.CurrentWorldArea;
                if (area.IsHideoutArea || area.IsTown)
                {
                    GlobalLog.Info("[StashTask] Chat command received, depositing inventory to stash.");
                    ShouldDepositFromChat = false;

                    var stashType = GetConfiguredStashType();
                    var tabName = FollowBotSettings.Instance.Stash.TargetStashTab;

                    bool success = await DepositInventoryToTab(tabName, stashType);
                    if (success)
                    {
                        GlobalLog.Info("[StashTask] Chat command deposit completed successfully.");
                    }
                    else
                    {
                        GlobalLog.Warn("[StashTask] Chat command deposit failed.");
                    }

                    return true;
                }
                else
                {
                    GlobalLog.Warn("[StashTask] Chat command deposit ignored - not in hideout or town.");
                    ShouldDepositFromChat = false;
                }
            }

            // This task is meant to be called by other tasks
            // No automatic logic here
            return false;
        }

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            if (message.Id == Events.Messages.AreaChanged)
            {
                var area = LokiPoe.CurrentWorldArea;

                // Check if we're transitioning from a map to a hideout and auto-deposit is enabled
                if (_wasInMap && area.IsHideoutArea && FollowBotSettings.Instance.Stash.AutoDepositOnMapExit)
                {
                    GlobalLog.Info("[StashTask] Detected map -> hideout transition, setting deposit flag.");
                    _shouldDepositToStash = true;
                }

                // Update the _wasInMap flag for the next area change
                _wasInMap = area.IsMap;

                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }

        #region Public Methods

        /// <summary>
        /// Deposits all tradable items from inventory to the specified stash tab.
        /// Filters out quest items and excluded slots.
        /// Relies on stash affinities for auto-routing (currency, maps, etc.).
        /// </summary>
        /// <param name="tabName">Target stash tab name</param>
        /// <param name="stashType">Type of stash (Regular or Guild)</param>
        /// <returns>True if operation completed successfully</returns>
        public async Task<bool> DepositInventoryToTab(string tabName, StashHelper.StashType stashType = StashHelper.StashType.Regular)
        {
            GlobalLog.Info($"[StashTask] Starting inventory deposit to {stashType} stash tab: {tabName}");

            // Step 1: Open stash
            if (!await OpenStash(stashType))
                return false;

            // Step 2: Switch to target tab with retry
            bool tabSwitched = false;
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                if (StashHelper.SwitchToTab(tabName, stashType))
                {
                    tabSwitched = true;
                    break;
                }

                if (attempt < MaxRetries)
                {
                    GlobalLog.Warn($"[StashTask] SwitchToTab attempt {attempt} failed, retrying in {RetryDelayMs}ms...");
                    await Wait.SleepSafe(RetryDelayMs);
                }
            }

            if (!tabSwitched)
            {
                GlobalLog.Error($"[StashTask] Failed to switch to tab '{tabName}' after {MaxRetries} attempts");
                return false;
            }

            // Small random delay after tab switch (anti-detection)
            await Wait.SleepSafe(LokiPoe.Random.Next(100, 200));

            // Step 3: Deposit items with filtering
            await DepositInventoryItems(stashType);

            GlobalLog.Info("[StashTask] Inventory deposit completed");
            return true;
        }

        #endregion

        #region Stash Interaction

        /// <summary>
        /// Opens the specified type of stash with retry logic.
        /// </summary>
        private async Task<bool> OpenStash(StashHelper.StashType stashType)
        {
            // Check if stash is already open
            bool isAlreadyOpen = stashType == StashHelper.StashType.Guild
                ? LokiPoe.InGameState.GuildStashUi.IsOpened
                : LokiPoe.InGameState.StashUi.IsOpened;

            if (isAlreadyOpen)
            {
                GlobalLog.Debug($"[StashTask] {stashType} stash is already open");
                return true;
            }

            NetworkObject stashObject = null;

            // Find the correct stash type
            if (stashType == StashHelper.StashType.Regular)
            {
                // Regular stash: Metadata/MiscellaneousObjects/Stash
                stashObject = LokiPoe.ObjectManager.Stash;
                if (stashObject == null)
                {
                    GlobalLog.Error("[StashTask] No regular stash found");
                    return false;
                }
            }
            else // Guild stash
            {
                // Guild stash: Metadata/MiscellaneousObjects/GuildStash
                stashObject = LokiPoe.ObjectManager
                    .GetObjectsByType<GuildStash>()
                    .FirstOrDefault();

                if (stashObject == null)
                {
                    GlobalLog.Error("[StashTask] No guild stash found");
                    return false;
                }
            }

            // Move closer if needed
            if (LokiPoe.Me.Position.Distance(stashObject.Position) > 20)
            {
                GlobalLog.Info($"[StashTask] Moving closer to {stashType} stash");
                await Move.AtOnce(stashObject.Position, $"{stashType} Stash", 15);

                // Random delay after movement (anti-detection)
                await Wait.SleepSafe(LokiPoe.Random.Next(100, 300));
            }

            // Random "look around" before interacting (5% chance)
            if (LokiPoe.Random.Next(1, 100) > 95)
            {
                await Wait.SleepSafe(LokiPoe.Random.Next(200, 600));
            }

            // Interact with stash with retry
            for (int attempt = 1; attempt <= MaxRetries; attempt++)
            {
                GlobalLog.Info($"[StashTask] Opening {stashType} stash (attempt {attempt}/{MaxRetries})");
                var interactResult = await PlayerAction.Interact(stashObject);
                if (!interactResult)
                {
                    if (attempt < MaxRetries)
                    {
                        GlobalLog.Warn($"[StashTask] Failed to interact with {stashType} stash, retrying in {RetryDelayMs}ms...");
                        await Wait.SleepSafe(RetryDelayMs);
                        continue;
                    }
                    GlobalLog.Error($"[StashTask] Failed to interact with {stashType} stash after {MaxRetries} attempts");
                    return false;
                }

                // Wait for UI with random variance (750-900ms)
                await Wait.Sleep(LokiPoe.Random.Next(200, 700));

                // Verify correct stash UI opened
                bool isOpened = stashType == StashHelper.StashType.Guild
                    ? LokiPoe.InGameState.GuildStashUi.IsOpened
                    : LokiPoe.InGameState.StashUi.IsOpened;

                if (isOpened)
                {
                    GlobalLog.Info($"[StashTask] {stashType} stash opened successfully");
                    return true;
                }

                if (attempt < MaxRetries)
                {
                    GlobalLog.Warn($"[StashTask] {stashType} stash UI did not open, retrying in {RetryDelayMs}ms...");
                    await Wait.SleepSafe(RetryDelayMs);
                }
            }

            GlobalLog.Error($"[StashTask] {stashType} stash UI did not open after {MaxRetries} attempts");
            return false;
        }

        #endregion

        #region Item Deposit Logic

        /// <summary>
        /// Deposits all valid items from player inventory to the currently open stash tab.
        /// Filters out quest items and excluded slots (similar to TradeTask logic).
        /// Uses randomization for human-like behavior.
        /// </summary>
        private async Task DepositInventoryItems(StashHelper.StashType stashType)
        {
            var mainInventoryItems = InventoryUi.InventoryControl_Main.Inventory.Items;
            int depositedCount = 0;
            int skippedCount = 0;

            // Iterate through inventory grid (12 columns x 5 rows)
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    var item = mainInventoryItems.FirstOrDefault(i =>
                        i.LocationTopLeft.X == x &&
                        i.LocationTopLeft.Y == y);

                    if (item == null)
                        continue;

                    // Filter: Skip quest items
                    if (item.Class == ItemClasses.QuestItem)
                    {
                        GlobalLog.Info($"[StashTask] Skipping quest item: {item.Name}");
                        skippedCount++;
                        continue;
                    }

                    // Filter: Skip excluded slots (from UI settings)
                    if (FollowBotSettings.Instance.Trade.IsSlotExcluded(x, y))
                    {
                        GlobalLog.Info($"[StashTask] Skipping excluded slot ({x}, {y}): {item.Name}");
                        skippedCount++;
                        continue;
                    }

                    // Human-like pause (10% chance, 100-500ms) - from TradeTask pattern
                    if (LokiPoe.Random.Next(1, 100) > 90)
                    {
                        int pauseDuration = LokiPoe.Random.Next(100, 200);
                        await Wait.SleepSafe(pauseDuration);
                    }

                    // Deposit item (affinity will auto-route to correct tabs)
                    bool success = StashHelper.DepositItem(item.LocalId, stashType);

                    if (success)
                    {
                        depositedCount++;
                    }
                    else
                    {
                        GlobalLog.Warn($"[StashTask] Failed to deposit: {item.Name}");
                    }

                    // Small random delay between items (30-70ms) - from TradeTask pattern
                    await Wait.SleepSafe(LokiPoe.Random.Next(30, 70));
                }
            }

            GlobalLog.Info($"[StashTask] Deposit complete. Deposited: {depositedCount}, Skipped: {skippedCount}");
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Gets the configured stash type from settings.
        /// </summary>
        private StashHelper.StashType GetConfiguredStashType()
        {
            return FollowBotSettings.Instance.Stash.UseGuildStash
                ? StashHelper.StashType.Guild
                : StashHelper.StashType.Regular;
        }

        #endregion
    }
}
