using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynChannelIndependet : IDynItems, IDynChannel, INotifyPropertyChanged
    {
        public string Name { get; set; } = "Platzhalter";

        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
