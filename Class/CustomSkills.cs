using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using System.Linq;

namespace FollowBot.Class
{
    public static class CustomSkills
    {
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
            if (enduringCry != null && enduringCry.CanUse())
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name == "Onslaught" && x.TimeLeft.Seconds <= 1) || x.Name != "Enduring Cry" || x.Name != "Onslaught" || (x.Name == "Enduring Cry" && x.TimeLeft.Seconds <= 4)))
                {
                    if (enduringCry != null && enduringCry.IsOnSkillBar && enduringCry.Slot != -1 && enduringCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(enduringCry.Slot, false, false);
                    GlobalLog.Debug($"Casting Enduring Cry");
                }
            }
        }

        /*public static void EnduringCryBeforeCluster()
        {
            var enduringCryBefore = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "EnduringCry");
            if (enduringCryBefore != null)
            {
                if (LokiPoe.Me.Auras.All(x => (x.Name != "Enduring Cry" || (x.Name == "Enduring Cry" && x.TimeLeft.Seconds <= 4))))
                {
                    if (enduringCryBefore != null && enduringCryBefore.IsOnSkillBar && enduringCryBefore.Slot != -1 && enduringCryBefore.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(enduringCryBefore.Slot, false, false);
					GlobalLog.Debug($"Casting Enduring Cry Before Cluster");
                }
            }
        }*/

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
            var seismicCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SeismicCry");
            if (seismicCry != null && seismicCry.CanUse())
            {
                var leader = FollowBot.Leader;
                bool meNeeds = LokiPoe.Me.Auras.All(x => (x.Name == "Seismic Cry" && x.TimeLeft.Seconds <= 4) || x.Name != "Seismic Cry");
                bool leaderNeeds = leader != null && leader.Auras.All(x => (x.Name == "Seismic Cry" && x.TimeLeft.Seconds <= 4) || x.Name != "Seismic Cry");
                if (meNeeds || (leaderNeeds && leader != null && leader.Distance <= 60))
                {
                    if (seismicCry != null && seismicCry.IsOnSkillBar && seismicCry.Slot != -1 && seismicCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(seismicCry.Slot, false, false);
                    GlobalLog.Debug($"Casting Seismic Cry");
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
            var battlemageCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "DivineCry");
            if (battlemageCry != null && battlemageCry.CanUse())
            {
                var leader = FollowBot.Leader;
                bool meNeeds = LokiPoe.Me.Auras.All(x => (x.Name == "Battlemage's Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Battlemage's Cry");
                bool leaderNeeds = leader != null && leader.Auras.All(x => (x.Name == "Battlemage's Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Battlemage's Cry");
                if (meNeeds || (leaderNeeds && leader != null && leader.Distance <= 60))
                {
                    if (battlemageCry != null && battlemageCry.IsOnSkillBar && battlemageCry.Slot != -1 && battlemageCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(battlemageCry.Slot, false, false);
                    GlobalLog.Debug($"Casting Battlemage's Cry");
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
            var ancestralCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "AncestralCry");
            if (ancestralCry != null && ancestralCry.CanUse())
            {
                var leader = FollowBot.Leader;
                bool meNeeds = LokiPoe.Me.Auras.All(x => (x.Name == "Ancestral Cry" && x.TimeLeft.Seconds <= 3) || x.Name != "Ancestral Cry");
                bool leaderNeeds = leader != null && leader.Auras.All(x => (x.Name == "Ancestral Cry" && x.TimeLeft.Seconds <= 3) || x.Name != "Ancestral Cry");
                if (meNeeds || (leaderNeeds && leader != null && leader.Distance <= 60))
                {
                    if (ancestralCry != null && ancestralCry.IsOnSkillBar && ancestralCry.Slot != -1 && ancestralCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(ancestralCry.Slot, false, false);
                    GlobalLog.Debug($"Casting Ancestral Cry");
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
            var intimidatingCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "IntimidatingCry");
            if (intimidatingCry != null && intimidatingCry.CanUse())
            {
                var leader = FollowBot.Leader;
                bool meNeeds = LokiPoe.Me.Auras.All(x => (x.Name == "Intimidating Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Intimidating Cry");
                bool leaderNeeds = leader != null && leader.Auras.All(x => (x.Name == "Intimidating Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Intimidating Cry");
                if (meNeeds || (leaderNeeds && leader != null && leader.Distance <= 60))
                {
                    if (intimidatingCry != null && intimidatingCry.IsOnSkillBar && intimidatingCry.Slot != -1 && intimidatingCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(intimidatingCry.Slot, false, false);
                    GlobalLog.Debug($"Casting Intimidating Cry");
                }
            }
        }

        public static void RallyingCry()
        {
            var RallyingCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "RallyingCryNew");
            if (RallyingCry != null && RallyingCry.CanUse())
            {
                var linkLeader = FollowBot.Leader;
                if (linkLeader != null && linkLeader.Distance <= 60 && linkLeader.Auras.All(x => (x.Name == "Rallied" && x.TimeLeft.Seconds <= 2) || x.Name != "Rallied"))
                {
                    if (RallyingCry != null && RallyingCry.IsOnSkillBar && RallyingCry.Slot != -1 && RallyingCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(RallyingCry.Slot, false, false);
                    GlobalLog.Debug($"Casting Rallying Cry");
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
            var infernalCry = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "InfernalCry");
            if (infernalCry != null && infernalCry.CanUse())
            {
                var leader = FollowBot.Leader;
                bool meNeeds = LokiPoe.Me.Auras.All(x => (x.Name == "Infernal Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Infernal Cry");
                bool leaderNeeds = leader != null && leader.Auras.All(x => (x.Name == "Infernal Cry" && x.TimeLeft.Seconds <= 2) || x.Name != "Infernal Cry");
                if (meNeeds || (leaderNeeds && leader != null && leader.Distance <= 60))
                {
                    if (infernalCry != null && infernalCry.IsOnSkillBar && infernalCry.Slot != -1 && infernalCry.CanUse())
                        LokiPoe.InGameState.SkillBarHud.Use(infernalCry.Slot, false, false);
                    GlobalLog.Debug($"Casting Infernal Cry");
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
            var allowedAuras = new[] { "Haste", "Hatred", "Malevolence", "Determination", "Anger", "Wrath", "Zealotry" };
            var minionSkillNames = new[] { "SummonRelic", "SummonChaosElemental" };

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

                // Find the minion skill (must be on bar and deployed)
                var minionSkill = LokiPoe.InGameState.SkillBarHud.SkillBarSkills
                    .FirstOrDefault(x => x != null && minionSkillNames.Contains(x.InternalName));
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

        public static void CallMercenary()
        {
            var MercRecall = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "CallMercenary");
            if (MercRecall != null && MercRecall.CanUse())
            {
                LokiPoe.InGameState.SkillBarHud.Use(MercRecall.Slot, false, false);
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


        public static void SoulLink2()
        {
            var link = LokiPoe.InGameState.SkillBarHud.SkillBarSkills.FirstOrDefault(x => x != null && x.InternalName == "SoulLink");
            if (link != null && link.CanUse())
            {
                string targetName = FollowBotSettings.Instance.SoulLink2TargetName;
                if (!string.IsNullOrEmpty(targetName))
                {
                    Player thePlayerToInvestigate = LokiPoe.ObjectManager.GetObjectsByType<Player>().FirstOrDefault(x => x.Name == targetName);
                    if (thePlayerToInvestigate != null && thePlayerToInvestigate.Distance <= 60)
                    {
                        if (thePlayerToInvestigate.Auras.All(x => (x.Name != "Soul Link" || (x.Name == "Soul Link" && x.TimeLeft.Seconds <= 3))))
                        {
                            LokiPoe.InGameState.SkillBarHud.UseOn(link.Slot, false, thePlayerToInvestigate, false);
                        }
                    }
                }
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