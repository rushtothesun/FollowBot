using System.Collections.ObjectModel;
using System.ComponentModel;
using DreamPoeBot.Loki.Common;
using Newtonsoft.Json;

namespace FollowBot.Settings
{
    /// <summary>
    /// Wrapper class for passive tree URLs to support two-way binding in XAML.
    /// Strings are immutable, so we need a class with a property that can be updated.
    /// </summary>
    public class PassiveTreeUrlEntry : INotifyPropertyChanged
    {
        private string _url = "";

        public event PropertyChangedEventHandler PropertyChanged;

        public string Url
        {
            get => _url;
            set
            {
                if (_url != value)
                {
                    _url = value;
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Url)));
                }
            }
        }

        public PassiveTreeUrlEntry() { }

        public PassiveTreeUrlEntry(string url)
        {
            _url = url;
        }
    }

    public class PassiveSkillTreeSettings : INotifyPropertyChanged
    {
        private bool _enableAutoAllocation;
        private bool _enableAscendancyAllocation = true;
        private bool _onlyInSafeZone = true;
        private bool _checkLeaderStationary = true;
        private int _safeMonsterDistance = 50;
        private ObservableCollection<PassiveTreeUrlEntry> _passiveTreeUrls = new ObservableCollection<PassiveTreeUrlEntry>
        {
            new PassiveTreeUrlEntry()
        };

        public event PropertyChangedEventHandler PropertyChanged;

        public PassiveSkillTreeSettings()
        {
        }

        public bool EnableAutoAllocation
        {
            get => _enableAutoAllocation;
            set
            {
                _enableAutoAllocation = value;
                NotifyPropertyChanged(nameof(EnableAutoAllocation));
            }
        }

        public bool EnableAscendancyAllocation
        {
            get => _enableAscendancyAllocation;
            set
            {
                _enableAscendancyAllocation = value;
                NotifyPropertyChanged(nameof(EnableAscendancyAllocation));
            }
        }

        public bool OnlyInSafeZone
        {
            get => _onlyInSafeZone;
            set
            {
                _onlyInSafeZone = value;
                NotifyPropertyChanged(nameof(OnlyInSafeZone));
            }
        }

        public bool CheckLeaderStationary
        {
            get => _checkLeaderStationary;
            set
            {
                _checkLeaderStationary = value;
                NotifyPropertyChanged(nameof(CheckLeaderStationary));
            }
        }

        public int SafeMonsterDistance
        {
            get => _safeMonsterDistance;
            set
            {
                _safeMonsterDistance = value;
                NotifyPropertyChanged(nameof(SafeMonsterDistance));
            }
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<PassiveTreeUrlEntry> PassiveTreeUrls
        {
            get => _passiveTreeUrls;
            set
            {
                _passiveTreeUrls = value;
                NotifyPropertyChanged(nameof(PassiveTreeUrls));
            }
        }

        private void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
