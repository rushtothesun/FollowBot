using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using Newtonsoft.Json;

namespace FollowBot.Settings
{
    public class LabSettings : INotifyPropertyChanged
    {
        public enum GemColor
        {
            Smart,
            Any,
            Red,
            Green,
            Blue,
        }

        public enum StashTabMode
        {
            Name,
            Index
        }

        private bool _enableDivineFontHandling = false;
        private bool _useFirstInventorySlot = true;
        private bool _searchForSuitableGem = false;
        private int _gemValueSafetyThreshold = 10;
        private GemColor _gemColor = GemColor.Any;
        private StashTabMode _stashTabMode = StashTabMode.Index;
        private string _stashTabName = "";
        private int _stashTabIndex = 0;
        private ObservableCollection<DivineFontOption> _divineFontOptions;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public LabSettings()
        {
            if (_divineFontOptions == null)
                _divineFontOptions = SetupDefaultDivineFontOptions();
        }

        private void EnsureAllOptionTypesExist()
        {
            var defaults = SetupDefaultDivineFontOptions();
            foreach (var defaultOption in defaults)
            {
                if (!_divineFontOptions.Any(o => o.Type == defaultOption.Type))
                {
                    _divineFontOptions.Add(defaultOption);
                }
            }
        }

        [DefaultValue(GemColor.Any)]
        public GemColor Color
        {
            get => _gemColor;
            set
            {
                _gemColor = value;
                OnPropertyChanged(nameof(Color));
            }
        }

        [DefaultValue(10)]
        public int GemValueSafetyThreshold
        {
            get => _gemValueSafetyThreshold;
            set
            {
                _gemValueSafetyThreshold = value;
                OnPropertyChanged(nameof(GemValueSafetyThreshold));
            }
        }

        [DefaultValue(false)]
        public bool EnableDivineFontHandling
        {
            get => _enableDivineFontHandling;
            set
            {
                _enableDivineFontHandling = value;
                OnPropertyChanged(nameof(EnableDivineFontHandling));
            }
        }

        [DefaultValue(true)]
        public bool UseFirstInventorySlot
        {
            get => _useFirstInventorySlot;
            set
            {
                _useFirstInventorySlot = value;
                OnPropertyChanged(nameof(UseFirstInventorySlot));
            }
        }

        [DefaultValue(false)]
        public bool SearchForSuitableGem
        {
            get => _searchForSuitableGem;
            set
            {
                _searchForSuitableGem = value;
                OnPropertyChanged(nameof(SearchForSuitableGem));
            }
        }

        [DefaultValue(StashTabMode.Index)]
        public StashTabMode TabMode
        {
            get => _stashTabMode;
            set
            {
                _stashTabMode = value;
                OnPropertyChanged(nameof(TabMode));
            }
        }

        [DefaultValue("")]
        public string StashTabName
        {
            get => _stashTabName;
            set
            {
                _stashTabName = value;
                OnPropertyChanged(nameof(StashTabName));
            }
        }

        [DefaultValue(0)]
        public int StashTabIndex
        {
            get => _stashTabIndex;
            set
            {
                _stashTabIndex = value;
                OnPropertyChanged(nameof(StashTabIndex));
            }
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<DivineFontOption> DivineFontOptions
        {
            get => _divineFontOptions;
            set
            {
                _divineFontOptions = value;
                EnsureAllOptionTypesExist();
                OnPropertyChanged(nameof(DivineFontOptions));
            }
        }

        public ObservableCollection<DivineFontOption> SetupDefaultDivineFontOptions()
        {
            return new ObservableCollection<DivineFontOption>
            {
                new DivineFontOption
                {
                    Name = "Transform to random Transfigured Gem (same color)",
                    Type = DivineFontOptionType.TransformRandomSameColor,
                    IsEnabled = true,
                    Priority = 1
                },
                new DivineFontOption
                {
                    Name = "Transform specific gem to Transfigured version",
                    Type = DivineFontOptionType.TransformSpecificGem,
                    IsEnabled = false,
                    Priority = 2,
                    GemName = ""
                },
                new DivineFontOption
                {
                    Name = "Exchange Support Gem for Exceptional Gem",
                    Type = DivineFontOptionType.ExchangeForExceptional,
                    IsEnabled = false,
                    Priority = 3
                }
            };
        }
    }

    public enum DivineFontOptionType
    {
        TransformRandomSameColor,
        TransformSpecificGem,
        ExchangeForExceptional
    }

    public class DivineFontOption : INotifyPropertyChanged
    {
        private string _name;
        private DivineFontOptionType _type;
        private bool _isEnabled;
        private int _priority;
        private string _gemName; // For TransformSpecificGem type

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged(nameof(Name));
            }
        }

        public DivineFontOptionType Type
        {
            get => _type;
            set
            {
                _type = value;
                OnPropertyChanged(nameof(Type));
            }
        }

        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                _isEnabled = value;
                OnPropertyChanged(nameof(IsEnabled));
            }
        }

        public int Priority
        {
            get => _priority;
            set
            {
                _priority = value;
                OnPropertyChanged(nameof(Priority));
            }
        }

        public string GemName
        {
            get => _gemName;
            set
            {
                _gemName = value;
                OnPropertyChanged(nameof(GemName));
            }
        }
    }
}