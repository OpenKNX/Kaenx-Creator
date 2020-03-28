using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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

namespace Kaenx.Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Models.ModelGeneral _general;


        public Models.ModelGeneral General
        {
            get { return _general; }
            set { _general = value; Changed("General"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
        }

        private void ClickNew(object sender, RoutedEventArgs e)
        {
            General = new Models.ModelGeneral();
            SetButtons(true);
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private void ClickAddVersion(object sender, RoutedEventArgs e)
        {
            //int newId = 0;
            //if (General.Versions.Count > 0)
            //    newId = General.Versions[General.Versions.Count - 1].VersionNumber + 1;

            //General.Versions.Add(new Models.AppVersion());
        }

        private void Test(object sender, RoutedEventArgs e)
        {

        }

        private void ClickOpenVersion(object sender, RoutedEventArgs e)
        {

        }

        private void ClickAddDevice(object sender, RoutedEventArgs e)
        {
            General.Devices.Add(new Models.Device());
        }

        private void ClickOpenDevice(object sender, RoutedEventArgs e)
        {

        }

        private void ClickSave(object sender, RoutedEventArgs e)
        {
            foreach(Models.Device dev in General.Devices)
            {
                if (!dev.HasApplicationProgramm)
                    dev.App = null;
            }

            string general = Newtonsoft.Json.JsonConvert.SerializeObject(General);

            SaveFileDialog diag = new SaveFileDialog();
            diag.FileName = General.ProjectName;
            diag.Title = "Projekt speichern";
            diag.Filter = "Kaenx hersteller Projekt|*.ae-manu";
            
            if(diag.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(diag.FileName, general);
            }
        }

        private void ClickOpen(object sender, RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = "Projekt öffnen";
            diag.Filter = "Kaenx hersteller Projekt|*.ae-manu";
            if(diag.ShowDialog() == true)
            {
                string general = System.IO.File.ReadAllText(diag.FileName);
                General = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.ModelGeneral>(general);
                SetButtons(true);
            }
        }



        private void SetButtons(bool enable)
        {
            MenuSave.IsEnabled = enable;
            MenuClose.IsEnabled = enable;
            MenuVersion.IsEnabled = enable;
            MenuDevices.IsEnabled = enable;
            MenuPublish.IsEnabled = enable;
            TabsEdit.IsEnabled = enable;
        }
    }
}
