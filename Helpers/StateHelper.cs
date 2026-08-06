using System.Linq;
using DreamPoeBot.Loki.Game;

namespace FollowBot.Helpers
{
    public static class StateHelper
    {
        public static bool IsDeepwaterEncounter()
        {
            if (!LeagueFeatureFlags.AllflameEnabled)
                return false;

            var area = LokiPoe.CurrentWorldArea;
            return area != null &&
                   (area.Id == "DeepwaterHub" ||
                    area.Id == "DeepwaterEncounter" ||
                    area.Id == "DeepwaterEncounterVoyage");
        }

        public static bool IsDeepwaterDrowning()
        {
            if (!LokiPoe.IsInGame || LokiPoe.Me == null || !IsDeepwaterEncounter())
                return false;

            var auras = LokiPoe.Me.Components.BuffsComponent.Auras;
            return auras != null && auras.Any(a => a.InternalName == "deepwater_drowning");
        }
    }
}
