using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynRepeat : IDynItems, INotifyPropertyChanged
    {
        [JsonIgnore]
        public IDynItems Parent { get; set; }

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; Changed("IsExpanded"); }
        }

        private long _id = -1;
        public long Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); }
        }

        private long _count = 0;
        public long Count
        {
            get { return _count; }
            set { _count = value; Changed("Count"); }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private bool _useParameterRef = false;
        public bool UseParameterRef
        {
            get { return _useParameterRef; }
            set { _useParameterRef = value; Changed("UseParameterRef"); }
        }
        
        [JsonIgnore]
        public int _parameterUId = -1;
        public int ParameterUId
        {
            get { return ParameterRefObject?.UId ?? _parameterUId; }
            set { _parameterUId = value; }
        }
        
        private Models.ParameterRef _parameterRef;
        [JsonIgnore]
        public Models.ParameterRef ParameterRefObject
        {
            get { return _parameterRef; }
            set { _parameterRef = value; Changed("ParameterRefObject"); if(value == null) _parameterUId = -1; }
        }


        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public IDynItems Copy()
        {
            DynRepeat dyn = (DynRepeat)this.MemberwiseClone();
            dyn.Items = new ObservableCollection<IDynItems>();
            return dyn;
        }
    }
}