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


        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();

        [JsonIgnore]
        public Models.Argument Argument { get; set; }

        [JsonIgnore]
        public int _argId = -1;
        public int ArgumentId {
            get { return Argument?.UId ?? -1; }
            set { _argId = value; }
        }

        private string _value = "";
        public string Value
        {
            get { return _value; }
            set { _value = value; Changed("Value"); }
        }

        private ArgumentTypes _type = ArgumentTypes.Numeric;
        public ArgumentTypes Type
        {
            get { return _type; }
            set { _type = value; Changed("Type"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum ArgumentTypes{
        Text,
        Numeric
    }
}
