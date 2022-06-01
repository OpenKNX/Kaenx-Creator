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

            mem.Sections.Clear();

            int memOffset = mem.Address;
            mem.StartAddress = mem.Address - (mem.Address % 16);

            if(ver.AddressMemoryObject == mem)
            {
                MemoryCalculationGroups(ver, mem);
            } else if(ver.AssociationMemoryObject == mem)
            {
                MemoryCalculationAssocs(ver, mem);
            } else {
                if(!mem.IsAutoSize)
                    mem.AddBytes(mem.Size);
                MemoryCalculationRegular(ver, mem);
            }
        }

        private static void MemoryCalculationGroups(AppVersion ver, Memory mem)
        {
            mem.AddBytes(mem.Size);
            int maxSize = (ver.AddressTableMaxCount+2) * 2;
            if(mem.Size < maxSize) maxSize = mem.Size;
            mem.SetBytesUsed(MemoryByteUsage.GroupAddress, maxSize, ver.AddressTableOffset);
        }

        private static void MemoryCalculationAssocs(AppVersion ver, Memory mem)
        {
            mem.AddBytes(mem.Size);
            int maxSize = (ver.AssociationTableMaxCount+1) * 2;
            if(mem.Size < maxSize) maxSize = mem.Size;
            mem.SetBytesUsed(MemoryByteUsage.Association, maxSize, ver.AssociationTableOffset);
        }

        private static void MemoryCalculationRegular(AppVersion ver, Memory mem)
        {
            List<Parameter> paras = ver.Parameters.Where(p => p.MemoryId == mem.UId && p.IsInUnion == false).ToList();

            if(!mem.IsAutoPara || (mem.IsAutoPara && !mem.IsAutoOrder))
            {
                foreach(Parameter para in paras.Where(p => p.Offset != -1))
                {
                    if(para.Offset >= mem.GetCount())
                    {
                        if(!mem.IsAutoSize) throw new Exception("Parameter liegt außerhalb des Speichers");
                        
                        int toadd = (para.Offset - mem.GetCount()) + 1;
                        if(para.ParameterTypeObject.SizeInBit > 8) toadd += (para.ParameterTypeObject.SizeInBit / 8) - 1;
                        mem.AddBytes(toadd);
                    }

                    mem.SetBytesUsed(para);
                }

                foreach (Union union in ver.Unions.Where(u => u.MemoryId == mem.UId && u.Offset != -1))
                {
                    if(union.Offset >= mem.GetCount())
                    {
                        if(!mem.IsAutoSize) throw new Exception("Parameter liegt außerhalb des Speichers");

                        int toadd = 1;
                        if(union.SizeInBit > 8) toadd = (union.Offset - mem.GetCount()) + (union.SizeInBit / 8);
                        mem.AddBytes(toadd);
                    }

                    mem.SetBytesUsed(union, ver.Parameters.Where(p => p.UnionId == union.UId).ToList());
                }

                foreach(Module mod in ver.Modules)
                {
                    mod.Memory.Sections.Clear();
                    //todo also check option isautoorder!
                    foreach(Parameter para in mod.Parameters.Where(p => p.MemoryId == mem.UId && p.Offset != -1))
                    {
                        if(para.Offset >= mod.Memory.GetCount())
                        {
                            int toadd = (para.Offset - mod.Memory.GetCount()) + 1;
                            if(para.ParameterTypeObject.SizeInBit > 8) toadd += (para.ParameterTypeObject.SizeInBit / 8) - 1;
                            int reloffset = mod.Memory.GetCount();
                            //for(int i = 0; i < toadd; i++)
                            //    mod.Memory.Bytes.Add(new MemoryByte(memOffset + reloffset + i));
                        }

                        if(para.ParameterTypeObject.SizeInBit > 7)
                        {
                            int sizeInByte = (int)Math.Ceiling(para.ParameterTypeObject.SizeInBit / 8.0);
                            //for(int i = 0; i < sizeInByte;i++)
                            //    mod.Memory.Bytes[para.Offset+i].SetBytesUsed(8,0);
                        } else {
                            //mod.Memory.Bytes[para.Offset].SetBytesUsed(para.ParameterTypeObject.SizeInBit, para.OffsetBit);
                        }
                    }

                    foreach(Parameter para in mod.Parameters.Where(p => p.MemoryId == mem.UId && p.Offset == -1))
                    {
                        (int offset, int offsetbit) result = mod.Memory.GetFreeOffset(para.ParameterTypeObject.SizeInBit);
                        para.Offset = result.offset;
                        para.OffsetBit = result.offsetbit;
                    }
                }
            }


            if(mem.IsAutoPara)
            {
                IEnumerable<Parameter> list1;
                if(mem.IsAutoOrder) list1 = paras.Where(p => p.MemoryId == mem.UId);
                else list1 = paras.Where(p => p.MemoryId == mem.UId && p.Offset == -1);
                foreach(Parameter para in list1)
                {
                    (int offset, int offsetbit) result = mem.GetFreeOffset(para.ParameterTypeObject.SizeInBit);
                    para.Offset = result.offset;
                    para.OffsetBit = result.offsetbit;
                    mem.SetBytesUsed(para);
                }

                IEnumerable<Union> list2;
                if(mem.IsAutoOrder) list2 = ver.Unions.Where(u => u.MemoryId == mem.UId);
                else list2 = ver.Unions.Where(u => u.MemoryId == mem.UId && u.Offset == -1);
                foreach (Union union in list2)
                {
                    (int offset, int offsetbit) result = mem.GetFreeOffset(union.SizeInBit);
                    union.Offset = result.offset;
                    union.OffsetBit = result.offsetbit;
                    mem.SetBytesUsed(union, ver.Parameters.Where(p => p.UnionId == union.UId).ToList());
                }
            }
            
            



            if (mem.IsAutoSize)
                mem.Size = mem.GetCount();
        }

        private static int SetBytes(Memory mem, int size, int offset)
        {
            int offsetbit = 0;
            int sizeInByte = (int)Math.Ceiling(size / 8.0);
            if(size > 7)
            {
                for(int i = 0; i < sizeInByte; i++)
                {
                    //mem.Bytes[offset+i].SetBytesUsed(8);
                }
                offsetbit = 0;
            } else {
                //offsetbit = mem.Bytes[offset].SetBytesUsed(size);
            }
            return offsetbit;
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
