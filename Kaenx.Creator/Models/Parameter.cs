using System;
using System.Collections.Generic;
using System.Linq;
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


        private SavePaths _savePath = SavePaths.Nowhere;
        public SavePaths SavePath
        {
            get { return _savePath; }
            set { 
                _savePath = value; 
                Changed("SavePath"); 
                if(_savePath == SavePaths.Property)
                    SaveObject = new Property() { Offset = 0, OffsetBit = 0 };
                else
                    SaveObject = null;
            }
        }



        private IParameterSavePath _saveObject;
        [JsonIgnore]
        public IParameterSavePath SaveObject
        {
            get {
                return _saveObject; 
            }
            set { _saveObject = value; Changed("SaveObject"); if(value == null) _memoryId = -1; }
        }

        [JsonIgnore]
        public int _memoryId;
        public int MemoryId
        {
            get { 
                if(SaveObject is Memory mem)
                    return mem?.UId ?? _memoryId; 
                return -1;
            }
            set { _memoryId = value; }
        }



        private Union _unionObject;
        [JsonIgnore]
        public Union UnionObject
        {
            get { return _unionObject; }
            set { _unionObject = value; Changed("UnionObject"); if(value == null) _unionId = -1; }
        }

        [JsonIgnore]
        public int _unionId;
        public int UnionId
        {
            get { return UnionObject?.UId ?? _unionId; }
            set { _unionId = value; }
        }



        private ParameterType _parameterTypeObject;
        [JsonIgnore]
        public ParameterType ParameterTypeObject
        {
            get { return _parameterTypeObject; }
            set { _parameterTypeObject = value; Changed("ParameterTypeObject"); if(value == null) _parameterType = -1; }
        }

        [JsonIgnore]
        public int _parameterType;
        public int ParameterType
        {
            get { return ParameterTypeObject?.UId ?? _parameterType; }
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
        public ObservableCollection<Translation> Suffix {get;set;} = new ObservableCollection<Translation>();

        private bool _transText = false;
        public bool TranslationText
        {
            get { return _transText; }
            set { _transText = value; Changed("TranslationText"); }
        }

        private bool _transSuffix = true;
        public bool TranslationSuffix
        {
            get { return _transSuffix; }
            set { _transSuffix = value; Changed("TranslationSuffix"); }
        }

        public ParamAccess Access { get; set; } = ParamAccess.ReadWrite;

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public Parameter Copy()
        {
            Parameter para = (Parameter)this.MemberwiseClone();
            
            /* overwrite old reference with deep copy of the Translation Objects*/
            para.Text = new ObservableCollection<Translation>();
            foreach (Translation translation in this.Text)
                para.Text.Add(new Translation(translation.Language, translation.Text));  
            para.Suffix = new ObservableCollection<Translation>();
            foreach (Translation translation in this.Suffix)
                para.Suffix.Add(new Translation(translation.Language, translation.Text));  
            
            return para;
        }
    }

    public enum ParamAccess
    {
        None,
        Read,
        ReadWrite
    }

    public enum SavePaths {
        Nowhere,
        Memory,
        Property
    }
}