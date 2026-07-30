using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using FollowBot.SimpleEXtensions;
using DreamPoeBot.Loki.Bot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using SkillBarHud = DreamPoeBot.Loki.Game.LokiPoe.InGameState.SkillBarHud;

namespace FollowBot.Class
{
    public static class CustomSkills
    {
        #region Private Fields
        private static readonly Dictionary<string, DateTime> _lastCastTimes = new Dictionary<string, DateTime>();
        private const int COOLDOWN_MIN_MS = 500;
        private const int COOLDOWN_MAX_MS = 1000;
        private const int LEADER_DISTANCE_THRESHOLD = 60;
        #endregion

        #region Buff Skills
        public static void PhaseRun()
        {
            if (LokiPoe.Me.Auras.All(x => x.Name != "Phase Run"))
            {
                var phaseRun = SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "NewPhaseRun");
                if (phaseRun != null && phaseRun.IsOnSkillBar && phaseRun.Slot != -1 && phaseRun.CanUse())
                    SkillBarHud.Use(phaseRun.Slot, false, false);
            }
        }

        public static void GuardSkill()
        {
            var skillName = FollowBotSettings.Instance.CustomSkills.GuardSkillName;
            if (string.IsNullOrEmpty(skillName))
                return;

            var guardSkill = SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.Name.Equals(skillName, StringComparison.OrdinalIgnoreCase));
            if (guardSkill != null)
            {
                if (LokiPoe.Me.Auras.All(x => x.Name != skillName))
                {
                    if (guardSkill.IsOnSkillBar && guardSkill.Slot != -1 && guardSkill.CanUse())
                        SkillBarHud.Use(guardSkill.Slot, false, false);
                }
            }
        }
        #endregion

        #region Warcries

        public static void SeismicCry() => CastWarcry("SeismicCry", "Seismic Cry", 4);

        public static void BattlemageCry() => CastWarcry("DivineCry", "Battlemage's Cry", 2, checkSelf: false, checkLeader: true);

        public static void AncestralCry() => CastWarcry("AncestralCry", "Ancestral Cry", 3);

        public static void IntimidatingCry() => CastWarcry("IntimidatingCry", "Intimidating Cry", 2);

        public static void InfernalCry() => CastWarcry("InfernalCry", "Infernal Cry", 2, checkSelf: false, checkLeader: true);

        public static void RallyingCry() => CastWarcry("RallyingCryNew", "Rallied", 2, checkSelf: false, checkLeader: true);

        //Onslaught cluster tied to Enduring Cry
        public static void EnduringCry()
        {
            if (IsOnCooldown("EnduringCry"))
                return;
            {
                var enduringCry = SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "EnduringCry");
                if (enduringCry != null && enduringCry.CanUse())
                {
                    bool needsCast;
                    if (FollowBotSettings.Instance.CustomSkills.EnduringCryHasOnslaughtCluster)
                    {
                        needsCast = LokiPoe.Me.Auras.All(x => (x.Name == "Onslaught" && x.TimeLeft.Seconds <= 1) || x.Name != "Enduring Cry" || x.Name != "Onslaught" || (x.Name == "Enduring Cry" && x.TimeLeft.Seconds <= 4));
                    }
                    else
                    {
                        needsCast = LokiPoe.Me.Auras.All(x => (x.Name != "Enduring Cry" || (x.Name == "Enduring Cry" && x.TimeLeft.Seconds <= 4)));
                    }

                    if (needsCast && enduringCry.IsOnSkillBar && enduringCry.Slot != -1)
                    {
                        SkillBarHud.Use(enduringCry.Slot, false, false);
                        UpdateCooldown("EnduringCry");
                    }
                }
            }
        }
        #endregion

        #region Guardian's Blessing
        public static void GuardiansBlessingHandler()
        {
            var allowedAuras = new[] {
                "Anger", "Clarity", "Determination", "Discipline", "Grace", "Haste", "Hatred", "Malevolence", "Precision",
                "Purity of Elements", "Purity of Fire", "Purity of Ice", "Purity of Lightning", "Vitality", "Wrath", "Zealotry",
                "Pride"
            };
            foreach (var auraSkill in SkillBarHud.SkillBarSkills
                .Where(x => x != null && allowedAuras.Contains(x.Name)))
            {
                var display = auraSkill.LinkedDisplayString;
                if (string.IsNullOrEmpty(display) || !display.Contains("Guardian's Blessing Support"))
                    continue;

                string auraName = auraSkill.Name;
                string expectedAura = auraName + " Aura";
                if (LokiPoe.Me.Auras.Any(x => x.Name == expectedAura))
                    continue;

                // Get the user-specified minion name
                var minionName = FollowBotSettings.Instance.CustomSkills.GuardiansBlessingMinion;
                if (string.IsNullOrEmpty(minionName))
                    continue;

                // Find the specified minion skill
                var minionSkill = SkillBarHud.SkillBarSkills
                    .FirstOrDefault(x => x != null && x.Name.Equals(minionName, StringComparison.OrdinalIgnoreCase));

                if (minionSkill == null)
                    continue;

                var minionObj = minionSkill.DeployedObjects.FirstOrDefault() as Monster;
                if (minionObj == null)
                    continue;

                if (!auraSkill.CanUse())
                    continue;

                GlobalLog.Debug($"[GB] Casting \"{auraSkill.Name}\" - Guardian's Blessing for {expectedAura}");
                SkillBarHud.Use(auraSkill.Slot, false, false);
                break; // Only cast one per call
            }
        }
        #endregion

        #region Utility Skills
        public static void Convocation()
        {
            var Convocation = SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "convocation");
            if (Convocation != null && Convocation.CanUse())
            {
                SkillBarHud.Use(Convocation.Slot, false, false);
            }
        }

        public static void ChaosElixir()
        {
            if (LokiPoe.Me.Auras.All(x => (x.Name != "Coruscating Elixir") || (x.Name == "Coruscating Elixir" && x.TimeLeft.Seconds <= 1.3)))
            {
                LokiPoe.InGameState.QuickFlaskHud.UseFlaskInSlot(FollowBotSettings.Instance.CustomSkills.ChaosElixirFlaskSlot);
            }
        }

        public static void RejuvenationTotem()
        {
            var rejuvTotem = SkillBarHud.SkillBarSkills
                .FirstOrDefault(x => x != null && x.InternalName == "TotemAuraLifeRegen");

            if (rejuvTotem == null || !rejuvTotem.CanUse() || rejuvTotem.NumberDeployed > 0)
                return;

            var settings = FollowBotSettings.Instance.CustomSkills;

            // Override: Always cast during Ultimatum if enabled
            if (settings.RejuvenationTotemAlwaysUseInUltimatum)
            {
                var ultimatum = LokiPoe.ObjectManager.GetObjectsByType<UltimatumChallengeInteractable>().FirstOrDefault();
                if (ultimatum != null && ultimatum.IsTrialActive && !ultimatum.IsTrialCompleted)
                {
                    SkillBarHud.Use(rejuvTotem.Slot, false, false);
                    GlobalLog.Debug($"[CustomSkills] Casting Rejuvenation Totem (Ultimatum Override).");
                    return;
                }
            }

            // Override: Always cast during Blight if enabled
            if (settings.RejuvenationTotemAlwaysUseInBlight)
            {
                const string blightPumpMetadata = "Metadata/Terrain/Leagues/Blight/Objects/BlightPump";
                var blightPump = LokiPoe.ObjectManager.GetObjectsByMetadata(blightPumpMetadata).FirstOrDefault();
                if (blightPump != null)
                {
                    var stateMachine = blightPump.Components.StateMachineComponent;
                    if (stateMachine != null)
                    {
                        var activatedState = stateMachine.StageStates.FirstOrDefault(s => s.Name == "activated");
                        if (activatedState != null && activatedState.IsActive && activatedState.Value == 2)
                        {
                            SkillBarHud.Use(rejuvTotem.Slot, false, false);
                            GlobalLog.Debug($"[CustomSkills] Casting Rejuvenation Totem (Blight Override).");
                            return;
                        }
                    }
                }
            }

            // Normal health-based logic
            var leader = FollowBot.Leader;
            if (leader == null || leader.Distance > 60)
                return;

            var unreservedLeaderHealth = leader.MaxHealth - leader.HealthReserved;
            var leaderHealthPercentage = unreservedLeaderHealth > 0 ? ((double)leader.Health / unreservedLeaderHealth) * 100 : 100;

            bool leaderNeedsHelp = leaderHealthPercentage <= settings.RejuvenationTotemLeaderHealthPercent;

            bool selfNeedsHelp;

            if (LokiPoe.Me.EnergyShieldMax >= 1000)
            {
                selfNeedsHelp = LokiPoe.Me.EnergyShieldPercent <= settings.RejuvenationTotemFollowerHealthPercent;
            }
            else
            {
                var unreservedSelfHealth = LokiPoe.Me.MaxHealth - LokiPoe.Me.HealthReserved;
                var selfHealthPercentage = unreservedSelfHealth > 0 ? ((double)LokiPoe.Me.Health / unreservedSelfHealth) * 100 : 100;
                selfNeedsHelp = selfHealthPercentage <= settings.RejuvenationTotemFollowerHealthPercent;
            }

            if (leaderNeedsHelp || selfNeedsHelp)
            {
                SkillBarHud.Use(rejuvTotem.Slot, false, false);
                GlobalLog.Debug($"[CustomSkills] Casting Rejuvenation Totem.");
            }
        }
        #endregion

        #region Banner Skills
        public static void ComprehensiveBannerHandler()
        {
            if (!FollowBotSettings.Instance.CustomSkills.EnableComprehensiveBanner)
                return;

            // Determine activation conditions
            bool isUltimatumActive = false;
            if (FollowBotSettings.Instance.CustomSkills.UseBannersInUltimatum)
            {
                var ultimatum = LokiPoe.ObjectManager.GetObjectsByType<UltimatumChallengeInteractable>().FirstOrDefault();
                isUltimatumActive = ultimatum != null && ultimatum.IsTrialActive && !ultimatum.IsTrialCompleted;
            }

            bool isBlightActive = false;
            if (FollowBotSettings.Instance.CustomSkills.UseBannersInBlight)
            {
                const string blightPumpMetadata = "Metadata/Terrain/Leagues/Blight/Objects/BlightPump";
                var blightPump = LokiPoe.ObjectManager.GetObjectsByMetadata(blightPumpMetadata).FirstOrDefault();
                if (blightPump != null)
                {
                    var stateMachine = blightPump.Components.StateMachineComponent;
                    if (stateMachine != null)
                    {
                        var activatedState = stateMachine.StageStates.FirstOrDefault(s => s.Name == "activated");
                        if (activatedState != null && activatedState.IsActive && activatedState.Value == 2)
                        {
                            isBlightActive = true;
                        }
                    }
                }
            }

            bool useBanner = isUltimatumActive || isBlightActive;

            if (!useBanner)
            {
                if (FollowBotSettings.Instance.CustomSkills.UseBannersNearUniques)
                {
                    if (LokiPoe.ObjectManager.GetObjectsByType<Monster>().Any(m => m.Rarity == Rarity.Unique && m.IsAliveHostile && m.Distance <= 100 && m.IsTargetable))
                        useBanner = true;
                }
                if (!useBanner && FollowBotSettings.Instance.CustomSkills.UseBannersNearRares)
                {
                    if (LokiPoe.ObjectManager.GetObjectsByType<Monster>().Any(m => m.Rarity == Rarity.Rare && m.IsAliveHostile && m.Distance <= 60 && m.IsTargetable))
                        useBanner = true;
                }
            }

            if (!useBanner)
                return;

            // Get Valour charges
            var valourBuff = LokiPoe.Me.Auras.FirstOrDefault(x => x.InternalName == "valour");
            int valourCharges = valourBuff?.Charges ?? 0;

            // --- Valor on Demand ---
            bool needsValor = false;
            if (FollowBotSettings.Instance.CustomSkills.UseWarBanner && !HasBannerBuff("bloodstained_banner_buff_aura", "BloodstainedBanner") && valourCharges < FollowBotSettings.Instance.CustomSkills.WarBannerCharges) needsValor = true;
            else if (FollowBotSettings.Instance.CustomSkills.UseDefianceBanner && !HasBannerBuff("armour_evasion_banner_buff_aura", "ArmourEvasionBanner") && valourCharges < FollowBotSettings.Instance.CustomSkills.DefianceBannerCharges) needsValor = true;
            else if (FollowBotSettings.Instance.CustomSkills.UseDreadBanner && !HasBannerBuff("puresteel_banner_buff_aura", "PuresteelBanner") && valourCharges < FollowBotSettings.Instance.CustomSkills.DreadBannerCharges) needsValor = true;

            if (needsValor)
            {
                bool shouldGenerateValor = false;
                if (isUltimatumActive && FollowBotSettings.Instance.CustomSkills.GenerateValorInUltimatum)
                {
                    shouldGenerateValor = true;
                }
                else if (isBlightActive && FollowBotSettings.Instance.CustomSkills.GenerateValorInBlight)
                {
                    shouldGenerateValor = true;
                }
                else if (useBanner && FollowBotSettings.Instance.CustomSkills.GenerateValorNearUniques) // Fallback for uniques if not in blight/ult
                {
                    shouldGenerateValor = true;
                }


                if (shouldGenerateValor)
                {
                    UseWarcryForValor();
                    return;
                }
            }

            // --- Banner Priority Logic ---
            // 1. War Banner
            if (FollowBotSettings.Instance.CustomSkills.UseWarBanner)
            {
                bool hasWarBannerBuff = HasBannerBuff("bloodstained_banner_buff_aura", "BloodstainedBanner");
                if (!hasWarBannerBuff && valourCharges >= FollowBotSettings.Instance.CustomSkills.WarBannerCharges)
                {
                    var warBanner = SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "BloodstainedBanner");
                    if (warBanner != null && warBanner.CanUse())
                    {
                        GlobalLog.Debug($"Using War Banner");
                        SkillBarHud.Use(warBanner.Slot, false, false);
                        return;
                    }
                }
            }

            // 2. Defiance Banner
            if (FollowBotSettings.Instance.CustomSkills.UseDefianceBanner)
            {
                bool hasDefianceBannerBuff = HasBannerBuff("armour_evasion_banner_buff_aura", "ArmourEvasionBanner");
                bool warBannerEnabled = FollowBotSettings.Instance.CustomSkills.UseWarBanner;
                bool hasWarBannerBuff = HasBannerBuff("bloodstained_banner_buff_aura", "BloodstainedBanner");

                // Only proceed if War Banner is not enabled OR (War Banner is enabled AND already active)
                if ((!warBannerEnabled || (warBannerEnabled && hasWarBannerBuff)) && !hasDefianceBannerBuff && valourCharges >= FollowBotSettings.Instance.CustomSkills.DefianceBannerCharges)
                {
                    var defianceBanner = SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "ArmourEvasionBanner");
                    if (defianceBanner != null && defianceBanner.CanUse())
                    {
                        GlobalLog.Debug($"Using Defiance Banner");
                        SkillBarHud.Use(defianceBanner.Slot, false, false);
                        return;
                    }
                }
            }

            // 3. Dread Banner
            if (FollowBotSettings.Instance.CustomSkills.UseDreadBanner)
            {
                bool warBannerEnabled = FollowBotSettings.Instance.CustomSkills.UseWarBanner;
                bool defianceBannerEnabled = FollowBotSettings.Instance.CustomSkills.UseDefianceBanner;
                bool hasWarBannerBuff = HasBannerBuff("bloodstained_banner_buff_aura", "BloodstainedBanner");
                bool hasDefianceBannerBuff = HasBannerBuff("armour_evasion_banner_buff_aura", "ArmourEvasionBanner");
                bool hasDreadBannerBuff = HasBannerBuff("puresteel_banner_buff_aura", "PuresteelBanner");

                // Only proceed if higher priority banners are not enabled OR (they are enabled AND already active)
                bool canCastDread = (!warBannerEnabled || (warBannerEnabled && hasWarBannerBuff)) &&
                                    (!defianceBannerEnabled || (defianceBannerEnabled && hasDefianceBannerBuff)) &&
                                    !hasDreadBannerBuff &&
                                    valourCharges >= FollowBotSettings.Instance.CustomSkills.DreadBannerCharges;

                if (canCastDread)
                {
                    var dreadBanner = SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "PuresteelBanner");
                    if (dreadBanner != null && dreadBanner.CanUse())
                    {
                        GlobalLog.Debug($"Using Dread Banner");
                        SkillBarHud.Use(dreadBanner.Slot, false, false);
                        return;
                    }
                }
            }
        }
        #endregion

        #region Helper Methods

        /// <summary>
        /// Checks if a banner skill is linked with Generosity Support.
        /// </summary>
        private static bool IsLinkedWithGenerosity(string bannerInternalName)
        {
            var skill = SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == bannerInternalName);
            return skill?.LinkedGems != null && skill.LinkedGems.Any(g => g != null && g.FullName == "Generosity Support");
        }

        /// <summary>
        /// Checks if a banner buff aura is active. If the banner is linked with Generosity,
        /// checks the party leader's auras instead of the player's own auras.
        /// </summary>
        private static bool HasBannerBuff(string buffInternalName, string bannerInternalName)
        {
            if (IsLinkedWithGenerosity(bannerInternalName))
            {
                var leader = FollowBot.Leader;
                return leader != null && leader.Auras.Any(x => x.InternalName == buffInternalName);
            }
            return LokiPoe.Me.Auras.Any(x => x.InternalName == buffInternalName);
        }

        /// <summary>
        /// Checks if a skill is on cooldown with randomized cooldown duration between 500ms and 1000ms.
        /// </summary>
        public static bool IsOnCooldown(string skillKey, int minMs = COOLDOWN_MIN_MS, int maxMs = COOLDOWN_MAX_MS)
        {
            if (!_lastCastTimes.ContainsKey(skillKey))
                return false;

            var randomCooldown = LokiPoe.Random.Next(minMs, maxMs);
            return (DateTime.UtcNow - _lastCastTimes[skillKey]).TotalMilliseconds < randomCooldown;
        }

        /// <summary>
        /// Updates the last cast time for a skill.
        /// </summary>
        public static void UpdateCooldown(string skillKey)
        {
            _lastCastTimes[skillKey] = DateTime.UtcNow;
        }

        /// <summary>
        /// Generic warcry casting helper that checks cooldown, player/leader buff status, and casts if needed.
        /// </summary>
        /// <param name="internalName">Skill internal name (e.g., "SeismicCry")</param>
        /// <param name="buffName">Buff name to check (e.g., "Seismic Cry")</param>
        /// <param name="buffThresholdSeconds">Seconds remaining before recasting</param>
        /// <param name="checkSelf">Whether to check player's buff status</param>
        /// <param name="checkLeader">Whether to check leader's buff status</param>
        private static void CastWarcry(
            string internalName,
            string buffName,
            int buffThresholdSeconds,
            bool checkSelf = true,
            bool checkLeader = true)
        {
            // Check cooldown
            if (IsOnCooldown(internalName))
                return;

            var warcry = SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == internalName);
            if (warcry == null || !warcry.CanUse())
                return;

            var leader = FollowBot.Leader;

            // Check if player needs buff (only if checkSelf is true)
            bool meNeeds = checkSelf && LokiPoe.Me.Auras.All(x =>
                (x.Name == buffName && x.TimeLeft.Seconds <= buffThresholdSeconds) ||
                x.Name != buffName);

            // Check if leader needs buff (if enabled)
            bool leaderNeeds = checkLeader &&
                               leader != null &&
                               leader.Distance <= LEADER_DISTANCE_THRESHOLD &&
                               leader.Auras.All(x =>
                                   (x.Name == buffName && x.TimeLeft.Seconds <= buffThresholdSeconds) ||
                                   x.Name != buffName);

            // Cast if either player or leader needs it
            if (meNeeds || leaderNeeds)
            {
                if (warcry.IsOnSkillBar && warcry.Slot != -1)
                {
                    SkillBarHud.Use(warcry.Slot, false, false);
                    UpdateCooldown(internalName);
                }
            }
        }

        private static void UseWarcryForValor()
        {
            if (IsOnCooldown("ValorWarcry", 150, 250))
                return;

            var settings = FollowBotSettings.Instance.CustomSkills;

            if (!settings.EnableWarcriesForBanners)
                return;

            // Priority Check for General's Cry
            if (settings.EnableGeneralsCry)
            {
                var generalsCry = SkillBarHud.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "DoubleCry" && s.CanUse());
                if (generalsCry != null)
                {
                    GlobalLog.Debug($"[UseWarcryForValor] Using {generalsCry.Name}.");
                    SkillBarHud.Use(generalsCry.Slot, false, false);
                    UpdateCooldown("ValorWarcry");
                    return;
                }
            }

            var availableWarcries = new List<DreamPoeBot.Loki.RemoteMemoryObjects.Skill>();

            if (settings.EnableEnduringCry)
            {
                var cry = SkillBarHud.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "EnduringCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableSeismicCry)
            {
                var cry = SkillBarHud.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "SeismicCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableBattlemageCry)
            {
                var cry = SkillBarHud.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "DivineCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableAncestralCry)
            {
                var cry = SkillBarHud.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "AncestralCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableIntimidatingCry)
            {
                var cry = SkillBarHud.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "IntimidatingCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableInfernalCry)
            {
                var cry = SkillBarHud.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "InfernalCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableRallyingCry)
            {
                var cry = SkillBarHud.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "RallyingCryNew" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }

            if (availableWarcries.Any())
            {
                var warcryToUse = availableWarcries[LokiPoe.Random.Next(availableWarcries.Count)];
                if (warcryToUse.CanUse())
                {
                    GlobalLog.Debug($"[UseWarcryForValor] Using {warcryToUse.Name}.");
                    SkillBarHud.Use(warcryToUse.Slot, false, false);
                    UpdateCooldown("ValorWarcry");
                }
            }
        }
        #endregion
    }
}