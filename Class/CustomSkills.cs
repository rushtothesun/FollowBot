using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using DreamPoeBot.Loki.Bot;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;
using SkillBar = DreamPoeBot.Loki.Game.LokiPoe.InGameState.SkillBarHud;

namespace FollowBot.Class
{
    public static class CustomSkills
    {
        private static readonly Stopwatch _enduringCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _seismicCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _battlemageCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _ancestralCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _intimidatingCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _rallyingCryStopwatch = new Stopwatch();
        private static readonly Stopwatch _infernalCryStopwatch = new Stopwatch();

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

        #region Crys
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
        #region Guardians Blessing
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

        public static void Convocation()
        {
            var Convocation = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "convocation");
            if (Convocation != null && Convocation.CanUse())
            {
                LokiPoe.InGameState.SkillBarHud.Use(Convocation.Slot, false, false);
            }
        }

        public static async void LinkSkillHandler()
        {
            var linkSkills = new[]
            {
                "Intuitive Link", "Vampiric Link", "Destructive Link",
                "Soul Link", "Protective Link", "Flame Link"
            };

            var linkSkill = LokiPoe.InGameState.SkillBarHud.SkillBarSkills
                .FirstOrDefault(s => s != null && linkSkills.Contains(s.Name));

            if (linkSkill == null || !linkSkill.CanUse())
                return;

            // If we have a link source buff that is about to expire, we should refresh.
            var needsRefresh = LokiPoe.Me.Auras.Any(x => linkSkills.Contains(x.Name) && x.TimeLeft.Seconds <= 4);

            // Target the leader
            var leader = FollowBot.Leader;
            if (leader != null && leader.Distance <= 60)
            {
                bool hasLink = leader.Auras.Any(a => linkSkills.Contains(a.Name) && a.TimeLeft.TotalSeconds >= 4);
                if (!hasLink || needsRefresh)
                {
                    var useResult = LokiPoe.InGameState.SkillBarHud.UseOn(linkSkill.Slot, false, leader, false);
                    if (useResult == LokiPoe.InGameState.UseResult.CouldNotHighlight)
                    {
                        LokiPoe.InGameState.SkillBarHud.UseAt(linkSkill.Slot, false, leader.Position, false);
                        await Task.Delay(500);
                        return; // Use at leader and exit
                    }
                    await Task.Delay(500);
                    return; // Use on leader and exit
                }
            }

            // Handle additional targets
            string additionalTargets = FollowBotSettings.Instance.LinkSkillAdditionalTargets;
            if (!string.IsNullOrEmpty(additionalTargets))
            {
                var targetNames = additionalTargets.Split(',').Select(s => s.Trim());
                foreach (var targetName in targetNames)
                {
                    if (string.IsNullOrEmpty(targetName)) continue;

                    var targetPlayer = LokiPoe.ObjectManager.GetObjectsByType<Player>()
                        .FirstOrDefault(p => p.Name.Equals(targetName, System.StringComparison.OrdinalIgnoreCase));

                    if (targetPlayer != null && targetPlayer.Distance <= 60)
                    {
                        bool hasLink = targetPlayer.Auras.Any(a => linkSkills.Contains(a.Name) && a.TimeLeft.TotalSeconds >= 4);
                        if (!hasLink || needsRefresh)
                        {
                            var useResult2 = LokiPoe.InGameState.SkillBarHud.UseOn(linkSkill.Slot, false, targetPlayer, false);
                            if (useResult2 == LokiPoe.InGameState.UseResult.CouldNotHighlight)
                            {
                                LokiPoe.InGameState.SkillBarHud.UseAt(linkSkill.Slot, false, targetPlayer.Position, false);
                                await Task.Delay(500);
                                return; // Use at target and exit
                            }
                            await Task.Delay(500);
                            return; // Cast on one additional target per tick
                        }
                    }
                }
            }
        }

        public static void RejuvenationTotem()
        {
            var rejuvTotem = LokiPoe.InGameState.SkillBarHud.SkillBarSkills
                .FirstOrDefault(x => x != null && x.InternalName == "TotemAuraLifeRegen");

            if (rejuvTotem == null || !rejuvTotem.CanUse() || rejuvTotem.NumberDeployed > 0)
                return;

            var leader = FollowBot.Leader;
            if (leader == null || leader.Distance > 60)
                return;

            var unreservedLeaderHealth = leader.MaxHealth - leader.HealthReserved;
            var leaderHealthPercentage = unreservedLeaderHealth > 0 ? ((double)leader.Health / unreservedLeaderHealth) * 100 : 100;

            bool leaderNeedsHelp = leaderHealthPercentage < 70;
            
            bool selfNeedsHelp;

            if (LokiPoe.Me.EnergyShieldMax >= 1000)
            {
                selfNeedsHelp = LokiPoe.Me.EnergyShieldPercent < 70;
            }
            else
            {
                var unreservedSelfHealth = LokiPoe.Me.MaxHealth - LokiPoe.Me.HealthReserved;
                var selfHealthPercentage = unreservedSelfHealth > 0 ? ((double)LokiPoe.Me.Health / unreservedSelfHealth) * 100 : 100;
                selfNeedsHelp = selfHealthPercentage < 70;
            }

            if (leaderNeedsHelp || selfNeedsHelp)
            {
                LokiPoe.InGameState.SkillBarHud.Use(rejuvTotem.Slot, false, false);
                GlobalLog.Debug($"[CustomSkills] Casting Rejuvenation Totem.");
            }
        }

