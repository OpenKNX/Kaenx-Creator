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
        public static readonly System.Windows.DependencyProperty GeneralProperty = System.Windows.DependencyProperty.Register("General", typeof(ModelGeneral), typeof(BaggageView), new System.Windows.PropertyMetadata(null));
        public ModelGeneral General {
            get { return (ModelGeneral)GetValue(GeneralProperty); }
            set { SetValue(GeneralProperty, value); }
        }

        public BaggageView()
		{
            InitializeComponent();
        }

        private void ClickAdd(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = "Baggage hinzufügen";
            diag.Filter = "Bilder (PNG, JPG)|*.png;*.jpg";
            if(diag.ShowDialog() == true)
            {
                Baggage bag = new Baggage();
                bag.UId = AutoHelper.GetNextFreeUId(General.Baggages);
                bag.Data = AutoHelper.GetFileBytes(diag.FileName);
                bag.Extension = Path.GetExtension(diag.FileName).ToLower();
                
                General.Baggages.Add(bag);
            }
        }

        private void ClickDelete(object sender, System.Windows.RoutedEventArgs e)
        {   
            Baggage bag = BaggageList.SelectedItem as Baggage;
            List<ParameterType> types = new List<ParameterType>();

            /*foreach(Models.Application app in General.Applications)
                foreach(AppVersion vers in app.Versions)
                    foreach(ParameterType type in vers.ParameterTypes)
                        if(type.Type == ParameterTypes.Picture && type.BaggageObject == bag)
                            types.Add(type);
*/
//TODO implement
            if(types.Count > 0)
            {
                var result = MessageBox.Show("Der Anhang wird von " + types.Count + " ParameterTypes verwendet.\r\nTrotzdem löschen?", "Anhang löschen", MessageBoxButton.YesNo);
                if(result == MessageBoxResult.No) return;
            }

            General.Baggages.Remove(bag);

            foreach(ParameterType type in types)
                type.BaggageObject = null;
        }

        private void ClickChangeFile(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = "Baggage ändern";
            diag.Filter = "Bilder (PNG, JPG)|*.png;*.jpg";
            if(diag.ShowDialog() == true)
            {
                Baggage bag = (sender as Button).DataContext as Baggage;
                bag.Data = AutoHelper.GetFileBytes(diag.FileName);
                bag.Extension = Path.GetExtension(diag.FileName).ToLower();
                bag.TimeStamp = DateTime.Now;
                System.Windows.MessageBox.Show("Datei wurde erfolgreich geändert.");
                BaggageList.SelectedItem = null;
                BaggageList.SelectedItem = bag;
            }
        }
        
        private void ClickImport(object sender, System.Windows.RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = "Baggage importieren";
            diag.Filter = "Baggage Datei (*.ae-baggage)|*.ae-baggage";
            if(diag.ShowDialog() == true)
            {   
                var x = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<Baggage>>(File.ReadAllText(diag.FileName));
                if(MessageBox.Show("Vorhandene Anhänge löschen?", "", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
                    General.Baggages.Clear();
                foreach(Baggage bag in x)
                    General.Baggages.Add(bag);
            }
        }

        private void ClickExport(object sender, System.Windows.RoutedEventArgs e)
        {
            SaveFileDialog diag = new SaveFileDialog();
            diag.FileName = General.ProjectName;
            diag.Title = "Baggage exportieren";
            diag.Filter = "Baggage Datei (*.ae-baggage)|*.ae-baggage";
            
            if(diag.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(diag.FileName, Newtonsoft.Json.JsonConvert.SerializeObject(General.Baggages));
            }
        }
        
        private void Failed(object sender, System.Windows.ExceptionRoutedEventArgs e)
        {

        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
