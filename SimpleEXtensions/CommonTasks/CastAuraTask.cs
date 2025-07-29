using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using FollowBot.SimpleEXtensions;
using SkillBar = DreamPoeBot.Loki.Game.LokiPoe.InGameState.SkillBarHud;

namespace FollowBot
{
    public class CastAuraTask : ITask
    {
        private const int MinGolemHpPercent = 40;
        private const int MinRelicHpPercent = 20;
        private static List<int> _temporaryBlacklistedAuras = new List<int>();
        public async Task<bool> Run()
        {
            var area = World.CurrentArea;
            //if (!area.IsHideoutArea && !area.IsMap && !area.IsMapRoom && !area.IsOverworldArea)
            if (area.IsTown || area.Id == "HeistHub" || LokiPoe.Me.IsDead)
                return false;

            await Coroutines.CloseBlockingWindows();

            if (FollowBotSettings.Instance.UseStalkerSentinel)
            {
                if (!LokiPoe.InGameState.SentinelSkillUi.StalkerSentinel.IsActive && LokiPoe.InGameState.SentinelSkillUi.StalkerSentinel.CanUse)
                    LokiPoe.InGameState.SentinelSkillUi.StalkerSentinel.Activate();
            }

            var golemSkill = SkillBar.Skills.FirstOrDefault(s => s.IsOnSkillBar && s.SkillTags.Contains("golem"));
            if (golemSkill != null && golemSkill.CanUse())
            {
                var golemObj = golemSkill.DeployedObjects.FirstOrDefault() as Monster;
                if (golemObj == null || golemObj.HealthPercent < MinGolemHpPercent)
                {
                    GlobalLog.Debug($"[CastAuraTask] Now summoning \"{golemSkill.Name}\".");
                    SkillBar.Use(golemSkill.Slot, false);
                    await Wait.SleepSafe(100);
                    await Coroutines.FinishCurrentAction();
                    await Wait.SleepSafe(100);
                }
            }

            var relicSkill = SkillBar.Skills.FirstOrDefault(s => s.IsOnSkillBar && s.InternalName == "SummonRelic");
            if (relicSkill != null && relicSkill.CanUse())
            {
                var relicObj = relicSkill.DeployedObjects.FirstOrDefault() as Monster;
                if (relicObj == null || relicObj.HealthPercent < MinRelicHpPercent)
                {
                    GlobalLog.Debug($"[CastAuraTask] Now summoning \"{relicSkill.Name}\".");
                    SkillBar.Use(relicSkill.Slot, false);
                    await Wait.SleepSafe(100);
                    await Coroutines.FinishCurrentAction();
                    await Wait.SleepSafe(100);
                }
            }
            // Auras
            var auras = GetAurasForCast();
            if (auras.Count > 0)
            {
                GlobalLog.Info($"[CastAuraTask] Found {auras.Count} aura(s) for casting.");
                await CastAuras(auras);
            }
            return false;
        }

        private static async Task CastAuras(IEnumerable<Skill> auras)
        {
            int slotForHidden = AllAuras.First(a => a.IsOnSkillBar).Slot;
            foreach (var aura in auras.OrderByDescending(a => a.Slot))
            {
                if (LokiPoe.Me.IsDead) break;
                if (aura.Slot == -1)
                {
                    await SetAuraToSlot(aura, slotForHidden);
                }
                await ApplyAura(aura);
            }
        }

