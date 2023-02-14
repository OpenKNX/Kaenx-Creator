using Kaenx.Creator.Models.Dynamic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

namespace Kaenx.Creator.Models
{
    public class AppVersionModel : INotifyPropertyChanged
    {
        private string _name = "Dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); Changed("NameText"); }
        }

        private int _number = 0;
        public int Number
        {
            get { return _number; }
            set { _number = value; Changed("Number"); Changed("NameText"); }
        }
        
        public string NameText
        {
            get
            {
                int main = (int)Math.Floor((double)Number / 16);
                int sub = Number - (main * 16);
                return $"V {main}.{sub} {Name}  (/{Namespace})";
            }
        }

        public int Namespace { get; set; }


        public string Version { get; set; }


        private AppVersion _model;
        [JsonIgnore]
        public AppVersion Model { 
            get { return _model; }
            set { _model = value; Changed("Model"); }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}