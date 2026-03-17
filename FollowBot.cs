using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Coroutine;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.NativeWrappers;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.Class;
using FollowBot.SimpleEXtensions;
using FollowBot.SimpleEXtensions.CommonTasks;
using FollowBot.SimpleEXtensions.Global;
using FollowBot.Tasks;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.LinkLabel;
using Message = DreamPoeBot.Loki.Bot.Message;
using UserControl = System.Windows.Controls.UserControl;

namespace FollowBot
{
    public class FollowBot : IBot
    {

        private FollowBotGui _gui;
        private Coroutine _coroutine;

        private readonly TaskManager _taskManager = new TaskManager();
        private readonly AutoLoginTask _autoLoginTask = new AutoLoginTask();
        internal static bool IsOnRun;
        public static Stopwatch RequestPartySw = Stopwatch.StartNew();
        private OverlayWindow _overlay = new OverlayWindow(LokiPoe.ClientWindowHandle);
        private ChatParser _chatParser = new ChatParser();
        private Stopwatch _chatSw = Stopwatch.StartNew();

        private static int _lastBoundMoveSkillSlot = -1;
        internal static int LastBoundMoveSkillSlot
        {
            get
            {
                if (_lastBoundMoveSkillSlot == -1)
                    _lastBoundMoveSkillSlot = LokiPoe.InGameState.SkillBarHud.LastBoundMoveSkill.Slot;
                return _lastBoundMoveSkillSlot;
            }
        }
        private static Keys _lastBoundMoveSkillKey = Keys.Clear;
        internal static Keys LastBoundMoveSkillKey
        {
            get
            {
                if (_lastBoundMoveSkillKey == Keys.Clear)
                    _lastBoundMoveSkillKey = LokiPoe.InGameState.SkillBarHud.LastBoundMoveSkill.BoundKeys.Last();
                return _lastBoundMoveSkillKey;
            }
        }

        internal static PartyMember _leaderPartyEntry => LokiPoe.InstanceInfo.PartyMembers.FirstOrDefault(x => x.MemberStatus == PartyStatus.PartyLeader);
        private static Player _leader;

        public static Player Leader
        {
            get
            {
                var leaderPartyEntry = _leaderPartyEntry;
                if (leaderPartyEntry?.PlayerEntry?.IsOnline != true)
                {
                    _leader = null;
                    return null;
                }

                var leaderName = leaderPartyEntry.PlayerEntry.Name;
                if (string.IsNullOrEmpty(leaderName) || leaderName == LokiPoe.Me.Name)
                {
                    _leader = null;
                    return null;
                }

                if (!LokiPoe.InGameState.PartyHud.IsInSameZone(leaderName))
                {
                    _leader = null;
                    return null;
                }

                if (_leader == null)
                {
                    //_leader = LokiPoe.ObjectManager.GetObjectsByType<Player>().FirstOrDefault(x => x.Name == leaderName);
                    var playersOfClass = LokiPoe.ObjectManager.GetObjectsByMetadatas(PlayerMetadataList).ToList();
                    var leaderPlayer = playersOfClass.FirstOrDefault(x => x.Name == leaderName);
                    _leader = leaderPlayer as Player;

                    if (_leader == null)
                    {
                        _leader = LokiPoe.ObjectManager.GetObjectsByType<Player>()
                            .FirstOrDefault(x => x.Name == leaderName);
                    }
                }
                return _leader;
            }
            set => _leader = value;
        }
        public static string[] PlayerMetadataList =
        {
            "Metadata/Characters/Dex/Dex", "Metadata/Characters/Int/Int", "Metadata/Characters/Str/Str",
                            "Metadata/Characters/StrDex/StrDex", "Metadata/Characters/StrInt/StrInt",
                            "Metadata/Characters/DexInt/DexInt",
                            "Metadata/Characters/StrDexInt/StrDexInt"
        };


