using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ComObject : INotifyPropertyChanged
    {
        private string _name = "Kommunikationsobjekt";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private string _text = "Taste n";
        public string Text
        {
            get { return _text; }
            set { _text = value; Changed("Text"); }
        }

        private string _funcText = "Kurze Tastenbetätigung";
        public string FunctionText
        {
            get { return _funcText; }
            set { _funcText = value; Changed("FunctionText"); }
        }

        private string _desc = "";
        public string Description
        {
            get { return _desc; }
            set { _desc = value; Changed("Description"); }
        }

        private int _numb = 0;
        public int Number
        {
            get { return _numb; }
            set { _numb = value; Changed("Number"); }
        }


        private FlagType _flagRead = FlagType.Default;
        public FlagType FlagRead
        {
            get { return _flagRead; }
            set { _flagRead = value; Changed("FlagRead"); }
        }

        private FlagType _flagWrite =  FlagType.Default;
        public FlagType FlagWrite
        {
            get { return _flagWrite; }
            set { _flagWrite = value; Changed("FlagWrite"); }
        }

        private FlagType _flagTrans = FlagType.Default;
        public FlagType FlagTrans
        {
            get { return _flagTrans; }
            set { _flagTrans = value; Changed("FlagTrans"); }
        }

        private FlagType _flagComm = FlagType.Enabled;
        public FlagType FlagComm
        {
            get { return _flagComm; }
            set { _flagComm = value; Changed("FlagComm"); }
        }

        private FlagType _flagUpdate = FlagType.Default;
        public FlagType FlagUpdate
        {
            get { return _flagUpdate; }
            set { _flagUpdate = value; Changed("FlagUpdate"); }
        }

        private FlagType _flagOnInit = FlagType.Default;
        public FlagType FlagOnInit
        {
            get { return _flagOnInit; }
            set { _flagOnInit = value; Changed("FlagOnInit"); }
        }






        private string _typeValue;
        public string TypeValue
        {
            get { return _typeValue; }
            set { if (value == null) return;  _typeValue = value; Changed("TypeValue"); }
        }

        private bool _hasSub = false;
        public bool HasSub
        {
            get { return _hasSub; }
            set { _hasSub = value; Changed("HasSub"); }
        }

        private string _typeParentValue;
        public string TypeParentValue
        {
            get { return _typeParentValue; }
            set { _typeParentValue = value; Changed("TypeParentValue"); }
        }

        private DataPointSubType _type;
        public DataPointSubType Type
        {
            get { return _type; }
            set { if (value == null) return; _type = value; Changed("Type"); }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        //Only used for export
        [JsonIgnore]
        public string RefId { get; set; }
    }

    public enum FlagType
    {
        Default,
        Enabled,
        Disabled
    }
}
