using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using Kaenx.Creator.Signing;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;
using System.Xml.Linq;

namespace Kaenx.Creator.Classes
{
    public class ExportHelper
    {
        List<Models.Hardware> hardware;
        List<Models.Device> devices;
        List<Models.Application> apps;
        List<Models.AppVersion> vers;
        Models.ModelGeneral general;
        XDocument doc;
        string appVersion;
        string currentNamespace;
        string convPath;

        public ExportHelper(Models.ModelGeneral g, List<Models.Hardware> h, List<Models.Device> d, List<Models.Application> a, List<Models.AppVersion> v, string cp)
        {
            hardware = h;
            devices = d;
            apps = a;
            vers = v;
            general = g;
            convPath = cp;
        }


        string currentLang = "";
        private Dictionary<string, Dictionary<string, Dictionary<string, string>>> languages {get;set;} = null;
 
        private void AddTranslation(string lang, string id, string attr, string value) {
            if(!languages.ContainsKey(lang)) languages.Add(lang, new Dictionary<string, Dictionary<string, string>>());
            if(!languages[lang].ContainsKey(id)) languages[lang].Add(id, new Dictionary<string, string>());
            if(!languages[lang][id].ContainsKey(attr)) languages[lang][id].Add(attr, value);
        }

        public void ExportEts()
        {
            string Manu = "M-" + general.ManufacturerId.ToString("X4");

            if (System.IO.Directory.Exists(GetRelPath()))
                System.IO.Directory.Delete(GetRelPath(), true);

            System.IO.Directory.CreateDirectory(GetRelPath());
            System.IO.Directory.CreateDirectory(GetRelPath("Temp"));
            System.IO.Directory.CreateDirectory(GetRelPath("Temp", Manu));

            int highestNS = 0;
            foreach (Models.AppVersion ver in vers)
            {
                if (ver.NamespaceVersion > highestNS)
                    highestNS = ver.NamespaceVersion;
            }
            currentNamespace = $"http://knx.org/xml/project/{highestNS}";

            Dictionary<string, string> ProductIds = new Dictionary<string, string>();
            Dictionary<string, string> HardwareIds = new Dictionary<string, string>();

            #region XML Applications
            Debug.WriteLine($"Exportiere Applikationen: {vers.Count}x");
            XElement xmanu = null;
            XElement xlanguages = null;
            foreach(Models.AppVersion ver in vers) {
                Debug.WriteLine($"Exportiere AppVersion: {ver.Name} {ver.NameText}");
                languages = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                xmanu = CreateNewXML(Manu);
                XElement xapps = new XElement(Get("ApplicationPrograms"));
                xmanu.Add(xapps);
                Models.Application app = apps.Single(a => a.Versions.Contains(ver));
                string appName = Manu + "_A-" + app.Number.ToString("X4");

                appVersion = appName + "-" + ver.Number.ToString("X2");
                appVersion += "-0000";

                currentLang = ver.DefaultLanguage;
                foreach(Models.Translation trans in ver.Text)
                    AddTranslation(trans.Language.CultureCode, appVersion, "Name", trans.Text);


                XElement xunderapp = new XElement(Get("Static"));
                XElement xapp = new XElement(Get("ApplicationProgram"), xunderapp);
                xapps.Add(xapp);
                xapp.SetAttributeValue("Id", appVersion);
                xapp.SetAttributeValue("ApplicationNumber", app.Number.ToString());
                xapp.SetAttributeValue("ApplicationVersion", ver.Number.ToString());
                xapp.SetAttributeValue("ProgramType", "ApplicationProgram");
                xapp.SetAttributeValue("MaskVersion", "MV-07B0");
                xapp.SetAttributeValue("Name", ver.Text.Single(e => e.Language.CultureCode == currentLang).Text); //TODO richtigen übersetzten Namen verwenden und nicht internen
                xapp.SetAttributeValue("DefaultLanguage", currentLang);
                xapp.SetAttributeValue("LoadProcedureStyle", "MergedProcedure");
                xapp.SetAttributeValue("PeiType", "0");
                xapp.SetAttributeValue("DynamicTableManagement", "false"); //TODO check when to add
                xapp.SetAttributeValue("Linkable", "false"); //TODO check when to add

                switch (currentNamespace)
                {
                    case "http://knx.org/xml/project/11":
                        xapp.SetAttributeValue("MinEtsVersion", "4.0");
                        break;
                    case "http://knx.org/xml/project/12":
                        xapp.SetAttributeValue("MinEtsVersion", "5.0");
                        break;
                    case "http://knx.org/xml/project/13":
                        xapp.SetAttributeValue("MinEtsVersion", "5.5");
                        break;
                    case "http://knx.org/xml/project/14":
                        xapp.SetAttributeValue("MinEtsVersion", "5.6");
                        break;
                    case "http://knx.org/xml/project/20":
                        xapp.SetAttributeValue("MinEtsVersion", "5.7");
                        break;
                    case "http://knx.org/xml/project/21":
                        xapp.SetAttributeValue("MinEtsVersion", "6.0");
                        break;
                }

                Dictionary<string, string> MemIds = new Dictionary<string, string>();
                XElement temp;

                #region Segmente
                Debug.WriteLine($"Exportiere Segmente: {ver.Memories.Count}x");
                temp = new XElement(Get("Code"));
                xunderapp.Add(temp);
                foreach (Memory mem in ver.Memories)
                {
                    //Debug.WriteLine($"    - Segment {mem.Name}");
                    if (ver.IsMemSizeAuto && mem.IsAutoSize)
                        AutoHelper.GetMemorySize(ver, mem);


                    XElement xmem = null;
                    string id = "";
                    switch (mem.Type)
                    {
                        case MemoryTypes.Absolute:
                            xmem = new XElement(Get("AbsoluteSegment"));
                            id = $"{appVersion}_AS-{mem.Address:X4}";
                            xmem.SetAttributeValue("Id", id);
                            xmem.SetAttributeValue("Address", mem.Address);
                            xmem.SetAttributeValue("Size", mem.Size);
                            //xmem.Add(new XElement(Get("Data"), "Hier kommt toller Base64 String hin"));
                            break;

                        case MemoryTypes.Relative:
                            xmem = new XElement(Get("RelativeSegment"));
                            id = $"{appVersion}_RS-04-{mem.Offset:X4}"; //TODO LoadStateMachine angeben
                            xmem.SetAttributeValue("Id", id);
                            xmem.SetAttributeValue("Name", mem.Name);
                            xmem.SetAttributeValue("Offset", mem.Offset);
                            xmem.SetAttributeValue("Size", mem.Size);
                            xmem.SetAttributeValue("LoadStateMachine", "4");
                            break;
                    }

                    if (xmem == null) continue;
                    temp.Add(xmem);
                    MemIds.Add(mem.Name, id);
                }
                #endregion

                #region ParamTypes
                Debug.WriteLine($"Exportiere ParameterTypes: {ver.ParameterTypes.Count}x");
                temp = new XElement(Get("ParameterTypes"));
                foreach (ParameterType type in ver.ParameterTypes)
                {
                    //Debug.WriteLine($"    - ParameterType {type.Name}x");
                    string id = appVersion + "_PT-" + GetEncoded(type.Name);
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
                            xcontent.SetAttributeValue("Type", type.Type == ParameterTypes.NumberUInt ? "unsignedInt" : "signedInt");
                            xcontent.SetAttributeValue("minInclusive", type.Min);
                            xcontent.SetAttributeValue("maxInclusive", type.Max);
                            break;

                        case ParameterTypes.Enum:
                            xcontent = new XElement(Get("TypeRestriction"));
                            xcontent.SetAttributeValue("Base", "Value");
                            int c = 0;
                            foreach (ParameterTypeEnum enu in type.Enums)
                            {
                                XElement xenu = new XElement(Get("Enumeration"));
                                xenu.SetAttributeValue("Text", enu.Text.Single(e => e.Language.CultureCode == currentLang).Text);
                                xenu.SetAttributeValue("Value", enu.Value);
                                xenu.SetAttributeValue("Id", $"{id}_EN-{c}");
                                xenu.SetAttributeValue("DisplayOrder", c.ToString());
                                xcontent.Add(xenu);
                                if(enu.Translate)
                                    foreach(Models.Translation trans in enu.Text) AddTranslation(trans.Language.CultureCode, $"{id}_EN-{c}", "Text", trans.Text);
                                c++;
                            }
                            break;

                        default:
                            throw new Exception("Unbekannter Parametertyp: " + type.Type);
                    }

                    if (xcontent != null && xcontent.Name.LocalName != "TypeNone")
                        xcontent.SetAttributeValue("SizeInBit", type.SizeInBit);
                    if (xcontent != null)
                        xtype.Add(xcontent);
                    temp.Add(xtype);
                }
                xunderapp.Add(temp);
                #endregion

                #region Parameter
                Debug.WriteLine($"Exportiere Parameter: {ver.Parameters.Count}x");
                temp = new XElement(Get("Parameters"));

                StringBuilder headers = new StringBuilder();

                foreach (Parameter para in ver.Parameters.Where(p => !p.IsInUnion))
                {
                    //Debug.WriteLine($"    - Parameter {para.UId} {para.Name}");
                    ParseParameter(para, temp, ver, headers);
                }

                Debug.WriteLine($"Exportiere Unions: {ver.Parameters.Where(p => p.IsInUnion).GroupBy(p => p.UnionObject).Count()}x");
                foreach (var paras in ver.Parameters.Where(p => p.IsInUnion).GroupBy(p => p.UnionObject))
                {
                    XElement xunion = new XElement(Get("Union"));
                    xunion.SetAttributeValue("SizeInBit", paras.Key.SizeInBit);

                    switch (paras.Key.SavePath)
                    {
                        case ParamSave.Memory:
                            XElement xmem = new XElement(Get("Memory"));
                            string memid = $"{appVersion}_";
                            if (paras.Key.MemoryObject.Type == MemoryTypes.Absolute)
                                memid += $"AS-{paras.Key.MemoryObject.Address:X4}";
                            else
                                memid += $"RS-04-{paras.Key.MemoryObject.Offset:X4}";
                            xmem.SetAttributeValue("CodeSegment", memid);
                            xmem.SetAttributeValue("Offset", paras.Key.Offset);
                            xmem.SetAttributeValue("BitOffset", paras.Key.OffsetBit);
                            xunion.Add(xmem);
                            break;

                        default:
                            throw new Exception("Not supportet SavePath for Union (" + paras.Key.Name + ")!");
                    }

                    foreach (Parameter para in paras)
                    {
                        //Debug.WriteLine($"        - Parameter {para.UId} {para.Name}");
                        ParseParameter(para, xunion, ver, headers);
                    }

                    temp.Add(xunion);
                }
                System.IO.File.WriteAllText(GetRelPath(appVersion + ".h"), headers.ToString());
                headers = null;

                xunderapp.Add(temp);
                #endregion

                #region ParameterRefs
                Debug.WriteLine($"Exportiere ParameterRefs: {ver.ParameterRefs.Count}x");
                temp = new XElement(Get("ParameterRefs"));

                foreach (ParameterRef pref in ver.ParameterRefs)
                {
                    //Debug.WriteLine($"    - ParameterRef {pref.UId} {pref.Name}");
                    if (pref.ParameterObject == null) continue;
                    XElement xpref = new XElement(Get("ParameterRef"));
                    if (pref.Id == -1)
                    {
                        pref.Id = AutoHelper.GetNextFreeId(ver.ParameterRefs);
                    }
                    string id = appVersion + (pref.ParameterObject.IsInUnion ? "_UP-" : "_P-") + pref.ParameterObject.Id;
                    xpref.SetAttributeValue("Id", $"{id}_R-{pref.Id}");
                    xpref.SetAttributeValue("RefId", id);
                    id += $"_R-{pref.Id}";
                    xpref.SetAttributeValue("Id", id);
                    if(pref.OverwriteAccess && pref.Access != ParamAccess.Default)
                        xpref.SetAttributeValue("Access", pref.Access.ToString());
                    if (pref.OverwriteValue)
                        xpref.SetAttributeValue("Value", pref.Value);
                    temp.Add(xpref);
                }

                xunderapp.Add(temp);
                #endregion

                #region ComObjects
                Debug.WriteLine($"Exportiere ComObjects: {ver.ComObjects.Count}x");
                temp = new XElement(Get("ComObjectTable"));

                foreach (ComObject com in ver.ComObjects)
                {
                    //Debug.WriteLine($"    - ComObject {com.UId} {com.Name}");
                    XElement xcom = new XElement(Get("ComObject"));
                    if (com.Id == -1)
                    {
                        com.Id = AutoHelper.GetNextFreeId(ver.ComObjects, 0);
                    }
                    xcom.SetAttributeValue("Id", $"{appVersion}_O-{com.Id}");
                    xcom.SetAttributeValue("Name", com.Name);
                    xcom.SetAttributeValue("Text", com.Text.Single(c => c.Language.CultureCode == currentLang).Text);
                    xcom.SetAttributeValue("Number", com.Number);
                    xcom.SetAttributeValue("FunctionText", com.FunctionText.Single(c => c.Language.CultureCode == currentLang).Text);
                    xcom.SetAttributeValue("VisibleDescription", com.Description.Single(c => c.Language.CultureCode == currentLang).Text);

                    if(!com.TranslationText)
                        foreach(Models.Translation trans in com.Text) AddTranslation(trans.Language.CultureCode, $"{appVersion}_O-{com.Id}", "Text", trans.Text);
                    if(!com.TranslationFunctionText)
                        foreach(Models.Translation trans in com.FunctionText) AddTranslation(trans.Language.CultureCode, $"{appVersion}_O-{com.Id}", "FunctionText", trans.Text);
                    if(!com.TranslationDescription)
                        foreach(Models.Translation trans in com.Description) AddTranslation(trans.Language.CultureCode, $"{appVersion}_O-{com.Id}", "VisibleDescription", trans.Text);

                    if (com.HasDpt && com.Type.Number != "0")
                    {
                        int size = com.Type.Size;
                        if (size > 7)
                            xcom.SetAttributeValue("ObjectSize", (size / 8) + " Byte");
                        else
                            xcom.SetAttributeValue("ObjectSize", size + " Bit");
                    }


                    xcom.SetAttributeValue("ReadFlag", com.FlagRead.ToString());
                    xcom.SetAttributeValue("WriteFlag", com.FlagWrite.ToString());
                    xcom.SetAttributeValue("CommunicationFlag", com.FlagComm.ToString());
                    xcom.SetAttributeValue("TransmitFlag", com.FlagTrans.ToString());
                    xcom.SetAttributeValue("UpdateFlag", com.FlagUpdate.ToString());
                    xcom.SetAttributeValue("ReadOnInitFlag", com.FlagOnInit.ToString());


                    if (com.HasDpt && com.Type.Number != "0")
                    {
                        if (com.HasDpts)
                            xcom.SetAttributeValue("DatapointType", "DPST-" + com.Type.Number + "-" + com.SubType.Number);
                        else
                            xcom.SetAttributeValue("DatapointType", "DPT-" + com.Type.Number);
                    }

                    temp.Add(xcom);
                }

                xunderapp.Add(temp);
                #endregion

                #region ComObjectRefs
                Debug.WriteLine($"Exportiere ComObjectRefs: {ver.ComObjectRefs.Count}x");
                temp = new XElement(Get("ComObjectRefs"));

                foreach (ComObjectRef cref in ver.ComObjectRefs)
                {
                    //Debug.WriteLine($"    - ComObjectRef {cref.UId} {cref.Name}");
                    XElement xcref = new XElement(Get("ComObjectRef"));
                    if (cref.Id == -1)
                    {
                        cref.Id = AutoHelper.GetNextFreeId(ver.ComObjectRefs, 0);
                    }
                    string id = $"{appVersion}_O-{cref.ComObjectObject.Id}";
                    xcref.SetAttributeValue("Id", $"{id}_R-{cref.Id}");
                    xcref.SetAttributeValue("RefId", id);
                    id += $"_R-{cref.Id}";
                    xcref.SetAttributeValue("Id", id);


                    if(cref.OverwriteText) {
                        if(!cref.TranslationText)
                            foreach(Models.Translation trans in cref.Text) AddTranslation(trans.Language.CultureCode, id, "Text", trans.Text);
                        xcref.SetAttributeValue("Text", cref.Text.Single(c => c.Language.CultureCode == currentLang).Text);
                    }
                    if(cref.OverwriteFunctionText) {
                        if(!cref.TranslationFunctionText)
                            foreach(Models.Translation trans in cref.FunctionText) AddTranslation(trans.Language.CultureCode, id, "FunctionText", trans.Text);
                        xcref.SetAttributeValue("FunctionText", cref.FunctionText.Single(c => c.Language.CultureCode == currentLang).Text);
                    }
                    if(cref.OverwriteDescription) {
                        if(!cref.TranslationDescription)
                            foreach(Models.Translation trans in cref.Description) AddTranslation(trans.Language.CultureCode, id, "Description", trans.Text);
                        xcref.SetAttributeValue("VisibleDescription", cref.Description.Single(c => c.Language.CultureCode == currentLang).Text);
                    }

                    if (cref.OverwriteDpt)
                    {
                        int size = cref.Type.Size;

                        if (cref.Type.Number == "0")
                        {
                            xcref.SetAttributeValue("DatapointType", "");
                        }
                        else
                        {
                            if (cref.OverwriteDpst)
                            {
                                xcref.SetAttributeValue("DatapointType", "DPST-" + cref.Type.Number + "-" + cref.SubType.Number);
                            }
                            else
                            {
                                xcref.SetAttributeValue("DatapointType", "DPT-" + cref.Type.Number);
                            }
                            if (size > 7)
                                xcref.SetAttributeValue("ObjectSize", (size / 8) + " Byte");
                            else
                                xcref.SetAttributeValue("ObjectSize", size + " Bit");
                        }
                    }
                    temp.Add(xcref);
                }

                xunderapp.Add(temp);
                #endregion

                #region Tables
                temp = new XElement(Get("AddressTable"));
                temp.SetAttributeValue("MaxEntries", "65535");
                xunderapp.Add(temp);
                temp = new XElement(Get("AssociationTable"));
                temp.SetAttributeValue("MaxEntries", "65535");
                xunderapp.Add(temp);


                switch (app.Mask.Procedure)
                {
                    case ProcedureTypes.Application:
                        temp = XDocument.Parse($"<LoadProcedures><LoadProcedure><LdCtrlConnect /><LdCtrlDisconnect /></LoadProcedure></LoadProcedures>").Root;
                        xunderapp.Add(temp);
                        break;

                    case ProcedureTypes.Merge:
                        temp = XDocument.Parse($"<LoadProcedures><LoadProcedure MergeId=\"2\"><LdCtrlRelSegment  AppliesTo=\"full\" LsmIdx=\"4\" Size=\"1\" Mode=\"0\" Fill=\"0\" /></LoadProcedure><LoadProcedure MergeId=\"4\"><LdCtrlWriteRelMem ObjIdx=\"4\" Offset=\"0\" Size=\"1\" Verify=\"true\" /></LoadProcedure></LoadProcedures>").Root;
                        xunderapp.Add(temp);
                        break;
                }



                #endregion

                xunderapp = new XElement(Get("Dynamic"));
                xapp.Add(xunderapp);

                HandleSubItems(ver.Dynamics[0], xunderapp);


                #region Translations
                Debug.WriteLine($"Exportiere Translations: {languages.Count} Sprachen");
                xlanguages = new XElement(Get("Languages"));
                foreach(KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> lang in languages) {
                    XElement xunit = new XElement(Get("TranslationUnit"));
                    xunit.SetAttributeValue("RefId", appVersion);
                    XElement xlang = new XElement(Get("Language"), xunit);
                    xlang.SetAttributeValue("Identifier", lang.Key);

                    foreach(KeyValuePair<string, Dictionary<string, string>> langitem in lang.Value) {
                        XElement xele = new XElement(Get("TranslationElement"));
                        xele.SetAttributeValue("RefId", langitem.Key);

                        foreach(KeyValuePair<string, string> langval in langitem.Value) {
                            XElement xtrans = new XElement(Get("Translation"));
                            xtrans.SetAttributeValue("AttributeName", langval.Key);
                            xtrans.SetAttributeValue("Text", langval.Value);
                            xele.Add(xtrans);
                        }

                        xunit.Add(xele);
                    }
                    xlanguages.Add(xlang);
                }
                xmanu.Add(xlanguages);
                #endregion

                Debug.WriteLine($"Speichere App: {GetRelPath("Temp", Manu, appVersion + ".xml")}");
                doc.Save(GetRelPath("Temp", Manu, appVersion + ".xml"));
                Debug.WriteLine($"Speichern beendet");
            }
            #endregion

