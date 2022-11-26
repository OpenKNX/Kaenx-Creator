using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class MaskVersion
    {
        private static Dictionary<string, string> MediumTypeNames = new Dictionary<string, string>() {
            { "MT-0", "Twisted Pair"},
            { "MT-1", "PowerLine"},
            { "MT-2", "KNX RF"},
            { "MT-5", "KNXnet/IP"}
        };

        public string Id { get; set; }
        public MemoryTypes Memory { get; set; }
        public ProcedureTypes Procedure { get; set; }
        public List<Procedure> Procedures {get;set;} = new List<Procedure>();

        public string MediumTypes { get; set; } = "";
        public string Mediums {
            get {
                string mediums = "";
                foreach(string type in MediumTypes.Split(' '))
                {
                    if(MediumTypeNames.ContainsKey(type))
                        mediums += "/" + MediumTypeNames[type];
                }
                if(mediums.Length == 0) return "";
                return mediums.Substring(1);
            }
        }
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
