using System.ComponentModel;

namespace FollowBot.Settings
{
    public class CustomSkillSettings : INotifyPropertyChanged
    {
        public string LinkSkillAdditionalTargets { get; set; } = "";
        private bool _enablePhaseRun = false;
        private bool _enableGuardSkill = false;
        private string _guardSkillName = "Molten Shell";
        private bool _enableWarcriesForBuffs = false;
        private bool _enableWarcriesForBanners = false;
        private bool _enableEnduringCry = false;
        private bool _enduringCryHasOnslaughtCluster = false;
        private bool _enableGeneralsCry = false;
        private bool _enableSeismicCry = false;
        private bool _enableBattlemageCry = false;
        private bool _enableAncestralCry = false;
        private bool _enableIntimidatingCry = false;
        private bool _enableInfernalCry = false;
        private bool _enableRallyingCry = false;
        private bool _enableGuardiansBlessingHandler = false;
        private bool _enableSentinelUsage = false;
        private bool _enableChaosElixir = false;
        private int _chaosElixirFlaskSlot = 1;
        private bool _enableConvocation = false;
        private bool _enableLinkSkill = false;
        private bool _enableRejuvenationTotem = false;
        private int _rejuvenationTotemLeaderHealthPercent = 70;
        private int _rejuvenationTotemFollowerHealthPercent = 70;
        private bool _rejuvenationTotemAlwaysUseInUltimatum = false;
        private bool _rejuvenationTotemAlwaysUseInBlight = false;
        private string _guardiansBlessingMinion = "";
        private string _golemManaToggleAura = "";
        private int _golemManaToggleTimeoutMs = 5000;
        private bool _enableSummonRagingSpirits = false;
        private int _minRagingSpirits = 10;
        private bool _srsOnNormalMagic = false;
        private int _srsMonsterDistance = 100;
        private int _srsCustomDistance = 40;
        private bool _enableSummonSkeletons = false;
        private int _minSkeletons = 5;
        private bool _skeletonsOnNormalMagic = false;
        private int _skeletonsMonsterDistance = 100;
        private int _skeletonsCustomDistance = 40;
        private bool _enableComprehensiveBanner = false;
        private bool _useWarBanner = false;
        private int _warBannerCharges = 105;
        private bool _useDefianceBanner = false;
        private int _defianceBannerCharges = 80;
        private bool _useDreadBanner = false;
        private int _dreadBannerCharges = 105;
        private bool _useBannersNearRares = false;
        private bool _useBannersNearUniques = false;
        private bool _useBannersInUltimatum = false;
        private bool _useBannersInBlight = false;
        private bool _generateValorNearUniques = false;
        private bool _generateValorInUltimatum = false;
        private bool _generateValorInBlight = false;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DefaultValue(false)]
        public bool EnablePhaseRun
        {
            get => _enablePhaseRun;
            set { _enablePhaseRun = value; NotifyPropertyChanged(nameof(EnablePhaseRun)); }
        }

        [DefaultValue(false)]
        public bool EnableGuardSkill
        {
            get => _enableGuardSkill;
            set { _enableGuardSkill = value; NotifyPropertyChanged(nameof(EnableGuardSkill)); }
        }

        [DefaultValue("Molten Shell")]
        public string GuardSkillName
        {
            get => _guardSkillName;
            set { _guardSkillName = value; NotifyPropertyChanged(nameof(GuardSkillName)); }
        }

        #region Warcries
        [DefaultValue(false)]
        public bool EnableWarcriesForBuffs
        {
            get => _enableWarcriesForBuffs;
            set { _enableWarcriesForBuffs = value; NotifyPropertyChanged(nameof(EnableWarcriesForBuffs)); }
        }

        [DefaultValue(false)]
        public bool EnableWarcriesForBanners
        {
            get => _enableWarcriesForBanners;
            set { _enableWarcriesForBanners = value; NotifyPropertyChanged(nameof(EnableWarcriesForBanners)); }
        }

