using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynButton : IDynItems, INotifyPropertyChanged
    {
        [JsonIgnore]
        public IDynItems Parent { get; set; }
        public bool IsExpanded { get; set; }

        public string Online { get; set; } = "";
        public string EventHandlerParameters { get; set; }
        public string Cell { get; set; }
        public ParamAccess Access { get; set; } = ParamAccess.ReadWrite;

        private long _id = -1;
        public long Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); Changed("NameHeader"); }
        }

        public string NameHeader
        {
            get { return Classes.ExportHelper.HeaderNameEscape(Name); }
        }

        private string _script = "";
        public string Script
        {
            get { return _script; }
            set { _script = value; Changed("Script"); }
        }

        //TODO add TextParameterRefId



        private bool _useTextParam = false;
        public bool UseTextParameter
        {
            get { return _useTextParam; }
            set { 
                _useTextParam = value; 
                if(!_useTextParam) TextRefObject = null;
                Changed("UseTextParameter"); 
            }
        }


        private ParameterRef _textRefObject;
        [JsonIgnore]
        public ParameterRef TextRefObject
        {
            get { return _textRefObject; }
            set { _textRefObject = value; Changed("TextRefObject"); if(value == null) _textRef = -1; }
        }

        [JsonIgnore]
        public int _textRef;
        public int TextRef
        {
            get { return TextRefObject?.UId ?? _textRef; }
            set { _textRef = value; }
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
            DynButton main = (DynButton)this.MemberwiseClone();
            main.Items = new ObservableCollection<IDynItems>();
            main.Text = new ObservableCollection<Translation>();
            foreach (Translation translation in this.Text)
                main.Text.Add(new Translation(translation.Language, translation.Text));  
            return main;
        }
    }
}