using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;

namespace Kaenx.Creator.Models
{
    public class Argument : INotifyPropertyChanged
    {   
        private int _uid = -1;
        public int UId
        {
            get { return _uid; }
            set { _uid = value; Changed("UId"); }
        }

        private long _id = -1;
        public long Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); }
        }
        
        private int _alloc = 0;
        public int Allocates
        {
            get { return _alloc; }
            set { _alloc = value; Changed("Allocates"); }
        }
        
        private string _name = "dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
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


