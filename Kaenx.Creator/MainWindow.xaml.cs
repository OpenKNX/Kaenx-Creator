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

        private Models.MainModel _general;

        public Models.MainModel General
        {
            get { return _general; }
            set { _general = value; Changed("General"); }
        }

        public ObservableCollection<Models.MaskVersion> BCUs
        {
            get { return Kaenx.Creator.Classes.Helper.BCUs; }
        }

        public ObservableCollection<Models.DataPointType> DPTs {
            get { return Kaenx.Creator.Classes.Helper.DPTs; }
        }

        public ObservableCollection<Models.ExportItem> Exports { get; set; } = new ObservableCollection<Models.ExportItem>();
        public ObservableCollection<Models.PublishAction> PublishActions { get; set; } = new ObservableCollection<Models.PublishAction>();

        public event PropertyChangedEventHandler PropertyChanged;

        private List<Models.EtsVersion> EtsVersions = new List<Models.EtsVersion>() {
            new Models.EtsVersion(11, "ETS 4.0 (11)", "4.0"),
            new Models.EtsVersion(12, "ETS 5.0 (12)", "5.0"),
            new Models.EtsVersion(13, "ETS 5.1 (13)", "5.1"),
            new Models.EtsVersion(14, "ETS 5.6 (14)", "5.6"),
            new Models.EtsVersion(20, "ETS 5.7 (20)", "5.7"),
            new Models.EtsVersion(21, "ETS 6.0 (21)", "6.0"),
            new Models.EtsVersion(22, "ETS 6.1 (22)", "6.1"),
            new Models.EtsVersion(23, "ETS 6.2 (23)", "6.2")
        };

        public MainWindow()
        {
            Instance = this;
            string lang = Properties.Settings.Default.language;
            if(lang != "def")
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(lang);
            InitializeComponent();
            this.DataContext = this;
            Kaenx.Creator.Classes.Helper.LoadBcus();
            Kaenx.Creator.Classes.Helper.LoadDpts();
            CheckLangs();
            CheckOutput();
            CheckEtsVersions();
            LoadTemplates();

            MenuDebug.IsChecked = Properties.Settings.Default.isDebug;
            MenuUpdate.IsChecked = Properties.Settings.Default.autoUpdate;
            if(Properties.Settings.Default.autoUpdate) AutoCheckUpdate();

            if(!string.IsNullOrEmpty(App.FilePath))
            {
                DoOpen(App.FilePath);
                MenuSaveBtn.IsEnabled = true;
            }

            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            if(General != null)
                return;

            if(MessageBox.Show("Projekt wirklich schließen?\r\nNicht gespeicherte Änderungen gehen verloren", "Projekt schließen", MessageBoxButton.YesNo) == MessageBoxResult.No)
                e.Cancel = true;
        }

        private async void AutoCheckUpdate()
        {
            System.Diagnostics.Debug.WriteLine("Checking Auto Update");
            (bool update, string vers) response = await CheckUpdate();
            if(response.update)
            {
                if(MessageBoxResult.Yes == MessageBox.Show(string.Format(Properties.Messages.update_new, response.vers), Properties.Messages.update_title, MessageBoxButton.YesNo, MessageBoxImage.Question))
                {
                    Process.Start(new ProcessStartInfo("https://github.com/OpenKNX/Kaenx-Creator/releases/latest") { UseShellExecute = true });
                }
            } 
        }

        public void GoToItem(object item, object module)
        {
            if(module != null && module.GetType() == typeof(Kaenx.Creator.Models.Module))
            {
                VersionTabs.SelectedIndex = 8;
                int index2 = item switch {
                    Models.Union => 4,
                    Models.Parameter => 7,
                    Models.ParameterRef => 8,
                    Models.ComObject => 9,
                    Models.ComObjectRef => 10,
                    Models.Dynamic.IDynItems => 14,
                    _ => -1
                };

                (VersionTabs.SelectedContent as ISelectable).ShowItem(module);
                (VersionTabs.SelectedContent as ISelectable).ShowItem(item);
                return;
            }

            int index = item switch{
                Models.ParameterType => 5,
                Models.Union => 6,
                Models.Module => 8,
                Models.Parameter => 9,
                Models.ParameterRef => 10,
                Models.ComObject => 11,
                Models.ComObjectRef => 12,
                Models.Dynamic.IDynItems => 17,
                _ => -1
            };

            if(index == -1) return;
            VersionTabs.SelectedIndex = index;
            ((VersionTabs.Items[index] as TabItem).Content as ISelectable).ShowItem(item);
        }

        private void CheckEtsVersions() {
            foreach(Models.EtsVersion v in EtsVersions)
                v.IsEnabled =  Kaenx.Creator.Classes.Helper.CheckExportNamespace(v.Number);
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
            bool wasset = false;
            foreach(UIElement ele in MenuLang.Items)
            {
                if(ele is MenuItem item)
                {
                    item.IsChecked = item.Tag?.ToString() == lang;
                    if(item.IsChecked) wasset = true;
                }
            }

            if(!wasset)
                (MenuLang.Items[2] as MenuItem).IsChecked = true;
        }

        private void CheckOutput()
        {
            string outp = Properties.Settings.Default.Output;

            bool valid = false;
            foreach(UIElement ele in MenuOutput.Items)
            {
                if(ele is MenuItem item)
                {
                    if(item.Tag?.ToString() == outp)
                    {
                        valid = true;
                        break;
                    }
                }
            }
            if(!valid)
            {
                outp = "exe";
                Properties.Settings.Default.Output = outp;
                Properties.Settings.Default.Save();
            }

            bool wasset = false;
            foreach(UIElement ele in MenuOutput.Items)
            {
                if(ele is MenuItem item)
                {
                    item.IsChecked = item.Tag?.ToString() == outp;
                    if(item.IsChecked) wasset = true;
                }
            }
            
            if(!wasset)
                (MenuOutput.Items[0] as MenuItem).IsChecked = true;
        }

        private void ClickNew(object sender, RoutedEventArgs e)
        {
            General = new Models.MainModel() { ImportVersion = Kaenx.Creator.Classes.Helper.CurrentVersion, Guid = Guid.NewGuid().ToString() };
            var currentLang = System.Threading.Thread.CurrentThread.CurrentUICulture.IetfLanguageTag;
            if(!ImportHelper._langTexts.ContainsKey(currentLang))
                if(currentLang.Contains('-'))
                    currentLang = currentLang.Split('-')[0];

            if(!currentLang.Contains('-'))
            {
                currentLang = ImportHelper._langTexts.Keys.FirstOrDefault(l => l.Split('-')[0] == currentLang);
                if(string.IsNullOrEmpty(currentLang)) currentLang = "en-US";
            }
            General.Application.DefaultLanguage = currentLang;
            General.Catalog.Add(new Models.CatalogItem() { Name = Properties.Messages.main_def_cat });

            General.Application.Languages.Add(new Models.Language(System.Threading.Thread.CurrentThread.CurrentUICulture.DisplayName, currentLang));
            foreach(Models.Language lang in General.Application.Languages)
            {
                if(!General.Info.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    General.Info.Text.Add(new Models.Translation(lang, ""));
                if(!General.Info.Description.Any(t => t.Language.CultureCode == lang.CultureCode))
                    General.Info.Description.Add(new Models.Translation(lang, ""));
                if(!General.Application.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    General.Application.Text.Add(new Models.Translation(lang, ""));
            }
            

            General.Application.Dynamics.Add(new Models.Dynamic.DynamicMain());

            SetButtons(true);
            MenuSaveBtn.IsEnabled = false;
            TabsEdit.SelectedIndex = 0;
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        #region Clicks

        #region Clicks Add/Remove

        private void ClickAddHardDevice(object sender, RoutedEventArgs e)
        {
            Models.Hardware hard = (sender as Button).DataContext as Models.Hardware;
            hard.Devices.Add(new Models.Device());
        }

        private void ClickAddMemory(object sender, RoutedEventArgs e)
        {
            General.Application.Memories.Add(new Models.Memory() { Type = General.Info.Mask.Memory, UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(General.Application.Memories) });
        }

        private void ClickRemoveMemory(object sender, RoutedEventArgs e)
        {
            Models.Memory mem = ListMemories.SelectedItem as Models.Memory;
            RecursiveRemoveMemory(General.Application, mem);
            General.Application.Memories.Remove(mem);
        }

        private void RecursiveRemoveMemory(Models.IVersionBase vbase, Models.Memory mem)
        {
            foreach(Models.Parameter para in vbase.Parameters.Where(p => p.SavePath == Models.SavePaths.Memory && p.SaveObject == mem))
                para.SaveObject = null;

            foreach(Models.Module mod in vbase.Modules)
                RecursiveRemoveMemory(mod, mem);
        }

        private void ClickAddLanguage(object sender, RoutedEventArgs e)
        {
            if(LanguagesList.SelectedItem == null){
                MessageBox.Show(Properties.Messages.main_lang_select);
                return;
            }
            Models.Language lang = LanguagesList.SelectedItem as Models.Language;
            LanguagesList.SelectedItem = null;
            
            if(General.Application.Languages.Any(l => l.CultureCode == lang.CultureCode))
                MessageBox.Show(Properties.Messages.main_lang_add_error);
            else {
                DoAddLanguage(lang);
            }
        }

        private void ClickFixLanguage(object sender, RoutedEventArgs e) {
            if(LanguagesList.SelectedItem == null){
                MessageBox.Show(Properties.Messages.main_lang_select);
                return;
            }
            Models.Language lang = LanguagesList.SelectedItem as Models.Language;
            
            DoAddLanguage(lang);
        }

        private void DoAddLanguage(Models.Language lang) {
            General.Application.Languages.Add(lang);
            if(!General.Application.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                General.Application.Text.Add(new Models.Translation(lang, ""));
            
            foreach(Models.ParameterType type in General.Application.ParameterTypes) {
                if(type.Type != Models.ParameterTypes.Enum) continue;

                foreach(Models.ParameterTypeEnum enu in type.Enums)
                    if(!enu.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        enu.Text.Add(new Models.Translation(lang, ""));
            }
            foreach(Models.Message msg in General.Application.Messages) {
                if(!msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    msg.Text.Add(new Models.Translation(lang, ""));
            }
            foreach(Models.Helptext msg in General.Application.Helptexts){
                if(!msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    msg.Text.Add(new Models.Translation(lang, ""));
            }

            LanguageCatalogItemAdd(General.Catalog[0], lang);
            if(!General.Info.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                General.Info.Text.Add(new Models.Translation(lang, ""));
            if(!General.Info.Description.Any(t => t.Language.CultureCode == lang.CultureCode))
                General.Info.Description.Add(new Models.Translation(lang, ""));

            addLangToVersion(General.Application, lang);
            addLangToVersion(General.Application.Dynamics[0], lang);
            foreach(Models.Module mod in General.Application.Modules)
            {
                addLangToVersion(mod, lang);
                addLangToVersion(mod.Dynamics[0], lang);
            }
        }

        private void ClickRemoveLanguage(object sender, RoutedEventArgs e) {
            if(SupportedLanguages.SelectedItem == null){
                MessageBox.Show(Properties.Messages.main_lang_select);
                return;
            }
            Models.Language lang = SupportedLanguages.SelectedItem as Models.Language;

            if(General.Application.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                General.Application.Text.Remove(General.Application.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            General.Application.Languages.Remove(General.Application.Languages.Single(l => l.CultureCode == lang.CultureCode));
            
            foreach(Models.ParameterType type in General.Application.ParameterTypes) {
                if(type.Type != Models.ParameterTypes.Enum) continue;

                foreach(Models.ParameterTypeEnum enu in type.Enums) {
                    if(enu.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        enu.Text.Remove(enu.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
                }
            }
            foreach(Models.Message msg in General.Application.Messages) {
                if(msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    msg.Text.Remove(msg.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            }
            foreach(Models.Helptext msg in General.Application.Helptexts){
                if(msg.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                    msg.Text.Remove(msg.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            }

            LanguageCatalogItemRemove(General.Catalog[0], lang);
            if(General.Info.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                General.Info.Text.Remove(General.Info.Text.Single(l => l.Language.CultureCode == lang.CultureCode));
            if(General.Info.Description.Any(t => t.Language.CultureCode == lang.CultureCode))
                General.Info.Description.Remove(General.Info.Description.Single(l => l.Language.CultureCode == lang.CultureCode));

            removeLangFromVersion(General.Application, lang);
            foreach(Models.Module mod in General.Application.Modules)
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
        #endregion

        private void ClickSave(object sender, RoutedEventArgs e)
        {
            General.ImportVersion = Kaenx.Creator.Classes.Helper.CurrentVersion;
            DoSave();
        }

        private void ClickClose(object sender, RoutedEventArgs e)
        {
            if(MessageBox.Show("Projekt wirklich schließen?\r\nNicht gespeicherte Änderungen gehen verloren", "Projekt schließen", MessageBoxButton.YesNo) == MessageBoxResult.No)
                return;
                
            General = null;
            SetButtons(false);
            MenuSaveBtn.IsEnabled = false;
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
            General.ImportVersion = Kaenx.Creator.Classes.Helper.CurrentVersion;
            SaveFileDialog diag = new SaveFileDialog();
            diag.FileName = General.ProjectName;
            diag.Title = Properties.Messages.main_project_save_title;
            diag.Filter = Properties.Messages.main_project_filter + " (*.ae-manu)|*.ae-manu";
            
            if(diag.ShowDialog() == true)
            {
                App.FilePath = diag.FileName;
                DoSave();
                MenuSaveBtn.IsEnabled = true;
            }
        }

        private void DoSave()
        {
            // using (MemoryStream ms = new MemoryStream())
            // using (Newtonsoft.Json.Bson.BsonDataWriter datawriter = new Newtonsoft.Json.Bson.BsonDataWriter(ms))
            // {
            //     JsonSerializer serializer = JsonSerializer.Create(new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            //     serializer.Serialize(datawriter, General);
            //     File.WriteAllBytes(App.FilePath, ms.ToArray());
            // }
            // <PackageReference Include="Newtonsoft.Json.Bson" Version="1.0.2" />
            string general = Newtonsoft.Json.JsonConvert.SerializeObject(General, new Newtonsoft.Json.JsonSerializerSettings() { 
                TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects ,
                Formatting = Formatting.Indented
            });
            System.IO.File.WriteAllText(App.FilePath, general);
        }

        private void ClickSaveTemplate(object sender, RoutedEventArgs e)
        {
            General.ImportVersion = Kaenx.Creator.Classes.Helper.CurrentVersion;
            while(true) {
                Controls.PromptDialog diag = new Controls.PromptDialog(Properties.Messages.main_save_template, Properties.Messages.main_save_template_title);
                if(diag.ShowDialog() == false) {
                    return;
                }

                if(string.IsNullOrEmpty(diag.Answer))
                {
                    System.Windows.MessageBox.Show(Properties.Messages.main_save_template_empty, Properties.Messages.main_save_template_title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Error);
                    continue;
                }

                if(System.IO.File.Exists("Templates\\" + diag.Answer + ".temp"))
                {
                    var res = System.Windows.MessageBox.Show(string.Format(Properties.Messages.main_save_template_duplicate, diag.Answer), Properties.Messages.main_save_template_title, System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning);
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
            MenuSaveBtn.IsEnabled = false;
            General.Guid = Guid.NewGuid().ToString();
        }

        private void ClickOpen(object sender, RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = Properties.Messages.main_project_open_title;
            diag.Filter = Properties.Messages.main_project_filter + " (*.ae-manu)|*.ae-manu";
            if(diag.ShowDialog() == true)
            {
                DoOpen(diag.FileName);
                MenuSaveBtn.IsEnabled = true;
            }
        }

        private void ClickOpenTranslator(object sender, RoutedEventArgs e)
        {
            TranslatorWindow window = new TranslatorWindow(General);
            window.ShowDialog();
        }

        private void DoOpen(string path)
        {
            if(!File.Exists(path)) return;
            
            App.FilePath = path;
            string general = System.IO.File.ReadAllText(path);

            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("\"ImportVersion\":[ ]?([0-9]+)");
            System.Text.RegularExpressions.Match match = reg.Match(general);

            int VersionToOpen = 0;
            if(match.Success)
            {
                VersionToOpen = int.Parse(match.Groups[1].Value);
            }

            if(VersionToOpen < Kaenx.Creator.Classes.Helper.CurrentVersion && MessageBox.Show(Properties.Messages.main_project_open_old, Properties.Messages.main_project_open_format, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                general = Kaenx.Creator.Classes.Helper.CheckImportVersion(general, VersionToOpen);
            }
            if(VersionToOpen > Kaenx.Creator.Classes.Helper.CurrentVersion)
            {
                MessageBox.Show(Properties.Messages.main_project_open_new, Properties.Messages.main_project_open_format, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
                
            try{
                General = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.MainModel>(general, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            } catch {
                MessageBox.Show(Properties.Messages.main_project_open_error, Properties.Messages.main_project_open_format, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Kaenx.Creator.Classes.Helper.LoadVersion(General, General.Application);
            General.ImportVersion = Kaenx.Creator.Classes.Helper.CurrentVersion;

            SetButtons(true);
            MenuSave.IsEnabled = true;
        }

        private void SetButtons(bool enable)
        {
            MenuSave.IsEnabled = enable;
            MenuClose.IsEnabled = enable;
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
            MessageBox.Show(string.Format(Properties.Messages.update_uptodate, string.Join('.', System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString().Split('.').Take(3))), Properties.Messages.update_title, MessageBoxButton.OK, MessageBoxImage.Information);
        }
        
        private void ClickDoResetParaIds(object sender, RoutedEventArgs e)
        {
            ClearHelper.ResetParameterIds(General.Application);
        }

        private async void ClickSignFolder(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            if(dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                string sourcePath = dialog.SelectedPath;
                string targetPath = Path.Combine(Path.GetTempPath(), "sign");
                if(Directory.Exists(targetPath))
                    Directory.Delete(targetPath, true);

                foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                {
                    string x = dirPath.Substring(sourcePath.Length+1);
                    if(!x.StartsWith("M-")) continue;
                    if(x.Split('\\').Length > 2)
                    {
                        x = x.Substring(x.IndexOf('\\')+1);
                        if(!x.StartsWith("Baggages"))
                            continue;
                    }
                    Directory.CreateDirectory(dirPath.Replace(sourcePath, targetPath));
                }

                int ns = 0;
                foreach(string filePath in Directory.GetFiles(sourcePath, "*", SearchOption.AllDirectories))
                {
                    string path = Path.GetDirectoryName(filePath);
                    if(!Directory.Exists(path.Replace(sourcePath, targetPath))) continue;
                    string relativePath = path.Replace(sourcePath, "");
                    if(relativePath == "") continue;
                    relativePath = relativePath.Substring(7);

                    if(!relativePath.StartsWith("\\Baggages"))
                    {
                        if(!filePath.EndsWith(".xml")
                            && !filePath.EndsWith(".mtxml"))
                            continue;
                    }
                    if(filePath.EndsWith(".xsd") || filePath.EndsWith(".mtproj") || filePath.Contains("knx_master")) continue;
                    if(ns == 0 && filePath.Contains("_A-"))
                    {
                        string content = File.ReadAllText(filePath);
                        System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("xmlns=\"http://knx\\.org/xml/project/([0-9]{2})");
                        System.Text.RegularExpressions.Match m = reg.Match(content);
                        if(!m.Success)
                        {
                            MessageBox.Show("NamespaceVersion konnte nicht ermittelt werden");
                            return;
                        }
                        ns = int.Parse(m.Groups[1].Value);
                    }
                    if(filePath.Contains("Hardware."))
                    {
                        // xreginfo.SetAttributeValue("RegistrationNumber", "0001/" + general.Info.Version + ver.Number);
                        XDocument xhard = XDocument.Load(filePath);
                        var xhards = xhard.Root.Descendants().Where(e => e.Name.LocalName == "Hardware2Program");

                        foreach(var xhardtoprog in xhards)
                        {
                            var xreginfo = xhardtoprog.Descendants().FirstOrDefault(e => e.Name.LocalName == "RegistrationInfo");
                            if(xreginfo == null)
                            {
                                // M-02DC_H-1-3_HP-0000-11-0000
                                string[] splits = xhardtoprog.Attribute("Id")?.Value.Split('-') ?? new string[0];
                                if(splits.Length < 7) continue;
                                int appversionInt = int.Parse(splits[5], System.Globalization.NumberStyles.HexNumber);
                                string appversion = appversionInt.ToString();
                                string hardversion = splits[3].Substring(0, splits[3].IndexOf('_'));
                                
                                XElement xreg = new XElement(XName.Get("RegistrationInfo", xhard.Root.Name.NamespaceName));
                                xreg.SetAttributeValue("RegistrationStatus", "Registered");
                                xreg.SetAttributeValue("RegistrationNumber", "0001/" + hardversion + appversion);
                                xhardtoprog.Add(xreg);
                                // <RegistrationInfo RegistrationStatus="Registered" RegistrationNumber="0001/317" />
                            }
                        }
                        xhard.Save(filePath.Replace(sourcePath, targetPath).Replace(".mtxml", ".xml"));
                    } else
                    {
                        File.Copy(filePath, filePath.Replace(sourcePath, targetPath).Replace(".mtxml", ".xml"));
                    }
                }

                Kaenx.Creator.Classes.ExportHelper helper = new Kaenx.Creator.Classes.ExportHelper(General, null);
                helper.SetNamespace(ns);
                await OpenKNX.Toolbox.Sign.SignHelper.CheckMaster(targetPath, ns);
                await helper.SignOutput(targetPath, Path.Combine(sourcePath, "sign.knxprod"), ns);
                Directory.Delete(targetPath, true);

                System.Windows.MessageBox.Show(Properties.Messages.main_export_success, Properties.Messages.main_export_title);
            }
        }

        private void ClickImport(object sender, RoutedEventArgs e)
        {
            Dictionary<string, string> filters = new Dictionary<string, string>() {
                {"knxprod", "KNX Produktdatenbank (*.knxprod)|*.knxprod"},
                {"xml", "XML Produktatenbank (*.xml)|*.xml"}
            };

            string prod = (sender as MenuItem).Tag.ToString();
            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.Filter = filters[prod];
            bool? result = dialog.ShowDialog();

            if (result == true)
            {
                _general = new Models.MainModel();
                _general.Catalog.Add(new Models.CatalogItem() { Name = Properties.Messages.main_def_cat });
                _general.IsOpenKnx = false;
                ImportHelper helper = new ImportHelper(dialog.FileName, Kaenx.Creator.Classes.Helper.BCUs);
                switch(prod)
                {
                    case "knxprod":
                        helper.StartZip(_general, Kaenx.Creator.Classes.Helper.DPTs);
                        SetButtons(true);
                        Changed("General");
                        break;

                    case "xml":
                        helper.StartXml(_general, Kaenx.Creator.Classes.Helper.DPTs);
                        SetButtons(true);
                        Changed("General");
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
            Models.CatalogItem item = new Models.CatalogItem() { Name = Properties.Messages.main_def_category, Parent = parent };
            foreach(Models.Language lang in _general.Application.Languages) {
                item.Text.Add(new Models.Translation(lang, ""));
            }
            parent.Items.Add(item);
        }

        private void ClickCatalogContextRemove(object sender, RoutedEventArgs e)
        {
            Models.CatalogItem item = (sender as MenuItem).DataContext as Models.CatalogItem;
            item.Parent.Items.Remove(item);
        }
        
        private void ClickCalcHeatmap(object sender, RoutedEventArgs e)
        {
            Models.Memory mem = (sender as Button).DataContext as Models.Memory;
            try {
                Kaenx.Creator.Classes.MemoryHelper.MemoryCalculation(General, mem); 
            } catch (Exception ex) {
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClickCheckHyperlink(object sender, RoutedEventArgs e)
        {
            TabsEdit.SelectedIndex = 3;
            Models.PublishAction action = (sender as System.Windows.Documents.Hyperlink).DataContext as Models.PublishAction;
            MainWindow.Instance.GoToItem(action.Item, action.Module);
        }
        #endregion

        private void TabChanged(object sender, SelectionChangedEventArgs e)
        {
            if(e.RemovedItems.Count > 0 && (e.RemovedItems[0] as TabItem) != null && (e.RemovedItems[0] as TabItem).Content is IFilterable mx1)
                mx1.FilterHide();

            if(e.AddedItems.Count > 0 && (e.AddedItems[0] as TabItem) != null &&(e.AddedItems[0] as TabItem).Content is IFilterable mx2)
                mx2.FilterShow();
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
            if(General.FileName.EndsWith(".knxprod"))
                General.FileName = General.FileName.Substring(0, General.FileName.LastIndexOf('.'));

            string fileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");

            switch(Properties.Settings.Default.Output)
            {
                case "exe":
                #if DEBUG
                    fileFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
                #else
                    fileFolder = Path.GetDirectoryName(Environment.ProcessPath);
                #endif
                    break;

                case "ae":
                    fileFolder = Path.GetDirectoryName(App.FilePath);
                    break;

                default:
                    MessageBox.Show("Einstellungen für Output nicht gültig");
                    return;
            }

            string filePath = System.IO.Path.Combine(fileFolder, General.FileName + ".knxprod");
            if(File.Exists(filePath))
            {
                if(MessageBoxResult.No == MessageBox.Show(string.Format(Properties.Messages.main_export_duplicate, General.FileName), Properties.Messages.main_export_title, MessageBoxButton.YesNo, MessageBoxImage.Question))
                    return;
            }

            PublishActions.Clear();
            await Task.Delay(1000);

            CheckHelper.CheckThis(General, PublishActions);

            if(PublishActions.Count(pa => pa.State == Models.PublishState.Fail) > 0)
            {
                PublishActions.Add(new Models.PublishAction() { Text = Properties.Messages.main_export_failed, State = Models.PublishState.Fail });
                return;
            }
            else
                PublishActions.Add(new Models.PublishAction() { Text = Properties.Messages.main_export_checked, State = Models.PublishState.Success });

            await Task.Delay(1000);

            PublishActions.Add(new Models.PublishAction() { Text = Properties.Messages.main_export_create, State = Models.PublishState.Info });

            await Task.Delay(1000);
            
            string headerPath = Path.Combine(Path.GetDirectoryName(filePath), "knxprod.h");
            Kaenx.Creator.Classes.ExportHelper helper = new Kaenx.Creator.Classes.ExportHelper(General, headerPath);
            
            try {
                bool success = helper.ExportEts(PublishActions, DevelopCheckBox.IsChecked == true);
                if(!success)
                {
                    MessageBox.Show(Properties.Messages.main_export_error, Properties.Messages.main_export_title);
                    return;
                }
            } catch(Exception ex) {
                MessageBox.Show(ex.Message, Properties.Messages.main_export_title, MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            
            await OpenKNX.Toolbox.Sign.SignHelper.CheckMaster(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Temp"), General.Application.NamespaceVersion);
            await helper.SignOutput(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", "Temp"), filePath, General.Application.NamespaceVersion);
            PublishActions.Add(new Models.PublishAction() { Text = Properties.Messages.main_export_success, State = Models.PublishState.Success } );
            PublishActions.Add(new Models.PublishAction() { Text = filePath, State = Models.PublishState.Success } );
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

        private void ChangeOutput(object sender, RoutedEventArgs e)
        {
            if(sender is MenuItem)
            {
                string tag = (sender as MenuItem).Tag.ToString();
                Properties.Settings.Default.Output = tag;
                Properties.Settings.Default.Save();
                CheckOutput();
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
        
        private void ClickResetBCU(object sender, RoutedEventArgs e)
        {
            if(MessageBoxResult.Yes == MessageBox.Show(Properties.Messages.reset_bcu, Properties.Messages.reset_bcu_title, MessageBoxButton.YesNo, MessageBoxImage.Question))
            {
                General.Info.Mask = null;
            }
        }

        private void ClickToggleDebug(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem)
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
                if(MessageBoxResult.Yes == MessageBox.Show(string.Format(Properties.Messages.update_new, response.vers), Properties.Messages.update_title, MessageBoxButton.YesNo, MessageBoxImage.Question))
                {
                    Process.Start(new ProcessStartInfo("https://github.com/OpenKNX/Kaenx-Creator/releases/latest") { UseShellExecute = true });
                }
            } else 
                MessageBox.Show(string.Format(Properties.Messages.update_uptodate, string.Join('.', System.Reflection.Assembly.GetEntryAssembly().GetName().Version.ToString().Split('.').Take(3)), Properties.Messages.update_title, MessageBoxButton.OK, MessageBoxImage.Information));
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
                MessageBox.Show(Properties.Messages.update_new, Properties.Messages.update_title, MessageBoxButton.OK, MessageBoxImage.Error);
                return (false, "");
            }
        }
    }
}
