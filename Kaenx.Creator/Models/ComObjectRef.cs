using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ComObjectRef : INotifyPropertyChanged
    {
        private string _name = "Kurze Beschreibung";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private ComObject _comObjectObject;
        [JsonIgnore]
        public ComObject ComObjectObject
        {
            get { return _comObjectObject; }
            set { _comObjectObject = value; Changed("ComObjectObject"); }
        }

        [JsonIgnore]
        public string _comObject;
        public string ComObject
        {
            get { return ComObjectObject?.Name; }
            set { _comObject = value; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        //Only used for export
        [JsonIgnore]
        public string RefId { get; set; }
    }
}
