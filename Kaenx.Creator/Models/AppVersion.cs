using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class AppVersion : INotifyPropertyChanged
    {

        public ObservableCollection<ParameterType> ParameterTypes { get; set; } = new ObservableCollection<ParameterType>();
        public ObservableCollection<Parameter> Parameters { get; set; } = new ObservableCollection<Parameter>();
        public ObservableCollection<ParameterRef> ParameterRefs { get; set; } = new ObservableCollection<ParameterRef>();
        public ObservableCollection<ComObject> ComObjects { get; set; } = new ObservableCollection<ComObject>();
        public ObservableCollection<Memory> Memories { get; set; } = new ObservableCollection<Memory>();
        public List<DynamicMain> Dynamics { get; set; } = new List<DynamicMain>();


        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("NameText"); }
        }

        public string NameText
        {
            get
            {
                int main = (int)Math.Floor((double)Number / 16);
                int sub = Number - (main * 16);
                return "V " + main + "." + sub + " " + Name;
            }
        }

        private int _number = 16;
        public int Number
        {
            get { return _number; }
            set { _number = value; Changed("Number"); Changed("NameText"); }
        }


        private bool _isAutoPR = true;
        public bool IsParameterRefAuto
        {
            get { return _isAutoPR; }
            set { _isAutoPR = value; Changed("IsParameterRefAuto"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
