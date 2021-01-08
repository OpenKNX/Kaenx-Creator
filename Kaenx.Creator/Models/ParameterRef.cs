using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class ParameterRef
    {
        public string Name { get; set; } = "Kurze Beschreibung";
        public string ParameterId { get; set; }
        public ParamAccess Access { get; set; } = ParamAccess.Default;
        public string Value { get; set; } = "";
        public string Suffix { get; set; } = "";
    }
}
