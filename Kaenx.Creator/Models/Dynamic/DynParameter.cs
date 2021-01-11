using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynParameter : IDynItems, INotifyPropertyChanged
    {
        private string _name = "Parameter";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private ParameterRef _parameterRefObject;
        public ParameterRef ParameterRefObject
        {
            get { return _parameterRefObject; }
            set { _parameterRefObject = value; Changed("ParameterRefObject"); }
        }

        private string _parameter;
        public string ParameterRef
        {
            get { return ParameterRefObject?.Name; }
            set { _parameter = value; }
        }



        public ObservableCollection<IDynItems> Items { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string GetParameter()
        {
            return _parameter;
        }
    }
}
