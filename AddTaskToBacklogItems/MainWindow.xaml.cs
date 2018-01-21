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
using System.IO;
using System.Runtime.Serialization.Json;
using Microsoft.Win32;

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
        public List<StoryItem> StoryItems { get; set; }

        private SettingsViewModel settingsViewModel;

        public MainWindow()
        {
            InitializeComponent();
            settingsViewModel = new SettingsViewModel();
            this.DataContext = settingsViewModel;
        }
      
        private void btnGo_Click(object sender, RoutedEventArgs e)
        {
            //// Settings settings = new Settings();
            //// TFSRepository tfsRepository = new TFSRepository(settings);
            //var wis = tfsRepository.GetWorkItemStore();
            //var wic = tfsRepository.GetListOfBacklogItemsWithoutWork(wis, settings); // settings.TfsArea, settings.TfsIteration, settings.NewTaskExceptionFilter, settings.NewTaskStoryExceptionFilter);
            //var result = tfsRepository.AddSQATaskToEachWorkItemInCollection(wis, wic, settings);
            //if (result.Count > 0)
            //{
            //    // Show a modal dialog.
            //    btnPreview_Click(null, null);
            //}
            //// tfsRepository.GetCurrentIterationPath(wis);
        }

        private void btnPreview_Click(object sender, RoutedEventArgs e)
        {
            //var wis = tfsRepository.GetWorkItemStore();
            //var wic = tfsRepository.GetListOfBacklogItemsWithoutWork(wis, settings); //  settings.TfsArea, settings.TfsIteration, settings.NewTaskExceptionFilter, settings.NewTaskStoryExceptionFilter);
            //StoryItems = tfsRepository.GetStoryItems(wic);
            //// dataGridOfStoryItems.ItemsSource = sil;
            //settings.IsVerified = (StoryItems.Count > 0);
        }

        // https://docs.microsoft.com/en-us/dotnet/framework/wcf/feature-details/how-to-serialize-and-deserialize-json-data
        private void btnExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.FileName = "settings";
            saveFileDialog.DefaultExt = ".json";
            saveFileDialog.Filter = "JavaScript Object Notation (.json)|*.json";

            Nullable<bool> result = saveFileDialog.ShowDialog();

            if (result == true)
            {
                string saveFilename = saveFileDialog.FileName;
                FileStream serializedFileStream = new FileStream(saveFilename, FileMode.Create);
                // MemoryStream serializedStream = new MemoryStream();
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Settings[]));
                // serializer.WriteObject(serializedStream, settings);
                Settings[] settingsArray = new Settings[] { settingsViewModel.Settings };
                serializer.WriteObject(serializedFileStream, settingsArray);
            }
        }

        private void btnImport_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.FileName = "settings";
            openFileDialog.DefaultExt = ".json";
            openFileDialog.Filter = "JavaScript Object Notation (.json)|*.json";

            Nullable<bool> result = openFileDialog.ShowDialog();

            if (result == true)
            {
                string openFilename = openFileDialog.FileName;
                try
                {
                    FileStream serializedFileStream = new FileStream(openFilename, FileMode.Open);
                    DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Settings[]));
                    var settingsArray = serializer.ReadObject(serializedFileStream);
                    if (settingsArray != null && (settingsArray as Settings[]) != null && ((settingsArray as Settings[]).Count() > 0))
                    {
                        settingsViewModel.Settings = (settingsArray as Settings[])[0];
                        // InitializeData();
                    }
                }
                catch (IOException exception)
                {
                    tbErrorMessage.Text = exception.Message;
                }
            }
        }

        private void dataGridOfStoryItems_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var chk = VisualTreeHelpers.FindAncestor<CheckBox>((DependencyObject)e.OriginalSource, "cbStoryItem");

            if (chk == null)
                e.Handled = true;
        }

        private void cbStoryItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var chk = (CheckBox)sender;
            var row = VisualTreeHelpers.FindAncestor<DataGridRow>(chk);
            var newValue = !chk.IsChecked.GetValueOrDefault();

            row.IsSelected = newValue;
            chk.IsChecked = newValue;

            // Mark event as handled so that the default 
            // DataGridPreviewMouseDown doesn't handle the event
            e.Handled = true;
        }

        //private void InitializeComponent()
        //{
        //    TFSRepository tfsRepository = new TFSRepository();
        //    var wis = tfsRepository.GetWorkItemStore();
        //    tfsRepository.CreateTest(wis);
        //}
    }
}