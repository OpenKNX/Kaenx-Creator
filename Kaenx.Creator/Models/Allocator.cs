using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Allocator : INotifyPropertyChanged
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

        private string _name = "dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private long _start = 0;
        public long Start
        {
            get { return _start; }
            set { _start = value; Changed("Start"); }
        }

        private long _max = 0;
        public long Max
        {
            get { return _max; }
            set { _max = value; Changed("Max"); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
