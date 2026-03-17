using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Controllers;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using FollowBot.SimpleEXtensions.Global;
using DreamPoeBot.BotFramework;
using DreamPoeBot.Common;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace FollowBot.Tasks
{
    /// <summary>
    /// Zone Sweeper architecture: nested loop approach.
    /// Outer loop finds clusters, smooth-moves to them.
    /// Inner loop sweeps all nearby items with inventory open once per cluster.
    /// </summary>
    public class UltimatumUnloaderTask : ITask
    {
        private static readonly Stopwatch _delaySw = new Stopwatch();
        private static bool _isEnabled = false;
        private static bool _isInProgress = false;
        private static Dictionary<int, int> _itemFailCounts = new Dictionary<int, int>();
        private const int MaxRetries = 3;

        // Backups for task suppression
        private static bool _originalShouldFollow = true;
        private static bool _originalShouldLoot = true;

        // Search radii
        private const float UltimatumItemRadius = 50f;
        private const float InnerSearchRadius = 50f;
        private const float InteractionRange = 30f;

        public string Name => "UltimatumUnloaderTask";
        public string Description => "Sweeps and unloads items hidden by loot filters for the Leader after an Ultimatum.";
        public string Author => "Rushtothesun";
        public string Version => "2.0.0.0";

        // This method is called by the UltimatumTask when it finishes successfully
        public static void TriggerUnloader()
        {
            GlobalLog.Info("[UltimatumUnloaderTask] Triggered. Suppressing Follow and Loot tasks.");

            // Backup settings
            _originalShouldFollow = FollowBotSettings.Instance.Follow.ShouldFollow;
            _originalShouldLoot = FollowBotSettings.Instance.Loot.ShouldLoot;

            // Force disable
            FollowBotSettings.Instance.Follow.ShouldFollow = false;
            FollowBotSettings.Instance.Loot.ShouldLoot = false;

            _isEnabled = true;
            _isInProgress = true;
            _itemFailCounts.Clear();
            _delaySw.Restart();
        }

        private static void RestoreSettings()
        {
            if (!_isEnabled && !_isInProgress) return;

            GlobalLog.Info($"[UltimatumUnloaderTask] Restoring settings: Follow={_originalShouldFollow}, Loot={_originalShouldLoot}");
            FollowBotSettings.Instance.Follow.ShouldFollow = _originalShouldFollow;
            FollowBotSettings.Instance.Loot.ShouldLoot = _originalShouldLoot;
        }

        /// <summary>
        /// Records a failure for an item. Returns true if the item has exceeded MaxRetries and is now ignored.
        /// </summary>
        private static bool RecordFailure(int itemId, string itemName, string reason)
        {
            if (!_itemFailCounts.ContainsKey(itemId))
                _itemFailCounts[itemId] = 0;

            _itemFailCounts[itemId]++;
            int count = _itemFailCounts[itemId];

            if (count >= MaxRetries)
            {
                GlobalLog.Error($"[UltimatumUnloaderTask] {itemName}: {reason} (attempt {count}/{MaxRetries}). Permanently ignoring.");
                return true;
            }

            GlobalLog.Warn($"[UltimatumUnloaderTask] {itemName}: {reason} (attempt {count}/{MaxRetries}). Will retry.");
            return false;
        }

        private static bool IsIgnored(int itemId)
        {
            return _itemFailCounts.ContainsKey(itemId) && _itemFailCounts[itemId] >= MaxRetries;
        }

        /// <summary>
        /// Finds the nearest eligible WorldItem to the bot that is within UltimatumItemRadius of the Ultimatum position.
        /// Optionally limits to items within maxBotDistance of the bot (for inner loop cluster sweeping).
        /// Excludes Gold, ignored items, and items allocated to other players.
        /// </summary>
        private static WorldItem FindNearestItem(Vector2i ultimatumPos, float maxBotDistance = 0f)
        {
            return LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
                .Where(i => i.HasAllocation
                    && !i.IsAllocatedToOther
                    && i.Item != null
                    && i.Item.Name != "Gold"
                    && !IsIgnored(i.Id)
                    && ultimatumPos.Distance(i.Position) <= UltimatumItemRadius
                    && (maxBotDistance <= 0f || i.Distance <= maxBotDistance))
                .OrderBy(i => i.Distance)
                .FirstOrDefault();
        }

        /// <summary>
        /// Reacquires a WorldItem by ID from the ObjectManager to get fresh pointers.
        /// </summary>
        private static WorldItem ReacquireItem(int id)
        {
            return LokiPoe.ObjectManager.GetObjectsByType<WorldItem>()
                .FirstOrDefault(i => i.Id == id);
        }

        /// <summary>
        /// Blocks until the inventory panel reaches the desired state, or times out.
        /// </summary>
        private static void WaitForInventory(bool opened, int timeoutMs = 2000)
        {
            var sw = Stopwatch.StartNew();
            while (LokiPoe.InGameState.InventoryUi.IsOpened != opened && sw.ElapsedMilliseconds < timeoutMs)
            {
                System.Threading.Thread.Sleep(50);
            }
        }

        /// <summary>
        /// Attempts to pick up a WorldItem using the Proven Space label sweep strategy.
        /// Returns the proven screen coordinate if successful, or null if the label could not be found.
        /// </summary>
        private static Vector2i? TryPickupItem(WorldItem target)
        {
            Vector2 coords;
            Vector2 size;
            bool hasLabel = WorldItem.GetClickableHighlightLabelDimensions(target, out coords, out size, false);

            if (!hasLabel)
            {
                RecordFailure(target.Id, target.Item?.Name ?? "Unknown", "No label found");
                System.Threading.Thread.Sleep(100);
                return null;
            }

            int[] sweepPoints = { 3, 4, 2, 5, 1, 6 };
            foreach (int i in sweepPoints)
            {
                var point = new Vector2i((int)(coords.X + (size.X / 7) * i), (int)(coords.Y + (size.Y / 2)));
                MouseManager.SetMousePosition(point, false);
                System.Threading.Thread.Sleep(100);

                if (GameController.Instance.Game.IngameState.FrameUnderCursor == target.Entity.Address)
                {
                    GlobalLog.Info($"[UltimatumUnloaderTask] Picking up {target.Item.Name} at ({point.X}, {point.Y})");
                    MouseManager.ClickLMB(point.X, point.Y);
                    return point;
                }
            }

            RecordFailure(target.Id, target.Item?.Name ?? "Unknown", "Label sweep failed");
            System.Threading.Thread.Sleep(100);
            return null;
        }

        /// <summary>
        /// Waits for an item to attach to the cursor after a pickup click.
        /// </summary>
        private static bool WaitForCursorAttach(int timeoutMs = 3000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                if (LokiPoe.InGameState.CursorItemOverlay.Item != null)
                    return true;
                System.Threading.Thread.Sleep(50);
            }
            return false;
        }

        /// <summary>
        /// Drops the item on the cursor at the given screen coordinate and waits for the cursor to clear.
        /// </summary>
        private static bool DropAtProvenSpace(Vector2i point)
        {
            GlobalLog.Info($"[UltimatumUnloaderTask] Dropping at ({point.X}, {point.Y})");
            System.Threading.Thread.Sleep(300);
            MouseManager.SetMousePosition(point, false);
            System.Threading.Thread.Sleep(100);
            MouseManager.ClickLMB(point.X, point.Y);

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < 3000)
            {
                if (LokiPoe.InGameState.CursorItemOverlay.Item == null)
                    return true;
                System.Threading.Thread.Sleep(50);
            }

            GlobalLog.Error("[UltimatumUnloaderTask] Item stuck on cursor after drop.");
            return false;
        }

        public async Task<bool> Run()
        {
            if (!_isEnabled || !_isInProgress)
                return false;

            if (!LokiPoe.IsInGame)
                return false;

            // Initial nuclear halt: kill any lingering movement/actions from other tasks
            await Coroutines.FinishCurrentAction(true);
            LokiPoe.ProcessHookManager.ClearAllKeyStates();

            // Locate the Ultimatum object — items must be near it to be eligible
            var ultimatum = LokiPoe.ObjectManager.Objects
                .OfType<UltimatumChallengeInteractable>()
                .FirstOrDefault();

            if (ultimatum == null)
            {
                GlobalLog.Error("[UltimatumUnloaderTask] No Ultimatum object found. Aborting.");
                RestoreSettings();
                _isInProgress = false;
                _isEnabled = false;
                return false;
            }

            var ultPos = ultimatum.Position;
            GlobalLog.Info($"[UltimatumUnloaderTask] Ultimatum position: {ultPos}. Filtering items within {UltimatumItemRadius} units.");

            // Ensure inventory is closed before we start
            if (LokiPoe.InGameState.InventoryUi.IsOpened)
            {
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.open_inventory_panel, true, false, false);
                WaitForInventory(false);
            }

            // ===== OUTER LOOP: Find clusters and move to them =====
            while (_isEnabled && _isInProgress)
            {
                // 1. Find nearest eligible item (must be within UltimatumItemRadius of the Ultimatum)
                var outerTarget = FindNearestItem(ultPos);
                if (outerTarget == null)
                {
                    GlobalLog.Info("[UltimatumUnloaderTask] Sweep complete. No items remaining.");
                    break;
                }

                GlobalLog.Info($"[UltimatumUnloaderTask] Outer target: {outerTarget.Item.Name} (Dist: {outerTarget.Distance:F0})");

                // 2. Smooth movement loop — travel to target without per-tick interrupts
                while (outerTarget.Distance > InteractionRange && _isEnabled && _isInProgress)
                {
                    if (!PlayerMoverManager.MoveTowards(outerTarget.Position))
                    {
                        RecordFailure(outerTarget.Id, outerTarget.Item.Name, "Path blocked");

                        outerTarget = null;
                        break;
                    }
                    System.Threading.Thread.Sleep(100);
                }

                // Path was blocked, try next target
                if (outerTarget == null)
                    continue;

                // 3. Arrived at cluster — halt and stabilize
                PlayerMoverManager.MoveTowards(LokiPoe.MyPosition);
                await Coroutines.FinishCurrentAction(true);
                LokiPoe.ProcessHookManager.ClearAllKeyStates();
                System.Threading.Thread.Sleep(500);

                // 4. Open inventory once for this cluster
                if (!LokiPoe.InGameState.InventoryUi.IsOpened)
                {
                    GlobalLog.Debug("[UltimatumUnloaderTask] Opening inventory for cluster.");
                    await Inventories.OpenInventory();
                    System.Threading.Thread.Sleep(900); // Wait for viewport shift
                    LokiPoe.ProcessHookManager.ClearAllKeyStates();
                }

                // 5. Hold highlight key once for the entire inner loop
                Keys k = LokiPoe.Input.Binding.highlight_combo.Key;
                Keys mod = LokiPoe.Input.Binding.highlight_combo.Modifier;
                LokiPoe.ProcessHookManager.SetKeyState(k, -32768, mod);
                System.Threading.Thread.Sleep(500); // Let labels render

                // ===== INNER LOOP: Sweep all items within cluster radius =====
                int clusterCount = 0;
                while (_isEnabled && _isInProgress)
                {
                    // Find nearest eligible item that's also within reach of the bot
                    var subTarget = FindNearestItem(ultPos, InnerSearchRadius);
                    if (subTarget == null)
                    {
                        GlobalLog.Info($"[UltimatumUnloaderTask] Cluster cleared. {clusterCount} items processed.");
                        break;
                    }

                    // Reacquire to ensure fresh data after viewport shift
                    subTarget = ReacquireItem(subTarget.Id);
                    if (subTarget == null || subTarget.Item == null)
                    {
                        GlobalLog.Debug("[UltimatumUnloaderTask] Sub-target lost on reacquire.");
                        continue;
                    }

                    // Pickup via proven space label sweep
                    var provenPoint = TryPickupItem(subTarget);
                    if (provenPoint == null)
                        continue;

                    // Wait for item to attach to cursor
                    if (!WaitForCursorAttach())
                    {
                        RecordFailure(subTarget.Id, subTarget.Item.Name, "Failed to reach cursor");
                        continue;
                    }

                    // Drop at the proven space
                    if (!DropAtProvenSpace(provenPoint.Value))
                    {
                        GlobalLog.Error("[UltimatumUnloaderTask] Drop failed. Aborting inner loop to avoid stuck state.");
                        break;
                    }

                    clusterCount++;
                    System.Threading.Thread.Sleep(300); // Inter-item delay for server re-sync of dropped item
                }

                // 6. Release highlight key
                LokiPoe.ProcessHookManager.SetKeyState(k, 0, mod);
                System.Threading.Thread.Sleep(100);

                // 7. Close inventory for this cluster
                if (LokiPoe.InGameState.InventoryUi.IsOpened)
                {
                    GlobalLog.Debug("[UltimatumUnloaderTask] Closing inventory after cluster.");
                    LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.open_inventory_panel, true, false, false);
                    WaitForInventory(false);
                }
            }

            // Final cleanup: clear any item stuck on cursor before handing control back
            if (LokiPoe.InGameState.CursorItemOverlay.Item != null)
            {
                GlobalLog.Warn("[UltimatumUnloaderTask] Item stuck on cursor at exit. Attempting to drop it.");

                // Ensure inventory is open so we have valid world space to drop into
                if (!LokiPoe.InGameState.InventoryUi.IsOpened)
                {
                    await Inventories.OpenInventory();
                    System.Threading.Thread.Sleep(900);
                }

                // Drop at character's screen position as a last resort
                int cx, cy;
                LokiPoe.ClientFunctions.WorldToScreen(LokiPoe.Me.InteractCenterWorld, out cx, out cy);
                var dropPoint = new Vector2i(cx, cy + 50); // Slightly below feet
                MouseManager.SetMousePosition(dropPoint, false);
                System.Threading.Thread.Sleep(200);
                MouseManager.ClickLMB(dropPoint.X, dropPoint.Y);

                var sw = Stopwatch.StartNew();
                while (sw.ElapsedMilliseconds < 3000)
                {
                    if (LokiPoe.InGameState.CursorItemOverlay.Item == null)
                        break;
                    System.Threading.Thread.Sleep(50);
                }

                if (LokiPoe.InGameState.CursorItemOverlay.Item != null)
                {
                    GlobalLog.Error("[UltimatumUnloaderTask] CRITICAL: Could not clear cursor. Bot may be stuck.");
                }
            }

            // Close inventory if still open
            if (LokiPoe.InGameState.InventoryUi.IsOpened)
            {
                LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.open_inventory_panel, true, false, false);
                WaitForInventory(false);
            }

            RestoreSettings();
            _isInProgress = false;
            _isEnabled = false;
            return true;
        }

        public void Start() { }
        public void Tick() { }
        public void Stop() { }
        public MessageResult Message(Message message)
        {
            if (message.Id == Events.Messages.AreaChanged)
            {
                GlobalLog.Debug("[UltimatumUnloaderTask] Area changed, resetting Unloader state.");
                RestoreSettings();
                _isEnabled = false;
                _isInProgress = false;
                _itemFailCounts.Clear();
                if (_delaySw.IsRunning) _delaySw.Stop();
                return MessageResult.Processed;
            }
            return MessageResult.Unprocessed;
        }
        public async Task<LogicResult> Logic(Logic logic) { return LogicResult.Unprovided; }
    }
}
