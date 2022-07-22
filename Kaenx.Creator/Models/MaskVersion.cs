using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class MaskVersion
    {
        public string Id { get; set; }
        public MemoryTypes Memory { get; set; }
        public ProcedureTypes Procedure { get; set; }
        public List<Procedure> Procedures {get;set;} = new List<Procedure>();
    }

    public class Procedure {
        public string Type {get;set;}
        public string SubType {get;set;}
        public string Controls {get;set;}
    }

    public enum ProcedureTypes
    {
        Default,
        Product,
        Merged
    }

    public enum MemoryTypes
    {
        Absolute,
        Relative
    }
}
