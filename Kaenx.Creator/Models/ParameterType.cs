using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace Kaenx.Creator.Models
{
    public class ParameterType
    {
        public string Name { get; set; } = "Dummy PT";
        public int Min { get; set; } = 0;
        public int Max { get; set; } = 255;
        public int SizeInBit { get; set; } = 8;
        public ParameterTypes Type { get; set; } = ParameterTypes.Text;

        public ObservableCollection<ParameterTypeEnum> Enums {get;set;} = new ObservableCollection<ParameterTypeEnum>();
    }

    public enum ParameterTypes {
        Text,
        Enum,
        NumberUInt,
        NumberInt,
        Float9,
        Picture,
        None,
        IpAdress
    }
}
