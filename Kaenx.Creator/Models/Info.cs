using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Info : INotifyPropertyChanged
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

        public ObservableCollection<Translation> Text {get;set;} = new ObservableCollection<Translation>();
        public ObservableCollection<Translation> Description {get;set;} = new ObservableCollection<Translation>();
        public string OrderNumber { get; set; } = "TA-00002.1";
        public bool IsRailMounted { get; set; } = true;

        private int _number = 0;
        public int AppNumber
        {
            get { return _number; }
            set { _number = value; Changed("AppNumber"); }
        }

        private MaskVersion _mask = null;
        [JsonIgnore]
        public MaskVersion Mask
        {
            get { return _mask; }
            set { _mask = value; Changed("Mask"); }
        }

        [JsonIgnore]
        public string _maskId;
        public string MaskId
        {
            get { return _mask?.Id ?? _maskId; }
            set { _maskId = value; }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
