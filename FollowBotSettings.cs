using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.Class;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace FollowBot
{
    public class FollowBotSettings : JsonSettings
    {
        private static FollowBotSettings _instance;
        public static FollowBotSettings Instance => _instance ?? (_instance = new FollowBotSettings());

        private FollowBotSettings()
            : base(GetSettingsFilePath(Configuration.Instance.Name, "FollowBot.json"))
        {
            if (DefensiveSkills == null)
                DefensiveSkills = SetupDefaultDefensiveSkills();
            if (Flasks == null)
                Flasks = SetupDefaultFlasks();
        }

        private bool _ignoreHiddenAuras;

        [DefaultValue(false)]
        public bool IgnoreHiddenAuras
        {
            get { return _ignoreHiddenAuras; }
            set
            { _ignoreHiddenAuras = value; NotifyPropertyChanged(() => IgnoreHiddenAuras); }
        }

        private bool _autoReloadPathfinder = false;

        [DefaultValue(false)]
        public bool AutoReloadPathfinder
        {
            get => _autoReloadPathfinder;
            set
            {
                if (value.Equals(_autoReloadPathfinder)) return;
                _autoReloadPathfinder = value;
                NotifyPropertyChanged(() => AutoReloadPathfinder);
            }
        }

        private string _acceptedManualInviteNames;
        private int _followDistance;
        private int _maxfollowDistance;
        private int _maxCombatDistance;
        private int _maxLootDistance;
        public int _portOutThreshold;

        #region Party Role
        private bool _shouldKill;
        private bool _shouldLoot;
        private bool _shouldLootOnlyQuestItem;
        private bool _useStalkerSentinel;
        private bool _dontPortOutofMap;
        private bool _shouldFollow = true;
        private bool _shouldLootUltimatum;
        private int _ultimatumLootTimer;

        #endregion

        #region Auras
        private bool _enableAspectsOfTheAvian;
        private bool _enableAspectsOfTheCat;
        private bool _enableAspectsOfTheCrab;
        private bool _enableAspectsOfTheSpider;
        private BloodAndSand _bloodOrSand;
        #endregion

        #region Auras
        [DefaultValue(false)]
        public bool EnableAspectsOfTheAvian
        {
            get
            {
                return _enableAspectsOfTheAvian;
            }
            set
            {
                _enableAspectsOfTheAvian = value;
                NotifyPropertyChanged(() => EnableAspectsOfTheAvian);
            }
        }
        [DefaultValue(false)]
        public bool EnableAspectsOfTheCat
        {
            get
            {
                return _enableAspectsOfTheCat;
            }
            set
            {
                _enableAspectsOfTheCat = value;
                NotifyPropertyChanged(() => EnableAspectsOfTheCat);
            }
        }
        [DefaultValue(false)]
        public bool EnableAspectsOfTheCrab
        {
            get
            {
                return _enableAspectsOfTheCrab;
            }
            set
            {
                _enableAspectsOfTheCrab = value;
                NotifyPropertyChanged(() => EnableAspectsOfTheCrab);
            }
        }
        [DefaultValue(false)]
        public bool EnableAspectsOfTheSpider
        {
            get
            {
                return _enableAspectsOfTheSpider;
            }
            set
            {
                _enableAspectsOfTheSpider = value;
                NotifyPropertyChanged(() => EnableAspectsOfTheSpider);
            }
        }

        [DefaultValue(BloodAndSand.Sand)]
        public BloodAndSand BloorOrSand
        {
            get
            {
                return _bloodOrSand;
            }
            set
            {
                _bloodOrSand = value;
                NotifyPropertyChanged(() => BloorOrSand);
            }
        }

        public enum BloodAndSand
        {
            Blood,
            Sand
        }

        #endregion

        #region Defence
        private ObservableCollection<DefensiveSkillsClass> _defensiveSkills;
        private ObservableCollection<FlasksClass> _flasks;
        #endregion

        #region Gems
        private bool _gemDebugStatements;
        private bool _levelAllGems;
        private bool _levelOffhandOnly;
        private bool _useLevelAllButton;
        private ObservableCollection<string> _globalNameIgnoreList;
 
        #endregion
        
        #region Follow Settings
        [DefaultValue("")]
        public string InviteWhiteList
        {
            get { return _acceptedManualInviteNames; }
            set
            { _acceptedManualInviteNames = value; NotifyPropertyChanged(() => InviteWhiteList); }
        }
        [DefaultValue(15)]
        public int FollowDistance
        {
            get { return _followDistance; }
            set
            { _followDistance = value; NotifyPropertyChanged(() => FollowDistance); }
        }
        [DefaultValue(25)]
        public int MaxFollowDistance
        {
            get { return _maxfollowDistance; }
            set
            { _maxfollowDistance = value; NotifyPropertyChanged(() => MaxFollowDistance); }
        }
        [DefaultValue(40)]
        public int MaxCombatDistance
        {
            get { return _maxCombatDistance; }
            set
            { _maxCombatDistance = value; NotifyPropertyChanged(() => MaxCombatDistance); }
        }
        [DefaultValue(40)]
        public int MaxLootDistance
        {
            get { return _maxLootDistance; }
            set
            { _maxLootDistance = value; NotifyPropertyChanged(() => MaxLootDistance); }
        }


        [DefaultValue(0)]
        public int PortOutThreshold
        {
            get { return _portOutThreshold; }
            set
            {
                _portOutThreshold = value;
                NotifyPropertyChanged(() => PortOutThreshold);
            }
        }
        #endregion

        #region Party Role

        [DefaultValue(true)]
        public bool ShouldKill
        {
            get { return _shouldKill; }
            set
            { _shouldKill = value; NotifyPropertyChanged(() => ShouldKill); }
        }
        [DefaultValue(false)]
        public bool ShouldLoot
        {
            get { return _shouldLoot; }
            set
            { _shouldLoot = value; NotifyPropertyChanged(() => ShouldLoot); }
        }
        [DefaultValue(false)]
        public bool ShouldLootOnlyQuestItem
        {
            get { return _shouldLootOnlyQuestItem; }
            set
            {
                _shouldLootOnlyQuestItem = value; NotifyPropertyChanged(() => ShouldLootOnlyQuestItem);
            }
        }
        private bool _interactQuest;
        [DefaultValue(false)]
        public bool InteractQuest
        {
            get { return _interactQuest; }
            set
            {
                _interactQuest = value; NotifyPropertyChanged(() => InteractQuest);
            }
        }

        [DefaultValue(false)]
        public bool UseStalkerSentinel
        {
            get { return _useStalkerSentinel; }
            set
            { _useStalkerSentinel = value; NotifyPropertyChanged(() => UseStalkerSentinel); }
        }
        [DefaultValue(false)]
        public bool DontPortOutofMap
        {
            get { return _dontPortOutofMap; }
            set
            { _dontPortOutofMap = value; NotifyPropertyChanged(() => DontPortOutofMap); }
        }
        [JsonIgnore]
        public bool ShouldFollow
        {
            get { return _shouldFollow; }
            set
            { _shouldFollow = value; NotifyPropertyChanged(() => ShouldFollow); }
        }

        [DefaultValue(false)]
        public bool ShouldLootUltimatum
        {
            get { return _shouldLootUltimatum; }
            set { _shouldLootUltimatum = value; NotifyPropertyChanged(() => ShouldLootUltimatum); }
        }

        [DefaultValue(5)]
        public int UltimatumLootTimer
        {
            get { return _ultimatumLootTimer; }
            set { _ultimatumLootTimer = value; NotifyPropertyChanged(() => UltimatumLootTimer); }
        }
        #endregion

        #region Defence Skills
        public ObservableCollection<DefensiveSkillsClass> DefensiveSkills
        {
            get => _defensiveSkills;//?? (_defensiveSkills = new ObservableCollection<DefensiveSkillsClass>());
            set
            {
                _defensiveSkills = value;
                NotifyPropertyChanged(() => DefensiveSkills);
            }
        }

        #endregion

        #region Flasks
        public ObservableCollection<FlasksClass> Flasks
        {
            get => _flasks;//?? (_flasks = new ObservableCollection<FlasksClass>());
            set
            {
                _flasks = value;
                NotifyPropertyChanged(() => Flasks);
            }
        }
        private ObservableCollection<DefensiveSkillsClass> SetupDefaultDefensiveSkills()
        {
            ObservableCollection<DefensiveSkillsClass> skills = new ObservableCollection<DefensiveSkillsClass>();

            skills.Add(new DefensiveSkillsClass(false, "Vaal Molten Shell", false, 0, 0, false, ""));
            skills.Add(new DefensiveSkillsClass(false, "Vaal Discipline", false, 0, 0, false, ""));
            skills.Add(new DefensiveSkillsClass(false, "Molten Shell", false, 0, 0, false, ""));
            skills.Add(new DefensiveSkillsClass(false, "Steelskin", false, 0, 0, false, ""));
            return skills;
        }
        private ObservableCollection<FlasksClass> SetupDefaultFlasks()
        {
            ObservableCollection<FlasksClass> flasks = new ObservableCollection<FlasksClass>();

            flasks.Add(new FlasksClass(false, 1, false, false, 0, 0, false));
            flasks.Add(new FlasksClass(false, 2, false, false, 0, 0, false));
            flasks.Add(new FlasksClass(false, 3, false, false, 0, 0, false));
            flasks.Add(new FlasksClass(false, 4, false, false, 0, 0, false));
            flasks.Add(new FlasksClass(false, 5, false, false, 0, 0, false));
            return flasks;
        }

        #endregion

        #region SkillGems
        /// <summary>
		/// Should the plugin log debug statements?
		/// </summary>
		[DefaultValue(false)]
        public bool GemDebugStatements
        {
            get
            {
                return _gemDebugStatements;
            }
            set
            {
                if (value.Equals(_gemDebugStatements))
                {
                    return;
                }
                _gemDebugStatements = value;
                NotifyPropertyChanged(() => GemDebugStatements);
            }
        }
        [DefaultValue(false)]
        public bool LevelOffhandOnly
        {
            get
            {
                return _levelOffhandOnly;
            }
            set
            {
                if (value.Equals(_levelOffhandOnly))
                {
                    return;
                }
                _levelOffhandOnly = value;
                NotifyPropertyChanged(() => LevelOffhandOnly);
            }
        }
        [DefaultValue(false)]
        public bool LevelAllGems
        {
            get
            {
                return _levelAllGems;
            }
            set
            {
                if (value.Equals(_levelAllGems))
                {
                    return;
                }
                _levelAllGems = value;
                NotifyPropertyChanged(() => LevelAllGems);
            }
        }
        [DefaultValue(false)]
        public bool UseLevelAllButton
        {
            get
            {
                return _useLevelAllButton;
            }
            set
            {
                if (value.Equals(_useLevelAllButton))
                {
                    return;
                }
                _useLevelAllButton = value;
                NotifyPropertyChanged(() => UseLevelAllButton);
            }
        }
        /// <summary>
		/// A list of skillgem names to ignore from leveling.
		/// </summary>
		public ObservableCollection<string> GlobalNameIgnoreList
        {
            get
            {
                return _globalNameIgnoreList ?? (_globalNameIgnoreList = new ObservableCollection<string>());
            }
            set
            {
                if (value.Equals(_globalNameIgnoreList))
                {
                    return;
                }
                _globalNameIgnoreList = value;
                NotifyPropertyChanged(() => GlobalNameIgnoreList);
            }
        }

        /// <summary>
		/// A list of SkillGemEntry for the user's skillgems.
		/// </summary>
		[JsonIgnore]
        public ObservableCollection<SkillGemEntry> UserSkillGemsInOffHands
        {
            get
            {
                //using (LokiPoe.AcquireFrame())
                //{
                ObservableCollection<SkillGemEntry> skillGemEntries = new ObservableCollection<SkillGemEntry>();

                if (!LokiPoe.IsInGame)
                {
                    return skillGemEntries;
                }

                foreach (Inventory inv in UsableOffInventories)
                {
                    foreach (Item item in inv.Items)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (item.Components.SocketsComponent == null)
                        {
                            continue;
                        }

                        for (int idx = 0; idx < item.SocketedGems.Length; idx++)
                        {
                            Item gem = item.SocketedGems[idx];
                            if (gem == null)
                            {
                                continue;
                            }

                            skillGemEntries.Add(new SkillGemEntry(gem.Name, inv.PageSlot, idx));
                        }
                    }
                }
                return skillGemEntries;
                //}
            }
        }
        /// <summary>
        /// A list of SkillGemEntry for the user's skillgems.
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<SkillGemEntry> UserSkillGems
        {
            get
            {
                //using (LokiPoe.AcquireFrame())
                //{
                ObservableCollection<SkillGemEntry> skillGemEntries = new ObservableCollection<SkillGemEntry>();

                if (!LokiPoe.IsInGame)
                {
                    return skillGemEntries;
                }

                foreach (Inventory inv in UsableInventories)
                {
                    foreach (Item item in inv.Items)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (item.Components.SocketsComponent == null)
                        {
                            continue;
                        }

                        for (int idx = 0; idx < item.SocketedGems.Length; idx++)
                        {
                            Item gem = item.SocketedGems[idx];
                            if (gem == null)
                            {
                                continue;
                            }

                            skillGemEntries.Add(new SkillGemEntry(gem.Name, inv.PageSlot, idx));
                        }
                    }
                }
                return skillGemEntries;
                //}
            }
        }
        public void UpdateGlobalNameIgnoreList()
        {
            NotifyPropertyChanged(() => GlobalNameIgnoreList);
        }
        public class SkillGemEntry
        {
            public string Name;
            public InventorySlot InventorySlot;
            public int SocketIndex;

            public string SerializationString { get; private set; }

            public SkillGemEntry(string name, InventorySlot slot, int socketIndex)
            {
                Name = name;
                InventorySlot = slot;
                SocketIndex = socketIndex;
                SerializationString = string.Format("{0} [{1}: {2}]", Name, InventorySlot, SocketIndex);
            }

            public Item InventoryItem
            {
                get
                {
                    return UsableInventories.Where(ui => ui.PageSlot == InventorySlot)
                        .Select(ui => ui.Items.FirstOrDefault())
                        .FirstOrDefault();
                }
            }

            public Item SkillGem
            {
                get
                {
                    Item item = InventoryItem;
                    if (item == null || item.Components.SocketsComponent == null)
                    {
                        return null;
                    }

                    Item sg = item.SocketedGems[SocketIndex];
                    if (sg == null)
                    {
                        return null;
                    }

                    if (sg.Name != Name)
                    {
                        return null;
                    }

                    return sg;
                }
            }
        }
        private static IEnumerable<Inventory> UsableInventories => new[]
        {
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.LeftHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.RightHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.OffLeftHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.OffRightHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Head),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Chest),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Gloves),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Boots),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.LeftRing),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.RightRing),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Neck)
        };
        private static IEnumerable<Inventory> UsableOffInventories => new[]
        {
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.OffLeftHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.OffRightHand),
        };
        #endregion

        #region ChatCommands

        private string _TeleportToLeaderChatCommand;
        private string _stopFollowChatCommand;
        private string _startFollowChatCommand;
        private string _stopLootChatCommand;
        private string _startLootChatCommand;
        private string _stopAttackChatCommand;
        private string _startAttackChatCommand;
        private string _stopSentinelChatCommand;
        private string _startSentinelChatCommand;
        private string _stopAutoTeleportChatCommand;
        private string _startAutoTeleportChatCommand;
        private string _openTownPortalChatCommand;
        private string _enterPortalChatCommand;

        [DefaultValue("EnterP")]
        public string EnterPortalChatCommand
        {
            get
            {
                return _enterPortalChatCommand;
            }
            set
            {
                _enterPortalChatCommand = value;
                NotifyPropertyChanged(() => EnterPortalChatCommand);
            }
        }

        [DefaultValue("Tele")]
        public string TeleportToLeaderChatCommand
        {
            get
            {
                return _TeleportToLeaderChatCommand;
            }
            set
            {
                _TeleportToLeaderChatCommand = value;
                NotifyPropertyChanged(() => TeleportToLeaderChatCommand);
            }
        }

        [DefaultValue("StopF")]
        public string StopFollowChatCommand
        {
            get
            {
                return _stopFollowChatCommand;
            }
            set
            {
                _stopFollowChatCommand = value;
                NotifyPropertyChanged(() => StopFollowChatCommand);
            }
        }

        [DefaultValue("StartF")]
        public string StartFollowChatCommand
        {
            get
            {
                return _startFollowChatCommand;
            }
            set
            {
                _startFollowChatCommand = value;
                NotifyPropertyChanged(() => StartFollowChatCommand);
            }
        }

        [DefaultValue("StopL")]
        public string StopLootChatCommand
        {
            get
            {
                return _stopLootChatCommand;
            }
            set
            {
                _stopLootChatCommand = value;
                NotifyPropertyChanged(() => StopLootChatCommand);
            }
        }

        [DefaultValue("StartL")]
        public string StartLootChatCommand
        {
            get
            {
                return _startLootChatCommand;
            }
            set
            {
                _startLootChatCommand = value;
                NotifyPropertyChanged(() => StartLootChatCommand);
            }
        }

        [DefaultValue("StopA")]
        public string StopAttackChatCommand
        {
            get
            {
                return _stopAttackChatCommand;
            }
            set
            {
                _stopAttackChatCommand = value;
                NotifyPropertyChanged(() => StopAttackChatCommand);
            }
        }

        [DefaultValue("StartA")]
        public string StartAttackChatCommand
        {
            get
            {
                return _startAttackChatCommand;
            }
            set
            {
                _startAttackChatCommand = value;
                NotifyPropertyChanged(() => StartAttackChatCommand);
            }
        }

        [DefaultValue("StopD")]
        public string StopSentinelChatCommand
        {
            get
            {
                return _stopSentinelChatCommand;
            }
            set
            {
                _stopSentinelChatCommand = value;
                NotifyPropertyChanged(() => StopSentinelChatCommand);
            }
        }

        [DefaultValue("StartD")]
        public string StartSentinelChatCommand
        {
            get
            {
                return _startSentinelChatCommand;
            }
            set
            {
                _startSentinelChatCommand = value;
                NotifyPropertyChanged(() => StartSentinelChatCommand);
            }
        }

        [DefaultValue("StopP")]
        public string StopAutoTeleportChatCommand
        {
            get
            {
                return _stopAutoTeleportChatCommand;
            }
            set
            {
                _stopAutoTeleportChatCommand = value;
                NotifyPropertyChanged(() => StopAutoTeleportChatCommand);
            }
        }

        [DefaultValue("StartP")]
        public string StartAutoTeleportChatCommand
        {
            get
            {
                return _startAutoTeleportChatCommand;
            }
            set
            {
                _startAutoTeleportChatCommand = value;
                NotifyPropertyChanged(() => StartAutoTeleportChatCommand);
            }
        }

        [DefaultValue("OpenP")]
        public string OpenTownPortalChatCommand
        {
            get { return _openTownPortalChatCommand; }
            set
            {
                _openTownPortalChatCommand = value;
                NotifyPropertyChanged(() => OpenTownPortalChatCommand);
            }
        }

        #endregion

        #region Overlay

        private bool _enableOverlay;
        private bool _drawInBackground;
        private bool _drawMobs;
        private bool _drawCorpses;
        private int _fps;
        private int _overlayXCoord;
        private int _overlayYCoord;
        private int _overlayTransparency;

        [DefaultValue(false)]
        public bool EnableOverlay
        {
            get => _enableOverlay;
            set
            {
                if (value == _enableOverlay) return;
                _enableOverlay = value;
                NotifyPropertyChanged(() => EnableOverlay);
            }
        }
        [DefaultValue(false)]
        public bool DrawInBackground
        {
            get => _drawInBackground;
            set
            {
                if (value == _drawInBackground) return;
                _drawInBackground = value;
                NotifyPropertyChanged(() => DrawInBackground);
            }
        }
        [DefaultValue(false)]
        public bool DrawMobs
        {
            get => _drawMobs;
            set
            {
                if (value == _drawMobs) return;
                _drawMobs = value;
                NotifyPropertyChanged(() => DrawMobs);
            }
        }
        [DefaultValue(false)]
        public bool DrawCorpses
        {
            get => _drawCorpses;
            set
            {
                if (value == _drawCorpses) return;
                _drawCorpses = value;
                NotifyPropertyChanged(() => DrawCorpses);
            }
        }

        [DefaultValue(30)]
        public int FPS
        {
            get => _fps;
            set
            {
                if (value == _fps) return;
                _fps = value;
                if (OverlayWindow.Instance != null)
                    OverlayWindow.Instance.SetFps(_fps);
                NotifyPropertyChanged(() => FPS);
            }
        }

        [DefaultValue(15)]
        public int OverlayXCoord
        {
            get => _overlayXCoord;
            set
            {
                if (value == _overlayXCoord) return;
                _overlayXCoord = value;
                NotifyPropertyChanged(() => OverlayXCoord);
            }
        }

        [DefaultValue(70)]
        public int OverlayYCoord
        {
            get => _overlayYCoord;
            set
            {
                if (value == _overlayYCoord) return;
                _overlayYCoord = value;
                NotifyPropertyChanged(() => OverlayYCoord);
            }
        }

        [DefaultValue(70)]
        public int OverlayTransparency
        {
            get => _overlayTransparency;
            set
            {
                if (value == _overlayTransparency) return;
                _overlayTransparency = value;
                if (OverlayWindow.Instance != null)
                    OverlayWindow.Instance.SetTransparency(_overlayTransparency);
                NotifyPropertyChanged(() => OverlayTransparency);
            }
        }

        #endregion

        #region CustomSkills
        // Custom Skills
        public string LinkSkillAdditionalTargets { get; set; } = "";
        private bool _enablePhaseRun;
        [DefaultValue(false)]
        public bool EnablePhaseRun
        {
            get => _enablePhaseRun;
            set { _enablePhaseRun = value; NotifyPropertyChanged(() => EnablePhaseRun); }
        }

        private bool _enableGuardSkill;
        [DefaultValue(false)]
        public bool EnableGuardSkill
        {
            get => _enableGuardSkill;
            set { _enableGuardSkill = value; NotifyPropertyChanged(() => EnableGuardSkill); }
        }

        private string _guardSkillName;
        [DefaultValue("Molten Shell")]
        public string GuardSkillName
        {
            get => _guardSkillName;
            set { _guardSkillName = value; NotifyPropertyChanged(() => GuardSkillName); }
        }
 
        #region Warcries
        private bool _enableEnduringCry;
        [DefaultValue(false)]
        public bool EnableEnduringCry
        {
            get => _enableEnduringCry;
            set { _enableEnduringCry = value; NotifyPropertyChanged(() => EnableEnduringCry); }
        }

        private bool _enduringCryHasOnslaughtCluster;
        [DefaultValue(false)]
        public bool EnduringCryHasOnslaughtCluster
        {
            get => _enduringCryHasOnslaughtCluster;
            set { _enduringCryHasOnslaughtCluster = value; NotifyPropertyChanged(() => EnduringCryHasOnslaughtCluster); }
        }
 
        private bool _enableSeismicCry;
        [DefaultValue(false)]
        public bool EnableSeismicCry
        {
            get => _enableSeismicCry;
            set { _enableSeismicCry = value; NotifyPropertyChanged(() => EnableSeismicCry); }
        }

        private bool _enableBattlemageCry;
        [DefaultValue(false)]
        public bool EnableBattlemageCry
        {
            get => _enableBattlemageCry;
            set { _enableBattlemageCry = value; NotifyPropertyChanged(() => EnableBattlemageCry); }
        }

        private bool _enableAncestralCry;
        [DefaultValue(false)]
        public bool EnableAncestralCry
        {
            get => _enableAncestralCry;
            set { _enableAncestralCry = value; NotifyPropertyChanged(() => EnableAncestralCry); }
        }

        private bool _enableIntimidatingCry;
        [DefaultValue(false)]
        public bool EnableIntimidatingCry
        {
            get => _enableIntimidatingCry;
            set { _enableIntimidatingCry = value; NotifyPropertyChanged(() => EnableIntimidatingCry); }
        }

        private bool _enableInfernalCry;
        [DefaultValue(false)]
        public bool EnableInfernalCry
        {
            get => _enableInfernalCry;
            set { _enableInfernalCry = value; NotifyPropertyChanged(() => EnableInfernalCry); }
        }

        private bool _enableRallyingCry;
        [DefaultValue(false)]
        public bool EnableRallyingCry
        {
            get => _enableRallyingCry;
            set { _enableRallyingCry = value; NotifyPropertyChanged(() => EnableRallyingCry); }
        }
        #endregion

        private bool _enableGuardiansBlessingHandler;
        [DefaultValue(false)]
        public bool EnableGuardiansBlessingHandler
        {
            get => _enableGuardiansBlessingHandler;
            set { _enableGuardiansBlessingHandler = value; NotifyPropertyChanged(() => EnableGuardiansBlessingHandler); }
        }

        private bool _enableSentinelUsage;
        [DefaultValue(false)]
        public bool EnableSentinelUsage
        {
            get => _enableSentinelUsage;
            set { _enableSentinelUsage = value; NotifyPropertyChanged(() => EnableSentinelUsage); }
        }

        private bool _enableChaosElixir;
        [DefaultValue(false)]
        public bool EnableChaosElixir
        {
            get => _enableChaosElixir;
            set { _enableChaosElixir = value; NotifyPropertyChanged(() => EnableChaosElixir); }
        }

        private bool _enableConvocation;
        [DefaultValue(false)]
        public bool EnableConvocation
        {
            get => _enableConvocation;
            set { _enableConvocation = value; NotifyPropertyChanged(() => EnableConvocation); }
        }

        private bool _enableLinkSkill;
        [DefaultValue(false)]
        public bool EnableLinkSkill
        {
            get => _enableLinkSkill;
            set { _enableLinkSkill = value; NotifyPropertyChanged(() => EnableLinkSkill); }
        }

        private bool _enableUseRejuvenationTotemDuringUltimatum;
        [DefaultValue(false)]
        public bool EnableUseRejuvenationTotemDuringUltimatum
        {
            get => _enableUseRejuvenationTotemDuringUltimatum;
            set { _enableUseRejuvenationTotemDuringUltimatum = value; NotifyPropertyChanged(() => EnableUseRejuvenationTotemDuringUltimatum); }
        }

        private bool _enableUseWarBannerDuringUltimatumOrNearUnique;
        [DefaultValue(false)]
        public bool EnableUseWarBannerDuringUltimatumOrNearUnique
        {
            get => _enableUseWarBannerDuringUltimatumOrNearUnique;
            set { _enableUseWarBannerDuringUltimatumOrNearUnique = value; NotifyPropertyChanged(() => EnableUseWarBannerDuringUltimatumOrNearUnique); }
        }

        private bool _enableUseWarDefianceBannerDuringUltimatumOrNearUnique;
        [DefaultValue(false)]
        public bool EnableUseWarDefianceBannerDuringUltimatumOrNearUnique
        {
            get => _enableUseWarDefianceBannerDuringUltimatumOrNearUnique;
            set { _enableUseWarDefianceBannerDuringUltimatumOrNearUnique = value; NotifyPropertyChanged(() => EnableUseWarDefianceBannerDuringUltimatumOrNearUnique); }
        }

        private bool _enableRejuvenationTotem;
        [DefaultValue(false)]
        public bool EnableRejuvenationTotem
        {
            get => _enableRejuvenationTotem;
            set { _enableRejuvenationTotem = value; NotifyPropertyChanged(() => EnableRejuvenationTotem); }
        }

        private string _guardiansBlessingMinion;
        [DefaultValue("")]
        public string GuardiansBlessingMinion
        {
            get => _guardiansBlessingMinion;
            set { _guardiansBlessingMinion = value; NotifyPropertyChanged(() => GuardiansBlessingMinion); }
        }

        #region Summon Raging Spirits
        private bool _enableSummonRagingSpirits;
        [DefaultValue(false)]
        public bool EnableSummonRagingSpirits
        {
            get => _enableSummonRagingSpirits;
            set { _enableSummonRagingSpirits = value; NotifyPropertyChanged(() => EnableSummonRagingSpirits); }
        }

        private int _minRagingSpirits;
        [DefaultValue(10)]
        public int MinRagingSpirits
        {
            get => _minRagingSpirits;
            set { _minRagingSpirits = value; NotifyPropertyChanged(() => MinRagingSpirits); }
        }

        private bool _srsOnNormalMagic;
        [DefaultValue(false)]
        public bool SrsOnNormalMagic
        {
            get => _srsOnNormalMagic;
            set { _srsOnNormalMagic = value; NotifyPropertyChanged(() => SrsOnNormalMagic); }
        }

        private int _srsMonsterDistance;
        [DefaultValue(100)]
        public int SrsMonsterDistance
        {
            get => _srsMonsterDistance;
            set { _srsMonsterDistance = value; NotifyPropertyChanged(() => SrsMonsterDistance); }
        }
        
        private int _srsCustomDistance;
        [DefaultValue(40)]
        public int SrsCustomDistance
        {
            get => _srsCustomDistance;
            set { _srsCustomDistance = value; NotifyPropertyChanged(() => SrsCustomDistance); }
        }
        #endregion

        #region Summon Skeletons
        private bool _enableSummonSkeletons;
        [DefaultValue(false)]
        public bool EnableSummonSkeletons
        {
            get => _enableSummonSkeletons;
            set { _enableSummonSkeletons = value; NotifyPropertyChanged(() => EnableSummonSkeletons); }
        }

        private int _minSkeletons;
        [DefaultValue(5)]
        public int MinSkeletons
        {
            get => _minSkeletons;
            set { _minSkeletons = value; NotifyPropertyChanged(() => MinSkeletons); }
        }

        private bool _skeletonsOnNormalMagic;
        [DefaultValue(false)]
        public bool SkeletonsOnNormalMagic
        {
            get => _skeletonsOnNormalMagic;
            set { _skeletonsOnNormalMagic = value; NotifyPropertyChanged(() => SkeletonsOnNormalMagic); }
        }

        private int _skeletonsMonsterDistance;
        [DefaultValue(100)]
        public int SkeletonsMonsterDistance
        {
            get => _skeletonsMonsterDistance;
            set { _skeletonsMonsterDistance = value; NotifyPropertyChanged(() => SkeletonsMonsterDistance); }
        }
        
        private int _skeletonsCustomDistance;
        [DefaultValue(40)]
        public int SkeletonsCustomDistance
        {
            get => _skeletonsCustomDistance;
            set { _skeletonsCustomDistance = value; NotifyPropertyChanged(() => SkeletonsCustomDistance); }
        }
        #endregion
        #endregion

    }
}

