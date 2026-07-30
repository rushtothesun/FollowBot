using DreamPoeBot.Loki.Common;
using DreamPoeBot.Loki.Game;
using FollowBot.Settings;
using FollowBot.SimpleEXtensions;
using System;
using System.Collections.Generic;
using System.Linq;
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

        private async void TestSmartLogicButton_Click(object sender, RoutedEventArgs e)
        {
            TestSmartLogicButton.IsEnabled = false;
            try
            {
                string result = await Tasks.DivineFontTask.RefreshPricesAndCalculateSmartGemChoice();
                GlobalLog.Info($"[DivineFontTask] {result}");
                MessageBox.Show(result, "Smart Gem Choice Analysis", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[DivineFontTask] Error calculating smart gem choice: {ex.Message}");
                MessageBox.Show($"Error calculating smart gem choice:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                TestSmartLogicButton.IsEnabled = true;
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

        private void BrowseGemNames_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var gemNames = new HashSet<string>();

                var inventoryItems = LokiPoe.InGameState.InventoryUi.InventoryControl_Main?.Inventory?.Items;
                if (inventoryItems != null)
                {
                    foreach (var item in inventoryItems)
                    {
                        if (item != null && item.Class == "Active Skill Gem" && !item.IsCorrupted)
                            gemNames.Add(item.Name);
                    }
                }

                if (LokiPoe.InGameState.StashUi.IsOpened)
                {
                    var stashItems = LokiPoe.InGameState.StashUi.InventoryControl?.Inventory?.Items;
                    if (stashItems != null)
                    {
                        foreach (var item in stashItems)
                        {
                            if (item != null && item.Class == "Active Skill Gem" && !item.IsCorrupted)
                                gemNames.Add(item.Name);
                        }
                    }
                }

                if (gemNames.Count == 0)
                {
                    MessageBox.Show("No skill gems found in inventory or stash.", "Browse Gems", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var button = sender as Button;
                var option = button?.DataContext as DivineFontOption;
                if (option == null) return;

                var menu = new ContextMenu();
                foreach (var name in gemNames.OrderBy(n => n))
                {
                    var menuItem = new MenuItem { Header = name };
                    menuItem.Click += (s, args) => { option.GemName = name; };
                    menu.Items.Add(menuItem);
                }
                menu.PlacementTarget = button;
                menu.IsOpen = true;
            }
            catch (Exception ex)
            {
                GlobalLog.Error($"[BrowseGemNames] Error: {ex.Message}");
                MessageBox.Show($"Error browsing gems:\n{ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddPassiveTreeUrl_Click(object sender, RoutedEventArgs e)
        {
            FollowBotSettings.Instance.PassiveTree.PassiveTreeUrls.Add(new PassiveTreeUrlEntry());
        }

        private void RemovePassiveTreeUrl_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var entry = button?.DataContext as PassiveTreeUrlEntry;
            if (entry != null)
            {
                FollowBotSettings.Instance.PassiveTree.PassiveTreeUrls.Remove(entry);
            }
        }

        private void AddWishPriority_Click(object sender, RoutedEventArgs e)
        {
            string text = WishPriorityTextBox.Text?.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var list = FollowBotSettings.Instance.Wish.WishPriority;
            if (!list.Contains(text))
            {
                list.Add(text);
                FollowBotSettings.Instance.Wish.UpdateWishPriority();
                WishPriorityTextBox.Text = "";
            }
        }

        private void RemoveWishPriority_Click(object sender, RoutedEventArgs e)
        {
            if (WishPriorityListBox.SelectedItem == null) return;
            var selected = WishPriorityListBox.SelectedItem.ToString();
            FollowBotSettings.Instance.Wish.WishPriority.Remove(selected);
            FollowBotSettings.Instance.Wish.UpdateWishPriority();
        }

        private void ResetWishPriority_Click(object sender, RoutedEventArgs e)
        {
            FollowBotSettings.Instance.Wish.WishPriority = Settings.WishSettings.SetupDefaultWishPriority();
        }

        private void MoveWishUp_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext as string;
            if (item == null) return;

            var list = FollowBotSettings.Instance.Wish.WishPriority;
            int index = list.IndexOf(item);
            if (index > 0)
            {
                list.Move(index, index - 1);
            }
        }

        private void MoveWishDown_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var item = button?.DataContext as string;
            if (item == null) return;

            var list = FollowBotSettings.Instance.Wish.WishPriority;
            int index = list.IndexOf(item);
            if (index < list.Count - 1)
            {
                list.Move(index, index + 1);
            }
        }

        private void WishPriorityListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e != null && e.AddedItems.Count > 0)
            {
                WishPriorityTextBox.Text = e.AddedItems[0].ToString();
            }
        }
    }
}
