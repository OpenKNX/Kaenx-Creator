using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Xml;
using System.Xml.Linq;
using System.IO;
using System.Linq;

namespace Kaenx.Creator.Classes
{
    public class ImportHelper
    {
        private string _namespace;
        private ObservableCollection<Models.MaskVersion> _bcus;
        private ZipArchive Archive { get; set; }
        private Models.ModelGeneral _general;
        private string _path;
        private ObservableCollection<Models.DataPointType> DPTs;
        private int _uidCounter = 1;

        private Models.Application currentApp = null;
        private Models.AppVersion currentVers = null;

        private Dictionary<string, string> _langTexts = new Dictionary<string, string>() {
            {"cs-CZ", "Tschechisch"},
            {"da-DK", "Dänisch"},
            {"de-DE", "Deutsch"},
            {"el-GR", "Griechisch"},
            {"en-US", "Englisch"},
            {"es-ES", "Spanisch"},
            {"fi-FI", "Finnisch"},
            {"fr-FR", "Französisch"},
            {"hu-HU", "Ungarisch"},
            {"is-IS", "Isländisch"},
            {"it-IT", "Italienisch"},
            {"ja-JP", "Japanisch"},
            {"nb-NO", "Norwegisch"},
            {"nl-NL", "Niederländisch"},
            {"pl-PL", "Polnisch"},
            {"pt-PT", "Portugisisch"},
            {"ro-RO", "Rumänisch"},
            {"ru-RU", "Russisch"},
            {"sk-SK", "Slovakisch"},
            {"sl-SI", "Slovenisch"},
            {"sv-SE", "Schwedisch"},
            {"tr-TR", "Türkisch"},
            {"zh-CN", "Chinesisch"}
        };
        private Dictionary<string, Dictionary<string, Dictionary<string, string>>> _translations = new Dictionary<string, Dictionary<string, Dictionary<string, string>>>();
        private bool _defaultLangIsInTrans = false;

        public ImportHelper(string path, ObservableCollection<Models.MaskVersion> bcus) {
            _path = path;
            _bcus = bcus;
        }

