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
using log4net;
using System.Collections.Generic;
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
        public static readonly ILog Log = Logger.GetLoggerInstanceForType();

        private FollowBotGui _gui;
        private Coroutine _coroutine;

        private readonly TaskManager _taskManager = new TaskManager();
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
        public static void PhaseRun()
        {
            if (LokiPoe.Me.Auras.All(x => x.Name != "Phase Run"))
            {
                var phaseRun = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "NewPhaseRun");
                if (phaseRun != null && phaseRun.IsOnSkillBar && phaseRun.Slot != -1 && phaseRun.CanUse())
                    LokiPoe.InGameState.SkillBarHud.Use(phaseRun.Slot, false, false);
            }
        }

        public static void MoltenShell()
        {
            var moltenShell = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "FireShield");
            if (moltenShell != null)
            {
                if (LokiPoe.Me.Auras.All(x => x.Name != "Molten Shell"))
                {
                    if (moltenShell != null && moltenShell.IsOnSkillBar && moltenShell.Slot != -1 && moltenShell.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(moltenShell.Slot, false, false);
                }
            }
        }

        #region Crys
        //Onslaught cluster tied to Enduring Cry
        public static void EnduringCry()
        {
            var enduringCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "EnduringCry");
            if (enduringCry != null)
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Onslaught" && x.TimeLeft.Seconds <= 1) || x.Name != "Enduring Cry" || x.Name != "Onslaught" || (x.Name == "Enduring Cry" && x.TimeLeft.Seconds <= 4)))
                {
                    if (enduringCry != null && enduringCry.IsOnSkillBar && enduringCry.Slot != -1 && enduringCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(enduringCry.Slot, false, false);
                }
            }
        }

        public static void EnduringCryBeforeCluster()
        {
            var enduringCryBefore = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "EnduringCry");
            if (enduringCryBefore != null)
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name != "Enduring Cry" || (x.Name == "Enduring Cry" && x.TimeLeft.Seconds <= 4))))
                {
                    if (enduringCryBefore != null && enduringCryBefore.IsOnSkillBar && enduringCryBefore.Slot != -1 && enduringCryBefore.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(enduringCryBefore.Slot, false, false);
                }
            }
        }

        public static void SeismicCry()
        {
            var seismicCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SeismicCry");
            if (seismicCry != null)
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Seismic Cry" && x.TimeLeft.Seconds <= 4) || x.Name != "Seismic Cry"))
                {
                    if (seismicCry != null && seismicCry.IsOnSkillBar && seismicCry.Slot != -1 && seismicCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(seismicCry.Slot, false, false);
                }
            }
        }

        public static void BattlemageCry()
        {
            var battlemageCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "DivineCry");
            if (battlemageCry != null)
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Battlemage's Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Battlemage's Cry"))
                {
                    if (battlemageCry != null && battlemageCry.IsOnSkillBar && battlemageCry.Slot != -1 && battlemageCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(battlemageCry.Slot, false, false);
                }
            }
        }

        public static void AncestralCry()
        {
            var ancestralCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "AncestralCry");
            if (ancestralCry != null)
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Ancestral Cry" && x.TimeLeft.Seconds <= 3) || x.Name != "Ancestral Cry"))
                {
                    if (ancestralCry != null && ancestralCry.IsOnSkillBar && ancestralCry.Slot != -1 && ancestralCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(ancestralCry.Slot, false, false);
                }
            }
        }

        public static void IntimidatingCry()
        {
            var intimidatingCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "IntimidatingCry");
            if (intimidatingCry != null)
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Intimidating Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Intimidating Cry"))
                {
                    if (intimidatingCry != null && intimidatingCry.IsOnSkillBar && intimidatingCry.Slot != -1 && intimidatingCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(intimidatingCry.Slot, false, false);
                }
            }
        }

        public static void InfernalCry()
        {
            var infernalCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "InfernalCry");
            if (infernalCry != null)
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Infernal Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Infernal Cry"))
                {
                    if (infernalCry != null && infernalCry.IsOnSkillBar && infernalCry.Slot != -1 && infernalCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(infernalCry.Slot, false, false);
                }
            }
        }
        #endregion
        #region Guardians Blessing
        public static void GuardiansBlessingMalevolence()
        {
            var guardiansBlessing = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "CastAuraDamageOverTime");
            var relicSkill2 = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SummonRelic");
			if (guardiansBlessing != null && LokiPoe.Me.Auras.All(x => (x.Name != "Malevolence Aura")) && guardiansBlessing.CanUse())
            {
                var relicObj2 = relicSkill2.DeployedObjects.FirstOrDefault() as Monster;
                if (relicObj2 != null)
                {
                    GlobalLog.Debug($"Casting \"{guardiansBlessing.Name}\" - Guardian's Blessing");
					LokiPoe.InGameState.SkillBarHud.Use(guardiansBlessing.Slot, false, false);
                }
            }           
        }	

        public static void GuardiansBlessingDetermination()
        {
            var guardiansBlessing = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "CastAuraArmour");
            var relicSkill2 = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SummonRelic");
            if (guardiansBlessing != null && LokiPoe.Me.Auras.All(x => (x.Name != "Determination Aura")) && guardiansBlessing.CanUse())
            {
                var relicObj2 = relicSkill2.DeployedObjects.FirstOrDefault() as Monster;
                if (relicObj2 != null)
                {
                    GlobalLog.Debug($"Casting \"{guardiansBlessing.Name}\" - Guardian's Blessing");
                    LokiPoe.InGameState.SkillBarHud.Use(guardiansBlessing.Slot, false, false);
                }
            }
        }

        public static void GuardiansBlessingHaste()
        {
            var guardiansBlessing = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "CastAuraSpeed");
            var relicSkill2 = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SummonRelic");
            if (guardiansBlessing != null && LokiPoe.Me.Auras.All(x => x.Name != "Haste Aura" && guardiansBlessing.CanUse()))
            {
                var relicObj2 = relicSkill2.DeployedObjects.FirstOrDefault() as Monster;
                if (relicObj2 != null)
                {
                    GlobalLog.Debug($"Casting \"{guardiansBlessing.Name}\" - Guardian's Blessing");
                    LokiPoe.InGameState.SkillBarHud.Use(guardiansBlessing.Slot, false, false);
                }
            }
        }
    
        public static void GuardiansBlessingHatred()
        {
            var guardiansBlessing = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "CastAuraColdDamage");
            var relicSkill2 = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SummonRelic");
            if (guardiansBlessing != null && LokiPoe.Me.Auras.All(x => x.Name != "Hatred Aura" && guardiansBlessing.CanUse()))
            {
                var relicObj2 = relicSkill2.DeployedObjects.FirstOrDefault() as Monster;
                if (relicObj2 != null)
                {
                    GlobalLog.Debug($"Casting \"{guardiansBlessing.Name}\" - Guardian's Blessing");
                    LokiPoe.InGameState.SkillBarHud.Use(guardiansBlessing.Slot, false, false);
                }
            }
        }
        #endregion

        //Guardian's Sentinel of Radiance
        public static void SentinelUsage()
        {
            var sentinelSkill = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SummonRadiantSentinel");
            if (sentinelSkill != null)
            {
                var sentinelObj = sentinelSkill.DeployedObjects.FirstOrDefault() as Monster;
                if (sentinelObj == null && sentinelSkill.CanUse())
                {
                    GlobalLog.Debug($"Casting \"{sentinelSkill.Name}\" - Sentinel");
                    LokiPoe.InGameState.SkillBarHud.Use(sentinelSkill.Slot, false, false);
                }
            }
        }

        //Coruscating Elixir in flask slot 1
        public static void ChaosElixir()
        {
            if (LokiPoe.Me.Auras.All(x => (x.Name != "Coruscating Elixir") || (x.Name == "Coruscating Elixir" && x.TimeLeft.Seconds <= 1.3)))
            {
                LokiPoe.InGameState.QuickFlaskHud.UseFlaskInSlot(1);
            }
        }

        public static void SoulLink()
        {
            var link = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SoulLink");
            if (link != null && link.CanUse())
            {
                var linkLeader = FollowBot.Leader;
                if (linkLeader != null && linkLeader.Distance <= 60)
                {
                    if (linkLeader.Auras.All(x => (x.Name != "Soul Link")) || LokiPoe.Me.Auras.Any(x => x.InternalName == "soul_link_source" && x.TimeLeft.Seconds <= 6))
                    {
                        //if (link.IsOnSkillBar && link.Slot != -1 && link.CanUse())
                        LokiPoe.InGameState.SkillBarHud.UseOn(link.Slot, false, FollowBot.Leader, false);
                    }       //await Coroutines.FinishCurrentAction();

                }
            }
        }

        public void Start()
        {
            _lastBoundMoveSkillSlot = -1;
            _lastBoundMoveSkillKey = Keys.Clear;

            ItemEvaluator.Instance = DefaultItemEvaluator.Instance;
            Explorer.CurrentDelegate = user => CombatAreaCache.Current.Explorer.BasicExplorer;

            ComplexExplorer.ResetSettingsProviders();
            ComplexExplorer.AddSettingsProvider("FollowBot", MapBotExploration, ProviderPriority.Low);

            // Cache all bound keys.
            LokiPoe.Input.Binding.Update();

            // Reset the default MsBetweenTicks on start.
            Log.Debug($"[Start] MsBetweenTicks: {BotManager.MsBetweenTicks}.");
            Log.Debug($"[Start] PlayerMover.Instance: {PlayerMoverManager.Current.GetType()}.");

            // Since this bot will be performing client actions, we need to enable the process hook manager.
            LokiPoe.ProcessHookManager.Enable();

            _coroutine = null;

            ExilePather.BlockLockedDoors = FeatureEnum.Disabled;
            ExilePather.BlockLockedTempleDoors = FeatureEnum.Disabled;
            ExilePather.BlockTrialOfAscendancy = FeatureEnum.Disabled;

            ExilePather.Reload();

            _taskManager.Reset();

            AddTasks();

            Events.Start();
            PluginManager.Start();
            RoutineManager.Start();
            _taskManager.Start();

            foreach (var plugin in PluginManager.EnabledPlugins)
            {
                Log.Debug($"[Start] The plugin {plugin.Name} is enabled.");
            }

            Log.Debug($"[Start] PlayerMover.Instance: {PlayerMoverManager.Current.GetType()}.");

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
                Log.Debug($"The bot coroutine has finished in a state of {_coroutine.Status}");
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
            _taskManager.Stop();
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
                if (LokiPoe.IsInLoginScreen)
                {
                    // Offload auto login logic to a plugin.
                    var logic = new Logic("hook_login_screen", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                    {
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                            break;
                    }
                }
                else if (LokiPoe.IsInCharacterSelectionScreen)
                {
                    // Offload character selection logic to a plugin.
                    var logic = new Logic("hook_character_selection", this);
                    foreach (var plugin in PluginManager.EnabledPlugins)
                    {
                        if (await plugin.Logic(logic) == LogicResult.Provided)
                            break;
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
                            Log.Debug("Waiting for game pause");
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
                    await Coroutine.Sleep(1000);
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
                handled = true;
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
            _taskManager.Add(new TradeTask());
            _taskManager.Add(new QuestInteractionTask());
            _taskManager.Add(new DefenseAndFlaskTask());
            _taskManager.Add(new LootItemTask());
            _taskManager.Add(new PreCombatFollowTask());
            _taskManager.Add(new CombatTask(50));
            _taskManager.Add(new PostCombatHookTask());
            _taskManager.Add(new LevelGemsTask());
            _taskManager.Add(new CombatTask(-1));
            _taskManager.Add(new CastAuraTask());
            _taskManager.Add(new TravelToPartyZoneTask());
            _taskManager.Add(new FollowTask());
            // _taskManager.Add(new OpenWaypointTask());
            _taskManager.Add(new JoinPartyTask());
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
            Log.Info($"[FollowBot] New map has been entered: {areaName}.");
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
        }

        public string Name => "FollowBot";
        public string Author => "NotYourFriend, origial code from Unknown";
        public string Description => "Bot that follow leader.";
        public string Version => "0.0.7.1";
        public UserControl Control => _gui ?? (_gui = new FollowBotGui());
        public JsonSettings Settings => FollowBotSettings.Instance;
        public override string ToString() => $"{Name}: {Description}";
    }
}
