using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

namespace FollowBot.Settings
{
    public class WishSettings : INotifyPropertyChanged
    {
        private bool _autoWish = false;
        private ObservableCollection<string> _wishPriority;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DefaultValue(false)]
        public bool AutoWish
        {
            get { return _autoWish; }
            set
            {
                _autoWish = value;
                NotifyPropertyChanged(nameof(AutoWish));
            }
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<string> WishPriority
        {
            get => _wishPriority ?? (_wishPriority = SetupDefaultWishPriority());
            set
            {
                _wishPriority = value;
                NotifyPropertyChanged(nameof(WishPriority));
            }
        }

        public void UpdateWishPriority()
        {
            NotifyPropertyChanged(nameof(WishPriority));
        }

        public static ObservableCollection<string> SetupDefaultWishPriority()
        {
            return new ObservableCollection<string>
            {
                "Wish for Providence",
                "Wish for Pursuit",
                "Wish for Prosperity",
                "Wish for Gold",
                "Wish for Wealth",
                "Wish for Glittering",
                "Wish for Foreknowledge",
                "Wish for Scarabs",
                "Wish for Troves",
                "Wish for Fortune",
                "Wish for Glyphs"
            };
        }
    }
}
