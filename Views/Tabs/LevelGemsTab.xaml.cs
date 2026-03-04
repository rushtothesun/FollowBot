using DreamPoeBot.Loki.Common;
using FollowBot.SimpleEXtensions;
using System.Windows.Controls;

namespace FollowBot.Views.Tabs
{
    public partial class LevelGemsTab : UserControl
    {

        public LevelGemsTab()
        {
            InitializeComponent();
        }

        private void AddGlobalNameIgnoreButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            string text = GlobalNameIgnoreTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!FollowBotSettings.Instance.Gems.GlobalNameIgnoreList.Contains(text))
            {
                FollowBotSettings.Instance.Gems.GlobalNameIgnoreList.Add(text);
                FollowBotSettings.Instance.Gems.UpdateGlobalNameIgnoreList();
                GlobalNameIgnoreTextBox.Text = "";
            }
            else
            {
                GlobalLog.Error($"[AddGlobalNameIgnoreButtonOnClick] The skillgem {text} is already in the GlobalNameIgnoreList.");
            }
        }

        private void RemoveGlobalNameIgnoreButton_OnClick(object sender, System.Windows.RoutedEventArgs e)
        {
            string text = GlobalNameIgnoreTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (FollowBotSettings.Instance.Gems.GlobalNameIgnoreList.Contains(text))
            {
                FollowBotSettings.Instance.Gems.GlobalNameIgnoreList.Remove(text);
                FollowBotSettings.Instance.Gems.UpdateGlobalNameIgnoreList();
                GlobalNameIgnoreTextBox.Text = "";
            }
            else
            {
                GlobalLog.Error($"[RemoveGlobalNameIgnoreButtonOnClick] The skillgem {text} is not in the GlobalNameIgnoreList.");
            }
        }

        private void GlobalNameIgnoreListListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null && e.AddedItems.Count > 0)
            {
                GlobalNameIgnoreTextBox.Text = e.AddedItems[0].ToString();
            }
        }
    }
}
