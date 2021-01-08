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
        public bool IsInMemory { get; set; } = true;
        public string Memory { get; set; }
        public string Suffix { get; set; }
        public int Offset { get; set; }
        public int OffsetBit { get; set; }
        public ParamAccess Access { get; set; } = ParamAccess.Default;
    }

    public enum ParamAccess
    {
        Default,
        None,
        Read,
        ReadWrite
    }
}