using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace Kaenx.Creator.Models
{
    public class Memory
    {
        public string Name { get; set; } = "dummy";
        public int Address { get; set; } = 0;
        public int Size { get; set; } = 0;
        public MemoryTypes Type { get; set; } = MemoryTypes.Absolute;
    }
}
