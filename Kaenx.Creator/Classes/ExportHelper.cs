using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml.Linq;

namespace Kaenx.Creator.Classes
{
    public class ExportHelper
    {

        public void ExportEts(ModelGeneral general)
        {
            string Manu = "M-" + general.ManufacturerId.ToString("X4");

            if (!System.IO.Directory.Exists(GetRelPath("")))
                System.IO.Directory.CreateDirectory(GetRelPath(""));

            FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            string toolVersion = fileVersion.FileVersion;

            Dictionary<string, string> ProductIds = new Dictionary<string, string>();
            Dictionary<string, string> HardwareIds = new Dictionary<string, string>();

            XElement xmanu = new XElement(Get("Manufacturer"));
            xmanu.SetAttributeValue("RefId", Manu);

            XDocument doc = new XDocument(new XElement(Get("KNX")));
            doc.Root.Add(new XElement(Get("ManufacturerData"), xmanu));

            #region XML Applications
            XElement xapps = new XElement(Get("ApplicationPrograms"));
            xmanu.Add(xapps);

            foreach (Application app in general.Applications)
            {
                string appName = Manu + "_A-" + app.Number.ToString("X4");

                foreach(AppVersion ver in app.Versions)
                {
                    string appVersion = appName + "-" + ver.Number.ToString("X2");
                    string hash = "0000";
                    appVersion += "-" + hash;


                    XElement xunderapp = new XElement(Get("Static"));
                    XElement xapp = new XElement(Get("ApplicationProgram"), xunderapp);
                    xapps.Add(xapp);
                    xapp.SetAttributeValue("Id", appVersion);
                    xapp.SetAttributeValue("ApplicationNumber", app.Number.ToString());
                    xapp.SetAttributeValue("ApplicationVersion", ver.Number.ToString());
                    xapp.SetAttributeValue("ProgramType", "ApplicationProgram");
                    xapp.SetAttributeValue("MaskVersion", "MV-07B0");
                    xapp.SetAttributeValue("Name", app.Name);
                    xapp.SetAttributeValue("DefaultLanguage", "de-DE");
                    xapp.SetAttributeValue("LoadProcedureStyle", "MergedProcedure");
                    xapp.SetAttributeValue("PeiType", "0");
                    xapp.SetAttributeValue("MinEtsVersion", "5.0");

                    Dictionary<string, string> MemIds = new Dictionary<string, string>();
                    Dictionary<string, string> ParamTypeIds = new Dictionary<string, string>();
                    Dictionary<string, string> ParamIds = new Dictionary<string, string>();
                    XElement temp;

                    #region Segmente
                    temp = new XElement(Get("Code"));
                    xunderapp.Add(temp);
                    foreach(Memory mem in ver.Memories)
                    {
                        XElement xmem = null;
                        string id = "";
                        switch (mem.Type)
                        {
                            case MemoryTypes.Absolute:
                                xmem = new XElement(Get("AbsoluteSegment"));
                                id = appVersion + "_AS-" + mem.Address.ToString("X4");
                                xmem.SetAttributeValue("Id", id);
                                xmem.SetAttributeValue("Address", mem.Address);
                                //xmem.Add(new XElement(Get("Data"), "Hier kommt toller Base64 String hin"));
                                break;

                            case MemoryTypes.Relative:
                                xmem = new XElement(Get("RelativeSegment"));
                                id = appVersion + "_RS-04-0000"; //TODO LoadStateMachine angeben
                                xmem.SetAttributeValue("Id", id);
                                xmem.SetAttributeValue("Name", mem.Name);
                                xmem.SetAttributeValue("Offset", 0);
                                xmem.SetAttributeValue("LoadStateMachine", "4");
                                break;
                        }

                        if (xmem == null) continue;
                        xmem.SetAttributeValue("Size", mem.Size);
                        temp.Add(xmem);
                        MemIds.Add(mem.Name, id);
                    }
                    #endregion

                    #region ParamTypes
                    temp = new XElement(Get("ParameterTypes"));
                    foreach(ParameterType type in ver.ParameterTypes)
                    {
                        string id = appVersion + "_PT-" + GetEncoded(type.Name);
                        ParamTypeIds.Add(type.Name, id);
                        XElement xtype = new XElement(Get("ParameterType"));
                        xtype.SetAttributeValue("Id", id);
                        xtype.SetAttributeValue("Name", type.Name);
                        XElement xcontent = null;

                        switch (type.Type)
                        {
                            case ParameterTypes.None:
                                xcontent = new XElement(Get("TypeNone"));
                                break;

                            case ParameterTypes.Text:
                                xcontent = new XElement(Get("TypeText"));
                                break;

                            case ParameterTypes.NumberInt:
                            case ParameterTypes.NumberUInt:
                                xcontent = new XElement(Get("TypeNumber"));
                                xcontent.SetAttributeValue("minInclusive", type.Min);
                                xcontent.SetAttributeValue("maxInclusive", type.Max);
                                xcontent.SetAttributeValue("Type", type.Type == ParameterTypes.NumberUInt ? "unsignedInt" : "signedInt");
                                break;

                            case ParameterTypes.Enum:
                                xcontent = new XElement(Get("TypeRestriction"));
                                xcontent.SetAttributeValue("Base", "Value");
                                int c = 0;
                                foreach(ParameterTypeEnum enu in type.Enums)
                                {
                                    XElement xenu = new XElement(Get("Enumeration"));
                                    xenu.SetAttributeValue("Id", id + "_EN-" + c);
                                    xenu.SetAttributeValue("DisplayOrder", c.ToString());
                                    xenu.SetAttributeValue("Text", enu.Name);
                                    xenu.SetAttributeValue("Value", enu.Value);
                                    xcontent.Add(xenu);
                                    c++;
                                }
                                break;

                            default:
                                throw new Exception("Unbekannter Parametertyp: " + type.Type);
                        }

                        if(xcontent != null && xcontent.Name.LocalName != "TypeNone")
                            xcontent.SetAttributeValue("SizeInBit", type.SizeInBit);
                        if (xcontent != null)
                            xtype.Add(xcontent);
                        temp.Add(xtype);
                    }
                    xunderapp.Add(temp);
                    #endregion

                    #region Parameter
                    temp = new XElement(Get("Parameters"));

                    int paraCount = 1;
                    foreach(Parameter para in ver.Parameters)
                    {
                        XElement xpara = new XElement(Get("Parameter"));
                        string id = appVersion + "_P-" + paraCount++;
                        ParamIds.Add(para.Name, id);
                        xpara.SetAttributeValue("Id", id);
                        xpara.SetAttributeValue("Name", para.Name);
                        xpara.SetAttributeValue("ParameterType", ParamTypeIds[para.ParameterType]);
                        xpara.SetAttributeValue("Text", para.Text);
                        if (para.Access != ParamAccess.Default) xpara.SetAttributeValue("Access", para.Access);
                        if (!string.IsNullOrWhiteSpace(para.Suffix)) xpara.SetAttributeValue("SuffixText", para.Suffix);
                        xpara.SetAttributeValue("Value", para.Value);

                        if (para.IsInMemory)
                        {
                            XElement xparamem = new XElement(Get("Memory"));
                            xparamem.SetAttributeValue("CodeSegment", MemIds[para.Memory]);
                            xparamem.SetAttributeValue("Offset", para.Offset);
                            xparamem.SetAttributeValue("BitOffset", para.OffsetBit);
                            xpara.Add(xparamem);
                        }

                        temp.Add(xpara);
                    }

                    xunderapp.Add(temp);
                    #endregion

                    //Todo add Unions

                    #region ParameterRefs
                    temp = new XElement(Get("ParameterRefs"));

                    int refCount = 1;
                    foreach(ParameterRef pref in ver.ParameterRefs)
                    {
                        if (pref.ParameterObject == null) continue;
                        XElement xpref = new XElement(Get("ParameterRef"));
                        string refid = ParamIds[pref.ParameterObject.Name];
                        xpref.SetAttributeValue("Id", refid + "_R-" + refCount);
                        xpref.SetAttributeValue("RefId", refid);
                        pref.RefId = refid + "_R-" + refCount;
                        temp.Add(xpref);
                        refCount++;
                    }

                    xunderapp.Add(temp);
                    #endregion

                    #region ComObjectTable
                    temp = new XElement(Get("ComObjectTable"));
                    xunderapp.Add(temp);
                    #endregion

                    #region ComObjectRefs
                    temp = new XElement(Get("ComObjectRefs"));
                    xunderapp.Add(temp);
                    #endregion

                    #region Tables
                    temp = new XElement(Get("AddressTable"));
                    temp.SetAttributeValue("MaxEntries", "65535");
                    xunderapp.Add(temp);
                    temp = new XElement(Get("AssociationTable"));
                    temp.SetAttributeValue("MaxEntries", "65535");
                    xunderapp.Add(temp);
                    temp = XDocument.Parse("<LoadProcedures><LoadProcedure MergeId=\"2\"><LdCtrlRelSegment LsmIdx=\"4\" Size=\"1\" Mode=\"0\" Fill=\"0\" AppliesTo=\"full\" /></LoadProcedure><LoadProcedure MergeId=\"4\"><LdCtrlWriteRelMem ObjIdx=\"4\" Offset=\"0\" Size=\"1\" Verify=\"true\" /></LoadProcedure></LoadProcedures>").Root;
                    xunderapp.Add(temp);
                    #endregion

                    xunderapp = new XElement(Get("Dynamic"));
                    xapp.Add(xunderapp);

                    HandleSubItems(ver.Dynamics[0], xunderapp);
                }
            }


            #endregion

            #region XML Hardware
            XElement xhards = new XElement(Get("Hardware"));
            xmanu.Add(xhards);


            int hardCount = 1;
            foreach (Hardware hard in general.Hardware)
            {
                string hid = Manu + "_H-" + GetEncoded(hard.SerialNumber) + "-" + hardCount++;
                XElement xhard = new XElement(Get("Hardware"));
                xhard.SetAttributeValue("Id", hid);
                xhard.SetAttributeValue("Name", hard.Name);
                xhard.SetAttributeValue("SerialNumber", hard.SerialNumber);
                xhard.SetAttributeValue("VersionNumber", hard.Version.ToString());
                xhard.SetAttributeValue("BusCurrent", hard.BusCurrent);
                if(hard.HasIndividualAddress) xhard.SetAttributeValue("HasIndividualAddress", "1");
                if(hard.HasApplicationProgram) xhard.SetAttributeValue("HasApplicationProgam", "1");
                if(hard.HasApplicationProgram2) xhard.SetAttributeValue("HasApplicationProgam2", "1");
                if(hard.IsPowerSupply) xhard.SetAttributeValue("IsPowerSupply", "1");
                xhard.SetAttributeValue("IsChocke", "0"); //Todo check what this is
                if(hard.IsCoppler) xhard.SetAttributeValue("IsCoupler", "1");
                xhard.SetAttributeValue("IsPowerLineRepeater", "0");
                xhard.SetAttributeValue("IsPowerLineSignalFilter", "0");
                if(hard.IsPowerSupply) xhard.SetAttributeValue("IsPowerSupply", "1");
                xhard.SetAttributeValue("IsCable", "0"); //Todo check if means PoweLine Cable
                if(hard.IsIpEnabled) xhard.SetAttributeValue("IsIPEnabled", "1");

                XElement xprods = new XElement(Get("Products"));
                xhard.Add(xprods);
                foreach(Device dev in hard.Devices){
                    XElement xprod = new XElement(Get("Product"));
                    string pid = hid + "_P-" + GetEncoded(dev.OrderNumber);
                    ProductIds.Add(dev.Name, pid);
                    xprod.SetAttributeValue("Id", pid);
                    xprod.SetAttributeValue("Text", dev.Text);
                    xprod.SetAttributeValue("OrderNumber", dev.OrderNumber);
                    xprod.SetAttributeValue("IsRailMounted", dev.IsRailMounted ? "1" : "0");
                    xprod.SetAttributeValue("DefaultLanguage", "de-DE");
                    xprod.Add(new XElement(Get("RegistrationInfo"), new XAttribute("RegistrationStatus", "Registered")));
                    xprods.Add(xprod);
                }


                XElement xasso = new XElement(Get("Hardware2Programs"));
                xhard.Add(xasso);

                foreach(Application app in hard.Apps){
                    foreach(AppVersion ver in app.Versions){
                        string appidx = app.Number.ToString("X4") + "-" + ver.Number.ToString("X2") + "-0000"; //Todo check hash

                        XElement xh2p = new XElement(Get("Hardware2Program"));
                        xh2p.SetAttributeValue("Id", hid + "_HP-" + appidx);
                        xh2p.SetAttributeValue("MediumTypes", "MT-0");

                        HardwareIds.Add(hard.Version + "-" + app.Number + "-" + ver.Number, hid + "_HP-" + appidx);

                        xh2p.Add(new XElement(Get("ApplicationProgramRef"), new XAttribute("RefId", Manu + "_A-" + appidx)));

                        XElement xreginfo = new XElement(Get("RegistrationInfo"));
                        xreginfo.SetAttributeValue("RegistrationStatus", "Registered");
                        xreginfo.SetAttributeValue("RegistrationNumber", "0001/" + hard.Version + ver.Number);
                        xh2p.Add(xreginfo);
                        xasso.Add(xh2p);
                    }
                }

                xhards.Add(xhard);
            }
            #endregion

            #region XML Catalog

            XElement cat = new XElement(Get("Catalog"));
            foreach (CatalogItem item in general.Catalog[0].Items)
            {
                GetCatalogItems(item, cat, ProductIds, HardwareIds);
            }
            xmanu.Add(cat);
            #endregion

            doc.Save(GetRelPath("temp.xml"));
        }


