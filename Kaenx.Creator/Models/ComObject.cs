using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ComObject : INotifyPropertyChanged
    {
        private string _name = "Kommunikationsobjekt";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
