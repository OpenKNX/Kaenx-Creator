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

            int counter = 0;
            foreach(Language lang in vers.Languages)
            {
                DataGridTextColumn textColumn = new DataGridTextColumn(); 
                textColumn.Header = lang.Text; 
                textColumn.Binding = new Binding($"Text[{counter++}].Text"); 
                TranslationList.Columns.Add(textColumn); 
            }

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
            TranslationTab tab = new TranslationTab() { Name = vbase.Name };

            foreach(Parameter para in vbase.Parameters)
            {
                tab.Items.Add(new TranslationItem() { Name = para.Name + " (Text)", Group = "Parameters", Text = para.Text });
                tab.Items.Add(new TranslationItem() { Name = para.Name + " (Suffix)", Group = "Parameters", Text = para.Suffix });
            }

            if(!vbase.IsParameterRefAuto)
            {
                foreach(ParameterRef para in vbase.ParameterRefs)
                {
                    if(para.OverwriteText)
                        tab.Items.Add(new TranslationItem() { Name = para.Name + " (Text)", Group = "ParameterRefs", Text = para.Text });
                    if(para.OverwriteSuffix)
                        tab.Items.Add(new TranslationItem() { Name = para.Name + " (Suffix)", Group = "ParameterRefs", Text = para.Suffix });
                }
            }

            foreach(ComObject com in vbase.ComObjects)
            {
                tab.Items.Add(new TranslationItem() { Name = com.Name + " (Text)", Group = "ComObjects", Text = com.Text });
                tab.Items.Add(new TranslationItem() { Name = com.Name + " (Function)", Group = "ComObjects", Text = com.FunctionText });
            }

            if(!vbase.IsComObjectRefAuto)
            {
                foreach(ComObjectRef com in vbase.ComObjectRefs)
                {
                    if(com.OverwriteText)
                        tab.Items.Add(new TranslationItem() { Name = com.Name + " (Text)", Group = "ComObjectRefs", Text = com.Text });
                    if(com.OverwriteFunctionText)
                        tab.Items.Add(new TranslationItem() { Name = com.Name + " (Function)", Group = "ComObjectRefs", Text = com.FunctionText });
                }
            }

            return tab;
        }
    }
}
