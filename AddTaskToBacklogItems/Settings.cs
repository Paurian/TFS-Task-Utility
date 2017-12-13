using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace AddTaskToBacklogItems
{
    public class Settings : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public Settings()
        {
            AppSettingsReader appSettings = new AppSettingsReader();
            TfsServer = (string)appSettings.GetValue("TfsServer", typeof(string));
            TfsWorkStore = (string)appSettings.GetValue("TfsWorkStore", typeof(string));
            TfsProject = (string)appSettings.GetValue("TfsProject", typeof(string));
            TfsArea = (string)appSettings.GetValue("TfsArea", typeof(string));
            TfsIteration = (string)appSettings.GetValue("TfsIteration", typeof(string));
            // TfsIterationPath = (string)appSettings.GetValue("TfsIterationPath", typeof(string));

            NewTaskTitleTemplate = (string)appSettings.GetValue("NewTaskTitleTemplate", typeof(string));
            NewTaskEstHours = (string)appSettings.GetValue("NewTaskEstHours", typeof(string));
            NewTaskAssignedTo = (string)appSettings.GetValue("NewTaskAssignedTo", typeof(string));
            NewTaskActivityType = (string)appSettings.GetValue("NewTaskActivityType", typeof(string));
            NewTaskExceptionFilter = (string)appSettings.GetValue("NewTaskExceptionFilter", typeof(string));
            NewTaskStoryExceptionFilter = (string)appSettings.GetValue("NewTaskStoryExceptionFilter", typeof(string));
        }

        public Settings(string tfsServer, string tfsWorkStore, string tfsProject, string tfsArea, string tfsIteration) // , string tfsIterationPath)
        {
            TfsServer = tfsServer;
            TfsWorkStore = tfsWorkStore;
            TfsProject = tfsProject;
            TfsArea = tfsArea;
            TfsIteration = tfsIteration;
            // TfsIterationPath = tfsIterationPath;
        }

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (propertyName != "IsVerified")
            {
                if (isVerified)
                {
                    isVerified = false;
                    PropertyChanged(this, new PropertyChangedEventArgs("IsVerified"));
                }
            }

            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool isVerified = false;
        public bool IsVerified { get { return isVerified; } set { isVerified = value; NotifyPropertyChanged(); } }

        private string tfsServerValue;
        public string TfsServer { get { return tfsServerValue; } set { tfsServerValue = value; NotifyPropertyChanged(); } }

        private string tfsWorkStoreValue;
        public string TfsWorkStore { get { return tfsWorkStoreValue; } set { tfsWorkStoreValue = value; NotifyPropertyChanged(); } }

        private string tfsProjectValue;
        public string TfsProject { get { return tfsProjectValue; } set { tfsProjectValue = value; NotifyPropertyChanged(); } }

        private string tfsAreaValue;
        public string TfsArea { get { return tfsAreaValue; } set { tfsAreaValue = value; NotifyPropertyChanged(); } }
        // public string TfsAreaPath { get { return TfsProject + @"\" + TfsArea; } }

        public string tfsIterationValue;
        public string TfsIteration { get { return tfsIterationValue; } set { tfsIterationValue = value; /** TfsIterationPath = value; **/ NotifyPropertyChanged(); } }

        //private string tfsIterationPath = String.Empty;
        //public string TfsIterationPath { get { return String.IsNullOrEmpty(tfsIterationPath) ? TfsAreaPath + @"\" + TfsIteration : tfsIterationPath; } set { tfsIterationPath = value; NotifyPropertyChanged(); } }

        private string newTaskTitleTemplateValue;
        public string NewTaskTitleTemplate { get { return newTaskTitleTemplateValue; } set { newTaskTitleTemplateValue = value; NotifyPropertyChanged(); } }

        int newTaskEstHoursValue;
        public string NewTaskEstHours { get { return newTaskEstHoursValue.ToString(); } set { int.TryParse(value, out newTaskEstHoursValue); NotifyPropertyChanged(); } } // should we make this double: e.g. 0.25, 0.5, 1.0, 1.5?

        private string newTaskAssignedToValue;
        public string NewTaskAssignedTo { get { return newTaskAssignedToValue; } set { newTaskAssignedToValue = value; NotifyPropertyChanged(); } }

        private string newTaskActivityTypeValue;
        public string NewTaskActivityType { get { return newTaskActivityTypeValue; } set { newTaskActivityTypeValue = value; NotifyPropertyChanged(); } }

        private string newTaskExceptionFilterValue;
        public string NewTaskExceptionFilter { get { return newTaskExceptionFilterValue; } set { newTaskExceptionFilterValue = value; NotifyPropertyChanged(); } }

        private string newTaskStoryExceptionFilterValue;
        public string NewTaskStoryExceptionFilter { get { return newTaskStoryExceptionFilterValue; } set { newTaskStoryExceptionFilterValue = value; NotifyPropertyChanged(); } }
    }
}
