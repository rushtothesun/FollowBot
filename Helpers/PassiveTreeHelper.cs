using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.Objects;
using FollowBot.SimpleEXtensions;

namespace FollowBot.Helpers
{
    public struct TargetPassiveNode
    {
        public ushort Id;
        public ushort MasteryHash;

        public TargetPassiveNode(ushort id, ushort hash = 0)
        {
            Id = id;
            MasteryHash = hash;
        }
    }

    public static class PassiveTreeHelper
    {
        /// <summary>
        /// Decodes a passive tree URL (Official or PoePlanner) into a list of TargetPassiveNode objects.
        /// </summary>
        public static List<TargetPassiveNode> GetTargetNodesFromUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return new List<TargetPassiveNode>();

            try
            {
                if (url.Contains("poeplanner.com"))
                {
                    return DecodePoePlannerUrl(url);
                }

                // Default to Official format (also covers simple skill tree links)
                return DecodeOfficialUrl(url);
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[PassiveTreeHelper] Error decoding URL '{url}': {ex.Message}");
                return new List<TargetPassiveNode>();
            }
        }

        private static List<TargetPassiveNode> DecodeOfficialUrl(string url)
        {
            string base64Part = url.Split('/').LastOrDefault();
            if (string.IsNullOrEmpty(base64Part) || base64Part.Contains("?"))
            {
                var uri = new Uri(url);
                base64Part = uri.Segments.Last().Trim('/');
            }

            string normalized = base64Part.Replace("-", "+").Replace("_", "/");
            int mod4 = normalized.Length % 4;
            if (mod4 > 0) normalized += new string('=', 4 - mod4);

            byte[] data = Convert.FromBase64String(normalized);

            // Official Format (Binary Version 6):
            // Bytes 0-3: Version (usually 6)
            // Bytes 4: Class
            // Bytes 5: Ascendancy
            // Bytes 6: Node Count (N)  <-- FIXED from 7
            // Bytes 7+: Every 2 bytes is a big-endian ushort node ID. <-- FIXED from 8
            // After N nodes: 2-byte Mastery Count (M)
            // Following: Every 4 bytes is (ushort hash, ushort nodeID)

            var nodes = new List<TargetPassiveNode>();
            int version = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];

            if (version >= 6)
            {
                int nodeCount = data[6];
                int offset = 7;
                for (int i = 0; i < nodeCount; i++)
                {
                    if (offset + 1 >= data.Length) break;
                    ushort id = (ushort)((data[offset] << 8) | data[offset + 1]);
                    nodes.Add(new TargetPassiveNode(id));
                    offset += 2;
                }

                if (offset + 1 < data.Length)
                {
                    ushort masteryCount = (ushort)((data[offset] << 8) | data[offset + 1]);
                    offset += 2;
                    for (int i = 0; i < masteryCount; i++)
                    {
                        if (offset + 3 < data.Length)
                        {
                            ushort hash = (ushort)((data[offset] << 8) | data[offset + 1]);
                            ushort id = (ushort)((data[offset + 2] << 8) | data[offset + 3]);

                            var existing = nodes.FirstOrDefault(n => n.Id == id);
                            if (existing.Id != 0)
                            {
                                nodes.Remove(existing);
                            }
                            nodes.Add(new TargetPassiveNode(id, hash));
                            offset += 4;
                        }
                    }
                }
            }
            else
            {
                // Legacy Version 4/5
                int nodeCount = data[7];
                int offset = 8;
                for (int i = 0; i < nodeCount; i++)
                {
                    if (offset + 1 >= data.Length) break;
                    ushort id = (ushort)((data[offset] << 8) | data[offset + 1]);
                    nodes.Add(new TargetPassiveNode(id));
                    offset += 2;
                }
            }

            return nodes;
        }

        private static List<TargetPassiveNode> DecodePoePlannerUrl(string url)
        {
            try
            {
                string base64Part = url.Split('/').LastOrDefault();
                if (string.IsNullOrEmpty(base64Part)) return new List<TargetPassiveNode>();

                string normalized = base64Part.Replace("-", "+").Replace("_", "/");
                int mod4 = normalized.Length % 4;
                if (mod4 > 0) normalized += new string('=', 4 - mod4);

                byte[] data = Convert.FromBase64String(normalized);
                if (data.Length < 12) return new List<TargetPassiveNode>();

                var nodes = new List<TargetPassiveNode>();

                // PoePlanner Layout (Version 4):
                // Bytes 0-3: Version (usually 4)
                // Bytes 10-11: Node Count (Big-Endian)
                // Bytes 12+: Node IDs (ushort, Little-Endian)

                int version = BitConverter.ToInt32(data.Take(4).ToArray(), 0);
                ushort nodeCount = (ushort)((data[10] << 8) | data[11]);
                int offset = 12;

                for (int i = 0; i < nodeCount; i++)
                {
                    if (offset + 1 >= data.Length) break;
                    ushort id = (ushort)((data[offset + 1] << 8) | data[offset]);
                    nodes.Add(new TargetPassiveNode(id));
                    offset += 2;
                }

                // After nodes, skip 4-byte divider (00 00 00 00)
                offset += 4;
                if (offset + 1 < data.Length)
                {
                    // Mastery Count (ushort, Little-Endian)
                    ushort masteryCount = (ushort)((data[offset + 1] << 8) | data[offset]);
                    offset += 2;

                    for (int i = 0; i < masteryCount; i++)
                    {
                        if (offset + 3 < data.Length)
                        {
                            // Mastery Entry (ushort id, ushort hash) - Both Little-Endian
                            ushort id = (ushort)((data[offset + 1] << 8) | data[offset]);
                            ushort hash = (ushort)((data[offset + 3] << 8) | data[offset + 2]);

                            var existing = nodes.FirstOrDefault(n => n.Id == id);
                            if (existing.Id != 0)
                            {
                                nodes.Remove(existing);
                            }
                            nodes.Add(new TargetPassiveNode(id, hash));
                            offset += 4;
                        }
                    }
                }

                GlobalLog.Info($"[PassiveTreeHelper] Decoded PoePlanner URL (v{version}): {nodes.Count} nodes found.");
                return nodes;
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[PassiveTreeHelper] Error decoding PoePlanner URL: {ex.Message}");
                return new List<TargetPassiveNode>();
            }
        }

        /// <summary>
        /// Returns a list of nodes that are reachable (neighbors of allocated nodes) and are in the target list.
        /// </summary>
        public static List<TargetPassiveNode> GetReachableTargetNodes(IEnumerable<TargetPassiveNode> targetNodes)
        {
            if (!LokiPoe.IsInGame) return new List<TargetPassiveNode>();

            var allocatedIds = LokiPoe.InstanceInfo.PassiveSkillIds.ToHashSet();
            var reachableTargets = new List<TargetPassiveNode>();

            foreach (var target in targetNodes)
            {
                if (allocatedIds.Contains(target.Id)) continue;

                // Check if this node is allowed to be allocated by the game API (reachable neighbor)
                if (LokiPoe.InGameState.SkillsUi.CanBeAllocate((int)target.Id))
                {
                    reachableTargets.Add(target);
                }
            }

            return reachableTargets;
        }

        /// <summary>
        /// Checks if a character is currently in combat based on nearby monsters.
        /// </summary>
        public static bool IsInCombat(int distance)
        {
            return LokiPoe.ObjectManager.GetObjectsByType<Monster>()
                .Any(m => m.Distance <= distance && m.IsAliveHostile);
        }
    }
}
