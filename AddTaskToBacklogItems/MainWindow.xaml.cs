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
        //private Settings settings;
        //private List<string> projects;
        //private List<string> areas;
        //private List<string> releases;
        //private List<string> resources; // Was List<UserResources>
        //private List<string> activities;
        //public List<StoryItem> StoryItems { get; set; }

        //private SettingsViewModel settingsViewModel;

        public MainWindow()
        {
            InitializeComponent();
            //settingsViewModel = new SettingsViewModel();
            //this.DataContext = settingsViewModel;
            //this.Resources.Add("data", settingsViewModel);
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
                DataContractJsonSerializer serializer = new DataContractJsonSerializer(typeof(Settings[]));
                var dc = this.DataContext as SettingsViewModel;
                if (dc != null)
                {
                    Settings[] settingsArray = new Settings[] { dc.Settings };
                    serializer.WriteObject(serializedFileStream, settingsArray);
                }
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
                    // Eventually we want to ask the user which setting in the set to use.
                    if (settingsArray != null && (settingsArray as Settings[]) != null && ((settingsArray as Settings[]).Count() > 0))
                    {
                        var dc = this.DataContext as SettingsViewModel;
                        if (dc != null)
                        {
                            dc.Settings = (settingsArray as Settings[])[0];
                        }
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
            {
                e.Handled = true;
            }
        }

        private void cbStoryItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var chk = (CheckBox)sender;
            var row = VisualTreeHelpers.FindAncestor<DataGridRow>(chk);
            //var newValue = !chk.IsChecked.GetValueOrDefault();

            //row.IsSelected = newValue;
            //chk.IsChecked = newValue;

            //// Only if there's something selected is the list verified that the "Create New Tasks" button may be enabled.
            //settingsViewModel.Settings.IsVerified = settingsViewModel.StoryItems.Where(s => s.IsSelected).Any();

            //// Mark event as handled so that the default 
            //// DataGridPreviewMouseDown doesn't handle the event
            //e.Handled = true;
        }
    }
}