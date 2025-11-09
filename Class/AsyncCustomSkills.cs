using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using System.Linq;
using System.Threading.Tasks;
using SkillBar = DreamPoeBot.Loki.Game.LokiPoe.InGameState.SkillBarHud;

namespace FollowBot.Class
{
    public static class AsyncCustomSkills
    {

        public static async Task LinkSkillHandler()
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
                        await Coroutines.ReactionWait();
                        return; // Use at leader and exit
                    }
                    await Coroutines.ReactionWait();
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
                                await Coroutines.ReactionWait();
                                return; // Use at target and exit
                            }
                            await Coroutines.ReactionWait();
                            return; // Cast on one additional target per tick
                        }
                    }
                }
            }
        }

        public static async Task SummonRagingSpirits()
        {
            var settings = FollowBotSettings.Instance;

            var srsSkill = SkillBar.Skills.FirstOrDefault(s => s.IsOnSkillBar && s.Name == "Summon Raging Spirit");
            if (srsSkill == null || !srsSkill.CanUse())
                return;

            var leader = FollowBot.Leader;
            if (leader == null || leader.Distance > settings.SrsCustomDistance)
                return;

            if (srsSkill.DeployedObjects.Count >= settings.MinRagingSpirits)
                return;

            bool monsterNearby = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Any(m => m.IsHostile && !m.IsHidden && !m.IsDead && m.IsTargetable && m.Distance <= settings.SrsMonsterDistance &&
                           ((m.Rarity == Rarity.Rare || m.Rarity == Rarity.Unique) ||
                            (settings.SrsOnNormalMagic && (m.Rarity == Rarity.Normal || m.Rarity == Rarity.Magic))));

            if (monsterNearby)
            {
                SkillBar.Use(srsSkill.Slot, false, false);
                await Coroutines.ReactionWait();
            }
        }

        public static async Task SummonSkeletons()
        {
            var settings = FollowBotSettings.Instance;

            var skeletonSkill = SkillBar.Skills.FirstOrDefault(s => s.IsOnSkillBar && s.Name == "Summon Skeletons");
            if (skeletonSkill == null || !skeletonSkill.CanUse())
                return;

            var leader = FollowBot.Leader;
            if (leader == null || leader.Distance > settings.SkeletonsCustomDistance)
                return;

            if (skeletonSkill.DeployedObjects.Count >= settings.MinSkeletons)
                return;

            bool monsterNearby = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Any(m => m.IsHostile && !m.IsHidden && !m.IsDead && m.IsTargetable && m.Distance <= settings.SkeletonsMonsterDistance &&
                           ((m.Rarity == Rarity.Rare || m.Rarity == Rarity.Unique) ||
                            (settings.SkeletonsOnNormalMagic && (m.Rarity == Rarity.Normal || m.Rarity == Rarity.Magic))));

            if (monsterNearby)
            {
                SkillBar.Use(skeletonSkill.Slot, false, false);
                await Coroutines.ReactionWait();
            }
        }

        public static async Task BreachGraft1()
        {
            var settings = FollowBotSettings.Instance;

            var skill = SkillBar.Skills.FirstOrDefault(s => s.IsOnSkillBar && s.InternalName == "ManualGraftTrigger");
            if (skill == null || !skill.CanUse())
                return;

            var leader = FollowBot.Leader;
            if (leader == null || leader.Distance > settings.BreachGraft1CustomDistance)
                return;

            bool monsterNearby = LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Any(m => m.IsHostile && !m.IsHidden && !m.IsDead && m.IsTargetable && m.Distance <= settings.BreachGraft1MonsterDistance &&
                           ((m.Rarity == Rarity.Rare || m.Rarity == Rarity.Unique) ||
                            (settings.BreachGraft1OnNormalMagic && (m.Rarity == Rarity.Normal || m.Rarity == Rarity.Magic))));

            if (monsterNearby)
            {
                SkillBar.Use(skill.Slot, false, false);
                await Coroutines.ReactionWait();
            }
        }

        public static async Task SentinelUsage()
        {
            var sentinelSkill = SkillBar.Skills.FirstOrDefault(s => s != null && s.InternalName == "SummonRadiantSentinel");
            if (sentinelSkill == null || !sentinelSkill.CanUse())
                return;

            var sentinelObj = sentinelSkill.DeployedObjects.FirstOrDefault() as Monster;
            if (sentinelObj == null)
            {
                GlobalLog.Debug($"Casting \"{sentinelSkill.Name}\" - Sentinel");
                SkillBar.Use(sentinelSkill.Slot, false, false);
                await Coroutines.ReactionWait();
            }
        }
    }
}