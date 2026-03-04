using System.Collections.ObjectModel;
using System.ComponentModel;
using FollowBot.Class;
using Newtonsoft.Json;

namespace FollowBot.Settings
{
    public class CombatSettings : INotifyPropertyChanged
    {
        private bool _shouldKill = false;
        private bool _useStalkerSentinel = false;
        private ObservableCollection<DefensiveSkillsClass> _defensiveSkills;
        private ObservableCollection<FlasksClass> _flasks;

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public CombatSettings()
        {
            if (_defensiveSkills == null)
                _defensiveSkills = SetupDefaultDefensiveSkills();
            if (_flasks == null)
                _flasks = SetupDefaultFlasks();
        }

        [DefaultValue(false)]
        public bool ShouldKill
        {
            get { return _shouldKill; }
            set
            { _shouldKill = value; NotifyPropertyChanged(nameof(ShouldKill)); }
        }

        [DefaultValue(false)]
        public bool UseStalkerSentinel
        {
            get { return _useStalkerSentinel; }
            set
            { _useStalkerSentinel = value; NotifyPropertyChanged(nameof(UseStalkerSentinel)); }
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<DefensiveSkillsClass> DefensiveSkills
        {
            get => _defensiveSkills;
            set
            {
                _defensiveSkills = value;
                NotifyPropertyChanged(nameof(DefensiveSkills));
            }
        }

        [JsonProperty(ObjectCreationHandling = ObjectCreationHandling.Replace)]
        public ObservableCollection<FlasksClass> Flasks
        {
            get => _flasks;
            set
            {
                _flasks = value;
                NotifyPropertyChanged(nameof(Flasks));
            }
        }

        private ObservableCollection<DefensiveSkillsClass> SetupDefaultDefensiveSkills()
        {
            ObservableCollection<DefensiveSkillsClass> skills = new ObservableCollection<DefensiveSkillsClass>();

            skills.Add(new DefensiveSkillsClass(false, "Vaal Molten Shell", false, 0, 0, false, ""));
            skills.Add(new DefensiveSkillsClass(false, "Vaal Discipline", false, 0, 0, false, ""));
            skills.Add(new DefensiveSkillsClass(false, "Molten Shell", false, 0, 0, false, ""));
            skills.Add(new DefensiveSkillsClass(false, "Steelskin", false, 0, 0, false, ""));
            return skills;
        }

        private ObservableCollection<FlasksClass> SetupDefaultFlasks()
        {
            ObservableCollection<FlasksClass> flasks = new ObservableCollection<FlasksClass>();

            flasks.Add(new FlasksClass(false, 1, false, false, 0, 0, false));
            flasks.Add(new FlasksClass(false, 2, false, false, 0, 0, false));
            flasks.Add(new FlasksClass(false, 3, false, false, 0, 0, false));
            flasks.Add(new FlasksClass(false, 4, false, false, 0, 0, false));
            flasks.Add(new FlasksClass(false, 5, false, false, 0, 0, false));
            return flasks;
        }
    }
}