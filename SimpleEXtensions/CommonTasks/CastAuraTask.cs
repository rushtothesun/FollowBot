using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using DreamPoeBot.Loki.RemoteMemoryObjects;
using FollowBot.Settings;
using FollowBot.SimpleEXtensions;
using SkillBar = DreamPoeBot.Loki.Game.LokiPoe.InGameState.SkillBarHud;

namespace FollowBot
{
    public class CastAuraTask : ITask
    {
        private const int MinGolemHpPercent = 40;
        private const int MinRelicHpPercent = 20;
        private static List<int> _temporaryBlacklistedAuras = new List<int>();
        private static Dictionary<int, int> _auraRetryCount = new Dictionary<int, int>();
        private const int MaxAuraRetries = 5;

        // Golem mana-toggle state machine
        private enum GolemToggleState { Idle, AuraDropped }
        private static GolemToggleState _golemToggleState = GolemToggleState.Idle;
        private static string _suppressedAuraName = null;
        private static int _suppressedAuraSkillId = 0;
        private static Stopwatch _golemToggleTimer = new Stopwatch();
        public async Task<bool> Run()
        {
            var area = World.CurrentArea;
            //if (!area.IsHideoutArea && !area.IsMap && !area.IsMapRoom && !area.IsOverworldArea)
            if (area.IsTown || area.Id == "HeistHub" || LokiPoe.Me.IsDead)
                return false;

            await Coroutines.CloseBlockingWindows();

            if (FollowBotSettings.Instance.Combat.UseStalkerSentinel)
            {
                if (!LokiPoe.InGameState.SentinelSkillUi.StalkerSentinel.IsActive && LokiPoe.InGameState.SentinelSkillUi.StalkerSentinel.CanUse)
                    LokiPoe.InGameState.SentinelSkillUi.StalkerSentinel.Activate();
            }

            // --- Golem Summoning with Mana Toggle Recovery ---
            var golemSkill = SkillBar.Skills.FirstOrDefault(s => s.IsOnSkillBar && s.SkillTags.Contains("golem"));
            if (golemSkill != null)
            {
                var golemObj = golemSkill.DeployedObjects.FirstOrDefault() as Monster;
                bool golemNeedsSummon = golemObj == null || golemObj.HealthPercent < MinGolemHpPercent;

                var toggleAuraName = FollowBotSettings.Instance.CustomSkills.GolemManaToggleAura;
                var timeoutMs = FollowBotSettings.Instance.CustomSkills.GolemManaToggleTimeoutMs;
                bool toggleFeatureEnabled = !string.IsNullOrEmpty(toggleAuraName);

                // Safety timeout: if we've been in a non-Idle state too long, reset
                if (_golemToggleState != GolemToggleState.Idle
                    && _golemToggleTimer.ElapsedMilliseconds > timeoutMs)
                {
                    GlobalLog.Warn($"[CastAuraTask] Golem mana-toggle timed out after {timeoutMs}ms. Resetting.");
                    _golemToggleState = GolemToggleState.Idle;
                    _suppressedAuraName = null;
                    _suppressedAuraSkillId = 0;
                    _golemToggleTimer.Reset();
                }

                switch (_golemToggleState)
                {
                    case GolemToggleState.Idle:
                        if (golemNeedsSummon)
                        {
                            if (golemSkill.CanUse())
                            {
                                // Normal path: enough mana, just summon
                                GlobalLog.Debug($"[CastAuraTask] Now summoning \"{golemSkill.Name}\".");
                                SkillBar.Use(golemSkill.Slot, false);
                                await Wait.SleepSafe(100);
                                await Coroutines.FinishCurrentAction();
                                await Wait.SleepSafe(100);
                            }
                            else if (toggleFeatureEnabled && golemObj == null)
                            {
                                // CanUse failed and golem is dead -- drop a low-priority aura to free mana
                                var auraToToggle = AllAuras.FirstOrDefault(s =>
                                    s.IsOnSkillBar &&
                                    s.Name.Equals(toggleAuraName, System.StringComparison.OrdinalIgnoreCase) &&
                                    PlayerHasAura(s));

                                if (auraToToggle != null)
                                {
                                    GlobalLog.Info($"[CastAuraTask] Golem dead, can't summon. Deactivating \"{auraToToggle.Name}\" to free mana.");
                                    _suppressedAuraName = auraToToggle.Name;
                                    _suppressedAuraSkillId = auraToToggle.Id;
                                    SkillBar.Use(auraToToggle.Slot, false);
                                    await Wait.SleepSafe(100);
                                    await Coroutines.FinishCurrentAction();
                                    await Wait.SleepSafe(100);
                                    _golemToggleState = GolemToggleState.AuraDropped;
                                    _golemToggleTimer.Restart();
                                }
                                else
                                {
                                    GlobalLog.Warn($"[CastAuraTask] Golem dead, can't summon. Toggle aura \"{toggleAuraName}\" not found active on skill bar.");
                                }
                            }
                        }
                        break;

                    case GolemToggleState.AuraDropped:
                        if (golemNeedsSummon && golemSkill.CanUse())
                        {
                            // Mana regenerated, summon the golem
                            GlobalLog.Info($"[CastAuraTask] Mana available. Summoning \"{golemSkill.Name}\" after aura drop.");
                            SkillBar.Use(golemSkill.Slot, false);
                            await Wait.SleepSafe(100);
                            await Coroutines.FinishCurrentAction();
                            await Wait.SleepSafe(100);
                            // Clear suppression so the aura recasts below
                            GlobalLog.Info($"[CastAuraTask] Golem summoned. Clearing aura suppression for \"{_suppressedAuraName}\".");
                            _golemToggleState = GolemToggleState.Idle;
                            _suppressedAuraName = null;
                            _suppressedAuraSkillId = 0;
                            _golemToggleTimer.Reset();
                        }
                        else if (!golemNeedsSummon)
                        {
                            // Golem is somehow alive already, reset
                            GlobalLog.Debug("[CastAuraTask] Golem alive during AuraDropped state. Resetting.");
                            _golemToggleState = GolemToggleState.Idle;
                            _suppressedAuraName = null;
                            _suppressedAuraSkillId = 0;
                            _golemToggleTimer.Reset();
                        }
                        // else: CanUse still fails, mana hasn't regen'd yet. Do nothing, retry next tick.
                        break;
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
            var auraOnBar = AllAuras.FirstOrDefault(a => a.IsOnSkillBar);
            int slotForHidden = auraOnBar?.Slot ?? 4;

            // Save the original skill in the slot we'll use for hidden auras
            Skill originalSkillInSlot = null;
            if (auraOnBar == null)
                originalSkillInSlot = SkillBar.Slot(slotForHidden);

            foreach (var aura in auras.OrderByDescending(a => a.Slot))
            {
                if (LokiPoe.Me.IsDead) break;
                if (!aura.CanUse())
                {
                    int retries;
                    _auraRetryCount.TryGetValue(aura.Id, out retries);
                    retries++;
                    if (retries >= MaxAuraRetries)
                    {
                        GlobalLog.Warn($"[CastAuraTask] CanUse() returned false for \"{aura.Name}\" after {retries} attempts. Adding to temporary blacklist.");
                        _temporaryBlacklistedAuras.Add(aura.Id);
                        _auraRetryCount.Remove(aura.Id);
                    }
                    else
                    {
                        GlobalLog.Debug($"[CastAuraTask] CanUse() returned false for \"{aura.Name}\". Retry {retries}/{MaxAuraRetries}.");
                        _auraRetryCount[aura.Id] = retries;
                    }
                    continue;
                }
                // CanUse succeeded, clear any retry count
                _auraRetryCount.Remove(aura.Id);
                if (aura.Slot == -1)
                {
                    await SetAuraToSlot(aura, slotForHidden);
                }
                await ApplyAura(aura);
            }

            // Restore the original skill if we displaced one
            if (originalSkillInSlot != null && originalSkillInSlot.Id != SkillBar.Slot(slotForHidden)?.Id)
            {
                await SetAuraToSlot(originalSkillInSlot, slotForHidden);
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
                if (FollowBotSettings.Instance.Auras.IgnoreHiddenAuras && !aura.IsOnSkillBar)
                    continue;

                if (PlayerHasAura(aura))
                    continue;

                // Skip auras linked with Guardian's Blessing Support (handled by CustomSkills)
                var display = aura.LinkedDisplayString;
                if (!string.IsNullOrEmpty(display) && display.Contains("Guardian's Blessing Support"))
                    continue;

                // Skip the aura that was intentionally deactivated for golem mana recovery
                if (_suppressedAuraName != null && aura.Id == _suppressedAuraSkillId)
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
                (AuraNames.Contains(skill.Name) || AuraInternalId.Contains(skill.InternalId) ||
                 (FollowBotSettings.Instance.Auras.EnableBlasphemyCurses && skill.IsAurifiedCurse) ||
                 (FollowBotSettings.Instance.Auras.EnableAspectsOfTheAvian && skill.Name == "Aspect of the Avian") ||
                 (FollowBotSettings.Instance.Auras.EnableAspectsOfTheCat && skill.Name == "Aspect of the Cat") ||
                 (FollowBotSettings.Instance.Auras.EnableAspectsOfTheCrab && skill.Name == "Aspect of the Crab") ||
                 (FollowBotSettings.Instance.Auras.EnableAspectsOfTheSpider && skill.Name == "Aspect of the Spider")
                ));
            }
        }

        private static bool PlayerHasAura(Skill aura)
        {
            //BloodAndSand Hack
            if (aura.InternalId == "blood_sand_armour")
            {
                if (FollowBotSettings.Instance.Auras.BloodOrSand == AuraSettings.BloodAndSand.Blood)
                    return LokiPoe.Me.Auras.Any(x => x.InternalName == "blood_armour");
                if (FollowBotSettings.Instance.Auras.BloodOrSand == AuraSettings.BloodAndSand.Sand)
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
            // auras
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
            "Zealotry",

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
            // auras
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
            "spell_damage_aura",//Zealotry

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
            _auraRetryCount.Clear();
            _golemToggleState = GolemToggleState.Idle;
            _suppressedAuraName = null;
            _suppressedAuraSkillId = 0;
            _golemToggleTimer.Reset();
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