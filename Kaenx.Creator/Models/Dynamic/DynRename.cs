using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynRename : IDynItems, INotifyPropertyChanged
    {
        [JsonIgnore]
        public IDynItems Parent { get; set; }

        private bool _isExpanded = false;
        public bool IsExpanded
        {
            get { return _isExpanded; }
            set { _isExpanded = value; Changed("IsExpanded"); }
        }

        private long _id = -1;
        public long Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); }
        }

        private long _refId = -1;
        public long RefId
        {
            get { return _refId; }
            set { _refId = value; Changed("RefId"); }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        public ObservableCollection<Translation> Text {get;set;} = new ObservableCollection<Translation>();

        private bool _transText = false;
        public bool TranslationText
        {
            get { return _transText; }
            set { _transText = value; Changed("TranslationText"); }
        }

        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public IDynItems Copy()
        {
            DynRename dyn = (DynRename)this.MemberwiseClone();
            return dyn;
        }
    }
}