using System.ComponentModel;

namespace FollowBot.Settings
{
    public class TradeSettings : INotifyPropertyChanged
    {
        private bool _enableTradeDebugLog = false;
        private bool[,] _excludedTradeSlots = new bool[12, 5]; // 12x5 grid, default all false

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DefaultValue(false)]
        public bool EnableTradeDebugLog
        {
            get => _enableTradeDebugLog;
            set
            {
                _enableTradeDebugLog = value;
                NotifyPropertyChanged(nameof(EnableTradeDebugLog));
            }
        }

        public bool[,] ExcludedTradeSlots
        {
            get => _excludedTradeSlots;
            set
            {
                _excludedTradeSlots = value;
                NotifyPropertyChanged(nameof(ExcludedTradeSlots));
            }
        }

        // Helper method to get/set individual slots
        public bool IsSlotExcluded(int x, int y)
        {
            if (x < 0 || x >= 12 || y < 0 || y >= 5) return false;
            return _excludedTradeSlots[x, y];
        }

        public void SetSlotExcluded(int x, int y, bool excluded)
        {
            if (x < 0 || x >= 12 || y < 0 || y >= 5) return;
            _excludedTradeSlots[x, y] = excluded;
            NotifyPropertyChanged(nameof(ExcludedTradeSlots));
        }
    }
}