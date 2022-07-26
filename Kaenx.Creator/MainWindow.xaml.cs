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

        private ObservableCollection<Models.DataPointType> dpts;
        public ObservableCollection<Models.DataPointType> DPTs
        {
            get { return dpts; }
            set { dpts = value; Changed("DPTs"); }
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
            //new Models.EtsVersion(20, "ETS 5.7 (20)", "5.7.293.38537"),
            new Models.EtsVersion(20, "ETS 5.7 (20)", "5.7"),
            new Models.EtsVersion(21, "ETS 6.0 (21)", "6.0")
        };


        public MainWindow()
        {
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
            if(Directory.Exists(System.IO.Path.Combine(path, "CV", versionInfo.FileVersion)))
                return true;

            string newVersion = versionInfo.FileVersion;
            if (versionInfo.FileVersion.Split('.').Length == 2) newVersion = string.Join('.', newVersion.Split('.').Take(2));
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
            General = new Models.ModelGeneral();
            General.Languages.Add(new Models.Language("Deutsch", "de-DE"));
            General.DefaultLanguage = "de-DE";
            General.Catalog.Add(new Models.CatalogItem() { Name = "Hauptkategorie (wird nicht exportiert)" });
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

            if(app.Versions.Count > 0){
                Models.AppVersion ver = app.Versions.OrderByDescending(v => v.Number).ElementAt(0);
                newVer.Number = ver.Number + 1;
            }
            
            app.Versions.Add(newVer);
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
            if (AppList.SelectedItem == null || VersionList.SelectedItem == null) return;

            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;

            app.Versions.Remove(ver);
        }

        private void ClickCopyVersion(object sender, RoutedEventArgs e)
        {
            if (AppList.SelectedItem == null || VersionList.SelectedItem == null) return;

            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(ver, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });

            Models.AppVersion copy = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AppVersion>(json, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            copy.Number += 1;
            copy.Name += " Kopie";
            app.Versions.Add(copy);
        }

        private void ClickOpenViewer(object sender, RoutedEventArgs e)
        {
            if(MessageBoxResult.Cancel == MessageBox.Show("Achtung, um den Viewer verwenden zu können, werden die IDs überprüft und ggf. neue vergeben.\r\n\r\nTrotzdem weiter machen?", "ProdViewer öffnen", MessageBoxButton.OKCancel)) return;

            AutoHelper.CheckIds((Models.AppVersion)VersionList.SelectedItem);

            ViewerWindow viewer = new ViewerWindow(new Viewer.ImporterCreator((Models.AppVersion)VersionList.SelectedItem, (Models.Application)AppList.SelectedItem));
            viewer.Show();
        }

        private void ClickViewerKnxProd(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Diese Funktion lädt eine Statische Datei und ist nur für Entwicklungszwecke eingebaut.");
            ViewerWindow viewer = new ViewerWindow(new Viewer.ImporterKnxProd(@"C:\Users\u6\Downloads\output.knxprod"));
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
        }

        private void ClickAddLanguageVers(object sender, RoutedEventArgs e)
        {
            if(LanguagesListVers.SelectedItem == null){
                MessageBox.Show("Bitte wählen Sie erst eine Sprache aus.");
                return;
            }
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            Models.Language lang = LanguagesListVers.SelectedItem as Models.Language;
            
            if(ver.Languages.Any(l => l.CultureCode == lang.CultureCode))
                MessageBox.Show("Die Sprache wird bereits unterstützt.");
            else {
                ver.Languages.Add(lang);
                ver.Text.Add(new Models.Translation(lang, ""));
                foreach(Models.Parameter para in ver.Parameters) para.Text.Add(new Models.Translation(lang, ""));
                foreach(Models.ComObject com in ver.ComObjects) {
                    com.Text.Add(new Models.Translation(lang, ""));
                    com.FunctionText.Add(new Models.Translation(lang, ""));
                }
                foreach(Models.ParameterType type in ver.ParameterTypes) {
                    if(type.Type != Models.ParameterTypes.Enum) continue;

                    foreach(Models.ParameterTypeEnum enu in type.Enums) {
                        enu.Text.Add(new Models.Translation(lang, ""));
                    }
                }
            }
        }

        private void ClickRemoveLanguageVers(object sender, RoutedEventArgs e) {
            if(SupportedLanguagesVers.SelectedItem == null){
                MessageBox.Show("Bitte wählen Sie erst oben eine Sprache aus.");
                return;
            }
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            Models.Language lang = SupportedLanguagesVers.SelectedItem as Models.Language;

            ver.Text.Remove(ver.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            ver.Languages.Remove(ver.Languages.Single(l => l.CultureCode == lang.CultureCode));
            foreach(Models.Parameter para in ver.Parameters) {
                para.Text.Remove(para.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            } 
            foreach(Models.ComObject com in ver.ComObjects) {
                com.Text.Remove(com.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                com.FunctionText.Remove(com.FunctionText.Single(l => l.Language.CultureCode == lang.CultureCode));
            }
            foreach(Models.ParameterType type in ver.ParameterTypes) {
                if(type.Type != Models.ParameterTypes.Enum) continue;

                foreach(Models.ParameterTypeEnum enu in type.Enums) {
                    enu.Text.Remove(enu.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                }
            }
        }


        private void ClickAddLanguageGen(object sender, RoutedEventArgs e)
        {
            if(LanguagesListGen.SelectedItem == null){
                MessageBox.Show("Bitte wählen Sie erst eine Sprache aus.");
                return;
            }
            Models.Language lang = LanguagesListGen.SelectedItem as Models.Language;
            
            if(_general.Languages.Any(l => l.CultureCode == lang.CultureCode))
                MessageBox.Show("Die Sprache wird bereits unterstützt.");
            else {
                _general.Languages.Add(lang);
                LanguageCatalogItemAdd(_general.Catalog[0], lang);
                foreach(Models.Hardware hard in _general.Hardware) {
                    foreach(Models.Device dev in hard.Devices) {
                        dev.Text.Add(new Models.Translation(lang, ""));
                        dev.Description.Add(new Models.Translation(lang, ""));
                    }
                }
            }
        }

        private void LanguageCatalogItemAdd(Models.CatalogItem parent, Models.Language lang)
        {
            foreach(Models.CatalogItem item in parent.Items) {
                item.Text.Add(new Models.Translation(lang, ""));

                LanguageCatalogItemAdd(item, lang);
            }
        }

        private void LanguageCatalogItemRemove(Models.CatalogItem parent, Models.Language lang)
        {
            foreach(Models.CatalogItem item in parent.Items) {
                if(item.IsSection)
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
                    dev.Text.Remove(dev.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                    dev.Description.Remove(dev.Description.Single(l => l.Language.CultureCode == lang.CultureCode));
                } 
            }
        }

        private void ClickAddModule(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            Models.Module mod = new Models.Module() { UId = AutoHelper.GetNextFreeUId(ver.Modules)};
            mod.Arguments.Add(new Models.Argument() { Name = "argParas", UId = AutoHelper.GetNextFreeUId(mod.Arguments) });
            mod.Arguments.Add(new Models.Argument() { Name = "argComs", UId = AutoHelper.GetNextFreeUId(mod.Arguments) });
            //mod.Arguments.Add(new Models.Argument() { Name = "argChan", UId = AutoHelper.GetNextFreeUId(mod.Arguments) });
            mod.ParameterBaseOffset = mod.Arguments[0];
            mod.ComObjectBaseNumber = mod.Arguments[1];
            mod.Dynamics.Add(new Models.Dynamic.DynamicModule());
            ver.Modules.Add(mod);
        }

        private void ClickRemoveModule(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            Models.Module mod = ModuleList.SelectedItem as Models.Module;
            ver.Modules.Remove(mod);
            RemoveModule(ver.Dynamics[0], mod);
        }

        private void RemoveModule(Models.Dynamic.IDynItems item, Models.Module mod)
        {
            if(item is Models.Dynamic.DynModule dm)
                dm.ModuleObject = null;

            if(item.Items != null)
                foreach(Models.Dynamic.IDynItems ditem in item.Items)
                    RemoveModule(ditem, mod);
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
            string general = Newtonsoft.Json.JsonConvert.SerializeObject(General, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            System.IO.File.WriteAllText(filePath, general);
        }

        private void ClickClose(object sender, RoutedEventArgs e)
        {
            General = null;
            SetButtons(false);
            MenuSaveBtn.IsEnabled = false;
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
            string general = System.IO.File.ReadAllText(path);
            General = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.ModelGeneral>(general, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            filePath = path;

            foreach(Models.Application app in General.Applications)
            {
                foreach(Models.AppVersion ver in app.Versions)
                {
                    LoadVersion(ver, ver);

                    foreach(Models.Module mod in ver.Modules)
                        LoadVersion(ver, mod);

                    foreach(Models.ParameterType ptype in ver.ParameterTypes.Where(p => p.Type == Models.ParameterTypes.Picture && p._baggageUId != -1))
                    {
                        ptype.BaggageObject = General.Baggages.SingleOrDefault(b => b.UId == ptype._baggageUId);
                    }
                }


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

        private void LoadVersion(Models.AppVersion vbase, Models.IVersionBase mod)
        {
            if(vbase == mod) {
                if(vbase._addressMemoryId != -1)
                    vbase.AddressMemoryObject = vbase.Memories.SingleOrDefault(m => m.UId == vbase._addressMemoryId);

                if(vbase._assocMemoryId != -1)
                    vbase.AssociationMemoryObject = vbase.Memories.SingleOrDefault(m => m.UId == vbase._assocMemoryId);
                    
                if(vbase._comMemoryId != -1)
                    vbase.ComObjectMemoryObject = vbase.Memories.SingleOrDefault(m => m.UId == vbase._comMemoryId);
            } else {
                Models.Module modu = mod as Models.Module;
                if(modu._parameterBaseOffsetUId != -1)
                    modu.ParameterBaseOffset = modu.Arguments.SingleOrDefault(m => m.UId == modu._parameterBaseOffsetUId);
                
                if(modu._comObjectBaseNumberUId != -1)
                    modu.ComObjectBaseNumber = modu.Arguments.SingleOrDefault(m => m.UId == modu._comObjectBaseNumberUId);
            }

            foreach(Models.Parameter para in mod.Parameters)
            {
                if (para._memoryId != -1)
                    para.MemoryObject = vbase.Memories.SingleOrDefault(m => m.UId == para._memoryId);
                    
                if (para._parameterType != -1)
                    para.ParameterTypeObject = vbase.ParameterTypes.SingleOrDefault(p => p.UId == para._parameterType);

                if(para.IsInUnion && para._unionId != -1)
                    para.UnionObject = mod.Unions.SingleOrDefault(u => u.UId == para._unionId);
            }

            foreach(Models.Union union in mod.Unions)
            {
                if (union._memoryId != -1)
                    union.MemoryObject = vbase.Memories.SingleOrDefault(u => u.UId == union._memoryId);
            }

            foreach(Models.ParameterRef pref in mod.ParameterRefs)
            {
                if (pref._parameter != -1)
                    pref.ParameterObject = mod.Parameters.SingleOrDefault(p => p.UId == pref._parameter);
            }

            foreach(Models.ComObject com in mod.ComObjects)
            {
                if(com._parameterRef != -1)
                    com.ParameterRefObject = mod.ParameterRefs.SingleOrDefault(p => p.UId == com._parameterRef);

                if (!string.IsNullOrEmpty(com._typeNumber))
                    com.Type = DPTs.Single(d => d.Number == com._typeNumber);
                    
                if(!string.IsNullOrEmpty(com._subTypeNumber) && com.Type != null)
                    com.SubType = com.Type.SubTypes.Single(d => d.Number == com._subTypeNumber);
            }

            foreach(Models.ComObjectRef cref in mod.ComObjectRefs)
            {
                if (cref._comObject != -1)
                    cref.ComObjectObject = mod.ComObjects.SingleOrDefault(c => c.UId == cref._comObject);

                if (!string.IsNullOrEmpty(cref._typeNumber))
                    cref.Type = DPTs.Single(d => d.Number == cref._typeNumber);
                    
                if(!string.IsNullOrEmpty(cref._subTypeNumber) && cref.Type != null)
                    cref.SubType = cref.Type.SubTypes.Single(d => d.Number == cref._subTypeNumber);
            }

            if(mod is Models.Module mod2)
            {
                if(mod2._parameterBaseOffsetUId != -1)
                    mod2.ParameterBaseOffset = mod2.Arguments.SingleOrDefault(a => a.UId == mod2._parameterBaseOffsetUId);

                if(mod2._comObjectBaseNumberUId != -1)
                    mod2.ComObjectBaseNumber = mod2.Arguments.SingleOrDefault(a => a.UId == mod2._comObjectBaseNumberUId);
            }

            if(mod.Dynamics.Count > 0)
                LoadSubDyn(mod.Dynamics[0], mod.ParameterRefs.ToList(), mod.ComObjectRefs.ToList(), vbase.Modules.ToList());
        }

        private void LoadSubDyn(Models.Dynamic.IDynItems dyn, List<Models.ParameterRef> paras, List<Models.ComObjectRef> coms, List<Models.Module> mods)
        {
            foreach (Models.Dynamic.IDynItems item in dyn.Items)
            {
                item.Parent = dyn;

                switch(item)
                {
                    case Models.Dynamic.DynChannel dch:
                        if(dch.UseTextParameter)
                            dch.ParameterRefObject = paras.SingleOrDefault(p => p.UId == dch._parameter);
                        break;

                    case Models.Dynamic.DynParameter dp:
                        if (dp._parameter != -1)
                            dp.ParameterRefObject = paras.SingleOrDefault(p => p.UId == dp._parameter);
                        break;

                    case Models.Dynamic.DynChooseBlock dcb:
                        if (dcb._parameterRef != -1)
                            dcb.ParameterRefObject = paras.SingleOrDefault(p => p.UId == dcb._parameterRef);
                        break;

                    case Models.Dynamic.DynChooseChannel dcc:
                        if (dcc._parameterRef != -1)
                            dcc.ParameterRefObject = paras.SingleOrDefault(p => p.UId == dcc._parameterRef);
                        break;

                    case Models.Dynamic.DynComObject dco:
                        if (dco._comObjectRef != -1)
                            dco.ComObjectRefObject = coms.SingleOrDefault(c => c.UId == dco._comObjectRef);
                        break;

                    case Models.Dynamic.DynParaBlock dpb:
                        if(dpb.UseParameterRef && dpb._parameterRef != -1)
                            dpb.ParameterRefObject = paras.SingleOrDefault(p => p.UId == dpb._parameterRef);
                        if(dpb.UseTextParameter && dpb._textRef != -1)
                            dpb.TextRefObject = paras.SingleOrDefault(p => p.UId == dpb._textRef);
                        break;

                    case Models.Dynamic.DynModule dm:
                        if(dm._module != -1)
                        {
                            dm.ModuleObject = mods.Single(m => m.UId == dm._module);
                            foreach(Models.Dynamic.DynModuleArg arg in dm.Arguments)
                            {
                                if(arg._argId != -1)
                                    arg.Argument = dm.ModuleObject.Arguments.Single(a => a.UId == arg._argId);
                            }
                        }
                        break;
                }

                if (item.Items != null)
                    LoadSubDyn(item, paras, coms, mods);
            }
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
                if(TabsEdit.SelectedIndex == 5)
                    TabsEdit.SelectedIndex = 4;
            } else {
                TabsEdit.SelectedIndex = 0;
                TabsEdit.Visibility = Visibility.Collapsed;
                LogoGrid.Visibility = Visibility.Visible;
            }
        }

        private void ClickShowVersion(object sender, RoutedEventArgs e)
        {
            var x = System.Reflection.Assembly.GetExecutingAssembly();
            MessageBox.Show(x.FullName, "Kaenx-Creator Version", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ClickShowClean(object sender, RoutedEventArgs e)
        { 
            Models.AppVersion vers = VersionList.SelectedItem as Models.AppVersion;
            if(vers == null)
            {
                MessageBox.Show("Bitte wählen Sie erst eine Applikationsversion aus.");
                return;
            }

            Models.ClearResult res = ClearHelper.ShowUnusedElements(vers);

            string message = $"Folgende Elemente wurden nicht verwendet:\r\n";
            message += $"{res.ParameterTypes}\tParameterTypes\r\n";
            message += $"{res.Parameters}\tParameter\r\n";
            message += $"{res.ParameterRefs}\tParameterRefs\r\n";
            message += $"{res.Unions}\tUnions\r\n";
            message += $"{res.ComObjects}\tComObjects\r\n";
            message += $"{res.ComObjectRefs}\tComObjectRefs\r\n";

            MessageBox.Show(message, "Fertig");  
        }

        private void ClickDoClean(object sender, RoutedEventArgs e)
        {
            Models.AppVersion vers = VersionList.SelectedItem as Models.AppVersion;
            if(vers == null)
            {
                MessageBox.Show("Bitte wählen Sie erst eine Applikationsversion aus.");
                return;
            }

            Models.ClearResult res = ClearHelper.ShowUnusedElements(vers);

            string message = $"Folgende Elemente werden gelöscht:\r\n";
            message += $"{res.ParameterTypes}\tParameterTypes\r\n";
            message += $"{res.Parameters}\tParameter\r\n";
            message += $"{res.ParameterRefs}\tParameterRefs\r\n";
            message += $"{res.Unions}\tUnions\r\n";
            message += $"{res.ComObjects}\tComObjects\r\n";
            message += $"{res.ComObjectRefs}\tComObjectRefs\r\n";
            message += "\r\nWollen Sie diese Elemente wirklich löschen?";

            var msgRes = MessageBox.Show(message, "Bereinigung", System.Windows.MessageBoxButton.YesNo);
            if(msgRes == MessageBoxResult.Yes)
                ClearHelper.RemoveUnusedElements(vers);
        }

        private void ClickDoResetParaIds(object sender, RoutedEventArgs e)
        {
            Models.AppVersion vers = VersionList.SelectedItem as Models.AppVersion;
            ClearHelper.ResetParameterIds(vers);
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
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            AutoHelper.MemoryCalculation(ver, mem);
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
                            Models.AppVersion ver = app.Versions.OrderByDescending(v => v.Number).First();
                            Models.ExportItem item = new Models.ExportItem();
                            item.Hardware = hard;
                            item.Device = dev;
                            item.App = app;
                            item.Version = ver;
                            Exports.Add(item);
                        } else
                        {
                            foreach (Models.AppVersion ver in app.Versions)
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
            PublishActions.Clear();
            await Task.Delay(1000);
            List<Models.Hardware> hardware = new List<Models.Hardware>();
            List<Models.Device> devices = new List<Models.Device>();
            List<Models.Application> apps = new List<Models.Application>();
            List<Models.AppVersion> versions = new List<Models.AppVersion>();

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
                //Models.EtsVersion etsVersion = EtsVersions.Single(v => v.Number == highestNS);
                //convPath = System.IO.Path.Combine(convPath, "CV", etsVersion.FolderPath);
            }

            ExportHelper helper = new ExportHelper(General, hardware, devices, apps, versions, convPath);
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
    }
}
