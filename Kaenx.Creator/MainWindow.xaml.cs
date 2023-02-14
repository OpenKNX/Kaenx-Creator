using Kaenx.Creator.Classes;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
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
using System.Xml.Linq;

namespace Kaenx.Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public static MainWindow Instance { get; set; }

        private Models.ModelGeneral _general;
        private string filePath = "";

        public Models.ModelGeneral General
        {
            get { return _general; }
            set { _general = value; Changed("General"); }
        }

        private Models.AppVersionModel _selectedVersion;
        public Models.AppVersionModel SelectedVersion
        {
            get { return _selectedVersion; }
            set { _selectedVersion = value; Changed("SelectedVersion"); }
        }

        private ObservableCollection<Models.MaskVersion> bcus;
        public ObservableCollection<Models.MaskVersion> BCUs
        {
            get { return bcus; }
            set { bcus = value; Changed("BCUs"); }
        }

        private static ObservableCollection<Models.DataPointType> dpts;
        public static ObservableCollection<Models.DataPointType> DPTs
        {
            get { return dpts; }
            set { dpts = value; }
        }

        public ObservableCollection<Models.ExportItem> Exports { get; set; } = new ObservableCollection<Models.ExportItem>();
        public ObservableCollection<Models.PublishAction> PublishActions { get; set; } = new ObservableCollection<Models.PublishAction>();

        public event PropertyChangedEventHandler PropertyChanged;

        private string etsPath {get;set;} = "";
        private List<Models.EtsVersion> EtsVersions = new List<Models.EtsVersion>() {
            new Models.EtsVersion(11, "ETS 4.0 (11)", "4.0.1997.50261"),
            new Models.EtsVersion(12, "ETS 5.0 (12)", "5.0.204.12971"),
            new Models.EtsVersion(13, "ETS 5.1 (13)", "5.1.84.17602"),
            new Models.EtsVersion(14, "ETS 5.6 (14)", "5.6.241.33672"),
            new Models.EtsVersion(20, "ETS 5.7 (20)", "5.7"),
            new Models.EtsVersion(21, "ETS 6.0 (21)", "6.0")
        };
        
        private int VersionCurrent = 5;


        public MainWindow()
        {
            Instance = this;
            string lang = Properties.Settings.Default.language;
            if(lang != "def")
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(lang);
            InitializeComponent();
            this.DataContext = this;
            LoadBcus();
            LoadDpts();
            CheckLangs();
            CheckEtsPath();
            CheckEtsVersions();
            LoadTemplates();

            
            MenuDebug.IsChecked = Properties.Settings.Default.isDebug;
            MenuUpdate.IsChecked = Properties.Settings.Default.autoUpdate;
            if(Properties.Settings.Default.autoUpdate) AutoCheckUpdate();

            if(!string.IsNullOrEmpty(App.FilePath))
                DoOpen(App.FilePath);
        }

        private async void AutoCheckUpdate()
        {
            System.Diagnostics.Debug.WriteLine("Checking Auto Update");
            (bool update, string vers) response = await CheckUpdate();
            if(response.update)
            {
                if(MessageBoxResult.Yes == MessageBox.Show($"Es ist eine neue version verfügbar: v{response.vers}\r\nJetzt zu den Github Releases gehen?", "Update suchen", MessageBoxButton.YesNo, MessageBoxImage.Question))
                {
                    Process.Start(new ProcessStartInfo("https://github.com/OpenKNX/Kaenx-Creator/releases/latest") { UseShellExecute = true });
                }
            } 
        }

        public void GoToItem(object item, object module)
        {
            if(module != null)
            {
                VersionTabs.SelectedIndex = 5;
                int index2 = item switch {
                    Models.Union => 4,
                    Models.Parameter => 7,
                    Models.ParameterRef => 8,
                    Models.ComObject => 9,
                    Models.ComObjectRef => 10,
                    Models.Dynamic.IDynItems => 14,
                    _ => -1
                };

                //TODO Get it back to work
                /*ModuleList.ScrollIntoView(module);
                ModuleList.SelectedItem = module;

                if(index2 == -1) return;
                ModuleTabs.SelectedIndex = index2;
                ((ModuleTabs.Items[index2] as TabItem).Content as ISelectable).ShowItem(item);
                */
                return;
            }


            int index = item switch{
                Models.ParameterType => 3,
                Models.Union => 4,
                Models.Parameter => 7,
                Models.ParameterRef => 8,
                Models.ComObject => 9,
                Models.ComObjectRef => 10,
                Models.Dynamic.IDynItems => 14,
                _ => -1
            };

            if(index == -1) return;
            VersionTabs.SelectedIndex = index;
            ((VersionTabs.Items[index] as TabItem).Content as ISelectable).ShowItem(item);
        }

        private void CheckEtsPath() {
            if(Directory.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CV"))) {
                if(CheckEtsPath(AppDomain.CurrentDomain.BaseDirectory)) {
                    etsPath = AppDomain.CurrentDomain.BaseDirectory; 
                    return;
                }
            }
            if(CheckEtsPath(@"C:\Program Files (x86)\ETS6")) { 
                etsPath = @"C:\Program Files (x86)\ETS6"; 
                return;
            }
            if(CheckEtsPath(@"C:\Program Files (x86)\ETS5")) { 
                etsPath = @"C:\Program Files (x86)\ETS5"; 
                return;
            }
            if(CheckEtsPath(@"C:\Program Files (x86)\ETS4")) { 
                etsPath = @"C:\Program Files (x86)\ETS4"; 
                return;
            }
        }

        private bool CheckEtsPath(string path)
        {
            if(!Directory.Exists(path)) return false;

            if(!File.Exists(System.IO.Path.Combine(path, "Knx.Ets.XmlSigning.dll"))) return true;
            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(System.IO.Path.Combine(path, "Knx.Ets.XmlSigning.dll"));
            if(versionInfo.FileVersion.StartsWith("6.") || Directory.Exists(System.IO.Path.Combine(path, "CV", versionInfo.FileVersion)))
                return true;

            string newVersion = versionInfo.FileVersion;
            if (versionInfo.FileVersion.Split('.').Length > 2) newVersion = string.Join('.', newVersion.Split('.').Take(2));
            try {
                Directory.CreateDirectory(System.IO.Path.Combine(path, "CV", newVersion));
            } catch{
                MessageBox.Show("Es wurde eine ETS installation erkannt. Um auch die aktuell Installierte Version bauen zu können, führen Sie die Anwendung einmalig als Admin aus.", "ETS Versionen", MessageBoxButton.OK, MessageBoxImage.Warning);
                return true;
            }
            File.Copy(System.IO.Path.Combine(path, "Knx.Ets.Xml.ObjectModel.dll"), System.IO.Path.Combine(path, "CV", newVersion, "Knx.Ets.Xml.ObjectModel.dll"));
            File.Copy(System.IO.Path.Combine(path, "Knx.Ets.Xml.ObjectModel.XmlSerializers.dll"), System.IO.Path.Combine(path, "CV", newVersion, "Knx.Ets.Xml.ObjectModel.XmlSerializers.dll"));
            File.Copy(System.IO.Path.Combine(path, "Knx.Ets.Xml.RegistrationRelevanceInformation.dll"), System.IO.Path.Combine(path, "CV", newVersion, "Knx.Ets.Xml.RegistrationRelevanceInformation.dll"));
            File.Copy(System.IO.Path.Combine(path, "Knx.Ets.XmlSigning.dll"), System.IO.Path.Combine(path, "CV", newVersion, "Knx.Ets.XmlSigning.dll"));
            File.Copy(System.IO.Path.Combine(path, "log4net.dll"), System.IO.Path.Combine(path, "CV", newVersion, "log4net.dll"));
            MessageBox.Show("Es wurde eine neue ETS installation erkannt und hinzugefügt.", "ETS Versionen", MessageBoxButton.OK, MessageBoxImage.Information);
            return true;
        }

        private void CheckEtsVersions() {
            bool flag = false;
            if(Directory.Exists(System.IO.Path.Combine(etsPath, "CV", "6.0"))) {
                flag = true;
                foreach(Models.EtsVersion v in EtsVersions)
                    v.IsEnabled = true;
            } else {
                if(File.Exists(System.IO.Path.Combine(etsPath, "Knx.Ets.XmlSigning.dll"))) {
                    FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(System.IO.Path.Combine(etsPath, "Knx.Ets.XmlSigning.dll"));
                    if(versionInfo.FileVersion.ToString().StartsWith("6.0")) {
                        flag = true;
                        foreach(Models.EtsVersion v in EtsVersions)
                            v.IsEnabled = true;
                    }
                }
            }

            if(!flag) {
                if(string.IsNullOrEmpty(etsPath)) {
                    foreach(Models.EtsVersion v in EtsVersions)
                        v.IsEnabled = false;
                } else {
                    foreach(Models.EtsVersion v in EtsVersions)
                        v.IsEnabled = Directory.Exists(System.IO.Path.Combine(etsPath, "CV", v.FolderPath));
                }
            }

            NamespaceSelection.ItemsSource = EtsVersions;
        }

        private void LoadTemplates() {
            foreach(string path in Directory.GetFiles(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Templates")))
            {
                string name = path.Substring(path.LastIndexOf('\\')+1);
                name = name.Substring(0, name.IndexOf('.'));
                MenuItem item = new MenuItem() { Header = name};
                item.Tag = path;
                item.Click += ClickOpenTemplate;
                MenuLoad.Items.Add(item);
            }
        }

        private void CheckLangs()
        {
            string lang = Properties.Settings.Default.language;
            foreach(UIElement ele in MenuLang.Items)
            {
                if(ele is MenuItem item)
                {
                    item.IsChecked = item.Tag?.ToString() == lang;
                }
            }
        }

        private void ClickNew(object sender, RoutedEventArgs e)
        {
            General = new Models.ModelGeneral() { ImportVersion = VersionCurrent, Guid = Guid.NewGuid().ToString() };
            General.Languages.Add(new Models.Language("Deutsch", "de-DE"));
            General.Catalog.Add(new Models.CatalogItem() { Name = "Hauptkategorie (wird nicht exportiert)" });
            SetButtons(true);
            MenuSaveBtn.IsEnabled = false;
            SelectedVersion = null;
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
                    mask.MediumTypes = xmask.Attribute("MediumTypeRefId").Value;
                    if(xmask.Attribute("OtherMediumTypeRefId") != null) mask.MediumTypes += " " + xmask.Attribute("OtherMediumTypeRefId").Value;

                    string eleStr = xmask.ToString();
                    if (eleStr.Contains("<Procedure ProcedureType=\"Load\""))
                    {
                        XElement prodLoad = xmask.Descendants(XName.Get("Procedure")).First(p => p.Attribute("ProcedureType")?.Value == "Load");
                        if (prodLoad.ToString().Contains("<LdCtrlMerge"))
                            mask.Procedure = Models.ProcedureTypes.Merged;
                        else
                            mask.Procedure = Models.ProcedureTypes.Default;
                    } else
                    {
                        mask.Procedure = Models.ProcedureTypes.Product;
                    }


                    if(mask.Procedure != Models.ProcedureTypes.Product)
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
                            mask.Memory = Models.MemoryTypes.Absolute;
                        }
                        else
                        {
                            continue;
                        }
                    }


                    if(xmask.Descendants(XName.Get("Procedures")).Count() > 0) {
                        foreach(XElement xproc in xmask.Element(XName.Get("HawkConfigurationData")).Element(XName.Get("Procedures")).Elements()) {
                            Models.Procedure proc = new Models.Procedure();
                            proc.Type = xproc.Attribute("ProcedureType").Value;
                            proc.SubType = xproc.Attribute("ProcedureSubType").Value;

                            StringBuilder sb = new StringBuilder();

                            foreach (XNode node in xproc.Nodes())
                                sb.Append(node.ToString() + "\r\n");

                            proc.Controls = sb.ToString();
                            mask.Procedures.Add(proc);
                        }
                    }

                    BCUs.Add(mask);
                }

                System.IO.File.WriteAllText(jsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(BCUs));
            }
        }

        private void LoadDpts()
        {
            string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "datapoints.json");
            string xmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "datapoints.xml");
            if (System.IO.File.Exists(jsonPath))
            {
                DPTs = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<Models.DataPointType>>(System.IO.File.ReadAllText(jsonPath));
            } else
            {
                DPTs = new ObservableCollection<Models.DataPointType>();
                XDocument xdoc = XDocument.Load(xmlPath);
                IEnumerable<XElement> xdpts = xdoc.Descendants(XName.Get("DatapointType"));
                
                DPTs.Add(new Models.DataPointType() {
                    Name = "Leeren Datentyp angeben (Nur bei ComRefs)",
                    Number = "0",
                    Size = 0
                });

                foreach(XElement xdpt in xdpts)
                {
                    Models.DataPointType dpt = new Models.DataPointType();
                    dpt.Name = xdpt.Attribute("Name").Value + " " + xdpt.Attribute("Text").Value;
                    dpt.Number = xdpt.Attribute("Number").Value;
                    dpt.Size = int.Parse(xdpt.Attribute("SizeInBit").Value);

                    IEnumerable<XElement> xsubs = xdpt.Descendants(XName.Get("DatapointSubtype"));

                    foreach(XElement xsub in xsubs)
                    {
                        Models.DataPointSubType dpst = new Models.DataPointSubType();
                        dpst.Name = dpt.Number + "." + Fill(xsub.Attribute("Number").Value, 3, "0") + " " + xsub.Attribute("Text").Value;
                        dpst.Number = xsub.Attribute("Number").Value;
                        dpst.ParentNumber = dpt.Number;
                        dpt.SubTypes.Add(dpst);
                    }

                    DPTs.Add(dpt);
                }


                System.IO.File.WriteAllText(jsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(DPTs));
            }
        }

        private string Fill(string input, int length, string fill)
        {
            for(int i = input.Length; i < length; i++)
            {
                input = fill + input;
            }
            return input;
        }

        #region Clicks

        #region Clicks Add/Remove

        private void ClickAddHardDevice(object sender, RoutedEventArgs e)
        {
            Models.Hardware hard = (sender as Button).DataContext as Models.Hardware;
            hard.Devices.Add(new Models.Device());
        }

        private void ClickAddHardApp(object sender, RoutedEventArgs e)
        {
            if(InHardApp.SelectedItem == null)
            {
                MessageBox.Show("Bitte wählen Sie erst eine Apllikation aus.", "Fehler beim Hinzufügen", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Models.Hardware hard = (sender as Button).DataContext as Models.Hardware;
            if(!hard.Apps.Contains(InHardApp.SelectedItem as Models.Application)) {
                hard.Apps.Add(InHardApp.SelectedItem as Models.Application);
            }
            InHardApp.SelectedItem = null;
        }

        private void ClickAddVersion(object sender, RoutedEventArgs e)
        {
            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion newVer = new Models.AppVersion() { Name = app.Name };
            Models.Language lang = new Models.Language("Deutsch", "de-DE");
            newVer.Languages.Add(lang);
            newVer.DefaultLanguage = lang.CultureCode;
            newVer.Text.Add(new Models.Translation(lang, "Dummy"));
            newVer.Dynamics.Add(new Models.Dynamic.DynamicMain());

            if(app.Mask.Procedure == Models.ProcedureTypes.Product)
            {
                newVer.Procedure = "<LoadProcedures>\r\n<LoadProcedure>\r\n<LdCtrlConnect />\r\n<LdCtrlCompareProp ObjIdx=\"0\" PropId=\"78\" InlineData=\"00000000012700000000\" />\r\n<LdCtrlUnload LsmIdx=\"1\" />\r\n<LdCtrlUnload LsmIdx=\"2\" />\r\n<LdCtrlUnload LsmIdx=\"3\" />\r\n<LdCtrlLoad LsmIdx=\"1\" />\r\n<LdCtrlAbsSegment LsmIdx=\"1\" SegType=\"0\" Address=\"16384\" Size=\"513\" Access=\"255\" MemType=\"3\" SegFlags=\"128\" />\r\n<LdCtrlTaskSegment LsmIdx=\"1\" Address=\"16384\" />\r\n<LdCtrlLoadCompleted LsmIdx=\"1\" />\r\n<LdCtrlLoad LsmIdx=\"2\" />\r\n<LdCtrlAbsSegment LsmIdx=\"2\" SegType=\"0\" Address=\"16897\" Size=\"511\" Access=\"255\" MemType=\"3\" SegFlags=\"128\" />\r\n<LdCtrlTaskSegment LsmIdx=\"2\" Address=\"16897\" />\r\n<LdCtrlLoadCompleted LsmIdx=\"2\" />\r\n<LdCtrlLoad LsmIdx=\"3\" />\r\n<LdCtrlAbsSegment LsmIdx=\"3\" SegType=\"0\" Address=\"1792\" Size=\"152\" Access=\"0\" MemType=\"2\" SegFlags=\"0\" />\r\n<LdCtrlAbsSegment LsmIdx=\"3\" SegType=\"1\" Address=\"1944\" Size=\"1\" Access=\"0\" MemType=\"2\" SegFlags=\"0\" />\r\n<LdCtrlAbsSegment LsmIdx=\"3\" SegType=\"0\" Address=\"17408\" Size=\"394\" Access=\"255\" MemType=\"3\" SegFlags=\"128\" />\r\n<LdCtrlTaskSegment LsmIdx=\"3\" Address=\"17408\" />\r\n<LdCtrlLoadCompleted LsmIdx=\"3\" />\r\n<LdCtrlRestart />\r\n<LdCtrlDisconnect />\r\n</LoadProcedure>\r\n</LoadProcedures>";
            } else if(app.Mask.Procedure == Models.ProcedureTypes.Merged)
            {
                newVer.Procedure = "<LoadProcedures>\r\n<LoadProcedure MergeId=\"2\">\r\n<LdCtrlRelSegment  AppliesTo=\"full\" LsmIdx=\"4\" Size=\"1\" Mode=\"0\" Fill=\"0\" />\r\n</LoadProcedure>\r\n<LoadProcedure MergeId=\"4\">\r\n<LdCtrlWriteRelMem ObjIdx=\"4\" Offset=\"0\" Size=\"1\" Verify=\"true\" />\r\n</LoadProcedure>\r\n</LoadProcedures>";
            }

            if(app.Versions.Count > 0) {
                Models.AppVersionModel ver = app.Versions.OrderByDescending(v => v.Number).ElementAt(0);
                newVer.Number = ver.Number + 1;
            }

            Models.AppVersionModel model = new Models.AppVersionModel() {
                Name = newVer.Name,
                Number = newVer.Number,
                Namespace = newVer.NamespaceVersion,
                Version = Newtonsoft.Json.JsonConvert.SerializeObject(newVer, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects })
            };
            
            app.Versions.Add(model);
        }

        private void ClickAddMemory(object sender, RoutedEventArgs e)
        {
            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.Memories.Add(new Models.Memory() { Type = app.Mask.Memory, UId = AutoHelper.GetNextFreeUId(version.Memories) });
        }

        private void ClickRemoveMemory(object sender, RoutedEventArgs e)
        {
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.Memories.Remove(ListMemories.SelectedItem as Models.Memory);
        }

        private void ClickRemoveVersion(object sender, RoutedEventArgs e)
        {
            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersionModel ver = (sender as MenuItem).DataContext as Models.AppVersionModel;

            app.Versions.Remove(ver);
        }

        private void ClickCopyVersion(object sender, RoutedEventArgs e)
        {
            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion ver = (sender as MenuItem).DataContext as Models.AppVersion;

            Models.AppVersion copy = ver.Copy();
            copy.Number += 1;
            copy.Name += " Kopie";
        }

        private void ClickOpenHere(object sender, RoutedEventArgs e)
        {
            long before, after;
            if(SelectedVersion != null)
            {
                SelectedVersion.PropertyChanged -= SelectedVersion_PropertyChanged;
                SelectedVersion.Name = SelectedVersion.Model.Name;
                SelectedVersion.Number = SelectedVersion.Model.Number;
                SelectedVersion.Version = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedVersion.Model, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
                before = System.GC.GetTotalMemory(false);
                System.GC.Collect();
                after = System.GC.GetTotalMemory(false);
                System.Diagnostics.Debug.WriteLine("Freigemacht: " + (before - after).ToString());
            }
            before = System.GC.GetTotalMemory(false);
            SelectedVersion = (sender as MenuItem).DataContext as Models.AppVersionModel;
            SelectedVersion.Model = AutoHelper.GetAppVersion(General, SelectedVersion);
            SelectedVersion.Model.PropertyChanged += SelectedVersion_PropertyChanged;
            //TODO auto open TabView Item
            after = System.GC.GetTotalMemory(false);
            System.Diagnostics.Debug.WriteLine("Neu verbraucht: " + (after - before).ToString());
            TabsEdit.SelectedIndex = 6;
        }

        private void SelectedVersion_PropertyChanged(object sender, PropertyChangedEventArgs e = null)
        {
            if(e.PropertyName != "NameText" && e.PropertyName != "NamespaceVersion") return;
            SelectedVersion.Name = SelectedVersion.Model.Name;
            SelectedVersion.Number = SelectedVersion.Model.Number;
            SelectedVersion.Namespace = SelectedVersion.Model.NamespaceVersion;
        }

        private void ClickOpenViewer(object sender, RoutedEventArgs e)
        {
            if(MessageBoxResult.Cancel == MessageBox.Show("Achtung, um den Viewer verwenden zu können, werden die IDs überprüft und ggf. neue vergeben.\r\n\r\nTrotzdem weiter machen?", "ProdViewer öffnen", MessageBoxButton.OKCancel, MessageBoxImage.Question)) return;
            Models.Application app = (Models.Application)AppList.SelectedItem;
            Models.AppVersionModel model = (sender as MenuItem).DataContext as Models.AppVersionModel;
            Models.AppVersion ver;
            if(model == SelectedVersion)
            {
                ver = SelectedVersion.Model;
            } else {
                ver = AutoHelper.GetAppVersion(General, model);
            }
            AutoHelper.CheckIds(ver);

            ObservableCollection<Models.PublishAction> actions = new ObservableCollection<Models.PublishAction>();
            CheckHelper.CheckVersion(General, app, ver, null, actions);
            if(actions.Any(a => a.State == Models.PublishState.Fail))
            {
                MessageBox.Show("Die Applikation enthält Fehler. Bitte korriegieren Sie diese und probieren es danach erneut.", "ProdViewer öffnen", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ViewerWindow viewer = new ViewerWindow(new Viewer.ImporterCreator(ver, app));
            viewer.Show();
        }

        private void ClickAddApp(object sender, RoutedEventArgs e)
        {
            General.Applications.Add(new Models.Application());
        }

        private void ClickRemoveApp(object sender, RoutedEventArgs e)
        {
            if(AppList.SelectedItem == null) return;

            Models.Application app = AppList.SelectedItem as Models.Application;
            General.Applications.Remove(app);

            foreach(Models.Hardware h in General.Hardware)
            {
                if(h.Apps.Contains(app))
                    h.Apps.Remove(app);
            }
        }

        private void ClickAddLanguageVers(object sender, RoutedEventArgs e)
        {
            if(LanguagesListVers.SelectedItem == null){
                MessageBox.Show("Bitte wählen Sie erst eine Sprache aus.");
                return;
            }
            Models.Language lang = LanguagesListVers.SelectedItem as Models.Language;
            LanguagesListVers.SelectedItem = null;
            
            if(SelectedVersion.Model.Languages.Any(l => l.CultureCode == lang.CultureCode))
                MessageBox.Show("Die Sprache wird bereits unterstützt.");
            else {
                SelectedVersion.Model.Languages.Add(lang);
                if(!SelectedVersion.Model.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    SelectedVersion.Model.Text.Add(new Models.Translation(lang, ""));
                
                foreach(Models.ParameterType type in SelectedVersion.Model.ParameterTypes) {
                    if(type.Type != Models.ParameterTypes.Enum) continue;

                    foreach(Models.ParameterTypeEnum enu in type.Enums)
                        if(!enu.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                            enu.Text.Add(new Models.Translation(lang, ""));
                }
                foreach(Models.Message msg in SelectedVersion.Model.Messages) {
                    if(!msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        msg.Text.Add(new Models.Translation(lang, ""));
                }
                foreach(Models.Helptext msg in SelectedVersion.Model.Helptexts){
                    if(!msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        msg.Text.Add(new Models.Translation(lang, ""));
                }

                addLangToVersion(SelectedVersion.Model, lang);
                addLangToVersion(SelectedVersion.Model.Dynamics[0], lang);
                foreach(Models.Module mod in SelectedVersion.Model.Modules)
                {
                    addLangToVersion(mod, lang);
                    addLangToVersion(mod.Dynamics[0], lang);
                }
            }
        }

        private void ClickRemoveLanguageVers(object sender, RoutedEventArgs e) {
            if(SupportedLanguagesVers.SelectedItem == null){
                MessageBox.Show("Bitte wählen Sie erst oben eine Sprache aus.");
                return;
            }
            Models.Language lang = SupportedLanguagesVers.SelectedItem as Models.Language;

            if(SelectedVersion.Model.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                SelectedVersion.Model.Text.Remove(SelectedVersion.Model.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            SelectedVersion.Model.Languages.Remove(SelectedVersion.Model.Languages.Single(l => l.CultureCode == lang.CultureCode));
            

            foreach(Models.ParameterType type in SelectedVersion.Model.ParameterTypes) {
                if(type.Type != Models.ParameterTypes.Enum) continue;

                foreach(Models.ParameterTypeEnum enu in type.Enums) {
                    if(enu.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        enu.Text.Remove(enu.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                }
            }
            foreach(Models.Message msg in SelectedVersion.Model.Messages) {
                if(msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    msg.Text.Remove(msg.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            }
            foreach(Models.Helptext msg in SelectedVersion.Model.Helptexts){
                if(msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    msg.Text.Remove(msg.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            }

            removeLangFromVersion(SelectedVersion.Model, lang);
            foreach(Models.Module mod in SelectedVersion.Model.Modules)
                removeLangFromVersion(mod, lang);
        }

        private void addLangToVersion(Models.IVersionBase vbase, Models.Language lang)
        {
            foreach(Models.Parameter para in vbase.Parameters)
            {
                if(!para.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Text.Add(new Models.Translation(lang, ""));
                if(!para.Suffix.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Suffix.Add(new Models.Translation(lang, ""));
            }
            foreach(Models.ParameterRef para in vbase.ParameterRefs)
            {
                if(!para.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Text.Add(new Models.Translation(lang, ""));
                if(!para.Suffix.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Suffix.Add(new Models.Translation(lang, ""));
            }
            foreach(Models.ComObject com in vbase.ComObjects) {
                if(!com.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.Text.Add(new Models.Translation(lang, ""));
                if(!com.FunctionText.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.FunctionText.Add(new Models.Translation(lang, ""));
            }
            foreach(Models.ComObjectRef com in vbase.ComObjectRefs) {
                if(!com.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.Text.Add(new Models.Translation(lang, ""));
                if(!com.FunctionText.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.FunctionText.Add(new Models.Translation(lang, ""));
            }
        }

        private void addLangToVersion(Models.Dynamic.IDynItems parent, Models.Language lang)
        {
            switch(parent)
            {
                case Models.Dynamic.DynChannel dch:
                    if(!dch.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        dch.Text.Add(new Models.Translation(lang, ""));
                    break;
                    
                case Models.Dynamic.DynParaBlock dpb:
                    if(!dpb.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        dpb.Text.Add(new Models.Translation(lang, ""));
                    break;

                case Models.Dynamic.DynSeparator ds:
                    if(!ds.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        ds.Text.Add(new Models.Translation(lang, ""));
                    break;
                    
                case Models.Dynamic.DynButton db:
                    if(!db.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        db.Text.Add(new Models.Translation(lang, ""));
                    break;
            }

            if(parent.Items?.Count > 0)
                foreach(Models.Dynamic.IDynItems item in parent.Items)
                    addLangToVersion(item, lang);
        }

        private void removeLangFromVersion(Models.IVersionBase vbase, Models.Language lang)
        {
            foreach(Models.Parameter para in vbase.Parameters) {
                if(para.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Text.Remove(para.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                if(para.Suffix.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Suffix.Remove(para.Suffix.Single(l => l.Language.CultureCode == lang.CultureCode));
            } 
            foreach(Models.ParameterRef para in vbase.ParameterRefs) {
                if(para.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Text.Remove(para.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                if(para.Suffix.Any(t => t.Language.CultureCode == lang.CultureCode))
                    para.Suffix.Remove(para.Suffix.Single(l => l.Language.CultureCode == lang.CultureCode));
            } 
            foreach(Models.ComObject com in vbase.ComObjects) {
                if(com.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.Text.Remove(com.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                if(com.FunctionText.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.FunctionText.Remove(com.FunctionText.Single(l => l.Language.CultureCode == lang.CultureCode));
            }
            foreach(Models.ComObjectRef com in vbase.ComObjectRefs) {
                if(com.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.Text.Remove(com.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                if(com.FunctionText.Any(t => t.Language.CultureCode == lang.CultureCode))
                    com.FunctionText.Remove(com.FunctionText.Single(l => l.Language.CultureCode == lang.CultureCode));
            }
        }

        private void removeLangToVersion(Models.Dynamic.IDynItems parent, Models.Language lang)
        {
            switch(parent)
            {
                case Models.Dynamic.DynChannel dch:
                    if(dch.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        dch.Text.Remove(dch.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                    break;
                    
                case Models.Dynamic.DynParaBlock dpb:
                    if(dpb.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        dpb.Text.Remove(dpb.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                    break;

                case Models.Dynamic.DynSeparator ds:
                    if(ds.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        ds.Text.Remove(ds.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                    break;

                case Models.Dynamic.DynButton db:
                    if(db.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        db.Text.Remove(db.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                    break;
            }

            if(parent.Items?.Count > 0)
                foreach(Models.Dynamic.IDynItems item in parent.Items)
                    addLangToVersion(item, lang);
        }


        private void ClickAddLanguageGen(object sender, RoutedEventArgs e)
        {
            if(LanguagesListGen.SelectedItem == null){
                MessageBox.Show("Bitte wählen Sie erst eine Sprache aus.");
                return;
            }
            Models.Language lang = LanguagesListGen.SelectedItem as Models.Language;
            LanguagesListGen.SelectedItem = null;
            
            if(_general.Languages.Any(l => l.CultureCode == lang.CultureCode))
                MessageBox.Show("Die Sprache wird bereits unterstützt.");
            else {
                _general.Languages.Add(lang);
                LanguageCatalogItemAdd(_general.Catalog[0], lang);
                foreach(Models.Hardware hard in _general.Hardware) {
                    foreach(Models.Device dev in hard.Devices) {
                        if(!dev.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                            dev.Text.Add(new Models.Translation(lang, ""));
                        if(!dev.Description.Any(t => t.Language.CultureCode == lang.CultureCode))
                            dev.Description.Add(new Models.Translation(lang, ""));
                    }
                }
            }
        }

        private void LanguageCatalogItemAdd(Models.CatalogItem parent, Models.Language lang)
        {
            foreach(Models.CatalogItem item in parent.Items) {
                if(!item.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    item.Text.Add(new Models.Translation(lang, ""));

                LanguageCatalogItemAdd(item, lang);
            }
        }

        private void LanguageCatalogItemRemove(Models.CatalogItem parent, Models.Language lang)
        {
            foreach(Models.CatalogItem item in parent.Items) {
                if(item.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    item.Text.Remove(item.Text.Single(l => l.Language.CultureCode == lang.CultureCode));

                LanguageCatalogItemRemove(item, lang);
            }
        }

        private void ClickRemoveLanguageGen(object sender, RoutedEventArgs e) {
            if(SupportedLanguagesGen.SelectedItem == null){
                MessageBox.Show("Bitte wählen Sie erst links eine Sprache aus.");
                return;
            }
            Models.Language lang = SupportedLanguagesGen.SelectedItem as Models.Language;


            _general.Languages.Remove(_general.Languages.Single(l => l.CultureCode == lang.CultureCode));
            LanguageCatalogItemRemove(_general.Catalog[0], lang);
            foreach(Models.Hardware hard in _general.Hardware) {
                foreach(Models.Device dev in hard.Devices) {
                    if(dev.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        dev.Text.Remove(dev.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                    if(dev.Description.Any(t => t.Language.CultureCode == lang.CultureCode))
                        dev.Description.Remove(dev.Description.Single(l => l.Language.CultureCode == lang.CultureCode));
                } 
            }
        }

        private void ClickAddHardware(object sender, RoutedEventArgs e)
        {
            General.Hardware.Add(new Models.Hardware());
        }

        private void ClickRemoveHardware(object sender, RoutedEventArgs e)
        {
            General.Hardware.Remove(HardwareList.SelectedItem as Models.Hardware);
        }

        private void OnAddingNewDevice(object sender, AddingNewItemEventArgs e)
        {
            Models.Hardware hard = (sender as DataGrid).DataContext as Models.Hardware;
            Models.Device device = new Models.Device();
            foreach(Models.Language lang in _general.Languages) {
                device.Text.Add(new Models.Translation(lang, ""));
                device.Description.Add(new Models.Translation(lang, ""));
            }
            e.NewItem = device;
        }

        private void ClickRemoveDeviceApp(object sender, RoutedEventArgs e)
        {
            (HardwareList.SelectedItem as Models.Hardware).Apps.Remove(DeviceAppList.SelectedItem as Models.Application);
        }

        #endregion

        private void ClickSave(object sender, RoutedEventArgs e)
        {
            if(SelectedVersion != null)
                SelectedVersion.Version = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedVersion.Model, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            
            string general = Newtonsoft.Json.JsonConvert.SerializeObject(General, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            System.IO.File.WriteAllText(filePath, general);
        }

        private void ClickClose(object sender, RoutedEventArgs e)
        {
            General = null;
            SetButtons(false);
            MenuSaveBtn.IsEnabled = false;
            SelectedVersion = null;
            System.GC.Collect();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            // for .NET Core you need to add UseShellExecute = true
            // see https://docs.microsoft.com/dotnet/api/system.diagnostics.processstartinfo.useshellexecute#property-value
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        private void ClickSaveAs(object sender, RoutedEventArgs e)
        {
            if(SelectedVersion != null)
                SelectedVersion.Version = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedVersion.Model, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            string general = Newtonsoft.Json.JsonConvert.SerializeObject(General, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });

            SaveFileDialog diag = new SaveFileDialog();
            diag.FileName = General.ProjectName;
            diag.Title = "Projekt speichern";
            diag.Filter = "Kaenx Hersteller Projekt (*.ae-manu)|*.ae-manu";
            
            if(diag.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(diag.FileName, general);
                filePath = diag.FileName;
                MenuSaveBtn.IsEnabled = true;
            }
        }

        private void ClickSaveTemplate(object sender, RoutedEventArgs e)
        {
            while(true) {
                Controls.PromptDialog diag = new Controls.PromptDialog("Name des Templates:", "Template speichern");
                if(diag.ShowDialog() == false) {
                    return;
                }

                if(string.IsNullOrEmpty(diag.Answer))
                {
                    System.Windows.MessageBox.Show($"Bitte geben Sie einen Namen für das Template ein.", "Template speichern", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Error);
                    continue;
                }

                if(System.IO.File.Exists("Templates\\" + diag.Answer + ".temp"))
                {
                    var res = System.Windows.MessageBox.Show($"Es existiert bereits ein Template mit dem Namen '{diag.Answer}'\r\nWollen Sie es überschreiben?", "Template überschreiben?", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
                    if(res == System.Windows.MessageBoxResult.No)
                        continue;
                }

                if(SelectedVersion?.Model != null)
                    SelectedVersion.Version = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedVersion.Model, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
                
                string general = Newtonsoft.Json.JsonConvert.SerializeObject(General, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
                System.IO.File.WriteAllText("Templates\\" + diag.Answer + ".temp", general);
                return;
            }
        }


        private void ClickOpenTemplate(object sender, RoutedEventArgs e)
        {
            MenuItem item = sender as MenuItem;
            DoOpen(item.Tag.ToString());
        }

        private void ClickOpen(object sender, RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = "Projekt öffnen";
            diag.Filter = "Kaenx Hersteller Projekt (*.ae-manu)|*.ae-manu";
            if(diag.ShowDialog() == true)
            {
                DoOpen(diag.FileName);
                MenuSaveBtn.IsEnabled = true;
            }
        }


        private void DoOpen(string path)
        {
            if(!File.Exists(path)) return;
            
            string general = System.IO.File.ReadAllText(path);

            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("\"ImportVersion\":[ ]?([0-9]+)");
            System.Text.RegularExpressions.Match match = reg.Match(general);

            int VersionToOpen = 0;
            if(match.Success)
            {
                VersionToOpen = int.Parse(match.Groups[1].Value);
            }

            if(VersionToOpen < VersionCurrent && MessageBox.Show("Diese Datei wurde mit einer älteren Version erstellt. Soll versucht werden die Datei zu konvertieren?", "Altes Dateiformat", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                general = CheckHelper.CheckImportVersion(general, VersionToOpen);
            }
                
            try{
                General = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.ModelGeneral>(general, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            } catch {
                MessageBox.Show("Die Datei scheint nicht mit der aktuellen Version zusammen zu passen.\r\nEvtl. muss diese vorher konvertiert werden.");
                return;
            }
            SelectedVersion = null;
            General.ImportVersion = VersionCurrent;
            filePath = path;

            foreach(Models.Application app in General.Applications)
            {
                string mid = app._maskId;
                if (string.IsNullOrEmpty(mid)) continue;

                Models.MaskVersion mask = BCUs.Single(bcu => bcu.Id == mid);
                app.Mask = mask;
            }


            foreach(Models.Hardware hard in General.Hardware){
                if(string.IsNullOrEmpty(hard._appsString)) continue;
                
                foreach(string name in hard._appsString.Split(',')){
                    try{
                        hard.Apps.Add(General.Applications.Single(app => app.Name == name));
                    } catch{}
                }
            }

            SetSubCatalogItems(General.Catalog[0]);

            SetButtons(true);
            MenuSave.IsEnabled = true;
        }

        


        private void SetSubCatalogItems(Models.CatalogItem parent)
        {
            foreach(Models.CatalogItem item in parent.Items)
            {
                item.Parent = parent;

                if (!string.IsNullOrEmpty(item._hardwareName))
                {
                    item.Hardware = General.Hardware.First(h => h.Name == item._hardwareName);
                }

                SetSubCatalogItems(item);
            }
        }

        private void SetButtons(bool enable)
        {
            MenuSave.IsEnabled = enable;
            MenuClose.IsEnabled = enable;
            MenuImport.IsEnabled = enable;
            TabsEdit.IsEnabled = enable;
            
            if(General != null)
            {
                TabsEdit.Visibility = Visibility.Visible;
                LogoGrid.Visibility = Visibility.Collapsed;
                if(TabsEdit.SelectedIndex == 6)
                    TabsEdit.SelectedIndex = 5;
            } else {
                TabsEdit.SelectedIndex = 0;
                TabsEdit.Visibility = Visibility.Collapsed;
                LogoGrid.Visibility = Visibility.Visible;
            }
        }

        private void ClickShowVersion(object sender, RoutedEventArgs e)
        {
            MessageBox.Show($"Sie verwenden aktuell die Version: {string.Join('.', System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString().Split('.').Take(3))}", "Kaenx-Creator Version", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ClickDoResetParaIds(object sender, RoutedEventArgs e)
        {
            ClearHelper.ResetParameterIds(SelectedVersion.Model);
        }


        private void ClickImport(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> filters = new Dictionary<string, string>() {
                {"knxprod", "KNX Produktdatenbank (*.knxprod)|*.knxprod"},
                {"xml", "XML Produktatenbank (*.xml)|*.xml"},
                {"ae-prod", "Kaenx Produktatenbank (*.ae-prod)|*.ae-prod"},
            };

            string prod = (sender as MenuItem).Tag.ToString();
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = filters[prod];
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                ImportHelper helper = new ImportHelper(dialog.FileName, bcus);
                switch(prod)
                {
                    case "knxprod":
                        helper.StartZip(_general, DPTs);
                        break;

                    case "xml":
                        helper.StartXml(_general, DPTs);
                        break;

                    default:
                        throw new Exception("Unbekannter Dateityp: " + prod);
                }
            }
            System.GC.Collect();
        }

        private void ClickCatalogContext(object sender, RoutedEventArgs e)
        {
            Models.CatalogItem parent = (sender as MenuItem).DataContext as Models.CatalogItem;
            Models.CatalogItem item = new Models.CatalogItem() { Name = "Neue Kategorie", Parent = parent };
            foreach(Models.Language lang in _general.Languages) {
                item.Text.Add(new Models.Translation(lang, ""));
            }
            parent.Items.Add(item);
        }

        private void ClickCatalogContextRemove(object sender, RoutedEventArgs e)
        {
            Models.CatalogItem item = (sender as MenuItem).DataContext as Models.CatalogItem;
            item.Parent.Items.Remove(item);
        }




        #endregion

        private void ClickCalcHeatmap(object sender, RoutedEventArgs e)
        {
            Models.Memory mem = (sender as Button).DataContext as Models.Memory;
            //Models.Application app = _general.Applications.Single(a => a.Versions.Any(v => v.Name == SelectedVersion.Model.Name && v.Number == SelectedVersion.Model.Number));
            Models.Application app = _general.Applications.Single(a => a.Versions.Contains(SelectedVersion));
            CheckHelper.CheckVersion(null, app, SelectedVersion.Model, null, new ObservableCollection<Models.PublishAction>());
            AutoHelper.MemoryCalculation(SelectedVersion.Model, mem);
        }

        private void TabChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.RemovedItems.Count > 0 && (e.RemovedItems[0] as TabItem) != null && (e.RemovedItems[0] as TabItem).Content is IFilterable mx1)
                mx1.FilterHide();

            if(e.AddedItems.Count > 0 && (e.AddedItems[0] as TabItem) != null &&(e.AddedItems[0] as TabItem).Content is IFilterable mx2)
                mx2.FilterShow();
        }

        private void TabItem_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ListDevicesForExport();
        }

        private void ChangedPublishOnlyLatest(object sender, RoutedEventArgs e)
        {
            ListDevicesForExport();
        }

        private void ListDevicesForExport()
        {
            Exports.Clear();
            foreach (Models.Hardware hard in General.Hardware)
            {
                foreach (Models.Device dev in hard.Devices)
                {
                    foreach (Models.Application app in hard.Apps)
                    {
                        if (InPublishOnlyLatest.IsChecked == true)
                        {
                            Models.AppVersionModel ver = app.Versions.OrderByDescending(v => v.Number).First();
                            Models.ExportItem item = new Models.ExportItem();
                            item.Hardware = hard;
                            item.Device = dev;
                            item.App = app;
                            item.Version = ver;
                            Exports.Add(item);
                        } else {
                            foreach (Models.AppVersionModel ver in app.Versions)
                            {
                                Models.ExportItem item = new Models.ExportItem();
                                item.Hardware = hard;
                                item.Device = dev;
                                item.App = app;
                                item.Version = ver;
                                Exports.Add(item);
                            }
                        }
                    }
                }
            }

            ExportList.ItemsSource = Exports;
        }

        private void ExportInFilter_TextChanged(object sender, TextChangedEventArgs e)
        {
            ExportList.ItemsSource = Exports.Where(i => i.Version.NameText.Contains(ExportInFilter.Text) || i.App.NameText.Contains(ExportInFilter.Text) || i.Hardware.Name.Contains(ExportInFilter.Text) || i.Device.Name.Contains(ExportInFilter.Text));
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            if((sender as Button).DataContext is Models.Module) {
                ((sender as Button).DataContext as Models.Module).Id = -1;
            } else {
                throw new Exception("Unbekannter Typ zum ID löschen: " + (sender as Button).DataContext.GetType().ToString());
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            if(ExportInName.Text.EndsWith(".knxprod"))
                ExportInName.Text = ExportInName.Text.Substring(0, ExportInName.Text.LastIndexOf('.'));

            if(File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", ExportInName.Text + ".knxprod")))
            {
                if(MessageBoxResult.No == MessageBox.Show($"Es existiert bereits ein Export'{ExportInName.Text}.knxprod'.\r\nSoll dieser Überschrieben werden?", "Datei überschreiben", MessageBoxButton.YesNo, MessageBoxImage.Question))
                    return;
            }


            PublishActions.Clear();
            await Task.Delay(1000);
            if(SelectedVersion != null)
                SelectedVersion.Version = Newtonsoft.Json.JsonConvert.SerializeObject(SelectedVersion.Model, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            
            List<Models.Hardware> hardware = new List<Models.Hardware>();
            List<Models.Device> devices = new List<Models.Device>();
            List<Models.Application> apps = new List<Models.Application>();
            List<Models.AppVersionModel> versions = new List<Models.AppVersionModel>();

            foreach(Models.ExportItem item in Exports.Where(ex => ex.Selected))
            {
                if (!hardware.Contains(item.Hardware)) hardware.Add(item.Hardware);
                if (!devices.Contains(item.Device)) devices.Add(item.Device);
                if (!apps.Contains(item.App)) apps.Add(item.App);
                if (!versions.Contains(item.Version)) versions.Add(item.Version);
            }

            CheckHelper.CheckThis(General, hardware, devices, apps, versions, PublishActions);


            if(PublishActions.Count(pa => pa.State == Models.PublishState.Fail) > 0)
            {
                PublishActions.Add(new Models.PublishAction() { Text = "Erstellen abgebrochen. Es traten Fehler bei der Überprüfung auf.", State = Models.PublishState.Fail });
                return;
            }
            else
                PublishActions.Add(new Models.PublishAction() { Text = "Überprüfung bestanden", State = Models.PublishState.Success });

            await Task.Delay(1000);

            PublishActions.Add(new Models.PublishAction() { Text = "Erstelle Produktdatenbank", State = Models.PublishState.Info });

            await Task.Delay(1000);
            
            string convPath = etsPath;
            if(System.IO.Directory.Exists(System.IO.Path.Combine(convPath, "CV", "6.0")))
                convPath = System.IO.Path.Combine(convPath, "CV", "6.0");
            else
            {
                if(versions.GroupBy(v => v.Namespace).Count() > 1)
                {
                    PublishActions.Add(new Models.PublishAction() { Text = "Produktdatenbank haben unterschiedlichen Namespace", State = Models.PublishState.Fail });
                    return;
                }

                Models.EtsVersion etsVersion = EtsVersions.Single(v => v.Number == versions[0].Namespace);
                if(!etsVersion.IsEnabled)
                {
                    PublishActions.Add(new Models.PublishAction() { Text = $"Der gewünschte Namespace /{etsVersion.Number} kann auf diesem System nicht erstellt werden", State = Models.PublishState.Fail });
                    return;
                }
                convPath = System.IO.Path.Combine(convPath, "CV", etsVersion.FolderPath);
            }

            ExportHelper helper = new ExportHelper(General, hardware, devices, apps, versions, convPath, ExportInName.Text);
            switch(InPublishTarget.SelectedValue) {
                case "ets":
                    bool success = helper.ExportEts(PublishActions);
                    if(!success)
                    {
                        MessageBox.Show("Produktdatenbank konnte nicht veröffentlicht werden.");
                        return;
                    }
                    helper.SignOutput();
                    break;

                case "kaenx":
                    throw new NotImplementedException("Dieses Feature wurde noch nicht implementiert");
            }
            System.Windows.MessageBox.Show("Erfolgreich erstellt");
        }

        private void CurrentCellChanged(object sender, EventArgs e)
        {
            Models.Memory mem = (sender as DataGrid).DataContext as Models.Memory;
            if(mem == null) return;
            DataGridCellInfo cell = (sender as DataGrid).CurrentCell;
            Models.MemorySection sec = cell.Item as Models.MemorySection;
            if(!cell.IsValid || (cell.Column.DisplayIndex > (sec.Bytes.Count - 1))) return;

            mem.CurrentMemoryByte = sec.Bytes[cell.Column.DisplayIndex];
        }

        private void ChangeLang(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem)
            {
                string tag = (sender as MenuItem).Tag.ToString();
                Properties.Settings.Default.language = tag;
                Properties.Settings.Default.Save();
                CheckLangs();
            }
        }

        private void ChangeAutoUpdate(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem)
            {
                Properties.Settings.Default.autoUpdate = (sender as MenuItem).IsChecked;
                Properties.Settings.Default.Save();
                CheckLangs();
            }
        }

        private void ClickToggleDebug(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem)
            {
                bool tag = (sender as MenuItem).IsChecked;
                Properties.Settings.Default.isDebug = tag;
                Properties.Settings.Default.Save();
            }
        }

        private void ClickHelp(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://github.com/OpenKNX/Kaenx-Creator/wiki") { UseShellExecute = true });
        }

        private async void ClickCheckVersion(object sender, RoutedEventArgs e)
        {
            (bool update, string vers) response = await CheckUpdate();
            if(response.update)
            {
                if(MessageBoxResult.Yes == MessageBox.Show($"Es ist eine neue version verfügbar: v{response.vers}\r\nJetzt zu den Github Releases gehen?", "Update suchen", MessageBoxButton.YesNo, MessageBoxImage.Question))
                {
                    Process.Start(new ProcessStartInfo("https://github.com/OpenKNX/Kaenx-Creator/releases/latest") { UseShellExecute = true });
                }
            } else 
                MessageBox.Show($"Sie verwenden bereits die neueste version: v{string.Join('.', System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString().Split('.').Take(3))}", "Update suchen", MessageBoxButton.OK, MessageBoxImage.Information);
                    

        }

        private async Task<(bool, string)> CheckUpdate()
        {
            try{
                HttpClient client = new HttpClient();
                    
                HttpResponseMessage resp = await client.GetAsync("https://github.com/OpenKNX/Kaenx-Creator/releases/latest", HttpCompletionOption.ResponseHeadersRead);
                string version = resp.RequestMessage.RequestUri.ToString();
                version = version.Substring(version.LastIndexOf('/') + 2);
                string[] newVers = version.Split('.');
                string[] oldVers = System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString().Split('.');
                bool flag = false;

                for(int i = 0; i < 3; i++)
                {
                    int comp = newVers[i].CompareTo(oldVers[i]);
                    if(comp == 1)
                    {
                        flag = true;
                        break;
                    }
                    if(comp == -1)
                    {
                        break;
                    }
                }
                return (flag, version);
            } catch {
                MessageBox.Show("Beim Abfragen der neuesten Version hab es Probleme.", "Update suchen", MessageBoxButton.OK, MessageBoxImage.Error);
                return (false, "");
            }
        }
    }
}
