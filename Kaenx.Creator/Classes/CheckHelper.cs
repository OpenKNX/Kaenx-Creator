using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Xml.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;


namespace Kaenx.Creator.Classes {

    public class CheckHelper
    {
        
        public static void CheckThis(Application app,
                                    AppVersionModel version,
                                    ObservableCollection<PublishAction> actions,
                                    bool showOnlyErrors = false)
        {
            CheckThis(null, null, null, new List<Application>() { app }, new List<AppVersionModel>() { version }, actions, showOnlyErrors);
        }

        public static void CheckThis(ModelGeneral General,
                                    List<Hardware> hardware,
                                    List<Device> devices,
                                    List<Application> apps,
                                    List<AppVersionModel> versions,
                                    ObservableCollection<PublishAction> actions, bool showOnlyErrors = false)
        {
            if(apps.Count == 0) {
                actions.Add(new PublishAction() { Text = $"Es wurden keine Applikationen ausgewählt.", State = PublishState.Fail });
                return;
            }

            actions.Add(new PublishAction() { Text = "Starte Check" });
            actions.Add(new PublishAction() { Text = $"{devices?.Count} Geräte - {hardware?.Count} Hardware - {apps.Count} Applikationen - {versions.Count} Versionen" });

            if(General != null)
            {
                if(General.Catalog[0].Items.Any(c => !c.IsSection ))
                    actions.Add(new PublishAction() { Text = "Katalog muss mindestens eine Unterkategorie haben.", State = PublishState.Fail });

                foreach(CatalogItem citem in General.Catalog[0].Items)
                    CheckCatalogItem(citem, actions);

                if(General.ManufacturerId <= 0 || General.ManufacturerId > 0xFFFF)
                    actions.Add(new PublishAction() { Text = $"Ungültige HerstellerId angegeben: {General.ManufacturerId:X4}", State = PublishState.Fail });


                #region Hardware Check
                actions.Add(new PublishAction() { Text = "Überprüfe Hardware" });
                List<string> serials = new List<string>();

                var check1 = General.Hardware.GroupBy(h => h.Name).Where(h => h.Count() > 1);
                foreach(var group in check1)
                    actions.Add(new PublishAction() { Text = "Hardwarename '" + group.Key + "' wird von " + group.Count() + " Hardware verwendet", State = PublishState.Fail });

                check1 = General.Hardware.GroupBy(h => h.SerialNumber).Where(h => h.Count() > 1);
                foreach (var group in check1)
                    actions.Add(new PublishAction() { Text = "Hardwareserial '" + group.Key + "' wird von " + group.Count() + " Hardware verwendet", State = PublishState.Fail });

                IEnumerable<Hardware> check2;
                if(!showOnlyErrors)
                {
                    check1 = null;
                    check2 = General.Hardware.Where(h => h.Devices.Count == 0);
                    foreach (var group in check2)
                        actions.Add(new PublishAction() { Text = "Hardware '" + group.Name + "' hat keine Geräte zugeordnet", State = PublishState.Warning });

                    check2 = General.Hardware.Where(h => h.HasApplicationProgram && h.Apps.Count == 0);
                    foreach (var group in check2)
                        actions.Add(new PublishAction() { Text = "Hardware '" + group.Name + "' hat keine Applikation zugeordnet", State = PublishState.Warning });

                    check2 = General.Hardware.Where(h => !h.HasApplicationProgram && h.Apps.Count != 0);
                    foreach (var group in check2)
                        actions.Add(new PublishAction() { Text = "Hardware '" + group.Name + "' hat Applikation zugeordnet obwohl angegeben ist, dass keine benötigt wird", State = PublishState.Warning });
                }
                #endregion


                #region Applikation Check
                actions.Add(new PublishAction() { Text = "Überprüfe Applikationen" });

                var check3 = General.Applications.GroupBy(h => h.Name).Where(h => h.Count() > 1);
                foreach (var group in check3)
                    actions.Add(new PublishAction() { Text = "Applikationsname '" + group.Key + "' wird von " + group.Count() + " Applikationen verwendet", State = PublishState.Fail });

                check3 = null;
                var check4 = General.Applications.GroupBy(h => h.Number).Where(h => h.Count() > 1);
                foreach (var group in check4)
                    actions.Add(new PublishAction() { Text = "Applikations Nummer " + group.Key + " (" + group.Key.ToString("X4") + ") wird von " + group.Count() + " Applikationen verwendet", State = PublishState.Fail });

                check4 = null;
                foreach(Application app in General.Applications)
                {
                    var check5 = app.Versions.GroupBy(v => v.Number).Where(l => l.Count() > 1);
                    foreach (var group in check5)
                        actions.Add(new PublishAction() { Text = "Applikation '" + app.NameText + "' verwendet Version " + group.Key + " (" + Math.Floor(group.Key / 16.0) + "." + (group.Key % 16) + ") " + group.Count() + " mal", State = PublishState.Fail });
                }
                #endregion
            }

            //TODO check hardware/device/app

            foreach(AppVersionModel model in versions)
            {
                AppVersion version = AutoHelper.GetAppVersion(General, model);
                Application app = apps.Single(a => a.Versions.Any(v => v.Name == version.Name && v.Number == version.Number));
                actions.Add(new PublishAction() { Text = $"Prüfe Applikation '{app.NameText}' Version '{version.NameText}'" });
                CheckVersion(General, app, version, actions, showOnlyErrors);
            }
            
            actions.Add(new PublishAction() { Text = "Ende Check" });
        }


