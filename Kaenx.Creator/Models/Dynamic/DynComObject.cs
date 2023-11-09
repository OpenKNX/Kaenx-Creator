using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynComObject : IDynItems, INotifyPropertyChanged
    {
        [JsonIgnore]
        public IDynItems Parent { get; set; }
        public bool IsExpanded { get; set; }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private ComObjectRef _comObjectRefObject;
        [JsonIgnore]
        public ComObjectRef ComObjectRefObject
        {
            get { return _comObjectRefObject; }
            set { _comObjectRefObject = value; Changed("ComObjectRefObject"); if(value == null) _comObjectRef = -1; }
        }

        [JsonIgnore]
        public int _comObjectRef;
        public int ComObjectRef
        {
            get { return ComObjectRefObject?.UId ?? _comObjectRef; }
            set { _comObjectRef = value; }
        }


        public ObservableCollection<IDynItems> Items { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        
        public IDynItems Copy()
        {
            return (DynComObject)this.MemberwiseClone();
        }
    }
}
