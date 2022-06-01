using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows.Media;

namespace Kaenx.Creator.Models
{
    public class Memory : INotifyPropertyChanged
    {
        public int StartAddress;

        private int _uid = -1;
        public int UId
        {
            get { return _uid; }
            set { _uid = value; Changed("UId"); }
        }

        
        private string _name = "dummy";
        public string Name
        {
            get { return _name; }
            set { _name = value; Changed("Name"); }
        }

        private int _address = 0;
        public int Address
        {
            get { return _address; }
            set { _address = value; Changed("Address"); }
        }

        private int _size = 0;
        public int Size
        {
            get { return _size; }
            set { _size = value; Changed("Size"); }
        }

        private int _offset = 0;
        public int Offset
        {
            get { return _offset; }
            set { _offset = value; Changed("Offset"); }
        }

        private bool _isPAutoPara = true;
        public bool IsAutoPara
        {
            get { return _isPAutoPara; }
            set { _isPAutoPara = value; Changed("IsAutoPara"); }
        }

        private bool _isPAutoOrder = true;
        public bool IsAutoOrder
        {
            get { return _isPAutoOrder; }
            set { _isPAutoOrder = value; Changed("IsAutoOrder"); }
        }

        private bool _isAutoSize = true;
        public bool IsAutoSize
        {
            get { return _isAutoSize; }
            set { _isAutoSize = value; Changed("IsAutoSize"); }
        }

        private DataGridCellInfo _currentCell;
        [JsonIgnore]
        public DataGridCellInfo CurrentCell
        {
            get { return _currentCell; }
            set { 
                _currentCell = value; 
                Changed("CurrentCell"); 
            }
        }


        private Models.MemoryByte _currentMemoryByte;
        [JsonIgnore]
        public Models.MemoryByte CurrentMemoryByte
        {
            get { return _currentMemoryByte; }
            set { _currentMemoryByte = value; Changed("CurrentMemoryByte"); }
        }


        private MemoryTypes _type = MemoryTypes.Absolute;
        public MemoryTypes Type
        {
            get { return _type; }
            set { _type = value; Changed("Type"); }
        }

        
        [JsonIgnore]
        public ObservableCollection<MemorySection> Sections {get;set;} = new ObservableCollection<MemorySection>();


        public void SetBytesUsed(Parameter para)
        {
            if(para.ParameterTypeObject.SizeInBit > 7)
            {
                int sizeInByte = (int)Math.Ceiling(para.ParameterTypeObject.SizeInBit / 8.0);
                for(int i = 0; i < sizeInByte;i++)
                {
                    int paraAddr = Address + para.Offset + i;
                    int secAddr = paraAddr - (paraAddr % 16);
                    MemorySection sec = Sections.Single(s => s.Address == secAddr);
                    int byteIndex = paraAddr - secAddr;
                    sec.Bytes[byteIndex].SetBitsUsed(para, 8, 0);
                }
            } else {
                int paraAddr = Address + para.Offset;
                int secAddr = paraAddr - (paraAddr % 16);
                MemorySection sec = Sections.Single(s => s.Address == secAddr);
                int byteIndex = paraAddr - secAddr;
                sec.Bytes[byteIndex].SetBitsUsed(para, para.ParameterTypeObject.SizeInBit, para.OffsetBit);
            }
        }

        public void SetBytesUsed(Union union, List<Parameter> paras)
        {
            if(union.SizeInBit > 7)
            {
                int sizeInByte = (int)Math.Ceiling(union.SizeInBit / 8.0);
                for(int i = 0; i < sizeInByte;i++)
                {
                    int paraAddr = Address + union.Offset + i;
                    int secAddr = paraAddr - (paraAddr % 16);
                    MemorySection sec = Sections.Single(s => s.Address == secAddr);
                    int byteIndex = paraAddr - secAddr;
                    sec.Bytes[byteIndex].SetBitsUsed(union, paras, 8, 0);
                }
            } else {
                throw new Exception("Union can? not be smaller than a byte");
            }
        }

        public void SetBytesUsed(MemoryByteUsage usage, int size, int offset = 0)
        {
            for(int i = 0; i < size;i++)
            {
                int paraAddr = Address + offset + i;
                int secAddr = paraAddr - (paraAddr % 16);
                MemorySection sec = Sections.Single(s => s.Address == secAddr);
                int byteIndex = paraAddr - secAddr;
                sec.Bytes[byteIndex].SetByteUsed(usage);
            }
        }

        public void AddBytes(int count)
        {
            for(int i = 0; i < count; i++)
            {
                int index = -1;

                for(int x = 0; x < Sections.Count; x++)
                {
                    if(Sections[x].Bytes.Count < 16){
                        index = x;
                        break;
                    }
                }

                if(index == -1)
                {
                    int startAddress = (Sections.Count * 16) + StartAddress;
                    Sections.Add(new MemorySection(startAddress));
                    if(Sections.Count == 1)
                    {
                        int toAdd = Address - StartAddress;
                        for(int x = 0; x < toAdd; x++)
                            Sections[0].Bytes.Add(new MemoryByte(startAddress + x, x, MemoryByteUsage.Used));
                    }
                    index = Sections.Count - 1;
                }

                int address = Sections[index].Address + Sections[index].Bytes.Count;
                Sections[index].Bytes.Add(new MemoryByte(address, address - Address));
            }
        }

        public (int offset, int offsetbit) GetFreeOffset(int size)
        {
            int offset = -1;
            int offsetbit = 0;
            int sizeInByte = (int)Math.Ceiling(size / 8.0);

            for(int i = 0; i < GetCount(); i++)
            {
                int availibleSize = 0;
                int availibleOffset = -1;
                
                for(int x = 0; x < sizeInByte; x++)
                {
                    if((i+x) > (GetCount()-1)) break;
                    int address = Address + i;
                    int secAddr = address - (address % 16);
                    int byteIndex = address - secAddr;
                    MemorySection sec = Sections.Single(s => s.Address == secAddr);


                    (int size, int offset) result = sec.Bytes[byteIndex].GetFreeBits();
                    //availibleSize += mem.Bytes[i+x].GetFreeBits();

                    if(result.offset == 0)
                    {
                        if(availibleOffset == -1) availibleOffset = result.offset;
                        availibleSize += result.size;
                    } else {
                        if(availibleOffset == -1)
                        {
                            availibleOffset = result.offset;
                            availibleSize += result.size;
                        } else {
                            break;
                        }
                    }
                }

                if(availibleSize >= size)
                {
                    offset = i;
                    offsetbit = availibleOffset;
                    break;
                }
            }

            if(offset == -1)
            {
                offset = GetCount();
                AddBytes(sizeInByte);
            }

            return (offset, offsetbit);
        }

        public int GetCount()
        {
            int counter = 0;
            foreach(MemorySection sec in Sections)
                counter += sec.Bytes.Count(b => b.Usage == MemoryByteUsage.Free);
            return counter;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