        public static void CheckVersion(
            ModelGeneral General, 
            Application app,
            AppVersion vers, 
            ObservableCollection<PublishAction> actions, 
            bool showOnlyErrors = false)
        {
            if(string.IsNullOrEmpty(app.Mask.MediumTypes))
            {
                actions.Add(new PublishAction() { Text = $"MediumTypes nicht gesetzt. Löschen Sie Data/maskversion.json und starten Sie die Anwendung neu.", State = PublishState.Fail });
            }
            
            if(vers.IsModulesActive && vers.NamespaceVersion < 20)
                actions.Add(new PublishAction() { Text = $"ModuleDefines werden erst ab Namespace 20 unterstützt.", State = PublishState.Fail });

            if(vers.IsMessagesActive && vers.NamespaceVersion < 14)
                actions.Add(new PublishAction() { Text = $"Messages werden erst ab Namespace 14 unterstützt.", State = PublishState.Fail });

            if(app.Mask.Procedure != ProcedureTypes.Default && string.IsNullOrEmpty(vers.Procedure))
                actions.Add(new PublishAction() { Text = $"Version muss eine Ladeprozedur enthalten.", State = PublishState.Fail });

            if(vers.IsHelpActive && vers.NamespaceVersion < 14)
                actions.Add(new PublishAction() { Text = $"Hilfstexte werden erst ab Namespace Version 14 unterstützt", State = PublishState.Fail });
            if(vers.Memories.Count > 0 && !vers.Memories[0].IsAutoLoad)
            {
                XElement xtemp = XElement.Parse(vers.Procedure);
                foreach(XElement xele in xtemp.Descendants())
                {
                    if(xele.Name.LocalName == "LdCtrlRelSegment")
                    {
                        if(xele.Attribute("LsmIdx").Value == "4")
                        {
                            int memsize = int.Parse(xele.Attribute("Size")?.Value ?? "0");
                            if(memsize != vers.Memories[0].Size)
                                actions.Add(new PublishAction() { Text = $"Ladeprozedur {xele.Name.LocalName}: Speichergröße stimmt nicht überein", State = PublishState.Warning });
                        }
                    }
                    if(xele.Name.LocalName == "LdCtrlWriteRelMem")
                    {
                        if(xele.Attribute("ObjIdx").Value == "4")
                        {
                            int memsize = int.Parse(xele.Attribute("Size")?.Value ?? "0");
                            if(memsize != vers.Memories[0].Size)
                                actions.Add(new PublishAction() { Text = $"Ladeprozedur {xele.Name.LocalName}: Speichergröße stimmt nicht überein", State = PublishState.Warning });
                        }
                    }
                }
            }

            foreach(ParameterType ptype in vers.ParameterTypes) {
                long maxsize = (long)Math.Pow(2, ptype.SizeInBit);
    
                if(ptype.UIHint == "CheckBox" && (ptype.Min != "0" || ptype.Max != "1"))
                    actions.Add(new PublishAction() { Text = $"    ParameterType Text {ptype.Name} ({ptype.UId}): Wenn UIHint Checkbox ist, ist Min=0 und Max=1 erforderlich", State = PublishState.Fail, Item = ptype });
                        
                switch(ptype.Type) {
                    case ParameterTypes.Text:
                        if(!showOnlyErrors && ptype.SizeInBit % 8 != 0)
                            actions.Add(new PublishAction() { Text = $"    ParameterType Text {ptype.Name} ({ptype.UId}): ist kein vielfaches von 8", State = PublishState.Fail, Item = ptype });
                        break;

                    case ParameterTypes.Enum:
                        var x = ptype.Enums.GroupBy(e => e.Value);
                        foreach(var group in x.Where(g => g.Count() > 1))
                            actions.Add(new PublishAction() { Text = $"    ParameterType Enum {ptype.Name} ({ptype.UId}): Wert ({group.Key}) wird öfters verwendet", State = PublishState.Fail, Item = ptype });
                        
                        if(!ptype.IsSizeManual)
                        {
                            int maxValue = -1;
                            foreach(ParameterTypeEnum penum in ptype.Enums)
                                if(penum.Value > maxValue)
                                    maxValue = penum.Value;
                            string bin = Convert.ToString(maxValue, 2);
                            ptype.SizeInBit = bin.Length;
                            maxsize = (int)Math.Pow(2, ptype.SizeInBit);
                        }

                        foreach(ParameterTypeEnum penum in ptype.Enums){
                            if(penum.Value >= maxsize)
                                actions.Add(new PublishAction() { Text = $"    ParameterType Enum {ptype.Name} ({ptype.UId}): Wert ({penum.Value}) ist größer als maximaler Wert ({maxsize-1})", State = PublishState.Fail, Item = ptype });

                            if(!penum.Translate) {
                                Translation trans = penum.Text.Single(t => t.Language.CultureCode == vers.DefaultLanguage);
                                if(string.IsNullOrEmpty(trans.Text))
                                    actions.Add(new PublishAction() { Text = $"    ParameterType Enum {penum.Name}/{ptype.Name} ({ptype.UId}): Keine Übersetzung vorhanden ({trans.Language.Text})", State = PublishState.Fail, Item = ptype });
                            } else {
                                if(!showOnlyErrors)
                                    foreach(Translation trans in penum.Text)
                                        if(string.IsNullOrEmpty(trans.Text))
                                            actions.Add(new PublishAction() { Text = $"    ParameterType Enum {penum.Name}/{ptype.Name} ({ptype.UId}): Keine Übersetzung vorhanden ({trans.Language.Text})", State = PublishState.Warning, Item = ptype });
                            }
                        }
                        break;

                    case ParameterTypes.NumberUInt:
                    {
                        if(ptype.UIHint == "ProgressBar" && vers.NamespaceVersion < 20)
                            actions.Add(new PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Progressbar wird erst ab /20 unterstützt", State = PublishState.Fail, Item = ptype });

                        long min, max;
                        if(!long.TryParse(ptype.Max, out max)) actions.Add(new PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Maximum ist keine Ganzzahl", State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.Max, out min)) actions.Add(new PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Minimum ist keine Ganzzahl", State = PublishState.Fail, Item = ptype });

                        if(!ptype.IsSizeManual)
                        {
                            string bin = Convert.ToString(max, 2);
                            ptype.SizeInBit = bin.Length;
                            maxsize = (int)Math.Pow(2, ptype.SizeInBit);
                        }
                        if(min < 0) actions.Add(new PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Min kann nicht kleiner als 0 sein", State = PublishState.Fail, Item = ptype });
                        if(min > max) actions.Add(new PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Min ({ptype.Min}) ist größer als Max ({ptype.Max})", State = PublishState.Fail, Item = ptype });
                        if(max >= maxsize) actions.Add(new PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Max ({ptype.Max}) kann nicht größer als das Maximum ({maxsize-1}) sein", State = PublishState.Fail, Item = ptype });
                        break;
                    }

                    case ParameterTypes.NumberInt:
                    {
                        if(ptype.UIHint == "Progressbar" && vers.NamespaceVersion < 20)
                            actions.Add(new PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Progressbar wird erst ab /20 unterstützt", State = PublishState.Fail, Item = ptype });

                        long min, max;
                        if(!long.TryParse(ptype.Max, out max)) actions.Add(new PublishAction() { Text = $"    ParameterType Int {ptype.Name} ({ptype.UId}): Maximum ist keine Ganzzahl", State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.Max, out min)) actions.Add(new PublishAction() { Text = $"    ParameterType Int {ptype.Name} ({ptype.UId}): Minimum ist keine Ganzzahl", State = PublishState.Fail, Item = ptype });

                        maxsize = (long)Math.Ceiling(maxsize / 2.0);
                        if(!ptype.IsSizeManual)
                        {
                            long z = min * (-1);
                            if(z < (max - 1)) z = min;
                            string y = z.ToString().Replace("-", "");
                            string bin = Convert.ToString(int.Parse(y), 2);
                            if(z == (min * (-1))) bin += "1";
                            if(!z.ToString().StartsWith("-")) bin = "1" + bin;
                            ptype.SizeInBit = bin.Length;
                            maxsize = (int)Math.Pow(2, ptype.SizeInBit);
                        }
                        if(min > max) actions.Add(new PublishAction() { Text = $"    ParameterType Int {ptype.Name} ({ptype.UId}): Min ({ptype.Min}) ist größer als Max ({ptype.Max})", State = PublishState.Fail, Item = ptype });
                        if(max > ((maxsize)-1)) actions.Add(new PublishAction() { Text = $"    ParameterType Int {ptype.Name} ({ptype.UId}): Max ({ptype.Max}) kann nicht größer als das Maximum ({(maxsize/2)-1}) sein", State = PublishState.Fail, Item = ptype });
                        if(min < ((maxsize)*(-1))) actions.Add(new PublishAction() { Text = $"    ParameterType Int {ptype.Name} ({ptype.UId}): Min ({ptype.Min}) kann nicht kleiner als das Minimum ({(maxsize/2)*(-1)}) sein", State = PublishState.Fail, Item = ptype });
                        break;
                    }

                    case ParameterTypes.Float_DPT9:
                    case ParameterTypes.Float_IEEE_Single:
                    case ParameterTypes.Float_IEEE_Double:
                        actions.Add(new PublishAction() { Text = $"    ParameterType Float {ptype.Name} ({ptype.UId}): Aktuell keine Überprüfung vorhanden", State = PublishState.Warning, Item = ptype });
                        break;

                    case ParameterTypes.Picture:
                        if(ptype.BaggageObject == null)
                            actions.Add(new PublishAction() { Text = $"    ParameterTyp Picture für {ptype.Name} ({ptype.UId}) ist kein Baggage zugeordnet", State = PublishState.Fail, Item = ptype });
                        break;

                    case ParameterTypes.None:
                        if(!vers.IsPreETS4)
                            actions.Add(new PublishAction() { Text = $"    ParameterTyp None {ptype.Name} ({ptype.UId}): Wird eigentlich nur in ETS3 unterstützt. IsPreETS4 aktivieren", State = PublishState.Warning, Item = ptype });
                        break;

                    case ParameterTypes.Color:
                        if(ptype.UIHint != "RGB" && ptype.UIHint != "RGBW" && ptype.UIHint != "HSV")
                            actions.Add(new PublishAction() { Text = $"    ParameterTyp Color {ptype.Name} ({ptype.UId}): Nicht unterstützter Farbraum {ptype.UIHint}", State = PublishState.Fail, Item = ptype });
                        if(ptype.UIHint == "RGBW" && vers.NamespaceVersion < 20)
                            actions.Add(new PublishAction() { Text = $"    ParameterTyp Color {ptype.Name} ({ptype.UId}): RGBW wird erst ab NamespaceVersion 20 unterstützt", State = PublishState.Fail, Item = ptype });
                        break;

                    case ParameterTypes.IpAddress:
                        //actions.Add(new PublishAction() { Text = $"    ParameterTyp IpAddress für {ptype.Name} ({ptype.UId}) wird nicht exportiert", State = PublishState.Warning, Item = ptype });
                        ptype.SizeInBit = 32;
                        break;

                    default:
                        actions.Add(new PublishAction() { Text = $"    Unbekannter ParameterTyp für {ptype.Name} ({ptype.UId})", State = PublishState.Fail, Item = ptype });
                        break;
                }
            }

            CheckVersion(vers, vers, actions, vers.DefaultLanguage, vers.NamespaceVersion, showOnlyErrors);
            if(General != null)
                CheckLanguages(vers, actions, General, null);

            foreach(Module mod in vers.Modules)
            {
                actions.Add(new PublishAction() { Text = $"Prüfe Module '{mod.Name}'" });
                CheckVersion(vers, mod, actions, vers.DefaultLanguage, vers.NamespaceVersion, showOnlyErrors);
                //TODO check for Argument exist
            }

            XElement temp = XElement.Parse(vers.Procedure);
            temp.Attributes().Where((x) => x.IsNamespaceDeclaration).Remove();
            foreach(XElement xele in temp.Descendants())
            {
                if(xele.Name.LocalName == "OnError")
                {
                    if(!vers.IsMessagesActive)
                    {
                        actions.Add(new PublishAction() { Text = $"Ladeprozedur: Es werden Meldungen verwendet, obwohl diese in der Applikation nicht aktiviert sind.", State = PublishState.Fail });
                        return;
                    }

                    int id = -1;
                    if(!int.TryParse(xele.Attribute("MessageRef").Value, out id))
                        actions.Add(new PublishAction() { Text = $"Ladeprozedur: MessageRef ist kein Integer.", State = PublishState.Fail });
                    if(id != -1)
                    {
                        if(!vers.Messages.Any(m => m.UId == id))
                            actions.Add(new PublishAction() { Text = $"Ladeprozedur: MessageRef zeigt auf nicht vorhandene Meldung ({id}).", State = PublishState.Fail });
                    }
                }
            }
        }

        private static void CheckCatalogItem(CatalogItem item, ObservableCollection<PublishAction> actions)
        {
            if(item.IsSection)
            {
                if(string.IsNullOrEmpty(item.Number))
                    actions.Add(new PublishAction() { Text = $"Katalog {item.Name}: Sektions Nummer ist leer", State = PublishState.Fail });

                foreach(CatalogItem citem in item.Items)
                    CheckCatalogItem(citem, actions);
            } else {
                int number = -1;
                if(!int.TryParse(item.Number, out number))
                    actions.Add(new PublishAction() { Text = $"Katalog {item.Name}: Produkt Nummer ist keine Ganzzahl ({item.Number})", State = PublishState.Fail });
                if(item.Hardware == null)
                    actions.Add(new PublishAction() { Text = $"Katalog {item.Name}: Es wurde keine Hardware ausgewählt", State = PublishState.Fail });
            }
        }

        private static void CheckVersion(AppVersion ver, IVersionBase vbase, ObservableCollection<PublishAction> actions, string defaultLang, int ns, bool showOnlyErrors)
        {
            Module mod = vbase as Module;
            //TODO check languages from Texts
            //TODO check hexvalue from parameter with parameertype color

            if(mod != null && ver.NamespaceVersion < 20 && mod.Allocators.Count > 0)
                actions.Add(new PublishAction() { Text = $"    Modul ({mod.Name}): Allocators werden erst ab /20 unterstützt", State = PublishState.Fail });

            foreach(Parameter para in vbase.Parameters) {
                if(para.ParameterTypeObject == null) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Kein ParameterTyp ausgewählt", State = PublishState.Fail, Item = para, Module = mod });
                else {
                    switch(para.ParameterTypeObject.Type) {
                        case ParameterTypes.Text:
                            if((para.Value.Length*8) > para.ParameterTypeObject.SizeInBit) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert benötigt mehr Speicher ({(para.Value.Length*8)}) als verfügbar ({para.ParameterTypeObject.SizeInBit}) ist", State = PublishState.Fail, Item = para, Module = mod });
                            break;

                        case ParameterTypes.Enum:
                            int paraval2;
                            if(!int.TryParse(para.Value, out paraval2)) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = PublishState.Fail, Item = para, Module = mod });
                            else {
                                if(!para.ParameterTypeObject.Enums.Any(e => e.Value == paraval2))
                                    actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist nicht als option in Enum vorhanden", State = PublishState.Fail, Item = para, Module = mod });
                            }
                            break;

                        case ParameterTypes.NumberUInt:
                        case ParameterTypes.NumberInt:
                            int paraval;
                            if(!int.TryParse(para.Value, out paraval)) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = PublishState.Fail, Item = para, Module = mod });
                            else {
                                if(paraval > long.Parse(para.ParameterTypeObject.Max) || paraval < long.Parse(para.ParameterTypeObject.Min))
                                    actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) fällt nicht in Bereich {para.ParameterTypeObject.Min}-{para.ParameterTypeObject.Max}", State = PublishState.Fail, Item = para, Module = mod });
                            }
                            break;

                        case ParameterTypes.Float_DPT9:
                        case ParameterTypes.Float_IEEE_Single:
                        case ParameterTypes.Float_IEEE_Double:


                        case ParameterTypes.Picture:
                        case ParameterTypes.None:
                        case ParameterTypes.IpAddress:
                            break;

                        case ParameterTypes.Color:
                            Regex reg = null;
                            switch(para.ParameterTypeObject.UIHint)
                            {
                                case "RGB":
                                case "HSV":
                                    reg = new Regex("([0-9a-fA-F]{6,6})");
                                    break;
                                    
                                case "RGBW":
                                    reg = new Regex("([0-9a-fA-F]{8,8})");
                                    break;
                            }
                            if(reg != null && !reg.IsMatch(para.Value))
                                actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültiger Hexwert für {para.ParameterTypeObject.UIHint}", State = PublishState.Fail, Item = para, Module = mod });
                            break;
                    }
                }
                
                if(!showOnlyErrors)
                {
                    if(para.TranslationText) {
                        Translation trans = para.Text.Single(t => t.Language.CultureCode == defaultLang);
                        if(string.IsNullOrEmpty(trans.Text))
                            actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = PublishState.Warning, Item = para, Module = mod });
                    } else {
                        if(para.Text.Any(s => string.IsNullOrEmpty(s.Text)))
                        {
                            actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Text nicht in allen Sprachen übersetzt", State = PublishState.Warning, Item = para, Module = mod });
                        }
                    }
                }

