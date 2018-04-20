using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AddTaskToBacklogItems
{
    public class SettingsViewModel : ObservableObject
    {
        private Settings _settings;
        private TFSRepository _tfsRepository;
        private ObservableCollection<StoryItem> _storyItems = new ObservableCollection<StoryItem>();
        private ObservableCollection<string> _projects = new ObservableCollection<string>();
        private ObservableCollection<string> _areas = new ObservableCollection<string>();
        private ObservableCollection<string> _teams = new ObservableCollection<string>();
        private ObservableCollection<string> _iterations = new ObservableCollection<string>();
        private ObservableCollection<string> _activities = new ObservableCollection<string>();
        private ObservableCollection<string> _resources = new ObservableCollection<string>();

        public SettingsViewModel()
        {
            Settings = new Settings();
            _tfsRepository = new TFSRepository(_settings);

            this.PropertyChanged += SettingsViewModel_PropertyChanged;

            Activities = new ObservableCollection<string>(_tfsRepository.GetActivities());
            RetrieveAllSettingDependents();
            Settings.TfsIteration = SetRelativeIteration(Settings.TfsIteration);
        }

        private List<IterationItem> GetIterations()
        {
            return _tfsRepository.GetIterationsFromTeam(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject, Settings.TfsTeam);
        }

        private string SetRelativeIteration(string iterationType)
        {
            string result = iterationType;

            if (iterationType.First() == '{' && iterationType.Last() == '}')
            {
                // Get current/next/prev if the request is in the Settings.
                IterationItem iteration = null;
                if (iterationType.ToLower() == "{current}")
                {
                    iteration = _tfsRepository.GetCurrentIteration(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject, Settings.TfsTeam);
                }
                else if (iterationType.ToLower() == "{next}")
                {
                    iteration = _tfsRepository.GetNextIteration(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject, Settings.TfsTeam);
                }
                else if (iterationType.ToLower() == "{prior}" || iterationType.ToLower() == "{prev}" || iterationType.ToLower() == "{previous}")
                {
                    iteration = _tfsRepository.GetPreviousIteration(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject, Settings.TfsTeam);
                }
                result = (iteration == null) ? String.Empty : iteration.QueryPath;
            }

            return result;
        }

        void SettingsViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "Settings")
            {
                if (Settings != null)
                {
                    RetrieveAllSettingDependents();
                }
            }
            //if (e.PropertyName == "Projects")
            //{
            //    if (Settings.TfsProject != null)
            //    {
            //        RetrieveAllProjectDependents();
            //    }
            //}
            //if (e.PropertyName == "Areas")
            //{
            //    if (Settings.TfsArea != null)
            //    {
            //        RetrieveAllAreaDependents();
            //    }
            //}
        }
        
        private void RetrieveAllSettingDependents()
        {
            Projects = new ObservableCollection<string>(_tfsRepository.GetProjectNames(Settings.TfsServer, Settings.TfsWorkStore));
            RetrieveAllProjectDependents();
        }

        private void RetrieveAllProjectDependents()
        {
            Areas = new ObservableCollection<string>(_tfsRepository.GetAreas(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject));
            Teams = new ObservableCollection<string>(_tfsRepository.GetTeams(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject));
            RetrieveAllAreaDependents();
        }

        private void RetrieveAllAreaDependents()
        {
            // Iterations = new ObservableCollection<string>(_tfsRepository.GetIterationPaths(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject, Settings.TfsArea));
            Iterations = new ObservableCollection<string>(_tfsRepository.GetIterationPathsFromTeamSortedByDate(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject, Settings.TfsTeam));
            Iterations.Insert(0, String.Empty); // Add an empty line at the top in case there are tasks directly associated with a team without an iteration (BLIs not in a project?)

            // TODO: Try to do this without Area
            // resources = tfsRepository.GetContributors(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject).Select(u => u.DisplayName).ToList(); // new List<string>() { "Michael Welch" }; // tfsRepository.GetUsers(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject);
            Resources = new ObservableCollection<string>(_tfsRepository.GetGroupAreaMembers(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject, Settings.GetWrappedTfsArea()).
                                                                         Select(u => u.DisplayName).OrderBy(o => o).ToList());
        }

        #region External Commands
        public ICommand RetrieveStoryItemsCommand
        {
            get { return new DelegateCommand(RetrieveStoryItems); }
        }

        public ICommand CreateTaskItemsCommand
        {
            get { return new DelegateCommand(CreateTaskItems); }
        }

        public ICommand ToggleStoryItemSelectionCommand
        {
            get { return new DelegateCommand(param => ToggleStoryItemSelection(param)); }
        }
        #endregion External Commands

        private void RetrieveStoryItems()
        {
            var wis = _tfsRepository.GetWorkItemStore();
            var wic = _tfsRepository.GetListOfBacklogItemsWithoutWork(wis, Settings);
            StoryItems = new ObservableCollection<StoryItem>(_tfsRepository.GetStoryItems(wic));
            if (StoryItems.Count > 0)
            {
                Settings.IsVerified = true;
            }
        }

        private void ToggleStoryItemSelection(object storyItem) // StoryItem is always null. Parameter isn't getting passed correctly.
        {
            ((StoryItem)storyItem).IsSelected = !((StoryItem)storyItem).IsSelected;
            // RaisePropertyChangedEvent("StoryItems");
            Settings.IsVerified = StoryItems.Where(s => s.IsSelected).Any();
        }

        private void CreateTaskItems()
        {
            if (Settings.IsVerified)
            {
                var wis = _tfsRepository.GetWorkItemStore();
                var wic = _tfsRepository.GetListOfBacklogItemsWithoutWork(wis, Settings);
                var result = _tfsRepository.AddSQATaskToEachWorkItemInCollection(wis, wic, Settings, StoryItems.ToList());
                if (result.Count > 0)
                {
                    ActionState = String.Format("{0} new tasks have been created.", result);
                    Settings.IsVerified = false;
                }
            }
        }

        public ObservableCollection<string> Projects
        {
            get { return _projects; }
            set { _projects = value; RaisePropertyChangedEvent("Projects"); }
        }

        public ObservableCollection<string> Teams
        {
            get { return _teams; }
            set { _teams = value; RaisePropertyChangedEvent("Teams"); }
        }

        public ObservableCollection<string> Areas
        {
            get { return _areas; }
            set { _areas = value; RaisePropertyChangedEvent("Areas"); }
        }

        public ObservableCollection<string> Iterations
        {
            get { return _iterations; }
            set { _iterations = value; RaisePropertyChangedEvent("Iterations"); }
        }

        public ObservableCollection<string> Activities
        {
            get { return _activities; }
            set { _activities = value; RaisePropertyChangedEvent("Activities"); }
        }

        public ObservableCollection<string> Resources
        {
            get { return _resources; }
            set { _resources = value; RaisePropertyChangedEvent("Resources"); }
        }

        public Settings Settings
        {
            get { return _settings; }
            set
            {
                if (_settings != value)
                {
                    string innerSettingsChanged = String.Empty;

                    // Clean up old event handler and prepare to raise SettingsChanged if we've been initialized.
                    if (_settings != null)
                    {
                        _settings.PropertyChanged -= SettingsChanged;

                        if (_settings?.TfsIteration != value?.TfsIteration)
                        {
                            innerSettingsChanged = "TfsIteration";
                        }
                        else if (
                            _settings?.TfsArea != value?.TfsArea ||
                            _settings?.TfsTeam != value?.TfsTeam ||
                            _settings?.TfsProject != value?.TfsProject ||
                            _settings?.TfsWorkStore != value?.TfsWorkStore ||
                            _settings?.TfsServer != value?.TfsServer)
                        {
                            innerSettingsChanged = "TfsServer";
                        }
                    }
                    _settings = value;
                    _settings.PropertyChanged += SettingsChanged;
                    RaisePropertyChangedEvent("Settings");
                    if (innerSettingsChanged != String.Empty)
                    {
                        SettingsChanged(null, new PropertyChangedEventArgs(innerSettingsChanged));
                    }
                }
            }
        }

        void SettingsChanged(object sender, PropertyChangedEventArgs e)
        {
            switch (e.PropertyName)
            {
                case "TfsServer":
                case "TfsWorkStore":
                case "TfsProject":
                case "TfsTeam":
                case "TfsArea":
                    {
                        if (Settings != null)
                        {
                            RetrieveAllSettingDependents();
                            if (!String.IsNullOrEmpty(Settings.TfsTeam))
                            {
                                Settings.TfsIteration = SetRelativeIteration("{current}");
                            }
                        }
                    }
                    break;
                case "TfsIteration":
                    {
                        // Automatically load the story items
                        RetrieveStoryItems();
                    }
                    break;
            }

        }

        private string _actionState;
        public string ActionState
        {
            get { return _actionState; }
            set { if ( _actionState != value) { _actionState = value; RaisePropertyChangedEvent("ActionState"); } }
        }

        public ObservableCollection<StoryItem> StoryItems
        {
            get { return _storyItems; }
            set { _storyItems = value; RaisePropertyChangedEvent("StoryItems"); }
        }
    }
}
