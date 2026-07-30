using System.ComponentModel;
using static FollowBot.Settings.LabSettings;

namespace FollowBot.Settings
{
    public class StashSettings : INotifyPropertyChanged
    {
        private bool _useGuildStash = false;
        private bool _autoDepositOnMapExit = false;

        // Regular stash tab settings
        private StashTabMode _regularTabMode = StashTabMode.Name;
        private string _regularStashTab = "Dump";
        private int _regularStashTabIndex = 0;

        // Guild stash tab settings
        private StashTabMode _guildTabMode = StashTabMode.Name;
        private string _guildStashTab = "Dump";
        private int _guildStashTabIndex = 0;

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

        // ── Regular Stash ──

        [DefaultValue(StashTabMode.Name)]
        public StashTabMode RegularTabMode
        {
            get => _regularTabMode;
            set
            {
                _regularTabMode = value;
                NotifyPropertyChanged(nameof(RegularTabMode));
            }
        }

        [DefaultValue("Dump")]
        public string RegularStashTab
        {
            get => _regularStashTab;
            set
            {
                _regularStashTab = value;
                NotifyPropertyChanged(nameof(RegularStashTab));
            }
        }

        [DefaultValue(0)]
        public int RegularStashTabIndex
        {
            get => _regularStashTabIndex;
            set
            {
                _regularStashTabIndex = value;
                NotifyPropertyChanged(nameof(RegularStashTabIndex));
            }
        }

        // ── Guild Stash ──

        [DefaultValue(StashTabMode.Name)]
        public StashTabMode GuildTabMode
        {
            get => _guildTabMode;
            set
            {
                _guildTabMode = value;
                NotifyPropertyChanged(nameof(GuildTabMode));
            }
        }

        [DefaultValue("Dump")]
        public string GuildStashTab
        {
            get => _guildStashTab;
            set
            {
                _guildStashTab = value;
                NotifyPropertyChanged(nameof(GuildStashTab));
            }
        }

        [DefaultValue(0)]
        public int GuildStashTabIndex
        {
            get => _guildStashTabIndex;
            set
            {
                _guildStashTabIndex = value;
                NotifyPropertyChanged(nameof(GuildStashTabIndex));
            }
        }
    }
}
