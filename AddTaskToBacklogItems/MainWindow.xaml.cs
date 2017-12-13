using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AddTaskToBacklogItems
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Settings settings;
        private List<string> projects;
        private List<string> areas;
        private List<string> releases;
        private List<string> resources; // Was List<UserResources>
        private List<string> activities;
        private TFSRepository tfsRepository;
        public List<StoryItem> StoryItems { get; set; }
        public bool isVerified = false;

        public MainWindow()
        {
            InitializeComponent();

            settings = new Settings();
            tfsRepository = new TFSRepository(settings);

            TfsToolSettings.DataContext = settings;
            GetComboBoxListData(settings);
            ApplyItemSources();
        }

        private void ApplyItemSources()
        {
            cbProject.ItemsSource = projects;
            cbArea.ItemsSource = areas;
            cbRelease.ItemsSource = releases;
            cbNewAssignedTo.ItemsSource = resources;
            cbNewActivity.ItemsSource = activities;
        }

        // If we have a TfsServer, get the options for Work Store, Project, Area, Etc.
        private void GetComboBoxListData(Settings settings)
        {
            projects = tfsRepository.GetProjectNames(settings.TfsServer, settings.TfsWorkStore);
            areas = tfsRepository.GetAreas(settings.TfsServer, settings.TfsWorkStore, settings.TfsProject);
            releases = tfsRepository.GetIterationPaths(settings.TfsServer, settings.TfsWorkStore, settings.TfsProject, settings.TfsArea);
            activities = new List<string>() { "Testing" };
            resources = new List<string>() { "Michael Welch" }; // tfsRepository.GetUsers(settings.TfsServer, settings.TfsWorkStore, settings.TfsProject);
        }

        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            // Settings settings = new Settings();
            // TFSRepository tfsRepository = new TFSRepository(settings);
            var wis = tfsRepository.GetWorkItemStore();
            var wic = tfsRepository.GetListOfBacklogItemsWithoutWork(wis, settings); // settings.TfsArea, settings.TfsIteration, settings.NewTaskExceptionFilter, settings.NewTaskStoryExceptionFilter);
            var result = tfsRepository.AddSQATaskToEachWorkItemInCollection(wis, wic, settings);
            if (result.Count > 0)
            {
                // Show a modal dialog.
                btnPreview_Click(null, null);
            }
            // tfsRepository.GetCurrentIterationPath(wis);
        }

        private void cbProject_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            areas = tfsRepository.GetAreas(settings.TfsServer, settings.TfsWorkStore, settings.TfsProject);
            releases = tfsRepository.GetIterationPaths(settings.TfsServer, settings.TfsWorkStore, settings.TfsProject);
            // resources = tfsRepository.GetUsers(settings.TfsServer, settings.TfsWorkStore, settings.TfsProject);

            cbArea.SelectedValue = null;
            cbNewAssignedTo.SelectedValue = null;

            cbArea.ItemsSource = areas;
            cbRelease.ItemsSource = releases;
        }

        private void cbArea_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            releases = tfsRepository.GetIterationPaths(settings.TfsServer, settings.TfsWorkStore, settings.TfsProject, settings.TfsArea);
            cbRelease.SelectedValue = null;
            cbRelease.ItemsSource = releases;
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            var wis = tfsRepository.GetWorkItemStore();
            var wic = tfsRepository.GetListOfBacklogItemsWithoutWork(wis, settings); //  settings.TfsArea, settings.TfsIteration, settings.NewTaskExceptionFilter, settings.NewTaskStoryExceptionFilter);
            var sil = tfsRepository.GetStoryItems(wic);
            dataGridOfStoryItems.ItemsSource = sil;
            settings.IsVerified = (sil.Count > 0);
        }

        //private void InitializeComponent()
        //{
        //    TFSRepository tfsRepository = new TFSRepository();
        //    var wis = tfsRepository.GetWorkItemStore();
        //    tfsRepository.CreateTest(wis);
        //}
    }
}