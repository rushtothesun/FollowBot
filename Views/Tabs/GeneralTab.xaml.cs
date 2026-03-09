using DreamPoeBot.Loki.Common;
using FollowBot.Settings;
using FollowBot.SimpleEXtensions;
using System;
using System.Windows;
using System.Windows.Controls;

namespace FollowBot.Views.Tabs
{
    public partial class GeneralTab : UserControl
    {
        private CheckBox[,] slotCheckboxes = new CheckBox[12, 5];

        public GeneralTab()
        {
            InitializeComponent();
            InitializeTradeSlotGrid();
            GemColorComboBox.ItemsSource = Enum.GetValues(typeof(LabSettings.GemColor));

            // Check initial state to show/hide button
            UpdateSmartButtonVisibility();

            // Initialize login password box placeholder
            if (FollowBotSettings.Instance.Login.HasPassword)
            {
                LoginPasswordBox.Password = "********";
                LoginPasswordBox.Tag = "placeholder"; // sentinel to avoid re-encrypting placeholder
            }
        }

        private void LoginPasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox == null) return;

            // Skip if this is the initial placeholder load
            if (passwordBox.Tag as string == "placeholder")
            {
                passwordBox.Tag = null;
                return;
            }

            var plaintext = passwordBox.Password;
            if (string.IsNullOrEmpty(plaintext) || plaintext == "********")
                return;

            FollowBotSettings.Instance.Login.SetAndEncryptPassword(plaintext);
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
                        IsChecked = FollowBotSettings.Instance.Trade.IsSlotExcluded(x, y)
                    };

                    // Capture x,y in closure properly
                    int capturedX = x;
                    int capturedY = y;

                    checkbox.Checked += (s, e) =>
                        FollowBotSettings.Instance.Trade.SetSlotExcluded(capturedX, capturedY, true);
                    checkbox.Unchecked += (s, e) =>
                        FollowBotSettings.Instance.Trade.SetSlotExcluded(capturedX, capturedY, false);

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

        private void AddInviteTradeWhiteListButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = InviteTradeWhiteListTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (!FollowBotSettings.Instance.Follow.PartyAndTradeWhitelist.Contains(text))
            {
                FollowBotSettings.Instance.Follow.PartyAndTradeWhitelist.Add(text);
                FollowBotSettings.Instance.Follow.UpdatePartyAndTradeWhitelist();
                InviteTradeWhiteListTextBox.Text = "";
            }
            else
            {
                GlobalLog.Error($"[AddInviteTradeWhiteListButton_OnClick] The name {text} is already in the PartyAndTradeWhitelist.");
            }
        }

        private void RemoveInviteTradeWhiteListButton_OnClick(object sender, RoutedEventArgs e)
        {
            string text = InviteTradeWhiteListTextBox.Text;
            if (string.IsNullOrEmpty(text))
            {
                return;
            }

            if (FollowBotSettings.Instance.Follow.PartyAndTradeWhitelist.Contains(text))
            {
                FollowBotSettings.Instance.Follow.PartyAndTradeWhitelist.Remove(text);
                FollowBotSettings.Instance.Follow.UpdatePartyAndTradeWhitelist();
                InviteTradeWhiteListTextBox.Text = "";
            }
            else
            {
                GlobalLog.Error($"[RemoveInviteTradeWhiteListButton_OnClick] The name {text} is not in the PartyAndTradeWhitelist.");
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
            if (FollowBotSettings.Instance.Loot.LootBlacklist.Contains(entry))
            {
                MessageBox.Show($"'{text}' is already blacklisted.", "Duplicate",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            FollowBotSettings.Instance.Loot.LootBlacklist.Add(entry);
            FollowBotSettings.Instance.Loot.UpdateLootBlacklist();
            LootBlacklistTextBox.Text = string.Empty;
        }

        private void RemoveLootBlacklistButton_OnClick(object sender, RoutedEventArgs e)
        {
            if (LootBlacklistListBox.SelectedItem == null) return;

            string selected = LootBlacklistListBox.SelectedItem.ToString();
            FollowBotSettings.Instance.Loot.LootBlacklist.Remove(selected);
            FollowBotSettings.Instance.Loot.UpdateLootBlacklist();
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

        private void GemColorComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSmartButtonVisibility();
        }

        private void UpdateSmartButtonVisibility()
        {
            if (GemColorComboBox.SelectedItem != null &&
                GemColorComboBox.SelectedItem is LabSettings.GemColor selectedColor)
            {
                TestSmartLogicButton.Visibility = selectedColor == LabSettings.GemColor.Smart
                    ? Visibility.Visible
                    : Visibility.Collapsed;
            }
        }

        private void TestSmartLogicButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string result = Tasks.DivineFontTask.CalculateSmartGemChoice();
                GlobalLog.Info($"[DivineFontTask] {result}");
                MessageBox.Show(result, "Smart Gem Choice Analysis", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[DivineFontTask] Error calculating smart gem choice: {ex.Message}");
                MessageBox.Show($"Error calculating smart gem choice:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DivineFontOptionsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Handle selection changes if needed
        }

        private void MoveDivineFontOptionUp_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var option = button?.DataContext as DivineFontOption;
            if (option == null) return;

            var options = FollowBotSettings.Instance.Lab.DivineFontOptions;
            int index = options.IndexOf(option);

            if (index > 0)
            {
                // Swap priorities
                var previousOption = options[index - 1];
                int tempPriority = option.Priority;
                option.Priority = previousOption.Priority;
                previousOption.Priority = tempPriority;

                // Move in collection
                options.Move(index, index - 1);
            }
        }

        private void MoveDivineFontOptionDown_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var option = button?.DataContext as DivineFontOption;
            if (option == null) return;

            var options = FollowBotSettings.Instance.Lab.DivineFontOptions;
            int index = options.IndexOf(option);

            if (index < options.Count - 1)
            {
                // Swap priorities
                var nextOption = options[index + 1];
                int tempPriority = option.Priority;
                option.Priority = nextOption.Priority;
                nextOption.Priority = tempPriority;

                // Move in collection
                options.Move(index, index + 1);
            }
        }
    }
}