            #region XML Hardware
            languages.Clear();
            Debug.WriteLine($"Exportiere Hardware: {hardware.Count}x");
            xmanu = CreateNewXML(Manu);
            XElement xhards = new XElement(Get("Hardware"));
            xmanu.Add(xhards);
            currentLang = general.DefaultLanguage;
            foreach (Models.Hardware hard in hardware)
            {
                string hid = Manu + "_H-" + GetEncoded(hard.SerialNumber) + "-" + hard.Version;
                XElement xhard = new XElement(Get("Hardware"));
                xhard.SetAttributeValue("Id", hid);
                xhard.SetAttributeValue("Name", hard.Name);
                xhard.SetAttributeValue("SerialNumber", hard.SerialNumber);
                xhard.SetAttributeValue("VersionNumber", hard.Version.ToString());
                xhard.SetAttributeValue("BusCurrent", hard.BusCurrent);
                if (hard.HasIndividualAddress) xhard.SetAttributeValue("HasIndividualAddress", "1");
                if (hard.HasApplicationProgram) xhard.SetAttributeValue("HasApplicationProgram", "1");
                if (hard.HasApplicationProgram2) xhard.SetAttributeValue("HasApplicationProgram2", "1");
                if (hard.IsPowerSupply) xhard.SetAttributeValue("IsPowerSupply", "1");
                xhard.SetAttributeValue("IsChocke", "0"); //Todo check what this is
                if (hard.IsCoppler) xhard.SetAttributeValue("IsCoupler", "1");
                xhard.SetAttributeValue("IsPowerLineRepeater", "0");
                xhard.SetAttributeValue("IsPowerLineSignalFilter", "0");
                if (hard.IsPowerSupply) xhard.SetAttributeValue("IsPowerSupply", "1");
                xhard.SetAttributeValue("IsCable", "0"); //Todo check if means PoweLine Cable
                if (hard.IsIpEnabled) xhard.SetAttributeValue("IsIPEnabled", "1");

                XElement xprods = new XElement(Get("Products"));
                xhard.Add(xprods);
                foreach (Device dev in hard.Devices)
                {
                    if (!devices.Contains(dev)) continue;

                    XElement xprod = new XElement(Get("Product"));
                    string pid = hid + "_P-" + GetEncoded(dev.OrderNumber);
                    ProductIds.Add(dev.Name, pid);
                    xprod.SetAttributeValue("Id", pid);
                    xprod.SetAttributeValue("Text", dev.Text.Single(e => e.Language.CultureCode == currentLang).Text);
                    xprod.SetAttributeValue("OrderNumber", dev.OrderNumber);
                    xprod.SetAttributeValue("IsRailMounted", dev.IsRailMounted ? "1" : "0");
                    xprod.SetAttributeValue("DefaultLanguage", currentLang);
                    xprod.Add(new XElement(Get("RegistrationInfo"), new XAttribute("RegistrationStatus", "Registered")));
                    xprods.Add(xprod);

                    foreach(Models.Translation trans in dev.Text) AddTranslation(trans.Language.CultureCode, pid, "Text", trans.Text);
                }


                XElement xasso = new XElement(Get("Hardware2Programs"));
                xhard.Add(xasso);

                foreach (Models.Application app in hard.Apps)
                {
                    if (!apps.Contains(app)) continue;

                    foreach (Models.AppVersion ver in app.Versions)
                    {
                        if (!vers.Contains(ver)) continue;

                        string appidx = app.Number.ToString("X4") + "-" + ver.Number.ToString("X2") + "-0000";

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

            Debug.WriteLine($"Exportiere Translations: {languages.Count} Sprachen");
            xlanguages = new XElement(Get("Languages"));
            foreach(KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> lang in languages) {
                XElement xunit = new XElement(Get("TranslationUnit"));
                xunit.SetAttributeValue("RefId", appVersion);
                XElement xlang = new XElement(Get("Language"), xunit);
                xlang.SetAttributeValue("Identifier", lang.Key);

                foreach(KeyValuePair<string, Dictionary<string, string>> langitem in lang.Value) {
                    XElement xele = new XElement(Get("TranslationElement"));
                    xele.SetAttributeValue("RefId", langitem.Key);

                    foreach(KeyValuePair<string, string> langval in langitem.Value) {
                        XElement xtrans = new XElement(Get("Translation"));
                        xtrans.SetAttributeValue("AttributeName", langval.Key);
                        xtrans.SetAttributeValue("Text", langval.Value);
                        xele.Add(xtrans);
                    }

                    xunit.Add(xele);
                }
                xlanguages.Add(xlang);
            }
            xmanu.Add(xlanguages);

            Debug.WriteLine($"Speichere Hardware: {GetRelPath("Temp", Manu, "Hardware.xml")}");
            doc.Save(GetRelPath("Temp", Manu, "Hardware.xml"));
            #endregion

            #region XML Catalog

            Debug.WriteLine($"Exportiere Catalog");
            xmanu = CreateNewXML(Manu);
            XElement cat = new XElement(Get("Catalog"));

            foreach (CatalogItem item in general.Catalog[0].Items)
            {
                GetCatalogItems(item, cat, ProductIds, HardwareIds);
            }
            xmanu.Add(cat);
            Debug.WriteLine($"Speichere Catalog: {GetRelPath("Temp", Manu, "Catalog.xml")}");
            doc.Save(GetRelPath("Temp", Manu, "Catalog.xml"));
            #endregion
        }

        private void ParseParameter(Parameter para, XElement parent, AppVersion ver, StringBuilder headers)
        {
            string line = "#define PARAM_" + para.Name + " " + para.Offset + " //Size: " + para.ParameterTypeObject.SizeInBit;
            if (para.ParameterTypeObject.SizeInBit % 8 == 0) line += " (" + (para.ParameterTypeObject.SizeInBit / 8) + " Byte)";
            if (para.OffsetBit > 0) line += " | Bit Offset: " + para.OffsetBit;
            headers.AppendLine(line);

            XElement xpara = new XElement(Get("Parameter"));

            if (para.Id == -1)
            {
                para.Id = AutoHelper.GetNextFreeId(ver.Parameters);
            }
            string id = appVersion + (para.IsInUnion ? "_UP-" : "_P-") + para.Id;
            xpara.SetAttributeValue("Id", id);
            xpara.SetAttributeValue("Name", para.Name);
            xpara.SetAttributeValue("ParameterType", $"{appVersion}_PT-{GetEncoded(para.ParameterTypeObject.Name)}");

            if(!para.TranslationText)
                foreach(Models.Translation trans in para.Text) AddTranslation(trans.Language.CultureCode, id, "Text", trans.Text);

            if(!para.IsInUnion) {
                switch(para.SavePath) {
                    case ParamSave.Memory:
                        XElement xparamem = new XElement(Get("Memory"));
                        string memid = appVersion;
                        if (para.MemoryObject.Type == MemoryTypes.Absolute)
                            memid += $"_AS-{para.MemoryObject.Address:X4}";
                        else
                            memid += $"_RS-04-{para.MemoryObject.Offset:X4}";
                        xparamem.SetAttributeValue("CodeSegment", memid);
                        xparamem.SetAttributeValue("Offset", para.Offset);
                        xparamem.SetAttributeValue("BitOffset", para.OffsetBit);
                        xpara.Add(xparamem);
                        break;
                }
            }
            else
            {
                xpara.SetAttributeValue("Offset", para.Offset);
                xpara.SetAttributeValue("BitOffset", para.OffsetBit);
                if (para.IsUnionDefault)
                    xpara.SetAttributeValue("DefaultUnionParameter", "true");
            }
            
            xpara.SetAttributeValue("Text", para.Text.Single(p => p.Language.CultureCode == currentLang).Text);
            if (para.Access != ParamAccess.Default && para.Access != ParamAccess.ReadWrite) xpara.SetAttributeValue("Access", para.Access);
            if (!string.IsNullOrWhiteSpace(para.Suffix)) xpara.SetAttributeValue("SuffixText", para.Suffix);
            xpara.SetAttributeValue("Value", para.Value);

            parent.Add(xpara);
        }





        #region Create Dyn Stuff

        private void HandleSubItems(IDynItems parent, XElement xparent)
        {
            foreach (IDynItems item in parent.Items)
            {
                XElement xitem = null;

                switch (item)
                {
                    case DynChannel dc:
                    case DynChannelIndependet dci:
                        xitem = Handle(item, xparent);
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

                    case DynComObject dc:
                        HandleCom(dc, xparent);
                        break;

                    case DynSeparator ds:
                        HandleSep(ds, xparent);
                        break;

                    default:
                        throw new Exception("Nicht behandeltes dynamisches Element: " + item.ToString());
                }

                if (item.Items != null && xitem != null)
                    HandleSubItems(item, xitem);
            }
        }


        private XElement Handle(IDynItems ch, XElement parent)
        {
            XElement channel = new XElement(Get("ChannelIndependentBlock"));
            parent.Add(channel);

            if (ch is DynChannel)
            {
                DynChannel dch = ch as DynChannel;
                channel.Name = Get("Channel");
                if (dch.ParameterRefObject != null)
                    channel.SetAttributeValue("ParamRefId", appVersion + (dch.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{dch.ParameterRefObject.ParameterObject.Id}_R-{dch.ParameterRefObject.Id}");
                else {
                    channel.SetAttributeValue("Text", dch.Text.Single(p => p.Language.CultureCode == currentLang).Text);
                    if(!dch.TranslationText)
                        foreach(Models.Translation trans in dch.Text) AddTranslation(trans.Language.CultureCode, $"{appVersion}_CH-{dch.Number}", "Text", trans.Text);
                }
                channel.SetAttributeValue("Number", dch.Number);
                channel.SetAttributeValue("Id", $"{appVersion}_CH-{dch.Number}");
                channel.SetAttributeValue("Name", ch.Name);
            }


            return channel;
        }

        private void HandleCom(DynComObject com, XElement parent)
        {
            XElement xcom = new XElement(Get("ComObjectRefRef"));
            xcom.SetAttributeValue("RefId", $"{appVersion}_O-{com.ComObjectRefObject.ComObjectObject.Id}_R-{com.ComObjectRefObject.Id}");
            parent.Add(xcom);
        }

        private void HandleSep(DynSeparator sep, XElement parent)
        {
            XElement xsep = new XElement(Get("ParameterSeparator"));
            if(sep.Id == -1) {
                sep.Id = 1; //TODO get real next free Id
            }
            xsep.SetAttributeValue("Id", $"{appVersion}_PS-{sep.Id}");
            parent.Add(xsep);
        }

        private XElement HandleChoose(DynChoose cho, XElement parent)
        {
            XElement xcho = new XElement(Get("choose"));
            parent.Add(xcho);
            xcho.SetAttributeValue("ParamRefId", appVersion + (cho.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{cho.ParameterRefObject.ParameterObject.Id}_R-{cho.ParameterRefObject.Id}");
            return xcho;
        }

        private XElement HandleWhen(DynWhen when, XElement parent)
        {
            XElement xwhen = new XElement(Get("when"));
            parent.Add(xwhen);

            //when.Condition = when.Condition.Replace(">", "&gt;");
            //when.Condition = when.Condition.Replace("<", "&lt;");


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



            if (bl.ParameterRefObject != null)
            {
                block.SetAttributeValue("Id", $"{appVersion}_PB-{bl.ParameterRefObject.Id}");
                block.SetAttributeValue("Name", bl.Name);
                block.SetAttributeValue("ParamRefId", appVersion + (bl.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{bl.ParameterRefObject.ParameterObject.Id}_R-{bl.ParameterRefObject.Id}");
            }
            else
            {
                if (bl.Id == -1)
                {
                    bl.Id = 1; //TODO get real next free Id
                }
                block.SetAttributeValue("Id", $"{appVersion}_PB-{bl.Id}");
                block.SetAttributeValue("Text", bl.Text.Single(p => p.Language.CultureCode == currentLang).Text);
                if(!bl.TranslationText)
                    foreach(Models.Translation trans in bl.Text) AddTranslation(trans.Language.CultureCode, $"{appVersion}_PB-{bl.Id}", "Text", trans.Text);
            }

            return block;
        }

        private void HandleParam(DynParameter pa, XElement parent)
        {
            XElement xpara = new XElement(Get("ParameterRefRef"));
            parent.Add(xpara);
            xpara.SetAttributeValue("RefId", appVersion + (pa.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{pa.ParameterRefObject.ParameterObject.Id}_R-{pa.ParameterRefObject.Id}");
        }
        #endregion

        private bool CheckSections(CatalogItem parent)
        {
            bool flag = false;

            foreach (CatalogItem item in parent.Items)
            {
                if (item.IsSection)
                {
                    if (CheckSections(item)) flag = true;
                }
                else
                {
                    if (item.Hardware.Devices.Any(d => devices.Contains(d))) flag = true;
                }
            }
            return flag;
        }

        private void GetCatalogItems(CatalogItem item, XElement parent, Dictionary<string, string> productIds, Dictionary<string, string> hardwareIds)
        {
            if (item.IsSection)
            {
                XElement xitem = new XElement(Get("CatalogSection"));

                if (CheckSections(item))
                {
                    if (item.Parent.Parent == null)
                    {
                        string id = $"M-{general.ManufacturerId.ToString("X4")}_CS-" + GetEncoded(item.Number);
                        xitem.SetAttributeValue("Id", id);
                    }
                    else
                    {
                        string id = parent.Attribute("Id").Value;
                        id += "-" + GetEncoded(item.Number);
                        xitem.SetAttributeValue("Id", id);
                    }

                    xitem.SetAttributeValue("Name", item.Name);
                    xitem.SetAttributeValue("Number", item.Number);
                    xitem.SetAttributeValue("DefaultLanguage", currentLang);
                    parent.Add(xitem);
                }

                foreach (CatalogItem sub in item.Items)
                    GetCatalogItems(sub, xitem, productIds, hardwareIds);
            }
            else
            {
                foreach (Device dev in item.Hardware.Devices)
                {
                    if (!devices.Contains(dev)) continue;

                    foreach (Application app in item.Hardware.Apps)
                    {
                        if (!apps.Contains(app)) continue;

                        foreach (AppVersion ver in app.Versions)
                        {
                            if (!vers.Contains(ver)) continue;
                            XElement xitem = new XElement(Get("CatalogItem"));

                            string id = $"M-{general.ManufacturerId.ToString("X4")}";
                            id += $"_H-{GetEncoded(item.Hardware.SerialNumber)}-{item.Hardware.Version}";
                            id += $"_HP-{app.Number.ToString("X4")}-{ver.Number.ToString("X2")}-0000";
                            string parentId = parent.Attribute("Id").Value;
                            parentId = parentId.Substring(parentId.LastIndexOf("_CS-") + 4);
                            id += $"_CI-{GetEncoded(dev.OrderNumber)}-{GetEncoded(item.Number)}";

                            xitem.SetAttributeValue("Id", id);
                            xitem.SetAttributeValue("Name", dev.Text);
                            xitem.SetAttributeValue("Number", item.Number); //TODO check if correct  (item.Hardware.SerialNumber);
                            if (!string.IsNullOrWhiteSpace(dev.Description.Single(e => e.Language.CultureCode == currentLang).Text)) xitem.SetAttributeValue("VisibleDescription", dev.Description);
                            xitem.SetAttributeValue("ProductRefId", productIds[dev.Name]);
                            string hardid = item.Hardware.Version + "-" + app.Number + "-" + ver.Number;
                            xitem.SetAttributeValue("Hardware2ProgramRefId", hardwareIds[hardid]);
                            xitem.SetAttributeValue("DefaultLanguage", currentLang);
                            parent.Add(xitem);
                        }
                    }
                }
            }
        }

        public void SignOutput()
        {
            string manu = $"M-{general.ManufacturerId:X4}";

            IDictionary<string, string> applProgIdMappings = new Dictionary<string, string>();
            IDictionary<string, string> applProgHashes = new Dictionary<string, string>();
            IDictionary<string, string> mapBaggageIdToFileIntegrity = new Dictionary<string, string>(50);

            FileInfo hwFileInfo = new FileInfo(GetRelPath("Temp", manu, "Hardware.xml"));
            FileInfo catalogFileInfo = new FileInfo(GetRelPath("Temp", manu, "Catalog.xml"));

            foreach (string file in Directory.GetFiles(GetRelPath("Temp", manu)))
            {
                if (!file.Contains("M-") || !file.Contains("_A-")) continue;

                FileInfo info = new FileInfo(file);
                ApplicationProgramHasher aph = new ApplicationProgramHasher(info, mapBaggageIdToFileIntegrity, convPath, true);
                aph.HashFile();

                string oldApplProgId = aph.OldApplProgId;
                string newApplProgId = aph.NewApplProgId;
                string genHashString = aph.GeneratedHashString;

                applProgIdMappings.Add(oldApplProgId, newApplProgId);
                if (!applProgHashes.ContainsKey(newApplProgId))
                    applProgHashes.Add(newApplProgId, genHashString);
            }

            HardwareSigner hws = new HardwareSigner(hwFileInfo, applProgIdMappings, applProgHashes, convPath, true);
            hws.SignFile();
            IDictionary<string, string> hardware2ProgramIdMapping = hws.OldNewIdMappings;

            CatalogIdPatcher cip = new CatalogIdPatcher(catalogFileInfo, hardware2ProgramIdMapping, convPath);
            cip.Patch();

            File.Copy(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "knx_master.xml"), GetRelPath("Temp", "knx_master.xml"));

            XmlSigning.SignDirectory(GetRelPath("Temp", manu), convPath);

            System.IO.Compression.ZipFile.CreateFromDirectory(GetRelPath("Temp"), GetRelPath("output.knxprod"));

            #if (!DEBUG)
            System.IO.Directory.Delete(GetRelPath("Temp"), true);
            #endif
        }

        private string GetEncoded(string input)
        {
            input = input.Replace(".", ".2E");

            input = input.Replace("%", ".25");
            input = input.Replace(" ", ".20");
            input = input.Replace("!", ".21");
            input = input.Replace("\"", ".22");
            input = input.Replace("#", ".23");
            input = input.Replace("$", ".24");
            input = input.Replace("&", ".26");
            input = input.Replace("(", ".28");
            input = input.Replace(")", ".29");
            input = input.Replace("+", ".2B");
            input = input.Replace("-", ".2D");
            input = input.Replace("/", ".2F");
            input = input.Replace(":", ".3A");
            input = input.Replace(";", ".3B");
            input = input.Replace("<", ".3C");
            input = input.Replace("=", ".3D");
            input = input.Replace(">", ".3E");
            input = input.Replace("?", ".3F");
            input = input.Replace("@", ".40");
            input = input.Replace("[", ".5B");
            input = input.Replace("\\", ".5C");
            input = input.Replace("]", ".5D");
            input = input.Replace("^", ".5C");
            input = input.Replace("_", ".5F");
            input = input.Replace("{", ".7B");
            input = input.Replace("|", ".7C");
            input = input.Replace("}", ".7D");

            input = input.Replace("%", ".");
            return input;
        }

        public string GetRelPath(params string[] path)
        {
            List<string> paths = new List<string>() { AppDomain.CurrentDomain.BaseDirectory, "Output" };
            paths.AddRange(path);
            return System.IO.Path.Combine(paths.ToArray());
        }

        public string GetRelPath()
        {
            return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Output");
        }

        private XName Get(string name)
        {
            return XName.Get(name); //, currentNamespace);
        }

        private XElement CreateNewXML(string manu)
        {
            XElement xmanu = new XElement(Get("Manufacturer"));
            xmanu.SetAttributeValue("RefId", manu);

            XElement knx = new XElement(Get("KNX"));
            knx.SetAttributeValue("CreatedBy", "Kaenx.Creator");
            knx.SetAttributeValue("ToolVersion", "0.1.0");
            doc = new XDocument(knx);
            doc.Root.Add(new XElement(Get("ManufacturerData"), xmanu));
            return xmanu;
        }
    }
}
