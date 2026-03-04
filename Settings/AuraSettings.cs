using System.ComponentModel;

namespace FollowBot.Settings
{
    public class AuraSettings : INotifyPropertyChanged
    {
        private bool _enableAspectsOfTheAvian = false;
        private bool _enableAspectsOfTheCat = false;
        private bool _enableAspectsOfTheCrab = false;
        private bool _enableAspectsOfTheSpider = false;
        private BloodAndSand _bloodOrSand = BloodAndSand.Sand;
        private bool _ignoreHiddenAuras = false;
        private bool _enableBlasphemyCurses = true;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DefaultValue(false)]
        public bool EnableAspectsOfTheAvian
        {
            get => _enableAspectsOfTheAvian;
            set
            {
                _enableAspectsOfTheAvian = value;
                NotifyPropertyChanged(nameof(EnableAspectsOfTheAvian));
            }
        }

        [DefaultValue(false)]
        public bool EnableAspectsOfTheCat
        {
            get => _enableAspectsOfTheCat;
            set
            {
                _enableAspectsOfTheCat = value;
                NotifyPropertyChanged(nameof(EnableAspectsOfTheCat));
            }
        }

        [DefaultValue(false)]
        public bool EnableAspectsOfTheCrab
        {
            get => _enableAspectsOfTheCrab;
            set
            {
                _enableAspectsOfTheCrab = value;
                NotifyPropertyChanged(nameof(EnableAspectsOfTheCrab));
            }
        }

        [DefaultValue(false)]
        public bool EnableAspectsOfTheSpider
        {
            get => _enableAspectsOfTheSpider;
            set
            {
                _enableAspectsOfTheSpider = value;
                NotifyPropertyChanged(nameof(EnableAspectsOfTheSpider));
            }
        }

        [DefaultValue(BloodAndSand.Sand)]
        public BloodAndSand BloodOrSand
        {
            get => _bloodOrSand;
            set
            {
                _bloodOrSand = value;
                NotifyPropertyChanged(nameof(BloodOrSand));
            }
        }

        [DefaultValue(false)]
        public bool IgnoreHiddenAuras
        {
            get { return _ignoreHiddenAuras; }
            set
            { _ignoreHiddenAuras = value; NotifyPropertyChanged(nameof(IgnoreHiddenAuras)); }
        }

        [DefaultValue(true)]
        public bool EnableBlasphemyCurses
        {
            get => _enableBlasphemyCurses;
            set
            {
                _enableBlasphemyCurses = value;
                NotifyPropertyChanged(nameof(EnableBlasphemyCurses));
            }
        }

        public enum BloodAndSand
        {
            Blood,
            Sand
        }
    }
}