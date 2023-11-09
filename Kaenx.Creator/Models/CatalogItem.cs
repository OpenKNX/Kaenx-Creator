using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class CatalogItem
    {
        public string Name { get; set; }
        public string Number { get; set; }

        public bool IsSection { get; set; } = true;
        [JsonIgnore]
        public CatalogItem Parent { get; set; }

        public ObservableCollection<Translation> Text {get;set;} = new ObservableCollection<Translation>();
        public ObservableCollection<CatalogItem> Items { get; set; } = new ObservableCollection<CatalogItem>();


        //todo remove hardware
        [JsonIgnore]
        public Hardware Hardware { get; set; }

        [JsonIgnore]
        public string _hardwareName;
        public string HardwareName
        {
            get { return Hardware?.Name; }
            set { _hardwareName = value; }
        }
    }
}
