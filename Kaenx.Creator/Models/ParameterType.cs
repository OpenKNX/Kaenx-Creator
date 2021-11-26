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

        public string Name { get; set; } = "Dummy PT";
        public int Min { get; set; } = 0;
        public int Max { get; set; } = 255;
        public int SizeInBit { get; set; } = 8;
        public bool IsSizeAuto { get; set; } = false;
        public ParameterTypes Type { get; set; } = ParameterTypes.Text;

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
