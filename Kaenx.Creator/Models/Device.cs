using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Device
    {
        public string Alias { get; set; } = "Mein Gerät R1";
        public string Name { get; set; } = "Mein Gerät";
        public string OrderNumber { get; set; } = "GE-01/3.1";
        public string Description { get; set; } = "Einbautaster 2-fach";
        public int BusCurrent { get; set; } = 10;
        public bool IsRailMounted { get; set; } = true;
        public bool HasIndividualAddress { get; set; } = true;
        public bool HasApplicationProgramm { get; set; } = true;

        public Application App { get; set; }
    }
}
