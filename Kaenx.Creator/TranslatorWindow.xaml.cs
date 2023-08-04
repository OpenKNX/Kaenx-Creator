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
    public partial class TranslatorWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableCollection<TranslationTab> Tabs { get; set; } = new ObservableCollection<TranslationTab>();


        public TranslatorWindow(AppVersion vers)
        {
            InitializeComponent();
            Tabs.Add(ParseVersion(vers));

            GetSub(vers);

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

        private TranslationTab ParseVersion(IVersionBase vbase)
        {
            TranslationTab tab = new() { Name = vbase.Name };

            if(vbase is AppVersion vers)
            {
                foreach(ParameterType parameterType in vers.ParameterTypes.OrderBy(p => p.Name))
                {
                    if(parameterType.Type == ParameterTypes.Enum)
                    {
                        AddLanguage(parameterType.Enums[0].Text);
                        foreach(ParameterTypeEnum typeEnum in parameterType.Enums.OrderBy(p => p.Name))
                            tab.Items.Add(new TranslationItem() { Name = $"Wert {typeEnum.Value}", Group = "ParameterType Enums", SubGroup = parameterType.Name, Text = typeEnum.Text });
                    }
                }

                foreach(Message msg in vers.Messages.OrderBy(m => m.Name))
                    tab.Items.Add(new TranslationItem() { Name = msg.Name, Group = "Meldungen", Text = msg.Text });
            }

            if(vbase.Parameters.Count > 0) AddLanguage(vbase.Parameters[0].Text);
            foreach(Parameter para in vbase.Parameters.OrderBy(p => p.Name))
            {
                tab.Items.Add(new TranslationItem() { Name = "Text", Group = "Parameters", SubGroup = para.Name + $" ({para.UId})", Text = para.Text });
                tab.Items.Add(new TranslationItem() { Name = "Suffix", Group = "Parameters", SubGroup = para.Name + $" ({para.UId})", Text = para.Suffix });
            }

            if(!vbase.IsParameterRefAuto)
            {
                foreach(ParameterRef para in vbase.ParameterRefs.OrderBy(p => p.Name))
                {
                    if(para.OverwriteText)
                        tab.Items.Add(new TranslationItem() { Name = "Text", Group = "ParameterRefs", SubGroup = para.Name + $" ({para.UId})", Text = para.Text });
                    if(para.OverwriteSuffix)
                        tab.Items.Add(new TranslationItem() { Name = "Suffix", Group = "ParameterRefs", SubGroup = para.Name + $" ({para.UId})", Text = para.Suffix });
                }
            }

            if(vbase.ComObjects.Count > 0) AddLanguage(vbase.ComObjects[0].Text);
            foreach(ComObject com in vbase.ComObjects.OrderBy(c => c.Name))
            {
                tab.Items.Add(new TranslationItem() { Name = "Text", Group = "ComObjects", SubGroup = com.Name + $" ({com.UId})", Text = com.Text });
                tab.Items.Add(new TranslationItem() { Name = "FunctionText", Group = "ComObjects", SubGroup = com.Name + $" ({com.UId})", Text = com.FunctionText });
            }

            if(!vbase.IsComObjectRefAuto)
            {
                foreach(ComObjectRef com in vbase.ComObjectRefs.OrderBy(c => c.Name))
                {
                    if(com.OverwriteText)
                        tab.Items.Add(new TranslationItem() { Name = "Text", Group = "ComObjectRefs", SubGroup = com.Name + $" ({com.UId})", Text = com.Text });
                    if(com.OverwriteFunctionText)
                        tab.Items.Add(new TranslationItem() { Name = "FunctionText", Group = "ComObjectRefs", SubGroup = com.Name + $" ({com.UId})", Text = com.FunctionText });
                }
            }

            return tab;
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
