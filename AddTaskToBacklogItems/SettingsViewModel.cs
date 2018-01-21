﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace AddTaskToBacklogItems
{
    class SettingsViewModel : ObservableObject
    {
        private Settings _settings;
        private TFSRepository _tfsRepository;
        private ObservableCollection<StoryItem> _storyItems = new ObservableCollection<StoryItem>();
        private ObservableCollection<string> _projects = new ObservableCollection<string>();
        private ObservableCollection<string> _areas = new ObservableCollection<string>();
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
            RetrieveAllAreaDependents();
        }

        private void RetrieveAllAreaDependents()
        {
            Iterations = new ObservableCollection<string>(_tfsRepository.GetIterationPaths(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject, Settings.TfsArea));
            // resources = tfsRepository.GetContributors(settings.TfsServer, settings.TfsWorkStore, settings.TfsProject).Select(u => u.DisplayName).ToList(); // new List<string>() { "Michael Welch" }; // tfsRepository.GetUsers(settings.TfsServer, settings.TfsWorkStore, settings.TfsProject);
            Resources = new ObservableCollection<string>(_tfsRepository.GetGroupAreaMembers(Settings.TfsServer, Settings.TfsWorkStore, Settings.TfsProject, Settings.GetWrappedTfsArea()).
                                                                         Select(u => u.DisplayName).OrderBy(o => o).ToList());
        }

        public ICommand RetrieveStoryItemsCommand
        {
            get { return new DelegateCommand(RetrieveStoryItems); }
        }

        private void RetrieveStoryItems()
        {
            var wis = _tfsRepository.GetWorkItemStore();
            var wic = _tfsRepository.GetListOfBacklogItemsWithoutWork(wis, Settings); //  settings.TfsArea, settings.TfsIteration, settings.NewTaskExceptionFilter, settings.NewTaskStoryExceptionFilter);
            StoryItems = new ObservableCollection<StoryItem>(_tfsRepository.GetStoryItems(wic));
        }

        public ObservableCollection<string> Projects
        {
            get { return _projects; }
            set { _projects = value;  RaisePropertyChangedEvent("Projects"); }
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
                    // Clean up old event handler
                    if (_settings != null)
                    {
                        _settings.PropertyChanged -= SettingsChanged;
                    }
                    _settings = value;
                    _settings.PropertyChanged += SettingsChanged;
                    RaisePropertyChangedEvent("Settings");
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
                case "TfsArea":
                    {
                        if (Settings != null)
                        {
                            RetrieveAllSettingDependents();
                        }
                    }
                    break;
                case "TfsIteration":
                    {
                        // Maybe here we check to verify the value.
                    }
                    break;
            }

        }

        public ObservableCollection<StoryItem> StoryItems
        {
            get { return _storyItems; }
            set { _storyItems = value; RaisePropertyChangedEvent("StoryItems"); }
        }
    }
}
