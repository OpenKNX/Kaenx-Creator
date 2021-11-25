using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Kaenx.Creator.Models;

namespace Kaenx.Creator.Classes
{
    public static class AutoHelper
    {

        public static byte[] GetMemorySize(AppVersion ver, Memory mem)
        {
            List<byte> data;

            if (mem.IsAutoSize)
                data = new List<byte>();
            else
                data = new List<byte>(new byte[mem.Size]);

            foreach (Models.Parameter para in ver.Parameters.Where(p => p.Memory == mem.Name))
            {
                if (para.ParameterTypeObject.SizeInBit > 7)
                {
                    if (mem.IsAutoSize || para.IsOffsetAuto)
                    {
                        para.Offset = -1;
                        para.OffsetBit = 0;
                    }
                    for (int i = 0; i < (para.ParameterTypeObject.SizeInBit / 8); i++)
                    {

                        if (mem.IsAutoSize)
                        {
                            if(para.Offset == -1)
                            {
                                para.Offset = data.Count;
                                para.OffsetBit = 0;
                            }
                            data.Add(8);
                        }
                        else
                            data[para.Offset + i] += 8;
                    }
                }
                else
                {
                    if (mem.IsAutoSize || para.IsOffsetAuto)
                    {
                        bool flag = false;
                        for(int i = 0; i < data.Count; i++)
                        {
                            if(8 - data[i] >= para.ParameterTypeObject.SizeInBit)
                            {
                                para.Offset = i;
                                para.OffsetBit = data[i];
                                data[i] = Convert.ToByte(data[i] + para.ParameterTypeObject.SizeInBit);
                                flag = true;
                                break;
                            }
                        }
                        if (!flag)
                        {
                            para.Offset = data.Count;
                            para.OffsetBit = 0;
                            data.Add(Convert.ToByte(para.ParameterTypeObject.SizeInBit));
                        }
                    }
                    else
                    {
                        data[para.Offset] += Convert.ToByte(para.ParameterTypeObject.SizeInBit);
                    }
                }
            }

            if (mem.IsAutoSize)
            {
                mem.Size = data.Count;
            }


            return data.ToArray();
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
            }
            return id;
        }
    }
}
