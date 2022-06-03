using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Kaenx.Creator.Models
{
    public class ParameterType : INotifyPropertyChanged
    {
        private int _uid = -1;
        public int UId
        {
            get { return _uid; }
            set { _uid = value; Changed("UId"); }
        }
        
        private string _name = "Dummy PT";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private bool _isSizeManual = false;
        public bool IsSizeManual
        {
            get { return _isSizeManual; }
            set { _isSizeManual = value; Changed("IsSizeManual"); }
        }

        private bool _translateEnums = false;
        public bool TranslateEnums
        {
            get { return _translateEnums; }
            set { _translateEnums = value; Changed("TranslateEnums"); }
        }

        private int _sizeInBit = 8;
        public int SizeInBit
        {
            get { return _sizeInBit; }
            set { _sizeInBit = value; Changed("SizeInBit"); }
        }
        
        private ParameterTypes _type = ParameterTypes.Text;
        public ParameterTypes Type
        {
            get { return _type; }
            set { _type = value; Changed("Type"); }
        }

        private bool _isNotUsed = false;
        [JsonIgnore]
        public bool IsNotUsed
        {
            get { return _isNotUsed; }
            set { _isNotUsed = value; Changed("IsNotUsed"); }
        }

        public string UIHint { get; set; } = "None";

        public double Min { get; set; } = 0;
        public double Max { get; set; } = 255;
        public double Increment { get; set; } = 1.0;

        public ObservableCollection<ParameterTypeEnum> Enums {get;set;} = new ObservableCollection<ParameterTypeEnum>();

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum ParameterTypes {
        Text,
        Enum,
        NumberUInt,
        NumberInt,
        Float_DPT9,
        Float_IEEE_Single,
        Float_IEEE_Double,
        Picture,
        None,
        IpAddress
    }
}
