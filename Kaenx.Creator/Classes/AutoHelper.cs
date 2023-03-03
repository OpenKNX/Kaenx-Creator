using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Kaenx.Creator.Classes
{
    public static class AutoHelper
    {

        public static AppVersion GetAppVersion(ModelGeneral general, AppVersionModel model)
        {
            AppVersion version = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.AppVersion>(model.Version, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Objects });
            LoadVersion(general, version, version);

            //TODO doesnt work anymore
            foreach(Models.ParameterType ptype in version.ParameterTypes)
            {
                if(ptype.Type == Models.ParameterTypes.Picture && ptype._baggageUId != -1)
                    ptype.BaggageObject = general.Baggages.SingleOrDefault(b => b.UId == ptype._baggageUId);

                if(ptype.Type == Models.ParameterTypes.Enum)
                {
                    foreach(ParameterTypeEnum penu in ptype.Enums)
                    {
                        if(penu._iconId != -1)
                            penu.IconObject = general.Icons.SingleOrDefault(i => i.UId == penu._iconId);
                    }
                }
            }

            return version;
        }
        
        private static Dictionary<long, Parameter> Paras;
        private static Dictionary<long, ParameterRef> ParaRefs;
        private static Dictionary<long, ComObject> Coms;
        private static Dictionary<long, ComObjectRef> ComRefs;

        private static void LoadVersion(ModelGeneral general, Models.AppVersion vbase, Models.IVersionBase mod)
        {
            Paras = new Dictionary<long, Parameter>();
            foreach(Parameter para in mod.Parameters)
                Paras.Add(para.UId, para);

            ParaRefs = new Dictionary<long, ParameterRef>();
            foreach(ParameterRef pref in mod.ParameterRefs)
                ParaRefs.Add(pref.UId, pref);

            Coms = new Dictionary<long, ComObject>();
            foreach(ComObject com in mod.ComObjects)
                Coms.Add(com.UId, com);

            ComRefs = new Dictionary<long, ComObjectRef>();
            foreach(ComObjectRef cref in mod.ComObjectRefs)
                ComRefs.Add(cref.UId, cref);



            if(vbase == mod) {
                if(vbase._addressMemoryId != -1)
                    vbase.AddressMemoryObject = vbase.Memories.SingleOrDefault(m => m.UId == vbase._addressMemoryId);

                if(vbase._assocMemoryId != -1)
                    vbase.AssociationMemoryObject = vbase.Memories.SingleOrDefault(m => m.UId == vbase._assocMemoryId);
                    
                if(vbase._comMemoryId != -1)
                    vbase.ComObjectMemoryObject = vbase.Memories.SingleOrDefault(m => m.UId == vbase._comMemoryId);
            } else {
                Models.Module modu = mod as Models.Module;
                if(modu._parameterBaseOffsetUId != -1)
                    modu.ParameterBaseOffset = modu.Arguments.SingleOrDefault(m => m.UId == modu._parameterBaseOffsetUId);
                
                if(modu._comObjectBaseNumberUId != -1)
                    modu.ComObjectBaseNumber = modu.Arguments.SingleOrDefault(m => m.UId == modu._comObjectBaseNumberUId);
            }

            foreach(Models.Parameter para in mod.Parameters)
            {
                if (para._memoryId != -1)
                    para.SaveObject = vbase.Memories.SingleOrDefault(m => m.UId == para._memoryId);
                    
                if (para._parameterType != -1)
                    para.ParameterTypeObject = vbase.ParameterTypes.SingleOrDefault(p => p.UId == para._parameterType);

                if(para.IsInUnion && para._unionId != -1)
                    para.UnionObject = mod.Unions.SingleOrDefault(u => u.UId == para._unionId);
            }

            foreach(Models.Union union in mod.Unions)
            {
                if (union._memoryId != -1)
                    union.MemoryObject = vbase.Memories.SingleOrDefault(u => u.UId == union._memoryId);
            }

            foreach(Models.ParameterRef pref in mod.ParameterRefs)
            {
                if (pref._parameter != -1)
                    pref.ParameterObject = Paras[pref._parameter];
            }

            foreach(Models.ComObject com in mod.ComObjects)
            {
                if (!string.IsNullOrEmpty(com._typeNumber))
                    com.Type = MainWindow.DPTs.Single(d => d.Number == com._typeNumber);
                    
                if(!string.IsNullOrEmpty(com._subTypeNumber) && com.Type != null)
                    com.SubType = com.Type.SubTypes.Single(d => d.Number == com._subTypeNumber);
                    
                if(vbase.IsComObjectRefAuto && com.UseTextParameter && com._parameterRef != -1)
                    com.ParameterRefObject = ParaRefs[com._parameterRef];
            }

            foreach(Models.ComObjectRef cref in mod.ComObjectRefs)
            {
                if(cref._comObject != -1)
                    cref.ComObjectObject = Coms[cref._comObject];

                if (!string.IsNullOrEmpty(cref._typeNumber))
                    cref.Type = MainWindow.DPTs.Single(d => d.Number == cref._typeNumber);
                    
                if(!string.IsNullOrEmpty(cref._subTypeNumber) && cref.Type != null)
                    cref.SubType = cref.Type.SubTypes.Single(d => d.Number == cref._subTypeNumber);

                if(!vbase.IsComObjectRefAuto && cref.UseTextParameter && cref._parameterRef != -1)
                    cref.ParameterRefObject = ParaRefs[cref._parameterRef];
            }

            if(mod is Models.Module mod2)
            {
                if(mod2._parameterBaseOffsetUId != -1)
                    mod2.ParameterBaseOffset = mod2.Arguments.SingleOrDefault(a => a.UId == mod2._parameterBaseOffsetUId);

                if(mod2._comObjectBaseNumberUId != -1)
                    mod2.ComObjectBaseNumber = mod2.Arguments.SingleOrDefault(a => a.UId == mod2._comObjectBaseNumberUId);
            }

            if(mod.Dynamics.Count > 0)
                LoadSubDyn(general, mod.Dynamics[0], vbase, mod);
            
            foreach(Models.Module mod3 in mod.Modules)
                LoadVersion(general, vbase, mod3);
        }

        private static void LoadSubDyn(ModelGeneral general, Models.Dynamic.IDynItems dyn, AppVersion vbase, IVersionBase mod)
        {
            foreach (Models.Dynamic.IDynItems item in dyn.Items)
            {
                item.Parent = dyn;

                switch(item)
                {
                    case Models.Dynamic.DynChannel dch:
                        if(dch.UseTextParameter)
                            dch.ParameterRefObject = ParaRefs[dch._parameter];
                        if(dch.UseIcon && dch._iconId != -1)
                            dch.IconObject = general.Icons.SingleOrDefault(i => i.UId == dch._iconId);
                        break;

                    case Models.Dynamic.DynParameter dp:
                        if (dp._parameter != -1)
                            dp.ParameterRefObject = ParaRefs[dp._parameter];
                        if(dp.HasHelptext)
                            dp.Helptext = vbase.Helptexts.SingleOrDefault(p => p.UId == dp._helptextId);
                        if(dp.UseIcon && dp._iconId != -1)
                            dp.IconObject = general.Icons.SingleOrDefault(i => i.UId == dp._iconId);
                        break;

                    case Models.Dynamic.DynChooseBlock dcb:
                        if (dcb._parameterRef != -1)
                            dcb.ParameterRefObject = ParaRefs[dcb._parameterRef];
                        break;

                    case Models.Dynamic.DynChooseChannel dcc:
                        if (dcc._parameterRef != -1)
                            dcc.ParameterRefObject = ParaRefs[dcc._parameterRef];
                        break;

                    case Models.Dynamic.DynComObject dco:
                        if (dco._comObjectRef != -1)
                            dco.ComObjectRefObject = ComRefs[dco._comObjectRef];
                        break;

                    case Models.Dynamic.DynParaBlock dpb:
                        if(dpb.UseParameterRef && dpb._parameterRef != -1)
                            dpb.ParameterRefObject = ParaRefs[dpb._parameterRef];
                        if(dpb.UseTextParameter && dpb._textRef != -1)
                            dpb.TextRefObject = ParaRefs[dpb._textRef];
                        if(dpb.UseIcon && dpb._iconId != -1)
                            dpb.IconObject = general.Icons.SingleOrDefault(i => i.UId == dpb._iconId);
                        break;

                    case Models.Dynamic.DynSeparator ds:
                        if(ds.UseIcon && ds._iconId != -1)
                            ds.IconObject = general.Icons.SingleOrDefault(i => i.UId == ds._iconId);
                        break;

                    case Models.Dynamic.DynModule dm:
                        if(dm._module != -1)
                        {
                            dm.ModuleObject = vbase.Modules.Single(m => m.UId == dm._module);
                            foreach(Models.Dynamic.DynModuleArg arg in dm.Arguments)
                            {
                                if(arg._argId != -1)
                                    arg.Argument = dm.ModuleObject.Arguments.Single(a => a.UId == arg._argId);
                                if(arg.UseAllocator && arg._allocId != -1)
                                    arg.Allocator = mod.Allocators.SingleOrDefault(a => a.UId == arg._allocId);
                            }
                        }
                        break;

                    case Models.Dynamic.DynAssign dass:
                        if(dass._targetUId != -1)
                            dass.TargetObject = ParaRefs[dass._targetUId];
                        if(string.IsNullOrEmpty(dass.Value) && dass._sourceUId != -1)
                            dass.SourceObject = ParaRefs[dass._sourceUId];
                        break;

                    case Models.Dynamic.DynRepeat dre:
                        if(dre.UseParameterRef && dre._parameterUId != -1)
                            dre.ParameterRefObject = ParaRefs[dre._parameterUId];
                        break;
                    
                    case Models.Dynamic.DynButton db:
                        if(db.UseTextParameter && db._textRef != -1)
                            db.TextRefObject = ParaRefs[db._textRef];
                        if(db.UseIcon && db._iconId != -1)
                            db.IconObject = general.Icons.SingleOrDefault(i => i.UId == db._iconId);
                        break;

                }

                if (item.Items != null)
                    LoadSubDyn(general, item, vbase, mod);
            }
        }

        public static void MemoryCalculation(AppVersion ver, Memory mem)
        {
            mem.Sections.Clear();

            if(mem.Type == MemoryTypes.Absolute)
                mem.StartAddress = mem.Address - (mem.Address % 16);
            else
            {
                mem.StartAddress = 0;
                mem.Address = 0;
            }

            foreach(Module mod in ver.Modules)
                mod.Memory.Sections.Clear();

            if(!mem.IsAutoSize)
                mem.AddBytes(mem.Size);

            if(mem.Type == MemoryTypes.Absolute)
            {
                if(ver.AddressMemoryObject == mem)
                    MemoryCalculationGroups(ver, mem);
                if(ver.AssociationMemoryObject == mem)
                    MemoryCalculationAssocs(ver, mem);
                if(ver.ComObjectMemoryObject == mem)
                    MemoryCalculationComs(ver, mem);
            }
            MemoryCalculationRegular(ver, mem);
        }

        private static void MemoryCalculationGroups(AppVersion ver, Memory mem)
        {
            int maxSize = (ver.AddressTableMaxCount+2) * 2;
            maxSize--; //TODO check why the heck it is smaller
            if(mem.IsAutoSize && (maxSize + ver.AddressTableOffset) > mem.GetCount())
                mem.AddBytes((maxSize + ver.AddressTableOffset) - mem.GetCount());
            //if(mem.Size < maxSize) maxSize = mem.Size;
            mem.SetBytesUsed(MemoryByteUsage.GroupAddress, maxSize, ver.AddressTableOffset);
        }

        private static void MemoryCalculationAssocs(AppVersion ver, Memory mem)
        {
            int maxSize = (ver.AssociationTableMaxCount+1) * 2;
            maxSize--;
            if(mem.IsAutoSize && (maxSize + ver.AssociationTableOffset) > mem.GetCount())
                mem.AddBytes((maxSize + ver.AssociationTableOffset) - mem.GetCount());
            //if(mem.Size < maxSize) maxSize = mem.Size;
            mem.SetBytesUsed(MemoryByteUsage.Association, maxSize, ver.AssociationTableOffset);
        }

        private static void MemoryCalculationComs(AppVersion ver, Memory mem)
        {

            int maxSize = (ver.ComObjects.Count * 3) + 2;
            if(mem.IsAutoSize && (maxSize + ver.ComObjectTableOffset) > mem.GetCount())
                mem.AddBytes((maxSize + ver.ComObjectTableOffset) - mem.GetCount());
            //if(mem.Size < maxSize) maxSize = mem.Size;
            mem.SetBytesUsed(MemoryByteUsage.Coms, maxSize, ver.ComObjectTableOffset);
        }

        private static void MemCalcStatics(IVersionBase vbase, Memory mem, int memId)
        {
            List<Parameter> paras = vbase.Parameters.Where(p => p.MemoryId == memId && p.IsInUnion == false && p.SavePath != SavePaths.Nowhere).ToList();

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

            foreach (Union union in vbase.Unions.Where(u => u.MemoryId == mem.UId && u.Offset != -1 && u.SavePath != SavePaths.Nowhere))
            {
                if(union.Offset >= mem.GetCount())
                {
                    if(!mem.IsAutoSize) throw new Exception("Parameter liegt außerhalb des Speichers");

                    int toadd = 1;
                    if(union.SizeInBit > 8) toadd = (union.Offset - mem.GetCount()) + (union.SizeInBit / 8);
                    mem.AddBytes(toadd);
                }

                mem.SetBytesUsed(union, vbase.Parameters.Where(p => p.UnionId == union.UId).ToList());
            }
        }

        private static void MemCalcAuto(IVersionBase vbase, Memory mem, int memId)
        {
            List<Parameter> paras = vbase.Parameters.Where(p => p.MemoryId == memId && p.IsInUnion == false && p.SavePath != SavePaths.Nowhere).ToList();
            IEnumerable<Parameter> list1;
            if(mem.IsAutoOrder) list1 = paras.ToList();
            else list1 = paras.Where(p => p.Offset == -1);
            foreach(Parameter para in list1)
            {
                (int offset, int offsetbit) result = mem.GetFreeOffset(para.ParameterTypeObject.SizeInBit);
                para.Offset = result.offset;
                para.OffsetBit = result.offsetbit;
                mem.SetBytesUsed(para);
            }

            IEnumerable<Union> list2;
            if(mem.IsAutoOrder) list2 = vbase.Unions.Where(u => u.MemoryId == memId && u.SavePath != SavePaths.Nowhere);
            else list2 = vbase.Unions.Where(u => u.MemoryId == memId && u.Offset == -1);
            foreach (Union union in list2)
            {
                (int offset, int offsetbit) result = mem.GetFreeOffset(union.SizeInBit);
                union.Offset = result.offset;
                union.OffsetBit = result.offsetbit;
                mem.SetBytesUsed(union, vbase.Parameters.Where(p => p.UnionId == union.UId).ToList());
            }
        }

        private static void MemoryCalculationRegular(AppVersion ver, Memory mem)
        {
            if(!mem.IsAutoPara || (mem.IsAutoPara && !mem.IsAutoOrder))
            {
                foreach(Module mod in ver.Modules)
                    MemCalcStatics(mod, mod.Memory, mem.UId);
                    
                MemCalcStatics(ver, mem, mem.UId);
            }

            if(mem.IsAutoPara)
            {
                foreach(Module mod in ver.Modules)
                    MemCalcAuto(mod, mod.Memory, mem.UId);

                MemCalcAuto(ver, mem, mem.UId);
            }

            List<Models.Dynamic.DynModule> mods = new List<Models.Dynamic.DynModule>();
            GetModules(ver.Dynamics[0], mods);
            int highestComNumber = ver.ComObjects.OrderByDescending(c => c.Number).FirstOrDefault()?.Number ?? -1;
            foreach(Models.Dynamic.DynModule dmod in mods)
            {
                Models.Dynamic.DynModuleArg argParas = dmod.Arguments.SingleOrDefault(a => a.ArgumentId == dmod.ModuleObject.ParameterBaseOffsetUId);
                if(argParas == null) continue;

                if(!mem.IsAutoPara || (mem.IsAutoPara && !mem.IsAutoOrder && !string.IsNullOrEmpty(argParas.Value)))
                {
                    int modSize = dmod.ModuleObject.Memory.GetCount();
                    int start = int.Parse(argParas.Value);
                    mem.SetBytesUsed(MemoryByteUsage.Module, modSize, start);
                }

                if(mem.IsAutoPara && (string.IsNullOrEmpty(argParas.Value) || mem.IsAutoOrder))
                {
                    int modSize = dmod.ModuleObject.Memory.GetCount();
                    (int offset, int offsetbit) result = mem.GetFreeOffset(modSize * 8);
                    argParas.Value = result.offset.ToString();
                    argParas.Argument.Allocates = modSize;
                    mem.SetBytesUsed(MemoryByteUsage.Module, modSize, result.offset);
                }

                if(dmod.ModuleObject.IsComObjectBaseNumberAuto)
                {
                    Models.Dynamic.DynModuleArg argComs = dmod.Arguments.SingleOrDefault(a => a.ArgumentId == dmod.ModuleObject.ComObjectBaseNumberUId);
                    if(argComs != null)
                    {
                        int highestComNumber2 = dmod.ModuleObject.ComObjects.OrderByDescending(c => c.Number).FirstOrDefault()?.Number ?? 0;
                        int lowestComNumber2 = dmod.ModuleObject.ComObjects.OrderBy(c => c.Number).FirstOrDefault()?.Number ?? 1;
                        argComs.Value = (++highestComNumber).ToString();

                        argComs.Argument.Allocates = highestComNumber2 - lowestComNumber2 + 1;
                        highestComNumber += highestComNumber2;
                    }
                }
            }

            if (mem.IsAutoSize)
                mem.Size = mem.GetCount();
        }

        public static void GetModules(Models.Dynamic.IDynItems item, List<Models.Dynamic.DynModule> mods, long repeater = 1)
        {
            if(item is Models.Dynamic.DynModule dm)
            {
                for(int i = 0; i < repeater; i++)
                    mods.Add(dm);
            }

            if(item.Items == null) return;

            long srepeat = repeater;
            if(item is Models.Dynamic.DynRepeat dr)
                srepeat = dr.Count;

            foreach(Models.Dynamic.IDynItems i in item.Items)
                GetModules(i, mods, srepeat);
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
            }else if(list is System.Collections.ObjectModel.ObservableCollection<Allocator>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Allocator>).Any(i => i.UId == id))
                    id++;
            } else if(list is System.Collections.ObjectModel.ObservableCollection<Baggage>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Baggage>).Any(i => i.UId == id))
                    id++;
            } else if(list is System.Collections.ObjectModel.ObservableCollection<Message>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Message>).Any(i => i.UId == id))
                    id++;
            } else if(list is System.Collections.ObjectModel.ObservableCollection<Helptext>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Helptext>).Any(i => i.UId == id))
                    id++;
            } else if(list is System.Collections.ObjectModel.ObservableCollection<Icon>) {
                while((list as System.Collections.ObjectModel.ObservableCollection<Icon>).Any(i => i.UId == id))
                    id++;
            } else {
                throw new Exception("Can't get NextFreeUId. Type not implemented.");
            }
            return id;
        }

        public static int GetNextFreeId(IVersionBase vbase, string list, int start = 1) {
            int id = start;

            if(list == "Parameters") {
                return ++vbase.LastParameterId;
            } else if(list == "ParameterRefs") {
                return ++vbase.LastParameterRefId;
            } else {
                var x = vbase.GetType().GetProperty(list).GetValue(vbase);
                if(x is System.Collections.ObjectModel.ObservableCollection<ComObject> lc) {
                    while(lc.Any(i => i.Id == id))
                        id++;
                }else if(x is System.Collections.ObjectModel.ObservableCollection<ComObjectRef> lcr) {
                    while(lcr.Any(i => i.Id == id))
                        id++;
                }else if(x is System.Collections.ObjectModel.ObservableCollection<Argument> la) {
                    while(la.Any(i => i.Id == id))
                        id++;
                }else if(x is System.Collections.ObjectModel.ObservableCollection<Module> lm) {
                    while(lm.Any(i => i.Id == id))
                        id++;
                }else if(x is System.Collections.ObjectModel.ObservableCollection<Message> ls) {
                    while(ls.Any(i => i.Id == id))
                        id++;
                }else if(x is System.Collections.ObjectModel.ObservableCollection<Allocator> lac) {
                    while(lac.Any(i => i.Id == id))
                        id++;
                }
                return id;
            }
        }
    
        public static void CheckIds(AppVersion version)
        {
            CheckIdsModule(version, version);
        }

        private static void CheckIdsModule(AppVersion version, IVersionBase vbase, IVersionBase vparent = null)
        {
            foreach(Parameter para in vbase.Parameters)
                if(para.Id == -1) para.Id = GetNextFreeId(vbase, "Parameters");

            foreach(ParameterRef pref in vbase.ParameterRefs)
                if(pref.Id == -1) pref.Id = GetNextFreeId(vbase, "ParameterRefs");

            foreach(ComObject com in vbase.ComObjects)
                if(com.Id == -1) com.Id = GetNextFreeId(vbase, "ComObjects", 0);

            foreach(ComObjectRef cref in vbase.ComObjectRefs)
                if(cref.Id == -1) cref.Id = GetNextFreeId(vbase, "ComObjectRefs", 0);

            if(vbase is Module mod)
            {
                if(mod.Id == -1)
                    mod.Id = GetNextFreeId(vparent, "Modules");

                foreach(Argument arg in mod.Arguments)
                    if(arg.Id == -1) arg.Id = GetNextFreeId(vbase, "Arguments");
            }

            counterBlock = 1;
            counterSeparator = 1;
            CheckDynamicIds(version.Dynamics[0]);

            foreach(Models.Module xmod in vbase.Modules)
                CheckIdsModule(version, xmod, vbase);
        }


        private static int counterBlock = 1;
        private static int counterSeparator = 1;
        public static void CheckDynamicIds(IDynItems parent)
        {
            foreach(IDynItems item in parent.Items)
            {
                switch(item)
                {
                    case DynParaBlock dpb:
                        dpb.Id = counterBlock++;
                        break;

                    case DynSeparator ds:
                        ds.Id = counterSeparator++;
                        break;
                }

                if(item.Items != null)
                    CheckDynamicIds(item);
            }
        }

        public static byte[] GetFileBytes(string file)
        {
            byte[] data;
            BitmapImage image = new BitmapImage(new Uri(file));
            BitmapEncoder encoder;

            switch(Path.GetExtension(file).ToLower())
            {
                case ".png":
                    encoder = new PngBitmapEncoder();
                    break;

                case ".jpg":
                case ".jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;

                default:
                    throw new Exception("Dataityp " + Path.GetExtension(file).ToLower() + " wird nicht unterstützt");
            }
            
            encoder.Frames.Add(BitmapFrame.Create(image));
            using (MemoryStream ms = new MemoryStream())
            {
                encoder.Save(ms);
                data = new byte[ms.Length];
                ms.ToArray().CopyTo(data, 0);
            }
            image = null;
            encoder.Frames.RemoveAt(0);
            encoder = null;
            
            return data;
        }
    
    }
}
