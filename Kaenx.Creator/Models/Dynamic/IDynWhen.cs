using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text;

namespace Kaenx.Creator.Models.Dynamic
{
    public interface IDynWhen : IDynItems
    {
        public bool IsDefault { get; set; }

        public string Condition { get; set; }
    }
}
