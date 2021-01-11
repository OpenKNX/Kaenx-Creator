using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ParameterRef : INotifyPropertyChanged
    {
        private string _name = "Kurze Beschreibung";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private Parameter _parameterObject;
        [JsonIgnore]
        public Parameter ParameterObject
        {
            get { return _parameterObject; }
            set { _parameterObject = value; Changed("ParameterObject"); }
        }

        private string _parameter;
        public string Parameter
        {
            get { return ParameterObject?.Name; }
            set { _parameter = value; }
        }


        public ParamAccess Access { get; set; } = ParamAccess.Default;
        public string Value { get; set; } = "";
        public string Suffix { get; set; } = "";


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        public string GetParameter()
        {
            return _parameter;
        }


        //Only used for export
        [JsonIgnore]
        public string RefId { get; set; }
    }
}
