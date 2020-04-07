using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.ObjectModel;

namespace Kaenx.Creator.Models
{
    public class Parameter
    {
        public string Name { get; set; } = "dummy";
        public string Text { get; set; } = "Dummy";
        public string Value { get; set; } = "1";
        public string ParameterType { get; set; }
    }
}