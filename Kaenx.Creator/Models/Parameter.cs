using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Kaenx.Creator.Models
{
    public class Parameter : INotifyPropertyChanged
    {   
        private int _uid = -1;
        public int UId
        {
            get { return _uid; }
            set { _uid = value; Changed("UId"); }
        }

        private int _id = -1;
        public int Id
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


        private bool _isInMemory = true;
        public bool IsInMemory
        {
            get { return _isInMemory; }
            set { _isInMemory = value; Changed("IsInMemory"); }
        }

        //TODO use UId from Memory Object
        private Memory _memoryObject;
        [JsonIgnore]
        public Memory MemoryObject
        {
            get { return _memoryObject; }
            set { _memoryObject = value; Changed("MemoryObject"); }
        }

        [JsonIgnore]
        public string _memory;
        public string Memory
        {
            get { return MemoryObject?.Name; }
            set { _memory = value; }
        }

        private ParameterType _parameterTypeObject;
        [JsonIgnore]
        public ParameterType ParameterTypeObject
        {
            get { return _parameterTypeObject; }
            set { _parameterTypeObject = value; Changed("ParameterTypeObject"); }
        }

        [JsonIgnore]
        public string _parameterType;
        public string ParameterType
        {
            get { return ParameterTypeObject?.Name; }
            set { _parameterType = value; }
        }

        public string Text { get; set; } = "Dummy";
        public string Value { get; set; } = "1";

        private bool _isOffsetAuto = false;
        public bool IsOffsetAuto
        {
            get { return _isOffsetAuto; }
            set { _isOffsetAuto = value; Changed("IsOffsetAuto"); }
        }

        private int _offset = -1;
        public int Offset
        {
            get { return _offset; }
            set { _offset = value; Changed("Offset"); }
        }

        private int _offsetBit = -1;
        public int OffsetBit
        {
            get { return _offsetBit; }
            set { _offsetBit = value; Changed("OffsetBit"); }
        }

        public string Suffix { get; set; }
        public ParamAccess Access { get; set; } = ParamAccess.Default;

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

    }

    public enum ParamAccess
    {
        Default,
        None,
        Read,
        ReadWrite
    }
}