        public void Start(Models.ModelGeneral general, ObservableCollection<Models.DataPointType> dpts)
        {
            _general = general;
            DPTs = dpts;
            Archive = ZipFile.OpenRead(_path);
            string manuHex = "";

            foreach (ZipArchiveEntry entryTemp in Archive.Entries)
            {
                if (entryTemp.FullName.Contains("M-"))
                {
                    manuHex = entryTemp.FullName.Substring(2, 4);
                    int manuId = int.Parse(manuHex, System.Globalization.NumberStyles.HexNumber);
                    if (_general.ManufacturerId == -1)
                    {
                        _general.ManufacturerId = manuId;
                    }
                    else if (_general.ManufacturerId != manuId)
                    {
                        if (System.Windows.MessageBox.Show("Hersteller der Produktdatenbank stimmt nicht mit dem Hersteller des Projekts über ein.\r\nSoll trotzdem importiert werden?", "Question", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.No)
                        {
                            Archive.Dispose();
                            return;
                        }
                        break;
                    }
                }
            }

            foreach (ZipArchiveEntry entryTemp in Archive.Entries)
            {
                if (entryTemp.FullName.Contains("_A-"))
                {
                    using (Stream entryStream = entryTemp.Open())
                    {
                        XElement xapp = XDocument.Load(entryStream).Root;
                        _namespace = xapp.Attribute("xmlns").Value;
                        xapp = xapp.Element(GetXName("ManufacturerData")).Element(GetXName("Manufacturer")).Element(GetXName("ApplicationPrograms")).Element(GetXName("ApplicationProgram"));
                        ImportApplication(xapp);
                    }
                }
            }

            ZipArchiveEntry entry = Archive.GetEntry($"M-{manuHex}/Hardware.xml");
            XElement xele = XDocument.Load(entry.Open()).Root;
            _namespace = xele.Attribute("xmlns").Value;
            xele = xele.Element(GetXName("ManufacturerData")).Element(GetXName("Manufacturer")).Element(GetXName("Hardware"));
            ImportHardware(xele);

            entry = Archive.GetEntry($"M-{manuHex}/Catalog.xml");
            xele = XDocument.Load(entry.Open()).Root;
            _namespace = xele.Attribute("xmlns").Value;
            xele = xele.Element(GetXName("ManufacturerData")).Element(GetXName("Manufacturer")).Element(GetXName("Catalog"));
            ImportCatalog(xele);
        }

        public void ImportApplication(XElement xapp)
        {

            #region "Create/Get Application and Version"
            currentApp = null;
            currentVers = null;
            int appNumber = int.Parse(xapp.Attribute("ApplicationNumber").Value);
            int versNumber = int.Parse(xapp.Attribute("ApplicationVersion").Value);

            foreach (Models.Application app in _general.Applications)
            {
                if (app.Number == appNumber)
                {
                    currentApp = app;
                    break;
                }
            }

            if (currentApp == null)
            {
                currentApp = new Models.Application()
                {
                    Number = appNumber,
                    Name = xapp.Attribute("Name").Value,
                    Mask = _bcus.Single(b => b.Id == xapp.Attribute("MaskVersion").Value)
                };
                _general.Applications.Add(currentApp);
            }

            foreach (Models.AppVersion vers in currentApp.Versions)
            {
                if (vers.Number == versNumber)
                {
                    currentVers = vers;
                    break;
                }
            }

            if (currentVers == null)
            {
                currentVers = new Models.AppVersion()
                {
                    Number = versNumber,
                    Name = "Imported",
                    IsParameterRefAuto = false,
                    IsComObjectRefAuto = false,
                    IsMemSizeAuto = false
                };
                currentApp.Versions.Add(currentVers);
            }
            string ns = xapp.Name.NamespaceName;
            ns = ns.Substring(ns.LastIndexOf('/') + 1);
            currentVers.NamespaceVersion = int.Parse(ns);
            currentVers.DefaultLanguage = xapp.Attribute("DefaultLanguage").Value;
            currentVers.Languages.Add(new Language(_langTexts[currentVers.DefaultLanguage], currentVers.DefaultLanguage));
#endregion
            ImportLanguages(xapp.Parent.Parent.Element(GetXName("Languages")));
            currentVers.Text = GetTranslation(xapp.Attribute("Id").Value, "Name", xapp);
            XElement xstatic = xapp.Element(GetXName("Static"));
            ImportSegments(xstatic.Element(GetXName("Code")));
            ImportParameterTypes(xstatic.Element(GetXName("ParameterTypes")));
            ImportParameter(xstatic.Element(GetXName("Parameters")), currentVers);
            ImportParameterRefs(xstatic.Element(GetXName("ParameterRefs")));
            ImportComObjects(xstatic.Element(GetXName("ComObjectTable")));
            ImportComObjectRefs(xstatic.Element(GetXName("ComObjectRefs")));
            ImportModules(xapp.Element(GetXName("ModuleDefs")));
            ImportDynamic(xapp.Element(GetXName("Dynamic")));
        }

        public void ImportLanguages(XElement xlangs) {
            foreach(XElement xlang in xlangs.Elements()) {
                string cultureCode = xlang.Attribute("Identifier").Value;
                if(!currentVers.Languages.Any(l => l.CultureCode == cultureCode))
                    currentVers.Languages.Add(new Language(_langTexts[cultureCode], cultureCode));

                foreach(XElement xtele in xlang.Descendants(GetXName("TranslationElement"))) {
                    foreach(XElement xattr in xtele.Elements()) {
                        AddTranslation(xtele.Attribute("RefId").Value,
                            xattr.Attribute("AttributeName").Value,
                            cultureCode,
                            xattr.Attribute("Text").Value);
                    }
                }
            }

            _defaultLangIsInTrans = xlangs.Elements().Any(x => x.Attribute("Identifier").Value == currentVers.DefaultLanguage);
        }

        public void AddTranslation(string id, string attr, string lang, string value) {
            if(!_translations.ContainsKey(id)) _translations.Add(id, new Dictionary<string, Dictionary<string, string>>());
            if(!_translations[id].ContainsKey(attr)) _translations[id].Add(attr, new Dictionary<string, string>());
            if(!_translations[id][attr].ContainsKey(lang)) _translations[id][attr].Add(lang, value);
        }

        public ObservableCollection<Translation> GetTranslation(string id, string attr, XElement xele) {
            ObservableCollection<Translation> translations = new ObservableCollection<Translation>();

            if(id == "M-0083_A-0024-15-6E79_O-42_R-12008") {

            }

            if(_translations.ContainsKey(id) && _translations[id].ContainsKey(attr)) {
                foreach(KeyValuePair<string, string> trans in _translations[id][attr]) {
                    translations.Add(new Translation(new Language(_langTexts[trans.Key], trans.Key), trans.Value));
                }
            }
            if(xele.Attribute(attr) != null && !translations.Any(t => t.Language.CultureCode == currentVers.DefaultLanguage)) {
                translations.Add(new Translation(new Language(_langTexts[currentVers.DefaultLanguage], currentVers.DefaultLanguage), xele.Attribute(attr).Value));
            }

            foreach(Language lang in currentVers.Languages) {
                if(!translations.Any(t => t.Language.CultureCode == lang.CultureCode)) {
                    if(lang.CultureCode == currentVers.DefaultLanguage)
                        translations.Add(new Translation(lang, xele.Attribute(attr)?.Value ?? ""));
                    else
                        translations.Add(new Translation(lang, ""));
                }
            }


            return translations;
        }

        public void ImportSegments(XElement xcodes) {
            _uidCounter = 1;
            foreach (XElement xcode in xcodes.Elements())
            {
                if (xcode.Name.LocalName == "AbsoluteSegment")
                {
                    currentVers.Memories.Add(new Models.Memory()
                    {
                        UId = _uidCounter++,
                        Address = int.Parse(xcode.Attribute("Address").Value),
                        Size = int.Parse(xcode.Attribute("Size").Value),
                        Name = GetLastSplit(xcode.Attribute("Id").Value) + " " + (xcode.Attribute("Name")?.Value ?? "Unnamed"),
                        Type = MemoryTypes.Absolute,
                        IsAutoSize = false,
                        IsAutoPara = false
                    });
                }
                else if (xcode.Name.LocalName == "RelativeSegment")
                {
                    currentVers.Memories.Add(new Models.Memory()
                    {
                        UId = _uidCounter++,
                        Size = int.Parse(xcode.Attribute("Size").Value),
                        Offset = int.Parse(xcode.Attribute("Offset")?.Value ?? "0"),
                        Name = GetLastSplit(xcode.Attribute("Id").Value) + (xcode.Attribute("Name")?.Value ?? ""),
                        Type = MemoryTypes.Relative,
                        IsAutoSize = false,
                        IsAutoPara = false
                    });
                }
                else
                {
                    throw new Exception("Masks Memory Type is not supported! " + currentApp.Mask.Memory);
                }
            }
        }

        public void ImportParameterTypes(XElement xparatypes)
        {
            _uidCounter = 1;

            foreach (XElement xparatype in xparatypes.Elements())
            {
                Models.ParameterType ptype = new Models.ParameterType()
                {
                    Name = xparatype.Attribute("Name").Value,
                    IsSizeAuto = false,
                    UId = _uidCounter++
                };

                XElement xsub = xparatype.Elements().ElementAt(0);
                switch (xsub.Name.LocalName)
                {
                    case "TypeNone":
                        ptype.Type = ParameterTypes.None;
                        break;

                    case "TypeNumber":
                        ptype.Type = xsub.Attribute("Type").Value switch
                        {
                            "unsignedInt" => ParameterTypes.NumberUInt,
                            "signedInt" => ParameterTypes.NumberInt,
                            "float9" => ParameterTypes.Float9,
                            _ => throw new Exception("Unbekannter TypeNumber Type: " + xsub.Attribute("Type").Value)
                        };
                        ptype.SizeInBit = int.Parse(xsub.Attribute("SizeInBit").Value);
                        ptype.Min = int.Parse(xsub.Attribute("minInclusive").Value);
                        ptype.Max = int.Parse(xsub.Attribute("maxInclusive").Value);
                        break;

                    case "TypeRestriction":
                        ptype.Type = ParameterTypes.Enum;
                        ptype.SizeInBit = int.Parse(xsub.Attribute("SizeInBit").Value);
                        foreach (XElement xenum in xsub.Elements())
                        {
                            ptype.Enums.Add(new Models.ParameterTypeEnum()
                            {
                                Name = xenum.Attribute("Text").Value,
                                Text = GetTranslation(xenum.Attribute("Id").Value, "Text", xenum),
                                Value = int.Parse(xenum.Attribute("Value").Value)
                            });
                            if(ptype.Enums.Any(e => e.Text.Any(l => !string.IsNullOrEmpty(l.Text))))
                                ptype.TranslateEnums = true;
                        }
                        break;

                    case "TypeText":
                        ptype.Type = ParameterTypes.Text;
                        ptype.SizeInBit = int.Parse(xsub.Attribute("SizeInBit").Value);
                        break;

                    case "TypeIPAddress":
                        ptype.Type = ParameterTypes.IpAddress;
                        //TODO read if ipv4 or ipv6
                        break;

                    default:
                        throw new Exception("Unbekannter ParameterType: " + xsub.Name.LocalName);
                }

                currentVers.ParameterTypes.Add(ptype);
            }
        }

        public void ImportParameter(XElement xparas, IVersionBase vbase) {
            if(xparas == null) return;
            _uidCounter = 1;
            
            foreach(XElement xpara in xparas.Elements(GetXName("Parameter"))) {
                ParseParameter(xpara, vbase);
            }

            int unionCounter = 1;
            foreach (XElement xunion in xparas.Elements(GetXName("Union")))
            {
                Union union = new Union()
                {
                    Name = $"Union {unionCounter}",
                    UId = unionCounter++,
                    SizeInBit = int.Parse(xunion.Attribute("SizeInBit").Value)
                };
                XElement xmem = xunion.Elements().ElementAt(0);

                switch (xmem.Name.LocalName)
                {
                    case "Memory":
                        union.SavePath = ParamSave.Memory;
                        string memName = GetLastSplit(xmem.Attribute("CodeSegment").Value);
                        union.MemoryObject = currentVers.Memories.Single(m => m.Name.StartsWith(memName));
                        union.Offset = int.Parse(xmem.Attribute("Offset").Value);
                        union.OffsetBit = int.Parse(xmem.Attribute("BitOffset").Value);
                        break;
                }
                vbase.Unions.Add(union);




                foreach(XElement xpara in xunion.Elements(GetXName("Parameter"))) {
                    ParseParameter(xpara, vbase, union, xmem);
                }
            }
            currentVers.IsUnionActive = unionCounter > 1;
        }

        public void ParseParameter(XElement xpara, IVersionBase vbase, Union union = null, XElement xmemory = null) {
            Models.Parameter para = new Models.Parameter() {
                Name = xpara.Attribute("Name").Value,
                Value = xpara.Attribute("Value").Value,
                IsOffsetAuto = false,
                Suffix = xpara.Attribute("SuffixText")?.Value ?? "",
                UId = _uidCounter++,
                IsInUnion = (xmemory != null),
                UnionObject = union,
                IsUnionDefault = xpara.Attribute("DefaultUnionParameter")?.Value == "true"
            };
            string id = GetLastSplit(xpara.Attribute("Id").Value, 2);
            if (id.StartsWith("-"))
                id = id.Substring(1);
            para.Id = int.Parse(id);

            para.Text = GetTranslation(xpara.Attribute("Id").Value, "Text", xpara);

            para.Access = (xpara.Attribute("Access")?.Value) switch {
                "None" => ParamAccess.None,
                "Read" => ParamAccess.Read,
                "ReadWrite" => ParamAccess.ReadWrite,
                null => ParamAccess.Default,
                _ => throw new Exception("Unbekannter AccesType für Parameter: " + xpara.Attribute("Access").Value)
            };

            string typeName = Unescape(GetLastSplit(xpara.Attribute("ParameterType").Value, 3));
            para.ParameterTypeObject = currentVers.ParameterTypes.Single(t => t.Name == typeName);

            if (xmemory != null || xpara.Elements().Count() > 0)
            {
                XElement xmem = xmemory ?? xpara.Elements().ElementAt(0);
                if (xmem.Name.LocalName == "Memory")
                {
                    para.SavePath = ParamSave.Memory;
                    string memName = GetLastSplit(xmem.Attribute("CodeSegment").Value);
                    para.MemoryObject = currentVers.Memories.Single(m => m.Name.StartsWith(memName));
                    if (para.IsInUnion)
                    {
                        para.Offset = int.Parse(xpara.Attribute("Offset").Value);
                        para.OffsetBit = int.Parse(xpara.Attribute("BitOffset").Value);
                    }
                    else
                    {
                        para.Offset = int.Parse(xmem.Attribute("Offset").Value);
                        para.OffsetBit = int.Parse(xmem.Attribute("BitOffset").Value);
                    }
                }
                else
                {
                    throw new Exception("Unbekannter MemoryTyp für Parameter: " + xmem.Name.LocalName);
                }
            }

            vbase.Parameters.Add(para);
        }

        public void ImportParameterRefs(XElement xrefs) {
            if(xrefs == null) return;
            _uidCounter = 1;

            foreach (XElement xref in xrefs.Elements())
            {
                //TODO also import DisplayOrder and Tag
                Models.ParameterRef pref = new Models.ParameterRef();

                pref.UId = _uidCounter++;
                pref.Id = int.Parse(GetLastSplit(xref.Attribute("Id").Value, 2));

                pref.OverwriteValue = xref.Attribute("Value") != null;
                pref.Value = xref.Attribute("Value")?.Value ?? "";
                pref.OverwriteAccess = xref.Attribute("Access") != null;
                pref.Access = xref.Attribute("Access")?.Value switch
                {
                    "None" => ParamAccess.None,
                    "Read" => ParamAccess.Read,
                    "ReadWrite" => ParamAccess.ReadWrite,
                    null => ParamAccess.Default,
                    _ => throw new Exception("Unbekannter Access Typ für ParameterRef: " + xref.Attribute("Access")?.Value)
                };
                string id = GetLastSplit(xref.Attribute("RefId").Value, 2);
                if (id.StartsWith("-"))
                    id = id.Substring(1);
                int paraId = int.Parse(id);
                pref.ParameterObject = currentVers.Parameters.Single(p => p.Id == paraId);
                pref.Name = pref.Id + " " + pref.ParameterObject.Name;

                currentVers.ParameterRefs.Add(pref);
            }
        }

        public void ImportComObjects(XElement xcoms)
        {
            _uidCounter = 1;

            foreach (XElement xcom in xcoms.Elements())
            {
                Models.ComObject com = new Models.ComObject()
                {
                    Name = xcom.Attribute("Name")?.Value ?? "",
                    Number = int.Parse(xcom.Attribute("Number").Value),
                    Id = int.Parse(GetLastSplit(xcom.Attribute("Id").Value, 2)),
                    UId = _uidCounter++
                };

                com.Text = GetTranslation(xcom.Attribute("Id").Value, "Text", xcom);
                com.FunctionText = GetTranslation(xcom.Attribute("Id").Value, "FunctionText", xcom);
                com.Description = GetTranslation(xcom.Attribute("Id").Value, "VisibleDescription", xcom);

                com.FlagRead = ParseFlagType(xcom.Attribute("ReadFlag")?.Value);
                com.FlagWrite = ParseFlagType(xcom.Attribute("WriteFlag")?.Value);
                com.FlagComm = ParseFlagType(xcom.Attribute("CommunicationFlag")?.Value);
                com.FlagTrans = ParseFlagType(xcom.Attribute("TransmitFlag")?.Value);
                com.FlagUpdate = ParseFlagType(xcom.Attribute("UpdateFlag")?.Value);
                com.FlagOnInit = ParseFlagType(xcom.Attribute("ReadOnInitFlag")?.Value);

                string type = xcom.Attribute("DatapointType")?.Value;

                if (type != null)
                {
                    com.HasDpt = true;
                    if (type.StartsWith("DPST-"))
                    {
                        string[] xtype = type.Split("-");
                        com.HasDpts = true;
                        com.Type = DPTs.Single(d => d.Number == xtype[1]);
                        com.SubType = com.Type.SubTypes.Single(s => s.Number == xtype[2]);
                    }
                    else if (type.StartsWith("DPT-"))
                    {
                        string[] xtype = type.Split("-");
                        com.HasDpts = false;
                        com.Type = DPTs.Single(d => d.Number == xtype[1]);
                    }
                    else
                    {
                        throw new Exception("Unbekanntes DPT Format für KO: " + type);
                    }
                }
                else
                {

                }

                currentVers.ComObjects.Add(com);
            }
        }

        public void ImportComObjectRefs(XElement xrefs) {
            if(xrefs == null) return;
            _uidCounter = 1;

            foreach (XElement xref in xrefs.Elements())
            {
                //TODO also import DisplayOrder and Tag
                Models.ComObjectRef cref = new Models.ComObjectRef();

                cref.UId = _uidCounter++;
                cref.Id = int.Parse(GetLastSplit(xref.Attribute("Id").Value, 2));
              
                //cref.OverwriteText = xref.Attribute("Text") != null;
                cref.OverwriteFunctionText = xref.Attribute("FunctionText") != null;
                cref.OverwriteDescription = xref.Attribute("VisibleDescription") != null;

                cref.Text = GetTranslation(xref.Attribute("Id").Value, "Text", xref);
                cref.FunctionText = GetTranslation(xref.Attribute("Id").Value, "FunctionText", xref);
                cref.Description = GetTranslation(xref.Attribute("Id").Value, "VisibleDescription", xref);

                cref.OverwriteText = cref.Text.Any(t => !string.IsNullOrEmpty(t.Text));

                string id = GetLastSplit(xref.Attribute("RefId").Value, 2);
                if (id.StartsWith("-"))
                    id = id.Substring(1);
                int comId = int.Parse(id);
                cref.ComObjectObject = currentVers.ComObjects.Single(c => c.Id == comId);
                cref.Name = cref.Id + " " + cref.ComObjectObject.Name;


                cref.FlagRead = ParseFlagType(xref.Attribute("ReadFlag")?.Value);
                cref.OverwriteFR = cref.FlagRead == FlagType.Undefined;
                if (cref.FlagRead == FlagType.Undefined) cref.FlagRead = FlagType.Disabled;
                cref.FlagWrite = ParseFlagType(xref.Attribute("WriteFlag")?.Value);
                cref.OverwriteFW = cref.FlagWrite == FlagType.Undefined;
                if (cref.FlagWrite == FlagType.Undefined) cref.FlagWrite = FlagType.Disabled;
                cref.FlagComm = ParseFlagType(xref.Attribute("CommunicationFlag")?.Value);
                cref.OverwriteFC = cref.FlagComm == FlagType.Undefined;
                if (cref.FlagComm == FlagType.Undefined) cref.FlagComm = FlagType.Disabled;
                cref.FlagTrans = ParseFlagType(xref.Attribute("TransmitFlag")?.Value);
                cref.OverwriteFT = cref.FlagTrans == FlagType.Undefined;
                if (cref.FlagTrans == FlagType.Undefined) cref.FlagTrans = FlagType.Disabled;
                cref.FlagUpdate = ParseFlagType(xref.Attribute("UpdateFlag")?.Value);
                cref.OverwriteFU = cref.FlagUpdate == FlagType.Undefined;
                if (cref.FlagUpdate == FlagType.Undefined) cref.FlagUpdate = FlagType.Disabled;
                cref.FlagOnInit = ParseFlagType(xref.Attribute("ReadOnInitFlag")?.Value);
                cref.OverwriteFOI = cref.FlagOnInit == FlagType.Undefined;
                if (cref.FlagOnInit == FlagType.Undefined) cref.FlagOnInit = FlagType.Disabled;

                if (xref.Attribute("DatapointType") != null)
                {
                    string dptstr = xref.Attribute("DatapointType").Value;

                    if (string.IsNullOrEmpty(dptstr))
                    {
                        cref.OverwriteDpt = true;
                        cref.OverwriteDpst = false;
                        cref.Type = DPTs[0];
                    }
                    else
                    {
                        string[] dpts = dptstr.Split(' ');
                        dpts = dpts[0].Split('-');
                        if (dpts[0] == "DPT")
                        {
                            cref.OverwriteDpt = true;
                            cref.OverwriteDpst = false;
                            cref.Type = DPTs.Single(d => d.Number == dpts[1]);
                        }
                        else
                        {
                            cref.OverwriteDpt = true;
                            cref.OverwriteDpst = true;
                            cref.Type = DPTs.Single(d => d.Number == dpts[1]);
                            cref.SubType = cref.Type.SubTypes.Single(s => s.Number == dpts[2]);
                        }
                    }

                }

                currentVers.ComObjectRefs.Add(cref);
            }
        }

        public void ImportModules(XElement xmods) {
            if(xmods == null) return;
            _uidCounter = 1;
            currentVers.IsModulesActive = true;

            foreach(XElement xmod in xmods.Elements()) {
                //TODO also import DisplayOrder and Tag
                Models.Module mod = new Models.Module() {
                    Name = xmod.Attribute("Name")?.Value ?? "Unbenannt",
                    UId = _uidCounter++,
                    Id = int.Parse(GetLastSplit(xmod.Attribute("Id").Value, 3))
                };

                XElement xstatic = xmod.Element(GetXName("Static"));
                ImportParameter(xstatic.Element(GetXName("Parameters")), mod);

                currentVers.Modules.Add(mod);
            }
        }

        public void ImportHardware(XElement xhards) {
            foreach(XElement xhard in xhards.Elements()) {
                Models.Hardware hardware;

                string snumb = xhard.Attribute("SerialNumber").Value;
                int vers = int.Parse(xhard.Attribute("VersionNumber").Value);

                if (_general.Hardware.Any(h => h.SerialNumber == snumb && h.Version == vers))
                {
                    hardware = _general.Hardware.Single(h => h.SerialNumber == snumb && h.Version == vers);
                }
                else
                {
                    hardware = new Hardware()
                    {
                        SerialNumber = snumb,
                        Version = vers,
                        Name = xhard.Attribute("Name").Value
                    };
                    hardware.HasApplicationProgram = xhard.Attribute("HasApplicationProgram")?.Value == "true";
                    hardware.HasIndividualAddress = xhard.Attribute("HasIndividualAddress")?.Value == "true";
                    hardware.BusCurrent = (int)StringToFloat(xhard.Attribute("BusCurrent")?.Value, 10);
                    _general.Hardware.Add(hardware);
                }

                foreach (XElement xapp in xhard.Descendants(GetXName("ApplicationProgramRef")))
                {
                    string[] appId = xapp.Attribute("RefId").Value.Split('-');
                    int number = int.Parse(appId[2], System.Globalization.NumberStyles.HexNumber);
                    int version = int.Parse(appId[3], System.Globalization.NumberStyles.HexNumber);

                    hardware.Apps.Add(_general.Applications.Single(a => a.Number == number));
                }

                foreach (XElement xprod in xhard.Descendants(GetXName("Product")))
                {
                    Models.Device device;
                    string ordernumb = xprod.Attribute("OrderNumber").Value;

                    if (_general.Devices.Any(d => d.OrderNumber == ordernumb))
                    {
                        device = _general.Devices.Single(d => d.OrderNumber == ordernumb);
                    }
                    else
                    {
                        device = new Models.Device()
                        {
                            OrderNumber = ordernumb,
                            Text = xprod.Attribute("Text").Value,
                            IsRailMounted = xprod.Attribute("IsRailMounted")?.Value == "true",
                            Description = xprod.Attribute("VisibleDescription")?.Value ?? ""
                        };
                        device.Name = device.Text;
                        hardware.Devices.Add(device);
                    }
                }
            }
        }

        public void ImportCatalog(XElement xcat)
        {
            foreach (XElement xitem in xcat.Elements())
            {
                ParseCatalogItem(xitem, _general.Catalog[0]);
            }
        }

        private void ParseCatalogItem(XElement xitem, CatalogItem parent)
        {
            CatalogItem item = new CatalogItem()
            {
                Parent = parent,
                Name = xitem.Attribute("Name").Value,
                Number = xitem.Attribute("Number").Value
            };


            switch (xitem.Name.LocalName)
            {
                case "CatalogSection":
                    item.IsSection = true;
                    //TODO import visibledescription
                    foreach (XElement xele in xitem.Elements())
                        ParseCatalogItem(xele, item);
                    break;

                case "CatalogItem":
                    item.IsSection = false;
                    item.VisibleDescription = xitem.Attribute("VisibleDescription")?.Value ?? "";
                    string[] hard2ref = xitem.Attribute("Hardware2ProgramRefId").Value.Split('-');
                    string serialNr = hard2ref[2];
                    item.Hardware = _general.Hardware.Single(h => h.SerialNumber == serialNr);
                    break;
            }

            parent.Items.Add(item);
        }

        private void ImportDynamic(XElement xdyn)
        {
            DynamicMain main = new DynamicMain();
            //TODO import dynamics

            ParseDynamic(main, xdyn);

            currentVers.Dynamics.Add(main);
        }

        private void ParseDynamic(IDynItems parent, XElement xeles)
        {
            foreach (XElement xele in xeles.Elements())
            {
                int paraId = 0;

                switch (xele.Name.LocalName)
                {
                    case "Channel":
                        DynChannel dc = new DynChannel()
                        {
                            Name = xele.Attribute("Name")?.Value ?? "",
                            Text = GetTranslation(xele.Attribute("Id")?.Value ?? "", "Text", xele),
                            Number = xele.Attribute("Number")?.Value ?? ""
                        };
                        if (xele.Attribute("ParamRefId") != null)
                        {
                            dc.UseTextParameter = true;
                            paraId = int.Parse(GetLastSplit(xele.Attribute("ParamRefId").Value, 2));
                            dc.ParameterRefObject = currentVers.ParameterRefs.Single(p => p.Id == paraId);
                        }
                        parent.Items.Add(dc);
                        ParseDynamic(dc, xele);
                        break;

                    case "IndependentChannel":
                        break;

                    case "ParameterBlock":
                        DynParaBlock dpb = new DynParaBlock() {
                            Name = xele.Attribute("Name")?.Value ?? ""
                        };
                        dpb.Text = GetTranslation(xele.Attribute("Id")?.Value ?? "", "Text", xele);
                        if(xele.Attribute("ParamRefId") != null) {
                            dpb.UseTextParameter = true;
                            paraId = int.Parse(GetLastSplit(xele.Attribute("ParamRefId").Value, 2));
                            dpb.ParameterRefObject = currentVers.ParameterRefs.Single(p => p.Id == paraId);
                        } else {
                            dpb.Id = int.Parse(GetLastSplit(xele.Attribute("Id").Value, 3));
                        }
                        parent.Items.Add(dpb);
                        ParseDynamic(dpb, xele);
                        break;

                    case "choose":
                        DynChoose dch = new DynChoose();
                        paraId = int.Parse(GetLastSplit(xele.Attribute("ParamRefId").Value, 2));
                        dch.ParameterRefObject = currentVers.ParameterRefs.Single(p => p.Id == paraId);
                        parent.Items.Add(dch);
                        ParseDynamic(dch, xele);
                        break;

                    case "when":
                        DynWhen dw = new DynWhen()
                        {
                            Condition = xele.Attribute("test")?.Value ?? "",
                            IsDefault = xele.Attribute("default")?.Value == "true"
                        };
                        parent.Items.Add(dw);
                        ParseDynamic(dw, xele);
                        break;

                    case "ParameterRefRef":
                        DynParameter dp = new DynParameter();
                        paraId = int.Parse(GetLastSplit(xele.Attribute("RefId").Value, 2));
                        dp.ParameterRefObject = currentVers.ParameterRefs.Single(p => p.Id == paraId);
                        parent.Items.Add(dp);
                        break;

                    case "ParameterSeparator":
                        DynSeparator ds = new DynSeparator();
                        paraId = int.Parse(GetLastSplit(xele.Attribute("Id").Value, 3));
                        ds.Id = paraId;
                        ds.Text = GetTranslation(xele.Attribute("Id")?.Value ?? "", "Text", xele);
                        parent.Items.Add(ds);
                        break;

                    case "ComObjectRefRef":
                        DynComObject dco = new DynComObject();
                        paraId = int.Parse(GetLastSplit(xele.Attribute("RefId").Value, 2));
                        dco.ComObjectRefObject = currentVers.ComObjectRefs.Single(p => p.Id == paraId);
                        parent.Items.Add(dco);
                        break;

                    case "Module":
                        //TODO import modules
                        break;

                    default:
                        throw new Exception("Unbekanntes Element in Dynamic: " + xele.Name.LocalName);
                }
            }
        }


        public FlagType ParseFlagType(string type)
        {
            return type switch
            {
                "Enabled" => FlagType.Enabled,
                "Disabled" => FlagType.Disabled,
                null => FlagType.Undefined, //TODO check
                _ => throw new Exception("Unbekannter FlagTyp: " + type)
            };
        }

        public static float StringToFloat(string input, float def = 0)
        {
            if (input == null) return def;

            if (input.ToLower().Contains("e+"))
            {
                float numb = float.Parse(input.Substring(0, 5).Replace('.', ','));
                int expo = int.Parse(input.Substring(input.IndexOf('+') + 1));
                if (expo == 0)
                    return int.Parse(numb.ToString());
                float res = numb * (10 * expo);
                return res;
            }

            try
            {
                return float.Parse(input);
            }
            catch
            {
                return def;
            }
        }

        public string Unescape(string input)
        {
            input = input.Replace(".25", "%");
            input = input.Replace(".20", " ");
            input = input.Replace(".21", "!");
            input = input.Replace(".22", "\"");
            input = input.Replace(".23", "#");
            input = input.Replace(".24", "$");
            input = input.Replace(".26", "&");
            input = input.Replace(".28", "(");
            input = input.Replace(".29", ")");
            input = input.Replace(".2B", "+");
            input = input.Replace(".2D", "-");
            input = input.Replace(".2F", "/");
            input = input.Replace(".3A", ":");
            input = input.Replace(".3B", ";");
            input = input.Replace(".3C", "<");
            input = input.Replace(".3D", "=");
            input = input.Replace(".3E", ">");
            input = input.Replace(".3F", "?");
            input = input.Replace(".40", "@");
            input = input.Replace(".5B", "[");
            input = input.Replace(".5C", "%\\");
            input = input.Replace(".5D", "]");
            input = input.Replace(".5C", "^");
            input = input.Replace(".5F", "_");
            input = input.Replace(".7B", "{");
            input = input.Replace(".7C", "|");
            input = input.Replace(".7D", "}");

            input = input.Replace(".2E", ".");
            return input;
        }

        public string GetLastSplit(string input, int offset = 0)
        {
            return input.Substring(input.LastIndexOf('_') + 1 + offset);
        }

        public XName GetXName(string name)
        {
            return XName.Get(name, _namespace);
        }
    }
}