using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using Kaenx.Creator.Signing;
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;

namespace Kaenx.Creator.Classes
{
    public class ExportHelper
    {
        List<Models.Hardware> hardware;
        List<Models.Device> devices;
        List<Models.Application> apps;
        List<Models.AppVersionModel> vers;
        Models.ModelGeneral general;
        XDocument doc;
        string appVersion;
        string appVersionMod;
        string currentNamespace;
        string convPath;
        List<Icon> iconsApp = new List<Icon>();
        List<string> buttonScripts;

        public ExportHelper(Models.ModelGeneral g, List<Models.Hardware> h, List<Models.Device> d, List<Models.Application> a, List<Models.AppVersionModel> v, string cp)
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
            foreach (Models.AppVersionModel ver in vers)
            {
                Regex reg = new Regex("NamespaceVersion\":[ ]?([0-9]{2})");
                Match m = reg.Match(ver.Version);
                if(!m.Success) continue;
                int nsv = int.Parse(m.Groups[1].Value);
                if (nsv > highestNS)
                    highestNS = nsv;
            }
            currentNamespace = $"http://knx.org/xml/project/{highestNS}";

            Dictionary<string, string> ProductIds = new Dictionary<string, string>();
            Dictionary<string, string> HardwareIds = new Dictionary<string, string>();
            List<Baggage> baggagesManu = new List<Baggage>();
            bool exportIcons = false;

            #region XML Applications
            Debug.WriteLine($"Exportiere Applikationen: {vers.Count}x");
            XElement xmanu = null;
            XElement xlanguages = null;
            foreach(Models.AppVersionModel model in vers) {
                Models.AppVersion ver = model.Model != null ? model.Model : AutoHelper.GetAppVersion(general, model);
                Debug.WriteLine($"Exportiere AppVersion: {ver.Name} {ver.NameText}");
                languages = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
                xmanu = CreateNewXML(Manu);
                XElement xapps = new XElement(Get("ApplicationPrograms"));
                xmanu.Add(xapps);
                Models.Application app = apps.Single(a => a.Versions.Contains(model));

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
                xapp.SetAttributeValue("Name", GetDefaultLanguage(ver.Text));
                xapp.SetAttributeValue("DefaultLanguage", currentLang);
                xapp.SetAttributeValue("LoadProcedureStyle", $"{app.Mask.Procedure}Procedure");
                xapp.SetAttributeValue("PeiType", "0");
                xapp.SetAttributeValue("DynamicTableManagement", "false"); //TODO check when to add
                xapp.SetAttributeValue("Linkable", "false"); //TODO check when to add

                buttonScripts = new List<string>();
                iconsApp = new List<Icon>();
                List<Baggage> baggagesApp = new List<Baggage>();
                if(ver.IsHelpActive)
                {
                    if(ver.NamespaceVersion == 14)
                    {
                        xapp.SetAttributeValue("ContextHelpFile", "HelpFile_" + ver.DefaultLanguage + ".zip");
                    } else {
                        xapp.SetAttributeValue("ContextHelpFile", $"{Manu}_BG--{GetEncoded("HelpFile_" + ver.DefaultLanguage + ".zip")}");
                    }
                    ExportHelptexts(ver, Manu, baggagesManu, baggagesApp);
                }

                if(ver.IsPreETS4)
                {
                    xapp.SetAttributeValue("PreEts4Style", "true"); //TODO check when to add
                    xapp.SetAttributeValue("ConvertedFromPreEts4Data", "true"); //TODO check when to add
                }

                if(!string.IsNullOrEmpty(ver.ReplacesVersions)) xapp.SetAttributeValue("ReplacesVersions", ver.ReplacesVersions);

                switch (currentNamespace)
                {
                    case "http://knx.org/xml/project/11":
                        xapp.SetAttributeValue("MinEtsVersion", "4.0");
                        break;
                    case "http://knx.org/xml/project/12":
                    case "http://knx.org/xml/project/13":
                    case "http://knx.org/xml/project/14":
                    case "http://knx.org/xml/project/20":
                        xapp.SetAttributeValue("MinEtsVersion", "5.0");
                        break;
                    case "http://knx.org/xml/project/21":
                        xapp.SetAttributeValue("MinEtsVersion", "6.0");
                        break;
                }

                AutoHelper.CheckIds(ver);

                XElement temp;
                ExportSegments(ver, xunderapp);

                #region ParamTypes/Baggages
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

                            switch(type.Type)
                            {
                                case ParameterTypes.NumberUInt:
                                    xcontent.SetAttributeValue("Type", "unsignedInt");
                                    break;
                                
                                case ParameterTypes.NumberInt:
                                    xcontent.SetAttributeValue("Type", "signedInt");
                                    break;

                                case ParameterTypes.Float_DPT9:
                                    xcontent.SetAttributeValue("Encoding", "DPT 9");
                                    break;

                                case ParameterTypes.Float_IEEE_Single:
                                    xcontent.SetAttributeValue("Encoding", "IEEE-754 Single");
                                    break;

                                case ParameterTypes.Float_IEEE_Double:
                                    xcontent.SetAttributeValue("Encoding", "IEEE-754 Double");
                                    break;

                                default:
                                    throw new Exception("Unbekannter ParameterType: " + type.Type.ToString());
                            }
                            xcontent.SetAttributeValue("minInclusive", type.Min.Replace(",", "."));
                            xcontent.SetAttributeValue("maxInclusive", type.Max.Replace(",", "."));
                            if(type.Increment != "1")
                                xcontent.SetAttributeValue("Increment", type.Increment.Replace(",", "."));
                            if(type.UIHint != "None" && !string.IsNullOrEmpty(type.UIHint))
                                xcontent.SetAttributeValue("UIHint", type.UIHint);
                            if(type.DisplayOffset != "0")
                                xcontent.SetAttributeValue("DisplayOffset", type.DisplayOffset);
                            if(type.DisplayFactor != "1")
                                xcontent.SetAttributeValue("DisplayFactor", type.DisplayFactor);
                            break;

                        case ParameterTypes.Enum:
                            xcontent = new XElement(Get("TypeRestriction"));
                            xcontent.SetAttributeValue("Base", "Value");
                            foreach (ParameterTypeEnum enu in type.Enums)
                            {
                                XElement xenu = new XElement(Get("Enumeration"));
                                xenu.SetAttributeValue("Text", GetDefaultLanguage(enu.Text));
                                xenu.SetAttributeValue("Value", enu.Value);
                                xenu.SetAttributeValue("Id", $"{id}_EN-{enu.Value}");
                                xcontent.Add(xenu);
                                if(enu.Translate)
                                    foreach(Models.Translation trans in enu.Text) AddTranslation(trans.Language.CultureCode, $"{id}_EN-{enu.Value}", "Text", trans.Text);
                            
                                if(enu.UseIcon)
                                {
                                    xenu.SetAttributeValue("Icon", enu.IconObject.Name);
                                    if(!iconsApp.Contains(enu.IconObject))
                                        iconsApp.Add(enu.IconObject);
                                }
                            }
                            break;

                        case ParameterTypes.Picture:
                            xcontent = new XElement(Get("TypePicture"));
                            xcontent.SetAttributeValue("RefId", $"M-{general.ManufacturerId:X4}_BG-{GetEncoded(type.BaggageObject.TargetPath)}-{GetEncoded(type.BaggageObject.Name + type.BaggageObject.Extension)}");
                            if (!baggagesApp.Contains(type.BaggageObject))
                                baggagesApp.Add(type.BaggageObject);
                            break;

                        case ParameterTypes.IpAddress:
                            xcontent = new XElement(Get("TypeIPAddress"));
                            xcontent.SetAttributeValue("AddressType", type.UIHint);
                            if(type.Increment == "IPv6")
                            {
                                xcontent.SetAttributeValue("Version", type.Increment);
                            }
                            break;

                        case ParameterTypes.Color:
                            xcontent = new XElement(Get("TypeColor"));
                            xcontent.SetAttributeValue("Space", type.UIHint);
                            break;

                        case ParameterTypes.RawData:
                            xcontent = new XElement(Get("TypeRawData"));
                            xcontent.SetAttributeValue("MaxSize", type.Max);
                            break;

                        case ParameterTypes.Date:
                            xcontent = new XElement(Get("TypeDate"));
                            xcontent.SetAttributeValue("Encoding", type.UIHint);
                            if(!type.OtherValue)
                                xcontent.SetAttributeValue("DisplayTheYear", "false");
                            break;

                        default:
                            throw new Exception("Unbekannter Parametertyp: " + type.Type);
                    }

