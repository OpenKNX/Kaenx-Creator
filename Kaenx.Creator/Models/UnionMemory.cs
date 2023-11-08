using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using Newtonsoft.Json;

namespace Kaenx.Creator.Models
{
    public class UnionMemory
    {
        public string Name {get;set;}
        public bool[] Memory { get; set; }
        public bool Overflow { get; set; } = false;

        public UnionMemory(string name, int totalSize, int offset, int offsetbit, int size)
        {
            Name = name;
            Memory = new bool[totalSize];

            if(offset == -1|| offsetbit == -1) return;


            for(int i = 0; i < size; i++)
                if(((offset*8)+offsetbit+i) < Memory.Length)
                    Memory[(offset*8)+offsetbit+i] = true;
                else
                    Overflow = true;
        }
    }
}