        #region Create Dyn Stuff

        private void HandleSubItems(IDynItems parent, XElement xparent)
        {
            foreach (IDynItems item in parent.Items)
            {
                XElement xitem = null;

                switch (item)
                {
                    case DynChannelIndependet dci:
                        xitem = Handle(dci, xparent);
                        break;

                    case DynParaBlock dpb:
                        xitem = HandleBlock(dpb, xparent);
                        break;

                    case DynParameter dp:
                        HandleParam(dp, xparent);
                        break;

                    case DynChoose dch:
                        xitem = HandleChoose(dch, xparent);
                        break;

                    case DynWhen dw:
                        xitem = HandleWhen(dw, xparent);
                        break;
                }

                if (item.Items != null && xitem != null)
                    HandleSubItems(item, xitem);
            }
        }


        private XElement Handle(IDynItems ch, XElement parent)
        {
            XElement channel = new XElement(Get("ChannelIndependentBlock"));
            parent.Add(channel);

            if(ch is DynChannel)
            {
                DynChannel dch = ch as DynChannel;
                channel.Name = Get("Channel");
                if(dch.ParameterRefObject != null)
                    channel.SetAttributeValue("ParamRefId", dch.ParameterRefObject.RefId);
            }

            channel.SetAttributeValue("Name", ch.Name);
            channel.SetAttributeValue("Text", ""); //Todo einfügen

            return channel;
        }

