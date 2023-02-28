using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Hardware : INotifyPropertyChanged
    {
        private string _name = "Hardware Name";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        public string SerialNumber { get; set; } = "1";
        public int Version { get; set; } = 1;
        public int BusCurrent { get; set; } = 10;
        public bool HasIndividualAddress { get; set; } = true;
        private bool _hasApp = true;
        public bool HasApplicationProgram
        {
            get { return _hasApp; }
            set { _hasApp = value; Changed("HasApplicationProgram"); }
        }
        public bool HasApplicationProgram2 { get; set; } = false;
        public bool IsPowerSupply { get; set; } = false;
        public bool IsCoppler { get; set; } = false;
        public bool IsIpEnabled { get; set; } = false;

        public ObservableCollection<Device> Devices { get; set; } = new ObservableCollection<Device>();
        [JsonIgnore]
        public ObservableCollection<Application> Apps { get; set; } = new ObservableCollection<Application>();

        [JsonIgnore]
        public string _appsString;
        public string AppsString {
            get {
                List<int> names = new List<int>();
                foreach(Application app in Apps)
                    names.Add(app.Number);
                return string.Join(",", names.ToArray());
            }
            set { _appsString = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
