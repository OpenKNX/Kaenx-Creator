using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;

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

        public int Min { get; set; } = 0;
        public int Max { get; set; } = 255;

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
        Float9,
        Picture,
        None,
        IpAddress
    }
}
