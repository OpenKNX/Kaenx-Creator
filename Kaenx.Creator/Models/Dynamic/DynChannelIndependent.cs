using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynChannelIndependent : IDynItems, IDynChannel, INotifyPropertyChanged
    {
        [JsonIgnore]
        public IDynItems Parent { get; set; }

        private bool _isExpanded = true;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; Changed("IsExpanded"); }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        
        public IDynItems Copy()
        {
            DynChannelIndependent main = (DynChannelIndependent)this.MemberwiseClone();

            /* overwrite old reference with deep copy of the Translation Objects*/
            main.Items = new ObservableCollection<IDynItems>();
            foreach (IDynItems item in this.Items)
                main.Items.Add((IDynItems)item.Copy());
            return main;
        }
    }
}
