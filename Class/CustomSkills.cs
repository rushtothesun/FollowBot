using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using FollowBot.SimpleEXtensions;
using DreamPoeBot.Loki.Bot;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using SkillBar = DreamPoeBot.Loki.Game.LokiPoe.InGameState.SkillBarHud;

namespace FollowBot.Class
{
    public static class CustomSkills
    {
        #region Private Fields
        private static readonly Stopwatch _enduringCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _seismicCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _battlemageCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _ancestralCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _intimidatingCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _rallyingCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _infernalCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _valorWarcryStopwatch = new Stopwatch();
        private static readonly System.Random _random = new System.Random();
        #endregion

        #region Buff Skills
        public static void PhaseRun()
        {
            if (LokiPoe.Me.Auras.All(x => x.Name != "Phase Run"))
            {
                var phaseRun = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "NewPhaseRun");
                if (phaseRun != null && phaseRun.IsOnSkillBar && phaseRun.Slot != -1 && phaseRun.CanUse())
                    LokiPoe.InGameState.SkillBarHud.Use(phaseRun.Slot, false, false);
            }
        }

        public static void GuardSkill()
        {
            var skillName = FollowBotSettings.Instance.GuardSkillName;
            if (string.IsNullOrEmpty(skillName))
                return;

            var guardSkill = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.Name.Equals(skillName, System.StringComparison.OrdinalIgnoreCase));
            if (guardSkill != null)
            {
                if (LokiPoe.Me.Auras.All(x => x.Name != skillName))
                {
                    if (guardSkill.IsOnSkillBar && guardSkill.Slot != -1 && guardSkill.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(guardSkill.Slot, false, false);
                }
            }
        }
        #endregion

