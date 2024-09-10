using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Kaenx.Creator.Controls
{
    public partial class BaggageView : UserControl, INotifyPropertyChanged
    {
        public static readonly System.Windows.DependencyProperty GeneralProperty = System.Windows.DependencyProperty.Register("General", typeof(MainModel), typeof(BaggageView), new System.Windows.PropertyMetadata(null));
        public MainModel General {
            get { return (MainModel)GetValue(GeneralProperty); }
            set { SetValue(GeneralProperty, value); }
        }

        public BaggageView()
		{
            InitializeComponent();
        }

        private void ClickAdd(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = Properties.Messages.baggage_add_title;
            diag.Filter = Properties.Messages.baggage_change_filter + " (PNG, JPG)|*.png;*.jpg";
            if(diag.ShowDialog() == true)
            {
                Baggage bag = new Baggage();
                bag.UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(General.Baggages);
                bag.Data = AutoHelper.GetFileBytes(diag.FileName);
                bag.Extension = Path.GetExtension(diag.FileName).ToLower();
                
                General.Baggages.Add(bag);
            }
        }

        private void ClickDelete(object sender, System.Windows.RoutedEventArgs e)
        {   
            Baggage bag = BaggageList.SelectedItem as Baggage;
            List<ParameterType> types = new List<ParameterType>();

            if(types.Count > 0)
            {
                var result = MessageBox.Show(Properties.Messages.baggage_delete_error, Properties.Messages.baggage_delete_error_title, MessageBoxButton.YesNo);
                if(result == MessageBoxResult.No) return;
            }

            General.Baggages.Remove(bag);

            foreach(ParameterType type in types)
                type.BaggageObject = null;
        }

        private void ClickChangeFile(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = Properties.Messages.baggage_change_title;
            diag.Filter = Properties.Messages.baggage_change_filter + " (PNG, JPG)|*.png;*.jpg";
            if(diag.ShowDialog() == true)
            {
                Baggage bag = (sender as Button).DataContext as Baggage;
                bag.Data = AutoHelper.GetFileBytes(diag.FileName);
                bag.Extension = Path.GetExtension(diag.FileName).ToLower();
                System.Windows.MessageBox.Show(Properties.Messages.baggage_change_success, Properties.Messages.icon_change_title);
                BaggageList.SelectedItem = null;
                BaggageList.SelectedItem = bag;
            }
        }
        
        private void ClickImport(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = Properties.Messages.baggage_import_title;
            diag.Filter = Properties.Messages.baggage_import_filter + " (*.ae-baggage)|*.ae-baggage";
            if(diag.ShowDialog() == true)
            {   
                var x = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<Baggage>>(File.ReadAllText(diag.FileName));
                if(MessageBox.Show(Properties.Messages.baggage_import_prompt, "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    General.Baggages.Clear();
                foreach(Baggage bag in x)
                    General.Baggages.Add(bag);
            }
        }

        private void ClickExport(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveFileDialog diag = new SaveFileDialog();
            diag.FileName = General.ProjectName;
            diag.Title = Properties.Messages.baggage_export_title;
            diag.Filter = Properties.Messages.baggage_export_filter + " (*.ae-baggage)|*.ae-baggage";
            
            if(diag.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(diag.FileName, Newtonsoft.Json.JsonConvert.SerializeObject(General.Baggages));
            }
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
