using Kaenx.Creator.Models.Dynamic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class AppVersion : INotifyPropertyChanged, IVersionBase
    {

        public ObservableCollection<ParameterType> ParameterTypes { get; set; } = new ObservableCollection<ParameterType>();
        public ObservableCollection<Parameter> Parameters { get; set; } = new ObservableCollection<Parameter>();
        public ObservableCollection<ParameterRef> ParameterRefs { get; set; } = new ObservableCollection<ParameterRef>();
        public ObservableCollection<ComObject> ComObjects { get; set; } = new ObservableCollection<ComObject>();
        public ObservableCollection<ComObjectRef> ComObjectRefs { get; set; } = new ObservableCollection<ComObjectRef>();
        public ObservableCollection<Memory> Memories { get; set; } = new ObservableCollection<Memory>();
        public ObservableCollection<Module> Modules { get; set; } = new ObservableCollection<Module>();
        public ObservableCollection<Union> Unions { get; set; } = new ObservableCollection<Union>();
        public ObservableCollection<Language> Languages { get; set; } = new ObservableCollection<Language>();
        public List<DynamicMain> Dynamics { get; set; } = new List<DynamicMain>();

        public AppVersion() { }

        private int _namespace = 14;
        public int NamespaceVersion
        {
            get { return _namespace; }
            set { _namespace = value; Changed("NamespaceVersion"); }
        }

        private string _defLang = null;
        public string DefaultLanguage
        {
            get { return _defLang; }
            set { _defLang = value; Changed("DefaultLanguage"); }
        }

        private int _defLangIndex = -1;
        public int DefaultLanguageIndex
        {
            get { return _defLangIndex; }
            set { 
                _defLangIndex = value; 
                Changed("DefaultLanguageIndex"); }
        }

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("NameText"); }
        }

        private string _replaces = "";
        public string ReplacesVersions
        {
            get { return _replaces; }
            set { _replaces = value; Changed("ReplacesVersions"); }
        }

        public string NameText
        {
            get
            {
                int main = (int)Math.Floor((double)Number / 16);
                int sub = Number - (main * 16);
                return "V " + main + "." + sub + " " + Name;
            }
        }

        public ObservableCollection<Translation> Text {get;set;} = new ObservableCollection<Translation>();

        private int _number = 16;
        public int Number
        {
            get { return _number; }
            set { _number = value; Changed("Number"); Changed("NameText"); }
        }



        private int _addrTableMax = 65535;
        public int AddressTableMaxCount
        {
            get { return _addrTableMax; }
            set { _addrTableMax = value; Changed("AddressTableMaxCount"); }
        }
        private int _addrTableOffset = 0;
        public int AddressTableOffset
        {
            get { return _addrTableOffset; }
            set { _addrTableOffset = value; Changed("AddressTableOffset"); }
        }
        private Memory _addressMemoryObject;
        [JsonIgnore]
        public Memory AddressMemoryObject
        {
            get { return _addressMemoryObject; }
            set { _addressMemoryObject = value; Changed("AddressMemoryObject"); }
        }
        [JsonIgnore]
        public int _addressMemoryId;
        public int AddressMemoryId
        {
            get { return AddressMemoryObject?.UId ?? -1; }
            set { _addressMemoryId = value; }
        }



        private int _assocTableMax = 65535;
        public int AssociationTableMaxCount
        {
            get { return _assocTableMax; }
            set { _assocTableMax = value; Changed("AssociationTableMaxCount"); }
        }
        private int _assocTableOffset = 0;
        public int AssociationTableOffset
        {
            get { return _assocTableOffset; }
            set { _assocTableOffset = value; Changed("AssociationTableOffset"); }
        }
        private Memory _assocMemoryObject;
        [JsonIgnore]
        public Memory AssociationMemoryObject
        {
            get { return _assocMemoryObject; }
            set { _assocMemoryObject = value; Changed("AssociationMemoryObject"); }
        }
        [JsonIgnore]
        public int _assocMemoryId;
        public int AssociationMemoryId
        {
            get { return AssociationMemoryObject?.UId ?? -1; }
            set { _assocMemoryId = value; }
        }

        
        private int _comTableOffset = 0;
        public int ComObjectTableOffset
        {
            get { return _comTableOffset; }
            set { _comTableOffset = value; Changed("ComObjectTableOffset"); }
        }
        private Memory _comMemoryObject;
        [JsonIgnore]
        public Memory ComObjectMemoryObject
        {
            get { return _comMemoryObject; }
            set { _comMemoryObject = value; Changed("ComObjectMemoryObject"); }
        }
        [JsonIgnore]
        public int _comMemoryId;
        public int ComObjectMemoryId
        {
            get { return ComObjectMemoryObject?.UId ?? -1; }
            set { _comMemoryId = value; }
        }



        private bool _isAutoPR = true;
        public bool IsParameterRefAuto
        {
            get { return _isAutoPR; }
            set { _isAutoPR = value; Changed("IsParameterRefAuto"); }
        }

        private bool _isAutoCR= true;
        public bool IsComObjectRefAuto
        {
            get { return _isAutoCR; }
            set { _isAutoCR = value; Changed("IsComObjectRefAuto"); }
        }

        private bool _isUnionActive = false;
        public bool IsUnionActive
        {
            get { return _isUnionActive; }
            set { _isUnionActive = value; Changed("IsUnionActive"); }
        }

        private bool _isModulesActive = false;
        public bool IsModulesActive
        {
            get { return _isModulesActive; }
            set { _isModulesActive = value; Changed("IsModulesActive"); }
        }

        private bool _isMemSizeAuto = true;
        public bool IsMemSizeAuto
        {
            get { return _isMemSizeAuto; }
            set { _isMemSizeAuto = value; Changed("IsMemSizeAuto"); }
        }

        private string _procedure = "";
        public string Procedure
        {
            get { return _procedure; }
            set { _procedure = value; Changed("Procedure"); }
        }

        public int LastParameterId { get; set; } = 0;
        public int LastParameterRefId { get; set; } = 0;

        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
