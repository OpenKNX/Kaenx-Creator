using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Kaenx.Creator.Models;

namespace Kaenx.Creator.Classes
{
    public static class AutoHelper
    {

        public static void MemoryCalculation(AppVersion ver, Memory mem)
        {
            ParameterTypeCalculations(ver);

            mem.Bytes.Clear();

            int memOffset = 0;
            if(mem.Type == MemoryTypes.Absolute) memOffset = mem.Address;

            if(!mem.IsAutoSize)
                for(int i = 0; i < mem.Size; i++)
                    mem.Bytes.Add(new MemoryByte(memOffset + i));

            //TODO add tables to memory

            List<Parameter> paras = ver.Parameters.Where(p => p.MemoryId == mem.UId && p.IsInUnion == false).ToList();
            foreach(Parameter para in paras.Where(p => p.Offset != -1))
            {
                if(para.Offset >= mem.Bytes.Count)
                {
                    if(!mem.IsAutoSize) throw new Exception("Parameter liegt außerhalb des Speichers");
                    
                    int toadd = (para.Offset - mem.Bytes.Count) + 1;
                    if(para.ParameterTypeObject.SizeInBit > 8) toadd += (para.ParameterTypeObject.SizeInBit / 8) - 1;
                    int reloffset = mem.Bytes.Count;
                    for(int i = 0; i < toadd; i++)
                        mem.Bytes.Add(new MemoryByte(memOffset + reloffset + i));
                }

                if(para.ParameterTypeObject.SizeInBit > 7)
                {
                    int sizeInByte = (int)Math.Ceiling(para.ParameterTypeObject.SizeInBit / 8.0);
                    for(int i = 0; i < sizeInByte;i++)
                        mem.Bytes[para.Offset+i].SetBytesUsed(8,0);
                } else {
                    mem.Bytes[para.Offset].SetBytesUsed(para.ParameterTypeObject.SizeInBit, para.OffsetBit);
                }
            }

            foreach (Union union in ver.Unions.Where(u => u.MemoryId == mem.UId && u.Offset != -1))
            {
                if(union.Offset >= mem.Bytes.Count)
                {
                    if(!mem.IsAutoSize) throw new Exception("Parameter liegt außerhalb des Speichers");

                    int toadd = 1;
                    if(union.SizeInBit > 8) toadd = (union.Offset - mem.Bytes.Count) + (union.SizeInBit / 8);
                    for(int i = 0; i < toadd; i++)
                        mem.Bytes.Add(new MemoryByte(memOffset + union.Offset + i));
                }

                if(union.SizeInBit > 7)
                {
                    int sizeInByte = (int)Math.Ceiling(union.SizeInBit / 8.0);
                    for(int i = 0; i < sizeInByte;i++)
                        mem.Bytes[union.Offset+i].SetBytesUsed(8,0);
                } else {
                    mem.Bytes[union.Offset].SetBytesUsed(union.SizeInBit, union.OffsetBit);
                }
            }


            foreach(Module mod in ver.Modules)
            {
                mod.Memory.Bytes.Clear();
                foreach(Parameter para in mod.Parameters.Where(p => p.MemoryId == mem.UId && p.Offset != -1))
                {
                    if(para.Offset >= mod.Memory.Bytes.Count)
                    {
                        int toadd = (para.Offset - mod.Memory.Bytes.Count) + 1;
                        if(para.ParameterTypeObject.SizeInBit > 8) toadd += (para.ParameterTypeObject.SizeInBit / 8) - 1;
                        int reloffset = mod.Memory.Bytes.Count;
                        for(int i = 0; i < toadd; i++)
                            mod.Memory.Bytes.Add(new MemoryByte(memOffset + reloffset + i));
                    }

                    if(para.ParameterTypeObject.SizeInBit > 7)
                    {
                        int sizeInByte = (int)Math.Ceiling(para.ParameterTypeObject.SizeInBit / 8.0);
                        for(int i = 0; i < sizeInByte;i++)
                            mod.Memory.Bytes[para.Offset+i].SetBytesUsed(8,0);
                    } else {
                        mod.Memory.Bytes[para.Offset].SetBytesUsed(para.ParameterTypeObject.SizeInBit, para.OffsetBit);
                    }
                }

                foreach(Parameter para in mod.Parameters.Where(p => p.MemoryId == mem.UId && p.Offset == -1))
                {
                    para.Offset = GetFreeOffset(mod.Memory, memOffset, para.ParameterTypeObject.SizeInBit);
                    para.OffsetBit = SetBytes(mod.Memory, para.ParameterTypeObject.SizeInBit, para.Offset);
                }
            }



            if(mem.IsAutoPara)
            {
                foreach(Parameter para in paras.Where(p => p.MemoryId == mem.UId && p.Offset == -1))
                {
                    para.Offset = GetFreeOffset(mem, memOffset, para.ParameterTypeObject.SizeInBit);
                    para.OffsetBit = SetBytes(mem, para.ParameterTypeObject.SizeInBit, para.Offset);
                }

                foreach (Union union in ver.Unions.Where(u => u.MemoryId == mem.UId && u.Offset == -1))
                {
                    union.Offset = GetFreeOffset(mem, memOffset, union.SizeInBit);
                    union.OffsetBit = SetBytes(mem, union.SizeInBit, union.Offset);
                }
            }
            
            



            if (mem.IsAutoSize)
                mem.Size = mem.Bytes.Count;
        }

