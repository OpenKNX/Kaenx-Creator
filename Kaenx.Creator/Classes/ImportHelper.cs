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
using Kaenx.Creator.Controls;
using System.Windows;

namespace Kaenx.Creator.Classes
{
    public class ImportHelper
    {
        private string _namespace;
        private ObservableCollection<MaskVersion> _bcus;
        private ZipArchive Archive { get; set; }
        private MainModel _general;
        private string _path;
        private ObservableCollection<DataPointType> DPTs;
        private int _uidCounter = 1;
        private string AppImportHelper;

        private AppVersion currentVers = null;

        public static Dictionary<string, string> _langTexts = new Dictionary<string, string>() {
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

        public ImportHelper() { }

        public ImportHelper(string path, ObservableCollection<MaskVersion> bcus) {
            _path = path;
            _bcus = bcus;
        }

        public void StartXml(MainModel general, ObservableCollection<DataPointType>dpts)
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
                var res = System.Windows.MessageBox.Show(string.Format(Properties.Resources.import_diff_manu, _general.ManufacturerId.ToString("X4"), manuId.ToString("X4")), Properties.Resources.import_diff_manu_title, System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Warning);
                switch(res) {
                    case System.Windows.MessageBoxResult.Yes:
                        _general.ManufacturerId = manuId;
                        break;
                    case System.Windows.MessageBoxResult.Cancel:
                        return;
                }
            }

            if(_general.ManufacturerId != 0xFA){
                _general.IsOpenKnx = false;
            }

            System.Diagnostics.Debug.WriteLine("XML unterstützt keine Baggages");


            XElement xtemp = xmanu.Element(Get("ApplicationPrograms")).Element(Get("ApplicationProgram"));
            ImportApplication(xtemp);

            ImportLanguages(xmanu.Element(Get("Languages")), _general.Application.Languages);
            xtemp = xmanu.Element(Get("Hardware"));
            ImportHardware(xtemp);

            xtemp = xmanu.Element(Get("Catalog"));
            ImportCatalog(xtemp);
        }

        public void StartZip(MainModel general, ObservableCollection<DataPointType> dpts)
        {
            _general = general;
            DPTs = dpts;
            string manuHex = "";
            Archive = ZipFile.Open(_path, ZipArchiveMode.Read, System.Text.Encoding.GetEncoding(850));
            foreach (ZipArchiveEntry entryTemp in Archive.Entries)
            {
                if (entryTemp.FullName.Contains("M-"))
                {
                    manuHex = entryTemp.FullName.Substring(2, 4);
                    int manuId = int.Parse(manuHex, System.Globalization.NumberStyles.HexNumber);
                    if (_general.ManufacturerId != manuId)
                    {
                        var res = System.Windows.MessageBox.Show(string.Format(Properties.Resources.import_diff_manu, _general.ManufacturerId.ToString("X4"), manuId.ToString("X4")), Properties.Resources.import_diff_manu_title, System.Windows.MessageBoxButton.YesNoCancel, System.Windows.MessageBoxImage.Warning);
                        switch(res) {
                            case System.Windows.MessageBoxResult.Yes:
                                _general.ManufacturerId = manuId;
                                break;
                            case System.Windows.MessageBoxResult.Cancel:
                                Archive.Dispose();
                                return;
                        }
                        break;
                    }
                }
            }

            if(_general.ManufacturerId != 0xFA){
                _general.IsOpenKnx = false;
            }
            
            ZipArchiveEntry entry;
            XElement xele;
            try{
                entry = Archive.GetEntry($"M-{manuHex}/Baggages.xml");
                xele = XDocument.Load(entry.Open()).Root;
                _namespace = xele.Attribute("xmlns").Value;
                ImportBaggages(xele, Archive);
            } catch (NullReferenceException) {
                System.Diagnostics.Debug.WriteLine("Keine Baggages gefunden");
            } catch (Exception ex) {
                System.Diagnostics.Debug.WriteLine($"Baggage Fehler: {ex.Message}");
            }


            IEnumerable<ZipArchiveEntry> entries = Archive.Entries.Where(e => e.FullName.Contains("_A-"));

            ZipArchiveEntry toImport = null;

            if(entries.Count() == 0)
            {
                // TODO translate
                System.Windows.MessageBox.Show("Knxprod enthält keine Applikation");
                return;
            } else if(entries.Count() == 1)
            {
                toImport = entries.ElementAt(0);
            } else {
                List<string> names = new();
                List<KeyValuePair<string, object>> apps = new();
                foreach(ZipArchiveEntry xentry in entries)
                {
                    string appId = xentry.Name.Substring(0, xentry.Name.LastIndexOf('.'));
                    XElement xapp = XDocument.Load(xentry.Open()).Root;
                    _namespace = xapp.Attribute("xmlns").Value;
                    xapp = xapp.Element(Get("ManufacturerData")).Element(Get("Manufacturer")).Element(Get("ApplicationPrograms")).Element(Get("ApplicationProgram"));     
                    string appName  = xapp.Attribute("Name").Value;

                    apps.Add(new KeyValuePair<string, object>(appName + "   (" + appId +")", xentry));
                    names.Add(appName + " (" + appId +")");
                }
                
                // TODO translate
                ListDialog diag = new ("Welche Applikation soll importiert werden?", "Import", (from app in apps select app.Key).ToList());
                diag.ShowDialog();

                if(diag.DialogResult != true) return;

                toImport = (ZipArchiveEntry)apps.First(app => app.Key == diag.Answer).Value;
            }

            using (Stream entryStream = toImport.Open())
            {
                XElement xapp = XDocument.Load(entryStream).Root;
                _namespace = xapp.Attribute("xmlns").Value;
                xapp = xapp.Element(Get("ManufacturerData")).Element(Get("Manufacturer")).Element(Get("ApplicationPrograms")).Element(Get("ApplicationProgram"));
                ImportApplication(xapp);
            }

            entry = Archive.GetEntry($"M-{manuHex}/Hardware.xml");
            xele = XDocument.Load(entry.Open()).Root;
            _namespace = xele.Attribute("xmlns").Value;
            xele = xele.Element(Get("ManufacturerData")).Element(Get("Manufacturer")).Element(Get("Hardware"));
            ImportLanguages(xele.Parent.Element(Get("Languages")), _general.Application.Languages);
            ImportHardware(xele);

            entry = Archive.GetEntry($"M-{manuHex}/Catalog.xml");
            xele = XDocument.Load(entry.Open()).Root;
            _namespace = xele.Attribute("xmlns").Value;
            xele = xele.Element(Get("ManufacturerData")).Element(Get("Manufacturer")).Element(Get("Catalog"));
            ImportLanguages(xele.Parent.Element(Get("Languages")), _general.Application.Languages);
            ImportCatalog(xele);
            Archive.Dispose();
        }

        List<string> supportedExtensions = new List<string>() { ".png", ".jpg", ".jpeg", ".zip" };

