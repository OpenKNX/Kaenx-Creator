using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynParaBlock
    {
        public string Name { get; set; } = "Block";

        public ObservableCollection<DynParameter> Parameters { get; set; } = new ObservableCollection<DynParameter>();
    }
}
