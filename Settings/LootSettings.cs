using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

namespace FollowBot.Settings
{
    public class LootSettings : INotifyPropertyChanged
    {
        private bool _shouldLoot = true;
        private bool _shouldLootOnlyQuestItem = false;
        private bool _shouldOpenChests = true;
        private bool _shouldLootUltimatum = false;
        private int _ultimatumLootTimer = 5;
        private bool _useUnloaderForUltimatum = true;
        private int _ultimatumUnloaderDelay = 2000;
        private ObservableCollection<string> _lootBlacklist;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DefaultValue(true)]
        public bool ShouldLoot
        {
            get { return _shouldLoot; }
            set
            { _shouldLoot = value; NotifyPropertyChanged(nameof(ShouldLoot)); }
        }

        [DefaultValue(false)]
        public bool ShouldLootOnlyQuestItem
        {
            get { return _shouldLootOnlyQuestItem; }
            set
            {
                _shouldLootOnlyQuestItem = value; NotifyPropertyChanged(nameof(ShouldLootOnlyQuestItem));
            }
        }

        [DefaultValue(true)]
        public bool ShouldOpenChests
        {
            get { return _shouldOpenChests; }
            set
            {
                _shouldOpenChests = value; NotifyPropertyChanged(nameof(ShouldOpenChests));
            }
        }

        [DefaultValue(false)]
        public bool ShouldLootUltimatum
        {
            get { return _shouldLootUltimatum; }
            set 
            { 
                if (_shouldLootUltimatum == value) return;
                _shouldLootUltimatum = value; 
                NotifyPropertyChanged(nameof(ShouldLootUltimatum)); 
                if (value)
                {
                    UseUnloaderForUltimatum = false;
                }
            }
        }

        [DefaultValue(true)]
        public bool UseUnloaderForUltimatum
        {
            get { return _useUnloaderForUltimatum; }
            set
            {
                if (_useUnloaderForUltimatum == value) return;
                _useUnloaderForUltimatum = value; 
                NotifyPropertyChanged(nameof(UseUnloaderForUltimatum));
                if (value)
                {
                    ShouldLootUltimatum = false;
                }
            }
        }

        [DefaultValue(2000)]
        public int UltimatumUnloaderDelay
        {
            get { return _ultimatumUnloaderDelay; }
            set
            {
                // Clamp to 1500-10000ms range
                var clamped = value < 1500 ? 1500 : (value > 10000 ? 10000 : value);
                if (_ultimatumUnloaderDelay == clamped) return;
                _ultimatumUnloaderDelay = clamped;
                NotifyPropertyChanged(nameof(UltimatumUnloaderDelay));
            }
        }

        [DefaultValue(5)]
        public int UltimatumLootTimer
        {
            get { return _ultimatumLootTimer; }
            set { _ultimatumLootTimer = value; NotifyPropertyChanged(nameof(UltimatumLootTimer)); }
        }

        /// <summary>
        /// A list of blacklisted items that won't be picked up.
        /// Format: "[N] itemName" or "[M] metadata"
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<string> LootBlacklist
        {
            get => _lootBlacklist ?? (_lootBlacklist = new ObservableCollection<string>());
            set
            {
                if (value.Equals(_lootBlacklist)) return;
                _lootBlacklist = value;
                NotifyPropertyChanged(nameof(LootBlacklist));
            }
        }

        public void UpdateLootBlacklist()
        {
            NotifyPropertyChanged(nameof(LootBlacklist));
        }
    }
}