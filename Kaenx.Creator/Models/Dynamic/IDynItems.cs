using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public interface IDynItems
    {
        public IDynItems Parent { get; set; }
        public string Name { get; set; }
        public ObservableCollection<IDynItems> Items { get; set; }
        public bool IsExpanded { get; set; }
        public IDynItems Copy();
    }
}