        public void ImportBaggages(XElement xele, ZipArchive archive, string tempPath = "", bool IsOpenKnxModule = false)
        {
            string tempFolder = Path.Combine(Path.GetTempPath(), "Knx.Creator");
            if(Directory.Exists(tempFolder))
                Directory.Delete(tempFolder, true);
            Directory.CreateDirectory(tempFolder);

            List<XElement> xbags = xele.Descendants(Get("Baggage")).ToList();

            int counter = 0;
            foreach(XElement xbag in xbags)
            {
                Baggage bag = new Baggage();
                bag.UId = counter++;
                bag.Name = xbag.Attribute("Name").Value;
                bag.Extension = bag.Name.Substring(bag.Name.LastIndexOf('.')).ToLower();
                bag.Name = bag.Name.Substring(0, bag.Name.LastIndexOf('.'));
                bag.TargetPath = xbag.Attribute("TargetPath").Value;

                if(!supportedExtensions.Contains(bag.Extension) || _general.Baggages.Any(b => b.Name == bag.Name && b.TargetPath == bag.TargetPath)) continue;

                ZipArchiveEntry entry = null;
                string path = $"{bag.Name}{bag.Extension}";

                if(bag.Extension == ".zip")
                {
                    if(IsOpenKnxModule)
                    {
                        tempPath = Path.Combine(tempPath, bag.Name);
                        Directory.CreateDirectory(tempPath);
                        foreach(ZipArchiveEntry ent in archive.Entries.Where(e => !string.IsNullOrEmpty(e.Name) && e.FullName.Contains($"/Baggages/{bag.Name}/")))
                            ent.ExtractToFile(Path.Combine(tempPath, ent.Name), true);
                    } else {
                        throw new NotImplementedException();
                    }

                    ImportBaggageZip(tempPath);
                    continue;
                } else {
                    if(!string.IsNullOrEmpty(bag.TargetPath)) path = $"{bag.TargetPath}/{path}";
                    entry = archive.Entries.Single(e => e.FullName.EndsWith($"/Baggages/{path}"));
                }

                bag.LastModified = DateTime.Parse(xbag.Element(Get("FileInfo")).Attribute("TimeInfo").Value);
                
                string tempFile = Path.Combine(tempFolder, bag.Name + bag.Extension);
                entry.ExtractToFile(tempFile, true);

                bag.Data = AutoHelper.GetFileBytes(tempFile);

                _general.Baggages.Add(bag);
            }
        }

        private void ImportBaggageZip(string path)
        {
            string folder = path.Substring(path.LastIndexOf('\\') + 1);
            string[] splits = folder.Split('_');
            string lang = _general.Application.DefaultLanguage;
            if(splits.Length > 1 && _general.Application.Languages.Any(l => l.CultureCode.StartsWith(splits[1])))
            {
                lang = _general.Application.Languages.Where(l => l.CultureCode.StartsWith(splits[1])).First().CultureCode;
            }

            foreach(string file in Directory.GetFiles(path))
            {
                string fileName = Path.GetFileName(file);
                string extension = "";
                if(fileName.Contains('.'))
                    extension = fileName.Substring(fileName.LastIndexOf('.'));
                if(!string.IsNullOrEmpty(extension))
                    fileName = fileName.Replace(extension, "");
                bool add = true;

                if(fileName == "LICENSE")
                    continue;

                switch(extension)
                {
                    case ".png":
                        Icon icon = new Icon() { Name = fileName };
                        if(_general.Icons.Any(i => i.Name == icon.Name))
                        {
                            icon = _general.Icons.Single(i => i.Name == icon.Name);
                            add = false;
                        }
                        icon.Data = AutoHelper.GetFileBytes(file);
                        if(add) _general.Icons.Add(icon);
                        break;

                    case "":
                    case ".txt":
                    case ".md":
                        Helptext help = new Helptext() { Name = fileName};
                        if(_general.Application.Helptexts.Any(h => h.Name == help.Name))
                        {
                            help = _general.Application.Helptexts.Single(h => h.Name == help.Name);
                            add = false;
                        }

                        string text = File.ReadAllText(file);
                        if(help.Text.Any(t => t.Language.CultureCode == lang))
                        {
                            var trans = help.Text.Single(t => t.Language.CultureCode == lang);
                            trans.Text = text;
                        } else {
                            help.Text.Add(new(_general.Application.Languages.Single(l => l.CultureCode == lang), text));
                        }
                        
                        if(add) _general.Application.Helptexts.Add(help);
                        _general.Application.IsHelpActive = true;
                        break;
                }
            }
        }

        private void ImportIcons(string path)
        {
            string manuHex = Archive.Entries.ElementAt(1).FullName.Substring(0, 6);
            ZipArchiveEntry bagEntry = Archive.GetEntry($"{manuHex}/Baggages/{path}");

            ZipArchive zip = new ZipArchive(bagEntry.Open(), ZipArchiveMode.Read, false, System.Text.Encoding.GetEncoding(850));
                    
            foreach(ZipArchiveEntry entry in zip.Entries)
            {
                Icon icon = new Icon()
                {
                    UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(_general.Icons),
                    Name = entry.Name.Substring(0, entry.Name.LastIndexOf('.'))
                };

                using(Stream s = entry.Open())
                {
                    using(MemoryStream ms = new MemoryStream())
                    {
                        s.CopyTo(ms);
                        icon.Data = ms.ToArray();
                    }
                }

                _general.Icons.Add(icon);
            }

            zip.Dispose();
        }

        public void SetCurrentVers(AppVersion vers)
        {
            currentVers = vers;
        }

        public void SetGeneral(MainModel gen)
        {
            _general = gen;
        }

        public void SetDPTs(ObservableCollection<DataPointType> dpts)
        {
            DPTs = dpts;
        }

