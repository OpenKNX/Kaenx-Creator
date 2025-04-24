using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
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
    public partial class TranslatorWindow : Window
    {
        public ObservableCollection<TranslationTab> Tabs { get; set; } = new ObservableCollection<TranslationTab>();

        private MainModel _gen;

        public TranslatorWindow(MainModel gen)
        {
            InitializeComponent();
            _gen = gen;
            Tabs.Add(ParseVersion(gen.Application));
            
            GetSub(gen.Application);

            this.DataContext = this;
            TabList.SelectedIndex = 0;
        }

        private void ModuleChanged(object sender, SelectionChangedEventArgs e)
        {
            ICollectionView cvTasks = CollectionViewSource.GetDefaultView(TranslationList.ItemsSource);
            if (cvTasks != null && cvTasks.CanGroup == true)
            {
                cvTasks.GroupDescriptions.Clear();
                cvTasks.GroupDescriptions.Add(new PropertyGroupDescription("Group"));
                cvTasks.GroupDescriptions.Add(new PropertyGroupDescription("SubGroup"));
            }
        }

        private void GetSub(IVersionBase vbase)
        {
            foreach(IVersionBase subbase in vbase.Modules)
            {
                Tabs.Add(ParseVersion(subbase));
                GetSub(subbase);
            }
        }

        private void GetSubCatalogItem(TranslationTab tab, CatalogItem item)
        {
            foreach(CatalogItem subitem in item.Items)
            {
                if(!subitem.IsSection)
                    continue;
                tab.Items.Add(new TranslationItem(-5, "Text", "Katalog", item.Name, subitem.Text));
                GetSubCatalogItem(tab, subitem);
            }
        }

        private TranslationTab ParseVersion(IVersionBase vbase)
        {
            TranslationTab tab = new() { Name = vbase.Name };

            if(vbase is AppVersion vers)
            {
                AddLanguage(_gen.Application.Text);
                tab.Items.Add(new TranslationItem(-5, "Applikation", "Infos", null, _gen.Application.Text));
                tab.Items.Add(new TranslationItem(-5, "Text", "Infos", null, _gen.Info.Text));
                tab.Items.Add(new TranslationItem(-5, "Beschreibung", "Infos", null, _gen.Info.Description));
            
                GetSubCatalogItem(tab, _gen.Catalog[0]);

                foreach(ParameterType parameterType in vers.ParameterTypes.OrderBy(p => p.Name))
                {
                    if(parameterType.Type == ParameterTypes.Enum)
                    {
                        AddLanguage(parameterType.Enums[0].Text);
                        foreach(ParameterTypeEnum typeEnum in parameterType.Enums.OrderBy(p => p.Value))
                            tab.Items.Add(new TranslationItem(parameterType.UId, $"Wert {typeEnum.Value}", "ParameterType Enums", parameterType.Name, typeEnum.Text));
                    }
                }

                foreach(Message msg in vers.Messages.OrderBy(m => m.Name))
                    tab.Items.Add(new TranslationItem(msg.UId, msg.Name, "Meldungen", null, msg.Text));

                foreach(Helptext ht in vers.Helptexts.OrderBy(h => h.Name))
                {
                    string subGroup = "Allgemein";

                    if(ht.Name.Contains("-"))
                        subGroup = ht.Name.Substring(0, ht.Name.IndexOf("-"));
                        
                    tab.Items.Add(new TranslationItem(ht.UId, ht.Name, "Hilfetext", subGroup, ht.Text));
                }
            } else {
                tab.UId = ((Module)vbase).UId;
            }

            if(vbase.Parameters.Count > 0) AddLanguage(vbase.Parameters[0].Text);
            foreach(Parameter para in vbase.Parameters.OrderBy(p => p.Name))
            {
                tab.Items.Add(new TranslationItem(para.UId, "Text", "Parameters", para.Name + $" ({para.UId})", para.Text));
                tab.Items.Add(new TranslationItem(para.UId, "Suffix", "Parameters", para.Name + $" ({para.UId})", para.Suffix));
            }

            if(!vbase.IsParameterRefAuto)
            {
                foreach(ParameterRef para in vbase.ParameterRefs.OrderBy(p => p.Name))
                {
                    if(para.OverwriteText)
                        tab.Items.Add(new TranslationItem(para.UId, "Text", "ParameterRefs", para.Name + $" ({para.UId})", para.Text));
                    if(para.OverwriteSuffix)
                        tab.Items.Add(new TranslationItem(para.UId, "Suffix", "ParameterRefs", para.Name + $" ({para.UId})", para.Suffix));
                }
            }

            if(vbase.ComObjects.Count > 0) AddLanguage(vbase.ComObjects[0].Text);
            foreach(ComObject com in vbase.ComObjects.OrderBy(c => c.Name))
            {
                tab.Items.Add(new TranslationItem(com.UId, "Text", "ComObjects", com.Name + $" ({com.UId})", com.Text));
                tab.Items.Add(new TranslationItem(com.UId, "FunctionText", "ComObjects", com.Name + $" ({com.UId})", com.FunctionText));
            }

            if(!vbase.IsComObjectRefAuto)
            {
                foreach(ComObjectRef com in vbase.ComObjectRefs.OrderBy(c => c.Name))
                {
                    if(com.OverwriteText)
                        tab.Items.Add(new TranslationItem(com.UId, "Text", "ComObjectRefs", com.Name + $" ({com.UId})", com.Text));
                    if(com.OverwriteFunctionText)
                        tab.Items.Add(new TranslationItem(com.UId, "FunctionText", "ComObjectRefs", com.Name + $" ({com.UId})", com.FunctionText));
                }
            }

            return tab;
        }
    
        private void DoExport(object sender, RoutedEventArgs e)
        {
            List<TranslationExport> export = new List<TranslationExport>();
            foreach(TranslationTab tab in Tabs)
            {
                foreach(TranslationItem item in tab.Items)
                {
                    export.Add(new TranslationExport() { Id = item.Id, Tab = tab.UId, Text = item.Text });
                }
            }

            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = "Export.json"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string json = JsonConvert.SerializeObject(export, Formatting.Indented);
                File.WriteAllText(saveFileDialog.FileName, json);
            }
        }

        private void DoImport(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = ".json",
                AddExtension = true,
                FileName = "Export.json"
            };

            Dictionary<string, bool> importLanguages = new Dictionary<string, bool>();
            Dictionary<int, TranslationTab> tabitems = new Dictionary<int, TranslationTab>();
            foreach(TranslationTab tab in Tabs)
                tabitems.Add(tab.UId, tab);

            if (openFileDialog.ShowDialog() == true)
            {
                string json = File.ReadAllText(openFileDialog.FileName);
                List<TranslationExport> export = JsonConvert.DeserializeObject<List<TranslationExport>>(json);

                foreach(Translation trans in export[0].Text)
                {
                    if(_gen.Application.Languages.Any(l => l.CultureCode == trans.Language.CultureCode))
                    {
                        bool doimport = MessageBox.Show($"Soll die Sprache '{trans.Language.Text}' importiert werden?", "Import", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
                        importLanguages.Add(trans.Language.CultureCode, doimport);
                        continue;
                    }
                    MessageBox.Show($"Die Sprache '{trans.Language.Text}' ist nicht im Projekt und wird ignoriert.", "Import");
                }

                int langCounter = importLanguages.Count(l => l.Value);
                if(langCounter == 0)
                {
                    MessageBox.Show("Es wurde keine Sprache zum Import ausgewählt.", "Import");
                    return;
                }

                foreach(TranslationTab tab in Tabs)
                {
                    foreach(TranslationItem item in tab.Items)
                    {
                        TranslationExport titem = export.FirstOrDefault(ex => ex.Id == item.Id && ex.Tab == tab.UId);
                        if(titem == null) continue;
                        foreach(Translation trans in titem.Text)
                        {
                            if(!importLanguages[trans.Language.CultureCode]) continue;
                            Translation x = item.Text.FirstOrDefault(t => t.Language.CultureCode == trans.Language.CultureCode);
                            if(x == null) continue;
                            x.Text = trans.Text;
                        }
                    }
                }
            }
        }


        bool langAdded = false;
        private void AddLanguage(ObservableCollection<Translation> text)
        {
            if(langAdded) return;
            langAdded = true;

            int counter = 0;
            foreach(Translation trans in text)
            {
                DataGridTextColumn textColumn = new DataGridTextColumn(); 
                textColumn.Header = trans.Language.Text; 
                textColumn.Binding = new Binding($"Text[{counter++}].Text"); 
                TranslationList.Columns.Add(textColumn); 
            }
        }
    }
}
