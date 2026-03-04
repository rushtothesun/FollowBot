using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using DreamPoeBot.Loki.Game;
using DreamPoeBot.Loki.Game.GameData;
using DreamPoeBot.Loki.Game.Objects;
using Newtonsoft.Json;

namespace FollowBot.Settings
{
    public class GemSettings : INotifyPropertyChanged
    {
        private bool _gemDebugStatements = false;
        private bool _levelGems = true;
        private bool _useLevelAllButton = true;
        private ObservableCollection<string> _globalNameIgnoreList;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Should the plugin log debug statements?
        /// </summary>
        [DefaultValue(false)]
        public bool GemDebugStatements
        {
            get => _gemDebugStatements;
            set
            {
                if (value.Equals(_gemDebugStatements))
                {
                    return;
                }
                _gemDebugStatements = value;
                NotifyPropertyChanged(nameof(GemDebugStatements));
            }
        }

        [DefaultValue(true)]
        public bool LevelGems
        {
            get => _levelGems;
            set
            {
                if (value.Equals(_levelGems))
                {
                    return;
                }
                _levelGems = value;
                NotifyPropertyChanged(nameof(LevelGems));
            }
        }

        [DefaultValue(true)]
        public bool UseLevelAllButton
        {
            get => _useLevelAllButton;
            set
            {
                if (value.Equals(_useLevelAllButton))
                {
                    return;
                }
                _useLevelAllButton = value;
                NotifyPropertyChanged(nameof(UseLevelAllButton));
            }
        }

        /// <summary>
        /// A list of skillgem names to ignore from leveling.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<string> GlobalNameIgnoreList
        {
            get => _globalNameIgnoreList ?? (_globalNameIgnoreList = new ObservableCollection<string>());
            set
            {
                if (value.Equals(_globalNameIgnoreList))
                {
                    return;
                }
                _globalNameIgnoreList = value;
                NotifyPropertyChanged(nameof(GlobalNameIgnoreList));
            }
        }

        /// <summary>
        /// A list of SkillGemEntry for the user's skillgems.
        /// </summary>
        [JsonIgnore]
        public ObservableCollection<SkillGemEntry> UserSkillGems
        {
            get
            {
                ObservableCollection<SkillGemEntry> skillGemEntries = new ObservableCollection<SkillGemEntry>();

                if (!LokiPoe.IsInGame)
                {
                    return skillGemEntries;
                }

                foreach (Inventory inv in UsableInventories)
                {
                    foreach (Item item in inv.Items)
                    {
                        if (item == null)
                        {
                            continue;
                        }

                        if (item.Components.SocketsComponent == null)
                        {
                            continue;
                        }

                        for (int idx = 0; idx < item.SocketedGems.Length; idx++)
                        {
                            Item gem = item.SocketedGems[idx];
                            if (gem == null)
                            {
                                continue;
                            }

                            skillGemEntries.Add(new SkillGemEntry(gem.Name, inv.PageSlot, idx));
                        }
                    }
                }
                return skillGemEntries;
            }
        }

        public void UpdateGlobalNameIgnoreList()
        {
            NotifyPropertyChanged(nameof(GlobalNameIgnoreList));
        }

        private static System.Collections.Generic.IEnumerable<Inventory> UsableInventories => new[]
        {
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.LeftHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.RightHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.OffLeftHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.OffRightHand),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Head),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Chest),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Gloves),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Boots),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.LeftRing),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.RightRing),
            LokiPoe.InstanceInfo.GetPlayerInventoryBySlot(InventorySlot.Neck)
        };

        public class SkillGemEntry
        {
            public string Name;
            public InventorySlot InventorySlot;
            public int SocketIndex;

            public string SerializationString { get; private set; }

            public SkillGemEntry(string name, InventorySlot slot, int socketIndex)
            {
                Name = name;
                InventorySlot = slot;
                SocketIndex = socketIndex;
                SerializationString = string.Format("{0} [{1}: {2}]", Name, InventorySlot, SocketIndex);
            }

            public Item InventoryItem
            {
                get
                {
                    return UsableInventories.Where(ui => ui.PageSlot == InventorySlot)
                        .Select(ui => ui.Items.FirstOrDefault())
                        .FirstOrDefault();
                }
            }

            public Item SkillGem
            {
                get
                {
                    Item item = InventoryItem;
                    if (item == null || item.Components.SocketsComponent == null)
                    {
                        return null;
                    }

                    Item sg = item.SocketedGems[SocketIndex];
                    if (sg == null)
                    {
                        return null;
                    }

                    if (sg.Name != Name)
                    {
                        return null;
                    }

                    return sg;
                }
            }
        }
    }
}