        #region Warcries
        //Onslaught cluster tied to Enduring Cry
        public static void EnduringCry()
        {
            if (!_enduringCryStopwatch.IsRunning || _enduringCryStopwatch.ElapsedMilliseconds > 500)
            {
                var enduringCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "EnduringCry");
                if (enduringCry != null && enduringCry.CanUse())
                {
                    bool needsCast;
                    if (FollowBotSettings.Instance.EnduringCryHasOnslaughtCluster)
                    {
                        needsCast = LokiPoe.Me.Auras.All(x => (x.Name == "Onslaught" && x.TimeLeft.Seconds <= 1) || x.Name != "Enduring Cry" || x.Name != "Onslaught" || (x.Name == "Enduring Cry" && x.TimeLeft.Seconds <= 4));
                    }
                    else
                    {
                        needsCast = LokiPoe.Me.Auras.All(x => (x.Name != "Enduring Cry" || (x.Name == "Enduring Cry" && x.TimeLeft.Seconds <= 4)));
                    }

                    if (needsCast)
                    {
                        if (enduringCry.IsOnSkillBar && enduringCry.Slot != -1)
                        {
                            LokiPoe.InGameState.SkillBarHud.Use(enduringCry.Slot, false, false);
                            _enduringCryStopwatch.Restart();
                        }
                    }
                }
            }
        }

        /*public static void SeismicCry()
        {
            var seismicCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SeismicCry");
            if (seismicCry != null && seismicCry.CanUse())
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Seismic Cry" && x.TimeLeft.Seconds <= 4) || x.Name != "Seismic Cry"))
                {
                    if (seismicCry != null && seismicCry.IsOnSkillBar && seismicCry.Slot != -1 && seismicCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(seismicCry.Slot, false, false);
					GlobalLog.Debug($"Casting Seismic Cry");
                }
            }
        }*/
        public static void SeismicCry()
        {
            if (!_seismicCryStopwatch.IsRunning || _seismicCryStopwatch.ElapsedMilliseconds > 500)
            {
                var seismicCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SeismicCry");
                if (seismicCry != null && seismicCry.CanUse())
                {
                    var leader = FollowBot.Leader;
                    bool meNeeds = LokiPoe.Me.Auras.All(x => (x.Name == "Seismic Cry" && x.TimeLeft.Seconds <= 4) || x.Name != "Seismic Cry");
                    bool leaderNeeds = leader != null && leader.Auras.All(x => (x.Name == "Seismic Cry" && x.TimeLeft.Seconds <= 4) || x.Name != "Seismic Cry");
                    if (meNeeds || (leaderNeeds && leader != null && leader.Distance <= 60))
                    {
                        if (seismicCry != null && seismicCry.IsOnSkillBar && seismicCry.Slot != -1 && seismicCry.CanUse())
                        {
                            LokiPoe.InGameState.SkillBarHud.Use(seismicCry.Slot, false, false);
                            _seismicCryStopwatch.Restart();
                        }
                    }
                }
            }
        }

        /*public static void BattlemageCry()
        {
            var battlemageCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "DivineCry");
            if (battlemageCry != null && battlemageCry.CanUse())
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Battlemage's Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Battlemage's Cry"))
                {
                    if (battlemageCry != null && battlemageCry.IsOnSkillBar && battlemageCry.Slot != -1 && battlemageCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(battlemageCry.Slot, false, false);
					GlobalLog.Debug($"Casting Battlemage's Cry");
                }
            }
        }*/
        public static void BattlemageCry()
        {
            if (!_battlemageCryStopwatch.IsRunning || _battlemageCryStopwatch.ElapsedMilliseconds > 500)
            {
                var battlemageCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "DivineCry");
                if (battlemageCry != null && battlemageCry.CanUse())
                {
                    var leader = FollowBot.Leader;
                    bool meNeeds = LokiPoe.Me.Auras.All(x => (x.Name == "Battlemage's Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Battlemage's Cry");
                    bool leaderNeeds = leader != null && leader.Auras.All(x => (x.Name == "Battlemage's Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Battlemage's Cry");
                    if (meNeeds || (leaderNeeds && leader != null && leader.Distance <= 60))
                    {
                        if (battlemageCry != null && battlemageCry.IsOnSkillBar && battlemageCry.Slot != -1 && battlemageCry.CanUse())
                        {
                            LokiPoe.InGameState.SkillBarHud.Use(battlemageCry.Slot, false, false);
                            _battlemageCryStopwatch.Restart();
                        }
                    }
                }
            }
        }

        /*public static void AncestralCry()
        {
            var ancestralCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "AncestralCry");
            if (ancestralCry != null && ancestralCry.CanUse())
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Ancestral Cry" && x.TimeLeft.Seconds <= 3) || x.Name != "Ancestral Cry"))
                {
                    if (ancestralCry != null && ancestralCry.IsOnSkillBar && ancestralCry.Slot != -1 && ancestralCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(ancestralCry.Slot, false, false);
					GlobalLog.Debug($"Casting Ancestral Cry");
                }
            }
        }*/
        public static void AncestralCry()
        {
            if (!_ancestralCryStopwatch.IsRunning || _ancestralCryStopwatch.ElapsedMilliseconds >500)
            {
                var ancestralCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "AncestralCry");
                if (ancestralCry != null && ancestralCry.CanUse())
                {
                    var leader = FollowBot.Leader;
                    bool meNeeds = LokiPoe.Me.Auras.All(x => (x.Name == "Ancestral Cry" && x.TimeLeft.Seconds <= 3) || x.Name != "Ancestral Cry");
                    bool leaderNeeds = leader != null && leader.Auras.All(x => (x.Name == "Ancestral Cry" && x.TimeLeft.Seconds <= 3) || x.Name != "Ancestral Cry");
                    if (meNeeds || (leaderNeeds && leader != null && leader.Distance <= 60))
                    {
                        if (ancestralCry != null && ancestralCry.IsOnSkillBar && ancestralCry.Slot != -1 && ancestralCry.CanUse())
                        {
                            LokiPoe.InGameState.SkillBarHud.Use(ancestralCry.Slot, false, false);
                            _ancestralCryStopwatch.Restart();
                        }
                    }
                }
            }
        }

        /*public static void IntimidatingCry()
        {
            var intimidatingCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "IntimidatingCry");
            if (intimidatingCry != null && intimidatingCry.CanUse())
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Intimidating Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Intimidating Cry"))
                {
                    if (intimidatingCry != null && intimidatingCry.IsOnSkillBar && intimidatingCry.Slot != -1 && intimidatingCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(intimidatingCry.Slot, false, false);
					GlobalLog.Debug($"Casting Intimidating Cry");
                }
            }
        }*/
        public static void IntimidatingCry()
        {
            if (!_intimidatingCryStopwatch.IsRunning || _intimidatingCryStopwatch.ElapsedMilliseconds > 500)
            {
                var intimidatingCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "IntimidatingCry");
                if (intimidatingCry != null && intimidatingCry.CanUse())
                {
                    var leader = FollowBot.Leader;
                    bool meNeeds = LokiPoe.Me.Auras.All(x => (x.Name == "Intimidating Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Intimidating Cry");
                    bool leaderNeeds = leader != null && leader.Auras.All(x => (x.Name == "Intimidating Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Intimidating Cry");
                    if (meNeeds || (leaderNeeds && leader != null && leader.Distance <= 60))
                    {
                        if (intimidatingCry != null && intimidatingCry.IsOnSkillBar && intimidatingCry.Slot != -1 && intimidatingCry.CanUse())
                        {
                            LokiPoe.InGameState.SkillBarHud.Use(intimidatingCry.Slot, false, false);
                            _intimidatingCryStopwatch.Restart();
                        }
                    }
                }
            }
        }

        public static void RallyingCry()
        {
            if (!_rallyingCryStopwatch.IsRunning || _rallyingCryStopwatch.ElapsedMilliseconds > 500)
            {
                var RallyingCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "RallyingCryNew");
                if (RallyingCry != null && RallyingCry.CanUse())
                {
                    var linkLeader = FollowBot.Leader;
                    if (linkLeader != null && linkLeader.Distance <= 60 && linkLeader.Auras.All(x => (x.Name == "Rallied" && x.TimeLeft.Seconds <= 2) || x.Name != "Rallied"))
                    {
                        if (RallyingCry != null && RallyingCry.IsOnSkillBar && RallyingCry.Slot != -1 && RallyingCry.CanUse())
                        {
                            LokiPoe.InGameState.SkillBarHud.Use(RallyingCry.Slot, false, false);
                            _rallyingCryStopwatch.Restart();
                        }
                    }
                }
            }
        }

        /*public static void InfernalCry()
        {
            var infernalCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "InfernalCry");
            if (infernalCry != null && infernalCry.CanUse())
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Infernal Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Infernal Cry"))
                {
                    if (infernalCry != null && infernalCry.IsOnSkillBar && infernalCry.Slot != -1 && infernalCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(infernalCry.Slot, false, false);
					GlobalLog.Debug($"Casting Infernal Cry");
                }
            }
        }*/
        public static void InfernalCry()
        {
            if (!_infernalCryStopwatch.IsRunning || _infernalCryStopwatch.ElapsedMilliseconds > 500)
            {
                var infernalCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "InfernalCry");
                if (infernalCry != null && infernalCry.CanUse())
                {
                    var leader = FollowBot.Leader;
                    bool meNeeds = LokiPoe.Me.Auras.All(x => (x.Name == "Infernal Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Infernal Cry");
                    bool leaderNeeds = leader != null && leader.Auras.All(x => (x.Name == "Infernal Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Infernal Cry");
                    if (meNeeds || (leaderNeeds && leader != null && leader.Distance <= 60))
                    {
                        if (infernalCry != null && infernalCry.IsOnSkillBar && infernalCry.Slot != -1 && infernalCry.CanUse())
                        {
                            LokiPoe.InGameState.SkillBarHud.Use(infernalCry.Slot, false, false);
                            _infernalCryStopwatch.Restart();
                        }
                    }
                }
            }
        }
        #endregion

        #region Guardian's Blessing
        /*
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

        public static void GuardiansBlessingHatredGolem()
        {
            var guardiansBlessing = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "CastAuraColdDamage");
            var relicSkill2 = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SummonChaosElemental");

            GlobalLog.Debug($"GuardiansBlessingHatredGolem LinkedDisplayString: '{guardiansBlessing.LinkedDisplayString}'");
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
        */

        public static void GuardiansBlessingHandler()
        {
            var allowedAuras = new[] {
                "Anger", "Clarity", "Determination", "Discipline", "Grace", "Haste", "Hatred", "Malevolence", "Precision",
                "Purity of Elements", "Purity of Fire", "Purity of Ice", "Purity of Lightning", "Vitality", "Wrath", "Zealotry",
                "Pride"
            };
            foreach (var auraSkill in LokiPoe.InGameState.SkillBarHud.SkillBarSkills
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
                var minionName = FollowBotSettings.Instance.GuardiansBlessingMinion;
                if (string.IsNullOrEmpty(minionName))
                    continue;

                // Find the specified minion skill
                var minionSkill = LokiPoe.InGameState.SkillBarHud.SkillBarSkills
                    .FirstOrDefault(x => x != null && x.Name.Equals(minionName, System.StringComparison.OrdinalIgnoreCase));

                if (minionSkill == null)
                    continue;

                var minionObj = minionSkill.DeployedObjects.FirstOrDefault() as Monster;
                if (minionObj == null)
                    continue;

                if (!auraSkill.CanUse())
                    continue;

                GlobalLog.Debug($"[GB] Casting \"{auraSkill.Name}\" - Guardian's Blessing for {expectedAura}");
                LokiPoe.InGameState.SkillBarHud.Use(auraSkill.Slot, false, false);
                break; // Only cast one per call
            }
        }
        #endregion

        #region Utility Skills
        public static void Convocation()
        {
            var Convocation = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "convocation");
            if (Convocation != null && Convocation.CanUse())
            {
                LokiPoe.InGameState.SkillBarHud.Use(Convocation.Slot, false, false);
            }
        }
        
        public static void ChaosElixir()
        {
            if (LokiPoe.Me.Auras.All(x => (x.Name != "Coruscating Elixir") || (x.Name == "Coruscating Elixir" && x.TimeLeft.Seconds <= 1.3)))
            {
                LokiPoe.InGameState.QuickFlaskHud.UseFlaskInSlot(FollowBotSettings.Instance.ChaosElixirFlaskSlot);
            }
        }

        public static void RejuvenationTotem()
        {
            var rejuvTotem = LokiPoe.InGameState.SkillBarHud.SkillBarSkills
                .FirstOrDefault(x => x != null && x.InternalName == "TotemAuraLifeRegen");

            if (rejuvTotem == null || !rejuvTotem.CanUse() || rejuvTotem.NumberDeployed > 0)
                return;

            var settings = FollowBotSettings.Instance;

            // Override: Always cast during Ultimatum if enabled
            if (settings.RejuvenationTotemAlwaysUseInUltimatum)
            {
                var ultimatum = LokiPoe.ObjectManager.GetObjectsByType<UltimatumChallengeInteractable>().FirstOrDefault();
                if (ultimatum != null && ultimatum.IsTrialActive && !ultimatum.IsTrialCompleted)
                {
                    LokiPoe.InGameState.SkillBarHud.Use(rejuvTotem.Slot, false, false);
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
                            LokiPoe.InGameState.SkillBarHud.Use(rejuvTotem.Slot, false, false);
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
                LokiPoe.InGameState.SkillBarHud.Use(rejuvTotem.Slot, false, false);
                GlobalLog.Debug($"[CustomSkills] Casting Rejuvenation Totem.");
            }
        }
        #endregion

        #region Banner Skills
        public static void ComprehensiveBannerHandler()
        {
            if (!FollowBotSettings.Instance.EnableComprehensiveBanner)
                return;

            // Determine activation conditions
            bool isUltimatumActive = false;
            if (FollowBotSettings.Instance.UseBannersInUltimatum)
            {
                var ultimatum = LokiPoe.ObjectManager.GetObjectsByType<UltimatumChallengeInteractable>().FirstOrDefault();
                isUltimatumActive = ultimatum != null && ultimatum.IsTrialActive && !ultimatum.IsTrialCompleted;
            }

            bool isBlightActive = false;
            if (FollowBotSettings.Instance.UseBannersInBlight)
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
                if (FollowBotSettings.Instance.UseBannersNearUniques)
                {
                    if (LokiPoe.ObjectManager.GetObjectsByType<Monster>().Any(m => m.Rarity == Rarity.Unique && m.IsAliveHostile && m.Distance <= 60 && m.IsTargetable))
                        useBanner = true;
                }
                if (!useBanner && FollowBotSettings.Instance.UseBannersNearRares)
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
            if (FollowBotSettings.Instance.UseWarBanner && !LokiPoe.Me.Auras.Any(x => x.InternalName == "bloodstained_banner_buff_aura") && valourCharges < FollowBotSettings.Instance.WarBannerCharges) needsValor = true;
            else if (FollowBotSettings.Instance.UseDefianceBanner && !LokiPoe.Me.Auras.Any(x => x.InternalName == "armour_evasion_banner_buff_aura") && valourCharges < FollowBotSettings.Instance.DefianceBannerCharges) needsValor = true;
            else if (FollowBotSettings.Instance.UseDreadBanner && !LokiPoe.Me.Auras.Any(x => x.InternalName == "puresteel_banner_buff_aura") && valourCharges < FollowBotSettings.Instance.DreadBannerCharges) needsValor = true;

            if (needsValor)
            {
                bool shouldGenerateValor = false;
                if (isUltimatumActive && FollowBotSettings.Instance.GenerateValorInUltimatum)
                {
                    shouldGenerateValor = true;
                }
                else if (isBlightActive && FollowBotSettings.Instance.GenerateValorInBlight)
                {
                    shouldGenerateValor = true;
                }
                else if (useBanner && FollowBotSettings.Instance.GenerateValorNearUniques) // Fallback for uniques if not in blight/ult
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
            if (FollowBotSettings.Instance.UseWarBanner)
            {
                bool hasWarBannerBuff = LokiPoe.Me.Auras.Any(x => x.InternalName == "bloodstained_banner_buff_aura");
                if (!hasWarBannerBuff && valourCharges >= FollowBotSettings.Instance.WarBannerCharges)
                {
                    var warBanner = SkillBar.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "BloodstainedBanner");
                    if (warBanner != null && warBanner.CanUse())
                    {
                        GlobalLog.Debug($"Using War Banner");
                        SkillBar.Use(warBanner.Slot, false, false);
                        return;
                    }
                }
            }

            // 2. Defiance Banner
            if (FollowBotSettings.Instance.UseDefianceBanner)
            {
                bool hasWarBannerBuff = LokiPoe.Me.Auras.Any(x => x.InternalName == "bloodstained_banner_buff_aura");
                bool hasDefianceBannerBuff = LokiPoe.Me.Auras.Any(x => x.InternalName == "armour_evasion_banner_buff_aura");
                if (hasWarBannerBuff && !hasDefianceBannerBuff && valourCharges >= FollowBotSettings.Instance.DefianceBannerCharges)
                {
                    var defianceBanner = SkillBar.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "ArmourEvasionBanner");
                    if (defianceBanner != null && defianceBanner.CanUse())
                    {
                        GlobalLog.Debug($"Using Defiance Banner");
                        SkillBar.Use(defianceBanner.Slot, false, false);
                        return;
                    }
                }
            }

            // 3. Dread Banner
            if (FollowBotSettings.Instance.UseDreadBanner)
            {
                bool hasWarBannerBuff = LokiPoe.Me.Auras.Any(x => x.InternalName == "bloodstained_banner_buff_aura");
                bool hasDefianceBannerBuff = LokiPoe.Me.Auras.Any(x => x.InternalName == "armour_evasion_banner_buff_aura");
                bool hasDreadBannerBuff = LokiPoe.Me.Auras.Any(x => x.InternalName == "puresteel_banner_buff_aura");
                if (hasWarBannerBuff && hasDefianceBannerBuff && !hasDreadBannerBuff && valourCharges >= FollowBotSettings.Instance.DreadBannerCharges)
                {
                    var dreadBanner = SkillBar.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "PuresteelBanner");
                    if (dreadBanner != null && dreadBanner.CanUse())
                    {
                        GlobalLog.Debug($"Using Dread Banner");
                        SkillBar.Use(dreadBanner.Slot, false, false);
                        return;
                    }
                }
            }
        }
        #endregion

        #region Helper Methods
        private static void UseWarcryForValor()
        {
            if (_valorWarcryStopwatch.IsRunning && _valorWarcryStopwatch.ElapsedMilliseconds < 500)
                return;

            var availableWarcries = new List<DreamPoeBot.Loki.RemoteMemoryObjects.Skill>();
            var settings = FollowBotSettings.Instance;

            if (settings.EnableEnduringCry)
            {
                var cry = SkillBar.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "EnduringCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableSeismicCry)
            {
                var cry = SkillBar.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "SeismicCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableBattlemageCry)
            {
                var cry = SkillBar.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "DivineCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableAncestralCry)
            {
                var cry = SkillBar.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "AncestralCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableIntimidatingCry)
            {
                var cry = SkillBar.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "IntimidatingCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableInfernalCry)
            {
                var cry = SkillBar.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "InfernalCry" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }
            if (settings.EnableRallyingCry)
            {
                var cry = SkillBar.SkillBarSkills.FirstOrDefault(s => s != null && s.InternalName == "RallyingCryNew" && s.CanUse());
                if (cry != null) availableWarcries.Add(cry);
            }

            if (availableWarcries.Any())
            {
                var warcryToUse = availableWarcries[_random.Next(availableWarcries.Count)];
                if (warcryToUse.CanUse())
                {
                    GlobalLog.Debug($"[UseWarcryForValor] Using {warcryToUse.Name}.");
                    SkillBar.Use(warcryToUse.Slot, false, false);
                    _valorWarcryStopwatch.Restart();
                }
            }
        }
        #endregion
    }
}