        private static int SetBytes(Memory mem, int size, int offset)
        {
            int offsetbit = 0;
            int sizeInByte = (int)Math.Ceiling(size / 8.0);
            if(size > 7)
            {
                for(int i = 0; i < sizeInByte; i++)
                {
                    mem.Bytes[offset+i].SetBytesUsed(8);
                }
                offsetbit = 0;
            } else {
                offsetbit = mem.Bytes[offset].SetBytesUsed(size);
            }
            return offsetbit;
        }

        private static int GetFreeOffset(Memory mem, int memOffset, int size)
        {
            int offset = -1;
            int sizeInByte = (int)Math.Ceiling(size / 8.0);

            for(int i = 0; i < mem.Bytes.Count; i++)
            {
                int availibleSize = 0;
                for(int x = 0; x < sizeInByte; x++)
                {
                    if((i+x) > (mem.Bytes.Count-1)) break;
                    availibleSize += mem.Bytes[i+x].GetFreeBits();
                }

                if(availibleSize >= size)
                {
                    offset = i;
                    break;
                }
            }

            if(offset == -1)
            {
                offset = mem.Bytes.Count;
                for(int i = 0; i < sizeInByte; i++)
                    mem.Bytes.Add(new MemoryByte(memOffset + offset + i));
            }

            return offset;
        }

        public static void ParameterTypeCalculations(AppVersion ver)
        {
            foreach(Models.ParameterType ptype in ver.ParameterTypes)
            {
                if(ptype.IsSizeManual) continue;

                switch(ptype.Type)
                {
                    case Models.ParameterTypes.Text:
                        throw new Exception($"ParameterTyp Größe für Text wurde nicht implementiert: {ptype.Name} ({ptype.UId})");
                    
                    case Models.ParameterTypes.Enum:
                    {
                        int maxValue = -1;
                        foreach(Models.ParameterTypeEnum penum in ptype.Enums)
                            if(penum.Value > maxValue)
                                maxValue = penum.Value;
                        if(maxValue >2)
                            ptype.SizeInBit = (int)Math.Ceiling(Math.Log2(maxValue));
                        else
                            ptype.SizeInBit = Convert.ToString(maxValue, 2).Length;
                        break;
                    }

                    case Models.ParameterTypes.NumberUInt:
                    {       
                        ptype.SizeInBit = (int)Math.Ceiling(Math.Log2(ptype.Max));
                        break;
                    }

                    case Models.ParameterTypes.NumberInt:
                    {
                        double b = Math.Log2(ptype.Min * (-1));
                        int bin1 = (int)Math.Ceiling(b) + 1;
                        int bin2 = Convert.ToString(ptype.Max, 2).Length;
                        ptype.SizeInBit = (bin1 > bin2) ? bin1 : bin2;
                        break;
                    }

                    case Models.ParameterTypes.Float9:
                        throw new Exception($"ParameterTyp Größe für Float9 wurde nicht implementiert: {ptype.Name} ({ptype.UId})");
                    
                    case Models.ParameterTypes.Picture:
                        throw new Exception($"ParameterTyp Größe für Picture kann nicht berechnet werden: {ptype.Name} ({ptype.UId})");
                    
                    case Models.ParameterTypes.None:
                        throw new Exception($"ParameterTyp Größe für None kann nicht berechnet werden: {ptype.Name} ({ptype.UId})");
                    
                    case Models.ParameterTypes.IpAddress:
                        throw new Exception($"ParameterTyp Größe für IpAddress kann nicht berechnet werden: {ptype.Name} ({ptype.UId})");
                    
                    default:
                        throw new Exception($"Unbekannter ParameterTyp zum Berechnen der Größe: {ptype.Name} ({ptype.UId})");
                }
            }
        }

        public static int GetNextFreeUId(object list, int start = 1) {
            int id = start;

            if(list is System.Collections.ObjectModel.ObservableCollection<Parameter>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Parameter>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<ParameterRef>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<ParameterRef>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<ComObject>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<ComObject>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<ComObjectRef>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<ComObjectRef>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<Memory>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Memory>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<ParameterType>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<ParameterType>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<Union>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Union>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<Module>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Module>).Any(i => i.UId == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<Argument>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Argument>).Any(i => i.UId == id))
                    id++;
            } else {
                throw new Exception("Can't get NextFreeUId. Type not implemented.");
            }
            return id;
        }

        public static int GetNextFreeId(object list, int start = 1) {
            int id = start;

            if(list is System.Collections.ObjectModel.ObservableCollection<Parameter>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Parameter>).Any(i => i.Id == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<ParameterRef>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<ParameterRef>).Any(i => i.Id == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<ComObject>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<ComObject>).Any(i => i.Id == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<ComObjectRef>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<ComObjectRef>).Any(i => i.Id == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<Argument>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Argument>).Any(i => i.Id == id))
                    id++;
            }else if(list is System.Collections.ObjectModel.ObservableCollection<Module>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Module>).Any(i => i.Id == id))
                    id++;
            }
            return id;
        }
    }
}
