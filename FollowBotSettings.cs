using DreamPoeBot.Loki;
using DreamPoeBot.Loki.Common;
using FollowBot.Settings;

namespace FollowBot
{
    public class FollowBotSettings : JsonSettings
    {
        private static FollowBotSettings _instance;
        public static FollowBotSettings Instance => _instance ?? (_instance = new FollowBotSettings());

        private FollowBotSettings()
            : base(GetSettingsFilePath(Configuration.Instance.Name, "FollowBot.json"))
        {
            // Initialize nested settings objects
            Follow = Follow ?? new FollowSettings();
            Combat = Combat ?? new CombatSettings();
            Loot = Loot ?? new LootSettings();
            Auras = Auras ?? new AuraSettings();
            Gems = Gems ?? new GemSettings();
            CustomSkills = CustomSkills ?? new CustomSkillSettings();
            ChatCommands = ChatCommands ?? new ChatCommandSettings();
            Overlay = Overlay ?? new OverlaySettings();
            Trade = Trade ?? new TradeSettings();
            Lab = Lab ?? new LabSettings();
            UI = UI ?? new UISettings();
            Stash = Stash ?? new StashSettings();
            Login = Login ?? new LoginSettings();
            PassiveTree = PassiveTree ?? new PassiveSkillTreeSettings();
            Wish = Wish ?? new WishSettings();
            TradeBuyout = TradeBuyout ?? new TradeBuyoutSettings();
        }

        // Nested settings properties
        public FollowSettings Follow { get; set; } = new FollowSettings();
        public CombatSettings Combat { get; set; } = new CombatSettings();
        public LootSettings Loot { get; set; } = new LootSettings();
        public AuraSettings Auras { get; set; } = new AuraSettings();
        public GemSettings Gems { get; set; } = new GemSettings();
        public CustomSkillSettings CustomSkills { get; set; } = new CustomSkillSettings();
        public ChatCommandSettings ChatCommands { get; set; } = new ChatCommandSettings();
        public OverlaySettings Overlay { get; set; } = new OverlaySettings();
        public TradeSettings Trade { get; set; } = new TradeSettings();
        public LabSettings Lab { get; set; } = new LabSettings();
        public UISettings UI { get; set; } = new UISettings();
        public StashSettings Stash { get; set; } = new StashSettings();
        public LoginSettings Login { get; set; } = new LoginSettings();
        public PassiveSkillTreeSettings PassiveTree { get; set; } = new PassiveSkillTreeSettings();
        public WishSettings Wish { get; set; } = new WishSettings();
        public TradeBuyoutSettings TradeBuyout { get; set; } = new TradeBuyoutSettings();
    }
}