        public static void UseRejuvenationTotemDuringUltimatum()
        {
            // Find the Ultimatum trial object in the area
            var ultimatum = LokiPoe.ObjectManager.GetObjectsByType<UltimatumChallengeInteractable>().FirstOrDefault();
            if (ultimatum != null && ultimatum.IsTrialActive && !ultimatum.IsTrialCompleted)
            {
                // Find the Rejuvenation Totem skill on the skill bar
                var rejuvTotem = LokiPoe.InGameState.SkillBarHud.SkillBarSkills
                    .FirstOrDefault(x => x != null && x.InternalName == "TotemAuraLifeRegen");

                // Only cast if not already deployed and can use
                if (rejuvTotem != null && rejuvTotem.CanUse() && rejuvTotem.NumberDeployed == 0)
                {
                    LokiPoe.InGameState.SkillBarHud.Use(rejuvTotem.Slot, false, false);
                }
            }
        }

        public static void UseWarBannerDuringUltimatumOrNearUnique()
        {
            // Check for Ultimatum trial in progress
            var ultimatum = LokiPoe.ObjectManager.GetObjectsByType<UltimatumChallengeInteractable>().FirstOrDefault();
            bool isUltimatumActive = ultimatum != null && ultimatum.IsTrialActive && !ultimatum.IsTrialCompleted;

            // Check for nearby unique monster (within 60 units)
            bool isNearUniqueMonster = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Any(m => m.Rarity == Rarity.Unique && m.IsAliveHostile && m.Distance <= 60 && m.IsTargetable);

            // Only proceed if either condition is true
            if (isUltimatumActive || isNearUniqueMonster)
            {
                // Find the War Banner skill on the skill bar
                var warBanner = LokiPoe.InGameState.SkillBarHud.SkillBarSkills
                    .FirstOrDefault(x => x != null && x.InternalName == "BloodstainedBanner");

                if (warBanner != null && warBanner.CanUse())
                {
                    // Check for Valour buff with at least 105 charges
                    var valourBuff = LokiPoe.Me.Auras.FirstOrDefault(x => x.InternalName == "valour");
                    int valourCharges = valourBuff?.Charges ?? 0;

                    // Check if War Banner buff is missing
                    bool hasWarBannerBuff = LokiPoe.Me.Auras.Any(x => x.InternalName == "bloodstained_banner_buff_aura");

                    if (valourCharges >= 105 && !hasWarBannerBuff)
                    {
                        LokiPoe.InGameState.SkillBarHud.Use(warBanner.Slot, false, false);
                    }
                }
            }
        }

        public static void UseWarDefianceBannerDuringUltimatumOrNearUnique()
        {
            // Check for Ultimatum trial in progress
            var ultimatum = LokiPoe.ObjectManager.GetObjectsByType<UltimatumChallengeInteractable>().FirstOrDefault();
            bool isUltimatumActive = ultimatum != null && ultimatum.IsTrialActive && !ultimatum.IsTrialCompleted;

            // Check for nearby unique monster (within 60 units)
            bool isNearUniqueMonster = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Any(m => m.Rarity == Rarity.Unique && m.IsAliveHostile && m.Distance <= 60 && m.IsTargetable);

            // Only proceed if either condition is true
            if (isUltimatumActive || isNearUniqueMonster)
            {
                // Check Valour buff and charges
                var valourBuff = LokiPoe.Me.Auras.FirstOrDefault(x => x.InternalName == "valour");
                int valourCharges = valourBuff?.Charges ?? 0;

                // Check if War Banner buff is present
                bool hasWarBannerBuff = LokiPoe.Me.Auras.Any(x => x.InternalName == "bloodstained_banner_buff_aura");

                // Try to use War Banner first (requires 105 Valour and not already placed)
                var warBanner = LokiPoe.InGameState.SkillBarHud.SkillBarSkills
                    .FirstOrDefault(x => x != null && x.InternalName == "BloodstainedBanner");

                if (warBanner != null && warBanner.CanUse() && valourCharges >= 105 && !hasWarBannerBuff)
                {
                    LokiPoe.InGameState.SkillBarHud.Use(warBanner.Slot, false, false);
                    return; // Prioritize War Banner, do not use Defiance Banner in the same tick
                }

                // If War Banner is already placed, try Defiance Banner (requires 80 Valour and not already placed)
                bool hasDefianceBannerBuff = LokiPoe.Me.Auras.Any(x => x.InternalName == "armour_evasion_banner_buff_aura");
                var defianceBanner = LokiPoe.InGameState.SkillBarHud.SkillBarSkills
                    .FirstOrDefault(x => x != null && x.InternalName == "ArmourEvasionBanner");

                if (defianceBanner != null && defianceBanner.CanUse() && valourCharges >= 80 && hasWarBannerBuff && !hasDefianceBannerBuff)
                {
                    LokiPoe.InGameState.SkillBarHud.Use(defianceBanner.Slot, false, false);
                }
            }
        }
    }
}