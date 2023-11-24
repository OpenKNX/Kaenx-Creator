using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Module : INotifyPropertyChanged, IVersionBase
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

        private int _sizeNeeded = 0;
        public int SizeNeeded
        {
            get { return _sizeNeeded; }
            set { _sizeNeeded = value; Changed("SizeNeeded"); }
        }


        [JsonIgnore]
        public Memory Memory { get;set; } = new Memory();
        
        private string _prefix = "";
        public string Prefix
        {
            get { return _prefix; }
            set { _prefix = value; Changed("Prefix"); }
        }

        private string _name = "dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
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

        private bool _isAutoCBN= true;
        public bool IsComObjectBaseNumberAuto
        {
            get { return _isAutoCBN; }
            set { _isAutoCBN = value; Changed("IsComObjectBaseNumberAuto"); }
        }

        
        private Argument _parameterBaseOffset;
        [JsonIgnore]
        public Argument ParameterBaseOffset
        {
            get { return _parameterBaseOffset; }
            set { _parameterBaseOffset = value; Changed("ParameterBaseOffset"); if(value == null) _parameterBaseOffsetUId = -1; }
        }

        [JsonIgnore]
        public int _parameterBaseOffsetUId;
        public int ParameterBaseOffsetUId
        {
            get { return ParameterBaseOffset?.UId ?? _parameterBaseOffsetUId; }
            set { _parameterBaseOffsetUId = value; }
        }

        private Argument _comObjectBaseNumber;
        [JsonIgnore]
        public Argument ComObjectBaseNumber
        {
            get { return _comObjectBaseNumber; }
            set { 
                _comObjectBaseNumber = value; 
                Changed("ComObjectBaseNumber");
                if(value == null) _comObjectBaseNumberUId = -1; 
            }
        }

        [JsonIgnore]
        public int _comObjectBaseNumberUId;
        public int ComObjectBaseNumberUId
        {
            get { return ComObjectBaseNumber?.UId ?? _comObjectBaseNumberUId; }
            set { _comObjectBaseNumberUId = value; }
        }

    
        public ObservableCollection<Parameter> Parameters { get; set; } = new ObservableCollection<Parameter>();
        public ObservableCollection<ParameterRef> ParameterRefs { get; set; } = new ObservableCollection<ParameterRef>();
        public ObservableCollection<ComObject> ComObjects { get; set; } = new ObservableCollection<ComObject>();
        public ObservableCollection<ComObjectRef> ComObjectRefs { get; set; } = new ObservableCollection<ComObjectRef>();
        public ObservableCollection<Module> Modules { get; set; } = new ObservableCollection<Module>();
        public ObservableCollection<Union> Unions { get; set; } = new ObservableCollection<Union>();
        public ObservableCollection<Argument> Arguments { get; set; } = new ObservableCollection<Argument>();
        public ObservableCollection<Allocator> Allocators { get; set; } = new ObservableCollection<Allocator>();
        public List<IDynamicMain> Dynamics { get; set; } = new List<IDynamicMain>();

        public long LastParameterId { get; set; } = 0;
        public long LastParameterRefId { get; set; } = 0;
        public bool IsOpenKnxModule { get; set; } = false;

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
