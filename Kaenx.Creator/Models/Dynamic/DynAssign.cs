using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynAssign : IDynItems, INotifyPropertyChanged
    {
        [JsonIgnore]
        public IDynItems Parent { get; set; }
        public bool IsExpanded { get; set; }

        public long uid { get; set; }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private ParameterRef _sourceObject;
        [JsonIgnore]
        public ParameterRef SourceObject
        {
            get { return _sourceObject; }
            set { _sourceObject = value; Changed("SourceObject"); }
        }

        [JsonIgnore]
        public int _sourceUId;
        public int SourceUId
        {
            get { return SourceObject?.UId ?? -1; }
            set { _sourceUId = value; }
        }

        private ParameterRef _targetObject;
        [JsonIgnore]
        public ParameterRef TargetObject
        {
            get { return _targetObject; }
            set { _targetObject = value; Changed("TargetObject"); }
        }

        [JsonIgnore]
        public int _targetUId;
        public int TargetUId
        {
            get { return TargetObject?.UId ?? -1; }
            set { _targetUId = value; }
        }

        private string _value;
        public string Value
        {
            get { return _value; }
            set { _value = value; Changed("Value"); }
        }

        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        
        public IDynItems Copy()
        {
            return (DynAssign)this.MemberwiseClone();
        }
    }
}
