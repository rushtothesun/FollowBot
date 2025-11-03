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
    }
}