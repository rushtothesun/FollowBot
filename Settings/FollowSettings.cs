using System.Collections.ObjectModel;
using System.ComponentModel;
using DreamPoeBot.Loki.Common;
using Newtonsoft.Json;

namespace FollowBot.Settings
{
    public class FollowSettings : INotifyPropertyChanged
    {
        private int _followDistance = 15;
        private int _maxfollowDistance = 25;
        private int _maxCombatDistance = 40;
        private int _maxLootDistance = 40;
        private int _portOutThreshold = 0;
        private bool _followInTown = false;
        private bool _followInHideout = false;
        private bool _followInHeistHub = false;
        private bool _dontPortOutofMap = false;
        private bool _shouldFollow = true;
        private bool _autoReloadPathfinder = false;
        private bool _interactQuest = true;
        private bool _clickShrines = true;
        private bool _activateMirageSpawners = false;
        private int _mirageSpawnerDistance = 40;
        private ObservableCollection<string> _partyAndTradeWhitelist;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DefaultValue(15)]
        public int FollowDistance
        {
            get { return _followDistance; }
            set
            { _followDistance = value; NotifyPropertyChanged(nameof(FollowDistance)); }
        }

        [DefaultValue(25)]
        public int MaxFollowDistance
        {
            get { return _maxfollowDistance; }
            set
            { _maxfollowDistance = value; NotifyPropertyChanged(nameof(MaxFollowDistance)); }
        }

        [DefaultValue(40)]
        public int MaxCombatDistance
        {
            get { return _maxCombatDistance; }
            set
            { _maxCombatDistance = value; NotifyPropertyChanged(nameof(MaxCombatDistance)); }
        }

        [DefaultValue(40)]
        public int MaxLootDistance
        {
            get { return _maxLootDistance; }
            set
            { _maxLootDistance = value; NotifyPropertyChanged(nameof(MaxLootDistance)); }
        }

        [DefaultValue(0)]
        public int PortOutThreshold
        {
            get { return _portOutThreshold; }
            set
            {
                _portOutThreshold = value;
                NotifyPropertyChanged(nameof(PortOutThreshold));
            }
        }

        [DefaultValue(false)]
        public bool FollowInTown
        {
            get { return _followInTown; }
            set { _followInTown = value; NotifyPropertyChanged(nameof(FollowInTown)); }
        }

        [DefaultValue(false)]
        public bool FollowInHideout
        {
            get { return _followInHideout; }
            set { _followInHideout = value; NotifyPropertyChanged(nameof(FollowInHideout)); }
        }

        [DefaultValue(false)]
        public bool FollowInHeistHub
        {
            get { return _followInHeistHub; }
            set { _followInHeistHub = value; NotifyPropertyChanged(nameof(FollowInHeistHub)); }
        }

        [DefaultValue(false)]
        public bool DontPortOutofMap
        {
            get { return _dontPortOutofMap; }
            set
            { _dontPortOutofMap = value; NotifyPropertyChanged(nameof(DontPortOutofMap)); }
        }

        [DefaultValue(true)]
        public bool ShouldFollow
        {
            get { return _shouldFollow; }
            set
            { _shouldFollow = value; NotifyPropertyChanged(nameof(ShouldFollow)); }
        }

        [DefaultValue(false)]
        public bool AutoReloadPathfinder
        {
            get => _autoReloadPathfinder;
            set
            {
                if (value.Equals(_autoReloadPathfinder)) return;
                _autoReloadPathfinder = value;
                NotifyPropertyChanged(nameof(AutoReloadPathfinder));
            }
        }

        [DefaultValue(true)]
        public bool InteractQuest
        {
            get { return _interactQuest; }
            set
            {
                _interactQuest = value; NotifyPropertyChanged(nameof(InteractQuest));
            }
        }

        [DefaultValue(true)]
        public bool ClickShrines
        {
            get { return _clickShrines; }
            set
            {
                _clickShrines = value; NotifyPropertyChanged(nameof(ClickShrines));
            }
        }


        [DefaultValue(false)]
        public bool ActivateMirageSpawners
        {
            get { return _activateMirageSpawners; }
            set
            {
                _activateMirageSpawners = value; NotifyPropertyChanged(nameof(ActivateMirageSpawners));
            }
        }

        [DefaultValue(40)]
        public int MirageSpawnerDistance
        {
            get { return _mirageSpawnerDistance; }
            set
            {
                _mirageSpawnerDistance = value; NotifyPropertyChanged(nameof(MirageSpawnerDistance));
            }
        }

        /// <summary>
        /// A list of account names or character names to allow for invites/trades.
        /// </summary>
        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<string> PartyAndTradeWhitelist
        {
            get => _partyAndTradeWhitelist ?? (_partyAndTradeWhitelist = new ObservableCollection<string>());
            set
            {
                if (value.Equals(_partyAndTradeWhitelist))
                {
                    return;
                }
                _partyAndTradeWhitelist = value;
                NotifyPropertyChanged(nameof(PartyAndTradeWhitelist));
            }
        }

        public void UpdatePartyAndTradeWhitelist()
        {
            NotifyPropertyChanged(nameof(PartyAndTradeWhitelist));
        }
    }
}