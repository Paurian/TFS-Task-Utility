using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;

// TODO: Remove tfsBaseIterationQueryPath
namespace AddTaskToBacklogItems
{
    [DataContract]
    public class Settings : ObservableObject
    {
        // public event PropertyChangedEventHandler PropertyChanged;

        #region Constructors
        public Settings()
        {
            AppSettingsReader appSettings = new AppSettingsReader();
            TfsServer = (string)appSettings.GetValue("TfsServer", typeof(string));
            TfsWorkStore = (string)appSettings.GetValue("TfsWorkStore", typeof(string));
            TfsProject = (string)appSettings.GetValue("TfsProject", typeof(string));
            TfsBaseIterationQueryPath = (string)appSettings.GetValue("TfsBaseIterationQueryPath", typeof(string));
            TfsArea = (string)appSettings.GetValue("TfsArea", typeof(string));
            TfsTeam = (string)appSettings.GetValue("TfsTeam", typeof(string));
            TfsIteration = (string)appSettings.GetValue("TfsIteration", typeof(string));
            // TfsIterationPath = (string)appSettings.GetValue("TfsIterationPath", typeof(string));

            NewTaskTitleTemplate = (string)appSettings.GetValue("NewTaskTitleTemplate", typeof(string));
            NewTaskEstHours = (string)appSettings.GetValue("NewTaskEstHours", typeof(string));
            NewTaskAssignedTo = (string)appSettings.GetValue("NewTaskAssignedTo", typeof(string));
            NewTaskActivityType = (string)appSettings.GetValue("NewTaskActivityType", typeof(string));
            NewTaskExceptionFilter = (string)appSettings.GetValue("NewTaskExceptionFilter", typeof(string));
            NewTaskStoryExceptionFilter = (string)appSettings.GetValue("NewTaskStoryExceptionFilter", typeof(string));
        }

        public Settings(string tfsServer, string tfsWorkStore, string tfsProject, string tfsBaseIterationQueryPath, string tfsTeam, string tfsIteration) // , string tfsIterationPath)
        {
            TfsServer = tfsServer;
            TfsWorkStore = tfsWorkStore;
            TfsProject = tfsProject;
            TfsBaseIterationQueryPath = tfsBaseIterationQueryPath;
            TfsTeam = tfsTeam;
            TfsIteration = tfsIteration;
            // TfsIterationPath = tfsIterationPath;
        }
        #endregion Constructors

        #region Properties
        #region Non-Serialized Properties
        #region Read-Only Properties
        public bool HasIterationValue { get { return !String.IsNullOrWhiteSpace(TfsIteration); } }
        public bool HasProjectValue { get { return !String.IsNullOrWhiteSpace(TfsProject); } }
        public bool HasAreaValue { get { return !String.IsNullOrWhiteSpace(TfsArea); } }
        public bool HasTeamValue { get { return !String.IsNullOrWhiteSpace(TfsTeam); } }
        #endregion Read-Only Properties

        private bool isVerified = false;
        public bool IsVerified { get { return isVerified; } set { if (isVerified != value) { isVerified = value; NotifyPropertyChanged(); } } }
        #endregion Non-Serialized Properties

        [DataMember]
        internal string tfsServer;
        public string TfsServer { get { return tfsServer; } set { if (tfsServer != value) { tfsServer = value; NotifyPropertyChanged(); } } }

        [DataMember]
        internal string tfsWorkStore;
        public string TfsWorkStore { get { return tfsWorkStore; } set { if (tfsWorkStore != value) { tfsWorkStore = value; NotifyPropertyChanged(); } } }

        [DataMember]
        internal string tfsProject;
        public string TfsProject { get { return tfsProject; } set { if (tfsProject != value) { tfsProject = value; NotifyPropertyChanged(); } } }

        [DataMember]
        internal string tfsBaseIterationQueryPath;
        public string TfsBaseIterationQueryPath { get { return tfsBaseIterationQueryPath;  } set { if (tfsBaseIterationQueryPath != value) { tfsBaseIterationQueryPath = value;  } } }

        [DataMember]
        internal string tfsArea;
        public string TfsArea { get { return tfsArea; } set { if (tfsArea != value) { tfsArea = value; NotifyPropertyChanged(); } } }
        // public string TfsAreaPath { get { return TfsProject + @"\" + TfsArea; } }

        [DataMember]
        internal string tfsTeam;
        public string TfsTeam { get { return tfsTeam; } set { if (tfsTeam != value) { tfsTeam = value; NotifyPropertyChanged(); } } }

        [DataMember]
        internal string tfsIteration;
        public string TfsIteration { get { return tfsIteration; } set { if (tfsIteration != value) { tfsIteration = value; /** TfsIterationPath = value; **/ NotifyPropertyChanged(); } } }

        //private string tfsIterationPath = String.Empty;
        //public string TfsIterationPath { get { return String.IsNullOrEmpty(tfsIterationPath) ? TfsAreaPath + @"\" + TfsIteration : tfsIterationPath; } set { tfsIterationPath = value; NotifyPropertyChanged(); } }

        [DataMember]
        internal string newTaskTitleTemplate;
        public string NewTaskTitleTemplate { get { return newTaskTitleTemplate; } set { if (newTaskTitleTemplate != value) { newTaskTitleTemplate = value; NotifyPropertyChanged(); } } }

        [DataMember]
        internal int newTaskEstHours;
        public string NewTaskEstHours { get { return newTaskEstHours.ToString(); } set { int _nteh = 0; int.TryParse(value, out _nteh); if (newTaskEstHours != _nteh) { newTaskEstHours = _nteh; NotifyPropertyChanged(); } } } // should we make this double: e.g. 0.25, 0.5, 1.0, 1.5?

        [DataMember]
        internal string newTaskAssignedTo;
        public string NewTaskAssignedTo { get { return newTaskAssignedTo; } set { if (newTaskAssignedTo != value) { newTaskAssignedTo = value; NotifyPropertyChanged(); } } }

        [DataMember]
        internal string newTaskActivityType;
        public string NewTaskActivityType { get { return newTaskActivityType; } set { if (newTaskActivityType != value) { newTaskActivityType = value; NotifyPropertyChanged(); } } }

        [DataMember]
        internal string newTaskExceptionFilter;
        public string NewTaskExceptionFilter { get { return newTaskExceptionFilter; } set { if (newTaskExceptionFilter != value) { newTaskExceptionFilter = value; NotifyPropertyChanged(); } } }

        [DataMember]
        internal string newTaskStoryExceptionFilter;
        public string NewTaskStoryExceptionFilter { get { return newTaskStoryExceptionFilter; } set { if (newTaskStoryExceptionFilter != value) { newTaskStoryExceptionFilter = value; NotifyPropertyChanged(); } } }
        #endregion Properties

        #region Methods
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
                    RaisePropertyChangedEvent("IsVerified");
                }
            }

            switch (propertyName)
            {
                case "TfsIteration":
                    RaisePropertyChangedEvent("HasIterationValue");
                    break;
                case "TfsProject":
                    RaisePropertyChangedEvent("HasProjectValue");
                    break;
                case "TfsArea":
                    RaisePropertyChangedEvent("HasAreaValue");
                    break;
                case "TfsTeam":
                    RaisePropertyChangedEvent("HasTeamValue");
                    break;
            }
            RaisePropertyChangedEvent(propertyName);
        }

        // This method wraps the TFS area for the Group Member search.
        public string GetWrappedTfsArea()
        {
            return TfsArea?.Replace("Centracs\\", "[Centracs]\\");
        }
        #endregion Methods
    }
}
