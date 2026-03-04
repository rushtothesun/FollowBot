using FollowBot.Class;
using System.Windows.Controls;

namespace FollowBot.Views.Tabs
{
    public partial class DefensiveSkillsTab : UserControl
    {
        public DefensiveSkillsTab()
        {
            InitializeComponent();
        }

        private void RemoveDefensiveSkillRule(object sender, System.Windows.RoutedEventArgs e)
        {
            DefensiveSkillsClass rule = (sender as Button).DataContext as DefensiveSkillsClass;
            FollowBotSettings.Instance.Combat.DefensiveSkills.Remove(rule);
        }
    }
}
