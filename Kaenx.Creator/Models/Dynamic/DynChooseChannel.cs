using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynChooseChannel : IDynChoose, INotifyPropertyChanged
    {
        [JsonIgnore]
        public IDynItems Parent { get; set; }

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; Changed("IsExpanded"); }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private bool _isGlobal = false;
        public bool IsGlobal
        {
            get { return _isGlobal; }
            set {
                _isGlobal = value;
                Changed("IsGlobal");
                ParameterRefObject = null;
            }
        }

        private ParameterRef _parameterRefObject;
        [JsonIgnore]
        public ParameterRef ParameterRefObject
        {
            get { return _parameterRefObject; }
            set { _parameterRefObject = value; Changed("ParameterRefObject"); if(value == null) _parameterRef = -1; }
        }

        [JsonIgnore]
        public int _parameterRef;
        public int ParameterRef
        {
            get { return ParameterRefObject?.UId ?? _parameterRef; }
            set { _parameterRef = value; }
        }

        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        
        public IDynItems Copy()
        {
            DynChooseBlock dyn = (DynChooseBlock)this.MemberwiseClone();
            dyn.Items = new ObservableCollection<IDynItems>();
            foreach (IDynItems item in this.Items)
                dyn.Items.Add((IDynItems)item.Copy());
            return dyn;
        }
    }
}
