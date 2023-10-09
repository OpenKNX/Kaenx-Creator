using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public interface IDynChoose : IDynItems
    {
        public ParameterRef ParameterRefObject { get; set; }
        public int ParameterRef { get; set; }
        public bool IsGlobal { get; set; }
    }
}
