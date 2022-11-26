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

        
        private ParameterTypes _type = ParameterTypes.Text;
        public ParameterTypes Type
        {
            get { return _type; }
            set { 
                _type = value;
                UIHint = "";
                SizeInBit = 8;
                OtherValue = true;
                Min = "0";
                Max = "255";
                Increment = "1"; 
                Enums.Clear();
                BaggageObject = null;
                TranslateEnums = true;
                IsSizeManual = _type == ParameterTypes.Text;
                Changed("Type");
            }
        }


        private bool _isSizeManual = true;
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
        
        private bool _isNotUsed = false;
        [JsonIgnore]
        public bool IsNotUsed
        {
            get { return _isNotUsed; }
            set { _isNotUsed = value; Changed("IsNotUsed"); }
        }

        private bool _otherValue = true;
        public bool OtherValue
        {
            get { return _otherValue; }
            set { _otherValue = value; Changed("OtherValue"); }
        }

        private string _uihint = "None";
        public string UIHint
        {
            get { return _uihint; }
            set { 
                _uihint = value; 
                Changed("UIHint"); }
        }

        private Baggage _baggageObject;
        [JsonIgnore]
        public Baggage BaggageObject
        {
            get { return _baggageObject; }
            set { _baggageObject = value; Changed("BaggageObject"); }
        }
        [JsonIgnore]
        public int _baggageUId = -1;
        public int BaggageUId
        {
            get { return BaggageObject?.UId ?? -1; }
            set { _baggageUId = value; }
        }

        private string _min = "0";
        public string Min
        {
            get { return _min; }
            set { _min = value; Changed("Min"); }
        }
        private string _max = "255";
        public string Max
        {
            get { return _max; }
            set { _max = value; Changed("Max"); }
        }
        private string _increment = "1";
        public string Increment
        {
            get { return _increment; }
            set { _increment = value; Changed("Increment"); }
        }

        public ObservableCollection<ParameterTypeEnum> Enums {get;set;} = new ObservableCollection<ParameterTypeEnum>();

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public enum ParameterTypes {
        Color,
        Date,
        Enum,
        Float_DPT9,
        Float_IEEE_Single,
        Float_IEEE_Double,
        IpAddress,
        None,
        NumberUInt,
        NumberInt,
        Picture,
        RawData,
        Text
    }
}
