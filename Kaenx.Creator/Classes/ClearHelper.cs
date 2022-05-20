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
        
        public static void ShowUnusedElements(AppVersion vers)
        {
            List<int> uids = new List<int>();


            foreach(Parameter para in vers.Parameters)
                if(!uids.Contains(para.ParameterType))
                    uids.Add(para.ParameterType);
            foreach(Module mod in vers.Modules)
                    foreach(Parameter para in mod.Parameters)
                    if(!uids.Contains(para.ParameterType))
                        uids.Add(para.ParameterType);    
            foreach(ParameterType ptype in vers.ParameterTypes)
                ptype.IsNotUsed = !uids.Contains(ptype.UId);

            CheckParameter(vers);
            CheckComObject(vers);
            
            foreach(Module mod in vers.Modules)
            {
                CheckParameter(mod);
                CheckComObject(mod);
            }
        }

        private static void CheckParameter(IVersionBase vbase)
        {
            List<int> uids = new List<int>();
            foreach(ParameterRef pref in vbase.ParameterRefs)
                if(!uids.Contains(pref.Parameter))
                    uids.Add(pref.Parameter);
            foreach(Parameter para in vbase.Parameters)
                para.IsNotUsed = !uids.Contains(para.UId);

            uids.Clear();
            GetIDs(vbase.Dynamics[0], uids, true);
            foreach(ParameterRef pref in vbase.ParameterRefs)
                pref.IsNotUsed = !uids.Contains(pref.UId);
        }
        
        private static void CheckComObject(IVersionBase vbase)
        {
            List<int> uids = new List<int>();
            foreach(Models.ComObjectRef cref in vbase.ComObjectRefs)
                if(!uids.Contains(cref.ComObject))
                    uids.Add(cref.ComObject);
            foreach(Models.ComObject com in vbase.ComObjects)
                com.IsNotUsed = !uids.Contains(com.UId);
                
            uids.Clear();
            GetIDs(vbase.Dynamics[0], uids, false);
            foreach(ComObjectRef rcom in vbase.ComObjectRefs)
                rcom.IsNotUsed = !uids.Contains(rcom.UId);
        }

        private static void GetIDs(IDynItems dyn, List<int> uids, bool isPara)
        {
            foreach(IDynItems item in dyn.Items)
            {
                switch(item)
                {
                    case DynChannel:
                    case DynChannelIndependent:
                    case DynChoose:
                    case DynParaBlock:
                    case DynWhen:
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
    }
}