        public void Start()
        {
            _lastBoundMoveSkillSlot = -1;
            _lastBoundMoveSkillKey = Keys.Clear;

            FollowBotSettings.Instance.PropertyChanged += OnSettingsPropertyChanged;

            UpdatePathfinderSettings();

            ItemEvaluator.Instance = DefaultItemEvaluator.Instance;
            Explorer.CurrentDelegate = user => CombatAreaCache.Current.Explorer.BasicExplorer;

            ComplexExplorer.ResetSettingsProviders();
            ComplexExplorer.AddSettingsProvider("FollowBot", MapBotExploration, ProviderPriority.Low);

            // Since this bot will be performing client actions, we need to enable the process hook manager.
            LokiPoe.ProcessHookManager.Enable();

            _coroutine = null;

            // These require in-game state — defer if starting from login screen
            if (LokiPoe.IsInGame)
            {
                // Cache all bound keys.
                LokiPoe.Input.Binding.Update();

                ExilePather.BlockLockedDoors = FeatureEnum.Disabled;
                ExilePather.BlockLockedTempleDoors = FeatureEnum.Disabled;
                ExilePather.BlockTrialOfAscendancy = FeatureEnum.Disabled;

                ExilePather.Reload();
            }

            // Reset the default MsBetweenTicks on start.
            GlobalLog.Debug($"[Start] MsBetweenTicks: {BotManager.MsBetweenTicks}.");

            _taskManager.Reset();

            AddTasks();

            Events.Start();
            PluginManager.Start();
            RoutineManager.Start();
            _taskManager.Start();
            _autoLoginTask.Start();

            foreach (var plugin in PluginManager.EnabledPlugins)
            {
                GlobalLog.Debug($"[Start] The plugin {plugin.Name} is enabled.");
            }

            GlobalLog.Debug($"[Start] PlayerMover.Instance: {PlayerMoverManager.Current.GetType()}.");

            //if (ExilePather.BlockTrialOfAscendancy == FeatureEnum.Unset)
            //{
            //    //no need for this, map trials are in separate areas
            //    ExilePather.BlockTrialOfAscendancy = FeatureEnum.Enabled;
            //}
        }

        public void Tick()
        {
            if (_coroutine == null)
            {
                _coroutine = new Coroutine(() => MainCoroutine());
            }

            if (LokiPoe.IsInGame)
                ExilePather.Reload();

            Events.Tick();
            CombatAreaCache.Tick();
            _taskManager.Tick();
            PluginManager.Tick();
            RoutineManager.Tick();

            if (_chatSw.ElapsedMilliseconds > 250)
            {
                _chatParser.Update();
                _chatSw.Restart();
            }
            // Check to see if the coroutine is finished. If it is, stop the bot.
            if (_coroutine.IsFinished)
            {
                GlobalLog.Debug($"The bot coroutine has finished in a state of {_coroutine.Status}");
                BotManager.Stop();
                return;
            }

            try
            {
                _coroutine.Resume();
            }
            catch
            {
                var c = _coroutine;
                _coroutine = null;
                c.Dispose();
                throw;
            }
        }

        public void Stop()
        {
            FollowBotSettings.Instance.PropertyChanged -= OnSettingsPropertyChanged;
            _taskManager.Stop();
            _autoLoginTask.Stop();
            PluginManager.Stop();
            RoutineManager.Stop();

            // When the bot is stopped, we want to remove the process hook manager.
            LokiPoe.ProcessHookManager.Disable();

            // Cleanup the coroutine.
            if (_coroutine != null)
            {
                _coroutine.Dispose();
                _coroutine = null;
            }
        }

