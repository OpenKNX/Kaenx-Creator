using Kaenx.Creator.Models.Dynamic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;

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
        public ObservableCollection<Message> Messages { get; set; } = new ObservableCollection<Message>();
        public ObservableCollection<Union> Unions { get; set; } = new ObservableCollection<Union>();
        public ObservableCollection<Language> Languages { get; set; } = new ObservableCollection<Language>();
        public ObservableCollection<Helptext> Helptexts { get; set; } = new ObservableCollection<Helptext>();
        public ObservableCollection<Allocator> Allocators { get; set; } = new ObservableCollection<Allocator>();
        public ObservableCollection<OpenKnxModule> OpenKnxModules { get; set; } = new ObservableCollection<OpenKnxModule>();
        public List<IDynamicMain> Dynamics { get; set; } = new List<IDynamicMain>();

        public AppVersion() { }

        private int _namespace = 20;
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

        private string _script = "";
        public string Script
        {
            get { return _script; }
            set { _script = value; Changed("Script"); }
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
            set { _addressMemoryObject = value; Changed("AddressMemoryObject"); if(value == null) _addressMemoryId = -1; }
        }
        [JsonIgnore]
        public int _addressMemoryId;
        public int AddressMemoryId
        {
            get { return AddressMemoryObject?.UId ?? _addressMemoryId; }
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
            set { _assocMemoryObject = value; Changed("AssociationMemoryObject");if(value == null) _assocMemoryId = -1; }
        }
        [JsonIgnore]
        public int _assocMemoryId;
        public int AssociationMemoryId
        {
            get { return AssociationMemoryObject?.UId ?? _assocMemoryId; }
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
            set { _comMemoryObject = value; Changed("ComObjectMemoryObject"); if(value == null) _comMemoryId = -1; }
        }
        [JsonIgnore]
        public int _comMemoryId;
        public int ComObjectMemoryId
        {
            get { return ComObjectMemoryObject?.UId ?? _comMemoryId; }
            set { _comMemoryId = value; }
        }



        private bool _isAutoPR = true;
        public bool IsParameterRefAuto
        {
            get { return _isAutoPR; }
            set { 
                _isAutoPR = value;
                Changed("IsParameterRefAuto");
                foreach(ParameterRef pref in ParameterRefs)
                    pref.IsAutoGenerated = value;
            }
        }

        private bool _isAutoCR= true;
        public bool IsComObjectRefAuto
        {
            get { return _isAutoCR; }
            set {
                _isAutoCR = value;
                Changed("IsComObjectRefAuto");
                foreach(ComObjectRef cref in ComObjectRefs)
                    cref.IsAutoGenerated = value;
            }
        }

        private bool _isUnionActive = false;
        public bool IsUnionActive
        {
            get { return _isUnionActive; }
            set { _isUnionActive = value; Changed("IsUnionActive"); if(!value) Unions.Clear(); }
        }

        private bool _isModulesActive = false;
        public bool IsModulesActive
        {
            get { return _isModulesActive; }
            set { _isModulesActive = value; Changed("IsModulesActive"); if(!value) Modules.Clear(); }
        }
        
        private bool _isMessagesActive = false;
        public bool IsMessagesActive
        {
            get { return _isMessagesActive; }
            set { _isMessagesActive = value; Changed("IsMessagesActive"); if(!value) Messages.Clear(); }
        }

        private bool _isMemSizeAuto = true;
        public bool IsMemSizeAuto
        {
            get { return _isMemSizeAuto; }
            set { _isMemSizeAuto = value; Changed("IsMemSizeAuto"); }
        }

        private bool _isHelpActive = false;
        public bool IsHelpActive
        {
            get { return _isHelpActive; }
            set { _isHelpActive = value; Changed("IsHelpActive"); }
        }

        private bool _isBusInterfaceActive = false;
        public bool IsBusInterfaceActive
        {
            get { return _isBusInterfaceActive; }
            set { _isBusInterfaceActive = value; Changed("IsBusInterfaceActive"); }
        }

        public bool HasBusInterfaceRouter { get; set; }
        public int BusInterfaceCounter { get; set; } = 4;

        private string _procedure = "";
        public string Procedure
        {
            get { return _procedure; }
            set { _procedure = value; Changed("Procedure"); }
        }

        private bool _isPreETS4 = false;
        public bool IsPreETS4
        {
            get { return _isPreETS4; }
            set { _isPreETS4 = value; Changed("IsPreETS4"); }
        }

        public long LastParameterId { get; set; } = 0;
        public long LastParameterRefId { get; set; } = 0;
        public int LastDynModuleId { get; set; } = 0;
        public int LastDynSeparatorId { get; set; } = 0;
        public int HighestComNumber { get; set; } = 0;

        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public AppVersion Copy()
        {
            string json = Newtonsoft.Json.JsonConvert.SerializeObject(this, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });

            AppVersion clone = Newtonsoft.Json.JsonConvert.DeserializeObject<AppVersion>(json, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });

            /*
        public ObservableCollection<Union> Unions { get; set; } = new ObservableCollection<Union>();
        public ObservableCollection<Language> Languages { get; set; } = new ObservableCollection<Language>();
        public ObservableCollection<Helptext> Helptexts { get; set; } = new ObservableCollection<Helptext>();
        public List<IDynamicMain> Dynamics { get; set; } = new List<IDynamicMain>();
            */
            
            CopyVersion(clone, clone);
            
            foreach(Module mod in clone.Modules)
            {
                if(mod._parameterBaseOffsetUId != -1)
                    mod.ParameterBaseOffset = mod.Arguments.Single(a => a.UId == mod._parameterBaseOffsetUId);

                if(mod._comObjectBaseNumberUId != -1)
                    mod.ComObjectBaseNumber = mod.Arguments.Single(a => a.UId == mod._comObjectBaseNumberUId);

                CopyVersion(mod, clone);
            }

            return clone;
        }

        private void CopyVersion(IVersionBase vbase, AppVersion vers)
        {
            foreach(Parameter para in vbase.Parameters)
            {
                if(para._parameterType != -1)
                    para.ParameterTypeObject = vers.ParameterTypes.Single(t => t.UId == para._parameterType);

                if(para._memoryId != -1)
                    para.SaveObject = vers.Memories.Single(m => m.UId == para._memoryId);
            }

            foreach(ParameterRef para in vbase.ParameterRefs)
            {
                if(para._parameter != -1)
                    para.ParameterObject = vbase.Parameters.Single(t => t.UId == para._parameter);
            }

            foreach(ComObject com in vbase.ComObjects)
            {
                if(vbase.IsComObjectRefAuto && com._parameterRef != -1)
                    com.ParameterRefObject = vbase.ParameterRefs.Single(p => p.UId == com._parameterRef);
            }

            foreach(ComObjectRef com in vbase.ComObjectRefs)
            {
                if(com._comObject != -1)
                    com.ComObjectObject = vbase.ComObjects.Single(c => c.UId == com._comObject);

                if(!vbase.IsComObjectRefAuto && com._parameterRef != -1)
                    com.ParameterRefObject = vbase.ParameterRefs.Single(p => p.UId == com._parameterRef);
            }

            vbase.Dynamics[0] = (Models.Dynamic.DynamicMain)Dynamics[0].Copy();

            foreach(Models.Dynamic.IDynItems item in vbase.Dynamics[0].Items)
            {
                CopyDynamicItem(item, vbase);
            }
        }

        //!!!! Also change in ImportCreator.cs
        private void CopyDynamicItem(Models.Dynamic.IDynItems item, IVersionBase vbase)
        {
            switch(item)
            {
                case Models.Dynamic.DynChannel dc:
                {
                    if(dc.UseTextParameter && dc.ParameterRef != -1)
                        dc.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dc.ParameterRef);
                    break;
                }

                case Models.Dynamic.DynParaBlock dpb:
                {
                    if(dpb.UseParameterRef && dpb.ParameterRef != -1)
                        dpb.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dpb.ParameterRef);
                    if(dpb.UseTextParameter && dpb.TextRef != -1)
                        dpb.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dpb.TextRef);
                    break;
                }

                case Models.Dynamic.DynChooseBlock dch:
                {
                    if(dch.ParameterRef != -1)
                        dch.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dch.ParameterRef);
                    break;
                }

                case Models.Dynamic.DynChooseChannel dch:
                {
                    if(dch.ParameterRef != -1)
                        dch.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dch.ParameterRef);
                    break;
                }

                case Models.Dynamic.DynParameter dp:
                {
                    if(dp.ParameterRef != -1)
                        dp.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dp.ParameterRef);
                    break;
                }

                case Models.Dynamic.DynComObject dco:
                {
                    if(dco.ComObjectRef != -1)
                        dco.ComObjectRefObject = vbase.ComObjectRefs.SingleOrDefault(c => c.UId == dco.ComObjectRef);
                    break;
                }

                case Models.Dynamic.DynSeparator dse:
                {
                    if(dse.TextRef != -1)
                        dse.TextRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dse.TextRef);
                    break;
                }

                case Models.Dynamic.DynChannelIndependent:
                case Models.Dynamic.IDynWhen:
                    break;

                case Models.Dynamic.DynAssign da:
                    if(da._targetUId != -1)
                        da.TargetObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == da._targetUId);
                    if(string.IsNullOrEmpty(da.Value) && da._sourceUId != -1)
                        da.SourceObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == da._sourceUId);
                    break;

                default:
                    throw new Exception("Not implemented copy " + item.GetType().ToString());

            }

            if(item.Items == null) return;
            foreach(Models.Dynamic.IDynItems ditem in item.Items)
                CopyDynamicItem(ditem, vbase);
        }
    }
}
