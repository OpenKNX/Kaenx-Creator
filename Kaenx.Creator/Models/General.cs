using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ModelGeneral : INotifyPropertyChanged
    {
        public string ProjectName { get; set; } = "Meine erste Applikation";
        public string Guid { get; set; }

        public string GetGuid()
        {
            return Guid.Substring(Guid.Length - 6, 6);
        }

        public ObservableCollection<CatalogItem> Catalog { get; set; } = new ObservableCollection<CatalogItem>();
        public ObservableCollection<Application> Applications { get; set; } = new ObservableCollection<Application>();
        public ObservableCollection<Hardware> Hardware { get; set; } = new ObservableCollection<Hardware>();
        public ObservableCollection<Language> Languages { get; set; } = new ObservableCollection<Language>();
        public ObservableCollection<Baggage> Baggages { get; set; } = new ObservableCollection<Baggage>();
        public ObservableCollection<Icon> Icons { get; set; } = new ObservableCollection<Icon>();

        public int ImportVersion { get; set; }

        private int _manuId = 250;
        public int ManufacturerId
        {
            get { return _manuId; }
            set { _manuId = value; Changed("ManufacturerId"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
