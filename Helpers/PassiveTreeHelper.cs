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
        public int Id;
        public ushort MasteryHash;

        public TargetPassiveNode(int id, ushort hash = 0)
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
            // Bytes 6: Node Count (N)
            // Bytes 7+: Every 2 bytes is a big-endian ushort node ID.
            
            var nodes = new List<TargetPassiveNode>();
            int version = (data[0] << 24) | (data[1] << 16) | (data[2] << 8) | data[3];

            if (version >= 6)
            {
                int nodeCount = data[6];
                int offset = 7;
                
                // 1. Regular/Ascendancy Nodes
                for (int i = 0; i < nodeCount; i++)
                {
                    if (offset + 1 >= data.Length) break;
                    int id = (data[offset] << 8) | data[offset + 1];
                    nodes.Add(new TargetPassiveNode(id));
                    offset += 2;
                }

                // 2. Cluster Jewels
                if (offset < data.Length)
                {
                    int clusterCount = data[offset];
                    offset++;
                    for (int i = 0; i < clusterCount; i++)
                    {
                        if (offset + 1 >= data.Length) break;
                        int id = ((data[offset] << 8) | data[offset + 1]) + 65536;
                        nodes.Add(new TargetPassiveNode(id));
                        offset += 2;
                    }
                }

                // 3. Masteries
                if (offset < data.Length)
                {
                    int masteryCount = data[offset];
                    offset++;
                    for (int i = 0; i < masteryCount; i++)
                    {
                        if (offset + 3 < data.Length)
                        {
                            ushort hash = (ushort)((data[offset] << 8) | data[offset + 1]);
                            int id = (data[offset + 2] << 8) | data[offset + 3];

                            // Remove existing node if it was already added without a hash
                            nodes.RemoveAll(n => n.Id == id);
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
                    int id = (data[offset] << 8) | data[offset + 1];
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
                if (data.Length < 13) return new List<TargetPassiveNode>();

                var nodes = new List<TargetPassiveNode>();

                // PoePlanner format (all multi-byte values are Little-Endian):
                // Bytes 0-1:  serializationVersion (u16 LE)
                // Byte  2:    buildType (u8)
                // Byte  3:    isPoE2 (u8)
                // Bytes 4-5:  treeSerializationVersion (u16 LE)
                // Bytes 6-7:  treeVersion (u16 LE)
                // Byte  8:    class (u8)
                // Byte  9:    ascendancy (u8)
                // Byte  10:   bandit (u8)
                // Bytes 11-12: nodeCount (u16 LE)
                // Then: nodeCount * u16 LE node hashes
                // Then: clusterNodeCount (u16 LE) + cluster node hashes (u16 LE each)
                // Then: ascendancyNodeCount (u16 LE) + ascendancy node hashes (u16 LE each)
                // Then: masteryEffectCount (u16 LE) + {masteryID u16 LE, effectID u16 LE} each

                int version = ReadU16LE(data, 0);
                int offset = 11;

                // 1. Regular nodes
                int nodeCount = ReadU16LE(data, offset);
                offset += 2;
                for (int i = 0; i < nodeCount; i++)
                {
                    if (offset + 1 >= data.Length) break;
                    nodes.Add(new TargetPassiveNode(ReadU16LE(data, offset)));
                    offset += 2;
                }

                // 2. Cluster nodes
                if (offset + 1 < data.Length)
                {
                    int clusterCount = ReadU16LE(data, offset);
                    offset += 2;
                    for (int i = 0; i < clusterCount; i++)
                    {
                        if (offset + 1 >= data.Length) break;
                        nodes.Add(new TargetPassiveNode(ReadU16LE(data, offset)));
                        offset += 2;
                    }
                }

                // 3. Ascendancy nodes (separate section in PoePlanner, unlike official format)
                if (offset + 1 < data.Length)
                {
                    int ascendancyCount = ReadU16LE(data, offset);
                    offset += 2;
                    for (int i = 0; i < ascendancyCount; i++)
                    {
                        if (offset + 1 >= data.Length) break;
                        nodes.Add(new TargetPassiveNode(ReadU16LE(data, offset)));
                        offset += 2;
                    }
                }

                // 4. Mastery effects: {masteryID u16 LE, effectID u16 LE}
                //    NOTE: PoePlanner swaps the order vs GGG — mastery node ID comes first, effect second.
                if (offset + 1 < data.Length)
                {
                    int masteryCount = ReadU16LE(data, offset);
                    offset += 2;
                    for (int i = 0; i < masteryCount; i++)
                    {
                        if (offset + 3 >= data.Length) break;
                        int masteryId = ReadU16LE(data, offset);
                        ushort effectId = (ushort)ReadU16LE(data, offset + 2);

                        nodes.RemoveAll(n => n.Id == masteryId);
                        nodes.Add(new TargetPassiveNode(masteryId, effectId));
                        offset += 4;
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

        private static int ReadU16LE(byte[] data, int offset)
        {
            return data[offset] | (data[offset + 1] << 8);
        }

        /// <summary>
        /// Returns a list of nodes that are reachable (neighbors of allocated nodes) and are in the target list.
        /// </summary>
        public static List<TargetPassiveNode> GetReachableTargetNodes(IEnumerable<TargetPassiveNode> targetNodes, HashSet<int> allocatedIds = null)
        {
            if (!LokiPoe.IsInGame) return new List<TargetPassiveNode>();

            if (allocatedIds == null)
                allocatedIds = LokiPoe.InstanceInfo.PassiveSkillIds.Select(id => (int)id).ToHashSet();
            var reachableTargets = new List<TargetPassiveNode>();

            // Get dictionaries to check existence before calling CanBeAllocate (avoids log spam)
            var passiveDict = LokiPoe.InGameState.SkillsUi.Dictionary_Passive;
            var ascendDict = LokiPoe.InGameState.SkillsUi.Dictionary_Ascend;

            foreach (var target in targetNodes)
            {
                if (allocatedIds.Contains(target.Id)) continue;

                // Only check if it's in the UI dictionary to avoid "Unable to find id" errors
                bool inUi = (passiveDict != null && passiveDict.ContainsKey(target.Id)) || 
                             (ascendDict != null && ascendDict.ContainsKey(target.Id));
                
                if (!inUi) continue;

                if (LokiPoe.InGameState.SkillsUi.CanBeAllocate(target.Id))
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
