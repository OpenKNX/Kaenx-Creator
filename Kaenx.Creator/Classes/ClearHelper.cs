using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;

namespace Kaenx.Creator.Classes
{
    public static class ClearHelper
    {
        
        public static ClearResult ShowUnusedElements(IVersionBase vers, ClearResult result = null)
        {
            List<int> uids = new List<int>();
            if(result == null)
                result = new ClearResult();

            foreach(Parameter para in vers.Parameters)
                if(!uids.Contains(para.ParameterType))
                    uids.Add(para.ParameterType);
            foreach(Module mod in vers.Modules)
                    foreach(Parameter para in mod.Parameters)
                    if(!uids.Contains(para.ParameterType))
                        uids.Add(para.ParameterType);    
            if(vers is AppVersion aver)
            {
                foreach(ParameterType ptype in aver.ParameterTypes)
                {
                    ptype.IsNotUsed = !uids.Contains(ptype.UId);
                    if(ptype.IsNotUsed)
                        result.ParameterTypes++;
                }
            }

            CheckParameter(vers, result);
            CheckComObject(vers, result);
            CheckUnion(vers, result);
            
            foreach(Module mod in vers.Modules)
                ShowUnusedElements(mod, result);

            return result;
        }

        public static void RemoveUnusedElements(IVersionBase vers)
        {
            if(vers is AppVersion avers)
            {
                foreach(ParameterType pt in avers.ParameterTypes.Where(p => p.IsNotUsed).ToList())
                    avers.ParameterTypes.Remove(pt);
            }

            RemoveElements(vers);

            foreach(Module mod in vers.Modules)
                RemoveElements(mod);
        }

        private static void RemoveElements(IVersionBase vbase)
        {
            foreach(Parameter p in vbase.Parameters.Where(p => p.IsNotUsed).ToList())
                vbase.Parameters.Remove(p);

            foreach(ParameterRef pr in vbase.ParameterRefs.Where(p => p.IsNotUsed).ToList())
                vbase.ParameterRefs.Remove(pr);

            foreach(ComObject c in vbase.ComObjects.Where(c => c.IsNotUsed).ToList())
                vbase.ComObjects.Remove(c);

            foreach(ComObjectRef cr in vbase.ComObjectRefs.Where(c => c.IsNotUsed).ToList())
                vbase.ComObjectRefs.Remove(cr);

            foreach(Union u in vbase.Unions.Where(u => u.IsNotUsed).ToList())
                vbase.Unions.Remove(u);
        }

        private static void CheckParameter(IVersionBase vbase, ClearResult res)
        {
            List<int> uids = new List<int>();
            foreach(ParameterRef pref in vbase.ParameterRefs)
                if(!uids.Contains(pref.Parameter))
                    uids.Add(pref.Parameter);
            foreach(Parameter para in vbase.Parameters)
            {
                para.IsNotUsed = !uids.Contains(para.UId);
                if(para.IsNotUsed) res.Parameters++;
            }

            uids.Clear();
            GetIDs(vbase.Dynamics[0], uids, true);
            foreach(ParameterRef pref in vbase.ParameterRefs)
            {
                pref.IsNotUsed = !uids.Contains(pref.UId);
                if(pref.IsNotUsed) res.ParameterRefs++;
            }
        }
        
        private static void CheckComObject(IVersionBase vbase, ClearResult res)
        {
            List<int> uids = new List<int>();
            foreach(Models.ComObjectRef cref in vbase.ComObjectRefs)
                if(!uids.Contains(cref.ComObject))
                    uids.Add(cref.ComObject);
            foreach(Models.ComObject com in vbase.ComObjects)
            {
                com.IsNotUsed = !uids.Contains(com.UId);
                if(com.IsNotUsed) res.ComObjects++;
            }
                
            uids.Clear();
            GetIDs(vbase.Dynamics[0], uids, false);
            foreach(ComObjectRef rcom in vbase.ComObjectRefs)
            {
                rcom.IsNotUsed = !uids.Contains(rcom.UId);
                if(rcom.IsNotUsed) 
                res.ComObjectRefs++;
            }
        }

        private static void CheckUnion(IVersionBase vbase, ClearResult res)
        {
            List<int> uids = new List<int>();
            foreach(Parameter para in vbase.Parameters)
                if(para.IsInUnion && para.UnionId != -1)
                    uids.Add(para.UnionId);
                    
            foreach(Union union in vbase.Unions)
            {
                union.IsNotUsed = !uids.Contains(union.UId);
                if(union.IsNotUsed) res.Unions++;
            }
        }

        public static void GetIDs(IDynItems dyn, List<int> uids, bool isPara)
        {
            foreach(IDynItems item in dyn.Items)
            {
                switch(item)
                {
                    case DynChannel:
                    case DynChannelIndependent:
                    case IDynWhen:
                        GetIDs(item, uids, isPara);
                        break;

                    case IDynChoose dc:
                        if(isPara && dc.ParameterRefObject != null)
                            uids.Add(dc.ParameterRefObject.UId);
                        GetIDs(item, uids, isPara);
                        break;
                        
                    case DynParaBlock db:
                        if(isPara && db.UseParameterRef && db.ParameterRefObject != null) 
                            uids.Add(db.ParameterRefObject.UId);
                        if(isPara && db.UseTextParameter && db.TextRefObject != null) 
                            uids.Add(db.TextRefObject.UId);
                        GetIDs(item, uids, isPara);
                        break;

                    case DynParameter dp:
                        if(isPara) uids.Add(dp.ParameterRef);
                        break;

                    case DynComObject dc:
                        if(!isPara) uids.Add(dc.ComObjectRef);
                        break;
                }
            }
        }

        public static void ClearIDs(IDynItems dyn, object ditem)
        {
            foreach(IDynItems item in dyn.Items)
            {
                switch(item)
                {
                    case DynChannelIndependent:
                    case IDynWhen:
                        ClearIDs(item, ditem);
                        break;

                    case DynChannel dch:
                        if(dch.ParameterRefObject == ditem)
                            dch.ParameterRefObject = null;
                        ClearIDs(item, ditem);
                        break;

                    case IDynChoose dc:
                        if(dc.ParameterRefObject == ditem)
                            dc.ParameterRefObject = null;
                        ClearIDs(item, ditem);
                        break;
                        
                    case DynParaBlock db:
                        if(db.ParameterRefObject == ditem)
                            db.ParameterRefObject = null;
                        if(db.TextRefObject == ditem)
                            db.TextRefObject = null;
                        ClearIDs(item, ditem);
                        break;

                    case DynParameter dp:
                        if(dp.ParameterRefObject == ditem)
                            dp.ResetParameterRefObject();
                        break;

                    case DynComObject dc:
                        if(dc.ComObjectRefObject == ditem)
                            dc.ComObjectRefObject = null;
                        break;
                }
            }
        }
    
        public static void ResetParameterIds(IVersionBase vbase)
        {
            foreach(Parameter para in vbase.Parameters)
                para.Id = -1;

            foreach(ParameterRef pref in vbase.ParameterRefs)
                pref.Id = -1;

            if(vbase.Modules.Count > 0)
            {
                foreach(Module mod in vbase.Modules)
                    ResetParameterIds(mod);
            }
        }
    }
}