        private static async Task ApplyAura(Skill aura)
        {
            string name = aura.Name;
            int id = aura.Id;
            GlobalLog.Debug($"[CastAuraTask] Now casting \"{name}\".");
            var used = SkillBar.Use(aura.Slot, false);
            if (used != LokiPoe.InGameState.UseResult.None)
            {
                GlobalLog.Error($"[CastAuraTask] Fail to cast \"{name}\". Error: \"{used}\".");
                return;
            }

            if (aura.InternalId == "blood_sand_armour")
            {
                if (!await Wait.For(() => !LokiPoe.Me.IsDead && !LokiPoe.Me.HasCurrentAction && LokiPoe.Me.Auras.Any(x => x.InternalName == "blood_armour" || x.InternalName == "sand_armour"), "aura applying"))
                {
                    if (LokiPoe.Me.IsDead) return;
                    GlobalLog.Warn($"[CastAuraTask] Failed to apply aura \"{name}\".");
                    GlobalLog.Warn($"[CastAuraTask] Pls make sure you can cast this aura (you have ennoght mana).");
                    GlobalLog.Warn($"[CastAuraTask] Also make sure you have blacklisted the auras you dont want to use (Settings-Content-SkillBlacklist).");
                    GlobalLog.Warn($"[CastAuraTask] This error Usually indicate that you have more Auras, slotted, than you can substain with your mana.");
                    GlobalLog.Warn($"[CastAuraTask] The aura \"{name}\" will not be blacklisted to allow the bot to continue, the aura will be recasted next time you stop/start the bot.");
                    _temporaryBlacklistedAuras.Add(id);
                }
            }
            else
            {
                if (!await Wait.For(() => !LokiPoe.Me.IsDead && !LokiPoe.Me.HasCurrentAction && PlayerHasAura(aura), "aura applying"))
                {
                    if (LokiPoe.Me.IsDead) return;
                    GlobalLog.Warn($"[CastAuraTask] Failed to apply aura \"{name}\".");
                    GlobalLog.Warn($"[CastAuraTask] Pls make sure you can cast this aura (you have ennoght mana).");
                    GlobalLog.Warn($"[CastAuraTask] Also make sure you have blacklisted the auras you dont want to use (Settings-Content-SkillBlacklist).");
                    GlobalLog.Warn($"[CastAuraTask] This error Usually indicate that you have more Auras, slotted, than you can substain with your mana.");
                    GlobalLog.Warn($"[CastAuraTask] The aura \"{name}\" will not be blacklisted to allow the bot to continue, the aura will be recasted next time you stop/start the bot.");
                    _temporaryBlacklistedAuras.Add(id);
                }
            }

            await Wait.SleepSafe(100);
        }

        private static async Task SetAuraToSlot(Skill aura, int slot)
        {
            string name = aura.Name;
            GlobalLog.Debug($"[CastAuraTask] Now setting \"{name}\" to slot {slot}.");
            var isSet = SkillBar.SetSlot(slot, aura);
            if (isSet != LokiPoe.InGameState.SetSlotResult.None)
            {
                GlobalLog.Error($"[CastAuraTask] Fail to set \"{name}\" to slot {slot}. Error: \"{isSet}\".");
                return;
            }
            await Wait.For(() => IsInSlot(slot, name), "aura slot changing");
            await Wait.SleepSafe(100);
        }

        private static bool IsInSlot(int slot, string name)
        {
            var skill = SkillBar.Slot(slot);
            return skill != null && skill.Name == name;
        }

        private static List<Skill> GetAurasForCast()
        {
            var auras = new List<Skill>();
            foreach (var aura in AllWhitelistedAuras)
            {
                if (FollowBotSettings.Instance.IgnoreHiddenAuras && !aura.IsOnSkillBar)
                    continue;

                if (PlayerHasAura(aura))
                    continue;

                auras.Add(aura);
            }
            return auras;
        }

        private static IEnumerable<Skill> AllAuras
        {
            get
            {
                return SkillBar.Skills.Where(skill => !skill.IsVaalSkill && (AuraNames.Contains(skill.Name) || AuraInternalId.Contains(skill.InternalId) || skill.IsAurifiedCurse || AspectsNames.Contains(skill.Name)));
            }
        }
        private static IEnumerable<Skill> AllWhitelistedAuras
        {
            get
            {
                return SkillBar.Skills.Where(skill => !skill.IsVaalSkill &&
                    _temporaryBlacklistedAuras.All(x => x != skill.Id) &&
                !SkillBlacklist.IsBlacklisted(skill) &&
                (AuraNames.Contains(skill.Name) || AuraInternalId.Contains(skill.InternalId) || skill.IsAurifiedCurse ||
                 (FollowBotSettings.Instance.EnableAspectsOfTheAvian && skill.Name == "Aspect of the Avian") ||
                 (FollowBotSettings.Instance.EnableAspectsOfTheCat && skill.Name == "Aspect of the Cat") ||
                 (FollowBotSettings.Instance.EnableAspectsOfTheCrab && skill.Name == "Aspect of the Crab") ||
                 (FollowBotSettings.Instance.EnableAspectsOfTheSpider && skill.Name == "Aspect of the Spider")
                ));
            }
        }

