using System;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions.Positions;
using AreaTransition = DreamPoeBot.Loki.Game.Objects.AreaTransition;
using Portal = DreamPoeBot.Loki.Game.Objects.Portal;

namespace FollowBot.SimpleEXtensions
{
    public static class PlayerAction
    {
        public static async Task<bool> Interact(NetworkObject obj, Func<bool> success, string desc, int timeout = 3000)
        {
            if (obj == null)
            {
                GlobalLog.Error("[Interact] Object for interaction is null.");
                return false;
            }

            var name = obj.Name;
            GlobalLog.Debug($"[Interact] Now going to interact with \"{name}\".");

            await Coroutines.CloseBlockingWindows();
            await Coroutines.FinishCurrentAction();
            await Wait.LatencySleep();
            if (await Coroutines.InteractWith(obj, name == "Tane Octavius"))
            {
                if (name == "Tane Octavius")
                {
                    await Wait.For(() => LokiPoe.InGameState.SellUi.IsOpened, "Tane Sell Inventory", 100, timeout);
                    return false;
                }
                if (!await Wait.For(success, desc, 100, timeout))
                    return false;

                GlobalLog.Debug($"[Interact] \"{name}\" has been successfully interacted.");
                return true;
            }
            GlobalLog.Error($"[Interact] Fail to interact with \"{name}\".");
            await Wait.SleepSafe(300, 500);
            return false;
        }
        public static async Task<bool> Interact(NetworkObject obj)
        {
            //return await Interact(obj._entity);
            if (obj == null)
            {
                GlobalLog.Error("[Interact] Object for interaction is null.");
                return false;
            }

            var name = obj.Name;
            GlobalLog.Debug($"[Interact] Now going to interact with \"{name}\".");

            await Coroutines.CloseBlockingWindows();
            await Coroutines.FinishCurrentAction();
            await Wait.LatencySleep();

            if (await Coroutines.InteractWith(obj))
            {
                GlobalLog.Debug($"[Interact] \"{name}\" has been successfully interacted.");
                return true;
            }
            GlobalLog.Error($"[Interact] Fail to interact with \"{name}\".");
            await Wait.SleepSafe(100, 200);
            return false;
        }
        public static async Task<bool> Interact(NetworkObject obj, int attempts)
        {
            if (obj == null)
            {
                GlobalLog.Error("[Interact] Object for interaction is null.");
                return false;
            }

            var name = obj.Name;
            GlobalLog.Debug($"[Interact] Now going to interact with \"{name}\".");

            for (int i = 1; i <= attempts; i++)
            {
                if (!LokiPoe.IsInGame || LokiPoe.Me.IsDead)
                    break;

                await Coroutines.CloseBlockingWindows();
                await Coroutines.FinishCurrentAction();
                await Wait.LatencySleep();

                if (await Coroutines.InteractWith(obj))
                {
                    GlobalLog.Debug($"[Interact] \"{name}\" has been successfully interacted.");
                    return true;
                }
                GlobalLog.Error($"[Interact] Fail to interact with \"{name}\". Attempt: {i}/{attempts}.");
                await Wait.SleepSafe(100, 200);
            }
            return false;
        }

