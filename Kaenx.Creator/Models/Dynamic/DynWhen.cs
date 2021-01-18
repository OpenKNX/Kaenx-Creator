using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynWhen : IDynItems, INotifyPropertyChanged
    {
        [JsonIgnore]
        public IDynItems Parent { get; set; }

        private string _name = "Unbenannt";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private bool _isDefault = false;
        public bool IsDefault
        {
            get { return _isDefault; }
            set { _isDefault = value; Changed("IsDefault"); }
        }


        public bool CanAddIndependent { get { return CheckForIndependent(Parent); } }
        public bool CanAddBlock { get { return CheckForBlock(Parent); } }
        public bool CanAddPara { get { return CheckForPara(Parent); } }




        private bool CheckForPara(IDynItems item)
        {
            if (item is DynParaBlock) return true;
            if (item.Parent == null) return false;
            return CheckForPara(item.Parent);
        }

        private bool CheckForIndependent(IDynItems item)
        {
            if (item is IDynChannel)
                return false;
            if (item.Parent == null) return true;
            return CheckForIndependent(item.Parent);
        }

        private bool CheckForBlock(IDynItems item)
        {
            if (item is DynChannelIndependet || item is DynChannel)
                return true;
            if (item.Parent == null || item is DynParaBlock) return false;
            return CheckForBlock(item.Parent);
        }


        public string Condition { get; set; } = "";

        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

}