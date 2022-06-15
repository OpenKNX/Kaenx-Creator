using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using Kaenx.Creator.Signing;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;

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
        string appVersionMod;
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
            if(string.IsNullOrEmpty(value)) return;
            if(!languages.ContainsKey(lang)) languages.Add(lang, new Dictionary<string, Dictionary<string, string>>());
            if(!languages[lang].ContainsKey(id)) languages[lang].Add(id, new Dictionary<string, string>());
            if(!languages[lang][id].ContainsKey(attr)) languages[lang][id].Add(attr, value);
        }

        public bool ExportEts(ObservableCollection<PublishAction> actions)
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

                appVersion = $"{Manu}_A-{app.Number:X4}-{ver.Number:X2}";
                appVersion += "-0000";
                appVersionMod = appVersion;

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
                xapp.SetAttributeValue("MaskVersion", app.Mask.Id);
                xapp.SetAttributeValue("Name", ver.Text.Single(e => e.Language.CultureCode == currentLang).Text);
                xapp.SetAttributeValue("DefaultLanguage", currentLang);
                xapp.SetAttributeValue("LoadProcedureStyle", $"{app.Mask.Procedure}Procedure");
                xapp.SetAttributeValue("PeiType", "0");
                xapp.SetAttributeValue("DynamicTableManagement", "false"); //TODO check when to add
                xapp.SetAttributeValue("Linkable", "false"); //TODO check when to add
                if(!string.IsNullOrEmpty(ver.ReplacesVersions)) xapp.SetAttributeValue("ReplacesVersions", ver.ReplacesVersions);

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

                XElement temp;
                ExportSegments(ver, xunderapp);

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
                        case ParameterTypes.Float_DPT9:
                        case ParameterTypes.Float_IEEE_Double:
                        case ParameterTypes.Float_IEEE_Single:
                            if(type.Type == ParameterTypes.NumberUInt || type.Type == ParameterTypes.NumberInt)
                                xcontent = new XElement(Get("TypeNumber"));
                            else
                                xcontent = new XElement(Get("TypeFloat"));

                            string ftype = "failed";
                            switch(type.Type)
                            {
                                case ParameterTypes.NumberUInt:
                                    ftype = "unsignedInt";
                                    break;
                                
                                case ParameterTypes.NumberInt:
                                    ftype = "signedInt";
                                    break;

                                case ParameterTypes.Float_DPT9:
                                    ftype = "DPT 9";
                                    break;

                                case ParameterTypes.Float_IEEE_Single:
                                    ftype = "IEEE-754 Single";
                                    break;

                                case ParameterTypes.Float_IEEE_Double:
                                    ftype = "IEEE-754 Double";
                                    break;
                            }
                            xcontent.SetAttributeValue("Type", ftype);
                            xcontent.SetAttributeValue("minInclusive", type.Min);
                            xcontent.SetAttributeValue("maxInclusive", type.Max);
                            if(type.Increment != 1)
                                xcontent.SetAttributeValue("Increment", type.Increment);
                            if(type.UIHint != "None")
                                xcontent.SetAttributeValue("UIHint", type.UIHint);
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
                                xenu.SetAttributeValue("Id", $"{id}_EN-{enu.Value}");
                                xenu.SetAttributeValue("DisplayOrder", c.ToString());
                                xcontent.Add(xenu);
                                if(enu.Translate)
                                    foreach(Models.Translation trans in enu.Text) AddTranslation(trans.Language.CultureCode, $"{id}_EN-{enu.Value}", "Text", trans.Text);
                                c++;
                            }
                            break;

                        case ParameterTypes.Picture:
                            xcontent = new XElement(Get("TypePicture"));
                            xcontent.SetAttributeValue("RefId", $"M-{general.ManufacturerId:X4}_BG-{GetEncoded(type.BaggageObject.TargetPath)}-{GetEncoded(type.BaggageObject.Name + type.BaggageObject.Extension)}");
                            break;

                        default:
                            throw new Exception("Unbekannter Parametertyp: " + type.Type);
                    }

                    if (xcontent != null && 
                        xcontent.Name.LocalName != "TypeNone" &&
                        xcontent.Name.LocalName != "TypePicture" &&
                        xcontent.Name.LocalName != "IpAddress")
                    {
                        xcontent.SetAttributeValue("SizeInBit", type.SizeInBit);
                    }
                    if (xcontent != null)
                        xtype.Add(xcontent);
                    temp.Add(xtype);
                }
                xunderapp.Add(temp);
                #endregion

                StringBuilder headers = new StringBuilder();

                ExportParameters(ver, xunderapp, headers);
                ExportParameterRefs(ver, xunderapp);
                ExportComObjects(ver, xunderapp, headers);
                ExportComObjectRefs(ver, xunderapp);

                #region Tables
                temp = new XElement(Get("AddressTable"));
                if(app.Mask.Memory == MemoryTypes.Absolute)
                {
                    temp.SetAttributeValue("CodeSegment", $"{appVersion}_AS-{ver.AddressMemoryObject.Address:X4}");
                    temp.SetAttributeValue("Offset", ver.AddressTableOffset);
                }
                temp.SetAttributeValue("MaxEntries", ver.AddressTableMaxCount);
                xunderapp.Add(temp);
                temp = new XElement(Get("AssociationTable"));
                if(app.Mask.Memory == MemoryTypes.Absolute)
                {
                    temp.SetAttributeValue("CodeSegment", $"{appVersion}_AS-{ver.AssociationMemoryObject.Address:X4}");
                    temp.SetAttributeValue("Offset", ver.AssociationTableOffset);
                }
                temp.SetAttributeValue("MaxEntries", ver.AssociationTableMaxCount);
                xunderapp.Add(temp);
                
                temp = XElement.Parse(ver.Procedure);
                temp.Attributes().Where((x) => x.IsNamespaceDeclaration).Remove();
                temp.Name = XName.Get(temp.Name.LocalName, currentNamespace);
                foreach(XElement xele in temp.Descendants())
                {
                    xele.Name = XName.Get(xele.Name.LocalName, currentNamespace);
                }
                xunderapp.Add(temp);


                #endregion

                #region ModuleDefines
                if(ver.Modules.Count > 0)
                {
                    xunderapp = new XElement(Get("ModuleDefs"));
                    xapp.Add(xunderapp);

                    foreach (Models.Module mod in ver.Modules)
                    {
                        if (mod.Id == -1)
                            mod.Id = AutoHelper.GetNextFreeId(ver.Modules);

                        appVersionMod += $"_MD-{mod.Id}";

                        temp = new XElement(Get("Arguments"));
                        foreach (Models.Argument arg in mod.Arguments)
                        {
                            XElement xarg = new XElement(Get("Argument"));
                            if (arg.Id == -1)
                                arg.Id = AutoHelper.GetNextFreeId(mod.Arguments);
                            xarg.SetAttributeValue("Id", $"{appVersionMod}_A-{arg.Id}");
                            xarg.SetAttributeValue("Name", arg.Name);
                            temp.Add(xarg);
                        }
                        XElement xmod = new XElement(Get("ModuleDef"), temp);
                        XElement xunderstatic = new XElement(Get("Static"));
                        xmod.Add(xunderstatic);
                        xunderapp.Add(xmod);

                        xmod.SetAttributeValue("Id", $"{appVersion}_MD-{mod.Id}");
                        xmod.SetAttributeValue("Name", mod.Name);

                        ExportParameters(mod, xunderstatic, null);
                        ExportParameterRefs(mod, xunderstatic);
                        ExportComObjects(mod, xunderstatic, null);
                        ExportComObjectRefs(mod, xunderstatic);

                        
                        XElement xmoddyn = new XElement(Get("Dynamic"));
                        xmod.Add(xmoddyn);

                        HandleSubItems(mod.Dynamics[0], xmoddyn);

                        appVersionMod = appVersion;
                    }
                }

                headers.AppendLine("");
                headers.AppendLine("//---------------------Modules----------------------------");

                List<DynModule> mods = new List<DynModule>();
                AutoHelper.GetModules(ver.Dynamics[0], mods);

                int counter = 1;
                foreach(DynModule dmod in mods)
                {
                    DynModuleArg dargp = dmod.Arguments.Single(a => a.ArgumentId == dmod.ModuleObject.ParameterBaseOffsetUId);
                    int poffset = int.Parse(dargp.Value);
                    foreach(Parameter para in dmod.ModuleObject.Parameters)
                    {
                        string line = $"#define PARAM_M{counter}_{para.Name.Replace(' ', '_')}";
                        if(para.IsInUnion && para.UnionObject != null)
                        {
                            line += $"\t0x{(poffset + para.UnionObject.Offset + para.Offset).ToString("X4")}\t//!< UnionOffset: {poffset + para.UnionObject.Offset}, ParaOffset: {para.Offset}";
                        } else {
                            line += $"\t0x{(poffset + para.Offset).ToString("X4")}\t//!< Offset: {poffset + para.Offset}";
                        }
                        if (para.OffsetBit > 0) line += ", BitOffset: " + para.OffsetBit;
                        line += $", Size: {para.ParameterTypeObject.SizeInBit} Bit";
                        if (para.ParameterTypeObject.SizeInBit % 8 == 0) line += " (" + (para.ParameterTypeObject.SizeInBit / 8) + " Byte)";
                        line += $", Module: {dmod.ModuleObject.Name}, Text: {para.Text.Single(p => p.Language.CultureCode == currentLang).Text}";
                        headers.AppendLine(line);
                    }

                    
                    DynModuleArg dargc = dmod.Arguments.Single(a => a.ArgumentId == dmod.ModuleObject.ComObjectBaseNumberUId);
                    int coffset = int.Parse(dargc.Value);
                    foreach(ComObject com in dmod.ModuleObject.ComObjects)
                    {
                        string line = $"#define COMOBJ_M{counter}_{com.Name.Replace(' ', '_')} \t{coffset + com.Number}\t//!< Number: {coffset + com.Number}, Module: {dmod.ModuleObject.Name}, Text: {com.Text.Single(c => c.Language.CultureCode == currentLang).Text}, Function: {com.FunctionText.Single(c => c.Language.CultureCode == currentLang).Text}";
                        headers.AppendLine(line);
                        
                    }
                    headers.AppendLine();
                    counter++;
                }

                System.IO.File.WriteAllText(GetRelPath(appVersion + ".h"), headers.ToString());
                headers = null;

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

                string nsnumber = currentNamespace.Substring(currentNamespace.LastIndexOf('/') + 1);
                string xsdFile = "Data\\knx_project_" + nsnumber + ".xsd";
                if (File.Exists(xsdFile))
                {
                    Debug.WriteLine("XSD gefunden. Validierung wird ausgeführt");
                    XmlSchemaSet schemas = new XmlSchemaSet();
                    schemas.Add(null, xsdFile);
                    bool flag = false;

                    doc.Validate(schemas, (o, e) => {
                        Debug.WriteLine($"Fehler beim Validieren! {e.Message} ({o})");
                        actions.Add(new PublishAction() { Text = $"    Fehler beim Validieren! {e.Message} ({o})", State = PublishState.Fail});
                        flag = true;
                    });
                    if(flag)
                    {
                        return false;
                    }
                }
                else
                {
                    Debug.WriteLine("XSD nicht gefunden. Validierung wird übersprungen");
                }
                
                doc.Root.Attributes().Where((x) => x.IsNamespaceDeclaration).Remove();
                doc.Root.Name = doc.Root.Name.LocalName;
                foreach(XElement xele in doc.Descendants())
                {
                    xele.Name = xele.Name.LocalName;
                }

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
                XElement xlang = new XElement(Get("Language"));
                xlang.SetAttributeValue("Identifier", lang.Key);

                foreach(KeyValuePair<string, Dictionary<string, string>> langitem in lang.Value) {
                    XElement xunit = new XElement(Get("TranslationUnit"));
                    xunit.SetAttributeValue("RefId", langitem.Key);
                    xlang.Add(xunit);
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
            doc.Root.Attributes().Where((x) => x.IsNamespaceDeclaration).Remove();
            doc.Root.Name = doc.Root.Name.LocalName;
            foreach(XElement xele in doc.Descendants())
            {
                xele.Name = xele.Name.LocalName;
            }
            doc.Save(GetRelPath("Temp", Manu, "Hardware.xml"));
            #endregion

            #region XML Catalog

            Debug.WriteLine($"Exportiere Catalog");
            languages.Clear();
            xmanu = CreateNewXML(Manu);
            XElement cat = new XElement(Get("Catalog"));

            foreach (CatalogItem item in general.Catalog[0].Items)
            {
                GetCatalogItems(item, cat, ProductIds, HardwareIds);
            }
            xmanu.Add(cat);

            Debug.WriteLine($"Exportiere Translations: {languages.Count} Sprachen");
            xlanguages = new XElement(Get("Languages"));
            foreach(KeyValuePair<string, Dictionary<string, Dictionary<string, string>>> lang in languages) {
                
                XElement xlang = new XElement(Get("Language"));
                xlang.SetAttributeValue("Identifier", lang.Key);

                foreach(KeyValuePair<string, Dictionary<string, string>> langitem in lang.Value) {
                    XElement xunit = new XElement(Get("TranslationUnit"));
                    xunit.SetAttributeValue("RefId", langitem.Key);
                    xlang.Add(xunit);

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

            Debug.WriteLine($"Speichere Catalog: {GetRelPath("Temp", Manu, "Catalog.xml")}");
            doc.Root.Attributes().Where((x) => x.IsNamespaceDeclaration).Remove();
            doc.Root.Name = doc.Root.Name.LocalName;
            foreach(XElement xele in doc.Descendants())
            {
                xele.Name = xele.Name.LocalName;
            }
            doc.Save(GetRelPath("Temp", Manu, "Catalog.xml"));
            #endregion
        
            #region XML Baggages

            Debug.WriteLine($"Exportiere Baggages");
            languages.Clear();
            xmanu = CreateNewXML(Manu);
            XElement xbags = new XElement(Get("Baggages"));

            //TODO only export used baggages
            foreach(Baggage bag in general.Baggages)
            {
                XElement xbag = new XElement(Get("Baggage"));
                xbag.SetAttributeValue("TargetPath", GetEncoded(bag.TargetPath));
                xbag.SetAttributeValue("Name", bag.Name + bag.Extension);
                xbag.SetAttributeValue("Id", $"M-{general.ManufacturerId.ToString("X4")}_BG-{GetEncoded(bag.TargetPath)}-{GetEncoded(bag.Name + bag.Extension)}");
            
                XElement xinfo = new XElement(Get("FileInfo"));
                //xinfo.SetAttributeValue("TimeInfo", "2022-01-28T13:55:35.2905057Z");
                string time = bag.TimeStamp.ToString("O");
                if (time.Contains("+"))
                    time = time.Substring(0, time.LastIndexOf("+"));
                xinfo.SetAttributeValue("TimeInfo", time + "Z");
                xbag.Add(xinfo);

                xbags.Add(xbag);

                if(!Directory.Exists(GetRelPath("Temp", Manu, "Baggages", bag.TargetPath)))
                    Directory.CreateDirectory(GetRelPath("Temp", Manu, "Baggages", bag.TargetPath));

                File.WriteAllBytes(GetRelPath("Temp", Manu, "Baggages", bag.TargetPath, bag.Name + bag.Extension), bag.Data);
            }

            xmanu.Add(xbags);
            doc.Save(GetRelPath("Temp", Manu, "Baggages.xml"));

            #endregion

            return true;
        }

        private void ExportSegments(AppVersion ver, XElement xparent)
        {
            Debug.WriteLine($"Exportiere Segmente: {ver.Memories.Count}x");
            XElement codes = new XElement(Get("Code"));
            foreach (Memory mem in ver.Memories)
            {
                AutoHelper.MemoryCalculation(ver, mem);
                    
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
                codes.Add(xmem);
            }
            xparent.Add(codes);
        }

        private void ExportParameters(IVersionBase vbase, XElement xparent, StringBuilder headers)
        {
            Debug.WriteLine($"Exportiere Parameter: {vbase.Parameters.Count}x");
            if(vbase.Parameters.Count == 0) return;
            XElement xparas = new XElement(Get("Parameters"));

            foreach (Parameter para in vbase.Parameters.Where(p => !p.IsInUnion))
            {
                //Debug.WriteLine($"    - Parameter {para.UId} {para.Name}");
                ParseParameter(para, xparas, vbase, headers);
            }

            Debug.WriteLine($"Exportiere Unions: {vbase.Parameters.Where(p => p.IsInUnion).GroupBy(p => p.UnionObject).Count()}x");
            foreach (var paras in vbase.Parameters.Where(p => p.IsInUnion).GroupBy(p => p.UnionObject))
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
                    ParseParameter(para, xunion, vbase, headers);
                }

                xparas.Add(xunion);
            }
            
            xparent.Add(xparas);
        }

        private void ExportParameterRefs(IVersionBase vbase, XElement xparent)
        {
            Debug.WriteLine($"Exportiere ParameterRefs: {vbase.ParameterRefs.Count}x");
            if(vbase.ParameterRefs.Count == 0) return;
            XElement xrefs = new XElement(Get("ParameterRefs"));

            foreach (ParameterRef pref in vbase.ParameterRefs)
            {
                //Debug.WriteLine($"    - ParameterRef {pref.UId} {pref.Name}");
                if (pref.ParameterObject == null) continue;
                XElement xpref = new XElement(Get("ParameterRef"));
                if (pref.Id == -1)
                {
                    pref.Id = AutoHelper.GetNextFreeId(vbase.ParameterRefs);
                }
                string id = appVersionMod + (pref.ParameterObject.IsInUnion ? "_UP-" : "_P-") + pref.ParameterObject.Id;
                xpref.SetAttributeValue("Id", $"{id}_R-{pref.Id}");
                xpref.SetAttributeValue("RefId", id);
                id += $"_R-{pref.Id}";
                xpref.SetAttributeValue("Id", id);
                if(pref.OverwriteAccess && pref.Access != ParamAccess.Default)
                    xpref.SetAttributeValue("Access", pref.Access.ToString());
                if (pref.OverwriteValue)
                    xpref.SetAttributeValue("Value", pref.Value);
                xrefs.Add(xpref);
            }

            xparent.Add(xrefs);
        }

        private void ExportComObjects(IVersionBase vbase, XElement xparent, StringBuilder headers)
        {
            Debug.WriteLine($"Exportiere ComObjects: {vbase.ComObjects.Count}x");
            XElement xcoms;
            if(vbase is Models.AppVersion)
                xcoms = new XElement(Get("ComObjectTable"));
            else
                xcoms = new XElement(Get("ComObjects"));

            Models.Argument baseNumber = null;
            if(vbase is Models.Module mod)
            {
                baseNumber = mod.ComObjectBaseNumber;
            }
            if(vbase is Models.AppVersion ver)
            {
                if(ver.ComObjectMemoryObject != null && ver.ComObjectMemoryObject.Type == MemoryTypes.Absolute)
                {
                    xcoms.SetAttributeValue("CodeSegment", $"{appVersion}_AS-{ver.ComObjectMemoryObject.Address:X4}");
                    xcoms.SetAttributeValue("Offset", ver.ComObjectTableOffset);
                }
            }

            foreach (ComObject com in vbase.ComObjects)
            {
                //Debug.WriteLine($"    - ComObject {com.UId} {com.Name}");
                if(headers != null)
                {
                    string line = $"#define COMOBJ_{com.Name.Replace(' ', '_')} \t{com.Number}\t//!< Number: {com.Number}, Text: {com.Text.Single(c => c.Language.CultureCode == currentLang).Text}, Function: {com.FunctionText.Single(c => c.Language.CultureCode == currentLang).Text}";
                    headers.AppendLine(line);
                }

                XElement xcom = new XElement(Get("ComObject"));
                if (com.Id == -1)
                {
                    com.Id = AutoHelper.GetNextFreeId(vbase.ComObjects, 0);
                }
                string id = $"{appVersionMod}_O-";
                if(vbase is Models.Module) id += "2-";
                id += com.Id;
                xcom.SetAttributeValue("Id", id);
                xcom.SetAttributeValue("Name", com.Name);
                xcom.SetAttributeValue("Text", com.Text.Single(c => c.Language.CultureCode == currentLang).Text);
                xcom.SetAttributeValue("Number", com.Number);
                xcom.SetAttributeValue("FunctionText", com.FunctionText.Single(c => c.Language.CultureCode == currentLang).Text);
                
                if(!com.TranslationText)
                    foreach(Models.Translation trans in com.Text) AddTranslation(trans.Language.CultureCode, id, "Text", trans.Text);
                if(!com.TranslationFunctionText)
                    foreach(Models.Translation trans in com.FunctionText) AddTranslation(trans.Language.CultureCode, id, "FunctionText", trans.Text);
                
                if (com.ObjectSize > 7)
                    xcom.SetAttributeValue("ObjectSize", (com.ObjectSize / 8) + " Byte"+ ((com.ObjectSize > 15) ? "s":""));
                else
                    xcom.SetAttributeValue("ObjectSize", com.ObjectSize + " Bit");

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

                if(baseNumber != null)
                    xcom.SetAttributeValue("BaseNumber", $"{appVersionMod}_A-{baseNumber.Id}");

                xcoms.Add(xcom);
            }

            xparent.Add(xcoms);
        }

        private void ExportComObjectRefs(IVersionBase vbase, XElement xparent)
        {
            Debug.WriteLine($"Exportiere ComObjectRefs: {vbase.ComObjectRefs.Count}x");
            if(vbase.ComObjectRefs.Count == 0) return;
            XElement xrefs = new XElement(Get("ComObjectRefs"));

            foreach (ComObjectRef cref in vbase.ComObjectRefs)
            {
                //Debug.WriteLine($"    - ComObjectRef {cref.UId} {cref.Name}");
                XElement xcref = new XElement(Get("ComObjectRef"));
                if (cref.Id == -1)
                {
                    cref.Id = AutoHelper.GetNextFreeId(vbase.ComObjectRefs, 0);
                }
                string id = $"{appVersionMod}_O-";
                if(vbase is Models.Module) id += "2-";
                id += cref.ComObjectObject.Id;
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

                if (cref.OverwriteDpt)
                {
                    if (cref.Type.Number == "0")
                    {
                        xcref.SetAttributeValue("DatapointType", "");
                    }
                    else
                    {
                        if (cref.OverwriteDpst)
                            xcref.SetAttributeValue("DatapointType", "DPST-" + cref.Type.Number + "-" + cref.SubType.Number);
                        else
                            xcref.SetAttributeValue("DatapointType", "DPT-" + cref.Type.Number);
                    }
                }

                if(cref.OverwriteOS)
                {
                    if (cref.ObjectSize > 7)
                        xcref.SetAttributeValue("ObjectSize", (cref.ObjectSize / 8) + " Byte" + ((cref.ObjectSize > 15) ? "s":""));
                    else
                        xcref.SetAttributeValue("ObjectSize", cref.ObjectSize + " Bit");
                }

                if (cref.ComObjectObject.UseTextParameter)
                {
                    int nsVersion = int.Parse(currentNamespace.Substring(currentNamespace.LastIndexOf('/')+1));
                    xcref.SetAttributeValue("TextParameterRefId", appVersionMod + (cref.ComObjectObject.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{cref.ComObjectObject.ParameterRefObject.ParameterObject.Id}_R-{cref.ComObjectObject.ParameterRefObject.Id}");
                }

                xrefs.Add(xcref);
            }

            xparent.Add(xrefs);
        }

        private void ParseParameter(Parameter para, XElement parent, IVersionBase ver, StringBuilder headers)
        {
            if((headers != null && para.SavePath != ParamSave.Nowhere) || (headers != null && para.IsInUnion && para.UnionObject != null && para.UnionObject.SavePath != ParamSave.Nowhere))
            {
                int offset = para.Offset;
                string line = $"#define PARAM_{para.Name.Replace(' ', '_')}";
                if(para.IsInUnion && para.UnionObject != null)
                {
                    line += $"\t0x{(para.UnionObject.Offset + para.Offset).ToString("X4")}\t//!< UnionOffset: {para.UnionObject.Offset}, ParaOffset: {para.Offset}";
                } else {
                    line += $"\t0x{para.Offset.ToString("X4")}\t//!< Offset: {para.Offset}";
                }
                if (para.OffsetBit > 0) line += ", BitOffset: " + para.OffsetBit;
                line += $", Size: {para.ParameterTypeObject.SizeInBit} Bit";
                if (para.ParameterTypeObject.SizeInBit % 8 == 0) line += " (" + (para.ParameterTypeObject.SizeInBit / 8) + " Byte)";
                line += $", Text: {para.Text.Single(p => p.Language.CultureCode == currentLang).Text}";
                headers.AppendLine(line);
            }

            XElement xpara = new XElement(Get("Parameter"));

            if (para.Id == -1)
            {
                para.Id = AutoHelper.GetNextFreeId(ver.Parameters);
            }
            string id = appVersionMod + (para.IsInUnion ? "_UP-" : "_P-") + para.Id;
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

                        if(ver is Models.Module mod)
                        {
                            xparamem.SetAttributeValue("BaseOffset", $"{appVersionMod}_A-{mod.ParameterBaseOffset.Id}");
                        }

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
                    case DynChannelIndependent dci:
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

                    case DynModule dm:
                        HandleMod(dm, xparent);
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
                if (dch.UseTextParameter)
                    channel.SetAttributeValue("TextParameterRefId", appVersionMod + (dch.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{dch.ParameterRefObject.ParameterObject.Id}_R-{dch.ParameterRefObject.Id}");

                channel.SetAttributeValue("Text", dch.Text.Single(p => p.Language.CultureCode == currentLang).Text);
                    if (!dch.TranslationText)
                        foreach (Models.Translation trans in dch.Text) AddTranslation(trans.Language.CultureCode, $"{appVersionMod}_CH-{dch.Number}", "Text", trans.Text);
                
                channel.SetAttributeValue("Number", dch.Number);
                channel.SetAttributeValue("Id", $"{appVersionMod}_CH-{dch.Number}");
                channel.SetAttributeValue("Name", ch.Name);
            }


            return channel;
        }

        private void HandleCom(DynComObject com, XElement parent)
        {
            XElement xcom = new XElement(Get("ComObjectRefRef"));
            string id = $"{appVersionMod}_O-";

            if(appVersion != appVersionMod) id += "2-";
            id += $"{com.ComObjectRefObject.ComObjectObject.Id}_R-{com.ComObjectRefObject.Id}";

            xcom.SetAttributeValue("RefId", id);
            parent.Add(xcom);
        }

        private void HandleMod(DynModule mod, XElement parent)
        {
            XElement xmod = new XElement(Get("Module"));
            if(mod.Id == -1)
                mod.Id = modCounter++;
            xmod.SetAttributeValue("Id", $"{appVersion}_MD-{mod.ModuleObject.Id}_M-{mod.Id}");
            xmod.SetAttributeValue("RefId", $"{appVersion}_MD-{mod.ModuleObject.Id}");

            foreach(DynModuleArg arg in mod.Arguments)
            {
                XElement xarg = new XElement(Get(arg.Type.ToString() + "Arg"));
                xarg.SetAttributeValue("RefId", $"{appVersion}_MD-{mod.ModuleObject.Id}_A-{arg.Argument.Id}");
                xarg.SetAttributeValue("Value", arg.Value);
                xmod.Add(xarg);
            }

            parent.Add(xmod);
        }

        private int separatorCounter = 1;
        private int modCounter = 1;

        private void HandleSep(DynSeparator sep, XElement parent)
        {
            XElement xsep = new XElement(Get("ParameterSeparator"));
            if(sep.Id == -1) {
                sep.Id = separatorCounter++; //TODO get real next free Id
            }
            xsep.SetAttributeValue("Id", $"{appVersion}_PS-{sep.Id}");
            xsep.SetAttributeValue("Text", sep.Text.Single(p => p.Language.CultureCode == currentLang).Text);
            if(sep.Hint != SeparatorHint.None)
            {
                xsep.SetAttributeValue("UIHint", sep.Hint.ToString());
            }
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

        int pbCounter = 1;
        private XElement HandleBlock(DynParaBlock bl, XElement parent)
        {
            XElement block = new XElement(Get("ParameterBlock"));
            parent.Add(block);

            if (bl.Id == -1)
            {
                bl.Id = pbCounter++; //TODO get real next free Id
            }
            if(bl.UseParameterRef)
            {
                block.SetAttributeValue("Id", $"{appVersionMod}_PB-{bl.ParameterRefObject.Id}");
                block.SetAttributeValue("ParamRefId", appVersionMod + (bl.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{bl.ParameterRefObject.ParameterObject.Id}_R-{bl.ParameterRefObject.Id}");
            }
            else
            {
                block.SetAttributeValue("Id", $"{appVersionMod}_PB-{bl.Id}");string dText = bl.Text.Single(p => p.Language.CultureCode == currentLang).Text;
                if (!string.IsNullOrEmpty(dText))
                {
                    block.SetAttributeValue("Text", dText);
                    if (!bl.TranslationText)
                        foreach (Models.Translation trans in bl.Text) AddTranslation(trans.Language.CultureCode, $"{appVersion}_PB-{bl.Id}", "Text", trans.Text);
                }
            }


            

            if(!string.IsNullOrEmpty(bl.Name))
                block.SetAttributeValue("Name", bl.Name);

            if (bl.UseTextParameter)
                block.SetAttributeValue("TextParameterRefId", appVersionMod + (bl.TextRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{bl.TextRefObject.ParameterObject.Id}_R-{bl.TextRefObject.Id}");

            return block;
        }

        private void HandleParam(DynParameter pa, XElement parent)
        {
            XElement xpara = new XElement(Get("ParameterRefRef"));
            parent.Add(xpara);
            xpara.SetAttributeValue("RefId", appVersionMod + (pa.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{pa.ParameterRefObject.ParameterObject.Id}_R-{pa.ParameterRefObject.Id}");
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
                string id;
                
                if (CheckSections(item))
                {
                    if (item.Parent.Parent == null)
                    {
                        id = $"M-{general.ManufacturerId.ToString("X4")}_CS-" + GetEncoded(item.Number);
                        xitem.SetAttributeValue("Id", id);
                    }
                    else
                    {
                        id = parent.Attribute("Id").Value;
                        id += "-" + GetEncoded(item.Number);
                        xitem.SetAttributeValue("Id", id);
                    }

                    xitem.SetAttributeValue("Name", item.Text.Single(e => e.Language.CultureCode == currentLang).Text);
                    xitem.SetAttributeValue("Number", item.Number);
                    xitem.SetAttributeValue("DefaultLanguage", currentLang);
                    parent.Add(xitem);

                    foreach(Translation trans in item.Text) AddTranslation(trans.Language.CultureCode, id, "Name", trans.Text);
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
                            xitem.SetAttributeValue("Name", dev.Text.Single(e => e.Language.CultureCode == currentLang).Text);
                            xitem.SetAttributeValue("Number", item.Number);
                            xitem.SetAttributeValue("VisibleDescription", dev.Description.Single(e => e.Language.CultureCode == currentLang).Text);
                            xitem.SetAttributeValue("ProductRefId", productIds[dev.Name]);
                            string hardid = item.Hardware.Version + "-" + app.Number + "-" + ver.Number;
                            xitem.SetAttributeValue("Hardware2ProgramRefId", hardwareIds[hardid]);
                            xitem.SetAttributeValue("DefaultLanguage", currentLang);
                            parent.Add(xitem);

                            foreach(Translation trans in dev.Text) AddTranslation(trans.Language.CultureCode, id, "Name", trans.Text);
                            foreach(Translation trans in dev.Description) AddTranslation(trans.Language.CultureCode, id, "VisibleDescription", trans.Text);
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
            
            int nsVersion = int.Parse(currentNamespace.Substring(currentNamespace.LastIndexOf('/')+1));;
            foreach (string file in Directory.GetFiles(GetRelPath("Temp", manu)))
            {
                if (!file.Contains("M-") || !file.Contains("_A-")) continue;

                FileInfo info = new FileInfo(file);
                ApplicationProgramHasher aph = new ApplicationProgramHasher(info, mapBaggageIdToFileIntegrity, convPath, nsVersion, true);
                aph.HashFile();

                string oldApplProgId = aph.OldApplProgId;
                string newApplProgId = aph.NewApplProgId;
                string genHashString = aph.GeneratedHashString;

                applProgIdMappings.Add(oldApplProgId, newApplProgId);
                if (!applProgHashes.ContainsKey(newApplProgId))
                    applProgHashes.Add(newApplProgId, genHashString);
            }

            HardwareSigner hws = new HardwareSigner(hwFileInfo, applProgIdMappings, applProgHashes, convPath, nsVersion, true);
            hws.SignFile();
            IDictionary<string, string> hardware2ProgramIdMapping = hws.OldNewIdMappings;

            CatalogIdPatcher cip = new CatalogIdPatcher(catalogFileInfo, hardware2ProgramIdMapping, convPath, nsVersion);
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
            if(input == null)
            {
                Debug.WriteLine("GetEncoded: Input was null");
                return "";
            }
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
            return XName.Get(name, currentNamespace);
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
