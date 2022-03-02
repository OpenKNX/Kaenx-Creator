using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ModelGeneral : INotifyPropertyChanged
    {
        public int ManufacturerId { get; set; } = 250;
        public string ProjectName { get; set; } = "Meine erste Applikation";


        public ObservableCollection<CatalogItem> Catalog { get; set; } = new ObservableCollection<CatalogItem>();
        public ObservableCollection<Device> Devices { get; set; } = new ObservableCollection<Device>();
        public ObservableCollection<Application> Applications { get; set; } = new ObservableCollection<Application>();
        public ObservableCollection<Hardware> Hardware { get; set; } = new ObservableCollection<Hardware>();
        public ObservableCollection<Language> Languages { get; set; } = new ObservableCollection<Language>();

        private string _defaultLang;
        public string DefaultLanguage
        {
            get { return _defaultLang; }
            set { _defaultLang = value; Changed("DefaultLanguage"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
