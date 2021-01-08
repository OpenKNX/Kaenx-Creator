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
    }

    public enum ProcedureTypes
    {
        Default,
        Application,
        Merge
    }

    public enum MemoryTypes
    {
        Absolute,
        Relative
    }
}
