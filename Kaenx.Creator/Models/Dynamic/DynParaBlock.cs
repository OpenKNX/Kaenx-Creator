using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynParaBlock : IDynItems, INotifyPropertyChanged
    {
        [JsonIgnore]
        public IDynItems Parent { get; set; }

        private int _id = -1;
        public int Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); }
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

        private bool _useParamRef = false;
        public bool UseParameterRef
        {
            get { return _useParamRef; }
            set { _useParamRef = value; Changed("UseParameterRef"); }
        }
        
        private ParameterRef _parameterRefObject;
        [JsonIgnore]
        public ParameterRef ParameterRefObject
        {
            get { return _parameterRefObject; }
            set { _parameterRefObject = value; Changed("ParameterRefObject"); }
        }

        [JsonIgnore]
        public int _parameterRef;
        public int ParameterRef
        {
            get { return ParameterRefObject?.UId ?? -1; }
            set { _parameterRef = value; }
        }



        private bool _useTextParam = false;
        public bool UseTextParameter
        {
            get { return _useTextParam; }
            set { _useTextParam = value; Changed("UseTextParameter"); }
        }


        private ParameterRef _textRefObject;
        [JsonIgnore]
        public ParameterRef TextRefObject
        {
            get { return _textRefObject; }
            set { _textRefObject = value; Changed("TextRefObject"); }
        }

        [JsonIgnore]
        public int _textRef;
        public int TextRef
        {
            get { return TextRefObject?.UId ?? -1; }
            set { _textRef = value; }
        }

        private BlockLayout _layout = BlockLayout.List;
        public BlockLayout Layout
        {
            get { return _layout; }
            set { _layout = value; Changed("Layout"); }
        }
        private bool _isInline = false;
        public bool IsInline
        {
            get { return _isInline; }
            set { _isInline = value; Changed("IsInline"); }
        }
        public ObservableCollection<IDynItems> Items { get; set; } = new ObservableCollection<IDynItems>();
        public ObservableCollection<ParameterBlockRow> Rows { get; set; } = new ObservableCollection<ParameterBlockRow>();
        public ObservableCollection<ParameterBlockColumn> Columns { get; set; } = new ObservableCollection<ParameterBlockColumn>();
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ParameterBlockRow
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class ParameterBlockColumn
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Width { get; set; }
    }

    public enum BlockLayout
    {
        List,
        Grid,
        Table
    }
}
