using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Kaenx.Creator.Models
{
    public interface IVersionBase
    {
        public ObservableCollection<Parameter> Parameters { get; set; }
        public ObservableCollection<ParameterRef> ParameterRefs { get; set; }
        public ObservableCollection<ComObject> ComObjects { get; set; }
        public ObservableCollection<ComObjectRef> ComObjectRefs { get; set; }
        public ObservableCollection<Union> Unions { get; set; }
        public ObservableCollection<Module> Modules { get; set; }
        public ObservableCollection<Allocator> Allocators { get; set; }
        public List<IDynamicMain> Dynamics { get; set; }

        public string Name { get; set; }
        public bool IsParameterRefAuto { get; set; }
        public bool IsComObjectRefAuto { get; set; }
        public long LastParameterId { get; set; }
        public long LastParameterRefId { get; set; }
    }
}