        private void ImportApplication(XElement xapp)
        {
            System.Console.WriteLine("----------------Neue Applikation-------------------");
            #region "Create/Get Application and Version"
            currentVers = null;

            _general.Info.AppNumber = int.Parse(xapp.Attribute("ApplicationNumber").Value);
            _general.Application.Number = int.Parse(xapp.Attribute("ApplicationVersion").Value);
            _general.Info.Mask = _bcus.Single(b => b.Id == xapp.Attribute("MaskVersion").Value);

            AppImportHelper = xapp.Attribute("Id").Value;

            currentVers = _general.Application;

            currentVers.Name = xapp.Attribute("Name")?.Value ?? "Imported";
            currentVers.IsParameterRefAuto = false;
            currentVers.IsComObjectRefAuto = false;
            currentVers.IsMemSizeAuto = false;
            currentVers.ReplacesVersions = xapp.Attribute("ReplacesVersions")?.Value;

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

            if(xapp.Attribute("PreEts4Style") != null || xapp.Attribute("ConvertedFromPreEts4Data") != null) currentVers.IsPreETS4 = true;
            
#endregion
            Stopwatch sw = new Stopwatch();
            sw.Start();
            XElement xstatic = xapp.Element(Get("Static"));
            CheckUniqueRefId(xstatic, xapp.Element(Get("Dynamic")));
            ImportLanguages(xapp.Parent.Parent.Element(Get("Languages")), currentVers.Languages);
            currentVers.Text = GetTranslation(xapp.Attribute("Id").Value, "Name", xapp);

            if(xapp.Attribute("IconFile") != null)
            {
                if(currentVers.NamespaceVersion < 20)
                    ImportIcons(xapp.Attribute("IconFile").Value);
                else {
                    string[] parts = xapp.Attribute("IconFile").Value.Split('-');
                    string path = ""; //M-0002_BG--Icons.5F20DD11.2Ezip
                    if(!string.IsNullOrEmpty(parts[2]))
                        path = Unescape(parts[2]) + "/";
                    path += Unescape(parts[3]);
                    ImportIcons(path);
                }
            }

            if(xapp.Attribute("AdditionalAddressesCount") != null && int.Parse(xapp.Attribute("AdditionalAddressesCount").Value) != 0)
            {
                currentVers.IsBusInterfaceActive = true;
                IEnumerable<XElement> interfaces = xstatic.Element(Get("BusInterfaces")).Elements();
                currentVers.BusInterfaceCounter = interfaces.Count(i => i.Attribute("AccessType")?.Value == "Tunneling");
                currentVers.HasBusInterfaceRouter = interfaces.Any(i => i.Attribute("AccessType")?.Value == "Routing");
            }

            ImportHelpFile(xapp);
            ImportSegments(xstatic.Element(Get("Code")));
            ImportScript(xstatic.Element(Get("Script")));
            ImportAllocators(xstatic.Element(Get("Allocators")), currentVers);
            ImportParameterTypes(xstatic.Element(Get("ParameterTypes")), currentVers);
            ImportParameter(xstatic.Element(Get("Parameters")), currentVers);
            ImportParameterRefs(xstatic.Element(Get("ParameterRefs")), currentVers);
            Dictionary<string, long> dic = null;
            ImportComObjects(xstatic.Element(Get("ComObjectTable")), currentVers, ref dic);
            ImportComObjectRefs(xstatic.Element(Get("ComObjectRefs")), currentVers);
            ImportMessages(xstatic.Element(Get("Messages")));
            ImportTables(xstatic);
            ImportModules(currentVers, xapp.Element(Get("ModuleDefs")));
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

            sw.Stop();
            System.Console.WriteLine($"Total Time: {sw.ElapsedMilliseconds} ms");
        }

        private Dictionary<long, Parameter> Paras;
        private Dictionary<long, ParameterRef> ParaRefs;
        private Dictionary<long, ComObject> Coms;
        private Dictionary<long, ComObjectRef> ComRefs;

        private void CheckUniqueRefId(XElement xstatic, XElement xdyn)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
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
                System.Windows.MessageBox.Show($"Die Applikation '{_general.Application.Name}' enthält {text}, die nicht komplett eindeutig sind.\r\n\r\nEin Import führt dazu, dass diese geändert werden. Die importierte Version kann somit nicht mehr als Update verwendet werden.", "RefId nicht eindeutig", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                DynamicRenameRefIds(flag1, flag2, xstatic, xdyn);
            }
            
            sw.Stop();
            System.Console.WriteLine($"CheckUniqueRefId: {sw.ElapsedMilliseconds} ms");
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

                foreach(XElement xele in xstatic.Descendants(Get("ComObjectRef")))
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
            if(xlangs == null || xlangs.Elements().Count() == 0) return;
            
