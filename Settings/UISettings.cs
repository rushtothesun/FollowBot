using System.ComponentModel;

namespace FollowBot.Settings
{
    public class UISettings : INotifyPropertyChanged
    {
        private string _checkboxColor = "Black";

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DefaultValue("Black")]
        public string CheckboxColor
        {
            get => _checkboxColor;
            set
            {
                if (value == _checkboxColor) return;
                _checkboxColor = value;
                NotifyPropertyChanged(nameof(CheckboxColor));
            }
        }
    }
}