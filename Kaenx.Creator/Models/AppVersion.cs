using Kaenx.Creator.Models.Dynamic;
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


        //TODO set defaultLanguage


        public AppVersion() { }

        public AppVersion(AppVersion ver)
        {
            ParameterRefs = ver.ParameterRefs;
            Parameters = ver.Parameters;
            ParameterTypes = ver.ParameterTypes;
            ComObjects = ver.ComObjects;
            ComObjectRefs = ver.ComObjectRefs;
            Memories = ver.Memories;
            Dynamics = ver.Dynamics;

            Name = ver.Name + " (Kopie)";
            Number = ver.Number;
            Number++;
            IsComObjectRefAuto = ver.IsComObjectRefAuto;
            IsParameterRefAuto = ver.IsParameterRefAuto;
            IsMemSizeAuto = ver.IsMemSizeAuto;
            Languages = ver.Languages;
        }

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

        private string _name = "";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("NameText"); }
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

        public event PropertyChangedEventHandler PropertyChanged;
        public void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