        private static bool PlayerHasAura(Skill aura)
        {
            //BloodAndSand Hack
            if (aura.InternalId == "blood_sand_armour")
            {
                if (FollowBotSettings.Instance.BloorOrSand == FollowBotSettings.BloodAndSand.Blood)
                    return LokiPoe.Me.Auras.Any(x => x.InternalName == "blood_armour");
                if (FollowBotSettings.Instance.BloorOrSand == FollowBotSettings.BloodAndSand.Sand)
                    return LokiPoe.Me.Auras.Any(x => x.InternalName == "sand_armour");
            }
            if (PlayerHasAura(aura.Name))
                return true;
            if (PlayerHasAura(aura.Id))
                return true;
            return false;
        }
        private static bool PlayerHasAura(string auraName)
        {
            return LokiPoe.Me.Auras.Any(a => (a.Name.EqualsIgnorecase(auraName) || a.Name.EqualsIgnorecase(auraName + " aura")) && a.CasterId == LokiPoe.Me.Id);
        }
        private static bool PlayerHasAura(int skillId)
        {
            //return LokiPoe.Me.Auras.Any(a => (a.Name.EqualsIgnorecase(auraName) || a.Name.EqualsIgnorecase(auraName + " aura")) && a.CasterId == LokiPoe.Me.Id);

            return LokiPoe.Me.Auras.Any(a => a.SkillOwnerId == skillId);
        }

        private static readonly HashSet<string> AuraNames = new HashSet<string>
        {
            /* auras
            "Anger",
            "Clarity",
            "Determination",
            "Discipline",
            "Grace",
            "Haste",
            "Hatred",
            "Malevolence",
            "Precision",
            "Pride",
            "Purity of Elements",
            "Purity of Fire",
            "Purity of Ice",
            "Purity of Lightning",
            "Vitality",
            "Wrath",
            "Zealotry",         */

            // heralds
            "Herald of Agony",
            "Herald of Ice",
            "Herald of Thunder",
            "Herald of Ash",
            "Herald of Purity",

            // the rest
            "Spellslinger",
            "Arctic Armour",
            "Flesh and Stone",
            "Envy",
        };
        private static readonly HashSet<string> AspectsNames = new HashSet<string>
        {
            // aspects
            "Aspect of the Avian",
            "Aspect of the Cat",
            "Aspect of the Crab",
            "Aspect of the Spider"
        };
        private static readonly HashSet<string> AuraInternalId = new HashSet<string>
        {
            /* auras
            "anger",
            "clarity",
            "determination",
            "discipline",
            "grace",
            "haste",
            "hatred",
            "damage_over_time_aura",//Malevolence
            "aura_accuracy_and_crits",//Precision
            "physical_damage_aura",//Pride
            "purity",//Purity of Elements
            "fire_resist_aura",//Purity of Fire
            "cold_resist_aura",//Purity of Ice
            "lightning_resist_aura",//Purity of Lightning
            "vitality",
            "wrath",
            "spell_damage_aura",//Zealotry */

            // heralds
            "herald_of_agony",
            "herald_of_ice",
            "herald_of_thunder",
            "herald_of_ash",
            "herald_of_light",//Herald of Purity

            // the rest
            "spellslinger",
            "new_arctic_armour",
            "tempest_shield",
            "skitterbots",
            "petrified_blood",
            "blood_sand_armour",
            "envy",


        };

        #region Unused interface methods

        public MessageResult Message(Message message)
        {
            return MessageResult.Unprocessed;
        }

        public Task<LogicResult> Logic(Logic logic)
        {
            return Task.FromResult(LogicResult.Unprovided);
        }

        public void Tick()
        {
        }

        public void Start()
        {
            _temporaryBlacklistedAuras.Clear();
        }

        public void Stop()
        {
        }

        public string Name => "CastAuraTask";
        public string Description => "Task for casting auras before entering a map.";
        public string Author => "ExVault";
        public string Version => "1.0";

        #endregion
    }
}