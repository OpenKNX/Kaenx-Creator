using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class DataPointType
    {
        public string Name { get; set; }
        public string Number { get; set; }
        public int Size { get; set; }

        public ObservableCollection<DataPointSubType> SubTypes { get; set; } = new ObservableCollection<DataPointSubType>();
    }
}
