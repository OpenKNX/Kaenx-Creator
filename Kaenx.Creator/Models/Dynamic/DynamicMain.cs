using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynamicMain : IDynItems, INotifyPropertyChanged
    {
        public string Name { get; set; } = "Root Knoten";

        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public int ItemsCount
        {
            get { return Items.Count; }
        }


        public DynamicMain()
        {
            Items.CollectionChanged += Items_CollectionChanged;
        }

        private void Items_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            Changed("ItemsCount");
            Changed("Items");
        }
    }
}