        private XElement HandleChoose(DynChoose cho, XElement parent)
        {
            XElement xcho = new XElement(Get("choose"));
            parent.Add(xcho);
            xcho.SetAttributeValue("ParamRefId", cho.ParameterRefObject.RefId);
            return xcho;
        }

        private XElement HandleWhen(DynWhen when, XElement parent)
        {
            XElement xwhen = new XElement(Get("when"));
            parent.Add(xwhen);

            when.Condition = when.Condition.Replace(">", "&gt;");
            when.Condition = when.Condition.Replace("<", "&lt;");


            if (when.IsDefault)
                xwhen.SetAttributeValue("default", "true");
            else
                xwhen.SetAttributeValue("test", when.Condition);

            return xwhen;
        }

        private XElement HandleBlock(DynParaBlock bl, XElement parent)
        {
            XElement block = new XElement(Get("ParameterBlock"));
            parent.Add(block);

            block.SetAttributeValue("Name", bl.Name);


            if (bl.ParameterRefObject != null)
                block.SetAttributeValue("ParamRefId", bl.ParameterRefObject.RefId);
            else
                block.SetAttributeValue("Text", bl.Text);

            return block;
        }

        private void HandleParam(DynParameter pa, XElement parent)
        {
            XElement xpara = new XElement(Get("ParameterRefRef"));
            parent.Add(xpara);
            xpara.SetAttributeValue("RefId", pa.ParameterRefObject.RefId);
        }
        #endregion