            Stopwatch sw = new Stopwatch();
            sw.Start();


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
            sw.Stop();
            System.Console.WriteLine($"ImportLanguages: {sw.ElapsedMilliseconds} ms");
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
                foreach(Language lang in _general.Application.Languages) {
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

        private void ImportHelpFile(XElement xapp)
        {
            if(xapp.Attribute("ContextHelpFile") == null) return;
            currentVers.IsHelpActive = true;
        
            Dictionary<string, Helptext> texts = new Dictionary<string, Helptext>();
            var x = GetTranslation(xapp.Attribute("Id").Value, "ContextHelpFile", xapp);
            if(!x.Any(x => x.Language.CultureCode == currentVers.DefaultLanguage))
                x.Add(new Translation(new Language("", currentVers.DefaultLanguage), xapp.Attribute("ContextHelpFile").Value));

            if(System.IO.Directory.Exists("HelpTemp"))
                System.IO.Directory.Delete("HelpTemp", true);
            System.IO.Directory.CreateDirectory("HelpTemp");

            int textCounter = 1;
            foreach(Translation trans in x)
            {
                string file = "";
                string fullPath = "";

                if(currentVers.NamespaceVersion == 14)
                {
                    file = trans.Text;
                    fullPath = $"{xapp.Attribute("Id").Value.Substring(0, 6):X4}/Baggages/{trans.Text}";
                } else {
                    string[] id = trans.Text.Split('-');
                    string path = id[2];
                    file = Unescape(id[3]);
                    fullPath = trans.Text.Substring(0, 6) + "/Baggages/";
                    if(!string.IsNullOrEmpty(path)) fullPath = fullPath + path + "/";
                    fullPath = fullPath + file;
                }

                ZipArchiveEntry entry = Archive.GetEntry(fullPath);
                string tempPath = System.IO.Path.Combine("HelpTemp", file);
                if(!System.IO.File.Exists(tempPath))
                    using(FileStream stream = System.IO.File.Create(tempPath))
                        using(Stream zs = entry.Open())
                            zs.CopyTo(stream);
                
                ZipArchive zip = ZipFile.Open(tempPath, ZipArchiveMode.Read, System.Text.Encoding.GetEncoding(850));
                
                foreach(ZipArchiveEntry fentry in zip.Entries)
                {
                    string fname = System.IO.Path.GetFileNameWithoutExtension(fentry.Name);
                    if(!texts.ContainsKey(fname))
                        texts.Add(fname, new Helptext() { Name = fname, UId = textCounter++ });

                    Helptext text = texts[fname];
                    if(!text.Text.Any(t => t.Language.CultureCode == trans.Language.CultureCode))
                    {
                        using(StreamReader reader = new StreamReader(fentry.Open()))
                        {
                            text.Text.Add(new Translation(trans.Language, reader.ReadToEnd()));
                        }
                    }
                }

                zip.Dispose();
            }

            foreach(Helptext text in texts.Values)
                currentVers.Helptexts.Add(text);
            
            System.IO.Directory.Delete("HelpTemp", true);
        }

        private void ImportSegments(XElement xcodes)
        {
            if(xcodes == null) return;

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
                    throw new Exception("Masks Memory Type is not supported! " + _general.Info.Mask.Memory);
                }
            }
        }

        public void ImportParameterTypes(XElement xparatypes, AppVersion vers, bool removeExisting = false)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            System.Console.Write("ImportParameterTypes: ");
            _uidCounter = 1;

            foreach (XElement xparatype in xparatypes.Elements())
            {
                ParameterType ptype = new ParameterType()
                {
                    //Name = xparatype.Attribute("Name").Value,
                    IsSizeManual = true,
                    UId = _uidCounter++,
                };

                ptype.Name = GetLastSplit(xparatype.Attribute("Id").Value, 3);
                ptype.Name = Unescape(ptype.Name);

                if(vers.ParameterTypes.Any(p => p.Name == ptype.Name))
                {
                    if(removeExisting)
                    {
                        vers.ParameterTypes.Remove(vers.ParameterTypes.Single(p => p.Name == ptype.Name));
                    } else {
                        ptype = vers.ParameterTypes.Single(p => p.Name == ptype.Name);
                        ptype.ImportHelperName = xparatype.Attribute("Id").Value;
                        continue;
                    }
                }

                ptype.ImportHelperName = xparatype.Attribute("Id").Value;

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
                        ptype.Min = xsub.Attribute("minInclusive").Value;
                        ptype.Max = xsub.Attribute("maxInclusive").Value;
                        if(xsub.Attribute("UIHint") != null)
                            ptype.UIHint = xsub.Attribute("UIHint").Value;
                        if(xsub.Attribute("Increment") != null)
                            ptype.Increment = xsub.Attribute("Increment").Value;
                        //TODO displayoffset & displayfactor ab xsd 20
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
                        ptype.Min = xsub.Attribute("minInclusive").Value.Replace('.', ',');
                        ptype.Max = xsub.Attribute("maxInclusive").Value.Replace('.', ',');
                        if(xsub.Attribute("UIHint") != null)
                            ptype.UIHint = xsub.Attribute("UIHint").Value;
                        if(xsub.Attribute("Increment") != null)
                            ptype.Increment = xsub.Attribute("Increment").Value.Replace('.', ',');
                        break;

                    case "TypeRestriction":
                        ptype.Type = ParameterTypes.Enum;
                        ptype.SizeInBit = int.Parse(xsub.Attribute("SizeInBit").Value);
                        foreach (XElement xenum in xsub.Elements())
                        {
                            ParameterTypeEnum penum = new ParameterTypeEnum()
                            {
                                Name = xenum.Attribute("Text")?.Value ?? "",
                                Text = GetTranslation(xenum.Attribute("Id").Value, "Text", xenum),
                                Value = int.Parse(xenum.Attribute("Value").Value)
                            };
                            
                            if(!string.IsNullOrEmpty(xenum.Attribute("Icon")?.Value))
                            {
                                penum.UseIcon = true;
                                penum.IconObject = _general.Icons.SingleOrDefault(i => i.Name == xenum.Attribute("Icon").Value);
                            }

                            ptype.Enums.Add(penum);
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
                        ptype.Increment = xsub.Attribute("Version")?.Value switch 
                        {
                            "IPV4" => "IPv4",
                            "IPV6" => "IPv6",
                            _ => "None"
                        };
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
                        ptype.UIHint = xsub.Attribute("HorizontalAlignment")?.Value ?? "";
                        break;

                    case "TypeColor":
                        ptype.Type = ParameterTypes.Color;
                        ptype.UIHint = xsub.Attribute("Space").Value;
                        break;

                    case "TypeRawData":
                        ptype.Type = ParameterTypes.RawData;
                        ptype.Max = xsub.Attribute("MaxSize").Value;
                        break;

                    case "TypeDate":
                        ptype.Type = ParameterTypes.Date;
                        ptype.OtherValue = (xsub.Attribute("DisplayTheYear")?.Value ?? "true") == "true"; 
                        break;

                    case "TypeTime":
                        ptype.Type = ParameterTypes.Time;
                        ptype.UIHint = xsub.Attribute("UIHint")?.Value ?? "";
                        ptype.Increment = xsub.Attribute("Unit").Value;
                        ptype.Min = xsub.Attribute("minInclusive").Value.Replace('.', ',');
                        ptype.Max = xsub.Attribute("maxInclusive").Value.Replace('.', ',');
                        break;

                    default:
                        throw new Exception("Unbekannter ParameterType: " + xsub.Name.LocalName);
                }

                ptype.IsSizeManual = true;

                vers.ParameterTypes.Add(ptype);
            }
            sw.Stop();
            System.Console.WriteLine($"{sw.ElapsedMilliseconds} ms");
        }

        public void ImportParameter(XElement xparas, IVersionBase vbase)
        {
            Paras = new Dictionary<long, Parameter>();
            if(xparas == null) return;
            _uidCounter = 1;
            Stopwatch sw = new Stopwatch();
            sw.Start();
            
            foreach(XElement xpara in xparas.Elements(Get("Parameter"))) {
                ParseParameter(xpara, vbase);
            }

            int unionCounter = 1;
            foreach (XElement xunion in xparas.Elements(Get("Union")))
            {
                _general.Application.IsUnionActive = true;
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
                        if(union.MemoryObject == null && memName.Contains("RS-"))
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

            sw.Stop();
            System.Console.WriteLine($"ImportParameter: {sw.ElapsedMilliseconds} ms");
        }

        private bool CheckTranslation(ObservableCollection<Translation> trans)
        {
            if(trans.Count <= 1) return false;
            return trans.Count(t => t.Language.CultureCode != currentVers.DefaultLanguage && string.IsNullOrEmpty(t.Text)) >= (trans.Count -1);
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
            if (id.StartsWith('-'))
                id = id.Substring(1);
            para.Id = int.Parse(id);

            Paras.Add(para.Id, para);

            para.Text = GetTranslation(xpara.Attribute("Id").Value, "Text", xpara);
            para.TranslationText = CheckTranslation(para.Text);
            para.Suffix = GetTranslation(xpara.Attribute("Id").Value, "SuffixText", xpara);
            para.TranslationSuffix = CheckTranslation(para.Suffix);

            para.Access = (xpara.Attribute("Access")?.Value) switch {
                "None" => ParamAccess.None,
                "Read" => ParamAccess.Read,
                "ReadWrite" => ParamAccess.ReadWrite,
                null => ParamAccess.ReadWrite,
                _ => throw new Exception("Unbekannter AccessType für Parameter: " + xpara.Attribute("Access").Value)
            };

            string typeName = xpara.Attribute("ParameterType").Value;
            para.ParameterTypeObject = currentVers.ParameterTypes.Single(t => t.ImportHelperName == typeName);

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
                        ObjectType = int.Parse(xmem.Attribute("ObjectIndex").Value),
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

        public void SetParas(ObservableCollection<Parameter> paras)
        {
            Paras = new Dictionary<long, Parameter>();
            foreach(Parameter para in paras)
                Paras.Add(para.Id, para);
        }

        public void ImportParameterRefs(XElement xrefs, IVersionBase vbase)
        {
            if(xrefs == null) return;
            _uidCounter = 1;
            Stopwatch sw = new Stopwatch();
            sw.Start();

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
                    null => ParamAccess.ReadWrite,
                    _ => throw new Exception("Unbekannter Access Typ für ParameterRef: " + xref.Attribute("Access")?.Value)
                };
                string id = GetLastSplit(xref.Attribute("RefId").Value, 2);
                if (id.StartsWith('-'))
                    id = id.Substring(1);
                long paraId = long.Parse(id);
                if(Paras.ContainsKey(paraId))
                    pref.ParameterObject = Paras[paraId];
                //pref.ParameterObject = vbase.Parameters.Single(p => p.Id == paraId);
                pref.Name = pref.ParameterObject?.Name ?? "Zuordnungsfehler";
                
                pref.OverwriteText = xref.Attribute("Text") != null;
                pref.Text = GetTranslation(xref.Attribute("Id").Value, "Text", xref);
                pref.Suffix = GetTranslation(xref.Attribute("Id").Value, "SuffixText", xref);
                if(xref.Attribute("DisplayOrder") == null)
                    pref.DisplayOrder = -1;
                else
                    pref.DisplayOrder = int.Parse(xref.Attribute("DisplayOrder").Value);

                vbase.ParameterRefs.Add(pref);
            }

            sw.Stop();
            System.Console.WriteLine($"ImportParameterRefs: {sw.ElapsedMilliseconds} ms");
        }

        public void ImportComObjects(XElement xcoms, IVersionBase vbase, ref Dictionary<string, long> idmapper, bool checkOffsets = true)
        {
            Coms = new Dictionary<long, ComObject>();

            if(xcoms == null) return;
            _uidCounter = 1;
            Stopwatch sw = new Stopwatch();
            sw.Start();


            bool countNew = false;
            int counter = 0;

            if(checkOffsets && vbase is Models.Module)
            {
                int firstId = -1;
                foreach(XElement xref in xcoms.Elements())
                {
                    string[] id = xref.Attribute("Id").Value.Split('-');
                    int currId = int.Parse(id[id.Length - 2]);
                    if(firstId == -1)
                    {
                        firstId = currId;
                    } else {
                        if(firstId != currId)
                        {
                            string modName = xcoms.Parent.Parent.Attribute("Name").Value;
                            System.Windows.MessageBox.Show($"Modul '{modName}' enthält mehrere BaseOffsets für ComObjects.\r\nDas wird nicht von Kaenx-Creator unterstützt.\r\nAlle Ids werden neu zugeordnet.", "Module Warnung", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
                            idmapper = new Dictionary<string, long>();
                            countNew = true;
                            break;
                        }
                    }
                }
            }


            foreach (XElement xcom in xcoms.Elements())
            {
                ComObject com = new ComObject()
                {
                    Number = int.Parse(xcom.Attribute("Number").Value),
                    UId = _uidCounter++
                };


                if(countNew)
                {
                    com.Id = counter++;
                    idmapper.Add(xcom.Attribute("Id").Value, com.Id);
                } else {
                    string id = xcom.Attribute("Id").Value;
                    id = id.Substring(id.LastIndexOf('-') + 1);
                    com.Id = long.Parse(id);
                }

                Coms.Add(com.Id, com);



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
                        string[] xtype = type.Split('-');
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
                        string[] xtype = type.Split('-');
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
            sw.Stop();
            System.Console.WriteLine($"ImportComObjects: {sw.ElapsedMilliseconds} ms");
        }

        public void ImportComObjectRefs(XElement xrefs, IVersionBase vbase, Dictionary<string, long> idmapper = null)
        {
            if(xrefs == null) return;
            _uidCounter = 1;
            Stopwatch sw = new Stopwatch();
            sw.Start();

            
            ParaRefs = new Dictionary<long, ParameterRef>();
            foreach(ParameterRef pref in vbase.ParameterRefs)
                ParaRefs.Add(pref.Id, pref);


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

                if(idmapper != null)
                {
                    long refId = idmapper[xref.Attribute("RefId").Value];
                    cref.ComObjectObject = Coms[refId];
                } else {
                    string id = xref.Attribute("RefId").Value;
                    id = id.Substring(id.LastIndexOf('-') + 1);
                    long comId = long.Parse(id);
                    cref.ComObjectObject = Coms[comId];
                }
                cref.Name = cref.ComObjectObject.Name;

                if(xref.Attribute("TextParameterRefId") != null)
                {
                    long tid = long.Parse(GetLastSplit(xref.Attribute("TextParameterRefId").Value, 2));
                    cref.UseTextParameter = true;
                    if(ParaRefs.ContainsKey(tid))
                        cref.ParameterRefObject = ParaRefs[tid];
                }

                cref.FlagRead = ParseFlagType(xref.Attribute("ReadFlag")?.Value);
                cref.OverwriteFR = IsFlagTypeOverwriten(xref.Attribute("ReadFlag")?.Value);
                //if (cref.FlagRead == FlagType.Undefined) cref.FlagRead = FlagType.Disabled;
                cref.FlagWrite = ParseFlagType(xref.Attribute("WriteFlag")?.Value);
                cref.OverwriteFW = IsFlagTypeOverwriten(xref.Attribute("WriteFlag")?.Value);
                //if (cref.FlagWrite == FlagType.Undefined) cref.FlagWrite = FlagType.Disabled;
                cref.FlagComm = ParseFlagType(xref.Attribute("CommunicationFlag")?.Value);
                cref.OverwriteFC = IsFlagTypeOverwriten(xref.Attribute("CommunicationFlag")?.Value);
                //if (cref.FlagComm == FlagType.Undefined) cref.FlagComm = FlagType.Disabled;
                cref.FlagTrans = ParseFlagType(xref.Attribute("TransmitFlag")?.Value);
                cref.OverwriteFT = IsFlagTypeOverwriten(xref.Attribute("TransmitFlag")?.Value);
                //if (cref.FlagTrans == FlagType.Undefined) cref.FlagTrans = FlagType.Disabled;
                cref.FlagUpdate = ParseFlagType(xref.Attribute("UpdateFlag")?.Value);
                cref.OverwriteFU = IsFlagTypeOverwriten(xref.Attribute("UpdateFlag")?.Value);
                //if (cref.FlagUpdate == FlagType.Undefined) cref.FlagUpdate = FlagType.Disabled;
                cref.FlagOnInit = ParseFlagType(xref.Attribute("ReadOnInitFlag")?.Value);
                cref.OverwriteFOI = IsFlagTypeOverwriten(xref.Attribute("ReadOnInitFlag")?.Value);
                //if (cref.FlagOnInit == FlagType.Undefined) cref.FlagOnInit = FlagType.Disabled;

                if (!string.IsNullOrEmpty(xref.Attribute("DatapointType")?.Value))
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

            sw.Stop();
            System.Console.WriteLine($"ImportComObjectRefs: {sw.ElapsedMilliseconds} ms");
        }

        private void ImportMessages(XElement xmsgs)
        {
            if(xmsgs == null) return;
            Stopwatch sw = new Stopwatch();
            sw.Start();

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

            sw.Stop();
            System.Console.WriteLine($"ImportMessages: {sw.ElapsedMilliseconds} ms");
        }

        private void ImportTables(XElement xstatic)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

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

            sw.Stop();
            System.Console.WriteLine($"ImportTables: {sw.ElapsedMilliseconds} ms");
        }

        private void ImportModules(IVersionBase vbase, XElement xmods) {
            if(xmods == null) return;
            int _uidCounter2 = 1;

            if(vbase is AppVersion av)
                av.IsModulesActive = true;

            foreach(XElement xmod in xmods.Elements()) {
                Models.Module mod = new Models.Module() {
                    Name = xmod.Attribute("Name")?.Value ?? "Unbenannt",
                    UId = _uidCounter2++,
                    Id = int.Parse(GetLastSplit(xmod.Attribute("Id").Value, 3)),
                    IsParameterRefAuto = false,
                    IsComObjectRefAuto = false
                };
                System.Console.WriteLine($"---Import Module {mod.Name}");

                Dictionary<string, long> idmapper = null;
                XElement xstatic = xmod.Element(Get("Static"));
                ImportArguments(xmod.Element(Get("Arguments")), mod);
                ImportAllocators(xstatic.Element(Get("Allocators")), mod);
                ImportParameter(xstatic.Element(Get("Parameters")), mod);
                ImportParameterRefs(xstatic.Element(Get("ParameterRefs")), mod);
                ImportComObjects(xstatic.Element(Get("ComObjects")), mod, ref idmapper);
                ImportComObjectRefs(xstatic.Element(Get("ComObjectRefs")), mod, idmapper);

                ImportModules(mod, xmod.Element(Get("SubModuleDefs")));

                ImportDynamic(xmod.Element(Get("Dynamic")), mod);

                vbase.Modules.Add(mod);
                

                System.Console.WriteLine("---End Module");
            }
        }
        
        private void ImportScript(XElement xscript)
        {
            currentVers.Script = xscript?.Value ?? "";
        }

        private void ImportAllocators(XElement xallocs, IVersionBase vbase)
        {
            if(xallocs == null) return;

            int counter = 1;
            foreach(XElement xalloc in xallocs.Elements())
            {
                Allocator alloc = new Allocator() {
                    UId = counter++,
                    Name = xalloc.Attribute("Name").Value,
                    Id = int.Parse(GetLastSplit(xalloc.Attribute("Id").Value, 2)),
                    Start = int.Parse(xalloc.Attribute("Start").Value),
                    Max = int.Parse(xalloc.Attribute("maxInclusive").Value)
                };

                vbase.Allocators.Add(alloc);
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
                    Id = int.Parse(GetLastSplit(xarg.Attribute("Id").Value, 2)),
                };

                if(xarg.Attribute("Allocates") != null)
                {
                    arg.Allocates = int.Parse(xarg.Attribute("Allocates").Value);
                }
                
                arg.Type = xarg.Attribute("Type")?.Value switch {
                    "Numeric" => ArgumentTypes.Numeric,
                    "Text" => ArgumentTypes.Text,
                    _ => ArgumentTypes.Numeric
                };
                vbase.Arguments.Add(arg);
            }
        }

        private void ImportHardware(XElement xhards) {
            XElement temp = xhards.Descendants(Get("ApplicationProgramRef")).Single(h => h.Attribute("RefId").Value == AppImportHelper);
            XElement xhard = temp.Parent.Parent.Parent;

            _general.Info.Name = xhard.Attribute("Name").Value;
            _general.Info.SerialNumber = xhard.Attribute("SerialNumber").Value;
            _general.Info.Version = int.Parse(xhard.Attribute("VersionNumber").Value);
            _general.Info.HasApplicationProgram = xhard.Attribute("HasApplicationProgram")?.Value == "true" || xhard.Attribute("HasApplicationProgram")?.Value == "1";
            _general.Info.HasIndividualAddress = xhard.Attribute("HasIndividualAddress")?.Value == "true" || xhard.Attribute("HasIndividualAddress")?.Value == "1";
            _general.Info.BusCurrent = (int)StringToFloat(xhard.Attribute("BusCurrent")?.Value, 10);
            _general.Info.IsIpEnabled = xhard.Attribute("IsIPEnabled")?.Value == "true" || xhard.Attribute("IsIPEnabled")?.Value == "1";

            XElement xprods = xhard.Element(Get("Products"));
            XElement xprod = null;
            string toImport = "";
            if(xprods.Elements().Count() > 1) {
                List<KeyValuePair<string, string>> prods = [];
                foreach(XElement xprodt in xprods.Elements()) {
                    prods.Add(new ($"{xprodt.Attribute("Text").Value} ({xprodt.Attribute("OrderNumber").Value})", xprodt.Attribute("Id").Value));
                }
                // TODO translate
                ListDialog diag = new ("Welches Produkt soll importiert werden?", "Import", (from prod in prods select prod.Key).ToList());
                diag.ShowDialog();
                if(diag.DialogResult == true) {
                    string id = prods.First(prod => prod.Key == diag.Answer).Value;
                    xprod = xprods.Elements().Single(p => p.Attribute("Id").Value == id);
                }
            }
            
            if(xprod == null) {
                xprod = xprods.Elements().ElementAt(0);
            }

            string def = xprod.Attribute("DefaultLanguage").Value;

            if(!def.Contains('-')){
                def = _langTexts.Keys.First(l => l.StartsWith(def + '-'));
            }

            if(!_general.Application.Languages.Any(l => l.CultureCode == def))
                _general.Application.Languages.Add(new Language(_langTexts[def], def));

            _general.Info.OrderNumber = xprod.Attribute("OrderNumber").Value;
            _general.Info.Name = xprod.Parent.Parent.Attribute("Name").Value;
            _general.Info.IsRailMounted = xprod.Attribute("IsRailMounted")?.Value == "true" || xprod.Attribute("IsRailMounted")?.Value == "1";
        }

        private void ImportCatalog(XElement xcat)
        {
            string orderNumber = ExportHelper.GetEncoded(_general.Info.OrderNumber);
            XElement xitem = xcat.Descendants(Get("CatalogItem")).Single(c => c.Attribute("Id").Value.Contains(orderNumber));

            List<XElement> parents = [xitem];
            while(xitem.Parent.Name.LocalName != "Catalog") {
                xitem = xitem.Parent;
                parents.Insert(0, xitem);
            }

            CatalogItem _current = _general.Catalog[0];
            foreach(XElement x in parents) {
                _current = ParseCatalogItem(x, _current);
                if(!_current.IsSection) continue;

                foreach(Language lang in _general.Application.Languages)
                {
                    if(!_current.Text.Any(t => t.Language.CultureCode == lang.CultureCode))
                        _current.Text.Add(new Translation(lang, ""));
                }
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

        private CatalogItem ParseCatalogItem(XElement xitem, CatalogItem parent)
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

                    return item;
                }

                case "CatalogItem":
                {
                    string id = xitem.Attribute("Hardware2ProgramRefId").Value.Substring(xitem.Attribute("Hardware2ProgramRefId").Value.IndexOf("_HP-") + 3, 13);
                    _general.Info.Text = GetTranslation(xitem.Attribute("Id")?.Value ?? "", "Name", xitem, true);
                    _general.Info.Description = GetTranslation(xitem.Attribute("Id")?.Value ?? "", "VisibleDescription", xitem, true);

                    CatalogItem item = new CatalogItem()
                    {
                        Parent = parent,
                        Name = xitem.Attribute("Name").Value,
                        Number = Unescape(xitem.Attribute("Number").Value),
                        IsSection = false,
                        Text = GetTranslation(xitem.Attribute("Id")?.Value ?? "", "Text", xitem)
                    };
                    
                    parent.Items.Add(item);
                    return item;
                }

                default:
                    throw new NotImplementedException("Not implemented CatalogType: " + xitem.Name.LocalName);
            }
        }

        public void ImportDynamic(XElement xdyn, IVersionBase vbase)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();

            ParaRefs = new Dictionary<long, ParameterRef>();
            foreach(ParameterRef pref in vbase.ParameterRefs)
                ParaRefs.Add(pref.Id, pref);

            ComRefs = new Dictionary<long, ComObjectRef>();
            foreach(ComObjectRef cref in vbase.ComObjectRefs)
                ComRefs.Add(cref.Id, cref);

            IDynamicMain main;

            if(vbase.Dynamics.Count() > 0)
                main = vbase.Dynamics[0];
            else {
                if(vbase is AppVersion av)
                    main = new DynamicMain();
                else
                    main = new DynamicModule();
                vbase.Dynamics.Add(main);
            }
            ParseDynamic(main, xdyn, vbase);
            sw.Stop();
            System.Console.WriteLine($"ImportDynamic: {sw.ElapsedMilliseconds} ms");
        }

        private long assigncounter = 1;

        private void ParseDynamic(IDynItems parent, XElement xeles, IVersionBase vbase)
        {
            foreach (XElement xele in xeles.Elements())
            {
                long paraId = 0;

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
                        if (xele.Attribute("TextParameterRefId") != null)
                        {
                            dc.UseTextParameter = true;
                            paraId = long.Parse(GetLastSplit(xele.Attribute("TextParameterRefId").Value, 2));
                            if(ParaRefs.ContainsKey(paraId))
                                dc.ParameterRefObject = ParaRefs[paraId];
                        }
                        if(!string.IsNullOrEmpty(xele.Attribute("Icon")?.Value))
                        {
                            dc.UseIcon = true;
                            dc.IconObject = _general.Icons.SingleOrDefault(i => i.Name == xele.Attribute("Icon").Value);
                        }
                        dc.Access = (xele.Attribute("Access")?.Value) switch {
                            "None" => ParamAccess.None,
                            "Read" => ParamAccess.Read,
                            "ReadWrite" => ParamAccess.ReadWrite,
                            null => ParamAccess.ReadWrite,
                            _ => throw new Exception("Unbekannter AccesType für Channel: " + xele.Attribute("Access").Value)
                        };
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
                            null => ParamAccess.ReadWrite,
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
                            paraId = long.Parse(GetLastSplit(xele.Attribute("ParamRefId").Value, 2));
                            if(ParaRefs.ContainsKey(paraId))
                                dpb.ParameterRefObject = ParaRefs[paraId];
                        } else {
                            dpb.Id = int.Parse(GetLastSplit(xele.Attribute("Id").Value, 3));
                        }
                        if(xele.Attribute("TextParameterRefId") != null)
                        {
                            dpb.UseTextParameter = true;
                            paraId = long.Parse(GetLastSplit(xele.Attribute("TextParameterRefId").Value, 2));
                            if(ParaRefs.ContainsKey(paraId))
                                dpb.TextRefObject = ParaRefs[paraId];
                        }
                        dpb.ShowInComObjectTree = xele.Attribute("ShowInComObjectTree")?.Value.ToLower() == "true";
                        if(!string.IsNullOrEmpty(xele.Attribute("Icon")?.Value))
                        {
                            dpb.UseIcon = true;
                            dpb.IconObject = _general.Icons.SingleOrDefault(i => i.Name == xele.Attribute("Icon").Value);
                        }
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
                        paraId = long.Parse(GetLastSplit(xele.Attribute("ParamRefId").Value, 2));
                        if(ParaRefs.ContainsKey(paraId))
                            dch.ParameterRefObject = ParaRefs[paraId];
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
                        paraId = long.Parse(GetLastSplit(xele.Attribute("RefId").Value, 2));
                        if(ParaRefs.ContainsKey(paraId))
                            dp.ParameterRefObject = ParaRefs[paraId];
                        if(xele.Attribute("HelpContext") != null && !string.IsNullOrEmpty(xele.Attribute("HelpContext").Value))
                        {
                            dp.HasHelptext = true;
                            dp.Helptext = currentVers.Helptexts.SingleOrDefault(h => h.Name == xele.Attribute("HelpContext").Value);
                        }
                        if(!string.IsNullOrEmpty(xele.Attribute("Icon")?.Value))
                        {
                            dp.UseIcon = true;
                            dp.IconObject = _general.Icons.SingleOrDefault(i => i.Name == xele.Attribute("Icon").Value);
                        }
                        parent.Items.Add(dp);
                        break;

                    case "ParameterSeparator":
                        DynSeparator ds = new DynSeparator() {
                            Parent = parent,
                            Cell = xele.Attribute("Cell")?.Value
                        };
                        paraId = long.Parse(GetLastSplit(xele.Attribute("Id").Value, 3));
                        ds.Id = (int)paraId;
                        ds.Text = GetTranslation(xele.Attribute("Id")?.Value ?? "", "Text", xele);
                        //TODO use parameterText??
                        if(xele.Attribute("UIHint") != null)
                            ds.Hint = (SeparatorHint)Enum.Parse(typeof(SeparatorHint), xele.Attribute("UIHint").Value);
                        else
                            ds.Hint = SeparatorHint.None;
                        if(!string.IsNullOrEmpty(xele.Attribute("Icon")?.Value))
                        {
                            ds.UseIcon = true;
                            ds.IconObject = _general.Icons.SingleOrDefault(i => i.Name == xele.Attribute("Icon").Value);
                        }
                        ds.Access = (xele.Attribute("Access")?.Value) switch {
                            "None" => ParamAccess.None,
                            "Read" => ParamAccess.Read,
                            "ReadWrite" => ParamAccess.ReadWrite,
                            null => ParamAccess.ReadWrite,
                            _ => throw new Exception("Unbekannter AccesType für ParameterSeparator: " + xele.Attribute("Access").Value)
                        };
                        parent.Items.Add(ds);
                        break;

                    case "ComObjectRefRef":
                        DynComObject dco = new DynComObject() {
                            Parent = parent
                        };
                        long comId = long.Parse(GetLastSplit(xele.Attribute("RefId").Value, 2));
                        if(ComRefs.ContainsKey(comId))
                        dco.ComObjectRefObject = ComRefs[comId];
                        parent.Items.Add(dco);
                        break;

                    case "Module":
                        DynModule dmo = new DynModule() {
                            Parent = parent
                        };
                        int xid = 0;
                        if(int.TryParse(GetLastSplit(xele.Attribute("Id").Value, 2), out xid))
                        {
                            dmo.Id = xid;
                            paraId = int.Parse(GetLastSplit(xele.Attribute("RefId").Value, 3));
                            dmo.ModuleObject = vbase.Modules.Single(m => m.Id == paraId);
                            foreach(XElement xarg in xele.Elements())
                            {
                                int id1 = int.Parse(GetLastSplit(xarg.Attribute("RefId").Value, 2));
                                Argument arg = dmo.ModuleObject.Arguments.Single(a => a.Id == id1);
                                DynModuleArg darg = dmo.Arguments.Single(a => a.Argument == arg);
                                if(xarg.Attribute("AllocatorRefId") != null)
                                {
                                    int id3 = int.Parse(GetLastSplit(xarg.Attribute("AllocatorRefId").Value, 2));
                                    darg.Allocator = vbase.Allocators.Single(a => a.Id == id3);
                                    darg.UseAllocator = true;
                                } else {
                                    darg.Value = xarg.Attribute("Value").Value;
                                }
                            }
                        } else {
                            MessageBox.Show("Ein Modul konnte nicht zugeordnet werden.\r\n\r\n" + xele.ToString()[..100]);
                        }
                        parent.Items.Add(dmo);
                        break;

                    case "Assign":
                        DynAssign dass = new DynAssign() {
                            Parent = parent,
                            uid = assigncounter++
                        };
                        long targetid = long.Parse(GetLastSplit(xele.Attribute("TargetParamRefRef").Value, 2));
                        if(ParaRefs.ContainsKey(targetid))
                            dass.TargetObject = ParaRefs[targetid];
                        if(xele.Attribute("SourceParamRefRef") != null)
                        {
                            long sourceid = long.Parse(GetLastSplit(xele.Attribute("SourceParamRefRef").Value, 2));
                            if(ParaRefs.ContainsKey(sourceid))
                                dass.SourceObject = ParaRefs[sourceid];
                        }
                        dass.Value = xele.Attribute("Value")?.Value;
                        parent.Items.Add(dass);
                        break;

                    case "Rename":
                    case "ParameterBlockRename":
                        DynRename dpbr = new DynRename() {
                            Parent = parent,
                            Name = xele.Attribute("Name")?.Value ?? "",
                            Id = long.Parse(GetLastSplit(xele.Attribute("Id").Value, 3)),
                        };
                        string[] id2 = GetLastSplit(xele.Attribute("RefId").Value).Split('-');
                        dpbr.RefId = long.Parse(id2[1]);
                        dpbr.Text = GetTranslation(xele.Attribute("Id").Value, "Text", xele);
                        break;

                    case "Rows":
                    case "Columns":
                        break;

                    case "Repeat":
                        DynRepeat drep = new DynRepeat() {
                            Name = xele.Attribute("Name").Value,
                            Id = int.Parse(GetLastSplit(xele.Attribute("Id").Value, 2))
                        };
                        if(xele.Attribute("Count") != null)
                        {
                            drep.Count = int.Parse(xele.Attribute("Count").Value);
                        }
                        if(xele.Attribute("ParameterRefId") != null)
                        {
                            drep.UseParameterRef = true;
                            paraId = long.Parse(GetLastSplit(xele.Attribute("ParameterRefId").Value, 2));
                            if(ParaRefs.ContainsKey(paraId))
                                drep.ParameterRefObject = ParaRefs[paraId];
                        }
                        parent.Items.Add(drep);
                        ParseDynamic(drep, xele, vbase);
                        break;

                    case "Button":
                        DynButton dbtn = new DynButton() {
                            EventHandlerParameters = xele.Attribute("EventHandlerParameters")?.Value ?? "",
                            Online = xele.Attribute("EventHandlerOnline")?.Value ?? ""
                        };
                        if(xele.Attribute("Name") != null)
                            dbtn.Name = xele.Attribute("Name").Value;
                        else
                            dbtn.Name = xele.Attribute("EventHandler").Value;

                        if(!string.IsNullOrEmpty(xele.Attribute("Icon")?.Value))
                        {
                            dbtn.UseIcon = true;
                            dbtn.IconObject = _general.Icons.SingleOrDefault(i => i.Name == xele.Attribute("Icon").Value);
                        }
                        if(xele.Attribute("TextParameterRefId") != null)
                        {
                            dbtn.UseTextParameter = true;
                            paraId = long.Parse(GetLastSplit(xele.Attribute("TextParameterRefId").Value, 2));
                            if(ParaRefs.ContainsKey(paraId))
                                dbtn.TextRefObject = ParaRefs[paraId];
                        }
                        Regex regex = new Regex(@"function %handler%[ ]?\(.+\)( ||\n){0,}{([^}]+)}".Replace("%handler%", xele.Attribute("EventHandler").Value));
                        Match m = regex.Match(currentVers.Script);
                        dbtn.Script = m.Groups[2].Value;
                        dbtn.Script = dbtn.Script.Trim(' ', '\r', '\n');
                        currentVers.Script = regex.Replace(currentVers.Script, "");
                        parent.Items.Add(dbtn);
                        dbtn.Text = GetTranslation(xele.Attribute("Id").Value, "Text", xele);
                        break;

                    case "include":
                        //include von producer überspringen
                        break;

                    default:
                        throw new Exception("Unbekanntes Element in Dynamic: " + xele.Name.LocalName);
                }
            }
        }


        private bool ParseFlagType(string type)
        {
            return type switch
            {
                "Enabled" => true,
                "Disabled" => false,
                "Undefined" => false,
                null => false,
                _ => throw new Exception("Unbekannter FlagTyp: " + type)
            };
        }

        private bool IsFlagTypeOverwriten(string type)
        {
            return type switch
            {
                "Enabled" => true,
                "Disabled" => true,
                "Undefined" => false,
                null => false,
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

        public static string Unescape(string input)
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

        public void SetNamespace(string ns)
        {
            _namespace = ns;
        }
    }
}
