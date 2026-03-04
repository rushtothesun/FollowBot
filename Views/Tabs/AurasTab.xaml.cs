using FollowBot.Settings;
using System.Windows.Controls;

namespace FollowBot.Views.Tabs
{
    public partial class AurasTab : UserControl
    {
        public AurasTab()
        {
            InitializeComponent();
        }

        private void ChangeStance_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            if (FollowBotSettings.Instance.Auras.BloodOrSand == AuraSettings.BloodAndSand.Blood)
                FollowBotSettings.Instance.Auras.BloodOrSand = AuraSettings.BloodAndSand.Sand;
            else
                FollowBotSettings.Instance.Auras.BloodOrSand = AuraSettings.BloodAndSand.Blood;
        }
    }
}