        private int catalogCounter = 1;
        private void GetCatalogItems(CatalogItem item, XElement parent, Dictionary<string, string> productIds, Dictionary<string, string> hardwareIds)
        {
            if(item.IsSection){
                XElement xitem = new XElement(Get("CatalogSection"));
                xitem.SetAttributeValue("Name", item.Name);
                xitem.SetAttributeValue("Number", catalogCounter++);
                xitem.SetAttributeValue("DefaultLanguage", "de-DE");
                parent.Add(xitem);
                foreach (CatalogItem sub in item.Items)
                    GetCatalogItems(sub, xitem, productIds, hardwareIds);
            } else {
                foreach(Device dev in item.Hardware.Devices) {
                    foreach(Application app in item.Hardware.Apps){
                        foreach(AppVersion ver in app.Versions){
                            XElement xitem = new XElement(Get("CatalogItem"));
                            xitem.SetAttributeValue("Name", dev.Text);
                            xitem.SetAttributeValue("Number", item.Hardware.SerialNumber);
                            if (!string.IsNullOrWhiteSpace(item.VisibleDescription)) xitem.SetAttributeValue("VisibleDescription", item.VisibleDescription);
                            xitem.SetAttributeValue("ProductRefId", productIds[dev.Name]);
                            string hardid = item.Hardware.Version + "-" + app.Number + "-" + ver.Number;
                            xitem.SetAttributeValue("Hardware2ProgramRefId", hardwareIds[hardid]);
                            xitem.SetAttributeValue("DefaultLanguage", "de-DE");
                            parent.Add(xitem);
                        }
                    }
                }
            }


        }