        public static async Task<bool> TakeTransition(AreaTransition transition, bool newInstance = false)
        {
            if (transition == null)
            {
                GlobalLog.Error("[TakeTransition] Transition object is null.");
                return false;
            }

            WalkablePosition pos;
            if (transition.Name == "Shrine of the Winds")
            {
                pos = transition.WalkablePosition(7, 10);
            }
            else if (transition.Name == "The Chamber of Sins Level 2")
            {
                pos = transition.WalkablePosition(2, 5);
            }
            else if (transition.Name == "The Crypt Level 1")
            {
                pos = transition.WalkablePosition(2, 5);
            }
            else if (transition.Name == "Tukohama's Keep")
            {
                pos = transition.WalkablePosition(2, 5);
            }
            else
            {
                pos = transition.WalkablePosition();
            }

            var type = transition.TransitionType;

            GlobalLog.Debug($"[TakeTransition] Now going to enter \"{pos.Name}\".");

            if (transition.Name == "Shrine of the Winds")
            {
                await pos.TryComeAtOnce(13);
            }
            else if (transition.Name == "Altar of Hunger")
            {
                await ComeToUnstuckPosition(new WalkablePosition("Unstuck Position", new Vector2i(1834, 3001), 2, 5), 6, pos);
            }
            else if (transition.Name == "The Chamber of Sins Level 2")
            {
                await pos.TryComeAtOnce(9);
            }
            else if (transition.Name == "The Crypt Level 1")
            {
                await pos.TryComeAtOnce(9);
            }
            else if (LokiPoe.CurrentWorldArea.Name == "The Sceptre of God" ||
                     LokiPoe.CurrentWorldArea.Name == "The Upper Sceptre of God")
            {
                if (transition.Name == "Tower Rooftop")
                {
                    await ComeToUnstuckPosition(new WalkablePosition("Unstuck Position", new Vector2i(2691, 423), 2, 5), 6, pos);
                }
                else
                    await pos.TryComeAtOnce(13);
            }
            else if (transition.Name == "Tukohama's Keep")
            {
                if (transition.Position == new Vector2i(1022, 350))
                {
                    await ComeToUnstuckPosition(new WalkablePosition("Unstuck Position", new Vector2i(1024, 315), 2, 5), 8, pos);
                }
                else
                    await ComeToUnstuckPosition(new WalkablePosition("Unstuck Position", new Vector2i(1232, 323), 2, 5), 8, pos);
            }
            else if (transition.Name == "The Quay")
            {
                await ComeToUnstuckPosition(
                    new WalkablePosition("Unstuck Position", new Vector2i(transition.Position.X + 10, transition.Position.Y - 10), 2, 6),
                    6, pos);
            }
            else
            {
                await pos.TryComeAtOnce();
            }
            await Coroutines.FinishCurrentAction();
            await Wait.SleepSafe(100);

            var hash = LokiPoe.LocalData.AreaHash;
            var myPos = LokiPoe.MyPosition;

            bool entered = newInstance
                ? await CreateNewInstance(transition)
                : await Interact(transition);

            if ((int)type == (int)TransitionType.Local)
            {
                if (!await Wait.For(() => LokiPoe.Me.HasAura("Grace Period") ||
                                          myPos.Distance(LokiPoe.MyPosition) > 15, "local transition", 500, 5000))
                {
                    GlobalLog.Error($"Grace: " +
                                    $"{LokiPoe.Me.HasAura("Grace Period")}, " +
                                    $"LastPos: {myPos}, CurrentPos: {LokiPoe.Me.Position}, " +
                                    $"Distance: {myPos.Distance(LokiPoe.MyPosition)}");
                    return false;
                }

                await Wait.SleepSafe(250);
            }
            else
            {
                if (!await Wait.ForAreaChange(hash))
                    return false;
            }
            GlobalLog.Debug($"[TakeTransition] \"{pos.Name}\" has been successfully entered.");
            return true;
        }
        private static async Task ComeToUnstuckPosition(WalkablePosition unstuck, int distance, WalkablePosition fallback)
        {
            if (await unstuck.TryComeAtOnce(distance))
                return;

            GlobalLog.Warn($"[TakeTransition] Unstuck position {unstuck} is unreachable. Falling back to {fallback}.");
            await fallback.TryComeAtOnce();
        }

