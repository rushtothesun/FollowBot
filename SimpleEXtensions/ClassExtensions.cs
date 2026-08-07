using System;
using System.Collections;
using System.Collections.Generic;
using DreamPoeBot.Common;
using DreamPoeBot.Loki.Bot.Pathfinding;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions.Positions;
using JetBrains.Annotations;
using DreamPoeBot.Loki;
using System.Linq;

namespace FollowBot.SimpleEXtensions
{
    public static class ClassExtensions
    {
        public static bool EqualsIgnorecase(this string thisStr, string str)
        {
            return thisStr.Equals(str, StringComparison.OrdinalIgnoreCase);
        }

        public static bool ContainsIgnorecase(this string thisStr, string str)
        {
            return thisStr.IndexOf(str, StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static Rarity RarityLite(this Item item)
        {
            var mods = item.Components.ModsComponent;
            return mods == null ? Rarity.Normal : mods.Rarity;
        }

        public static WalkablePosition WalkablePosition(this NetworkObject obj, int step = 10, int radius = 30)
        {
            return new WalkablePosition(obj.Name, obj.Position, step, radius);
        }

        public static bool PathExists(this NetworkObject obj)
        {
            return ExilePather.PathExistsBetween(LokiPoe.MyPosition, obj.Position);
        }

        public static float PathDistance(this NetworkObject obj)
        {
            return ExilePather.PathDistance(LokiPoe.MyPosition, obj.Position);
        }


        public static bool IsPlayerPortal(this DreamPoeBot.Loki.Game.Objects.Portal p)
        {
            if (!p.IsTargetable)
                return false;

            var m = p.Metadata;
            return m == "Metadata/MiscellaneousObjects/PlayerPortal" ||
                   m == "Metadata/MiscellaneousObjects/MapReturnPortal" ||
                   m == "Metadata/MiscellaneousObjects/MultiplexPortal";
        }

        [CanBeNull]
        public static T Closest<T>(this IEnumerable collection, Func<T, bool> match) where T : NetworkObject
        {
            T closest = null;
            foreach (var element in collection)
            {
                var typed = element as T;
                if (typed != null && match(typed))
                {
                    if (closest == null || typed.DistanceSqr < closest.DistanceSqr)
                        closest = typed;
                }
            }
            return closest;
        }

        [CanBeNull]
        public static T FirstOrDefault<T>(this IEnumerable collection) where T : class
        {
            foreach (var element in collection)
            {
                var t = element as T;
                if (t != null)
                    return t;
            }
            return null;
        }

        [CanBeNull]
        public static T FirstOrDefault<T>(this IEnumerable collection, Func<T, bool> match) where T : class
        {
            foreach (var element in collection)
            {
                var t = element as T;
                if (t != null && match(t))
                    return t;
            }
            return null;
        }
        
        public static IEnumerable<T> Valid<T>(this IEnumerable<T> collection) where T : CachedObject
        {
            foreach (var element in collection)
            {
                if (!element.Ignored && !element.Unwalkable)
                    yield return element;
            }
        }

        [CanBeNull]
        public static T ClosestValid<T>(this IEnumerable<T> collection) where T : CachedObject
        {
            T closest = null;
            foreach (var element in collection)
            {
                if (!element.Ignored && !element.Unwalkable)
                {
                    if (closest == null || element.Position.DistanceSqr < closest.Position.DistanceSqr)
                        closest = element;
                }
            }
            return closest;
        }

        [CanBeNull]
        public static T ClosestValid<T>(this IEnumerable<T> collection, Func<T, bool> match) where T : CachedObject
        {
            T closest = null;
            foreach (var element in collection)
            {
                if (!element.Ignored && !element.Unwalkable && match(element))
                {
                    if (closest == null || element.Position.DistanceSqr < closest.Position.DistanceSqr)
                        closest = element;
                }
            }
            return closest;
        }

        public static Element GetElementByPath(params int[] childIndices)
        {
            var allElements = LokiPoe.GetGuiElements();
            var root = Enumerable.FirstOrDefault(allElements, e => e.IdLabel == "root");

            if (root == null || root.Children == null || root.Children.Count < 2)
                return null;

            Element current = root.Children[1];

            foreach (var index in childIndices)
            {
                if (current.Children == null || current.Children.Count <= index)
                    return null;
                current = current.Children[index];
            }

            return current;
        }

        private const string MirageButtonTooltip = "Teleports you back to the entrance of this Mirage.";

        public static Element FindElementByLabels(params string[] labels)
        {
            var allElements = LokiPoe.GetGuiElements();
            var root = Enumerable.FirstOrDefault(allElements, e => e.IdLabel == "root");
            if (root?.Children == null || root.Children.Count < 2)
                return null;

            Element current = root.Children[1];
            foreach (var label in labels)
            {
                if (current?.Children == null)
                    return null;
                current = Enumerable.FirstOrDefault(current.Children, c => c?.IdLabel == label);
                if (current == null)
                    return null;
            }
            return current;
        }

        public static Element FindMirageReturnButton()
        {
            if (!LeagueFeatureFlags.MirageEnabled)
                return null;

            var container = FindElementByLabels("HUD", "HUDRight", "skip_button_layout");
            if (container?.Children == null)
                return null;

            foreach (var child in container.Children)
            {
                if (child == null || !child.IsVisible)
                    continue;

                try
                {
                    var tooltip = child.Tooltip;
                    if (tooltip?.Text?.Contains(MirageButtonTooltip) == true)
                        return child;

                    // Some tooltips have text in children
                    if (tooltip?.Children != null && tooltip.Children.Count > 0)
                    {
                        var text = tooltip.Children[0]?.Text;
                        if (text != null && text.Contains(MirageButtonTooltip))
                            return child;
                    }
                }
                catch
                {
                    // Tooltip access can throw on stale elements
                }
            }

            return null;
        }

        public static bool IsUnderGracePeriod
        {
            get
            {
                if (!LokiPoe.Me.HasAura("Grace Period"))
                    return false;

                if (LeagueFeatureFlags.MirageEnabled && FindMirageReturnButton() != null)
                    return false;

                return true;
            }
        }
    }
}
