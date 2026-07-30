using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SkillBar = DreamPoeBot.Loki.Game.LokiPoe.InGameState.SkillBarHud;

namespace FollowBot.Class
{
    public static class AsyncCustomSkills
    {
        private static readonly string[] LinkSkillNames =
        {
            "Intuitive Link", "Vampiric Link", "Destructive Link",
            "Soul Link", "Protective Link", "Flame Link"
        };

        // Tracks when we last successfully cast a link on each target
        private static readonly Dictionary<string, DateTime> _linkCastTimes = new Dictionary<string, DateTime>();

        // Refresh threshold: recast when 60% of the duration has elapsed
        private const double RefreshPercent = 0.60;

        private static bool NeedsLink(string targetName, double durationMs)
        {
            if (!_linkCastTimes.TryGetValue(targetName, out var lastCast))
                return true;

            var elapsed = (DateTime.UtcNow - lastCast).TotalMilliseconds;
            return elapsed >= durationMs * RefreshPercent;
        }

        private static bool HasLinkBuff(Player target)
        {
            return target.Auras.Any(a => LinkSkillNames.Contains(a.Name));
        }

        private static bool TryCastLinkOn(int slot, Player target, string targetName)
        {
            var result = LokiPoe.InGameState.SkillBarHud.UseOn(slot, false, target, false);
            if (result == LokiPoe.InGameState.UseResult.None)
            {
                _linkCastTimes[targetName] = DateTime.UtcNow;
                GlobalLog.Debug($"[LinkSkill] Linked {targetName} (UseOn success)");
                return true;
            }

            GlobalLog.Debug($"[LinkSkill] UseOn failed on {targetName}: {result}");
            return false;
        }

        public static async Task LinkSkillHandler()
        {
            if (CustomSkills.IsOnCooldown("LinkSkill", 200, 400))
                return;

            var linkSkill = LokiPoe.InGameState.SkillBarHud.SkillBarSkills
                .FirstOrDefault(s => s != null && LinkSkillNames.Contains(s.Name));

            if (linkSkill == null || !linkSkill.CanUse())
                return;

            // Read actual link duration from skill stats (SkillEffectDuration in ms)
            int durationMs;
            if (linkSkill.Stats.TryGetValue(StatTypeGGG.SkillEffectDuration, out var dur))
                durationMs = dur;
            else
                durationMs = 8000; // fallback 8s

            // Build target list: leader first, then additional targets
            var targets = new List<KeyValuePair<string, Player>>();

            var leader = FollowBot.Leader;
            if (leader != null && leader.Distance <= 60)
                targets.Add(new KeyValuePair<string, Player>(leader.Name, leader));

            string additionalTargets = FollowBotSettings.Instance.CustomSkills.LinkSkillAdditionalTargets;
            if (!string.IsNullOrEmpty(additionalTargets))
            {
                var players = LokiPoe.ObjectManager.GetObjectsByType<Player>();
                foreach (var name in additionalTargets.Split(',').Select(s => s.Trim()))
                {
                    if (string.IsNullOrEmpty(name)) continue;
                    // Skip if already added as leader
                    if (leader != null && name.Equals(leader.Name, StringComparison.OrdinalIgnoreCase)) continue;

                    var player = players.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
                    if (player != null && player.Distance <= 60)
                        targets.Add(new KeyValuePair<string, Player>(name, player));
                }
            }

            // Process each target: timer-based proactive refresh + buff-absence reactive fallback
            foreach (var kvp in targets)
            {
                var targetName = kvp.Key;
                var targetPlayer = kvp.Value;

                bool buffPresent = HasLinkBuff(targetPlayer);
                bool timerExpiring = NeedsLink(targetName, durationMs);

                // Skip if buff is present AND our timer says we're still within the safe window
                if (buffPresent && !timerExpiring)
                    continue;

                // Reactive: no buff = immediate cast. Proactive: timer says refresh.
                if (!buffPresent)
                    GlobalLog.Debug($"[LinkSkill] {targetName} has no link buff — casting immediately");
                else
                    GlobalLog.Debug($"[LinkSkill] {targetName} timer at 60% — proactive refresh");

                if (TryCastLinkOn(linkSkill.Slot, targetPlayer, targetName))
                {
                    CustomSkills.UpdateCooldown("LinkSkill");
                    await Coroutines.ReactionWait();
                    return; // One cast per tick
                }
            }
        }


        /*public static async Task LinkSkillHandler()
        {

            if (CustomSkills.IsOnCooldown("LinkSkill"))
                return;

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
            var needsRefresh = LokiPoe.Me.Auras.Any(x => linkSkills.Contains(x.Name) && x.TimeLeft.Seconds <= 6);

            // Target the leader
            var leader = FollowBot.Leader;
            if (leader != null && leader.Distance <= 60)
            {
                bool hasLink = leader.Auras.Any(a => linkSkills.Contains(a.Name) && a.TimeLeft.Seconds >= 6);
                if (!hasLink || needsRefresh)
                {
                    var useResult = LokiPoe.InGameState.SkillBarHud.UseOn(linkSkill.Slot, false, leader, false);
                    if (useResult == LokiPoe.InGameState.UseResult.CouldNotHighlight)
                    {
                        LokiPoe.InGameState.SkillBarHud.UseAt(linkSkill.Slot, false, leader.Position, false);
                        CustomSkills.UpdateCooldown("LinkSkill");
                        await Coroutines.ReactionWait();
                        return; // Use at leader and exit
                    }
                    CustomSkills.UpdateCooldown("LinkSkill");
                    await Coroutines.ReactionWait();
                    return; // Use on leader and exit
                }
            }

            // Handle additional targets
            string additionalTargets = FollowBotSettings.Instance.CustomSkills.LinkSkillAdditionalTargets;
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
                        bool hasLink = targetPlayer.Auras.Any(a => linkSkills.Contains(a.Name) && a.TimeLeft.Seconds >= 6);
                        if (!hasLink || needsRefresh)
                        {
                            var useResult2 = LokiPoe.InGameState.SkillBarHud.UseOn(linkSkill.Slot, false, targetPlayer, false);
                            if (useResult2 == LokiPoe.InGameState.UseResult.CouldNotHighlight)
                            {
                                LokiPoe.InGameState.SkillBarHud.UseAt(linkSkill.Slot, false, targetPlayer.Position, false);
                                CustomSkills.UpdateCooldown("LinkSkill");
                                await Coroutines.ReactionWait();
                                return; // Use at target and exit
                            }
                            CustomSkills.UpdateCooldown("LinkSkill");
                            await Coroutines.ReactionWait();
                            return; // Cast on one additional target per tick
                        }
                    }
                }
            }
        }*/

        public static async Task SummonRagingSpirits()
        {
            var settings = FollowBotSettings.Instance.CustomSkills;

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
            var settings = FollowBotSettings.Instance.CustomSkills;

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