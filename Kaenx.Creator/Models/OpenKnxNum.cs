using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Kaenx.Creator.Models
{
    public class OpenKnxNum
    {
        public string UId { get; set; }
        public string Property { get; set; }
        public NumberType Type { get; set; }
    }

    public enum NumberType
    {
        Parameter,
        ParameterType
    }
}