using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using Newtonsoft.Json;

namespace Kaenx.Creator.Models
{
    public class MemoryUnion
    {
        public Union UnionObject {get;set;}
        public List<Parameter> ParameterList = new List<Parameter>();
    }
}
