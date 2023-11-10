using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class MainModel : INotifyPropertyChanged
    {
        public string ProjectName { get; set; } = "Meine erste Applikation";

        private string _guid = "";
        public string Guid
        {
            get { return _guid; }
            set { _guid = value; Changed("Guid"); }
        }

        private string _fileName = "output";
        public string FileName
        {
            get { return _fileName; }
            set { _fileName = value; Changed("FileName"); }
        }


        public string GetGuid()
        {
            return Guid.Substring(Guid.Length - 6, 6);
        }


        private Info _info = new Info();
        public Info Info
        {
            get { return _info; }
            set { _info = value; Changed("Info"); }
        }

        private AppVersion _app = null;
        public AppVersion Application
        {
            get {
                if(_app == null)
                {
                    _app = new AppVersion();
                } 
                return _app; 
            }
            set { _app = value; Changed("Application"); }
        }

        public ObservableCollection<CatalogItem> Catalog { get; set; } = new ObservableCollection<CatalogItem>();
        public ObservableCollection<Language> Languages { get; set; } = new ObservableCollection<Language>();
        public ObservableCollection<Baggage> Baggages { get; set; } = new ObservableCollection<Baggage>();
        public ObservableCollection<Icon> Icons { get; set; } = new ObservableCollection<Icon>();

        public int ImportVersion { get; set; }

        private int _manuId = 0xAF;
        public int ManufacturerId
        {
            get { return _manuId; }
            set { _manuId = value; Changed("ManufacturerId"); }
        }

        private bool _isOpenKnx = true;
        public bool IsOpenKnx
        {
            get { return _isOpenKnx; }
            set { 
                _isOpenKnx = value; 
                Changed("IsOpenKnx"); 
                //TODO delete all OpenKnxModules
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