                if(!para.TranslationSuffix && !showOnlyErrors) {
                    if(!string.IsNullOrEmpty(para.Suffix.Single(t => t.Language.CultureCode == defaultLang).Text))
                    {
                        if(para.Suffix.Any(s => string.IsNullOrEmpty(s.Text)))
                        {
                            actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Suffix nicht in allen Sprachen übersetzt", State = PublishState.Warning, Item = para, Module = mod });
                        }
                    }
                }

                if(!para.IsInUnion) {
                    switch(para.SavePath) {
                        case SavePaths.Memory:
                        {
                            if(para.SaveObject == null)
                                actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Kein Speichersegment ausgewählt", State = PublishState.Fail, Item = para, Module = mod });
                            else {
                                if(!(para.SaveObject as Memory).IsAutoPara && para.Offset == -1) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name}: Kein Offset angegeben", State = PublishState.Fail, Item = para, Module = mod });
                                if(!(para.SaveObject as Memory).IsAutoPara && para.OffsetBit == -1) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name}: Kein Bit Offset angegeben", State = PublishState.Fail, Item = para, Module = mod });

                            }
                            if(para.OffsetBit > 7) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): BitOffset größer als 7 und somit obsolet", State = PublishState.Fail, Item = para, Module = mod });
                            break;
                        }

                        case SavePaths.Property:
                        {
                            if((para.SaveObject as Property).ObjectIndex == -1) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): ObjectIndex nicht festgelegt", State = PublishState.Fail, Item = para, Module = mod });
                            if((para.SaveObject as Property).PropertyId == -1) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): PropertyId nicht festgelegt", State = PublishState.Fail, Item = para, Module = mod });
                            break;
                        }
                    }
                }
            }
        
            foreach(ParameterRef para in vbase.ParameterRefs) {
                if(para.ParameterObject == null) actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Kein Parameter ausgewählt", State = PublishState.Fail, Item = para, Module = mod });
                else {
                    if(para.ParameterObject.ParameterTypeObject == null || string.IsNullOrEmpty(para.Value))
                        continue;
                    
                    //TODO check value overwrite
                    ParameterType ptype = para.ParameterObject.ParameterTypeObject;

                    if(para.OverwriteText)
                    {
                        if(para.ParameterObject.TranslationText) {
                            Translation trans = para.Text.Single(t => t.Language.CultureCode == defaultLang);
                            if(string.IsNullOrEmpty(trans.Text))
                                actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Keine Übersetzung vorhanden ({trans.Language.Text})", State = PublishState.Fail, Item = para, Module = mod });
                        } else {
                            if(!showOnlyErrors)
                            {
                               if(para.Text.Any(s => string.IsNullOrEmpty(s.Text)))
                                {
                                    actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Text nicht in allen Sprachen übersetzt", State = PublishState.Warning, Item = para, Module = mod });
                                }
                            }
                        }
                    }

                    if(para.OverwriteValue)
                    {
                        switch(ptype.Type) {
                            case ParameterTypes.Text:
                                if((para.Value.Length*8) > ptype.SizeInBit) actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Wert benötigt mehr Speicher ({(para.Value.Length*8)}) als verfügbar ({ptype.SizeInBit}) ist", State = PublishState.Fail, Item = para, Module = mod });
                                break;

                            case ParameterTypes.Enum:
                                int paraval2;
                                if(!int.TryParse(para.Value, out paraval2)) actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = PublishState.Fail, Item = para, Module = mod });
                                else {
                                    if(!ptype.Enums.Any(e => e.Value == paraval2))
                                        actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Wert ({para.Value}) ist nicht als option in Enum vorhanden", State = PublishState.Fail, Item = para, Module = mod });
                                }
                                break;

                            case ParameterTypes.NumberUInt:
                            case ParameterTypes.NumberInt:
                                long paraval;
                                if(!long.TryParse(para.Value, out paraval)) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = PublishState.Fail, Item = para, Module = mod });
                                else {
                                    if(paraval > long.Parse(ptype.Max) || paraval < long.Parse(ptype.Min))
                                        actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) fällt nicht in Bereich {ptype.Min}-{ptype.Max}", State = PublishState.Fail, Item = para, Module = mod });
                                }
                                break;

                            case ParameterTypes.Float_DPT9:
                            case ParameterTypes.Float_IEEE_Single:
                            case ParameterTypes.Float_IEEE_Double:


                            case ParameterTypes.Picture:
                            case ParameterTypes.None:
                            case ParameterTypes.IpAddress:
                                break;
                                
                            case ParameterTypes.Color:
                                Regex reg = null;
                                switch(para.ParameterObject.ParameterTypeObject.UIHint)
                                {
                                    case "RGB":
                                    case "HSV":
                                        reg = new Regex("([0-9a-fA-F]{6,6})");
                                        break;
                                        
                                    case "RGBW":
                                        reg = new Regex("([0-9a-fA-F]{8,8})");
                                        break;
                                }
                                if(reg != null && !reg.IsMatch(para.Value))
                                    actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültiger Hexwert für {para.ParameterObject.ParameterTypeObject.UIHint}", State = PublishState.Fail, Item = para, Module = mod });
                                break;
                        }
                    }

                    if(para.OverwriteSuffix)
                    {
                        if(!string.IsNullOrEmpty(para.Suffix.Single(t => t.Language.CultureCode == defaultLang).Text))
                        {
                            if(para.Suffix.Any(s => string.IsNullOrEmpty(s.Text)))
                            {
                                actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Suffix nicht in allen Sprachen übersetzt", State = PublishState.Warning, Item = para, Module = mod });
                            }
                        }
                    }

                    
                }
            }
        
            foreach(ComObject com in vbase.ComObjects) {
                if(com.HasDpt && com.Type == null) actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Kein DataPointType angegeben", State = PublishState.Fail, Item = com, Module = mod });
                if(com.HasDpt && com.Type != null && com.Type.Number == "0") actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Angabe des DPT nur bei Refs", State = PublishState.Fail, Item = com, Module = mod });
                if(com.HasDpt && com.HasDpts && com.SubType == null) actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Kein DataPointSubType angegeben", State = PublishState.Fail, Item = com, Module = mod });
            
                //TODO auslagern in Funktion
                if(com.TranslationText) {
                    Translation trans = com.Text.Single(t => t.Language.CultureCode == defaultLang);
                    if(string.IsNullOrEmpty(trans.Text))
                        actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = PublishState.Fail, Item = com, Module = mod });
                } else {
                    if(!showOnlyErrors)
                    {
                        if(com.Text.Count == com.Text.Count(t => string.IsNullOrEmpty(t.Text)))
                                actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzungen für Text vorhanden", State = PublishState.Warning, Item = com, Module = mod });
                        else
                            foreach(Translation trans in com.Text)
                                if(string.IsNullOrEmpty(trans.Text))
                                    actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = PublishState.Warning, Item = com, Module = mod });
                    }
                }
                

                if(com.TranslationFunctionText) {
                    Translation trans = com.FunctionText.Single(t => t.Language.CultureCode == defaultLang);
                    if(string.IsNullOrEmpty(trans.Text))
                        actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = PublishState.Fail, Item = com, Module = mod });
                } else {
                    if(!showOnlyErrors)
                    {
                        if(com.FunctionText.Count == com.FunctionText.Count(t => string.IsNullOrEmpty(t.Text)))
                                actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzungen für FunktionsText vorhanden", State = PublishState.Warning, Item = com, Module = mod });
                        else
                            foreach(Translation trans in com.FunctionText)
                                if(string.IsNullOrEmpty(trans.Text))
                                    actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = PublishState.Warning, Item = com, Module = mod });
                    }
                }

                if(ver.IsComObjectRefAuto && com.UseTextParameter && com.ParameterRefObject == null)
                    actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Kein TextParameter angegeben", State = PublishState.Fail, Item = com, Module = mod });
            }

            foreach(ComObjectRef rcom in vbase.ComObjectRefs) {
                if(rcom.ComObjectObject == null) actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Kein KO-Ref angegeben", State = PublishState.Fail, Item = rcom, Module = mod });
                //if(rcom.HasDpts && rcom.Type == null && rcom.Name.ToLower() != "dummy") actions.Add(new PublishAction() { Text = $"    ComObject {rcom.Name}: Kein DataPointSubType angegeben", State = PublishState.Fail });

                if(rcom.OverwriteText) {
                    if(rcom.TranslationText) {
                        Translation trans = rcom.Text.Single(t => t.Language.CultureCode == defaultLang);
                        if(string.IsNullOrEmpty(trans.Text))
                            actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = PublishState.Fail, Item = rcom, Module = mod });
                    } else {
                        if(!showOnlyErrors)
                            foreach(Translation trans in rcom.Text)
                                if(string.IsNullOrEmpty(trans.Text))
                                    actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = PublishState.Warning, Item = rcom, Module = mod });
                    }
                }

                if(rcom.OverwriteFunctionText) {
                    if(rcom.TranslationFunctionText) {
                        Translation trans = rcom.FunctionText.Single(t => t.Language.CultureCode == defaultLang);
                        if(string.IsNullOrEmpty(trans.Text))
                            actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = PublishState.Fail, Item = rcom, Module = mod });
                    } else {
                        if(!showOnlyErrors)
                            foreach(Translation trans in rcom.FunctionText)
                                if(string.IsNullOrEmpty(trans.Text))
                                    actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = PublishState.Warning, Item = rcom, Module = mod });
                    }
                }

                if(!ver.IsComObjectRefAuto && rcom.UseTextParameter && rcom.ParameterRefObject == null)
                    actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Kein TextParameter angegeben", State = PublishState.Fail, Item = rcom, Module = mod });
            }
        
            //TODO check union size fits parameter+offset

            CheckDynamicItem(vbase.Dynamics[0], actions, ns, showOnlyErrors, mod);
        }

        private static void CheckLanguages(AppVersion vers,  ObservableCollection<PublishAction> actions, ModelGeneral general, List<Device> devices)
        {
            List<CatalogItem> toCheck = new List<CatalogItem>();
            CheckCatalog(vers, general.Catalog[0], devices, general, actions);
        }

        private static void CheckCatalog(AppVersion vers, CatalogItem item, List<Device> devices, ModelGeneral general,  ObservableCollection<PublishAction> actions)
        {
            foreach(CatalogItem citem in item.Items)
            {
                if(!citem.IsSection)
                {
                    Application app = general.Applications.Single(a => a.Versions.Any(v => v.Number == vers.Number));
                    if(devices != null && citem.Hardware.Apps.Contains(app))
                    {
                        foreach(Device dev in citem.Hardware.Devices.Where(d => devices.Contains(d)))
                        {
                            foreach(Language lang in vers.Languages)
                            {
                                if(!dev.Text.Any(l => l.Language.CultureCode == lang.CultureCode || string.IsNullOrEmpty(l.Text)))
                                    actions.Add(new PublishAction() { Text = $"Geräte: Text enthält nicht alle Sprachen der Applikation.", State = PublishState.Fail });
                                if(!dev.Description.Any(l => l.Language.CultureCode == lang.CultureCode || string.IsNullOrEmpty(l.Text)))
                                    actions.Add(new PublishAction() { Text = $"Geräte: Beschreibung enthält nicht alle Sprachen der Applikation.", State = PublishState.Fail });
                            }
                        }
                    }
                } else {
                    foreach(Language lang in vers.Languages)
                    {
                        if(!citem.Text.Any(l => l.Language.CultureCode == lang.CultureCode || string.IsNullOrEmpty(l.Text)))
                            actions.Add(new PublishAction() { Text = $"Katalog: Text enthält nicht alle Sprachen der Applikation.", State = PublishState.Fail });
                    }
                    CheckCatalog(vers, citem, devices, general, actions);
                }
            }
        }
        
        private static void CheckDynamicItem(Models.Dynamic.IDynItems item, ObservableCollection<Models.PublishAction> actions, int ns, bool showOnlyErrors, IVersionBase vbase)
        {
            switch(item)
            {
                case DynChannel dc:
                {
                    if(string.IsNullOrEmpty(dc.Number))
                        actions.Add(new PublishAction() { Text = $"    DynChannel {dc.Name} hat keine Nummer", State = PublishState.Fail});

                    if(dc.UseTextParameter && dc.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynChannel {dc.Name} wurde kein Text Parameter zugeordnet", State = PublishState.Fail});
                    break;
                }

                case DynParaBlock dpb:
                {
                    if(vbase is AppVersion av)
                    {
                        if(!av.IsPreETS4 && dpb.UseParameterRef)
                            actions.Add(new PublishAction() { Text = $"    DynParaBlock {dpb.Name} ParameterRef wird nur in ETS3 unterstützt. IsPreETS4 aktivieren", State = PublishState.Warning});
                    }
                    if(dpb.UseParameterRef && dpb.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynParaBlock {dpb.Name} wurde kein ParameterRef zugeordnet", State = PublishState.Fail});
                        
                    if(dpb.UseTextParameter && dpb.TextRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynParaBlock {dpb.Name} wurde kein TextParameterRef zugeordnet", State = PublishState.Fail});
                    break;
                }

                case DynModule dm:
                {
                    if(dm.ModuleObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynModule {dm.Name} wurde kein Module zugeordnet", State = PublishState.Fail});
                        
                    if(dm.Arguments.Any(a => string.IsNullOrEmpty(a.Value)))
                        actions.Add(new PublishAction() { Text = $"    DynModule {dm.Name} hat Argumente, die leer sind", State = PublishState.Fail});
                    if(dm.Arguments.Any(a => a.Argument.Type != ArgumentTypes.Numeric && a.UseAllocator))
                        actions.Add(new PublishAction() { Text = $"    DynModule {dm.Name} hat Argumente, die einen Allocator verwenden, aber nicht Numeric sind", State = PublishState.Fail});
                    break;
                }

                case IDynChoose dco:
                {
                    if(dco.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynChoose {dco.Name} wurde kein ParameterRef zugeordnet", State = PublishState.Fail});
                    break;
                }

                case IDynWhen dwh:
                {
                    if(string.IsNullOrEmpty(dwh.Condition) && !dwh.IsDefault)
                        actions.Add(new PublishAction() { Text = $"    DynWhen {dwh.Name} wurde keine Bedingung angegeben", State = PublishState.Fail});
                    
                    if(!showOnlyErrors && !string.IsNullOrEmpty(dwh.Condition) && dwh.IsDefault)
                        actions.Add(new PublishAction() { Text = $"    DynWhen {dwh.Name} ist Default, Bedingung wird ignoriert", State = PublishState.Warning});
                    break;
                }

                case DynParameter dpa:
                {
                    if(dpa.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynParameter {dpa.Name} wurde kein ParameterRef zugeordnet", State = PublishState.Fail, Item = dpa, Module = vbase });
                    if(dpa.HasHelptext && dpa.Helptext == null)
                        actions.Add(new PublishAction() { Text = $"    DynParameter {dpa.Name} wurde kein Hilfetext zugeordnet", State = PublishState.Fail, Item = dpa, Module = vbase });
                    break;
                }

                case DynComObject dco:
                {
                    if(dco.ComObjectRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynComObject {dco.Name} wurde kein ComObjectRef zugeordnet", State = PublishState.Fail});
                    break;
                }

                case DynSeparator dse:
                {
                    if(dse.UseTextParameter && dse.TextRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynSeparator {dse.Name} wurde kein ParameterRef zugeordnet", State = PublishState.Fail});
                    if(dse.Hint != SeparatorHint.None && ns < 14)
                        actions.Add(new PublishAction() { Text = $"    DynSeparator {dse.Name} UIHint wird erst ab NamespaceVersion 14 unterstützt", State = PublishState.Fail});
                    break;
                }

                case DynAssign das:
                {
                    if(das.TargetObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynAssign {das.Name} wurde kein Ziel-Parameter zugeordnet", State = PublishState.Fail});
                        
                    if(das.SourceObject == null && string.IsNullOrEmpty(das.Value))
                        actions.Add(new PublishAction() { Text = $"    DynAssign {das.Name} wurde kein Quell-Parameter/Wert zugeordnet", State = PublishState.Fail});
                    break;
                }

                default:
                    System.Diagnostics.Debug.WriteLine("Not checked DynElement: " + item.ToString());
                    break;
            }

            if(item.Items == null) return;
            foreach(IDynItems xitem in item.Items)
                CheckDynamicItem(xitem, actions, ns, showOnlyErrors, vbase);
        }

        public static string CheckImportVersion(string json, int version)
        {
            JObject gen = JObject.Parse(json);

            if(version < 1)
            {
                foreach(JObject app in gen["Applications"])
                {
                    foreach(JObject ver in app["Versions"])
                    {
                        ObservableCollection<Language> langs = ver["Languages"].ToObject<ObservableCollection<Language>>();

                        for(int i = 0; i < ver["Parameters"].Count(); i++)
                        {
                            string oldSuffix = "";
                            List<Translation> trans = new List<Translation>();
                            if(!string.IsNullOrEmpty(ver["Parameters"][i]["Suffix"].ToString()))
                            {
                                oldSuffix = ver["Parameters"][i]["Suffix"].ToString();
                            }
                            foreach(Language lang in langs)
                                trans.Add(new Translation(lang, oldSuffix));
                            ver["Parameters"][i]["Suffix"] = JValue.FromObject(trans);
                        }
                        
                        for(int i = 0; i < ver["ParameterRefs"].Count(); i++)
                        {
                            Parameter para = ver["ParameterRefs"][i].ToObject<Parameter>();
                            para.Suffix = new ObservableCollection<Translation>();
                            foreach(Language lang in langs)
                                para.Suffix.Add(new Translation(lang, ver["ParameterRefs"][i]["Suffix"].Value<string>()));
                            ver["ParameterRefs"][i] = JObject.FromObject(para);
                        }

                        foreach(JObject jmodule in ver["Modules"])
                        {
                            for(int i = 0; i < jmodule["Parameters"].Count(); i++)
                            {
                                Parameter para = jmodule["Parameters"][i].ToObject<Parameter>();
                                para.Suffix = new ObservableCollection<Translation>();
                                foreach(Language lang in langs)
                                    para.Suffix.Add(new Translation(lang, jmodule["Parameters"][i]["Suffix"].Value<string>()));
                                jmodule["Parameters"][i] = JObject.FromObject(para);
                            }
                            
                            for(int i = 0; i < jmodule["ParameterRefs"].Count(); i++)
                            {
                                Parameter para = jmodule["ParameterRefs"][i].ToObject<Parameter>();
                                para.Suffix = new ObservableCollection<Translation>();
                                foreach(Language lang in langs)
                                    para.Suffix.Add(new Translation(lang, jmodule["ParameterRefs"][i]["Suffix"].Value<string>()));
                                jmodule["ParameterRefs"][i] = JObject.FromObject(para);
                            }
                        }
                    }
                }
            }
            
            if(version < 2)
            {
                foreach(JObject app in gen["Applications"])
                {
                    List<AppVersionModel> newVers = new List<AppVersionModel>();
                    foreach(JObject ver in app["Versions"])
                    {
                        AppVersionModel model = new AppVersionModel();
                        model.Version = ver.ToString();
                        model.Number = (int)ver["Number"];
                        model.Name = ver["Name"].ToString();
                        newVers.Add(model);
                    }
                    app["Versions"] = JValue.FromObject(newVers);
                }
            }
            
            return gen.ToString();
        }
    }
}