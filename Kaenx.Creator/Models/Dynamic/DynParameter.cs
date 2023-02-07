using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynParameter : IDynItems, INotifyPropertyChanged
    {
        [JsonIgnore]
        public IDynItems Parent { get; set; }
        public bool IsExpanded { get; set; }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private ParameterRef _parameterRefObject;
        [JsonIgnore]
        public ParameterRef ParameterRefObject
        {
            get { return _parameterRefObject; }
            set { if(value == null) return; _parameterRefObject = value; Changed("ParameterRefObject"); }
        }

        [JsonIgnore]
        public int _parameter;
        public int ParameterRef
        {
            get { return ParameterRefObject?.UId ?? -1; }
            set { _parameter = value; }
        }
        
        private bool _hasHelptext = false;
        public bool HasHelptext
        {
            get { return _hasHelptext; }
            set { 
                _hasHelptext = value; 
                if(!_hasHelptext) Helptext = null;
                Changed("HasHelptext"); 
            }
        }

        private Helptext _helptext;
        [JsonIgnore]
        public Helptext Helptext
        {
            get { return _helptext; }
            set { _helptext = value; Changed("Helptext"); }
        }

        [JsonIgnore]
        public int _helptextId;
        public int HelptextId
        {
            get { return Helptext?.UId ?? -1; }
            set { _helptextId = value; }
        }

        
        private bool _useIcon = false;
        public bool UseIcon
        {
            get { return _useIcon; }
            set { 
                _useIcon = value; 
                if(!_useIcon) IconObject = null;
                Changed("UseIcon"); 
            }
        }

        [JsonIgnore]
        public int _iconId = -1;
        public int IconId{
            get { return IconObject?.UId ?? -1; }
            set { _iconId = value; }
        }

        private Icon _icon;
        [JsonIgnore]
        public Icon IconObject
        {
            get { return _icon; }
            set { _icon = value; Changed("IconObject"); }
        }



        public string Cell { get; set; }

        public ObservableCollection<IDynItems> Items { get; set; }
        public ObservableCollection<Translation> HelpText { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        
        public object Copy()
        {
            return this.MemberwiseClone();;
        }
    }
}
