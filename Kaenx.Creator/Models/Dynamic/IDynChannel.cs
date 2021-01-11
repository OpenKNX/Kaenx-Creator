using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public interface IDynChannel
    {
        ObservableCollection<DynParaBlock> Blocks { get; set; }
    }
}
