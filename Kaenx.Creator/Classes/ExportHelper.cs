using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Web;
using System.Xml.Linq;

namespace Kaenx.Creator.Classes
{
    public class ExportHelper
    {

        public void ExportEts(ModelGeneral general)
        {
            string Manu = "M-" + Fill(general.ManufacturerId.ToString("X2"), 4);

            if (!System.IO.Directory.Exists(GetRelPath("")))
                System.IO.Directory.CreateDirectory(GetRelPath(""));

            if (System.IO.Directory.Exists(GetRelPath(Manu)))
                System.IO.Directory.Delete(GetRelPath(Manu), true);

            System.IO.Directory.CreateDirectory(GetRelPath(Manu));

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
                string appName = Manu + "_A-" + Fill(app.Number.ToString("X2"), 4);

                foreach(AppVersion ver in app.Versions)
                {
                    string appVersion = appName + "-" + Fill(ver.Number.ToString("X2"), 2);
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
                                id = appVersion + "_AS-" + Fill(mem.Address.ToString("X2"), 4);
                                xmem.SetAttributeValue("Id", id);
                                xmem.SetAttributeValue("Address", mem.Address);
                                xmem.Add(new XElement(Get("Data"), "Hier kommt toller Base64 String hin"));
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
                        XElement xpref = new XElement(Get("ParameterRef"));
                        string refid = ParamIds[pref.ParameterId];
                        xpref.SetAttributeValue("Id", refid + "_R-" + refCount);
                        xpref.SetAttributeValue("RefId", refid);
                        temp.Add(xpref);
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

                XElement xprod = new XElement(Get("Product"));
                string pid = hid + "_P-" + GetEncoded(hard.DeviceObject.OrderNumber);
                ProductIds.Add(hard.DeviceObject.Name, pid);
                xprod.SetAttributeValue("Id", pid);
                xprod.SetAttributeValue("Text", hard.DeviceObject.Text);
                xprod.SetAttributeValue("OrderNumber", hard.DeviceObject.OrderNumber);
                xprod.SetAttributeValue("IsRailMounted", hard.DeviceObject.IsRailMounted ? "1" : "0");
                xprod.SetAttributeValue("DefaultLanguage", "de-DE");
                xprod.Add(new XElement(Get("RegistrationInfo"), new XAttribute("RegistrationStatus", "Registered")));


                XElement xasso = new XElement(Get("Hardware2Programs"));

                foreach (HardwareApp happ in hard.Apps)
                {
                    string appidx = Fill(happ.AppObject.Number.ToString("X2"), 4) + "-" + Fill(happ.AppVersionObject.Number.ToString("X2"), 2) + "-0000"; //Todo check hash

                    XElement xh2p = new XElement(Get("Hardware2Program"));
                    xh2p.SetAttributeValue("Id", hid + "_HP-" + appidx);
                    xh2p.SetAttributeValue("MediumTypes", "MT-0");

                    HardwareIds.Add(hard.Version + "-" + happ.AppObject.Number + "-" + happ.AppVersionObject.Number, hid + "_HP-" + appidx);

                    xh2p.Add(new XElement(Get("ApplicationProgramRef"), new XAttribute("RefId", Manu + "_A-" + appidx)));

                    XElement xreginfo = new XElement(Get("RegistrationInfo"));
                    xreginfo.SetAttributeValue("RegistrationStatus", "Registered");
                    xreginfo.SetAttributeValue("RegistrationNumber", "0001/" + hard.Version + happ.AppVersion);
                    xh2p.Add(xreginfo);
                    xasso.Add(xh2p);
                }
                xhard.Add(new XElement(Get("Products"), xprod));
                xhard.Add(xasso);


                xhards.Add(xhard);
            }
            System.IO.File.WriteAllText(GetRelPath(Manu, "Hardware.xml"), doc.ToString());
            #endregion

            #region XML Catalog

            XElement cat = new XElement(Get("Catalog"));
            foreach (CatalogItem item in general.Catalog)
            {
                GetCatalogItems(item, cat, ProductIds, HardwareIds);
            }
            xmanu.Add(cat);
            #endregion

            doc.Save(GetRelPath("temp.xml"));
        }

        private int catalogCounter = 1;
        private void GetCatalogItems(CatalogItem item, XElement parent, Dictionary<string, string> productIds, Dictionary<string, string> hardwareIds)
        {
            XElement xitem = new XElement(Get(item.IsSection ? "CatalogSection" : "CatalogItem"));
            parent.Add(xitem);

            if (item.IsSection)
            {
                xitem.SetAttributeValue("Name", item.Name);
                xitem.SetAttributeValue("Number", catalogCounter++);
            }
            else
            {
                xitem.SetAttributeValue("Name", item.Hardware.DeviceObject.Name);
                xitem.SetAttributeValue("Number", item.Hardware.SerialNumber);
                xitem.SetAttributeValue("ProductRefId", productIds[item.Hardware.DeviceObject.Name]);
                string hardid = item.Hardware.Version + "-" + item.HardApp.AppObject.Number + "-" + item.HardApp.AppVersionObject.Number;
                xitem.SetAttributeValue("Hardware2ProgramRefId", hardwareIds[hardid]);
            }

            if (!string.IsNullOrWhiteSpace(item.VisibleDescription)) xitem.SetAttributeValue("VisibleDescription", item.VisibleDescription);

            int counter = 0;
            foreach (CatalogItem sub in item.Items)
                GetCatalogItems(sub, xitem, productIds, hardwareIds);
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
            input = input.Replace("_", ".5F");
            input = input.Replace("-", ".2D");
            input = input.Replace(" ", ".20");
            input = input.Replace("/", ".2F");
            return HttpUtility.UrlEncode(input).Replace('%', '.');
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

        private string Fill(string input, int length)
        {
            for (int i = input.Length; i < length; i++)
                input = "0" + input;
            return input;
        }
    }
}
