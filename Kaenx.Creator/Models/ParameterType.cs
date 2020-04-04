using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ParameterType
    {
        public string Name { get; set; } = "Dummy PT";
        public ParameterTypes Type { get; set; } = ParameterTypes.Text;
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
