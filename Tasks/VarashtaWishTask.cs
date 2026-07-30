using DreamPoeBot.BotFramework;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace FollowBot.Tasks
{
    /// <summary>
    /// Handles the Faridun league Varashta NPC wish mechanic.
    /// Detects Varashta with an exclamation mark, interacts with her,
    /// picks the best wish from a priority list, and confirms.
    /// </summary>
    class VarashtaWishTask : ITask
    {
        #region Constants

        private const string VarashtaMetadata = "Metadata/NPC/League/Faridun/Kubera/Varashta";
        private const int MaxVarashtaDistance = 50; // Don't run across the map to find her

        // Wish priority is now configurable via WishSettings.

        #endregion

        #region State

        private static Stopwatch _interactCooldown = new Stopwatch();

        #endregion

        #region ITask Properties

        public string Name { get { return "VarashtaWish"; } }
        public string Description { get { return "Interacts with Varashta and picks a wish."; } }
        public string Author { get { return "Rushtothesun"; } }
        public string Version { get { return "1.1.0"; } }

        #endregion

        #region ITask Lifecycle

        public void Start() { _interactCooldown.Reset(); }
        public void Stop() { _interactCooldown.Reset(); }
        public void Tick() { }

        #endregion

        #region Main Logic

        public async Task<bool> Run()
        {
            if (!LeagueFeatureFlags.MirageEnabled)
                return false;

            if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead)
                return false;

            if (!FollowBotSettings.Instance.Wish.AutoWish)
                return false;

            // If the wish panel is already open, handle it
            if (LokiPoe.InGameState.WishesUi.IsOpened)
            {
                await HandleWishPanel();
                return true;
            }

            // Find Varashta with the exclamation mark (HasNpcFloatingIcon)
            var varashta = LokiPoe.ObjectManager.Objects
                .OfType<Npc>()
                .FirstOrDefault(n => n.Metadata == VarashtaMetadata
                    && n.IsTargetable
                    && n.HasNpcFloatingIcon);

            if (varashta == null)
                return false;

            // Don't run across the map — only interact when already nearby
            if (varashta.Distance > MaxVarashtaDistance)
                return false;

            // Cooldown to avoid spamming interaction attempts
            if (_interactCooldown.IsRunning && _interactCooldown.ElapsedMilliseconds < 2000)
                return false;

            GlobalLog.Debug($"[{Name}] Found Varashta at distance {(int)varashta.Distance} with quest icon.");

            // Move closer if needed
            if (varashta.Distance > 20)
            {
                var walkablePos = ExilePather.FastWalkablePositionFor(varashta.Position, 20);
                Move.Towards(walkablePos, "moving to Varashta");
                return true;
            }

            // Interact
            GlobalLog.Debug($"[{Name}] Interacting with Varashta.");
            await PlayerAction.Interact(varashta);
            _interactCooldown.Restart();
            await Wait.SleepSafe(800, 1200);

            return true;
        }

        #endregion

        #region Wish Panel Logic

        private async System.Threading.Tasks.Task HandleWishPanel()
        {
            GlobalLog.Debug($"[{Name}] Wish panel is open. Selecting best wish...");

            var wishes = LokiPoe.InGameState.WishesUi.Wishes;
            if (wishes == null || wishes.Count == 0)
            {
                GlobalLog.Error($"[{Name}] No wishes found in the panel.");
                return;
            }

            // Find the best wish based on priority
            var wishPriority = FollowBotSettings.Instance.Wish.WishPriority;
            LokiPoe.InGameState.WishesUi.Wish bestWish = null;
            int bestPriority = int.MaxValue;

            foreach (var wish in wishes)
            {
                var title = wish.Title;
                GlobalLog.Debug($"[{Name}]   Option: {title}");

                for (int p = 0; p < wishPriority.Count; p++)
                {
                    if (title == wishPriority[p] && p < bestPriority)
                    {
                        bestPriority = p;
                        bestWish = wish;
                        break;
                    }
                }
            }

            // If no priority match found, default to the first one
            if (bestWish == null)
            {
                bestWish = wishes[0];
                GlobalLog.Info($"[{Name}] No priority match found. Defaulting to: {bestWish.Title}");
            }
            else
            {
                GlobalLog.Info($"[{Name}] Selected high priority wish: {bestWish.Title} (Priority {bestPriority})");
            }

            // Select the wish
            if (!bestWish.IsSelected)
            {
                GlobalLog.Debug($"[{Name}] Selecting {bestWish.Title}.");
                bestWish.Select();
                await Wait.SleepSafe(400, 700);
            }

            // Click confirm
            GlobalLog.Debug($"[{Name}] Clicking Confirm.");
            LokiPoe.InGameState.WishesUi.Confirm();
            await Wait.SleepSafe(500, 800);
        }

        #endregion

        #region ITask Interface

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        #endregion
    }
}
