using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ModelGeneral
    {
        public int ManufacturerId { get; set; } = 250;
        public int AppNumber { get; set; } = 0;
        public string AppName { get; set; } = "Meine erste Applikation";

        public ObservableCollection<Device> Devices { get; set; } = new ObservableCollection<Device>();
        public ObservableCollection<Application> Applications { get; set; } = new ObservableCollection<Application>();
    }
}