                    if (xcontent != null && 
                        xcontent.Name.LocalName != "TypeFloat" &&
                        xcontent.Name.LocalName != "TypeNone" &&
                        xcontent.Name.LocalName != "TypePicture" &&
                        xcontent.Name.LocalName != "TypeColor" &&
                        xcontent.Name.LocalName != "TypeDate" &&
                        xcontent.Name.LocalName != "TypeRawData" &&
                        xcontent.Name.LocalName != "TypeIPAddress")
                    {
                        xcontent.SetAttributeValue("SizeInBit", type.SizeInBit);
                    }
                    if (xcontent != null)
                        xtype.Add(xcontent);
                    temp.Add(xtype);
                }
                xunderapp.Add(temp);
                XElement xextension = new XElement(Get("Extension"));

                if (baggagesApp.Count > 0)
                {
                    foreach(Baggage bag in baggagesApp)
                    {
                        XElement xbag = new XElement(Get("Baggage"));
                        xbag.SetAttributeValue("RefId", $"M-{general.ManufacturerId:X4}_BG-{GetEncoded(bag.TargetPath)}-{GetEncoded(bag.Name + bag.Extension)}");
                        xextension.Add(xbag);
                        if (!baggagesManu.Contains(bag))
                            baggagesManu.Add(bag);
                    }
                }

                #endregion

                StringBuilder headers = new StringBuilder();

                
                headers.AppendLine("//--------------------Allgemein---------------------------");
                headers.AppendLine($"#define MAIN_OpenKnxId 0x{(app.Number >> 8):X2}");
                headers.AppendLine($"#define MAIN_ApplicationNumber 0x{app.Number:X4}");
                headers.AppendLine($"#define MAIN_ApplicationVersion 0x{ver.Number:X2}");
                headers.AppendLine($"#define MAIN_OrderNumber \"{hardware.First(h => h.Apps.Contains(app)).Devices.First().OrderNumber}\" //may not work with multiple devices on same hardware or app on different hardware");
                headers.AppendLine();

                ExportParameters(ver, xunderapp, headers);
                ExportParameterRefs(ver, xunderapp);
                ExportComObjects(ver, xunderapp, headers);
                ExportComObjectRefs(ver, ver, xunderapp);

                #region "Tables / LoadProcedure"
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

                if (app.Mask.Procedure != ProcedureTypes.Default)
                {
                    temp = XElement.Parse(ver.Procedure);
                    //Write correct Memory Size if AutoLoad is activated
                    foreach (XElement xele in temp.Descendants())
                    {
                        switch (xele.Name.LocalName)
                        {
                            case "LdCtrlWriteRelMem":
                                {
                                    if (xele.Attribute("ObjIdx").Value == "4" && ver.Memories[0].IsAutoLoad)
                                    {
                                        xele.SetAttributeValue("Size", ver.Memories[0].Size);
                                    }
                                    break;
                                }

                            case "LdCtrlRelSegment":
                                {
                                    if (xele.Attribute("LsmIdx").Value == "4" && ver.Memories[0].IsAutoLoad)
                                    {
                                        xele.SetAttributeValue("Size", ver.Memories[0].Size);
                                    }
                                    break;
                                }
                        }
                    }
                    ver.Procedure = temp.ToString();
                }
                temp.Attributes().Where((x) => x.IsNamespaceDeclaration).Remove();
                temp.Name = XName.Get(temp.Name.LocalName, currentNamespace);
                foreach(XElement xele in temp.Descendants())
                {
                    xele.Name = XName.Get(xele.Name.LocalName, currentNamespace);
                    switch(xele.Name.LocalName)
                    {
                        case "OnError":
                        {
                            int id = int.Parse(xele.Attribute("MessageRef").Value);
                            Message msg = ver.Messages.SingleOrDefault(m => m.UId == id);
                            xele.SetAttributeValue("MessageRef", $"{appVersion}_M-{msg.Id}");
                            break;
                        }
                    }
                }
                xunderapp.Add(temp);
                #endregion


                xunderapp.Add(xextension);


                if(ver.IsMessagesActive && ver.Messages.Count > 0)
                {
                    temp = new XElement(Get("Messages"));
                    foreach(Message msg in ver.Messages)
                    {
                        if(msg.Id == -1)
                            msg.Id = AutoHelper.GetNextFreeId(ver, "Messages");

                        XElement xmsg = new XElement(Get("Message"));
                        xmsg.SetAttributeValue("Id", $"{appVersion}_M-{msg.Id}");
                        xmsg.SetAttributeValue("Name", msg.Name);
                        xmsg.SetAttributeValue("Text",  GetDefaultLanguage(msg.Text));
                        temp.Add(xmsg);

                        if(msg.TranslationText)
                            foreach(Translation trans in msg.Text)
                                AddTranslation(trans.Language.CultureCode, $"{appVersion}_M-{msg.Id}", "Text", trans.Text);
                    }
                    xunderapp.Add(temp);
                }

                XElement xscript = new XElement(Get("Script"), "");
                xunderapp.Add(xscript);

                
                #region Modules

                if(ver.Allocators.Count > 0)
                {
                    XElement xallocs = new XElement(Get("Allocators"));
                    xunderapp.Add(xallocs);
                    
                    foreach(Models.Allocator alloc in ver.Allocators)
                    {
                        XElement xalloc = new XElement(Get("Allocator"));

                        if (alloc.Id == -1)
                            alloc.Id = AutoHelper.GetNextFreeId(ver, "Allocators");
                        xalloc.SetAttributeValue("Id", $"{appVersionMod}_L-{alloc.Id}");
                        xalloc.SetAttributeValue("Name", alloc.Name);
                        xalloc.SetAttributeValue("Start", alloc.Start);
                        xalloc.SetAttributeValue("maxInclusive", alloc.Max);
                        //TODO errormessageid
                        
                        xallocs.Add(xalloc);
                    }
                }



                if(ver.Modules.Count > 0)
                {
                    headers.AppendLine("");
                    headers.AppendLine("//---------------------Modules----------------------------");
                }

                ExportModules(xapp, ver, ver.Modules, appVersion, headers, appVersion);
                appVersionMod = appVersion;

                List<DynModule> mods = new List<DynModule>();
                AutoHelper.GetModules(ver.Dynamics[0], mods);