        private async Task MainCoroutine()
        {
            while (true)
            {
                if (LokiPoe.IsInLoginScreen || LokiPoe.IsInCharacterSelectionScreen)
                {
                    // Try plugin hooks first
                    var hookName = LokiPoe.IsInLoginScreen ? "hook_login_screen" : "hook_character_selection";
                    var logic = new Logic(hookName, this);
                    var handled = false;
                    foreach (var plugin in PluginManager.EnabledPlugins)
                    {
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                        {
                            handled = true;
                            break;
                        }
                    }

                    // If no plugin handled it, use built-in auto-login
                    if (!handled)
                    {
                        await _autoLoginTask.Run();
                    }
                }
                else if (LokiPoe.IsInGame)
                {
                    // To make things consistent, we once again allow user coorutine logic to preempt the bot base coroutine logic.
                    // This was supported to a degree in 2.6, and in general with our bot bases. Technically, this probably should
                    // be at the top of the while loop, but since the bot bases offload two sets of logic to plugins this way, this
                    // hook is being placed here.
                    var hooked = false;
                    var logic = new Logic("hook_ingame", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                    {
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                        {
                            hooked = true;
                            break;
                        }
                    }

                    if (!hooked)
                    {
                        // Wait for game pause
                        if (LokiPoe.InstanceInfo.IsGamePaused)
                        {
                            GlobalLog.Debug("Waiting for game pause");
                        }
                        // Resurrect character if it is dead
                        else if (LokiPoe.Me.IsDead && World.CurrentArea.Id != "HallsOfTheDead_League")
                        {
                            await ResurrectionLogic.Execute();
                        }
                        // What the bot does now is up to the registered tasks.
                        else
                        {
                            await _taskManager.Run(TaskGroup.Enabled, RunBehavior.UntilHandled);
                        }
                    }
                }
                else
                {
                    // Most likely in a loading screen, which will cause us to block on the executor, 
                    // but just in case we hit something else that would cause us to execute...
                    await Wait.SleepSafe(1000);
                    continue;
                }

                // End of the tick.
                await Coroutine.Yield();
            }
            // ReSharper disable once FunctionNeverReturns
        }

        public MessageResult Message(Message message)
        {
            var handled = false;
            var id = message.Id;

            if (id == BotStructure.GetTaskManagerMessage)
            {
                message.AddOutput(this, _taskManager);
                handled = true;
            }
            else if (id == Messages.GetIsOnRun)
            {
                message.AddOutput(this, IsOnRun);
                handled = true;
            }
            else if (id == Messages.SetIsOnRun)
            {
                var value = message.GetInput<bool>();
                GlobalLog.Info($"[FollowBot] SetIsOnRun: {value}");
                IsOnRun = value;
                handled = true;
            }
            else if (message.Id == Events.Messages.AreaChanged)
            {
                Leader = null;
                UpdatePathfinderSettings();
                handled = true;
            }
            // === RemoteControl message handlers ===
            else
            {
                handled = HandleRemoteControlMessage(id, message) || handled;
            }

            Events.FireEventsFromMessage(message);

            var res = _taskManager.SendMessage(TaskGroup.Enabled, message);
            if (res == MessageResult.Processed)
                handled = true;

            return handled ? MessageResult.Processed : MessageResult.Unprocessed;
        }

        public async Task<LogicResult> Logic(Logic logic)
        {
            return await _taskManager.ProvideLogic(TaskGroup.Enabled, RunBehavior.UntilHandled, logic);
        }

        public TaskManager GetTaskManager()
        {
            return _taskManager;
        }

        public void Initialize()
        {
            BotManager.OnBotChanged += BotManagerOnOnBotChanged;
            GameOverlay.TimerService.EnableHighPrecisionTimers();
            _overlay.Start();
        }

        public void Deinitialize()
        {
            BotManager.OnBotChanged -= BotManagerOnOnBotChanged;
        }

        private void BotManagerOnOnBotChanged(object sender, BotChangedEventArgs botChangedEventArgs)
        {
            if (botChangedEventArgs.New == this)
            {
                ItemEvaluator.Instance = DefaultItemEvaluator.Instance;
            }
        }

        private void AddTasks()
        {

            _taskManager.Add(new ClearCursorTask());
            _taskManager.Add(new JoinPartyTask());
            _taskManager.Add(new DivineFontTask());
            _taskManager.Add(new TrialPickerTask());
            _taskManager.Add(new TradeTask());
            _taskManager.Add(new StashTask());
            _taskManager.Add(new UltimatumTask());
            _taskManager.Add(new UltimatumUnloaderTask());
            _taskManager.Add(new VarashtaWishTask());
            _taskManager.Add(new QuestInteractionTask());
            _taskManager.Add(new DefenseAndFlaskTask());
            _taskManager.Add(new CustomSkillsTask());
            _taskManager.Add(new AsyncCustomSkillsTask());
            _taskManager.Add(new LootItemTask());
            _taskManager.Add(new PreCombatFollowTask());
            _taskManager.Add(new CombatTask(50));
            _taskManager.Add(new PostCombatHookTask());
            _taskManager.Add(new LevelGemsTask());
            _taskManager.Add(new AutoAllocatePassiveTask());
            _taskManager.Add(new CombatTask(-1));
            _taskManager.Add(new CastAuraTask());
            _taskManager.Add(new TravelToPartyZoneTask());
            _taskManager.Add(new FollowTask());
            _taskManager.Add(new FallbackTask());
        }

        private static ExplorationSettings MapBotExploration()
        {
            if (!World.CurrentArea.IsMap)
                return new ExplorationSettings();

            OnNewMapEnter();

            return new ExplorationSettings(tileSeenRadius: TileSeenRadius);
        }

        private static void OnNewMapEnter()
        {
            var areaName = World.CurrentArea.Name;
            GlobalLog.Info($"[FollowBot] New map has been entered: {areaName}.");
            IsOnRun = true;
            Utility.BroadcastMessage(null, Messages.NewMapEntered, areaName);
        }

        private static int TileSeenRadius
        {
            get
            {
                if (TileSeenDict.TryGetValue(World.CurrentArea.Name, out int radius))
                    return radius;

                return ExplorationSettings.DefaultTileSeenRadius;
            }
        }

        private static readonly Dictionary<string, int> TileSeenDict = new Dictionary<string, int>
        {
            [MapNames.MaoKun] = 3,
            [MapNames.Arena] = 3,
            [MapNames.CastleRuins] = 3,
            [MapNames.UndergroundRiver] = 3,
            [MapNames.TropicalIsland] = 3,
            [MapNames.Beach] = 5,
            [MapNames.Strand] = 5,
            [MapNames.Port] = 5,
            [MapNames.Alleyways] = 5,
            [MapNames.Phantasmagoria] = 5,
            [MapNames.Wharf] = 5,
            [MapNames.Cemetery] = 5,
            [MapNames.MineralPools] = 5,
            [MapNames.Temple] = 5,
            [MapNames.Malformation] = 5,
        };

        public static class Messages
        {
            public const string NewMapEntered = "MB_new_map_entered_event";
            public const string MapFinished = "MB_map_finished_event";
            public const string MapTrialEntered = "MB_map_trial_entered_event";
            public const string GetIsOnRun = "MB_get_is_on_run";
            public const string SetIsOnRun = "MB_set_is_on_run";

            // RemoteControl plugin message IDs
            public const string RcStartFollow = "RC_start_follow";
            public const string RcStopFollow = "RC_stop_follow";
            public const string RcStartAttack = "RC_start_attack";
            public const string RcStopAttack = "RC_stop_attack";
            public const string RcStartLoot = "RC_start_loot";
            public const string RcStopLoot = "RC_stop_loot";
            public const string RcStartPortal = "RC_start_portal";
            public const string RcStopPortal = "RC_stop_portal";
            public const string RcTeleport = "RC_teleport";
            public const string RcOpenPortal = "RC_open_portal";
            public const string RcEnterPortal = "RC_enter_portal";
            public const string RcStash = "RC_stash";
            public const string RcNewInstance = "RC_new_instance";
            public const string RcFollowTownOn = "RC_follow_town_on";
            public const string RcFollowTownOff = "RC_follow_town_off";
            public const string RcFollowHideoutOn = "RC_follow_hideout_on";
            public const string RcFollowHideoutOff = "RC_follow_hideout_off";
            public const string RcFollowHeistOn = "RC_follow_heist_on";
            public const string RcFollowHeistOff = "RC_follow_heist_off";
            public const string RcAutoDepositOn = "RC_auto_deposit_on";
            public const string RcAutoDepositOff = "RC_auto_deposit_off";
            public const string RcUseGuildStash = "RC_use_guild_stash";
            public const string RcUseRegularStash = "RC_use_regular_stash";
            public const string RcUltPortalOn = "RC_ult_portal_on";
            public const string RcUltPortalOff = "RC_ult_portal_off";
            public const string RcSetUltTimer = "RC_set_ult_timer";
        }

        /// <summary>
        /// Handles RemoteControl plugin commands by flipping the same static flags as ChatParser.
        /// Returns true if the message was handled.
        /// </summary>
        private bool HandleRemoteControlMessage(string id, Message message)
        {
            var follow = FollowBotSettings.Instance.Follow;
            var combat = FollowBotSettings.Instance.Combat;
            var loot = FollowBotSettings.Instance.Loot;
            var stash = FollowBotSettings.Instance.Stash;

            switch (id)
            {
                case Messages.RcStartFollow:
                    follow.ShouldFollow = true;
                    GlobalLog.Info("[FollowBot] RC: StartFollow");
                    return true;
                case Messages.RcStopFollow:
                    follow.ShouldFollow = false;
                    GlobalLog.Info("[FollowBot] RC: StopFollow");
                    return true;
                case Messages.RcStartAttack:
                    combat.ShouldKill = true;
                    GlobalLog.Info("[FollowBot] RC: StartAttack");
                    return true;
                case Messages.RcStopAttack:
                    combat.ShouldKill = false;
                    GlobalLog.Info("[FollowBot] RC: StopAttack");
                    return true;
                case Messages.RcStartLoot:
                    loot.ShouldLoot = true;
                    GlobalLog.Info("[FollowBot] RC: StartLoot");
                    return true;
                case Messages.RcStopLoot:
                    loot.ShouldLoot = false;
                    GlobalLog.Info("[FollowBot] RC: StopLoot");
                    return true;
                case Messages.RcStartPortal:
                    follow.DontPortOutofMap = false;
                    GlobalLog.Info("[FollowBot] RC: StartPortal (auto-TP enabled)");
                    return true;
                case Messages.RcStopPortal:
                    follow.DontPortOutofMap = true;
                    GlobalLog.Info("[FollowBot] RC: StopPortal (auto-TP disabled)");
                    return true;
                case Messages.RcTeleport:
                    Tasks.DefenseAndFlaskTask.ShouldTeleport = true;
                    GlobalLog.Info("[FollowBot] RC: Teleport");
                    return true;
                case Messages.RcOpenPortal:
                    Tasks.DefenseAndFlaskTask.ShouldOpenPortal = true;
                    GlobalLog.Info("[FollowBot] RC: OpenPortal");
                    return true;
                case Messages.RcEnterPortal:
                    Tasks.UltimatumTask.ShouldEnterPortal = true;
                    GlobalLog.Info("[FollowBot] RC: EnterPortal");
                    return true;
                case Messages.RcStash:
                    Tasks.StashTask.ShouldDepositFromChat = true;
                    GlobalLog.Info("[FollowBot] RC: Stash");
                    return true;
                case Messages.RcNewInstance:
                    Tasks.FollowTask.ShouldCreateNewInstance = true;
                    GlobalLog.Info("[FollowBot] RC: NewInstance");
                    return true;
                case Messages.RcFollowTownOn:
                    follow.FollowInTown = true;
                    GlobalLog.Info("[FollowBot] RC: FollowInTown ON");
                    return true;
                case Messages.RcFollowTownOff:
                    follow.FollowInTown = false;
                    GlobalLog.Info("[FollowBot] RC: FollowInTown OFF");
                    return true;
                case Messages.RcFollowHideoutOn:
                    follow.FollowInHideout = true;
                    GlobalLog.Info("[FollowBot] RC: FollowInHideout ON");
                    return true;
                case Messages.RcFollowHideoutOff:
                    follow.FollowInHideout = false;
                    GlobalLog.Info("[FollowBot] RC: FollowInHideout OFF");
                    return true;
                case Messages.RcFollowHeistOn:
                    follow.FollowInHeistHub = true;
                    GlobalLog.Info("[FollowBot] RC: FollowInHeistHub ON");
                    return true;
                case Messages.RcFollowHeistOff:
                    follow.FollowInHeistHub = false;
                    GlobalLog.Info("[FollowBot] RC: FollowInHeistHub OFF");
                    return true;
                case Messages.RcAutoDepositOn:
                    stash.AutoDepositOnMapExit = true;
                    GlobalLog.Info("[FollowBot] RC: AutoDeposit ON");
                    return true;
                case Messages.RcAutoDepositOff:
                    stash.AutoDepositOnMapExit = false;
                    GlobalLog.Info("[FollowBot] RC: AutoDeposit OFF");
                    return true;
                case Messages.RcUseGuildStash:
                    stash.UseGuildStash = true;
                    GlobalLog.Info("[FollowBot] RC: UseGuildStash");
                    return true;
                case Messages.RcUseRegularStash:
                    stash.UseGuildStash = false;
                    GlobalLog.Info("[FollowBot] RC: UseRegularStash");
                    return true;
                case Messages.RcUltPortalOn:
                    loot.ShouldLootUltimatum = true;
                    GlobalLog.Info("[FollowBot] RC: Portal After Ultimatum ON");
                    return true;
                case Messages.RcUltPortalOff:
                    loot.ShouldLootUltimatum = false;
                    GlobalLog.Info("[FollowBot] RC: Portal After Ultimatum OFF");
                    return true;
                case Messages.RcSetUltTimer:
                    int timerVal;
                    if (message.TryGetInput<int>("value", out timerVal))
                    {
                        loot.UltimatumLootTimer = timerVal;
                        GlobalLog.Info($"[FollowBot] RC: UltimatumLootTimer = {timerVal}");
                    }
                    return true;
                case "RC_allocate": // Adding a standard ID for allocation as well
                    Tasks.AutoAllocatePassiveTask.ForceTrigger();
                    GlobalLog.Info("[FollowBot] RC: Allocate");
                    return true;
                case "RC_unloader":
                    Tasks.UltimatumUnloaderTask.TriggerUnloader();
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
                default:
                    return false;
            }
        }

        public string Name => "FollowBot";
        public string Author => "NotYourFriend, origial code from Unknown, Rushtothesun";
        public string Description => "Bot that follow leader.";
        public string Version => "1.0.0R";
        public UserControl Control => _gui ?? (_gui = new FollowBotGui());
        public JsonSettings Settings => FollowBotSettings.Instance;
        public override string ToString() => $"{Name}: {Description}";

        private void OnSettingsPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "AutoReloadPathfinder")
            {
                UpdatePathfinderSettings();
            }
        }

        private void UpdatePathfinderSettings()
        {
            var desiredState = FollowBotSettings.Instance.Follow.AutoReloadPathfinder;
            if (BotManager.AutoReloadPathfinde != desiredState)
            {
                GlobalLog.Debug($"[UpdatePathfinderSettings] Setting BotManager.AutoReloadPathfinde to {desiredState}.");
                BotManager.AutoReloadPathfinde = desiredState;
            }
        }
    }
}
