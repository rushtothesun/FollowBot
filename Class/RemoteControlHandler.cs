// Author: Rushtothesun
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using FollowBot.SimpleEXtensions;
using FollowBot.Settings;
using FollowBot.Tasks;
using Message = DreamPoeBot.Loki.Bot.Message;

namespace FollowBot.Class
{
    /// <summary>
    /// Handles RemoteControl plugin commands by flipping the same state flags as ChatParser,
    /// but received over TCP instead of in-game chat.
    /// </summary>
    public static class RemoteControlHandler
    {
        public static bool HandleMessage(string id, Message message)
        {
            var follow = FollowBotSettings.Instance.Follow;
            var combat = FollowBotSettings.Instance.Combat;
            var loot = FollowBotSettings.Instance.Loot;
            var stash = FollowBotSettings.Instance.Stash;

            switch (id)
            {
                case "RC_start_follow":
                    follow.ShouldFollow = true;
                    GlobalLog.Info("[FollowBot] RC: StartFollow");
                    return true;
                case "RC_stop_follow":
                    follow.ShouldFollow = false;
                    GlobalLog.Info("[FollowBot] RC: StopFollow");
                    return true;
                case "RC_start_attack":
                    combat.ShouldKill = true;
                    GlobalLog.Info("[FollowBot] RC: StartAttack");
                    return true;
                case "RC_stop_attack":
                    combat.ShouldKill = false;
                    GlobalLog.Info("[FollowBot] RC: StopAttack");
                    return true;
                case "RC_start_loot":
                    loot.ShouldLoot = true;
                    GlobalLog.Info("[FollowBot] RC: StartLoot");
                    return true;
                case "RC_stop_loot":
                    loot.ShouldLoot = false;
                    GlobalLog.Info("[FollowBot] RC: StopLoot");
                    return true;
                case "RC_start_portal":
                    follow.DontPortOutofMap = false;
                    GlobalLog.Info("[FollowBot] RC: StartPortal (auto-TP enabled)");
                    return true;
                case "RC_stop_portal":
                    follow.DontPortOutofMap = true;
                    GlobalLog.Info("[FollowBot] RC: StopPortal (auto-TP disabled)");
                    return true;
                case "RC_teleport":
                    DefenseAndFlaskTask.ShouldTeleport = true;
                    GlobalLog.Info("[FollowBot] RC: Teleport");
                    return true;
                case "RC_open_portal":
                    DefenseAndFlaskTask.ShouldOpenPortal = true;
                    GlobalLog.Info("[FollowBot] RC: OpenPortal");
                    return true;
                case "RC_enter_portal":
                    UltimatumTask.ShouldEnterPortal = true;
                    GlobalLog.Info("[FollowBot] RC: EnterPortal");
                    return true;
                case "RC_stash":
                    StashTask.ShouldDepositFromChat = true;
                    GlobalLog.Info("[FollowBot] RC: Stash");
                    return true;
                case "RC_stash_currency":
                    StashTask.ShouldDepositCurrencyOnly = true;
                    GlobalLog.Info("[FollowBot] RC: StashCurrency");
                    return true;
                case "RC_new_instance":
                    FollowTask.ShouldCreateNewInstance = true;
                    GlobalLog.Info("[FollowBot] RC: NewInstance");
                    return true;
                case "RC_follow_town_on":
                    follow.FollowInTown = true;
                    GlobalLog.Info("[FollowBot] RC: FollowInTown ON");
                    return true;
                case "RC_follow_town_off":
                    follow.FollowInTown = false;
                    GlobalLog.Info("[FollowBot] RC: FollowInTown OFF");
                    return true;
                case "RC_follow_hideout_on":
                    follow.FollowInHideout = true;
                    GlobalLog.Info("[FollowBot] RC: FollowInHideout ON");
                    return true;
                case "RC_follow_hideout_off":
                    follow.FollowInHideout = false;
                    GlobalLog.Info("[FollowBot] RC: FollowInHideout OFF");
                    return true;
                case "RC_follow_heist_on":
                    follow.FollowInHeistHub = true;
                    GlobalLog.Info("[FollowBot] RC: FollowInHeistHub ON");
                    return true;
                case "RC_follow_heist_off":
                    follow.FollowInHeistHub = false;
                    GlobalLog.Info("[FollowBot] RC: FollowInHeistHub OFF");
                    return true;
                case "RC_auto_deposit_on":
                    stash.AutoDepositOnMapExit = true;
                    GlobalLog.Info("[FollowBot] RC: AutoDeposit ON");
                    return true;
                case "RC_auto_deposit_off":
                    stash.AutoDepositOnMapExit = false;
                    GlobalLog.Info("[FollowBot] RC: AutoDeposit OFF");
                    return true;
                case "RC_use_guild_stash":
                    stash.UseGuildStash = true;
                    GlobalLog.Info("[FollowBot] RC: UseGuildStash");
                    return true;
                case "RC_use_regular_stash":
                    stash.UseGuildStash = false;
                    GlobalLog.Info("[FollowBot] RC: UseRegularStash");
                    return true;
                case "RC_ult_portal_on":
                    loot.ShouldLootUltimatum = true;
                    GlobalLog.Info("[FollowBot] RC: Portal After Ultimatum ON");
                    return true;
                case "RC_ult_portal_off":
                    loot.ShouldLootUltimatum = false;
                    GlobalLog.Info("[FollowBot] RC: Portal After Ultimatum OFF");
                    return true;
                case "RC_set_ult_timer":
                    int timerVal;
                    if (message.TryGetInput<int>("value", out timerVal))
                    {
                        loot.UltimatumLootTimer = timerVal;
                        GlobalLog.Info($"[FollowBot] RC: UltimatumLootTimer = {timerVal}");
                    }
                    return true;
                case "RC_allocate":
                    AutoAllocatePassiveTask.ForceTrigger();
                    GlobalLog.Info("[FollowBot] RC: Allocate");
                    return true;
                case "RC_unloader":
                    UltimatumUnloaderTask.TriggerUnloader();
                    GlobalLog.Info("[FollowBot] RC: Unloader triggered");
                    return true;
                case "RC_set_unloader_delay":
                    int delayVal;
                    if (message.TryGetInput<int>("value", out delayVal))
                    {
                        loot.UltimatumUnloaderDelay = delayVal;
                        GlobalLog.Info($"[FollowBot] RC: UltimatumUnloaderDelay = {delayVal}");
                    }
                    return true;
                case "RC_open_doors_on":
                    follow.OpenDoors = true;
                    GlobalLog.Info("[FollowBot] RC: OpenDoors ON");
                    return true;
                case "RC_open_doors_off":
                    follow.OpenDoors = false;
                    GlobalLog.Info("[FollowBot] RC: OpenDoors OFF");
                    return true;
                case "RC_open_chests_on":
                    loot.ShouldOpenChests = true;
                    GlobalLog.Info("[FollowBot] RC: OpenChests ON");
                    return true;
                case "RC_open_chests_off":
                    loot.ShouldOpenChests = false;
                    GlobalLog.Info("[FollowBot] RC: OpenChests OFF");
                    return true;
                case "RC_buy_trade_item":
                    int buyX, buyY;
                    string buyFullName, buyBaseName, buyCost;
                    if (message.TryGetInput<int>("x", out buyX) &&
                        message.TryGetInput<int>("y", out buyY) &&
                        message.TryGetInput<string>("fullName", out buyFullName) &&
                        message.TryGetInput<string>("baseName", out buyBaseName) &&
                        message.TryGetInput<string>("cost", out buyCost))
                    {
                        if (TradeBuyoutTask.HasPendingBuyout)
                        {
                            GlobalLog.Warn("[FollowBot] RC: BuyTradeItem ignored — a buyout is already in progress.");
                            return true;
                        }
                        if (!LokiPoe.Me.IsInTown && !LokiPoe.Me.IsInHideout)
                        {
                            GlobalLog.Error("[FollowBot] RC: BuyTradeItem rejected — bot is not in a town or hideout.");
                            return true;
                        }
                        if (buyX < 0 || buyY < 0)
                        {
                            GlobalLog.Error($"[FollowBot] RC: BuyTradeItem malformed coordinates ({buyX},{buyY}), ignoring.");
                            return true;
                        }
                        if (string.IsNullOrEmpty(buyBaseName))
                        {
                            GlobalLog.Error("[FollowBot] RC: BuyTradeItem has no baseName, rejecting.");
                            return true;
                        }
                        TradeBuyoutTask.TargetX = buyX;
                        TradeBuyoutTask.TargetY = buyY;
                        TradeBuyoutTask.ExpectedFullName = buyFullName;
                        TradeBuyoutTask.ExpectedBaseName = buyBaseName;
                        TradeBuyoutTask.ExpectedCost = buyCost;
                        TradeBuyoutTask.HasPendingBuyout = true;
                        GlobalLog.Info($"[FollowBot] RC: BuyTradeItem at ({buyX},{buyY}) fullName='{buyFullName}' baseName='{buyBaseName}' cost='{buyCost}'");
                    }
                    else
                    {
                        GlobalLog.Error("[FollowBot] RC: BuyTradeItem message missing required inputs.");
                    }
                    return true;
                case "RC_divine_font_on":
                    FollowBotSettings.Instance.Lab.EnableDivineFontHandling = true;
                    GlobalLog.Info("[FollowBot] RC: DivineFontHandling ON");
                    return true;
                case "RC_divine_font_off":
                    FollowBotSettings.Instance.Lab.EnableDivineFontHandling = false;
                    GlobalLog.Info("[FollowBot] RC: DivineFontHandling OFF");
                    return true;
                case "RC_set_lab_gem_color":
                    string colorVal;
                    if (message.TryGetInput<string>("value", out colorVal))
                    {
                        Settings.LabSettings.GemColor parsedColor;
                        if (System.Enum.TryParse(colorVal, true, out parsedColor))
                        {
                            FollowBotSettings.Instance.Lab.Color = parsedColor;
                            GlobalLog.Info($"[FollowBot] RC: Lab GemColor = {parsedColor}");
                        }
                        else
                        {
                            GlobalLog.Warn($"[FollowBot] RC: Unknown GemColor value '{colorVal}'");
                        }
                    }
                    return true;
                case "RC_set_lab_transfigure_gem":
                    string gemVal;
                    if (message.TryGetInput<string>("value", out gemVal))
                    {
                        var options = FollowBotSettings.Instance.Lab.DivineFontOptions;
                        if (options != null)
                        {
                            var specificOption = System.Linq.Enumerable.FirstOrDefault(options,
                                o => o.Type == DivineFontOptionType.TransformSpecificGem);
                            if (specificOption != null)
                            {
                                specificOption.GemName = gemVal;
                                GlobalLog.Info($"[FollowBot] RC: Lab TransfigureGem = '{gemVal}'");
                            }
                            else
                            {
                                GlobalLog.Warn("[FollowBot] RC: TransformSpecificGem option not found in DivineFontOptions.");
                            }
                        }
                    }
                    return true;
                default:
                    return false;
            }
        }
    }
}
