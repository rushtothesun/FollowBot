using DreamPoeBot.Common;
using DreamPoeBot.Loki.Controllers;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions.Positions;
using System.Linq;
using DreamPoeBot.Loki.Models;

namespace FollowBot.SimpleEXtensions
{
    public class CachedWorldItem : CachedObject
    {
        public Vector2i Size { get; }
        public Rarity Rarity { get; }
        public string Metadata { get; }
        public string Class { get; }

        public CachedWorldItem(int id, WalkablePosition position, Vector2i size, Rarity rarity, string metadata, string itemClass)
            : base(id, position)
        {
            Size = size;
            Rarity = rarity;
            Metadata = metadata ?? string.Empty;
            Class = itemClass ?? string.Empty;
        }

        public new WorldItem Object
        {
            get
            {
                var wIts = GameController.Instance.Game.IngameState.IngameUi.ItemsOnGroundLabels.Where(x =>
                    x != null && 
                    x.ItemOnGround != null).ToList();
                if (wIts.Count <= 0) return null;
                foreach (var it in wIts)
                {
                    var wIt = new WorldItem(new EntityWrapper(it.ItemOnGround.Address));
                    if (!wIt.IsValid) 
                        continue;
                    if (wIt.Id != Id) 
                        continue;
                    return wIt;
                }

                return null;
            }
        }
    }
}
