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
        public string VisibleDescription { get; set; }

        public bool IsSection { get; set; } = true;
        [JsonIgnore]
        public CatalogItem Parent { get; set; }

        public ObservableCollection<CatalogItem> Items { get; set; } = new ObservableCollection<CatalogItem>();


        [JsonIgnore]
        public Hardware Hardware { get; set; }

        private string hardwareName;
        public string HardwareName
        {
            get { return Hardware?.Name; }
            set { hardwareName = value; }
        }

        public string GetHardwareName()
        {
            return hardwareName;
        }
    }
}
