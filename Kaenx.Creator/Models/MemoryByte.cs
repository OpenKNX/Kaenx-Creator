using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows.Data;
using System.Globalization;
using System.ComponentModel;

namespace Kaenx.Creator.Models
{
    public class MemoryByte : INotifyPropertyChanged
    {   
        private string _name = "dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        public MemoryByte(int address)
        {
            Name = $"0x{address:X4}";
        }

        public MemoryByte(int address, int usedBits = 0)
        {
            Name = $"0x{address:X4}";

            for(int x = 0; x < usedBits; x++)
                Bits[x] = "used";
        }


        public List<string> Bits {get;set;} = new List<string>() {
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null
        };

        public int GetFreeBits()
        {
            int maxSize = 0;
            int currentSize = 0;

            for(int i = 0; i < 8; i++)
            {
                if(string.IsNullOrEmpty(Bits[i]))
                    currentSize++;
                else
                {
                    if(currentSize > maxSize) maxSize = currentSize;
                    currentSize = 0;
                }
            }
            if(currentSize > maxSize) maxSize = currentSize;
            return maxSize;
        }

        public void SetBytesUsed(int size, int offset)
        {
            for(int x = offset; x < size; x++)
            {
                if(!string.IsNullOrEmpty(Bits[x]))
                    throw new Exception("Kein freier Speicherplatz in Byte");
                
                Bits[x] = "used";
            }
        }

        public int SetBytesUsed(int size)
        {
            int offset = 0;

            for(int i = 0; i < 8; i++)
            {
                bool flag = true;
                for(int x = 0; x < size; x++)
                {
                    if(!string.IsNullOrEmpty(Bits[i+x]))
                    {
                        flag = false;
                        break;
                    }

                }
                if(flag) break;
                offset++;
            }
            if((offset + size) > 8) throw new Exception("Kein freier Speicherplatz in Byte");

            for(int x = 0; x < size; x++)
                Bits[offset+x] = "used";

            return offset;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}