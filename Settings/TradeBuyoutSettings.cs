using System.ComponentModel;

namespace FollowBot.Settings
{
    public class TradeBuyoutSettings : INotifyPropertyChanged
    {
        private bool _checkAffordability = true;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DefaultValue(true)]
        public bool CheckAffordability
        {
            get => _checkAffordability;
            set
            {
                _checkAffordability = value;
                NotifyPropertyChanged(nameof(CheckAffordability));
            }
        }
    }
}
