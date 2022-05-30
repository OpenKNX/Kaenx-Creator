using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;
using System.Linq;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynModule : IDynItems, INotifyPropertyChanged
    {
        [JsonIgnore]
        public IDynItems Parent { get; set; }

        private int _id = -1;
        public int Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); }
        }
        
        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private Module _moduleObject;
        [JsonIgnore]
        public Module ModuleObject
        {
            get { return _moduleObject; }
            set { 
                _moduleObject = value;
                Changed("ModuleObject"); 
                if(_moduleObject == null)
                {
                    Arguments.Clear();
                } else
                {
                    foreach(Argument arg in _moduleObject.Arguments)
                        if(!Arguments.Any(a => a._argId == arg.UId))
                            Arguments.Add(new DynModuleArg(arg));
                }
            }
        }

        [JsonIgnore]
        public int _module;
        public int ModuleUId
        {
            get { return ModuleObject?.UId ?? -1; }
            set { _module = value; }
        }

        public ObservableCollection<DynModuleArg> Arguments { get; set; } = new ObservableCollection<DynModuleArg>();

        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