                if(mods.Count > 0)
                {
                    headers.AppendLine("");
                    headers.AppendLine("//-----Module specific starts");
                }

                Dictionary<string, List<long>> modStartPara = new Dictionary<string, List<long>>();
                Dictionary<string, List<long>> modStartComs = new Dictionary<string, List<long>>();
                Dictionary<string, long> allocators = new Dictionary<string, long>();
                foreach(DynModule dmod in mods)
                {
                    if(!modStartPara.ContainsKey(dmod.ModuleObject.Name))
                        modStartPara.Add(dmod.ModuleObject.Name, new List<long>());
                    if(!modStartComs.ContainsKey(dmod.ModuleObject.Name))
                        modStartComs.Add(dmod.ModuleObject.Name, new List<long>());

                    DynModuleArg dargp = dmod.Arguments.Single(a => a.ArgumentId == dmod.ModuleObject.ParameterBaseOffsetUId);
                    if(dargp.UseAllocator)
                    {
                        if(!allocators.ContainsKey(dargp.Allocator.Name))
                            allocators.Add(dargp.Allocator.Name, dargp.Allocator.Start);

                        modStartPara[dmod.ModuleObject.Name].Add(allocators[dargp.Allocator.Name]);

                        allocators[dargp.Allocator.Name] += dargp.Argument.Allocates;
                    } else {
                        long poffset = long.Parse(dargp.Value);
                        modStartPara[dmod.ModuleObject.Name].Add(poffset);
                    }
                    

                    
                    DynModuleArg dargc = dmod.Arguments.Single(a => a.ArgumentId == dmod.ModuleObject.ComObjectBaseNumberUId);
                    if(dargc.UseAllocator)
                    {
                        if(!allocators.ContainsKey(dargc.Allocator.Name))
                            allocators.Add(dargc.Allocator.Name, dargc.Allocator.Start);

                        modStartComs[dmod.ModuleObject.Name].Add(allocators[dargc.Allocator.Name]);

                        allocators[dargc.Allocator.Name] += dargc.Argument.Allocates;
                    } else {
                        int coffset = int.Parse(dargc.Value);
                        modStartComs[dmod.ModuleObject.Name].Add(coffset);
                    }
                }

                foreach(KeyValuePair<string, List<long>> item in modStartPara)
                    headers.AppendLine($"const long mod_{HeaderNameEscape(item.Key)}_para[] = {{ {string.Join(',', item.Value)} }};");
                foreach(KeyValuePair<string, List<long>> item in modStartComs)
                    headers.AppendLine($"const long mod_{HeaderNameEscape(item.Key)}_coms[] = {{ {string.Join(',', item.Value)} }};");

                System.IO.File.WriteAllText(GetRelPath(appVersion + ".h"), headers.ToString());
                headers = null;

                #endregion


                XElement xdyn = new XElement(Get("Dynamic"));
                HandleSubItems(ver.Dynamics[0], xdyn, ver);


                if(buttonScripts.Count > 0)
                {
                    string scripts = "";
                    scripts += string.Join(null, buttonScripts);
                    xscript.Value += scripts;
                }

                if(string.IsNullOrEmpty(xscript.Value))
                    xscript.Remove();


                if(iconsApp.Count > 0)
                {
                    string zipName = "Icons_" + general.GetGuid();
                    Baggage bag = new Baggage() {
                        Name = zipName,
                        Extension = ".zip",
                        LastModified = DateTime.Now
                    };
                    baggagesManu.Add(bag);
                    if(ver.NamespaceVersion == 14)
                    {
                        xapp.SetAttributeValue("IconFile", $"{zipName}.zip");
                    } else {
                        xapp.SetAttributeValue("IconFile", $"{Manu}_BG--{GetEncoded($"{zipName}.zip")}");
                    }

                    XElement xbag = new XElement(Get("Baggage"));
                    xbag.SetAttributeValue("RefId", $"M-{general.ManufacturerId:X4}_BG--{GetEncoded($"{zipName}.zip")}");
                    xextension.Add(xbag);
                    exportIcons = true;
                }
                
                if(!xextension.HasElements)
                    xextension.Remove();

                xapp.Add(xdyn);


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

