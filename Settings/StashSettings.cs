using System.ComponentModel;

namespace FollowBot.Settings
{
    public class StashSettings : INotifyPropertyChanged
    {
        private bool _useGuildStash = false;
        private string _targetStashTab = "Dump";
        private bool _autoDepositOnMapExit = false;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// If true, uses Guild Stash. If false, uses Regular Stash.
        /// </summary>
        [DefaultValue(false)]
        public bool UseGuildStash
        {
            get => _useGuildStash;
            set
            {
                _useGuildStash = value;
                NotifyPropertyChanged(nameof(UseGuildStash));
            }
        }

        /// <summary>
        /// Name of the stash tab to deposit items into.
        /// </summary>
        [DefaultValue("Dump")]
        public string TargetStashTab
        {
            get => _targetStashTab;
            set
            {
                _targetStashTab = value;
                NotifyPropertyChanged(nameof(TargetStashTab));
            }
        }

        /// <summary>
        /// If true, automatically deposit inventory to stash when exiting a map to hideout.
        /// </summary>
        [DefaultValue(false)]
        public bool AutoDepositOnMapExit
        {
            get => _autoDepositOnMapExit;
            set
            {
                _autoDepositOnMapExit = value;
                NotifyPropertyChanged(nameof(AutoDepositOnMapExit));
            }
        }
    }
}