        [DefaultValue(false)]
        public bool EnableEnduringCry
        {
            get => _enableEnduringCry;
            set { _enableEnduringCry = value; NotifyPropertyChanged(nameof(EnableEnduringCry)); }
        }

        [DefaultValue(false)]
        public bool EnduringCryHasOnslaughtCluster
        {
            get => _enduringCryHasOnslaughtCluster;
            set { _enduringCryHasOnslaughtCluster = value; NotifyPropertyChanged(nameof(EnduringCryHasOnslaughtCluster)); }
        }

        [DefaultValue(false)]
        public bool EnableGeneralsCry
        {
            get => _enableGeneralsCry;
            set { _enableGeneralsCry = value; NotifyPropertyChanged(nameof(EnableGeneralsCry)); }
        }

        [DefaultValue(false)]
        public bool EnableSeismicCry
        {
            get => _enableSeismicCry;
            set { _enableSeismicCry = value; NotifyPropertyChanged(nameof(EnableSeismicCry)); }
        }

        [DefaultValue(false)]
        public bool EnableBattlemageCry
        {
            get => _enableBattlemageCry;
            set { _enableBattlemageCry = value; NotifyPropertyChanged(nameof(EnableBattlemageCry)); }
        }

        [DefaultValue(false)]
        public bool EnableAncestralCry
        {
            get => _enableAncestralCry;
            set { _enableAncestralCry = value; NotifyPropertyChanged(nameof(EnableAncestralCry)); }
        }

        [DefaultValue(false)]
        public bool EnableIntimidatingCry
        {
            get => _enableIntimidatingCry;
            set { _enableIntimidatingCry = value; NotifyPropertyChanged(nameof(EnableIntimidatingCry)); }
        }

        [DefaultValue(false)]
        public bool EnableInfernalCry
        {
            get => _enableInfernalCry;
            set { _enableInfernalCry = value; NotifyPropertyChanged(nameof(EnableInfernalCry)); }
        }

        [DefaultValue(false)]
        public bool EnableRallyingCry
        {
            get => _enableRallyingCry;
            set { _enableRallyingCry = value; NotifyPropertyChanged(nameof(EnableRallyingCry)); }
        }
        #endregion

        [DefaultValue(false)]
        public bool EnableGuardiansBlessingHandler
        {
            get => _enableGuardiansBlessingHandler;
            set { _enableGuardiansBlessingHandler = value; NotifyPropertyChanged(nameof(EnableGuardiansBlessingHandler)); }
        }

        [DefaultValue(false)]
        public bool EnableSentinelUsage
        {
            get => _enableSentinelUsage;
            set { _enableSentinelUsage = value; NotifyPropertyChanged(nameof(EnableSentinelUsage)); }
        }

        [DefaultValue(false)]
        public bool EnableChaosElixir
        {
            get => _enableChaosElixir;
            set { _enableChaosElixir = value; NotifyPropertyChanged(nameof(EnableChaosElixir)); }
        }

        [DefaultValue(1)]
        public int ChaosElixirFlaskSlot
        {
            get => _chaosElixirFlaskSlot;
            set { _chaosElixirFlaskSlot = value; NotifyPropertyChanged(nameof(ChaosElixirFlaskSlot)); }
        }

        [DefaultValue(false)]
        public bool EnableConvocation
        {
            get => _enableConvocation;
            set { _enableConvocation = value; NotifyPropertyChanged(nameof(EnableConvocation)); }
        }

        [DefaultValue(false)]
        public bool EnableLinkSkill
        {
            get => _enableLinkSkill;
            set { _enableLinkSkill = value; NotifyPropertyChanged(nameof(EnableLinkSkill)); }
        }

        [DefaultValue(false)]
        public bool EnableRejuvenationTotem
        {
            get => _enableRejuvenationTotem;
            set { _enableRejuvenationTotem = value; NotifyPropertyChanged(nameof(EnableRejuvenationTotem)); }
        }

