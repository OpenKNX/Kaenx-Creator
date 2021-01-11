using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;

namespace Kaenx.Creator.Models
{
    public class Parameter : INotifyPropertyChanged
    {
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

        private Memory _memoryObject;
        public Memory MemoryObject
        {
            get { return _memoryObject; }
            set { _memoryObject = value; Changed("MemoryObject"); }
        }

        private string _memory;
        public string Memory
        {
            get { return MemoryObject?.Name; }
            set { _memory = value; }
        }

        private ParameterType _parameterTypeObject;
        public ParameterType ParameterTypeObject
        {
            get { return _parameterTypeObject; }
            set { _parameterTypeObject = value; Changed("ParameterTypeObject"); }
        }

        private string _parameterType;
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

        private int _offset;
        public int Offset
        {
            get { return _offset; }
            set { _offset = value; Changed("Offset"); }
        }

        private int _offsetBit;
        public int OffsetBit
        {
            get { return _offsetBit; }
            set { _offsetBit = value; Changed("OffsetBit"); }
        }

        public string Suffix { get; set; }
        public ParamAccess Access { get; set; } = ParamAccess.Default;

        public string GetMemory()
        {
            return _memory;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public string GetParameterType()
        {
            return _parameterType;
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