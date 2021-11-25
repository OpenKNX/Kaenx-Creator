using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Memory : INotifyPropertyChanged
    {
        //TODO add UId
        
        private string _name = "dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private int _address = 0;
        public int Address
        {
            get { return _address; }
            set { _address = value; Changed("Address"); }
        }

        private int _size = 0;
        public int Size
        {
            get { return _size; }
            set { _size = value; Changed("Size"); }
        }

        private int _offset = 0;
        public int Offset
        {
            get { return _offset; }
            set { _offset = value; Changed("Offset"); }
        }

        private bool _isPAutoPara = true;
        public bool IsAutoPara
        {
            get { return _isPAutoPara; }
            set { _isPAutoPara = value; Changed("IsAutoPara"); }
        }

        private bool _isAutoSize = true;
        public bool IsAutoSize
        {
            get { return _isAutoSize; }
            set { _isAutoSize = value; Changed("IsAutoSize"); }
        }

        private MemoryTypes _type = MemoryTypes.Absolute;
        public MemoryTypes Type
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
}
