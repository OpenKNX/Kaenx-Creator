using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynChannel : IDynChannel
    {
        public ObservableCollection<DynParaBlock> Blocks { get; set; } = new ObservableCollection<DynParaBlock>();
    }
}
