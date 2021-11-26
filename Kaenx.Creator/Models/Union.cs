using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;

namespace Kaenx.Creator.Models
{
    public class Union : INotifyPropertyChanged
    {
        private int _uid = -1;
        public int UId
        {
            get { return _uid; }
            set { _uid = value; Changed("UId"); }
        }

        private string _name = "dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private int _sizeInBit = 0;
        public int SizeInBit
        {
            get { return _sizeInBit; }
            set { _sizeInBit = value; Changed("SizeInBit"); }
        }

        private int _offset = 0;
        public int Offset
        {
            get { return _offset; }
            set { _offset = value; Changed("Offset"); }
        }

        private int _offsetBit = 0;
        public int OffsetBit
        {
            get { return _offsetBit; }
            set { _offsetBit = value; Changed("OffsetBit"); }
        }



        
        private Memory _memoryObject;
        [JsonIgnore]
        public Memory MemoryObject
        {
            get { return _memoryObject; }
            set { _memoryObject = value; Changed("MemoryObject"); }
        }

        [JsonIgnore]
        public int _memoryId;
        public int MemoryId
        {
            get { return MemoryObject?.UId ?? -1; }
            set { _memoryId = value; }
        }



        private ParamSave _savePath = ParamSave.Nowhere;
        public ParamSave SavePath
        {
            get { return _savePath; }
            set { _savePath = value; Changed("SavePath"); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
