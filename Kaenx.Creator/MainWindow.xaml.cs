using Kaenx.Creator.Classes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Xml.Linq;

namespace Kaenx.Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Models.ModelGeneral _general;
        private string filePath = "";

        public Models.ModelGeneral General
        {
            get { return _general; }
            set { _general = value; Changed("General"); }
        }

        private ObservableCollection<Models.MaskVersion> bcus;
        public ObservableCollection<Models.MaskVersion> BCUs
        {
            get { return bcus; }
            set { bcus = value; Changed("BCUs"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            LoadBcus();
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


        private void LoadBcus()
        {
            string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "maskversion.json");
            string xmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "maskversion.xml");
            if (System.IO.File.Exists(jsonPath))
            {
                BCUs = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<Models.MaskVersion>>(System.IO.File.ReadAllText(jsonPath));
            } else
            {
                BCUs = new ObservableCollection<Models.MaskVersion>();
                XDocument xdoc = XDocument.Load(xmlPath);
                foreach(XElement xmask in xdoc.Root.Elements())
                {
                    Models.MaskVersion mask = new Models.MaskVersion();
                    mask.Id = xmask.Attribute("Id").Value;

                    string eleStr = xmask.ToString();
                    if (eleStr.Contains("<Procedure ProcedureType=\"Load\""))
                    {
                        XElement prodLoad = xmask.Descendants(XName.Get("Procedure")).First(p => p.Attribute("ProcedureType")?.Value == "Load");
                        if (prodLoad.ToString().Contains("<LdCtrlMerge"))
                            mask.Procedure = Models.ProcedureTypes.Merge;
                        else
                            mask.Procedure = Models.ProcedureTypes.Default;
                    } else
                    {
                        mask.Procedure = Models.ProcedureTypes.Application;
                    }


                    if(mask.Procedure != Models.ProcedureTypes.Application)
                    {
                        if (eleStr.Contains("<LdCtrlAbsSegment"))
                        {
                            mask.Memory = Models.MemoryTypes.Absolute;
                        }
                        else if (eleStr.Contains("<LdCtrlWriteRelMem"))
                        {
                            mask.Memory = Models.MemoryTypes.Relative;
                        }
                        else if (eleStr.Contains("<LdCtrlWriteMem"))
                        {
                            mask.Memory = Models.MemoryTypes.Relative;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    BCUs.Add(mask);
                }

                System.IO.File.WriteAllText(jsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(BCUs));
            }
        }


        #region Clicks

        #region Clicks Add/Remove

        private void ClickAddDevice(object sender, RoutedEventArgs e)
        {
            General.Devices.Add(new Models.Device());
        }

        private void ClickRemoveDevice(object sender, RoutedEventArgs e)
        {
            if(DeviceList.SelectedItem == null) return;

            Models.Device dev = DeviceList.SelectedItem as Models.Device;
            General.Devices.Remove(dev);
        }

        private void ClickAddParamRef(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            ver.ParameterRefs.Add(new Models.ParameterRef());
        }

        private void ClickRemoveParamRef(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;

            Models.ParameterRef dev = ParamRefList.SelectedItem as Models.ParameterRef;
            ver.ParameterRefs.Remove(dev);
        }

        private void ClickAddVersion(object sender, RoutedEventArgs e)
        {
            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion newVer = new Models.AppVersion();

            if(app.Versions.Count > 0){
                Models.AppVersion ver = app.Versions.OrderByDescending(v => v.Number).ElementAt(0);
                newVer.Number = ver.Number + 1;
            }

            app.Versions.Add(newVer);
        }

        private void ClickAddParamType(object sender, RoutedEventArgs e)
        {
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.ParameterTypes.Add(new Models.ParameterType());
        }

        private void ClickAddMemory(object sender, RoutedEventArgs e)
        {
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.Memories.Add(new Models.Memory());
        }

        private void ClickRemoveParamType(object sender, RoutedEventArgs e)
        {
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.ParameterTypes.Remove(ListParamTypes.SelectedItem as Models.ParameterType);
        }

        private void ClickRemoveMemory(object sender, RoutedEventArgs e)
        {
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.Memories.Remove(ListMemories.SelectedItem as Models.Memory);
        }

        private void ClickRemoveVersion(object sender, RoutedEventArgs e)
        {
            if(AppList.SelectedItem == null || VersionList.SelectedItem == null) return;

            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;

            app.Versions.Remove(ver);
        }

        private void ClickAddApp(object sender, RoutedEventArgs e)
        {
            Models.Application newApp = new Models.Application();
            newApp.Versions.Add(new Models.AppVersion());
            
            if(General.Applications.Count > 0){
                Models.Application app = General.Applications.OrderByDescending(a => a.Number).ElementAt(0);
                newApp.Number = app.Number + 1;
            }

            General.Applications.Add(newApp);
        }

        private void ClickRemoveApp(object sender, RoutedEventArgs e)
        {
            if(AppList.SelectedItem == null) return;

            Models.Application app = AppList.SelectedItem as Models.Application;
            General.Applications.Remove(app);
        }


        private void ClickAddParamEnum(object sender, RoutedEventArgs e)
        {
            Models.ParameterType type = ListParamTypes.SelectedItem as Models.ParameterType;

            type.Enums.Add(new Models.ParameterTypeEnum() { Name = "Name", Value = "Wert" });
        }


        private void ParamTypeChanged(object sender, SelectionChangedEventArgs e)
        {
            if ((sender as ComboBox).SelectedItem == null) return;
            Models.ParameterTypes type = (Models.ParameterTypes)(sender as ComboBox).SelectedItem;

            if (type == Models.ParameterTypes.Enum)
                Paramtype_Enum.Visibility = Visibility.Visible;
            else
                Paramtype_Enum.Visibility = Visibility.Collapsed;

            if (type == Models.ParameterTypes.Float9 ||
                type == Models.ParameterTypes.NumberInt ||
                type == Models.ParameterTypes.NumberUInt)
                Paramtype_MinMax.Visibility = Visibility.Visible;
            else
                Paramtype_MinMax.Visibility = Visibility.Collapsed;
        }


        private void ClickAddParam(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            ver.Parameters.Add(new Models.Parameter());
        }


        private void ClickRemoveParam(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            ver.Parameters.Remove(ParamList.SelectedItem as Models.Parameter);
        }


        private void ClickAddHardware(object sender, RoutedEventArgs e)
        {
            General.Hardware.Add(new Models.Hardware());
        }


        private void ClickRemoveHardware(object sender, RoutedEventArgs e)
        {
            General.Hardware.Remove(HardwareList.SelectedItem as Models.Hardware);
        }

        private void ClickAddHardwareApp(object sender, RoutedEventArgs e)
        {
            Models.Hardware hard = HardwareList.SelectedItem as Models.Hardware;
            Models.HardwareApp happ = new Models.HardwareApp();
            happ.AppObject = InHardwareApp.SelectedItem as Models.Application;
            happ.AppVersionObject = InHardwareVer.SelectedItem as Models.AppVersion;
            if (happ.AppObject == null || happ.AppVersionObject == null) return;
            hard.Apps.Add(happ);
        }


        private void ClickRemoveHardwareApp(object sender, RoutedEventArgs e)
        {
            Models.Hardware hard = HardwareList.SelectedItem as Models.Hardware;
            hard.Apps.Remove(HardwareAppList.SelectedItem as Models.HardwareApp);
        }

        #endregion

        private void ClickSave(object sender, RoutedEventArgs e)
        {
            string general = Newtonsoft.Json.JsonConvert.SerializeObject(General);

            if(filePath != "") {
                System.IO.File.WriteAllText(filePath, general);
                return;
            }

            SaveFileDialog diag = new SaveFileDialog();
            diag.FileName = General.ProjectName;
            diag.Title = "Projekt speichern";
            diag.Filter = "Kaenx Hersteller Projekt (*.ae-manu)|*.ae-manu";
            
            if(diag.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(diag.FileName, general);
                filePath = diag.FileName;
            }
        }

        private void ClickOpen(object sender, RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = "Projekt öffnen";
            diag.Filter = "Kaenx Hersteller Projekt (*.ae-manu)|*.ae-manu";
            if(diag.ShowDialog() == true)
            {
                string general = System.IO.File.ReadAllText(diag.FileName);
                General = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.ModelGeneral>(general);
                filePath = diag.FileName;

                foreach(Models.Hardware hard in General.Hardware)
                {
                    hard.DeviceObject = General.Devices.Single(d => d.Name == hard.GetDevice());

                    foreach(Models.HardwareApp happ in hard.Apps)
                    {
                        Models.Application mapp = General.Applications.Single(app => app.Name == happ.GetApp());
                        happ.AppObject = mapp;
                        Models.AppVersion mver = mapp.Versions.Single(ver => ver.Number == happ.GetVersion());
                        happ.AppVersionObject = mver;
                    }
                }


                SetSubCatalogItems(General.Catalog[0]);

                SetButtons(true);
            }
        }


        private void SetSubCatalogItems(Models.CatalogItem parent)
        {
            foreach(Models.CatalogItem item in parent.Items)
            {
                item.Parent = parent;

                if (!string.IsNullOrEmpty(item.GetHardwareName()))
                {
                    item.Hardware = General.Hardware.Single(h => h.Name == item.GetHardwareName());
                    if(item.GetHardwareApp() != -1)
                    {
                        item.HardApp = item.Hardware.Apps.Single(h => h.AppVersionObject.Number == item.GetHardwareApp());
                    }
                }

                SetSubCatalogItems(item);
            }
        }

        private void SetButtons(bool enable)
        {
            MenuSave.IsEnabled = enable;
            MenuClose.IsEnabled = enable;
            MenuPublish.IsEnabled = enable;
            MenuImport.IsEnabled = enable;
            TabsEdit.IsEnabled = enable;
        }

        private void ClickExportEts(object sender, RoutedEventArgs e)
        {
            ExportHelper helper = new ExportHelper();
            helper.ExportEts(General);
            helper.SignOutput();
        }

        private void ClickGenerateRefAuto(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            ver.ParameterRefs.Clear();

            foreach(Models.Parameter para in ver.Parameters)
            {
                Models.ParameterRef pref = new Models.ParameterRef();
                pref.Name = para.Name + " - 1";
                pref.ParameterId = para.Name;
                ver.ParameterRefs.Add(pref);
            }
        }

        private void HardwareAppChanged(object sender, SelectionChangedEventArgs e)
        {
            Models.Application app = InHardwareApp.SelectedItem as Models.Application;
            InHardwareVer.ItemsSource = app.Versions;
        }

        private void ClickExportSign(object sender, RoutedEventArgs e)
        {
            ExportHelper helper = new ExportHelper();
            helper.SignOutput();
        }

        private void ClickTest(object sender, RoutedEventArgs e)
        {
            General.Catalog.Add(new Models.CatalogItem() { Name = "Allgemeine Geräte" });
            General.Catalog[0].Items.Add(new Models.CatalogItem() { Name = "Sensoren", Parent = General.Catalog[0] });
        }

        private void ClickCatalogContext(object sender, RoutedEventArgs e)
        {
            Models.CatalogItem item = (sender as MenuItem).DataContext as Models.CatalogItem;
            item.Items.Add(new Models.CatalogItem() { Name = "Neue Kategorie", Parent = item });
        }

        private void LoadedCatalogContext(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            Models.CatalogItem item = menu.DataContext as Models.CatalogItem;
            
            (menu.Items[0] as MenuItem).IsEnabled = item.IsSection;
            (menu.Items[1] as MenuItem).IsEnabled = item.Parent != null;
        }

        private void ClickCatalogContextRemove(object sender, RoutedEventArgs e)
        {
            Models.CatalogItem item = (sender as MenuItem).DataContext as Models.CatalogItem;
            item.Parent.Items.Remove(item);
        }

#endregion


    }
}
