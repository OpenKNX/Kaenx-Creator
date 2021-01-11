using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynParaBlock : IDynItems
    {
        public string Name { get; set; } = "Block";

        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();
    }
}
