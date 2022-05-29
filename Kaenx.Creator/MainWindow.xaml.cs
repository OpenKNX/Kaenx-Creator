using Kaenx.Creator.Classes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
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
                    item.IsChecked = item.Tag.ToString() == lang;
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

            if(app.Versions.Count > 0){
                Models.AppVersion ver = app.Versions.OrderByDescending(v => v.Number).ElementAt(0);
                newVer.Number = ver.Number + 1;
            }
            
            app.Versions.Add(newVer);
        }

        private void ClickAddParamType(object sender, RoutedEventArgs e)
        {
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.ParameterTypes.Add(new Models.ParameterType() { UId = AutoHelper.GetNextFreeUId(version.ParameterTypes) });
        }

        private void ClickAddParamEnum(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            Models.ParameterType ptype = (sender as Button).DataContext as Models.ParameterType;
            Models.ParameterTypeEnum penum = new Models.ParameterTypeEnum();
            foreach(Models.Language lang in ver.Languages)
                penum.Text.Add(new Models.Translation(lang, ""));
            if(ptype.Enums.Count > 0)
                penum.Value = ptype.Enums.OrderByDescending(e => e.Value).First().Value + 1;
            ptype.Enums.Add(penum);
        }

        private void ClickAddMemory(object sender, RoutedEventArgs e)
        {
            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.Memories.Add(new Models.Memory() { Type = app.Mask.Memory, UId = AutoHelper.GetNextFreeUId(version.Memories) });
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

            string json = Newtonsoft.Json.JsonConvert.SerializeObject(ver, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto });

            Models.AppVersion copy = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AppVersion>(json, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto });
            copy.Number += 1;
            copy.Name += " Kopie";
            app.Versions.Add(copy);
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
                MessageBox.Show("Bitte wählen Sie erst links eine Sprache aus.");
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
            mod.Dynamics.Add(new Models.Dynamic.DynamicMain());
            ver.Modules.Add(mod);
        }

        private void ClickRemoveModule(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            ver.Modules.Remove(ModuleList.SelectedItem as Models.Module);
        }

        private void ClickAddUnion(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            ver.Unions.Add(new Models.Union() { UId = AutoHelper.GetNextFreeUId(ver.Unions)});
        }
        
        private void ClickRemoveUnion(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            ver.Unions.Remove(UnionList.SelectedItem as Models.Union);
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
            string general = Newtonsoft.Json.JsonConvert.SerializeObject(General, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto });
            System.IO.File.WriteAllText(filePath, general);
        }

        private void ClickSaveAs(object sender, RoutedEventArgs e)
        {
            string general = Newtonsoft.Json.JsonConvert.SerializeObject(General, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto });

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

                string general = Newtonsoft.Json.JsonConvert.SerializeObject(General, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto });
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
            General = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.ModelGeneral>(general, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto });
            filePath = path;

            foreach(Models.Application app in General.Applications)
            {
                foreach(Models.AppVersion ver in app.Versions)
                {
                    LoadVersion(ver, ver);

                    foreach(Models.Module mod in ver.Modules)
                        LoadVersion(ver, mod);
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
                LoadSubDyn(mod.Dynamics[0], mod.ParameterRefs.ToList(), mod.ComObjectRefs.ToList());
        }

        private void LoadSubDyn(Models.Dynamic.IDynItems dyn, List<Models.ParameterRef> paras, List<Models.ComObjectRef> coms)
        {
            foreach (Models.Dynamic.IDynItems item in dyn.Items)
            {
                item.Parent = dyn;

                if (item is Models.Dynamic.DynParameter dp)
                {
                    if (dp._parameter != -1)
                        dp.ParameterRefObject = paras.Single(p => p.UId == dp._parameter);
                } else if(item is Models.Dynamic.DynChoose dc)
                {
                    if (dc._parameterRef != -1)
                        dc.ParameterRefObject = paras.Single(p => p.UId == dc._parameterRef);
                } else if(item is Models.Dynamic.DynComObject dco)
                {
                    if (dco._comObjectRef != -1)
                        dco.ComObjectRefObject = coms.Single(c => c.UId == dco._comObjectRef);
                } else if(item is Models.Dynamic.DynParaBlock dpb) {
                    if(dpb._parameterRef != -1)
                        dpb.ParameterRefObject = paras.Single(p => p.UId == dpb._parameterRef);
                }

                if (!(item is Models.Dynamic.DynParameter) && item.Items != null)
                    LoadSubDyn(item, paras, coms);
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
            
            if(TabsEdit.SelectedIndex == 4)
                TabsEdit.SelectedIndex = 3;
        }

        
        private void ClickShowClean(object sender, RoutedEventArgs e)
        { 
            Models.AppVersion vers = VersionList.SelectedItem as Models.AppVersion;

            ClearHelper.ShowUnusedElements(vers);

            MessageBox.Show("Um nicht verwendete ParameterRefs oder ComObjectRefs zu sehen, deaktivieren Sie bitte die Option 'Parameter/KOs Refs automatisch berechnen'.", "Fertig");  
        }

        private void ClickImport(object sender, RoutedEventArgs e)
        {
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".knxprod"; // Default file extension
            dialog.Filter = "KNX Produktdatenbank|*.knxprod";
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                ImportHelper helper = new ImportHelper(dialog.FileName, bcus);
                helper.Start(_general, DPTs);
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
            ExportList.ItemsSource = Exports.Where(i => i.Version.NameText.Contains(ExportInFilter.Text) || i.App.NameText.Contains(ExportInFilter.Text) || i.Hardware.Name.Contains(ExportInFilter.Text) || i.Device.Name.Contains(ExportInFilter.Text)).ToList();
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

            foreach(Models.ExportItem item in Exports.Where(ex => ex.Selected).ToList())
            {
                if (!hardware.Contains(item.Hardware)) hardware.Add(item.Hardware);
                if (!devices.Contains(item.Device)) devices.Add(item.Device);
                if (!apps.Contains(item.App)) apps.Add(item.App);
                if (!versions.Contains(item.Version)) versions.Add(item.Version);
            }

            if(apps.Count == 0) {
                PublishActions.Add(new Models.PublishAction() { Text = $"Es wurden keine Applikationen ausgewählt.", State = Models.PublishState.Fail });
                return;
            }

            PublishActions.Add(new Models.PublishAction() { Text = "Starte Check" });
            PublishActions.Add(new Models.PublishAction() { Text = $"{devices.Count} Geräte - {hardware.Count} Hardware - {apps.Count} Applikationen - {versions.Count} Versionen" });

            if(General.Catalog[0].Items.Any(c => !c.IsSection ))
                PublishActions.Add(new Models.PublishAction() { Text = "Katalog muss mindestens eine Unterkategorie haben.", State = Models.PublishState.Fail });

            if(General.ManufacturerId <= 0 || General.ManufacturerId > 0xFFFF)
                PublishActions.Add(new Models.PublishAction() { Text = $"Ungültige HerstellerId angegeben: {General.ManufacturerId:X4}", State = Models.PublishState.Fail });

            #region Hardware Check
            PublishActions.Add(new Models.PublishAction() { Text = "Überprüfe Hardware" });
            Regex reg = new Regex("^([0-9a-zA-Z_-]|\\s)+$");
            List<string> serials = new List<string>();

            var check1 = General.Hardware.GroupBy(h => h.Name).Where(h => h.Count() > 1);
            foreach(var group in check1)
                PublishActions.Add(new Models.PublishAction() { Text = "Hardwarename '" + group.Key + "' wird von " + group.Count() + " Hardware verwendet", State = Models.PublishState.Fail });

            check1 = General.Hardware.GroupBy(h => h.SerialNumber).Where(h => h.Count() > 1);
            foreach (var group in check1)
                PublishActions.Add(new Models.PublishAction() { Text = "Hardwareserial '" + group.Key + "' wird von " + group.Count() + " Hardware verwendet", State = Models.PublishState.Fail });

            check1 = null;
            var check2 = General.Hardware.Where(h => h.Devices.Count == 0);
            foreach (var group in check2)
                PublishActions.Add(new Models.PublishAction() { Text = "Hardware '" + group.Name + "' hat keine Geräte zugeordnet", State = Models.PublishState.Warning });

            check2 = General.Hardware.Where(h => h.HasApplicationProgram && h.Apps.Count == 0);
            foreach (var group in check2)
                PublishActions.Add(new Models.PublishAction() { Text = "Hardware '" + group.Name + "' hat keine Applikation zugeordnet", State = Models.PublishState.Warning });

            check2 = General.Hardware.Where(h => !h.HasApplicationProgram && h.Apps.Count != 0);
            foreach (var group in check2)
                PublishActions.Add(new Models.PublishAction() { Text = "Hardware '" + group.Name + "' hat Applikation zugeordnet obwohl angegeben ist, dass keine benötigt wird", State = Models.PublishState.Warning });

            check2 = General.Hardware.Where(h => !reg.IsMatch(h.Name));
            foreach (var group in check2)
                PublishActions.Add(new Models.PublishAction() { Text = "Hardware '" + group.Name + "' hat ungültige Zeichen im Namen", State = Models.PublishState.Fail });
            check2 = null;
            #endregion

            #region Applikation Check
            PublishActions.Add(new Models.PublishAction() { Text = "Überprüfe Applikationen" });

            var check3 = General.Applications.GroupBy(h => h.Name).Where(h => h.Count() > 1);
            foreach (var group in check3)
                PublishActions.Add(new Models.PublishAction() { Text = "Applikationsname '" + group.Key + "' wird von " + group.Count() + " Applikationen verwendet", State = Models.PublishState.Fail });

            check3 = null;
            var check4 = General.Applications.GroupBy(h => h.Number).Where(h => h.Count() > 1);
            foreach (var group in check4)
                PublishActions.Add(new Models.PublishAction() { Text = "Applikations Nummer " + group.Key + " (" + group.Key.ToString("X4") + ") wird von " + group.Count() + " Applikationen verwendet", State = Models.PublishState.Fail });

            check4 = null;
            foreach(Models.Application app in General.Applications)
            {
                var check5 = app.Versions.GroupBy(v => v.Number).Where(l => l.Count() > 1);
                foreach (var group in check5)
                    PublishActions.Add(new Models.PublishAction() { Text = "Applikation '" + app.Name + "' verwendet Version " + group.Key + " (" + Math.Floor(group.Key / 16.0) + "." + (group.Key % 16) + ") " + group.Count() + " mal", State = Models.PublishState.Fail });
            }

            int highestNS = 0;
            foreach(Models.AppVersion vers in versions) {
                Models.Application app = apps.Single(a => a.Versions.Contains(vers));
                PublishActions.Add(new Models.PublishAction() { Text = $"Prüfe Applikation '{app.Name}' Version '{vers.NameText}'" });
                
                if (vers.NamespaceVersion > highestNS)
                    highestNS = vers.NamespaceVersion;

                foreach(Models.ParameterType ptype in vers.ParameterTypes) {
                    int maxsize = (int)Math.Pow(2, ptype.SizeInBit);
        
                    switch(ptype.Type) {
                        case Models.ParameterTypes.Text:
                            if(ptype.SizeInBit % 8 != 0)
                                PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterType Text {ptype.Name} ({ptype.UId}): ist kein vielfaches von 8", State = Models.PublishState.Warning });
                            break;

                        case Models.ParameterTypes.Enum:
                            var x = ptype.Enums.GroupBy(e => e.Value);
                            foreach(var group in x.Where(g => g.Count() > 1))
                                PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterType Enum {ptype.Name} ({ptype.UId}): Wert ({group.Key}) wird öfters verwendet", State = Models.PublishState.Fail });
                            
                            if(!ptype.IsSizeManual)
                            {
                                int maxValue = -1;
                                foreach(Models.ParameterTypeEnum penum in ptype.Enums)
                                    if(penum.Value > maxValue)
                                        maxValue = penum.Value;
                                string bin = Convert.ToString(maxValue, 2);
                                ptype.SizeInBit = bin.Length;
                                maxsize = (int)Math.Pow(2, ptype.SizeInBit);
                            }

                            foreach(Models.ParameterTypeEnum penum in ptype.Enums){
                                if(penum.Value >= maxsize)
                                    PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterType Enum {ptype.Name} ({ptype.UId}): Wert ({penum.Value}) ist größer als maximaler Wert ({maxsize-1})", State = Models.PublishState.Fail });

                                if(!penum.Translate) {
                                    Models.Translation trans = penum.Text.Single(t => t.Language.CultureCode == vers.DefaultLanguage);
                                    if(string.IsNullOrEmpty(trans.Text))
                                        PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterType Enum {penum.Name}/{ptype.Name} ({ptype.UId}): Keine Übersetzung vorhanden ({trans.Language.Text})", State = Models.PublishState.Fail });
                                } else {
                                    foreach(Models.Translation trans in penum.Text)
                                        if(string.IsNullOrEmpty(trans.Text))
                                            PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterType Enum {penum.Name}/{ptype.Name} ({ptype.UId}): Keine Übersetzung vorhanden ({trans.Language.Text})", State = Models.PublishState.Warning });
                                }
                            }
                            break;

                        case Models.ParameterTypes.NumberUInt:
                            if(!ptype.IsSizeManual)
                            {
                                string bin = Convert.ToString(ptype.Max, 2);
                                ptype.SizeInBit = bin.Length;
                                maxsize = (int)Math.Pow(2, ptype.SizeInBit);
                            }
                            if(ptype.Min < 0) PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Min kann nicht kleiner als 0 sein", State = Models.PublishState.Fail });
                            if(ptype.Min > ptype.Max) PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Min ({ptype.Min}) ist größer als Max ({ptype.Max})", State = Models.PublishState.Fail });
                            if(ptype.Max >= maxsize) PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Max ({ptype.Max}) kann nicht größer als das Maximum ({maxsize-1}) sein", State = Models.PublishState.Fail });
                            break;

                        case Models.ParameterTypes.NumberInt:
                            if(!ptype.IsSizeManual)
                            {
                                int z = ptype.Min * (-1);
                                if(z < (ptype.Max - 1)) z = ptype.Max;
                                string y = z.ToString().Replace("-", "");
                                string bin = Convert.ToString(int.Parse(y), 2);
                                if(z == (ptype.Min * (-1))) bin += "1";
                                ptype.SizeInBit = bin.Length;
                                maxsize = (int)Math.Pow(2, ptype.SizeInBit);
                            }
                            if(ptype.Min > ptype.Max) PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterType Int {ptype.Name} ({ptype.UId}): Min ({ptype.Min}) ist größer als Max ({ptype.Max})", State = Models.PublishState.Fail });
                            if(ptype.Max > ((maxsize/2)-1)) PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterType Int {ptype.Name} ({ptype.UId}): Max ({ptype.Max}) kann nicht größer als das Maximum ({(maxsize/2)-1}) sein", State = Models.PublishState.Fail });
                            if(ptype.Min < ((maxsize/2)*(-1))) PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterType Int {ptype.Name} ({ptype.UId}): Min ({ptype.Min}) kann nicht kleiner als das Minimum ({(maxsize/2)*(-1)}) sein", State = Models.PublishState.Fail });
                            break;

                        case Models.ParameterTypes.Float9:
                            break;

                        case Models.ParameterTypes.Picture:
                            break;

                        case Models.ParameterTypes.None:
                            break;

                        case Models.ParameterTypes.IpAddress:
                            break;

                        default:
                            PublishActions.Add(new Models.PublishAction() { Text = $"    Unbekannter ParameterTyp für {ptype.Name} ({ptype.UId})", State = Models.PublishState.Fail });
                            break;
                    }
                }

                foreach(Models.Parameter para in vers.Parameters) {
                    if(para.ParameterTypeObject == null) PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Kein ParameterTyp ausgewählt", State = Models.PublishState.Fail });
                    else {
                        switch(para.ParameterTypeObject.Type) {
                            case Models.ParameterTypes.Text:
                                if((para.Value.Length*8) > para.ParameterTypeObject.SizeInBit) PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert benötigt mehr Speicher ({(para.Value.Length*8)}) als verfügbar ({para.ParameterTypeObject.SizeInBit}) ist", State = Models.PublishState.Fail });
                                break;

                            case Models.ParameterTypes.Enum:
                                int paraval2;
                                if(!int.TryParse(para.Value, out paraval2)) PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = Models.PublishState.Fail });
                                else {
                                    if(!para.ParameterTypeObject.Enums.Any(e => e.Value == paraval2))
                                        PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist nicht als option in Enum vorhanden", State = Models.PublishState.Fail });
                                }
                                break;

                            case Models.ParameterTypes.NumberUInt:
                            case Models.ParameterTypes.NumberInt:
                                int paraval;
                                if(!int.TryParse(para.Value, out paraval)) PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = Models.PublishState.Fail });
                                else {
                                    if(paraval > para.ParameterTypeObject.Max || paraval < para.ParameterTypeObject.Min)
                                        PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) fällt nicht in Bereich {para.ParameterTypeObject.Min}-{para.ParameterTypeObject.Max}", State = Models.PublishState.Fail });
                                }
                                break;

                            case Models.ParameterTypes.Float9:


                            case Models.ParameterTypes.Picture:
                            case Models.ParameterTypes.None:
                            case Models.ParameterTypes.IpAddress:
                                break;
                        }
                    }
                    

                    //TODO auslagern in Funktion
                    if(para.TranslationText) {
                        Models.Translation trans = para.Text.Single(t => t.Language.CultureCode == vers.DefaultLanguage);
                        if(string.IsNullOrEmpty(trans.Text))
                            PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Keine Übersetzung vorhanden ({trans.Language.Text})", State = Models.PublishState.Fail });
                    } else {
                        foreach(Models.Translation trans in para.Text)
                            if(string.IsNullOrEmpty(trans.Text))
                                PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Keine Übersetzung vorhanden ({trans.Language.Text})", State = Models.PublishState.Warning });
                    }

                    if(!para.IsInUnion) {
                        switch(para.SavePath) {
                            case Models.ParamSave.Memory:
                                if(para.MemoryObject == null)
                                    PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Kein Speichersegment ausgewählt", State = Models.PublishState.Fail });
                                else {
                                    if(!para.MemoryObject.IsAutoPara && para.Offset == -1) PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name}: Kein Offset angegeben", State = Models.PublishState.Fail });
                                    if(!para.MemoryObject.IsAutoPara && para.OffsetBit == -1) PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name}: Kein Bit Offset angegeben", State = Models.PublishState.Fail });

                                }
                                if(para.OffsetBit > 7) PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): BitOffset größer als 7 und somit obsolet", State = Models.PublishState.Fail });
                                    break;
                        }
                    }
                }
            
                foreach(Models.ParameterRef para in vers.ParameterRefs) {
                    if(para.ParameterObject == null) PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Kein Parameter ausgewählt", State = Models.PublishState.Fail });
                    else {
                        if(para.ParameterObject.ParameterTypeObject == null || string.IsNullOrEmpty(para.Value))
                            continue;
                        
                        //TODO check value overwrite
                        Models.ParameterType ptype = para.ParameterObject.ParameterTypeObject;

                        switch(ptype.Type) {
                            case Models.ParameterTypes.Text:
                                if((para.Value.Length*8) > ptype.SizeInBit) PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Wert benötigt mehr Speicher ({(para.Value.Length*8)}) als verfügbar ({ptype.SizeInBit}) ist", State = Models.PublishState.Fail });
                                break;

                            case Models.ParameterTypes.Enum:
                                int paraval2;
                                if(!int.TryParse(para.Value, out paraval2)) PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = Models.PublishState.Fail });
                                else {
                                    if(!ptype.Enums.Any(e => e.Value == paraval2))
                                        PublishActions.Add(new Models.PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Wert ({para.Value}) ist nicht als option in Enum vorhanden", State = Models.PublishState.Fail });
                                }
                                break;

                            case Models.ParameterTypes.NumberUInt:
                            case Models.ParameterTypes.NumberInt:
                                int paraval;
                                if(!int.TryParse(para.Value, out paraval)) PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = Models.PublishState.Fail });
                                else {
                                    if(paraval > ptype.Max || paraval < ptype.Min)
                                        PublishActions.Add(new Models.PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) fällt nicht in Bereich {ptype.Min}-{ptype.Max}", State = Models.PublishState.Fail });
                                }
                                break;

                            case Models.ParameterTypes.Float9:


                            case Models.ParameterTypes.Picture:
                            case Models.ParameterTypes.None:
                            case Models.ParameterTypes.IpAddress:
                                break;
                        }
                    }
                }
            
                foreach(Models.ComObject com in vers.ComObjects) {
                    if(com.HasDpt && com.Type == null) PublishActions.Add(new Models.PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Kein DataPointType angegeben", State = Models.PublishState.Fail });
                    if(com.HasDpt && com.Type != null && com.Type.Number == "0") PublishActions.Add(new Models.PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Angabe des DPT nur bei Refs", State = Models.PublishState.Fail });
                    if(com.HasDpt && com.HasDpts && com.SubType == null) PublishActions.Add(new Models.PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Kein DataPointSubType angegeben", State = Models.PublishState.Fail });
                
                    //TODO auslagern in Funktion
                    if(com.TranslationText) {
                        Models.Translation trans = com.Text.Single(t => t.Language.CultureCode == vers.DefaultLanguage);
                        if(string.IsNullOrEmpty(trans.Text))
                            PublishActions.Add(new Models.PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = Models.PublishState.Fail });
                    } else {
                        foreach(Models.Translation trans in com.Text)
                            if(string.IsNullOrEmpty(trans.Text))
                                PublishActions.Add(new Models.PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = Models.PublishState.Warning });
                    }

                    if(com.TranslationFunctionText) {
                        Models.Translation trans = com.FunctionText.Single(t => t.Language.CultureCode == vers.DefaultLanguage);
                        if(string.IsNullOrEmpty(trans.Text))
                            PublishActions.Add(new Models.PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = Models.PublishState.Fail });
                    } else {
                        foreach(Models.Translation trans in com.FunctionText)
                            if(string.IsNullOrEmpty(trans.Text))
                                PublishActions.Add(new Models.PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = Models.PublishState.Warning });
                    }
                }

                foreach(Models.ComObjectRef rcom in vers.ComObjectRefs) {
                    if(rcom.ComObjectObject == null) PublishActions.Add(new Models.PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Kein KO-Ref angegeben", State = Models.PublishState.Fail });
                    //if(rcom.HasDpts && rcom.Type == null && rcom.Name.ToLower() != "dummy") PublishActions.Add(new Models.PublishAction() { Text = $"    ComObject {rcom.Name}: Kein DataPointSubType angegeben", State = Models.PublishState.Fail });

                    if(rcom.OverwriteText) {
                        if(rcom.TranslationText) {
                            Models.Translation trans = rcom.Text.Single(t => t.Language.CultureCode == vers.DefaultLanguage);
                            if(string.IsNullOrEmpty(trans.Text))
                                PublishActions.Add(new Models.PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = Models.PublishState.Fail });
                        } else {
                            foreach(Models.Translation trans in rcom.Text)
                                if(string.IsNullOrEmpty(trans.Text))
                                    PublishActions.Add(new Models.PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = Models.PublishState.Warning });
                        }
                    }

                    if(rcom.OverwriteFunctionText) {
                        if(rcom.TranslationFunctionText) {
                            Models.Translation trans = rcom.FunctionText.Single(t => t.Language.CultureCode == vers.DefaultLanguage);
                            if(string.IsNullOrEmpty(trans.Text))
                                PublishActions.Add(new Models.PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = Models.PublishState.Fail });
                        } else {
                            foreach(Models.Translation trans in rcom.FunctionText)
                                if(string.IsNullOrEmpty(trans.Text))
                                    PublishActions.Add(new Models.PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = Models.PublishState.Warning });
                        }
                    }

                    if(rcom.UseTextParameter && rcom.ParameterRefObject == null)
                        PublishActions.Add(new Models.PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Kein TextParameter angegeben", State = Models.PublishState.Fail });
                }

                //TODO check ComObjectRefs for overwriting
                // dpt, functiontext, description

                //TODO check Modules also
                //check for Argument exist

                //TODO check union size fits parameter+offset

                //TODO check dynamic
                // - separator Text required
            
            }

            
            if(EtsVersions.Single(v => v.Number == highestNS).IsEnabled == false)
                    PublishActions.Add(new Models.PublishAction() { Text = $"Mindestens eine Applikation verwendet einen Namespace ({highestNS}), der auf diesem Rechner nicht erstellt werden kann.", State = Models.PublishState.Fail });

            #endregion


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
                Models.EtsVersion etsVersion = EtsVersions.Single(v => v.Number == highestNS);
                convPath = System.IO.Path.Combine(convPath, "CV", etsVersion.FolderPath);
            }

            ExportHelper helper = new ExportHelper(General, hardware, devices, apps, versions, convPath);
            switch(InPublishTarget.SelectedValue) {
                case "ets":
                    helper.ExportEts();
                    helper.SignOutput();
                    break;

                case "kaenx":
                    throw new NotImplementedException("Dieses Feature wurde noch nicht implementiert");
            }
            System.Windows.MessageBox.Show("Erfolgreich erstellt");
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