                        if(xele.HasElements)
                            xunit.Add(xele);
                    }
                    if(xlang.HasElements)
                        xlanguages.Add(xlang);
                }
                xmanu.Add(xlanguages);
                #endregion

                string nsnumber = currentNamespace.Substring(currentNamespace.LastIndexOf('/') + 1);
                string xsdFile = "Data\\knx_project_" + nsnumber + ".xsd";
                if (File.Exists(xsdFile))
                {
                    doc.Save(GetRelPath("Temp", Manu, appVersion + ".validate.xml"));
                    Debug.WriteLine("XSD gefunden. Validierung wird ausgeführt");
                    XmlSchemaSet schemas = new XmlSchemaSet();
                    schemas.Add(null, xsdFile);
                    bool flag = false;

                    XDocument doc2 = XDocument.Load(GetRelPath("Temp", Manu, appVersion + ".validate.xml"), LoadOptions.SetLineInfo);

                    doc2.Validate(schemas, (o, e) => {
                        Debug.WriteLine($"Fehler beim Validieren! Zeile {e.Exception.LineNumber}:{e.Exception.LinePosition}\r\n--->{e.Message}\r\n--->({o})");
                        actions.Add(new PublishAction() { Text = $"    Fehler beim Validieren! Zeile {e.Exception.LineNumber}:{e.Exception.LinePosition} -> {e.Message} ({o})", State = PublishState.Fail});
                        flag = true;
                    });

                    if(!flag)
                        File.Delete(GetRelPath("Temp", Manu, appVersion + ".validate.xml"));

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
                if (hard.IsCoppler) xhard.SetAttributeValue("IsCoupler", "1");
                if (hard.IsPowerSupply) xhard.SetAttributeValue("IsPowerSupply", "1");
                //xhard.SetAttributeValue("IsCable", "0"); //Todo check if means PoweLine Cable
                //xhard.SetAttributeValue("IsChoke", "0"); //Ist immer 0 da keine Drossel
                //xhard.SetAttributeValue("IsPowerLineRepeater", "0");
                //xhard.SetAttributeValue("IsPowerLineSignalFilter", "0");
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
                    xprod.SetAttributeValue("Text", GetDefaultLanguage(dev.Text));
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

                    foreach (Models.AppVersionModel ver in app.Versions)
                    {
                        //if (!vers.Contains(ver)) continue;
                        
                        string appidx = app.Number.ToString("X4") + "-" + ver.Number.ToString("X2") + "-0000";

                        XElement xh2p = new XElement(Get("Hardware2Program"));
                        xh2p.SetAttributeValue("Id", hid + "_HP-" + appidx);
                        xh2p.SetAttributeValue("MediumTypes", app.Mask.MediumTypes);

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
        
            #region XML Baggages/Icons

            if(baggagesManu.Count > 0)
            {
                Debug.WriteLine($"Exportiere Baggages");
                languages.Clear();
                xmanu = CreateNewXML(Manu);
                XElement xbags = new XElement(Get("Baggages"));

                //TODO only export used baggages
                foreach (Baggage bag in baggagesManu)
                {
                    XElement xbag = new XElement(Get("Baggage"));
                    xbag.SetAttributeValue("TargetPath", GetEncoded(bag.TargetPath));
                    xbag.SetAttributeValue("Name", bag.Name + bag.Extension);
                    xbag.SetAttributeValue("Id", $"M-{general.ManufacturerId.ToString("X4")}_BG-{GetEncoded(bag.TargetPath)}-{GetEncoded(bag.Name + bag.Extension)}");

                    XElement xinfo = new XElement(Get("FileInfo"));
                    //xinfo.SetAttributeValue("TimeInfo", "2022-01-28T13:55:35.2905057Z");
                    string time = bag.LastModified.ToString("O");
                    if (time.Contains("+"))
                        time = time.Substring(0, time.LastIndexOf("+"));
                    xinfo.SetAttributeValue("TimeInfo", time + "Z");
                    xbag.Add(xinfo);

                    xbags.Add(xbag);

                    if (!Directory.Exists(GetRelPath("Temp", Manu, "Baggages", bag.TargetPath)))
                        Directory.CreateDirectory(GetRelPath("Temp", Manu, "Baggages", bag.TargetPath));

                    if(bag.Data != null)
                    {
                        File.WriteAllBytes(GetRelPath("Temp", Manu, "Baggages", bag.TargetPath, bag.Name + bag.Extension), bag.Data);
                        File.SetLastWriteTime(GetRelPath("Temp", Manu, "Baggages", bag.TargetPath, bag.Name + bag.Extension), bag.LastModified);
                    }
                }

                xmanu.Add(xbags);
                doc.Save(GetRelPath("Temp", Manu, "Baggages.xml"));
            } else
            {
                Debug.WriteLine($"Exportiere keine Baggages");
            }

            if(exportIcons)
            {
                string zipName = "Icons_" + general.GetGuid() + ".zip";
                using (var stream = new FileStream(GetRelPath("Temp", Manu, "Baggages", zipName), FileMode.Create))
                    using (var archive = new ZipArchive(stream , ZipArchiveMode.Create, false,  System.Text.Encoding.GetEncoding(850)))
                    {
                        foreach(Icon icon in general.Icons)
                        {
                            ZipArchiveEntry entry = archive.CreateEntry(icon.Name + ".png");
                            using(Stream s = entry.Open())
                            {
                                s.Write(icon.Data, 0, icon.Data.Length);
                            }
                        }
                    }

                DateTime last = general.Icons.OrderByDescending(i => i.LastModified).First().LastModified;
                File.SetLastWriteTime(GetRelPath("Temp", Manu, "Baggages", zipName), last);
            }
            

            #endregion

            return true;
        }

        public static string HeaderNameEscape(string name)
        {
            return name.Replace(' ', '_').Replace('-', '_');
        }

        private void ExportModules(XElement xparent, AppVersion ver, ObservableCollection<Models.Module> Modules, string modVersion, StringBuilder headers, string moduleName, int depth = 0)
        {
            /*if(ver.Allocators.Count > 0)
            {
                XElement xallocs = new XElement(Get("Allocators"));
                xunderapp.Add(xallocs);
                
                foreach(Models.Allocator alloc in ver.Allocators)
                {
                    XElement xalloc = new XElement(Get("Allocator"));

                    if (alloc.Id == -1)
                        alloc.Id = AutoHelper.GetNextFreeId(ver, "Allocators");
                    xalloc.SetAttributeValue("Id", $"{appVersionMod}_L-{alloc.Id}");
                    xalloc.SetAttributeValue("Name", alloc.Name);
                    xalloc.SetAttributeValue("Start", alloc.Start);
                    xalloc.SetAttributeValue("maxInclusive", alloc.Max);
                    //TODO errormessageid
                    
                    xallocs.Add(xalloc);
                }
            }*/

            if(Modules.Count > 0)
            {
                string subName = depth == 0 ? "ModuleDefs" : "SubModuleDefs";
                XElement xunderapp = new XElement(Get(subName));
                xparent.Add(xunderapp);

                int counter = 0;

                foreach (Models.Module mod in Modules)
                {
                    counter++;
                    mod.Id = counter;
                    headers.AppendLine("//-----Module: " + mod.Name);
                    //if (mod.Id == -1)
                    //    mod.Id = AutoHelper.GetNextFreeId(vers, "Modules");

                    XElement temp = new XElement(Get("Arguments"));
                    XElement xmod = new XElement(Get("ModuleDef"), temp);
                    xmod.SetAttributeValue("Name", mod.Name);

                    appVersionMod = $"{modVersion}_{(depth == 0 ? "MD" : "SM")}-{mod.Id}";
                    string newModVersion = appVersionMod;
                    xmod.SetAttributeValue("Id", $"{appVersionMod}");

                    foreach (Models.Argument arg in mod.Arguments)
                    {
                        XElement xarg = new XElement(Get("Argument"));
                        if (arg.Id == -1)
                            arg.Id = AutoHelper.GetNextFreeId(mod, "Arguments");
                        xarg.SetAttributeValue("Id", $"{appVersionMod}_A-{arg.Id}");
                        xarg.SetAttributeValue("Name", arg.Name);
                        xarg.SetAttributeValue("Allocates", arg.Allocates);
                        temp.Add(xarg);
                    }
                    XElement xunderstatic = new XElement(Get("Static"));
                    xmod.Add(xunderstatic);
                    xunderapp.Add(xmod);


                    ExportParameters(mod, xunderstatic, headers);
                    ExportParameterRefs(mod, xunderstatic);
                    ExportComObjects(mod, xunderstatic, headers);
                    ExportComObjectRefs(mod, ver, xunderstatic);

                    if(mod.Allocators.Count > 0)
                    {
                        XElement xallocs = new XElement(Get("Allocators"));
                        xunderstatic.Add(xallocs);
                        
                        foreach(Models.Allocator alloc in mod.Allocators)
                        {
                            XElement xalloc = new XElement(Get("Allocator"));

                            if (alloc.Id == -1)
                                alloc.Id = AutoHelper.GetNextFreeId(mod, "Allocators");
                            xalloc.SetAttributeValue("Id", $"{appVersionMod}_L-{alloc.Id}");
                            xalloc.SetAttributeValue("Name", alloc.Name);
                            xalloc.SetAttributeValue("Start", alloc.Start);
                            xalloc.SetAttributeValue("maxInclusive", alloc.Max);
                            //TODO errormessageid
                            
                            xallocs.Add(xalloc);
                        }
                    }
                    ExportModules(xmod, ver, mod.Modules, appVersionMod, headers, newModVersion, depth + 1);

                    appVersionMod = $"{modVersion}_{(depth == 0 ? "MD" : "SM")}-{mod.Id}";
                    
                    XElement xmoddyn = new XElement(Get("Dynamic"));
                    xmod.Add(xmoddyn);

                    HandleSubItems(mod.Dynamics[0], xmoddyn, ver);

                    headers.AppendLine("");

                    appVersionMod = modVersion;
                }
            }
        }

        private void ExportHelptexts(AppVersion ver, string manu, List<Baggage> baggagesManu, List<Baggage> baggagesApp)
        {
            if(ver.Helptexts.Count == 0) return;
            if(System.IO.Directory.Exists("HelpTemp"))
                System.IO.Directory.Delete("HelpTemp", true);
            System.IO.Directory.CreateDirectory("HelpTemp");

            foreach(Language lang in ver.Languages)
            {
                if(!System.IO.Directory.Exists(System.IO.Path.Combine("HelpTemp", lang.CultureCode)))
                    System.IO.Directory.CreateDirectory(System.IO.Path.Combine("HelpTemp", lang.CultureCode));
            }

            foreach(Helptext text in ver.Helptexts)
            {
                foreach(Translation trans in text.Text)
                {
                    System.IO.File.WriteAllText(System.IO.Path.Combine("HelpTemp", trans.Language.CultureCode, text.Name + ".txt"), trans.Text);
                }
            }


            if(!System.IO.Directory.Exists(System.IO.Path.Combine("Output", "Temp", manu, "Baggages")))
                System.IO.Directory.CreateDirectory(System.IO.Path.Combine("Output", "Temp", manu, "Baggages"));
            
            foreach(Language lang in ver.Languages)
            {
                string destPath = System.IO.Path.Combine("Output", "Temp", manu, "Baggages", "HelpFile_" + lang.CultureCode + ".zip");
                System.IO.Compression.ZipFile.CreateFromDirectory(System.IO.Path.Combine("HelpTemp", lang.CultureCode), destPath);
                Baggage bag = new Baggage() {
                    Name = "HelpFile_" + lang.CultureCode,
                    Extension = ".zip",
                    LastModified = DateTime.Now
                };
                if(!baggagesManu.Contains(bag))
                    baggagesManu.Add(bag);
                baggagesApp.Add(bag);
                if(ver.NamespaceVersion == 14)
                    AddTranslation(lang.CultureCode, appVersion, "ContextHelpFile", "HelpFile_" + lang.CultureCode + ".zip");
                else
                    AddTranslation(lang.CultureCode, appVersion, "ContextHelpFile", $"{manu}_BG--{GetEncoded("HelpFile_" + lang.CultureCode + ".zip")}");
            }

            System.IO.Directory.Delete("HelpTemp", true);
        }

        private void ExportSegments(AppVersion ver, XElement xparent)
        {
            Debug.WriteLine($"Exportiere Segmente: {ver.Memories.Count}x");
            XElement codes = new XElement(Get("Code"));
            foreach (Memory mem in ver.Memories)
            {
                if(mem.IsAutoPara)
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
                        id = $"{appVersion}_RS-04-{mem.Offset:X5}";
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
                    case SavePaths.Memory:
                        XElement xmem = new XElement(Get("Memory"));
                        string memid = $"{appVersion}_";
                        if (paras.Key.MemoryObject.Type == MemoryTypes.Absolute)
                            memid += $"AS-{paras.Key.MemoryObject.Address:X4}";
                        else
                            memid += $"RS-04-{paras.Key.MemoryObject.Offset:X5}";
                        xmem.SetAttributeValue("CodeSegment", memid);
                        xmem.SetAttributeValue("Offset", paras.Key.Offset);
                        xmem.SetAttributeValue("BitOffset", paras.Key.OffsetBit);
                        if(vbase is Models.Module mod)
                        {
                            xmem.SetAttributeValue("BaseOffset", $"{appVersionMod}_A-{mod.ParameterBaseOffset.Id}");
                        }
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
                string id = appVersionMod + (pref.ParameterObject.IsInUnion ? "_UP-" : "_P-") + pref.ParameterObject.Id;
                xpref.SetAttributeValue("Id", $"{id}_R-{pref.Id}");
                xpref.SetAttributeValue("RefId", id);
                id += $"_R-{pref.Id}";
                xpref.SetAttributeValue("Id", id);
                if(!string.IsNullOrEmpty(pref.Name))
                    xpref.SetAttributeValue("Name", pref.Name);

                if(pref.OverwriteAccess && pref.Access != ParamAccess.ReadWrite)
                    xpref.SetAttributeValue("Access", pref.Access.ToString());
                if (pref.OverwriteValue)
                    xpref.SetAttributeValue("Value", pref.Value);
                if(pref.OverwriteText)
                {
                    xpref.SetAttributeValue("Text", GetDefaultLanguage(pref.Text));
                    if(!pref.ParameterObject.TranslationText)
                    foreach(Models.Translation trans in pref.Text) AddTranslation(trans.Language.CultureCode, id, "SuffixText", trans.Text);
                }
                if(pref.OverwriteSuffix)
                {
                    xpref.SetAttributeValue("SuffixText", pref.Suffix.Single(p => p.Language.CultureCode == currentLang).Text);
                    if(!pref.ParameterObject.TranslationSuffix)
                    foreach(Models.Translation trans in pref.Suffix) AddTranslation(trans.Language.CultureCode, id, "SuffixText", trans.Text);
                }
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
                    string line;
                    if(vbase is Models.Module vmod)
                        line = $"#define COMOBJ_{HeaderNameEscape(vmod.Name)}_{HeaderNameEscape(com.Name)} ";
                    else
                        line = $"#define COMOBJ_{HeaderNameEscape(com.Name)} ";
                    line += $"\t{com.Number}\t//!< Number: {com.Number}, Text: {GetDefaultLanguage(com.Text)}, Function: {GetDefaultLanguage(com.FunctionText)}";
                    headers.AppendLine(line);
                }

                XElement xcom = new XElement(Get("ComObject"));
                string id = $"{appVersionMod}_O-";
                if(vbase is Models.Module) id += "2-";
                id += com.Id;
                xcom.SetAttributeValue("Id", id);
                xcom.SetAttributeValue("Name", com.Name);
                xcom.SetAttributeValue("Text", GetDefaultLanguage(com.Text));
                xcom.SetAttributeValue("Number", com.Number);
                xcom.SetAttributeValue("FunctionText", GetDefaultLanguage(com.FunctionText));
                
                if(!com.TranslationText)
                    foreach(Models.Translation trans in com.Text) AddTranslation(trans.Language.CultureCode, id, "Text", trans.Text);
                if(!com.TranslationFunctionText)
                    foreach(Models.Translation trans in com.FunctionText) AddTranslation(trans.Language.CultureCode, id, "FunctionText", trans.Text);
                
                if (com.ObjectSize > 7)
                    xcom.SetAttributeValue("ObjectSize", (com.ObjectSize / 8) + " Byte"+ ((com.ObjectSize > 15) ? "s":""));
                else
                    xcom.SetAttributeValue("ObjectSize", com.ObjectSize + " Bit");

                //TODO implement mayread >=20

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

        private void ExportComObjectRefs(IVersionBase vbase, AppVersion vers, XElement xparent)
        {
            Debug.WriteLine($"Exportiere ComObjectRefs: {vbase.ComObjectRefs.Count}x");
            if(vbase.ComObjectRefs.Count == 0) return;
            XElement xrefs = new XElement(Get("ComObjectRefs"));

            foreach (ComObjectRef cref in vbase.ComObjectRefs)
            {
                //Debug.WriteLine($"    - ComObjectRef {cref.UId} {cref.Name}");
                XElement xcref = new XElement(Get("ComObjectRef"));
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
                    xcref.SetAttributeValue("Text", GetDefaultLanguage(cref.Text));
                }
                if(cref.OverwriteFunctionText) {
                    if(!cref.TranslationFunctionText)
                        foreach(Models.Translation trans in cref.FunctionText) AddTranslation(trans.Language.CultureCode, id, "FunctionText", trans.Text);
                    xcref.SetAttributeValue("FunctionText", GetDefaultLanguage(cref.FunctionText));
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

                if(cref.OverwriteOS || (cref.OverwriteDpt && cref.ObjectSize != cref.ComObjectObject.ObjectSize))
                {
                    if (cref.ObjectSize > 7)
                        xcref.SetAttributeValue("ObjectSize", (cref.ObjectSize / 8) + " Byte" + ((cref.ObjectSize > 15) ? "s":""));
                    else
                        xcref.SetAttributeValue("ObjectSize", cref.ObjectSize + " Bit");
                }


                if(vers.IsComObjectRefAuto && cref.ComObjectObject.UseTextParameter)
                {
                    int nsVersion = int.Parse(currentNamespace.Substring(currentNamespace.LastIndexOf('/')+1));
                    xcref.SetAttributeValue("TextParameterRefId", appVersionMod + (cref.ComObjectObject.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{cref.ComObjectObject.ParameterRefObject.ParameterObject.Id}_R-{cref.ComObjectObject.ParameterRefObject.Id}");
                }
                if(!vers.IsComObjectRefAuto && cref.UseTextParameter)
                {
                    int nsVersion = int.Parse(currentNamespace.Substring(currentNamespace.LastIndexOf('/')+1));
                    xcref.SetAttributeValue("TextParameterRefId", appVersionMod + (cref.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{cref.ParameterRefObject.ParameterObject.Id}_R-{cref.ParameterRefObject.Id}");    
                }
                

                xrefs.Add(xcref);
            }

            xparent.Add(xrefs);
        }

        private void ParseParameter(Parameter para, XElement parent, IVersionBase ver, StringBuilder headers)
        {
            if((headers != null && para.SavePath != SavePaths.Nowhere) || (headers != null && para.IsInUnion && para.UnionObject != null && para.UnionObject.SavePath != SavePaths.Nowhere))
            {
                int offset = para.Offset;

                string lineStart;
                string lineComm = "";
                if(ver is Models.Module mod)
                {
                    lineStart = $"#define PARAM_{HeaderNameEscape(mod.Name)}_{HeaderNameEscape(para.Name)}";
                } else {
                    lineStart = $"#define PARAM_{HeaderNameEscape(para.Name)}";
                }
                
                string linePara = lineStart;
                if(para.IsInUnion && para.UnionObject != null)
                {
                    lineComm += $"// UnionOffset: {para.UnionObject.Offset}, ParaOffset: {para.Offset}";
                    linePara += $"\t\t0x{(para.UnionObject.Offset + para.Offset).ToString("X4")}";
                } else {
                    lineComm += $"// Offset: {para.Offset}";
                    linePara += $"\t\t0x{para.Offset.ToString("X4")}";
                }
                
                if (para.OffsetBit > 0) lineComm += ", BitOffset: " + para.OffsetBit;
                lineComm += $", Size: {para.ParameterTypeObject.SizeInBit} Bit";
                if (para.ParameterTypeObject.SizeInBit % 8 == 0) lineComm += " (" + (para.ParameterTypeObject.SizeInBit / 8) + " Byte)";
                lineComm += $", Text: {GetDefaultLanguage(para.Text)}";
                headers.AppendLine(lineComm);
                headers.AppendLine(linePara);

                if (para.OffsetBit > 0 || para.ParameterTypeObject.SizeInBit < 8)
                {
                    int mask = 0;
                    for(int i = 0; i < para.ParameterTypeObject.SizeInBit; i++)
                        mask += (int)Math.Pow(2, i);
                        
                    mask = mask << (8 - para.OffsetBit - (para.ParameterTypeObject.SizeInBit % 8));
                    headers.AppendLine($"{lineStart}_Mask\t0x{mask:X4}");

                    headers.AppendLine($"{lineStart}_Shift\t{8 - para.OffsetBit - (para.ParameterTypeObject.SizeInBit % 8)}");
                }
            }

            XElement xpara = new XElement(Get("Parameter"));
            string id = appVersionMod + (para.IsInUnion ? "_UP-" : "_P-") + para.Id;
            xpara.SetAttributeValue("Id", id);
            xpara.SetAttributeValue("Name", para.Name);
            xpara.SetAttributeValue("ParameterType", $"{appVersion}_PT-{GetEncoded(para.ParameterTypeObject.Name)}");

            if(!para.TranslationText)
                foreach(Models.Translation trans in para.Text) AddTranslation(trans.Language.CultureCode, id, "Text", trans.Text);

            if(!para.TranslationSuffix)
                foreach(Models.Translation trans in para.Suffix) AddTranslation(trans.Language.CultureCode, id, "SuffixText", trans.Text);

            if(!para.IsInUnion) {
                switch(para.SavePath) {
                    case SavePaths.Memory:
                    {
                        XElement xparamem = new XElement(Get("Memory"));
                        Memory mem = para.SaveObject as Memory;
                        if(mem == null) throw new Exception("Parameter soll in Memory gespeichert werden, aber der Typ von SaveObject ist kein Memory: " + para.SaveObject.GetType().ToString());
                        string memid = appVersion;
                        if (mem.Type == MemoryTypes.Absolute)
                            memid += $"_AS-{mem.Address:X4}";
                        else
                            memid += $"_RS-04-{mem.Offset:X5}";
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

                    case SavePaths.Property:
                    {
                        XElement xparamem = new XElement(Get("Property"));
                        Property prop = para.SaveObject as Property;
                        if(prop == null) throw new Exception("Parameter soll in Property gespeichert werden, aber der Typ von SaveObject ist kein Property: " + para.SaveObject.GetType().ToString());
                        
                        xparamem.SetAttributeValue("ObjectIndex", prop.ObjectIndex);
                        xparamem.SetAttributeValue("PropertyId", prop.PropertyId);
                        xparamem.SetAttributeValue("Offset", prop.Offset);
                        xparamem.SetAttributeValue("BitOffset", prop.OffsetBit);
                        break;
                    }
                }
            }
            else
            {
                xpara.SetAttributeValue("Offset", para.Offset);
                xpara.SetAttributeValue("BitOffset", para.OffsetBit);
                if (para.IsUnionDefault)
                    xpara.SetAttributeValue("DefaultUnionParameter", "true");
            }
            
            xpara.SetAttributeValue("Text", GetDefaultLanguage(para.Text));
            if (para.Access != ParamAccess.ReadWrite) xpara.SetAttributeValue("Access", para.Access);
            if (!string.IsNullOrWhiteSpace(GetDefaultLanguage(para.Suffix))) xpara.SetAttributeValue("SuffixText", GetDefaultLanguage(para.Suffix));
            
            if(para.ParameterTypeObject.Type == ParameterTypes.Picture)
                xpara.SetAttributeValue("Value", "");
            else
                xpara.SetAttributeValue("Value", para.Value);

            parent.Add(xpara);
        }


        #region Create Dyn Stuff

        private void HandleSubItems(IDynItems parent, XElement xparent, AppVersion ver = null)
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
                        HandleParam(dp, xparent, ver);
                        break;

                    case IDynChoose dch:
                        xitem = HandleChoose(dch, xparent);
                        break;

                    case IDynWhen dw:
                        xitem = HandleWhen(dw, xparent);
                        break;

                    case DynComObject dc:
                        HandleCom(dc, xparent);
                        break;

                    case DynSeparator ds:
                        HandleSep(ds, xparent);
                        break;

                    case DynModule dm:
                        HandleMod(dm, xparent, ver);
                        break;

                    case DynAssign da:
                        HandleAssign(da, xparent);
                        break;

                    case DynRepeat dr:
                        xitem = HandleRepeat(dr, xparent);
                        break;

                    case DynButton db:
                        HandleButton(db, xparent);
                        break;

                    default:
                        throw new Exception("Nicht behandeltes dynamisches Element: " + item.ToString());
                }

                if (item.Items != null && xitem != null)
                    HandleSubItems(item, xitem, ver);
            }
        }


        private XElement Handle(IDynItems ch, XElement parent)
        {
            XElement channel = new XElement(Get("ChannelIndependentBlock"));
            parent.Add(channel);

            if (ch is DynChannel dch)
            {
                channel.Name = Get("Channel");
                if (dch.UseTextParameter)
                    channel.SetAttributeValue("TextParameterRefId", appVersionMod + (dch.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{dch.ParameterRefObject.ParameterObject.Id}_R-{dch.ParameterRefObject.Id}");

                channel.SetAttributeValue("Text", GetDefaultLanguage(dch.Text));
                if (!dch.TranslationText)
                    foreach (Models.Translation trans in dch.Text) AddTranslation(trans.Language.CultureCode, $"{appVersionMod}_CH-{dch.Number}", "Text", trans.Text);
                
                channel.SetAttributeValue("Number", dch.Number);
                channel.SetAttributeValue("Id", $"{appVersionMod}_CH-{dch.Number}");
                channel.SetAttributeValue("Name", ch.Name);

                
                if(dch.UseIcon)
                {
                    channel.SetAttributeValue("Icon", dch.IconObject.Name);
                    if(!iconsApp.Contains(dch.IconObject))
                        iconsApp.Add(dch.IconObject);
                }

                if(dch.Access != ParamAccess.ReadWrite)
                    channel.SetAttributeValue("Access", dch.Access.ToString());
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

        private int moduleCounter = 1;
        private void HandleMod(DynModule mod, XElement parent, AppVersion ver)
        {
            XElement xmod = new XElement(Get("Module"));
            if(mod.Id == -1)
                mod.Id = moduleCounter++;
            xmod.SetAttributeValue("Id", $"{appVersionMod}_{(appVersionMod.Contains("_MD-") ? "SM":"MD")}-{mod.ModuleObject.Id}_M-{mod.Id}");
            xmod.SetAttributeValue("RefId", $"{appVersionMod}_MD-{mod.ModuleObject.Id}");

            int argCounter = 1;
            foreach(DynModuleArg arg in mod.Arguments)
            {
                XElement xarg = new XElement(Get(arg.Argument.Type.ToString() + "Arg"));
                xarg.SetAttributeValue("RefId", $"{appVersion}_MD-{mod.ModuleObject.Id}_A-{arg.Argument.Id}");

                //M-0002_A-20DE-22-4365-O000A_MD-3_M-18_A-3
                if(arg.Argument.Type == ArgumentTypes.Text)
                    xarg.SetAttributeValue("Id", $"{appVersion}_MD-{mod.ModuleObject.Id}_M-{mod.Id}_A-{argCounter}");

                if(arg.UseAllocator)
                {
                    xarg.SetAttributeValue("AllocatorRefId", $"{appVersion}_L-{arg.Allocator.Id}");
                } else {
                    xarg.SetAttributeValue("Value", arg.Value);
                }
                xmod.Add(xarg);
                argCounter++;
            }

            parent.Add(xmod);
        }

        private int separatorCounter = 1;

        private void HandleSep(DynSeparator sep, XElement parent)
        {
            XElement xsep = new XElement(Get("ParameterSeparator"));
            if(sep.Id == -1) {
                sep.Id = separatorCounter++;
            }
            xsep.SetAttributeValue("Id", $"{appVersionMod}_PS-{sep.Id}");
            xsep.SetAttributeValue("Text", GetDefaultLanguage(sep.Text));
            if(sep.Hint != SeparatorHint.None)
            {
                xsep.SetAttributeValue("UIHint", sep.Hint.ToString());
            }
            if(!string.IsNullOrEmpty(sep.Cell))
                xsep.SetAttributeValue("Cell", sep.Cell);
            
            if(sep.UseIcon)
            {
                xsep.SetAttributeValue("Icon", sep.IconObject.Name);
                if(!iconsApp.Contains(sep.IconObject))
                    iconsApp.Add(sep.IconObject);
            }

            if(sep.Access != ParamAccess.ReadWrite)
                xsep.SetAttributeValue("Access", sep.Access.ToString());

            parent.Add(xsep);

            if(!sep.TranslationText)
                foreach(Models.Translation trans in sep.Text) AddTranslation(trans.Language.CultureCode, $"{appVersionMod}_PS-{sep.Id}", "Text", trans.Text);
        }

        private XElement HandleChoose(IDynChoose cho, XElement parent)
        {
            XElement xcho = new XElement(Get("choose"));
            parent.Add(xcho);
            xcho.SetAttributeValue("ParamRefId", appVersionMod + (cho.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{cho.ParameterRefObject.ParameterObject.Id}_R-{cho.ParameterRefObject.Id}");
            return xcho;
        }

        private XElement HandleWhen(IDynWhen when, XElement parent)
        {
            XElement xwhen = new XElement(Get("when"));
            parent.Add(xwhen);

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

            bl.Id = pbCounter++;

            //Wenn Block InLine ist, kann kein ParamRef angegeben werden
            if(bl.IsInline)
            {
                block.SetAttributeValue("Id", $"{appVersionMod}_PB-{bl.Id}");
                block.SetAttributeValue("Inline", "true");
            } else {
                if(bl.UseParameterRef)
                {
                    block.SetAttributeValue("Id", $"{appVersionMod}_PB-{bl.ParameterRefObject.Id}");
                    block.SetAttributeValue("ParamRefId", appVersionMod + (bl.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{bl.ParameterRefObject.ParameterObject.Id}_R-{bl.ParameterRefObject.Id}");
                }
                else
                {
                    block.SetAttributeValue("Id", $"{appVersionMod}_PB-{bl.Id}");
                    string dText = GetDefaultLanguage(bl.Text);
                    //Wenn Block InLine ist, kann kein Text angegeben werden
                    if (!string.IsNullOrEmpty(dText))
                    {
                        block.SetAttributeValue("Text", dText);
                        if (!bl.TranslationText)
                            foreach (Models.Translation trans in bl.Text) AddTranslation(trans.Language.CultureCode, $"{appVersionMod}_PB-{bl.Id}", "Text", trans.Text);
                    }
                }
            }

            if(bl.Layout != BlockLayout.List)
            {
                block.SetAttributeValue("Layout", bl.Layout.ToString());

                if(bl.Rows.Count > 0)
                {
                    int rowCounter = 1;
                    XElement xrows = new XElement(Get("Rows"));
                    foreach(ParameterBlockRow row in bl.Rows)
                    {
                        XElement xrow = new XElement(Get("Row"));
                        xrow.SetAttributeValue("Id", $"{appVersionMod}_PB-{bl.Id}_R-{rowCounter++}");
                        xrow.SetAttributeValue("Name", row.Name);
                        xrows.Add(xrow);
                    }
                    block.Add(xrows);
                }

                if(bl.Columns.Count > 0)
                {
                    int colCounter = 1;
                    XElement xcols = new XElement(Get("Columns"));
                    foreach(ParameterBlockColumn col in bl.Columns)
                    {
                        XElement xcol = new XElement(Get("Column"));
                        xcol.SetAttributeValue("Id", $"{appVersionMod}_PB-{bl.Id}_C-{colCounter++}");
                        xcol.SetAttributeValue("Name", col.Name);
                        xcol.SetAttributeValue("Width", $"{col.Width}%");
                        xcols.Add(xcol);
                    }
                    block.Add(xcols);
                }
            }

            if(!string.IsNullOrEmpty(bl.Name))
                block.SetAttributeValue("Name", bl.Name);

            //Wenn Block InLine ist, kann kein TextParameter angegeben werden
            if (bl.UseTextParameter && !bl.IsInline)
                block.SetAttributeValue("TextParameterRefId", appVersionMod + (bl.TextRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{bl.TextRefObject.ParameterObject.Id}_R-{bl.TextRefObject.Id}");

            if(bl.ShowInComObjectTree)
                block.SetAttributeValue("ShowInComObjectTree", "true");

            if(bl.UseIcon)
            {
                block.SetAttributeValue("Icon", bl.IconObject.Name);
                if(!iconsApp.Contains(bl.IconObject))
                    iconsApp.Add(bl.IconObject);
            }

            if(bl.Access != ParamAccess.ReadWrite)
                block.SetAttributeValue("Access", bl.Access.ToString());

            return block;
        }

        private void HandleParam(DynParameter pa, XElement parent, AppVersion vbase)
        {
            XElement xpara = new XElement(Get("ParameterRefRef"));
            parent.Add(xpara);
            xpara.SetAttributeValue("RefId", appVersionMod + (pa.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{pa.ParameterRefObject.ParameterObject.Id}_R-{pa.ParameterRefObject.Id}");
            if(!string.IsNullOrEmpty(pa.Cell))
                xpara.SetAttributeValue("Cell", pa.Cell);

            if(vbase.IsHelpActive && pa.HasHelptext)
            {
                xpara.SetAttributeValue("HelpContext", pa.Helptext.Name);
            }

            if(pa.UseIcon)
            {
                xpara.SetAttributeValue("Icon", pa.IconObject.Name);
                if(!iconsApp.Contains(pa.IconObject))
                    iconsApp.Add(pa.IconObject);
            }
        }
        
        private XElement HandleAssign(DynAssign da, XElement parent)
        {
            XElement xcho = new XElement(Get("Assign"));
            parent.Add(xcho);
            xcho.SetAttributeValue("TargetParamRefRef", appVersionMod + (da.TargetObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{da.TargetObject.ParameterObject.Id}_R-{da.TargetObject.Id}");
            if(string.IsNullOrEmpty(da.Value))
                xcho.SetAttributeValue("SourceParamRefRef", appVersionMod + (da.SourceObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{da.SourceObject.ParameterObject.Id}_R-{da.SourceObject.Id}");
            else
                xcho.SetAttributeValue("Value", da.Value);
            return xcho;
        }

        int repCount = 1;
        private XElement HandleRepeat(DynRepeat dr, XElement parent)
        {
            XElement xcho = new XElement(Get("Repeat"));
            parent.Add(xcho);
            dr.Id = repCount++;
            xcho.SetAttributeValue("Id", $"{appVersionMod}_X-{dr.Id}");
            xcho.SetAttributeValue("Name", dr.Name);
            xcho.SetAttributeValue("Count", dr.Count);
            if(dr.UseParameterRef)
                xcho.SetAttributeValue("ParameterRefId", appVersionMod + (dr.ParameterRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{dr.ParameterRefObject.ParameterObject.Id}_R-{dr.ParameterRefObject.Id}");
            return xcho;
        }

        int btnCounter = 1;

        private void HandleButton(DynButton db, XElement parent)
        {
            XElement xbtn = new XElement(Get("Button"));
            string id = $"{appVersionMod}_B-{btnCounter++}";
            xbtn.SetAttributeValue("Id", id);
            xbtn.SetAttributeValue("Text", GetDefaultLanguage(db.Text));

            int ns = int.Parse(currentNamespace.Substring(currentNamespace.LastIndexOf('/') + 1));
            if(ns > 14)
                xbtn.SetAttributeValue("Name", db.Name);

            xbtn.SetAttributeValue("EventHandler", $"button{HeaderNameEscape(db.Name)}");

            if(!string.IsNullOrEmpty(db.Cell))
                xbtn.SetAttributeValue("Cell", db.Cell);
            if(!string.IsNullOrEmpty(db.EventHandlerParameters))
                xbtn.SetAttributeValue("EventHandlerParameters", db.EventHandlerParameters);
            if(!string.IsNullOrEmpty(db.Online))
                xbtn.SetAttributeValue("EventHandlerOnline", db.Online);

            if(db.UseIcon)
            {
                xbtn.SetAttributeValue("Icon", db.IconObject.Name);
                if(!iconsApp.Contains(db.IconObject))
                    iconsApp.Add(db.IconObject);
            }
            if (db.UseTextParameter)
                xbtn.SetAttributeValue("TextParameterRefId", appVersionMod + (db.TextRefObject.ParameterObject.IsInUnion ? "_UP-" : "_P-") + $"{db.TextRefObject.ParameterObject.Id}_R-{db.TextRefObject.Id}");


            parent.Add(xbtn);

            if(!db.TranslationText)
            {
                foreach(Translation trans in db.Text) AddTranslation(trans.Language.CultureCode, id, "Text", trans.Text);
            }

            string function = $"function button{HeaderNameEscape(db.Name)}(device, online, progress, context)";
            function += "\r\n{\r\n";
            function += db.Script;
            function += "\r\n}\r\n";
            buttonScripts.Add(function);
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

                    xitem.SetAttributeValue("Name", GetDefaultLanguage(item.Text));
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

                        foreach (AppVersionModel ver in app.Versions)
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
                            xitem.SetAttributeValue("Name", GetDefaultLanguage(dev.Text));
                            xitem.SetAttributeValue("Number", item.Number);
                            xitem.SetAttributeValue("VisibleDescription", GetDefaultLanguage(dev.Description));
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

        public async void SignOutput()
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

            if(!File.Exists(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "knx_master_" + nsVersion + ".xml")))
            {
                try{
                    System.Net.Http.HttpClient client = new System.Net.Http.HttpClient();
                    Stream down = await client.GetStreamAsync($"https://update.knx.org/data/XML/project-{nsVersion}/knx_master.xml");
                    Stream file = File.OpenWrite(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"knx_master_{nsVersion}.xml"));
                    await down.CopyToAsync(file);
                    file.Close();
                    file.Dispose();
                    down.Close();
                    down.Dispose();
                } catch (Exception ex){
                    System.Windows.MessageBox.Show(ex.Message, "Fehler beim herunterladen");
                    if(ex.InnerException != null)
                        System.Windows.MessageBox.Show(ex.InnerException.Message, "InnerException");
                    throw new Exception("knx_master.xml konnte nicht herunter geladen werden.", ex);
                }
            }

            File.Copy(System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", $"knx_master_{nsVersion}.xml"), GetRelPath("Temp", "knx_master.xml"));

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
            input = input.Replace(",", ".2C");
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
            input = input.Replace("°", ".C2.B0");
            return input;
        }

        public string GetDefaultLanguage(ObservableCollection<Translation> trans)
        {
            return trans.Single(e => e.Language.CultureCode == currentLang).Text;
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
            //this makes icons work...
            knx.SetAttributeValue("CreatedBy", "MT");
            knx.SetAttributeValue("ToolVersion", "5.7.617.38708");
            //knx.SetAttributeValue("CreatedBy", "Kaenx.Creator");
            //knx.SetAttributeValue("ToolVersion", Assembly.GetEntryAssembly().GetName().Version.ToString());
            doc = new XDocument(knx);
            doc.Root.Add(new XElement(Get("ManufacturerData"), xmanu));
            return xmanu;
        }
    }
}
