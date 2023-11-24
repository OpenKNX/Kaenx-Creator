using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ComObject : INotifyPropertyChanged
    {
        private int _uid = -1;
        public int UId
        {
            get { return _uid; }
            set { _uid = value; Changed("UId"); }
        }

        private long _id = -1;
        public long Id
        {
            get { return _id; }
            set { _id = value; Changed("Id"); }
        }

        private string _name = "Kommunikationsobjekt";
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


        public ObservableCollection<Translation> FunctionText {get;set;} = new ObservableCollection<Translation>();
        private bool _transFuncText = false;
        public bool TranslationFunctionText
        {
            get { return _transFuncText; }
            set { _transFuncText = value; Changed("TranslationFunctionText"); }
        }


        private int _numb = 0;
        public int Number
        {
            get { return _numb; }
            set { _numb = value; Changed("Number"); }
        }


        private bool _flagRead = false;
        public bool FlagRead
        {
            get { return _flagRead; }
            set { _flagRead = value; Changed("FlagRead"); }
        }

        private bool _flagWrite = false;
        public bool FlagWrite
        {
            get { return _flagWrite; }
            set { _flagWrite = value; Changed("FlagWrite"); }
        }

        private bool _flagTrans = false;
        public bool FlagTrans
        {
            get { return _flagTrans; }
            set { _flagTrans = value; Changed("FlagTrans"); }
        }

        private bool _flagComm = true;
        public bool FlagComm
        {
            get { return _flagComm; }
            set { _flagComm = value; Changed("FlagComm"); }
        }

        private bool _flagUpdate = false;
        public bool FlagUpdate
        {
            get { return _flagUpdate; }
            set { _flagUpdate = value; Changed("FlagUpdate"); }
        }

        private bool _flagOnInit = false;
        public bool FlagOnInit
        {
            get { return _flagOnInit; }
            set { _flagOnInit = value; Changed("FlagOnInit"); }
        }






        private string _typeValue;
        public string TypeValue
        {
            get { return _typeValue; }
            set { if (value == null) return; _typeValue = value; Changed("TypeValue"); }
        }

        private bool _hasDpt = false;
        public bool HasDpt
        {
            get { return _hasDpt; }
            set { _hasDpt = value; Changed("HasDpt"); }
        }

        private bool _hasDpts = false;
        public bool HasDpts
        {
            get { return _hasDpts; }
            set { _hasDpts = value; Changed("HasDpts"); }
        }

        private int _objectSize = 1;
        public int ObjectSize
        {
            get { return _objectSize; }
            set { _objectSize = value; Changed("ObjectSize"); }
        }




        [JsonIgnore]
        public string _subTypeNumber;
        public string SubTypeNumber
        {
            get { return SubType?.Number; }
            set { _subTypeNumber = value; Changed("SubTypeNumber"); }
        }

        private DataPointSubType _subType;
        public DataPointSubType SubType
        {
            get { return _subType; }
            set { if (value == null) return; _subType = value; Changed("SubType"); }
        }

        [JsonIgnore]
        public string _typeNumber;
        public string TypeNumber
        {
            get { return Type?.Number; }
            set { if (value == null) return; _typeNumber = value; Changed("TypeNumber"); }
        }

        //TODO dont save all subtypes in .ae-menu file
        private DataPointType _type;
        public DataPointType Type
        {
            get { return _type; }
            set { 
                if (value == null) return;
                ObjectSize = value.Size;
                _type = value; Changed("Type"); 
            }
        }

        private bool _isNotUsed = false;
        [JsonIgnore]
        public bool IsNotUsed
        {
            get { return _isNotUsed; }
            set { _isNotUsed = value; Changed("IsNotUsed"); }
        }



        private bool _useTextParam = false;
        public bool UseTextParameter
        {
            get { return _useTextParam; }
            set { 
                _useTextParam = value; 
                Changed("UseTextParameter"); 
                if(!_useTextParam)
                    ParameterRefObject = null;
            }
        }

        private ParameterRef _parameterRefObject;
        [JsonIgnore]
        public ParameterRef ParameterRefObject
        {
            get { return _parameterRefObject; }
            set { _parameterRefObject = value; Changed("ParameterRefObject"); if(value == null) _parameterRef = -1; }
        }

        [JsonIgnore]
        public int _parameterRef;
        public int ParameterRef
        {
            get { return ParameterRefObject?.UId ?? _parameterRef; }
            set { _parameterRef = value; }
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public ComObject Copy()
        {
            ComObject comobj = (ComObject)this.MemberwiseClone();

            /* overwrite old reference with deep copy of the Translation Objects*/
            comobj.Text = new ObservableCollection<Translation>();
            foreach (Translation translation in this.Text)
                comobj.Text.Add(new Translation(translation.Language, translation.Text));

            comobj.FunctionText = new ObservableCollection<Translation>();
            foreach (Translation translation in this.FunctionText)
                comobj.FunctionText.Add(new Translation(translation.Language, translation.Text));
                
            return comobj;
        }
    }
}
