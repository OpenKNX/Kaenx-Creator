using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Device
    {
        public string Name { get; set; } = "Dummy";
        public string Text { get; set; } = "Dummy";
        public string OrderNumber { get; set; } = "TA-00002.1";
        public string Description { get; set; } = "Dummy Einbautaster 2-fach";
        public bool IsRailMounted { get; set; } = true;
    }
}
