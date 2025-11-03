using DreamPoeBot.Loki.Bot;
using DreamPoeBot.Loki.Game;
using FollowBot.Class;
using FollowBot.SimpleEXtensions;
using System.Threading.Tasks;

namespace FollowBot.Tasks
{
    public class CustomSkillsTask : ITask
    {
        public string Name => "CustomSkillsTask";
        public string Description => "Handles custom skill usage based on settings.";
        public string Author => "Rushtothesun";
        public string Version => "1.0.0";

        public void Start() { }
        public void Stop() { }
        public void Tick() { }

        public Task<bool> Run()
        {

            if (!LokiPoe.IsInGame) return Task.FromResult(false);
            if (FollowBot.Leader == null) return Task.FromResult(false);
            if (LokiPoe.Me.IsDead) return Task.FromResult(false);
            if (LokiPoe.CurrentWorldArea.IsTown) return Task.FromResult(false);
            if (LokiPoe.CurrentWorldArea.Id == "HeistHub") return Task.FromResult(false);
            if (!LokiPoe.CurrentWorldArea.IsCombatArea) return Task.FromResult(false);

            if (LokiPoe.Me.HasAura("Grace Period"))
            {
                GlobalLog.Debug("[CustomSkillsTask] Find grace period, wait player moves.");
                return Task.FromResult(false);
            }

            var settings = FollowBotSettings.Instance;


            if (settings.EnablePhaseRun) CustomSkills.PhaseRun();
            if (settings.EnableGuardSkill) CustomSkills.GuardSkill();
            if (settings.EnableEnduringCry) CustomSkills.EnduringCry();
            if (settings.EnableSeismicCry) CustomSkills.SeismicCry();
            if (settings.EnableBattlemageCry) CustomSkills.BattlemageCry();
            if (settings.EnableAncestralCry) CustomSkills.AncestralCry();
            if (settings.EnableIntimidatingCry) CustomSkills.IntimidatingCry();
            if (settings.EnableInfernalCry) CustomSkills.InfernalCry();
            if (settings.EnableRallyingCry) CustomSkills.RallyingCry();
            if (settings.EnableGuardiansBlessingHandler) CustomSkills.GuardiansBlessingHandler();
            if (settings.EnableSentinelUsage) CustomSkills.SentinelUsage();
            if (settings.EnableChaosElixir) CustomSkills.ChaosElixir();
            if (settings.EnableConvocation) CustomSkills.Convocation();
            if (settings.EnableLinkSkill) CustomSkills.LinkSkillHandler();
            if (settings.EnableUseRejuvenationTotemDuringUltimatum) CustomSkills.UseRejuvenationTotemDuringUltimatum();
            if (settings.EnableUseWarBannerDuringUltimatumOrNearUnique) CustomSkills.UseWarBannerDuringUltimatumOrNearUnique();
            if (settings.EnableUseWarDefianceBannerDuringUltimatumOrNearUnique) CustomSkills.UseWarDefianceBannerDuringUltimatumOrNearUnique();
            if (settings.EnableRejuvenationTotem) CustomSkills.RejuvenationTotem();

            return Task.FromResult(false);
        }

        public Task<LogicResult> Logic(Logic logic) => Task.FromResult(LogicResult.Unprovided);
        public MessageResult Message(Message message) => MessageResult.Unprocessed;
    }
}