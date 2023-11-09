using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynModuleArg : INotifyPropertyChanged
    {

        public DynModuleArg(Argument arg)
        {
            Argument = arg;
        }

        private Argument _arg;
        [JsonIgnore]
        public Models.Argument Argument
        {
            get { return _arg; }
            set { _arg = value; if(Value == null) _argId = -1; }
        }

        [JsonIgnore]
        public int _argId = -1;
        public int ArgumentId {
            get { return Argument?.UId ?? _argId; }
            set { _argId = value; }
        }

        private string _value = "";
        public string Value
        {
            get { return _value; }
            set { _value = value; Changed("Value"); }
        }

        private bool _useAllocator = false;
        public bool UseAllocator
        {
            get { return _useAllocator; }
            set { _useAllocator = value; Changed("UseAllocator"); }
        }

        private Models.Allocator _alloc;
        [JsonIgnore]
        public Models.Allocator Allocator
        {
            get { return _alloc; }
            set { if(value == null) { _allocId = -1; return; } _alloc = value; Changed("Allocator"); }
        }

        [JsonIgnore]
        public int _allocId = -1;
        public int AllocatorId {
            get { return Allocator?.UId ?? _allocId; }
            set { _allocId = value; }
        }



        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
