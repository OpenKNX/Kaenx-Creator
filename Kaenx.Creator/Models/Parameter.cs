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


        private ParamSave _savePath = ParamSave.Nowhere;
        public ParamSave SavePath
        {
            get { return _savePath; }
            set { _savePath = value; Changed("SavePath"); }
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



        private Union _unionObject;
        [JsonIgnore]
        public Union UnionObject
        {
            get { return _unionObject; }
            set { _unionObject = value; Changed("UnionObject"); }
        }

        [JsonIgnore]
        public int _unionId;
        public int UnionId
        {
            get { return UnionObject?.UId ?? -1; }
            set { _unionId = value; }
        }



        private ParameterType _parameterTypeObject;
        [JsonIgnore]
        public ParameterType ParameterTypeObject
        {
            get { return _parameterTypeObject; }
            set { _parameterTypeObject = value; Changed("ParameterTypeObject"); }
        }

        [JsonIgnore]
        public int _parameterType;
        public int ParameterType
        {
            get { return ParameterTypeObject?.UId ?? -1; }
            set { _parameterType = value; }
        }



        public string Value { get; set; } = "1";

        private bool _inUnion = false;
        public bool IsInUnion
        {
            get { return _inUnion; }
            set { _inUnion = value; Changed("IsInUnion"); }
        }

        private bool _unionDefault = false;
        public bool IsUnionDefault
        {
            get { return _unionDefault; }
            set { _unionDefault = value; Changed("IsUnionDefault"); }
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

        private bool _isNotUsed = false;
        [JsonIgnore]
        public bool IsNotUsed
        {
            get { return _isNotUsed; }
            set { _isNotUsed = value; Changed("IsNotUsed"); }
        }

        public ObservableCollection<Translation> Text {get;set;} = new ObservableCollection<Translation>();

        private bool _transText = false;
        public bool TranslationText
        {
            get { return _transText; }
            set { _transText = value; Changed("TranslationText"); }
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

    //TODO change name so union can also use it
    public enum ParamSave {
        Nowhere,
        Memory,
        Property
    }
}