        [DefaultValue(70)]
        public int RejuvenationTotemLeaderHealthPercent
        {
            get => _rejuvenationTotemLeaderHealthPercent;
            set { _rejuvenationTotemLeaderHealthPercent = value; NotifyPropertyChanged(nameof(RejuvenationTotemLeaderHealthPercent)); }
        }

        [DefaultValue(70)]
        public int RejuvenationTotemFollowerHealthPercent
        {
            get => _rejuvenationTotemFollowerHealthPercent;
            set { _rejuvenationTotemFollowerHealthPercent = value; NotifyPropertyChanged(nameof(RejuvenationTotemFollowerHealthPercent)); }
        }

        [DefaultValue(false)]
        public bool RejuvenationTotemAlwaysUseInUltimatum
        {
            get => _rejuvenationTotemAlwaysUseInUltimatum;
            set { _rejuvenationTotemAlwaysUseInUltimatum = value; NotifyPropertyChanged(nameof(RejuvenationTotemAlwaysUseInUltimatum)); }
        }

        [DefaultValue(false)]
        public bool RejuvenationTotemAlwaysUseInBlight
        {
            get => _rejuvenationTotemAlwaysUseInBlight;
            set { _rejuvenationTotemAlwaysUseInBlight = value; NotifyPropertyChanged(nameof(RejuvenationTotemAlwaysUseInBlight)); }
        }

        [DefaultValue("")]
        public string GuardiansBlessingMinion
        {
            get => _guardiansBlessingMinion;
            set { _guardiansBlessingMinion = value; NotifyPropertyChanged(nameof(GuardiansBlessingMinion)); }
        }

        [DefaultValue("")]
        public string GolemManaToggleAura
        {
            get => _golemManaToggleAura;
            set { _golemManaToggleAura = value; NotifyPropertyChanged(nameof(GolemManaToggleAura)); }
        }

        [DefaultValue(5000)]
        public int GolemManaToggleTimeoutMs
        {
            get => _golemManaToggleTimeoutMs;
            set { _golemManaToggleTimeoutMs = value; NotifyPropertyChanged(nameof(GolemManaToggleTimeoutMs)); }
        }

        #region Summon Raging Spirits
        [DefaultValue(false)]
        public bool EnableSummonRagingSpirits
        {
            get => _enableSummonRagingSpirits;
            set { _enableSummonRagingSpirits = value; NotifyPropertyChanged(nameof(EnableSummonRagingSpirits)); }
        }

        [DefaultValue(10)]
        public int MinRagingSpirits
        {
            get => _minRagingSpirits;
            set { _minRagingSpirits = value; NotifyPropertyChanged(nameof(MinRagingSpirits)); }
        }

        [DefaultValue(false)]
        public bool SrsOnNormalMagic
        {
            get => _srsOnNormalMagic;
            set { _srsOnNormalMagic = value; NotifyPropertyChanged(nameof(SrsOnNormalMagic)); }
        }

        [DefaultValue(100)]
        public int SrsMonsterDistance
        {
            get => _srsMonsterDistance;
            set { _srsMonsterDistance = value; NotifyPropertyChanged(nameof(SrsMonsterDistance)); }
        }

        [DefaultValue(40)]
        public int SrsCustomDistance
        {
            get => _srsCustomDistance;
            set { _srsCustomDistance = value; NotifyPropertyChanged(nameof(SrsCustomDistance)); }
        }
        #endregion

        #region Summon Skeletons
        [DefaultValue(false)]
        public bool EnableSummonSkeletons
        {
            get => _enableSummonSkeletons;
            set { _enableSummonSkeletons = value; NotifyPropertyChanged(nameof(EnableSummonSkeletons)); }
        }

        [DefaultValue(5)]
        public int MinSkeletons
        {
            get => _minSkeletons;
            set { _minSkeletons = value; NotifyPropertyChanged(nameof(MinSkeletons)); }
        }

        [DefaultValue(false)]
        public bool SkeletonsOnNormalMagic
        {
            get => _skeletonsOnNormalMagic;
            set { _skeletonsOnNormalMagic = value; NotifyPropertyChanged(nameof(SkeletonsOnNormalMagic)); }
        }

