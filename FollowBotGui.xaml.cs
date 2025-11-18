using DreamPoeBot.Loki.Common;
using FollowBot.Class;
using log4net;
using System.Windows;
using System.Windows.Controls;

namespace FollowBot
{
    /// <summary>
    /// Logica di interazione per FollowBotGui.xaml
    /// </summary>
    public partial class FollowBotGui : UserControl
    {
        private static readonly ILog Log = Logger.GetLoggerInstanceForType();
        private CheckBox[,] slotCheckboxes = new CheckBox[12, 5];

        public FollowBotGui()
        {
            InitializeComponent();
            InitializeTradeSlotGrid();
        }

        private void InitializeTradeSlotGrid()
        {
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    var checkbox = new CheckBox
                    {
                        ToolTip = $"Slot ({x}, {y})",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center,
                        Width = 18,
                        Height = 18,
                        Margin = new Thickness(1),
                        IsChecked = FollowBotSettings.Instance.IsSlotExcluded(x, y)
                    };

                    // Capture x,y in closure properly
                    int capturedX = x;
                    int capturedY = y;

                    checkbox.Checked += (s, e) =>
                        FollowBotSettings.Instance.SetSlotExcluded(capturedX, capturedY, true);
                    checkbox.Unchecked += (s, e) =>
                        FollowBotSettings.Instance.SetSlotExcluded(capturedX, capturedY, false);

                    Grid.SetColumn(checkbox, x);
                    Grid.SetRow(checkbox, y);
                    TradeSlotGrid.Children.Add(checkbox);

                    slotCheckboxes[x, y] = checkbox;
                }
            }
        }

        private void ClearAllSlots_Click(object sender, RoutedEventArgs e)
        {
            for (int y = 0; y < 5; y++)
            {
                for (int x = 0; x < 12; x++)
                {
                    slotCheckboxes[x, y].IsChecked = false;
                }
            }
        }

        private void ExcludeLeftColumn_Click(object sender, RoutedEventArgs e)
        {
            for (int y = 0; y < 5; y++)
            {
                slotCheckboxes[0, y].IsChecked = true;
            }
        }

        private void RemoveDefensiveSkillRule(object sender, RoutedEventArgs e)
        {
            DefensiveSkillsClass rule = (sender as Button).DataContext as DefensiveSkillsClass;
            FollowBotSettings.Instance.DefensiveSkills.Remove(rule);
        }

        private void AddGlobalNameIgnoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = GlobalNameIgnoreTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!FollowBotSettings.Instance.GlobalNameIgnoreList.Contains(text))
            {
                FollowBotSettings.Instance.GlobalNameIgnoreList.Add(text);
                FollowBotSettings.Instance.UpdateGlobalNameIgnoreList();
                GlobalNameIgnoreTextBox.Text = "";
            }
            else
            {
                Log.ErrorFormat(
                    "[AddGlobalNameIgnoreButtonOnClick] The skillgem {0} is already in the GlobalNameIgnoreList.", text);
            }
        }

        private void RemoveGlobalNameIgnoreButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = GlobalNameIgnoreTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (FollowBotSettings.Instance.GlobalNameIgnoreList.Contains(text))
            {
                FollowBotSettings.Instance.GlobalNameIgnoreList.Remove(text);
                FollowBotSettings.Instance.UpdateGlobalNameIgnoreList();
                GlobalNameIgnoreTextBox.Text = "";
            }
            else
            {
                Log.ErrorFormat("[RemoveGlobalNameIgnoreButtonOnClick] The skillgem {0} is not in the GlobalNameIgnoreList.", text);
            }
        }
        private void GlobalNameIgnoreListListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null && e.AddedItems.Count > 0)
            {
                GlobalNameIgnoreTextBox.Text = e.AddedItems[0].ToString();
            }
        }

        private void AddInviteTradeWhiteListButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = InviteTradeWhiteListTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!FollowBotSettings.Instance.InviteTradeWhiteList.Contains(text))
            {
                FollowBotSettings.Instance.InviteTradeWhiteList.Add(text);
                FollowBotSettings.Instance.UpdateInviteTradeWhiteList();
                InviteTradeWhiteListTextBox.Text = "";
            }
            else
            {
                Log.ErrorFormat(
                    "[AddInviteTradeWhiteListButton_OnClick] The name {0} is already in the InviteTradeWhiteList.", text);
            }
        }

        private void RemoveInviteTradeWhiteListButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = InviteTradeWhiteListTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (FollowBotSettings.Instance.InviteTradeWhiteList.Contains(text))
            {
                FollowBotSettings.Instance.InviteTradeWhiteList.Remove(text);
                FollowBotSettings.Instance.UpdateInviteTradeWhiteList();
                InviteTradeWhiteListTextBox.Text = "";
            }
            else
            {
                Log.ErrorFormat("[RemoveInviteTradeWhiteListButton_OnClick] The name {0} is not in the InviteTradeWhiteList.", text);
            }
        }

        private void InviteTradeWhiteListListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null && e.AddedItems.Count > 0)
            {
                InviteTradeWhiteListTextBox.Text = e.AddedItems[0].ToString();
            }
        }

        private void AddLootBlacklistButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = LootBlacklistTextBox.Text.Trim();
            if (string.IsNullOrEmpty(text)) return;
            
            // Determine type based on radio button
            bool isMetadata = BlacklistByMetadata.IsChecked == true;
            string prefix = isMetadata ? "[M] " : "[N] ";
            string entry = prefix + text;
            
            // Check for duplicates
            if (FollowBotSettings.Instance.LootBlacklist.Contains(entry))
            {
                MessageBox.Show($"'{text}' is already blacklisted.", "Duplicate",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            FollowBotSettings.Instance.LootBlacklist.Add(entry);
            FollowBotSettings.Instance.UpdateLootBlacklist();
            LootBlacklistTextBox.Text = string.Empty;
        }

        private void RemoveLootBlacklistButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (LootBlacklistListBox.SelectedItem == null) return;
            
            string selected = LootBlacklistListBox.SelectedItem.ToString();
            FollowBotSettings.Instance.LootBlacklist.Remove(selected);
            FollowBotSettings.Instance.UpdateLootBlacklist();
        }

        private void LootBlacklistListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (LootBlacklistListBox.SelectedItem != null)
            {
                string selected = LootBlacklistListBox.SelectedItem.ToString();
                if (selected.StartsWith("[N] "))
                {
                    LootBlacklistTextBox.Text = selected.Substring(4);
                    BlacklistByName.IsChecked = true;
                }
                else if (selected.StartsWith("[M] "))
                {
                    LootBlacklistTextBox.Text = selected.Substring(4);
                    BlacklistByMetadata.IsChecked = true;
                }
            }
        }

        private void ChangeStance_OnClick(object sender, RoutedEventArgs e)
        {
            if (FollowBotSettings.Instance.BloorOrSand == FollowBotSettings.BloodAndSand.Blood)
                FollowBotSettings.Instance.BloorOrSand = FollowBotSettings.BloodAndSand.Sand;
            else
                FollowBotSettings.Instance.BloorOrSand = FollowBotSettings.BloodAndSand.Blood;
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void CheckBox_Checked_1(object sender, RoutedEventArgs e)
        {

        }
    }
}
