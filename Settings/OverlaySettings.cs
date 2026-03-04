using System.ComponentModel;
using FollowBot.Class;

namespace FollowBot.Settings
{
    public class OverlaySettings : INotifyPropertyChanged
    {
        private bool _enableOverlay = false;
        private bool _drawInBackground = false;
        private bool _drawMobs = false;
        private bool _drawCorpses = false;
        private int _fps = 30;
        private int _overlayXCoord = 15;
        private int _overlayYCoord = 70;
        private int _overlayTransparency = 70;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        [DefaultValue(false)]
        public bool EnableOverlay
        {
            get => _enableOverlay;
            set
            {
                if (value == _enableOverlay) return;
                _enableOverlay = value;
                NotifyPropertyChanged(nameof(EnableOverlay));
            }
        }

        [DefaultValue(false)]
        public bool DrawInBackground
        {
            get => _drawInBackground;
            set
            {
                if (value == _drawInBackground) return;
                _drawInBackground = value;
                NotifyPropertyChanged(nameof(DrawInBackground));
            }
        }

        [DefaultValue(false)]
        public bool DrawMobs
        {
            get => _drawMobs;
            set
            {
                if (value == _drawMobs) return;
                _drawMobs = value;
                NotifyPropertyChanged(nameof(DrawMobs));
            }
        }

        [DefaultValue(false)]
        public bool DrawCorpses
        {
            get => _drawCorpses;
            set
            {
                if (value == _drawCorpses) return;
                _drawCorpses = value;
                NotifyPropertyChanged(nameof(DrawCorpses));
            }
        }

        [DefaultValue(30)]
        public int FPS
        {
            get => _fps;
            set
            {
                if (value == _fps) return;
                _fps = value;
                if (OverlayWindow.Instance != null)
                    OverlayWindow.Instance.SetFps(_fps);
                NotifyPropertyChanged(nameof(FPS));
            }
        }

        [DefaultValue(15)]
        public int OverlayXCoord
        {
            get => _overlayXCoord;
            set
            {
                if (value == _overlayXCoord) return;
                _overlayXCoord = value;
                NotifyPropertyChanged(nameof(OverlayXCoord));
            }
        }

        [DefaultValue(70)]
        public int OverlayYCoord
        {
            get => _overlayYCoord;
            set
            {
                if (value == _overlayYCoord) return;
                _overlayYCoord = value;
                NotifyPropertyChanged(nameof(OverlayYCoord));
            }
        }

        [DefaultValue(70)]
        public int OverlayTransparency
        {
            get => _overlayTransparency;
            set
            {
                if (value == _overlayTransparency) return;
                _overlayTransparency = value;
                if (OverlayWindow.Instance != null)
                    OverlayWindow.Instance.SetTransparency(_overlayTransparency);
                NotifyPropertyChanged(nameof(OverlayTransparency));
            }
        }
    }
}