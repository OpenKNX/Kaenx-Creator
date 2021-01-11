using System;
using System.Collections.Generic;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public class DynParameter
    {
        public string Name { get; set; } = "Parameter";
        public ParameterRef Parameter { get; set; }
    }
}
