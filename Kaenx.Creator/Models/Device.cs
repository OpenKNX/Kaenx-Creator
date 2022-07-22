using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Device
    {
        public string Name { get; set; } = "Dummy";
        public ObservableCollection<Translation> Text {get;set;} = new ObservableCollection<Translation>();
        public ObservableCollection<Translation> Description {get;set;} = new ObservableCollection<Translation>();
        public string OrderNumber { get; set; } = "TA-00002.1";
        public bool IsRailMounted { get; set; } = true;
    }
}