        [DefaultValue(100)]
        public int SkeletonsMonsterDistance
        {
            get => _skeletonsMonsterDistance;
            set { _skeletonsMonsterDistance = value; NotifyPropertyChanged(nameof(SkeletonsMonsterDistance)); }
        }

        [DefaultValue(40)]
        public int SkeletonsCustomDistance
        {
            get => _skeletonsCustomDistance;
            set { _skeletonsCustomDistance = value; NotifyPropertyChanged(nameof(SkeletonsCustomDistance)); }
        }
        #endregion


        #region Comprehensive Banner Settings
        [DefaultValue(false)]
        public bool EnableComprehensiveBanner
        {
            get => _enableComprehensiveBanner;
            set { _enableComprehensiveBanner = value; NotifyPropertyChanged(nameof(EnableComprehensiveBanner)); }
        }

        [DefaultValue(false)]
        public bool UseWarBanner
        {
            get => _useWarBanner;
            set { _useWarBanner = value; NotifyPropertyChanged(nameof(UseWarBanner)); }
        }

        [DefaultValue(105)]
        public int WarBannerCharges
        {
            get => _warBannerCharges;
            set { _warBannerCharges = value; NotifyPropertyChanged(nameof(WarBannerCharges)); }
        }

        [DefaultValue(false)]
        public bool UseDefianceBanner
        {
            get => _useDefianceBanner;
            set { _useDefianceBanner = value; NotifyPropertyChanged(nameof(UseDefianceBanner)); }
        }

        [DefaultValue(80)]
        public int DefianceBannerCharges
        {
            get => _defianceBannerCharges;
            set { _defianceBannerCharges = value; NotifyPropertyChanged(nameof(DefianceBannerCharges)); }
        }

        [DefaultValue(false)]
        public bool UseDreadBanner
        {
            get => _useDreadBanner;
            set { _useDreadBanner = value; NotifyPropertyChanged(nameof(UseDreadBanner)); }
        }

        [DefaultValue(105)]
        public int DreadBannerCharges
        {
            get => _dreadBannerCharges;
            set { _dreadBannerCharges = value; NotifyPropertyChanged(nameof(DreadBannerCharges)); }
        }

        [DefaultValue(false)]
        public bool UseBannersNearRares
        {
            get => _useBannersNearRares;
            set { _useBannersNearRares = value; NotifyPropertyChanged(nameof(UseBannersNearRares)); }
        }

        [DefaultValue(false)]
        public bool UseBannersNearUniques
        {
            get => _useBannersNearUniques;
            set { _useBannersNearUniques = value; NotifyPropertyChanged(nameof(UseBannersNearUniques)); }
        }

        [DefaultValue(false)]
        public bool UseBannersInUltimatum
        {
            get => _useBannersInUltimatum;
            set { _useBannersInUltimatum = value; NotifyPropertyChanged(nameof(UseBannersInUltimatum)); }
        }

        [DefaultValue(false)]
        public bool UseBannersInBlight
        {
            get => _useBannersInBlight;
            set { _useBannersInBlight = value; NotifyPropertyChanged(nameof(UseBannersInBlight)); }
        }

        [DefaultValue(false)]
        public bool GenerateValorNearUniques
        {
            get => _generateValorNearUniques;
            set { _generateValorNearUniques = value; NotifyPropertyChanged(nameof(GenerateValorNearUniques)); }
        }

        [DefaultValue(false)]
        public bool GenerateValorInUltimatum
        {
            get => _generateValorInUltimatum;
            set { _generateValorInUltimatum = value; NotifyPropertyChanged(nameof(GenerateValorInUltimatum)); }
        }

        [DefaultValue(false)]
        public bool GenerateValorInBlight
        {
            get => _generateValorInBlight;
            set { _generateValorInBlight = value; NotifyPropertyChanged(nameof(GenerateValorInBlight)); }
        }
        #endregion
    }
}