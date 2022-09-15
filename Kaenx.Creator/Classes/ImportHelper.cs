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
        private ObservableCollection<MaskVersion> _bcus;
        private ZipArchive Archive { get; set; }
        private ModelGeneral _general;
        private string _path;
        private ObservableCollection<DataPointType> DPTs;
        private int _uidCounter = 1;

        private Application currentApp = null;
        private AppVersion currentVers = null;

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

        public ImportHelper(string path, ObservableCollection<MaskVersion> bcus) {
            _path = path;
            _bcus = bcus;
        }

        public void StartXml(ModelGeneral general, ObservableCollection<DataPointType>dpts)
        {
            _general = general;
            DPTs = dpts;
            XDocument xdoc = XDocument.Load(_path);
            _namespace = xdoc.Root.Attribute("xmlns").Value;
            XElement xmanu = xdoc.Root.Element(Get("ManufacturerData")).Element(Get("Manufacturer"));
            
            string manuHex = xmanu.Attribute("RefId").Value.Substring(2);
            int manuId = int.Parse(manuHex, System.Globalization.NumberStyles.HexNumber);
            if (_general.ManufacturerId != manuId)
            {
                var res = System.Windows.MessageBox.Show($"Hersteller der Produktdatenbank stimmt nicht mit dem Hersteller des Projekts über ein.\r\nWollen Sie ihre HerstellerId von {_general.ManufacturerId:X4} auf {manuId:X4} ändern?", "Question", System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Warning);
                switch(res) {
                    case System.Windows.MessageBoxResult.Yes:
                        _general.ManufacturerId = manuId;;
                        break;
                    case System.Windows.MessageBoxResult.Cancel:
                        return;
                }
            }

            System.Diagnostics.Debug.WriteLine("XML unterstützt keine Baggages");


            XElement xtemp = xmanu.Element(Get("ApplicationPrograms")).Element(Get("ApplicationProgram"));
            ImportApplication(xtemp);

            ImportLanguages(xmanu.Element(Get("Languages")), _general.Languages);
            xtemp = xmanu.Element(Get("Hardware"));
            ImportHardware(xtemp);

            xtemp = xmanu.Element(Get("Catalog"));
            ImportCatalog(xtemp);

        }

        public void StartZip(ModelGeneral general, ObservableCollection<DataPointType> dpts)
        {
            _general = general;
            DPTs = dpts;
            string manuHex = "";
            Archive = ZipFile.OpenRead(_path);
            foreach (ZipArchiveEntry entryTemp in Archive.Entries)
            {
                if (entryTemp.FullName.Contains("M-"))
                {
                    manuHex = entryTemp.FullName.Substring(2, 4);
                    int manuId = int.Parse(manuHex, System.Globalization.NumberStyles.HexNumber);
                    if (_general.ManufacturerId != manuId)
                    {
                        var res = System.Windows.MessageBox.Show($"Hersteller der Produktdatenbank stimmt nicht mit dem Hersteller des Projekts über ein.\r\nWollen Sie ihre HerstellerId von {_general.ManufacturerId:X4} auf {manuId:X4} ändern?", "Question", System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Warning);
                        switch(res) {
                            case System.Windows.MessageBoxResult.Yes:
                                _general.ManufacturerId = manuId;;
                                break;
                            case System.Windows.MessageBoxResult.Cancel:
                                Archive.Dispose();
                                return;
                        }
                        break;
                    }
                }
            }

            
            ZipArchiveEntry entry;
            XElement xele;
            try{
                entry = Archive.GetEntry($"M-{manuHex}/Baggages.xml");
                xele = XDocument.Load(entry.Open()).Root;
                _namespace = xele.Attribute("xmlns").Value;
                ImportBaggages(manuHex, xele, Archive);
            } catch{
                System.Diagnostics.Debug.WriteLine("Keine Baggages gefunden");
            }


            foreach (ZipArchiveEntry entryTemp in Archive.Entries)
            {
                if (entryTemp.FullName.Contains("_A-"))
                {
                    using (Stream entryStream = entryTemp.Open())
                    {
                        XElement xapp = XDocument.Load(entryStream).Root;
                        _namespace = xapp.Attribute("xmlns").Value;
                        xapp = xapp.Element(Get("ManufacturerData")).Element(Get("Manufacturer")).Element(Get("ApplicationPrograms")).Element(Get("ApplicationProgram"));
                        ImportApplication(xapp);
                    }
                }
            }

            entry = Archive.GetEntry($"M-{manuHex}/Hardware.xml");
            xele = XDocument.Load(entry.Open()).Root;
            _namespace = xele.Attribute("xmlns").Value;
            xele = xele.Element(Get("ManufacturerData")).Element(Get("Manufacturer")).Element(Get("Hardware"));
            ImportLanguages(xele.Parent.Element(Get("Languages")), _general.Languages);
            ImportHardware(xele);

            entry = Archive.GetEntry($"M-{manuHex}/Catalog.xml");
            xele = XDocument.Load(entry.Open()).Root;
            _namespace = xele.Attribute("xmlns").Value;
            xele = xele.Element(Get("ManufacturerData")).Element(Get("Manufacturer")).Element(Get("Catalog"));
            ImportLanguages(xele.Parent.Element(Get("Languages")), _general.Languages);
            ImportCatalog(xele);
        }

        List<string> supportedExtensions = new List<string>() { ".png", ".jpg", ".jpeg" };

        private void ImportBaggages(string manuHex, XElement xele, ZipArchive archive)
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "Knx.Creator");
            if(Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
            Directory.CreateDirectory(tempFolder);

            List<XElement> xbags = xele.Descendants(Get("Baggage")).ToList();
            foreach(XElement xbag in xbags)
            {
                Baggage bag = new Baggage();
                bag.Name = xbag.Attribute("Name").Value;
                bag.Extension = bag.Name.Substring(bag.Name.LastIndexOf('.')).ToLower();
                bag.Name = bag.Name.Substring(0, bag.Name.LastIndexOf('.'));
                bag.TargetPath = xbag.Attribute("TargetPath").Value;

                if(!supportedExtensions.Contains(bag.Extension) || _general.Baggages.Any(b => b.Name == bag.Name && b.TargetPath == bag.TargetPath)) continue;

                bag.TimeStamp = DateTime.Parse(xbag.Element(Get("FileInfo")).Attribute("TimeInfo").Value);
                
                string path = $"{bag.Name}{bag.Extension}";
                if(!string.IsNullOrEmpty(bag.TargetPath)) path = $"{bag.TargetPath}/{path}";
                ZipArchiveEntry entry = Archive.GetEntry($"M-{manuHex}/Baggages/{path}");
                string tempFile = Path.Combine(tempFolder, bag.Name + bag.Extension);
                entry.ExtractToFile(tempFile, true);

                bag.Data = AutoHelper.GetFileBytes(tempFile);

                _general.Baggages.Add(bag);
            }
        }

        private void ImportApplication(XElement xapp)
        {

            #region "Create/Get Application and Version"
            currentApp = null;
            currentVers = null;
            int appNumber = int.Parse(xapp.Attribute("ApplicationNumber").Value);
            int versNumber = int.Parse(xapp.Attribute("ApplicationVersion").Value);

            foreach (Application app in _general.Applications)
            {
                if (app.Number == appNumber)
                {
                    currentApp = app;
                    break;
                }
            }

            if (currentApp == null)
            {
                currentApp = new Application()
                {
                    Number = appNumber,
                    Name = xapp.Attribute("Name").Value,
                    Mask = _bcus.Single(b => b.Id == xapp.Attribute("MaskVersion").Value)
                };
                _general.Applications.Add(currentApp);
            }

            foreach (AppVersion vers in currentApp.Versions)
            {
                if (vers.Number == versNumber)
                {
                    currentVers = vers;
                    break;
                }
            }

            if (currentVers == null)
            {
                currentVers = new AppVersion()
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
            if(_langTexts.ContainsKey(currentVers.DefaultLanguage))
            {
                currentVers.Languages.Add(new Language(_langTexts[currentVers.DefaultLanguage], currentVers.DefaultLanguage));
            } else {
                if(_langTexts.Any(l => l.Key.StartsWith(currentVers.DefaultLanguage)))
                {
                    string lang = _langTexts.First(l => l.Key.StartsWith(currentVers.DefaultLanguage)).Key;
                    currentVers.DefaultLanguage = lang;
                    currentVers.Languages.Add(new Language(_langTexts[currentVers.DefaultLanguage], currentVers.DefaultLanguage));
                } else {
                    System.Windows.Forms.MessageBox.Show($"Sprache '{currentVers.DefaultLanguage}' konnte nicht zugeordnet werden");
                    currentVers.DefaultLanguage = "";
                }
            }

            if(xapp.Attribute("ReplacesVersions") != null) currentVers.ReplacesVersions = xapp.Attribute("ReplacesVersions").Value;
            
#endregion
            XElement xstatic = xapp.Element(Get("Static"));
            CheckUniqueRefId(xstatic, xapp.Element(Get("Dynamic")));
            ImportLanguages(xapp.Parent.Parent.Element(Get("Languages")), currentVers.Languages);
            currentVers.Text = GetTranslation(xapp.Attribute("Id").Value, "Name", xapp);
            ImportSegments(xstatic.Element(Get("Code")));
            ImportParameterTypes(xstatic.Element(Get("ParameterTypes")));
            ImportParameter(xstatic.Element(Get("Parameters")), currentVers);
            ImportParameterRefs(xstatic.Element(Get("ParameterRefs")), currentVers);
            ImportComObjects(xstatic.Element(Get("ComObjectTable")), currentVers);
            ImportComObjectRefs(xstatic.Element(Get("ComObjectRefs")), currentVers);
            ImportMessages(xstatic.Element(Get("Messages")));
            ImportTables(xstatic);
            ImportModules(xapp.Element(Get("ModuleDefs")));
            ImportDynamic(xapp.Element(Get("Dynamic")), currentVers);


            if(xstatic.Element(Get("LoadProcedures")) != null)
            {
                XElement xproc = xstatic.Element(Get("LoadProcedures"));
                xproc.Attributes().Where((x) => x.IsNamespaceDeclaration).Remove();
                xproc.Name = xproc.Name.LocalName;
                foreach(XElement xele in xproc.Descendants())
                {
                    xele.Name = xele.Name.LocalName;
                    if(xele.Name.LocalName == "OnError")
                    {
                        string id = GetLastSplit(xele.Attribute("MessageRef").Value, 2);
                        xele.SetAttributeValue("MessageRef", id);
                    }
                }
                currentVers.Procedure = xproc.ToString();
            }
        }

        private void CheckUniqueRefId(XElement xstatic, XElement xdyn)
        {
            List<long> ids = new List<long>();
            bool flag1 = false;
            bool flag2 = false;

            foreach(XElement xele in xstatic.Descendants(Get("ParameterRef")))
            {
                long paraId = long.Parse(GetLastSplit(xele.Attribute("Id").Value, 2));
                if(!ids.Contains(paraId))
                    ids.Add(paraId);
                else {
                    flag1 = true;
                    break;
                }
            }

            ids.Clear();
            foreach(XElement xele in xstatic.Descendants(Get("ComObjectRef")))
            {
                long comId = long.Parse(GetLastSplit(xele.Attribute("Id").Value, 2));
                if(!ids.Contains(comId))
                    ids.Add(comId);
                else {
                    flag2 = true;
                    break;
                }
            }

            if(flag1 || flag2)
            {
                string text = "Parameter-/ComObjectRefIds";
                if(flag1 && !flag2) text = "ParameterRefIds";
                if(!flag1 && flag2) text = "ComObjectRefIds";
                System.Windows.MessageBox.Show($"Die Produktdatenbank enthält {text}, die nicht komplett eindeutig sind.\r\n\r\nEin Import führt dazu, dass diese geändert werden. Die importierte Version kann somit nicht mehr als Update verwendet werden.", "RefId nicht eindeutig", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            }
            
            DynamicRenameRefIds(flag1, flag2, xstatic, xdyn);
        }

        private void DynamicRenameRefIds(bool renameParas, bool renameComs, XElement xstatic, XElement xdyn)
        {
            Dictionary<string, int> newIds = new Dictionary<string, int>();

            int counter = 1;
            if(renameParas)
            {
                foreach(XElement xele in xstatic.Descendants(Get("ParameterRef")))
                {
                    newIds.Add(xele.Attribute("Id").Value, counter);
                    xele.Attribute("Id").Value = "P-x_R-" + counter;
                    counter++;
                }

                foreach(XElement xele in xdyn.Descendants(Get("ParameterRefRef")))
                    if(xele.Attribute("RefId") != null)
                        xele.Attribute("RefId").Value = "P-x_R-" + newIds[xele.Attribute("RefId").Value];

                foreach(XElement xele in xdyn.Descendants(Get("ComObjectRef")))
                    if(xele.Attribute("TextParameterRefId") != null)
                        xele.Attribute("TextParameterRefId").Value = "P-x_R-" + newIds[xele.Attribute("TextParameterRefId").Value];

                foreach(XElement xele in xdyn.Descendants(Get("Channel")))
                    if(xele.Attribute("TextParameterRefId") != null)
                        xele.Attribute("TextParameterRefId").Value = "P-x_R-" + newIds[xele.Attribute("TextParameterRefId").Value];

                foreach(XElement xele in xdyn.Descendants(Get("Separator")))
                    if(xele.Attribute("TextParameterRefId") != null)
                        xele.Attribute("TextParameterRefId").Value = "P-x_R-" + newIds[xele.Attribute("TextParameterRefId").Value];

                foreach(XElement xele in xdyn.Descendants(Get("ParameterBlock")))
                {
                    if(xele.Attribute("TextParameterRefId") != null)
                        xele.Attribute("TextParameterRefId").Value = "P-x_R-" + newIds[xele.Attribute("TextParameterRefId").Value];
                    if(xele.Attribute("ParamRefId") != null)
                        xele.Attribute("ParamRefId").Value = "P-x_R-" + newIds[xele.Attribute("ParamRefId").Value];
                }

                foreach(XElement xele in xdyn.Descendants(Get("Assign")))
                {
                    if(xele.Attribute("TargetParamRefRef") != null)
                        xele.Attribute("TargetParamRefRef").Value = "P-x_R-" + newIds[xele.Attribute("TargetParamRefRef").Value];
                    if(xele.Attribute("SourceParamRefRef") != null)
                        xele.Attribute("SourceParamRefRef").Value = "P-x_R-" + newIds[xele.Attribute("SourceParamRefRef").Value];
                }

                foreach(XElement xele in xdyn.Descendants(Get("choose")))
                    xele.Attribute("ParamRefId").Value = "P-x_R-" + newIds[xele.Attribute("ParamRefId").Value];

                    
                foreach(XElement xele in xstatic.Parent.Parent.Parent.Element(Get("Languages")).Descendants(Get("TranslationElement")))
                    if(newIds.ContainsKey(xele.Attribute("RefId").Value))
                        xele.Attribute("RefId").Value = "P-x_R-" + newIds[xele.Attribute("RefId").Value];

            }

            counter = 1;
            if(renameComs)
            {
                foreach(XElement xele in xstatic.Descendants(Get("ComObjectRef")))
                {
                    newIds.Add(xele.Attribute("Id").Value, counter);
                    xele.Attribute("Id").Value = "O-x_R-" + counter;
                    counter++;
                }

                foreach(XElement xele in xstatic.Descendants(Get("ComObjectRefRef")))
                {
                    xele.Attribute("Id").Value = "O-x_R-" + newIds[xele.Attribute("Id").Value];
                }
            }

            //TODO rename translationelement
        }

        private void ImportLanguages(XElement xlangs, ObservableCollection<Language> langs) {
            _translations.Clear();
            if(xlangs == null) return;
            
            foreach(XElement xlang in xlangs.Elements()) {
                string cultureCode = xlang.Attribute("Identifier").Value;

                XElement firstUnit = xlang.Elements().ElementAt(0);
                string firstId = firstUnit.Attribute("RefId").Value;

                if(!langs.Any(l => l.CultureCode == cultureCode))
                    langs.Add(new Language(_langTexts[cultureCode], cultureCode));

                foreach(XElement xtele in xlang.Descendants(Get("TranslationElement"))) {
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

        private void AddTranslation(string id, string attr, string lang, string value) {
            if(!_translations.ContainsKey(id)) _translations.Add(id, new Dictionary<string, Dictionary<string, string>>());
            if(!_translations[id].ContainsKey(attr)) _translations[id].Add(attr, new Dictionary<string, string>());
            if(!_translations[id][attr].ContainsKey(lang)) _translations[id][attr].Add(lang, value);
        }

        private ObservableCollection<Translation> GetTranslation(string id, string attr, XElement xele, bool isGeneral = false) {
            ObservableCollection<Translation> translations = new ObservableCollection<Translation>();

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

            if(isGeneral)
            {
                foreach(Language lang in _general.Languages) {
                    if(!translations.Any(t => t.Language.CultureCode == lang.CultureCode)) {
                        if(lang.CultureCode == currentVers.DefaultLanguage)
                            translations.Add(new Translation(lang, xele.Attribute(attr)?.Value ?? ""));
                        else
                            translations.Add(new Translation(lang, ""));
                    }
                }
            }


            return translations;
        }

        private void ImportSegments(XElement xcodes)
        {
            _uidCounter = 1;
            foreach (XElement xcode in xcodes.Elements())
            {
                if (xcode.Name.LocalName == "AbsoluteSegment")
                {
                    currentVers.Memories.Add(new Memory()
                    {
                        UId = _uidCounter++,
                        Address = int.Parse(xcode.Attribute("Address").Value),
                        Size = int.Parse(xcode.Attribute("Size").Value),
                        Name = string.IsNullOrEmpty(xcode.Attribute("Name")?.Value) ? GetLastSplit(xcode.Attribute("Id").Value) : xcode.Attribute("Name").Value,
                        Type = MemoryTypes.Absolute,
                        IsAutoSize = false,
                        IsAutoPara = false
                    });
                }
                else if (xcode.Name.LocalName == "RelativeSegment")
                {
                    currentVers.Memories.Add(new Memory()
                    {
                        UId = _uidCounter++,
                        Size = int.Parse(xcode.Attribute("Size").Value),
                        Offset = int.Parse(xcode.Attribute("Offset")?.Value ?? "0"),
                        Name = string.IsNullOrEmpty(xcode.Attribute("Name")?.Value) ? GetLastSplit(xcode.Attribute("Id").Value) : xcode.Attribute("Name").Value,
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

        private void ImportParameterTypes(XElement xparatypes)
        {
            _uidCounter = 1;

            foreach (XElement xparatype in xparatypes.Elements())
            {
                ParameterType ptype = new ParameterType()
                {
                    Name = xparatype.Attribute("Name").Value,
                    IsSizeManual = true,
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
                            _ => throw new Exception("Unbekannter TypeNumber Type: " + xsub.Attribute("Type").Value)
                        };
                        ptype.SizeInBit = int.Parse(xsub.Attribute("SizeInBit").Value);
                        ptype.Min = int.Parse(xsub.Attribute("minInclusive").Value);
                        ptype.Max = int.Parse(xsub.Attribute("maxInclusive").Value);
                        if(xsub.Attribute("UIHint") != null)
                            ptype.UIHint = xsub.Attribute("UIHint").Value;
                        if(xsub.Attribute("Increment") != null)
                            ptype.Increment = int.Parse(xsub.Attribute("Increment").Value);
                        //TODO displayoffset & displayfactor ab xsd 20
                        break;

                    case "TypeRestriction":
                        ptype.Type = ParameterTypes.Enum;
                        ptype.SizeInBit = int.Parse(xsub.Attribute("SizeInBit").Value);
                        foreach (XElement xenum in xsub.Elements())
                        {
                            ptype.Enums.Add(new ParameterTypeEnum()
                            {
                                Name = xenum.Attribute("Text")?.Value ?? "",
                                Icon = xenum.Attribute("Icon")?.Value ?? "",
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
                        ptype.UIHint = xsub.Attribute("AddressType").Value;
                        ptype.SizeInBit = xsub.Attribute("Version")?.Value switch 
                        {
                            "IPV4" => 1,
                            "IPV6" => 2,
                            _ => 0
                        };
                        break;

                    case "TypeFloat":
                        ptype.Type = xsub.Attribute("Encoding").Value switch
                        {
                            "DPT 9" => ParameterTypes.Float_DPT9,
                            "IEEE-754 Single" => ParameterTypes.Float_IEEE_Single,
                            "IEEE-754 Double" => ParameterTypes.Float_IEEE_Double,
                            _ => throw new Exception("Unbekannter TypeFloat Type: " + xsub.Attribute("Type").Value)
                        };
                        ptype.SizeInBit = 16;
                        ptype.Min = float.Parse(xsub.Attribute("minInclusive").Value.Replace('.', ','));
                        ptype.Max = float.Parse(xsub.Attribute("maxInclusive").Value.Replace('.', ','));
                        if(xsub.Attribute("UIHint") != null)
                            ptype.UIHint = xsub.Attribute("UIHint").Value;
                        if(xsub.Attribute("Increment") != null)
                            ptype.Increment = float.Parse(xsub.Attribute("Increment").Value.Replace('.', ','));
                        break;

                    case "TypePicture":
                        ptype.Type = ParameterTypes.Picture;
                        //ptype.UIHint = xsub.Attribute("RefId").Value;
                        //M-0083_BG--Komfort.2Epng
                        string[] ids = xsub.Attribute("RefId").Value.Split('-');
                        string path = Unescape(ids[2]);
                        string name = Unescape(ids[3]);
                        string extension = name.Substring(name.LastIndexOf('.')).ToLower();
                        name = name.Substring(0, name.LastIndexOf('.'));
                        ptype.BaggageObject = _general.Baggages.Single(b => b.Name == name && b.TargetPath == path && b.Extension == extension);
                        break;

                    case "TypeColor":
                        ptype.Type = ParameterTypes.Color;
                        ptype.UIHint = xsub.Attribute("Space").Value;
                        break;

                    default:
                        throw new Exception("Unbekannter ParameterType: " + xsub.Name.LocalName);
                }

                currentVers.ParameterTypes.Add(ptype);
            }
        }

        private void ImportParameter(XElement xparas, IVersionBase vbase)
        {
            if(xparas == null) return;
            _uidCounter = 1;
            
            foreach(XElement xpara in xparas.Elements(Get("Parameter"))) {
                ParseParameter(xpara, vbase);
            }

            int unionCounter = 1;
            foreach (XElement xunion in xparas.Elements(Get("Union")))
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
                        union.SavePath = SavePaths.Memory;
                        string memName = GetLastSplit(xmem.Attribute("CodeSegment").Value);
                        union.MemoryObject = currentVers.Memories.SingleOrDefault(m => m.Name.StartsWith(memName));
                        if(union.MemoryObject == null && memName.Contains("-RS-"))
                        {
                            int offset = int.Parse(memName.Split('-')[2], System.Globalization.NumberStyles.HexNumber);
                            union.MemoryObject = currentVers.Memories.Single(m => m.Offset == offset);
                        }
                        union.Offset = int.Parse(xmem.Attribute("Offset").Value);
                        union.OffsetBit = int.Parse(xmem.Attribute("BitOffset").Value);
                        break;

                    default:
                        throw new Exception("Not supportet SavePath for Union: " + xmem.Name.LocalName);
                }
                vbase.Unions.Add(union);

                foreach(XElement xpara in xunion.Elements(Get("Parameter"))) {
                    ParseParameter(xpara, vbase, union, xmem);
                }
            }
            
            if(!currentVers.IsUnionActive && unionCounter > 1)
                currentVers.IsUnionActive = true;
        }

        private void ParseParameter(XElement xpara, IVersionBase vbase, Union union = null, XElement xmemory = null)
        {
            Parameter para = new Parameter() {
                Name = xpara.Attribute("Name").Value,
                Value = xpara.Attribute("Value").Value,
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
            para.Suffix = GetTranslation(xpara.Attribute("Id").Value, "SuffixText", xpara);

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
                    para.SavePath = SavePaths.Memory;
                    string memName = GetLastSplit(xmem.Attribute("CodeSegment").Value);
                    if(memName.StartsWith("RS-"))
                        para.SaveObject = currentVers.Memories[0];
                    else{
                        int addr = int.Parse(memName.Split('-')[1], System.Globalization.NumberStyles.HexNumber);
                        para.SaveObject = currentVers.Memories.Single(m => m.Address == addr);
                    }

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
                    if(vbase is Models.Module mod && xmem.Attribute("BaseOffset") != null) {
                        int argid = int.Parse(GetLastSplit(xmem.Attribute("BaseOffset").Value, 2));
                        mod.ParameterBaseOffset = mod.Arguments.Single(a => a.Id == argid);
                    }
                } else if(xmem.Name.LocalName == "Property")
                {
                    para.SavePath = SavePaths.Property;
                    para.SaveObject = new Property() {
                        ObjectIndex = int.Parse(xmem.Attribute("ObjectIndex").Value),
                        PropertyId = int.Parse(xmem.Attribute("PropertyId").Value),
                        Offset = int.Parse(xmem.Attribute("Offset").Value),
                        OffsetBit = int.Parse(xmem.Attribute("BitOffset").Value),
                    };
                }
                else
                {
                    throw new Exception("Unbekannter MemoryTyp für Parameter: " + xmem.Name.LocalName);
                }
            }

            vbase.Parameters.Add(para);
        }

        private void ImportParameterRefs(XElement xrefs, IVersionBase vbase)
        {
            if(xrefs == null) return;
            _uidCounter = 1;

            foreach (XElement xref in xrefs.Elements())
            {
                ParameterRef pref = new ParameterRef();

                pref.UId = _uidCounter++;
                pref.Id = long.Parse(GetLastSplit(xref.Attribute("Id").Value, 2));

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
                pref.ParameterObject = vbase.Parameters.Single(p => p.Id == paraId);
                pref.Name = pref.ParameterObject.Name;
                
                pref.OverwriteText = xref.Attribute("Text") != null;
                pref.Text = GetTranslation(xref.Attribute("Id").Value, "Text", xref);
                pref.Suffix = GetTranslation(xref.Attribute("Id").Value, "SuffixText", xref);
                if(xref.Attribute("DisplayOrder") == null)
                    pref.DisplayOrder = -1;
                else
                    pref.DisplayOrder = int.Parse(xref.Attribute("DisplayOrder").Value);

                vbase.ParameterRefs.Add(pref);
            }
        }

        private void ImportComObjects(XElement xcoms, IVersionBase vbase)
        {
            if(xcoms == null) return;
            _uidCounter = 1;

            foreach (XElement xcom in xcoms.Elements())
            {
                ComObject com = new ComObject()
                {
                    Number = int.Parse(xcom.Attribute("Number").Value),
                    UId = _uidCounter++
                };

                string id = GetLastSplit(xcom.Attribute("Id").Value, 2);
                id = id.Substring(id.LastIndexOf('-') + 1); //Modules haben Id M-00FA_A-0207-23-E298_MD-1_O-2-0
                com.Id = int.Parse(id);

                com.Name = xcom.Attribute("Name")?.Value;
                if(string.IsNullOrEmpty(com.Name))
                    com.Name =  $"{com.Id} - {com.Number}";

                com.Text = GetTranslation(xcom.Attribute("Id").Value, "Text", xcom);
                com.FunctionText = GetTranslation(xcom.Attribute("Id").Value, "FunctionText", xcom);

                com.FlagRead = ParseFlagType(xcom.Attribute("ReadFlag")?.Value);
                com.FlagWrite = ParseFlagType(xcom.Attribute("WriteFlag")?.Value);
                com.FlagComm = ParseFlagType(xcom.Attribute("CommunicationFlag")?.Value);
                com.FlagTrans = ParseFlagType(xcom.Attribute("TransmitFlag")?.Value);
                com.FlagUpdate = ParseFlagType(xcom.Attribute("UpdateFlag")?.Value);
                com.FlagOnInit = ParseFlagType(xcom.Attribute("ReadOnInitFlag")?.Value);

                string[] objSize = xcom.Attribute("ObjectSize").Value.Split(' ');
                if(objSize[1] == "Bit")
                    com.ObjectSize = int.Parse(objSize[0]);
                else
                    com.ObjectSize = int.Parse(objSize[0]) * 8;
                string type = xcom.Attribute("DatapointType")?.Value;

                if (!string.IsNullOrEmpty(type))
                {
                    com.HasDpt = true;
                    if (type.StartsWith("DPST-"))
                    {
                        string[] xtype = type.Split("-");
                        com.Type = DPTs.Single(d => d.Number == xtype[1]);
                        com.HasDpts = com.Type.SubTypes.Any(s => s.Number == xtype[2]);
                        if(com.HasDpts)
                            com.SubType = com.Type.SubTypes.Single(s => s.Number == xtype[2]);
                        else
                        {
                            System.Windows.MessageBox.Show($"{type} wurde nicht gefunden.\r\nSie können versuchen die Datei 'datapoints.json' zu löschen und das Produkt erneut importieren.", "DPST nicht gefunden", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                        }
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

                if(vbase is Models.Module mod && xcom.Attribute("BaseNumber") != null)
                {
                    int argid = int.Parse(GetLastSplit(xcom.Attribute("BaseNumber").Value, 2));
                    mod.ComObjectBaseNumber = mod.Arguments.Single(a => a.Id == argid);
                }

                vbase.ComObjects.Add(com);
            }
        }

        private void ImportComObjectRefs(XElement xrefs, IVersionBase vbase)
        {
            if(xrefs == null) return;
            _uidCounter = 1;

            foreach (XElement xref in xrefs.Elements())
            {
                ComObjectRef cref = new ComObjectRef();

                cref.UId = _uidCounter++;
                cref.Id = int.Parse(GetLastSplit(xref.Attribute("Id").Value, 2));
              
                //cref.OverwriteText = xref.Attribute("Text") != null;
                cref.OverwriteFunctionText = xref.Attribute("FunctionText") != null;

                cref.Text = GetTranslation(xref.Attribute("Id").Value, "Text", xref);
                cref.FunctionText = GetTranslation(xref.Attribute("Id").Value, "FunctionText", xref);

                cref.OverwriteText = cref.Text.Any(t => !string.IsNullOrEmpty(t.Text));

                string id = GetLastSplit(xref.Attribute("RefId").Value, 2);
                id = id.Substring(id.LastIndexOf('-') + 1); //Modules haben Id M-00FA_A-0207-23-E298_MD-1_O-2-0
                int comId = int.Parse(id);
                cref.ComObjectObject = vbase.ComObjects.Single(c => c.Id == comId);
                cref.Name = cref.ComObjectObject.Name;


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

                if(xref.Attribute("ObjectSize") != null)
                {
                    cref.OverwriteOS = true;

                    string[] objSize = xref.Attribute("ObjectSize").Value.Split(' ');
                    if(objSize[1] == "Bit")
                        cref.ObjectSize = int.Parse(objSize[0]);
                    else
                        cref.ObjectSize = int.Parse(objSize[0]) * 8;
                }

                vbase.ComObjectRefs.Add(cref);
            }
        }

        private void ImportMessages(XElement xmsgs)
        {
            if(xmsgs == null) return;

            currentVers.IsMessagesActive = true;

            int counter = 1;
            foreach(XElement xmsg in xmsgs.Elements())
            {
                Message msg = new Message()
                {
                    UId = counter++,
                    Id = int.Parse(GetLastSplit(xmsg.Attribute("Id").Value, 2)),
                    Name = xmsg.Attribute("Name")?.Value,
                    Text = GetTranslation(xmsg.Attribute("Id").Value, "Text", xmsg)
                };
                currentVers.Messages.Add(msg);
            }
        }

        private void ImportTables(XElement xstatic)
        {
            if(xstatic.Element(Get("AddressTable")) != null)
            {
                XElement tadd = xstatic.Element(Get("AddressTable"));
                if(tadd.Attribute("CodeSegment") != null)
                {
                    string segName = GetLastSplit(tadd.Attribute("CodeSegment").Value);
                    currentVers.AddressMemoryObject = currentVers.Memories.SingleOrDefault(m => m.Name == segName);
                }
                currentVers.AddressTableMaxCount = int.Parse(tadd.Attribute("MaxEntries")?.Value ?? "0");
                currentVers.AddressTableOffset = int.Parse(tadd.Attribute("Offset")?.Value ?? "0");
            }
            if(xstatic.Element(Get("AssociationTable")) != null)
            {
                XElement tadd = xstatic.Element(Get("AssociationTable"));
                if(tadd.Attribute("CodeSegment") != null)
                {
                    string segName = GetLastSplit(tadd.Attribute("CodeSegment").Value);
                    currentVers.AssociationMemoryObject = currentVers.Memories.SingleOrDefault(m => m.Name == segName);
                }
                currentVers.AssociationTableMaxCount = int.Parse(tadd.Attribute("MaxEntries")?.Value ?? "0");
                currentVers.AssociationTableOffset = int.Parse(tadd.Attribute("Offset")?.Value ?? "0");
            }
            if(xstatic.Element(Get("ComObjectTable")) != null)
            {
                XElement tadd = xstatic.Element(Get("ComObjectTable"));
                if(tadd.Attribute("CodeSegment") != null)
                {
                    string segName = GetLastSplit(tadd.Attribute("CodeSegment").Value);
                    currentVers.ComObjectMemoryObject = currentVers.Memories.SingleOrDefault(m => m.Name == segName);
                }
                currentVers.ComObjectTableOffset = int.Parse(tadd.Attribute("Offset")?.Value ?? "0");
            }
        }

        private void ImportModules(XElement xmods) {
            if(xmods == null) return;
            _uidCounter = 1;
            currentVers.IsModulesActive = true;

            foreach(XElement xmod in xmods.Elements()) {
                Models.Module mod = new Models.Module() {
                    Name = xmod.Attribute("Name")?.Value ?? "Unbenannt",
                    UId = _uidCounter++,
                    Id = int.Parse(GetLastSplit(xmod.Attribute("Id").Value, 3)),
                    IsParameterRefAuto = false,
                    IsComObjectRefAuto = false
                };

                XElement xstatic = xmod.Element(Get("Static"));
                ImportArguments(xmod.Element(Get("Arguments")), mod);
                ImportParameter(xstatic.Element(Get("Parameters")), mod);
                ImportParameterRefs(xstatic.Element(Get("ParameterRefs")), mod);
                ImportComObjects(xstatic.Element(Get("ComObjects")), mod);
                ImportComObjectRefs(xstatic.Element(Get("ComObjectRefs")), mod);
                ImportDynamic(xmod.Element(Get("Dynamic")), mod);

                currentVers.Modules.Add(mod);
            }
        }

        private void ImportArguments(XElement xargs, Models.Module vbase)
        {
            if(xargs == null) return;
            _uidCounter = 1;

            foreach(XElement xarg in xargs.Elements())
            {
                Argument arg = new Argument() {
                    UId = _uidCounter++,
                    Name = xarg.Attribute("Name").Value,
                    Id = int.Parse(GetLastSplit(xarg.Attribute("Id").Value, 2))
                };
                vbase.Arguments.Add(arg);
            }
        }

        private void ImportHardware(XElement xhards) {
            XElement temp = xhards.Descendants(Get("Product")).FirstOrDefault();
            if(temp != null)
            {
                string def = temp.Attribute("DefaultLanguage").Value;
                if(!_general.Languages.Any(l => l.CultureCode == def))
                    _general.Languages.Add(new Language(_langTexts[def], def));
            }

            foreach(XElement xhard in xhards.Elements()) {
                Hardware hardware;

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

                foreach (XElement xapp in xhard.Descendants(Get("ApplicationProgramRef")))
                {
                    string[] appId = xapp.Attribute("RefId").Value.Split('-');
                    int number = int.Parse(appId[2], System.Globalization.NumberStyles.HexNumber);
                    int version = int.Parse(appId[3], System.Globalization.NumberStyles.HexNumber);

                    hardware.Apps.Add(_general.Applications.Single(a => a.Number == number));
                }

                foreach (XElement xprod in xhard.Descendants(Get("Product")))
                {
                    Device device;
                    string ordernumb = xprod.Attribute("OrderNumber").Value;

                    if (hardware.Devices.Any(d => d.OrderNumber == ordernumb))
                    {
                        device = hardware.Devices.Single(d => d.OrderNumber == ordernumb);
                    }
                    else
                    {
                        device = new Device()
                        {
                            OrderNumber = ordernumb,
                            Name = xprod.Parent.Parent.Attribute("Name").Value,
                            //Text = GetTranslation(xprod.Attribute("Id").Value, "Text", xprod),
                            IsRailMounted = xprod.Attribute("IsRailMounted")?.Value == "true"
                        };
                        hardware.Devices.Add(device);
                    }
                }
            }
        }

        private void ImportCatalog(XElement xcat)
        {
            foreach (XElement xitem in xcat.Elements())
            {
                ParseCatalogItem(xitem, _general.Catalog[0]);
            }

            foreach(Hardware hard in _general.Hardware)
            {
                foreach(Device dev in hard.Devices)
                {
                    foreach(Language lang in _general.Languages)
                    {
                        if(!dev.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                            dev.Text.Add(new Translation(lang, ""));
                        if(!dev.Description.Any(t => t.Language.CultureCode == lang.CultureCode))
                            dev.Description.Add(new Translation(lang, ""));
                    }
                }
            }

            CheckCatalogSectionLanguages(_general.Catalog[0]);
        }

        private void CheckCatalogSectionLanguages(CatalogItem parent)
        {
            foreach(CatalogItem item in parent.Items)
            {
                if(!item.IsSection) continue;

                foreach(Language lang in _general.Languages)
                {
                    if(!item.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        item.Text.Add(new Translation(lang, ""));
                }

                CheckCatalogSectionLanguages(item);
            }
        }

        private CatalogItem GetSection(IEnumerable<string> ids)
        {
            CatalogItem item = null;
            int index = 0;

            while(true)
            {
                item = _general.Catalog[0].Items.SingleOrDefault(i => i.Number == ids.ElementAt(index));
                index++;
                if(item == null) break;
                if(index == ids.Count()) break;
            }

            return item;
        }

        private void ParseCatalogItem(XElement xitem, CatalogItem parent)
        {
            //!TODO check if section exists
            switch (xitem.Name.LocalName)
            {
                case "CatalogSection":
                {
                    string[] ids = xitem.Attribute("Id").Value.Split('-');
                    for(int i = 0; i < ids.Count(); i++)
                        ids[i] = Unescape(ids[i]);
                    CatalogItem item = GetSection(ids.Skip(2));
                    if(item == null)
                    {
                        item = new CatalogItem() 
                        {
                            Parent = parent,
                            Name = xitem.Attribute("Name").Value,
                            Number = xitem.Attribute("Number").Value
                        };
                        parent.Items.Add(item);
                    }
                    item.IsSection = true;
                    item.Text = GetTranslation(xitem.Attribute("Id").Value, "Name", xitem, true);

                    foreach (XElement xele in xitem.Elements())
                        ParseCatalogItem(xele, item);
                    break;
                }

                case "CatalogItem":
                {
                    string[] hard2ref = xitem.Attribute("Hardware2ProgramRefId").Value.Split('-');
                    string serialNr = hard2ref[2];
                    serialNr = Unescape(serialNr);
                    int version = int.Parse(hard2ref[3].Split('_')[0]);
                    Hardware hard = _general.Hardware.Single(h => h.SerialNumber == serialNr && h.Version == version);
                    string prodId = xitem.Attribute("ProductRefId").Value;
                    prodId = prodId.Substring(prodId.LastIndexOf('-')+1);
                    prodId = Unescape(prodId);

                    Device device = hard.Devices.Single(d => d.OrderNumber == prodId);

                    if(device.Text.Count > 0)
                        return;
                        

                    CatalogItem item = new CatalogItem()
                    {
                        Parent = parent,
                        Name = xitem.Attribute("Name").Value,
                        Number = Unescape(xitem.Attribute("Number").Value),
                        IsSection = false,
                        Hardware = hard
                    };
                    item.Text = GetTranslation(xitem.Attribute("Id")?.Value ?? "", "Text", xitem);
                    device.Text = GetTranslation(xitem.Attribute("Id")?.Value ?? "", "Name", xitem, true);
                    device.Description = GetTranslation(xitem.Attribute("Id")?.Value ?? "", "VisibleDescription", xitem, true);
                    parent.Items.Add(item);
                    break;
                }
            }

        }

        private void ImportDynamic(XElement xdyn, IVersionBase vbase)
        {
            DynamicMain main = new DynamicMain();
            ParseDynamic(main, xdyn, vbase);
            vbase.Dynamics.Add(main);
        }


        private void ParseDynamic(IDynItems parent, XElement xeles, IVersionBase vbase)
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
                            Number = xele.Attribute("Number")?.Value ?? "",
                            Parent = parent
                        };
                        if (xele.Attribute("ParamRefId") != null)
                        {
                            dc.UseTextParameter = true;
                            paraId = int.Parse(GetLastSplit(xele.Attribute("ParamRefId").Value, 2));
                            dc.ParameterRefObject = vbase.ParameterRefs.Single(p => p.Id == paraId);
                        }
                        parent.Items.Add(dc);
                        ParseDynamic(dc, xele, vbase);
                        break;

                    case "ChannelIndependentBlock":
                    //case "IndependentChannel":
                        DynChannelIndependent dci = new DynChannelIndependent() {
                            Parent = parent
                        };
                        parent.Items.Add(dci);
                        ParseDynamic(dci, xele, vbase);
                        break;

                    case "ParameterBlock":
                        DynParaBlock dpb = new DynParaBlock() {
                            Name = xele.Attribute("Name")?.Value ?? "",
                            Parent = parent,
                            IsInline = xele.Attribute("Inline")?.Value == "true"
                        };
                        
                        dpb.Access = (xele.Attribute("Access")?.Value) switch {
                            "None" => ParamAccess.None,
                            "Read" => ParamAccess.Read,
                            "ReadWrite" => ParamAccess.ReadWrite,
                            null => ParamAccess.Default,
                            _ => throw new Exception("Unbekannter AccesType für ParameterBlock: " + xele.Attribute("Access").Value)
                        };
                        dpb.Layout = xele.Attribute("Layout")?.Value switch {
                            "Table" => BlockLayout.Table,
                            "Grid" => BlockLayout.Grid,
                            "List" => BlockLayout.List,
                            _ => BlockLayout.List
                        };
                        dpb.Text = GetTranslation(xele.Attribute("Id")?.Value ?? "", "Text", xele);
                        if(xele.Element(Get("Rows")) != null)
                        {
                            foreach(XElement xrow in xele.Element(Get("Rows")).Elements())
                            {
                                ParameterBlockRow row = new ParameterBlockRow()
                                {
                                    Id = int.Parse(GetLastSplit(xrow.Attribute("Id").Value, 2)),
                                    Name = xrow.Attribute("Name")?.Value ?? ""
                                };
                                dpb.Rows.Add(row);
                            }
                        }
                        if(xele.Element(Get("Columns")) != null)
                        {
                            foreach(XElement xcol in xele.Element(Get("Columns")).Elements())
                            {
                                ParameterBlockColumn col = new ParameterBlockColumn()
                                {
                                    Id = int.Parse(GetLastSplit(xcol.Attribute("Id").Value, 2)),
                                    Name = xcol.Attribute("Name")?.Value ?? ""
                                };
                                string width = xcol.Attribute("Width").Value;
                                width = width.Substring(0, width.Length - 1);
                                col.Width = int.Parse(width);
                                dpb.Columns.Add(col);
                            }
                        }
                        if(xele.Attribute("ParamRefId") != null) {
                            dpb.UseParameterRef = true;
                            paraId = int.Parse(GetLastSplit(xele.Attribute("ParamRefId").Value, 2));
                            dpb.ParameterRefObject = vbase.ParameterRefs.Single(p => p.Id == paraId);
                        } else {
                            dpb.Id = int.Parse(GetLastSplit(xele.Attribute("Id").Value, 3));
                        }
                        if(xele.Attribute("TextParameterRefId") != null)
                        {
                            dpb.UseTextParameter = true;
                            paraId = int.Parse(GetLastSplit(xele.Attribute("TextParameterRefId").Value, 2));
                            dpb.TextRefObject = vbase.ParameterRefs.Single(p => p.Id == paraId);
                        }
                        dpb.ShowInComObjectTree = xele.Attribute("ShowInComObjectTree")?.Value.ToLower() == "true";
                        parent.Items.Add(dpb);
                        ParseDynamic(dpb, xele, vbase);
                        break;


                    case "choose":
                        IDynChoose dch;
                        switch(parent)
                        {
                            case DynWhenBlock:
                            case DynParaBlock:
                                dch = new DynChooseBlock();
                                break;

                            case DynWhenChannel:
                            case IDynChannel:
                            case DynamicMain:
                                dch = new DynChooseChannel();
                                break;

                            default:
                                throw new Exception("Not implemented Parent");
                        }
                        dch.Parent = parent;
                        long paraId64_2 = long.Parse(GetLastSplit(xele.Attribute("ParamRefId").Value, 2));
                        dch.ParameterRefObject = vbase.ParameterRefs.Single(p => p.Id == paraId64_2);
                        parent.Items.Add(dch);
                        ParseDynamic(dch, xele, vbase);
                        break;

                    case "when":
                        IDynWhen dw;
                        switch(parent)
                        {
                            case DynChooseBlock:
                                dw = new DynWhenBlock();
                                break;

                            case DynChooseChannel:
                                dw = new DynWhenChannel();
                                break;

                            default:
                                throw new Exception("Not possible Parent");
                        }
                        dw.Condition = xele.Attribute("test")?.Value ?? "";
                        dw.IsDefault = xele.Attribute("default")?.Value == "true";
                        dw.Parent = parent;
                        parent.Items.Add(dw);
                        ParseDynamic(dw, xele, vbase);
                        break;

                    case "ParameterRefRef":
                        DynParameter dp = new DynParameter() {
                            Parent = parent,
                            Cell = xele.Attribute("Cell")?.Value
                        };
                        long paraId64 = long.Parse(GetLastSplit(xele.Attribute("RefId").Value, 2));
                        dp.ParameterRefObject = vbase.ParameterRefs.Single(p => p.Id == paraId64);
                        parent.Items.Add(dp);
                        break;

                    case "ParameterSeparator":
                        DynSeparator ds = new DynSeparator() {
                            Parent = parent,
                            Cell = xele.Attribute("Cell")?.Value
                        };
                        paraId = int.Parse(GetLastSplit(xele.Attribute("Id").Value, 3));
                        ds.Id = paraId;
                        ds.Text = GetTranslation(xele.Attribute("Id")?.Value ?? "", "Text", xele);
                        if(xele.Attribute("UIHint") != null)
                            ds.Hint = (SeparatorHint)Enum.Parse(typeof(SeparatorHint), xele.Attribute("UIHint").Value);
                        else
                            ds.Hint = SeparatorHint.None;
                        parent.Items.Add(ds);
                        break;

                    case "ComObjectRefRef":
                        DynComObject dco = new DynComObject() {
                            Parent = parent
                        };
                        paraId = int.Parse(GetLastSplit(xele.Attribute("RefId").Value, 2));
                        dco.ComObjectRefObject = vbase.ComObjectRefs.Single(p => p.Id == paraId);
                        parent.Items.Add(dco);
                        break;

                    case "Module":
                        DynModule dmo = new DynModule() {
                            Parent = parent
                        };
                        dmo.Id = int.Parse(GetLastSplit(xele.Attribute("Id").Value, 2));
                        paraId = int.Parse(GetLastSplit(xele.Attribute("RefId").Value, 3));
                        dmo.ModuleObject = currentVers.Modules.Single(m => m.Id == paraId);
                        foreach(XElement xarg in xele.Elements())
                        {
                            int id = int.Parse(GetLastSplit(xarg.Attribute("RefId").Value, 2));
                            Argument arg = dmo.ModuleObject.Arguments.Single(a => a.Id == id);
                            DynModuleArg darg = dmo.Arguments.Single(a => a.Argument == arg);
                            darg.Value = xarg.Attribute("Value").Value;
                        }
                        parent.Items.Add(dmo);
                        break;

                    case "Assign":
                        DynAssign dass = new DynAssign() {
                            Parent = parent
                        };
                        int targetid = int.Parse(GetLastSplit(xele.Attribute("TargetParamRefRef").Value, 2));
                        dass.TargetObject = vbase.ParameterRefs.Single(p => p.Id == targetid);
                        if(xele.Attribute("SourceParamRefRef") != null)
                        {
                            int sourceid = int.Parse(GetLastSplit(xele.Attribute("SourceParamRefRef").Value, 2));
                            dass.SourceObject = vbase.ParameterRefs.Single(p => p.Id == sourceid);
                        }
                        dass.Value = xele.Attribute("Value")?.Value;
                        parent.Items.Add(dass);
                        break;

                    case "Rows":
                    case "Columns":
                        break;

                    default:
                        throw new Exception("Unbekanntes Element in Dynamic: " + xele.Name.LocalName);
                }
            }
        }


        private FlagType ParseFlagType(string type)
        {
            return type switch
            {
                "Enabled" => FlagType.Enabled,
                "Disabled" => FlagType.Disabled,
                null => FlagType.Undefined,
                _ => throw new Exception("Unbekannter FlagTyp: " + type)
            };
        }

        private static float StringToFloat(string input, float def = 0)
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

        private string Unescape(string input)
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
            input = input.Replace(".2C", ",");
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
            input = input.Replace(".C2.B0", "°");

            input = input.Replace(".2E", ".");
            return input;
        }

        private string GetLastSplit(string input, int offset = 0)
        {
            return input.Substring(input.LastIndexOf('_') + 1 + offset);
        }

        private XName Get(string name)
        {
            return XName.Get(name, _namespace);
        }
    }
}