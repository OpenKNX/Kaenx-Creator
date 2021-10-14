using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ModelGeneral
    {
        public int ManufacturerId { get; set; } = -1;
        public string ProjectName { get; set; } = "Meine erste Applikation";


        public ObservableCollection<CatalogItem> Catalog { get; set; } = new ObservableCollection<CatalogItem>();
        public ObservableCollection<Device> Devices { get; set; } = new ObservableCollection<Device>();
        public ObservableCollection<Application> Applications { get; set; } = new ObservableCollection<Application>();
        public ObservableCollection<Hardware> Hardware { get; set; } = new ObservableCollection<Hardware>();
    }
}
