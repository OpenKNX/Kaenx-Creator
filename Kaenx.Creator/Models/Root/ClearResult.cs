using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;

namespace Kaenx.Creator.Models
{
    public class ClearResult
    {   
        public int ParameterTypes { get; set; }
        public int Parameters { get; set; }
        public int ParameterRefs { get; set; }
        public int ComObjects { get; set; }
        public int ComObjectRefs { get; set; }
        public int Unions { get; set; }
    }
}