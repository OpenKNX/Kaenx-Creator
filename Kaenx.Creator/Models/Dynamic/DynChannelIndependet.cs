using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynChannelIndependet : IDynChannel, INotifyPropertyChanged
    {
        public string Name { get; set; } = "Platzhalter";

        public ObservableCollection<DynParaBlock> Blocks { get; set; } = new ObservableCollection<DynParaBlock>();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
