using System.ComponentModel;

namespace FollowBot.Settings
{
    public class ChatCommandSettings : INotifyPropertyChanged
    {
        private string _TeleportToLeaderChatCommand = "Tele";
        private string _stopFollowChatCommand = "StopF";
        private string _startFollowChatCommand = "StartF";
        private string _stopLootChatCommand = "StopL";
        private string _startLootChatCommand = "StartL";
        private string _stopAttackChatCommand = "StopA";
        private string _startAttackChatCommand = "StartA";
        private string _stopSentinelChatCommand = "StopD";
        private string _startSentinelChatCommand = "StartD";
        private string _stopAutoTeleportChatCommand = "StopP";
        private string _startAutoTeleportChatCommand = "StartP";
        private string _openTownPortalChatCommand = "OpenP";
        private string _enterPortalChatCommand = "EnterP";
        private string _depositStashChatCommand = "Stash";
        private string _newInstanceChatCommand = "NewI";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DefaultValue("EnterP")]
        public string EnterPortalChatCommand
        {
            get => _enterPortalChatCommand;
            set
            {
                _enterPortalChatCommand = value;
                NotifyPropertyChanged(nameof(EnterPortalChatCommand));
            }
        }

        [DefaultValue("Tele")]
        public string TeleportToLeaderChatCommand
        {
            get => _TeleportToLeaderChatCommand;
            set
            {
                _TeleportToLeaderChatCommand = value;
                NotifyPropertyChanged(nameof(TeleportToLeaderChatCommand));
            }
        }

        [DefaultValue("StopF")]
        public string StopFollowChatCommand
        {
            get => _stopFollowChatCommand;
            set
            {
                _stopFollowChatCommand = value;
                NotifyPropertyChanged(nameof(StopFollowChatCommand));
            }
        }

        [DefaultValue("StartF")]
        public string StartFollowChatCommand
        {
            get => _startFollowChatCommand;
            set
            {
                _startFollowChatCommand = value;
                NotifyPropertyChanged(nameof(StartFollowChatCommand));
            }
        }

        [DefaultValue("StopL")]
        public string StopLootChatCommand
        {
            get => _stopLootChatCommand;
            set
            {
                _stopLootChatCommand = value;
                NotifyPropertyChanged(nameof(StopLootChatCommand));
            }
        }

        [DefaultValue("StartL")]
        public string StartLootChatCommand
        {
            get => _startLootChatCommand;
            set
            {
                _startLootChatCommand = value;
                NotifyPropertyChanged(nameof(StartLootChatCommand));
            }
        }

        [DefaultValue("StopA")]
        public string StopAttackChatCommand
        {
            get => _stopAttackChatCommand;
            set
            {
                _stopAttackChatCommand = value;
                NotifyPropertyChanged(nameof(StopAttackChatCommand));
            }
        }

        [DefaultValue("StartA")]
        public string StartAttackChatCommand
        {
            get => _startAttackChatCommand;
            set
            {
                _startAttackChatCommand = value;
                NotifyPropertyChanged(nameof(StartAttackChatCommand));
            }
        }

        [DefaultValue("StopD")]
        public string StopSentinelChatCommand
        {
            get => _stopSentinelChatCommand;
            set
            {
                _stopSentinelChatCommand = value;
                NotifyPropertyChanged(nameof(StopSentinelChatCommand));
            }
        }

        [DefaultValue("StartD")]
        public string StartSentinelChatCommand
        {
            get => _startSentinelChatCommand;
            set
            {
                _startSentinelChatCommand = value;
                NotifyPropertyChanged(nameof(StartSentinelChatCommand));
            }
        }

        [DefaultValue("StopP")]
        public string StopAutoTeleportChatCommand
        {
            get => _stopAutoTeleportChatCommand;
            set
            {
                _stopAutoTeleportChatCommand = value;
                NotifyPropertyChanged(nameof(StopAutoTeleportChatCommand));
            }
        }

        [DefaultValue("StartP")]
        public string StartAutoTeleportChatCommand
        {
            get => _startAutoTeleportChatCommand;
            set
            {
                _startAutoTeleportChatCommand = value;
                NotifyPropertyChanged(nameof(StartAutoTeleportChatCommand));
            }
        }

        [DefaultValue("OpenP")]
        public string OpenTownPortalChatCommand
        {
            get { return _openTownPortalChatCommand; }
            set
            {
                _openTownPortalChatCommand = value;
                NotifyPropertyChanged(nameof(OpenTownPortalChatCommand));
            }
        }

        [DefaultValue("Stash")]
        public string DepositStashChatCommand
        {
            get => _depositStashChatCommand;
            set
            {
                _depositStashChatCommand = value;
                NotifyPropertyChanged(nameof(DepositStashChatCommand));
            }
        }

        [DefaultValue("NewI")]
        public string NewInstanceChatCommand
        {
            get => _newInstanceChatCommand;
            set
            {
                _newInstanceChatCommand = value;
                NotifyPropertyChanged(nameof(NewInstanceChatCommand));
            }
        }
    }
}