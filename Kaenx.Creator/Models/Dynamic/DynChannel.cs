using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynChannel : IDynItems, IDynChannel, INotifyPropertyChanged
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

        private string _number = "0";
        public string Number
        {
            get { return _number; }
            set { _number = value; Changed("Number"); }
        }

        public ObservableCollection<Translation> Text {get;set;} = new ObservableCollection<Translation>();

        private bool _transText = false;
        public bool TranslationText
        {
            get { return _transText; }
            set { _transText = value; Changed("TranslationText"); }
        }

        private bool _useTextParam = false;
        public bool UseTextParameter
        {
            get { return _useTextParam; }
            set { _useTextParam = value; Changed("UseTextParameter"); if(!value) ParameterRefObject = null; }
        }

        private ParameterRef _parameterRefObject;
        [JsonIgnore]
        public ParameterRef ParameterRefObject
        {
            get { return _parameterRefObject; }
            set { _parameterRefObject = value; Changed("ParameterRefObject"); if(value == null) _parameter = -1; }
        }

        [JsonIgnore]
        public int _parameter;
        public int ParameterRef
        {
            get { return ParameterRefObject?.UId ?? _parameter; }
            set { _parameter = value; }
        }

        
        private bool _useIcon = false;
        public bool UseIcon
        {
            get { return _useIcon; }
            set { 
                _useIcon = value; 
                Changed("UseIcon"); 
            }
        }

        [JsonIgnore]
        public int _iconId = -1;
        public int IconId{
            get { return IconObject?.UId ?? _iconId; }
            set { _iconId = value; }
        }

        private Icon _icon;
        [JsonIgnore]
        public Icon IconObject
        {
            get { return _icon; }
            set { _icon = value; Changed("IconObject"); if(value == null) _iconId = -1; }
        }


        public ParamAccess Access { get; set; } = ParamAccess.ReadWrite;

        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();

        public event PropertyChangedEventHandler PropertyChanged;

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        
        public IDynItems Copy()
        {
            DynChannel main = (DynChannel)this.MemberwiseClone();
            main.Items = new ObservableCollection<IDynItems>();
            foreach (IDynItems item in this.Items)
                main.Items.Add((IDynItems)item.Copy());
            main.Text = new ObservableCollection<Translation>();
            foreach (Translation translation in this.Text)
                main.Text.Add(new Translation(translation.Language, translation.Text));  
            return main;
        }
    }
}