        public void SignOutput()
        {
            string etsPath = @"C:\Program Files (x86)\ETS5\CV\5.6.241.33672";
            string inputFile = GetRelPath("temp.xml");
            string outputFile = GetRelPath("output.knxprod");
            var asmPath = System.IO.Path.Combine(etsPath, "Knx.Ets.Converter.ConverterEngine.dll");
            var asm = Assembly.LoadFrom(asmPath);
            var eng = asm.GetType("Knx.Ets.Converter.ConverterEngine.ConverterEngine");
            var bas = asm.GetType("Knx.Ets.Converter.ConverterEngine.ConvertBase");

            //ConvertBase.Uninitialize();
            InvokeMethod(bas, "Uninitialize", null);

            //var dset = ConverterEngine.BuildUpRawDocumentSet( files );
            var dset = InvokeMethod(eng, "BuildUpRawDocumentSet", new object[] { new string[] { inputFile } });

            //ConverterEngine.CheckOutputFileName(outputFile, ".knxprod");
            InvokeMethod(eng, "CheckOutputFileName", new object[] { outputFile, ".knxprod" });

            //ConvertBase.CleanUnregistered = false;
            //SetProperty(bas, "CleanUnregistered", false);

            //dset = ConverterEngine.ReOrganizeDocumentSet(dset);
            dset = InvokeMethod(eng, "ReOrganizeDocumentSet", new object[] { dset });

            //ConverterEngine.PersistDocumentSetAsXmlOutput(dset, outputFile, null, string.Empty, true, _toolName, _toolVersion);
            InvokeMethod(eng, "PersistDocumentSetAsXmlOutput", new object[] { dset, outputFile, null,
                            "", true, "Kaenx.Creator", "0.0.1" });
        }

        private object InvokeMethod(Type type, string methodName, object[] args)
        {
            var mi = type.GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic);
            return mi.Invoke(null, args);
        }

        private string GetEncoded(string input)
        {
            StringBuilder sb = new StringBuilder();
            foreach (char c in input)
            {
                if ((c >32 && c < 48) || (c >57 && c < 65) || c == 95)
                {
                    // This character is too big for ASCII
                    string encodedValue = "." + ((int)c).ToString("X2");
                    sb.Append(encodedValue);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private string GetRelPath(string path)
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", path);
        }

        private string GetRelPath(string manu, string path)
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output", manu, path);
        }

        private XName Get(string name, string ns = "http://knx.org/xml/project/14")
        {
            return XName.Get(name, ns);
        }
    }
}