        public static async Task<bool> CreateNewInstance(AreaTransition transition)
        {
            var name = transition.Name;
            if (!await Coroutines.InteractWith(transition, true))
            {
                GlobalLog.Error($"[CreateNewInstance] Fail to interact with \"{name}\" transition.");
                return false;
            }

            if (!await Wait.For(() => LokiPoe.InGameState.InstanceManagerUi.IsOpened, "instance manager opening"))
                return false;

            await Wait.ArtificialDelay();

            GlobalLog.Debug($"[CreateNewInstance] Creating new instance for \"{name}\".");

            var err = LokiPoe.InGameState.InstanceManagerUi.JoinNewInstance();
            if (err != LokiPoe.InGameState.JoinInstanceResult.None)
            {
                GlobalLog.Error($"[CreateNewInstance] Fail to create a new instance. Error: \"{err}\".");
                return false;
            }
            GlobalLog.Debug($"[CreateNewInstance] New instance for \"{name}\" has been successfully created.");
            return true;
        }
        private static Portal PortalInRangeOf(int range)
        {
            return LokiPoe.ObjectManager.Objects
                .Closest<Portal>(p => p.IsPlayerPortal() && p.Distance <= range && p.PathDistance() <= range + 3);
        }
        public static async Task<Portal> CreateTownPortal()
        {
            var portalSkill = LokiPoe.InGameState.SkillBarHud.Skills.FirstOrDefault(s => s.Name == "Portal" && s.IsOnSkillBar);
            if (portalSkill != null)
            {
                await Coroutines.FinishCurrentAction();
                await Wait.SleepSafe(100);
                var err = LokiPoe.InGameState.SkillBarHud.Use(portalSkill.Slot, false);
                if (err != LokiPoe.InGameState.UseResult.None)
                {
                    GlobalLog.Error($"[CreateTownPortal] Fail to cast portal skill. Error: \"{err}\".");
                    return null;
                }
                await Coroutines.FinishCurrentAction();
                await Wait.SleepSafe(100);
            }
            else
            {
                var portalScroll = Inventories.InventoryItems
                    .Where(i => i.Name == CurrencyNames.Portal)
                    .OrderBy(i => i.StackCount)
                    .FirstOrDefault();

                if (portalScroll == null)
                {
                    GlobalLog.Error("[CreateTownPortal] Out of portal scrolls.");
                    return null;
                }

                int itemId = portalScroll.LocalId;

                if (!await Inventories.OpenInventory())
                    return null;

                await Coroutines.FinishCurrentAction();
                await Wait.SleepSafe(100);

                var err = LokiPoe.InGameState.InventoryUi.InventoryControl_Main.UseItem(itemId);
                if (err != UseItemResult.None)
                {
                    GlobalLog.Error($"[CreateTownPortal] Fail to use a Portal Scroll. Error: \"{err}\".");
                    return null;
                }

                await Wait.ArtificialDelay();

                await Coroutines.CloseBlockingWindows();
            }

            Portal portal = null;
            await Wait.For(() => (portal = PortalInRangeOf(40)) != null, "portal spawning");
            return portal;
        }
        public static async Task EnableAlwaysHighlight()
        {
            if (LokiPoe.ConfigManager.IsAlwaysHighlightEnabled)
                return;

            GlobalLog.Info("[EnableAlwaysHighlight] Now enabling always highlight.");
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.highlight_toggle, true, false, false);
            await Wait.For(() => LokiPoe.ConfigManager.IsAlwaysHighlightEnabled, "EnableAlwaysHighlight", 10, 100);//.SleepSafe(30);
        }
        public static async Task DisableAlwaysHighlight()
        {
            if (!LokiPoe.ConfigManager.IsAlwaysHighlightEnabled)
                return;

            GlobalLog.Info("[DisableAlwaysHighlight] Now disabling always highlight.");
            LokiPoe.Input.SimulateKeyEvent(LokiPoe.Input.Binding.highlight_toggle, true, false, false);
            await Wait.For(() => !LokiPoe.ConfigManager.IsAlwaysHighlightEnabled, "DisableAlwaysHighlight", 10, 100);//.Sleep(30);
        }
    }
}
