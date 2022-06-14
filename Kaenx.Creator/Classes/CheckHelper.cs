using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;


namespace Kaenx.Creator.Classes {

    public class CheckHelper
    {
        
        public static void CheckThis(ModelGeneral General,
                                    List<Hardware> hardware,
                                    List<Device> devices,
                                    List<Application> apps,
                                    List<AppVersion> versions,
                                    ObservableCollection<PublishAction> actions)
        {
            if(apps.Count == 0) {
                actions.Add(new PublishAction() { Text = $"Es wurden keine Applikationen ausgewählt.", State = PublishState.Fail });
                return;
            }

            actions.Add(new PublishAction() { Text = "Starte Check" });
            actions.Add(new PublishAction() { Text = $"{devices.Count} Geräte - {hardware.Count} Hardware - {apps.Count} Applikationen - {versions.Count} Versionen" });

            if(General.Catalog[0].Items.Any(c => !c.IsSection ))
                actions.Add(new PublishAction() { Text = "Katalog muss mindestens eine Unterkategorie haben.", State = PublishState.Fail });

            if(General.ManufacturerId <= 0 || General.ManufacturerId > 0xFFFF)
                actions.Add(new PublishAction() { Text = $"Ungültige HerstellerId angegeben: {General.ManufacturerId:X4}", State = PublishState.Fail });

            #region Hardware Check
            actions.Add(new PublishAction() { Text = "Überprüfe Hardware" });
            Regex reg = new Regex("^([0-9a-zA-Z_-]|\\s)+$");
            List<string> serials = new List<string>();

            var check1 = General.Hardware.GroupBy(h => h.Name).Where(h => h.Count() > 1);
            foreach(var group in check1)
                actions.Add(new PublishAction() { Text = "Hardwarename '" + group.Key + "' wird von " + group.Count() + " Hardware verwendet", State = PublishState.Fail });

            check1 = General.Hardware.GroupBy(h => h.SerialNumber).Where(h => h.Count() > 1);
            foreach (var group in check1)
                actions.Add(new PublishAction() { Text = "Hardwareserial '" + group.Key + "' wird von " + group.Count() + " Hardware verwendet", State = PublishState.Fail });

            check1 = null;
            var check2 = General.Hardware.Where(h => h.Devices.Count == 0);
            foreach (var group in check2)
                actions.Add(new PublishAction() { Text = "Hardware '" + group.Name + "' hat keine Geräte zugeordnet", State = PublishState.Warning });

            check2 = General.Hardware.Where(h => h.HasApplicationProgram && h.Apps.Count == 0);
            foreach (var group in check2)
                actions.Add(new PublishAction() { Text = "Hardware '" + group.Name + "' hat keine Applikation zugeordnet", State = PublishState.Warning });

            check2 = General.Hardware.Where(h => !h.HasApplicationProgram && h.Apps.Count != 0);
            foreach (var group in check2)
                actions.Add(new PublishAction() { Text = "Hardware '" + group.Name + "' hat Applikation zugeordnet obwohl angegeben ist, dass keine benötigt wird", State = PublishState.Warning });

            check2 = General.Hardware.Where(h => !reg.IsMatch(h.Name));
            foreach (var group in check2)
                actions.Add(new PublishAction() { Text = "Hardware '" + group.Name + "' hat ungültige Zeichen im Namen", State = PublishState.Fail });
            check2 = null;
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

            int highestNS = 0;
            foreach(AppVersion vers in versions) {
                Application app = apps.Single(a => a.Versions.Contains(vers));
                actions.Add(new PublishAction() { Text = $"Prüfe Applikation '{app.NameText}' Version '{vers.NameText}'" });
                
                if (vers.NamespaceVersion > highestNS)
                    highestNS = vers.NamespaceVersion;

                if(vers.IsModulesActive && vers.NamespaceVersion < 20)
                    actions.Add(new PublishAction() { Text = $"Applikation '{app.NameText}': ModuleDefindes werden erst ab Namespace 20 unterstützt.", State = PublishState.Fail });

                if(app.Mask.Procedure != ProcedureTypes.Default && string.IsNullOrEmpty(vers.Procedure))
                    actions.Add(new PublishAction() { Text = $"Applikation '{app.NameText}': Version muss eine Ladeprozedur enthalten.", State = PublishState.Fail });


                foreach(ParameterType ptype in vers.ParameterTypes) {
                    int maxsize = (int)Math.Pow(2, ptype.SizeInBit);
        
                    if(ptype.UIHint == "CheckBox" && (ptype.Min != 0 || ptype.Max != 1 || ptype.SizeInBit != 1))
                        actions.Add(new PublishAction() { Text = $"    ParameterType Text {ptype.Name} ({ptype.UId}): Wenn UIHint Checkbox ist, ist Min=0 und Max=1 erforderlich, sowie Size=1", State = PublishState.Fail });
                            
                    switch(ptype.Type) {
                        case ParameterTypes.Text:
                            if(ptype.SizeInBit % 8 != 0)
                                actions.Add(new PublishAction() { Text = $"    ParameterType Text {ptype.Name} ({ptype.UId}): ist kein vielfaches von 8", State = PublishState.Warning });
                            break;

                        case ParameterTypes.Enum:
                            var x = ptype.Enums.GroupBy(e => e.Value);
                            foreach(var group in x.Where(g => g.Count() > 1))
                                actions.Add(new PublishAction() { Text = $"    ParameterType Enum {ptype.Name} ({ptype.UId}): Wert ({group.Key}) wird öfters verwendet", State = PublishState.Fail });
                            
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
                                    actions.Add(new PublishAction() { Text = $"    ParameterType Enum {ptype.Name} ({ptype.UId}): Wert ({penum.Value}) ist größer als maximaler Wert ({maxsize-1})", State = PublishState.Fail });

                                if(!penum.Translate) {
                                    Translation trans = penum.Text.Single(t => t.Language.CultureCode == vers.DefaultLanguage);
                                    if(string.IsNullOrEmpty(trans.Text))
                                        actions.Add(new PublishAction() { Text = $"    ParameterType Enum {penum.Name}/{ptype.Name} ({ptype.UId}): Keine Übersetzung vorhanden ({trans.Language.Text})", State = PublishState.Fail });
                                } else {
                                    foreach(Translation trans in penum.Text)
                                        if(string.IsNullOrEmpty(trans.Text))
                                            actions.Add(new PublishAction() { Text = $"    ParameterType Enum {penum.Name}/{ptype.Name} ({ptype.UId}): Keine Übersetzung vorhanden ({trans.Language.Text})", State = PublishState.Warning });
                                }
                            }
                            break;

                        case ParameterTypes.NumberUInt:
                            if(!ptype.IsSizeManual)
                            {
                                string bin = Convert.ToString((int)ptype.Max, 2);
                                ptype.SizeInBit = bin.Length;
                                maxsize = (int)Math.Pow(2, ptype.SizeInBit);
                            }
                            if(ptype.Min < 0) actions.Add(new PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Min kann nicht kleiner als 0 sein", State = PublishState.Fail });
                            if(ptype.Min > ptype.Max) actions.Add(new PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Min ({ptype.Min}) ist größer als Max ({ptype.Max})", State = PublishState.Fail });
                            if(ptype.Max >= maxsize) actions.Add(new PublishAction() { Text = $"    ParameterType UInt {ptype.Name} ({ptype.UId}): Max ({ptype.Max}) kann nicht größer als das Maximum ({maxsize-1}) sein", State = PublishState.Fail });
                            break;

                        case ParameterTypes.NumberInt:
                            if(!ptype.IsSizeManual)
                            {
                                int z = ((int)ptype.Min) * (-1);
                                if(z < (ptype.Max - 1)) z = (int)ptype.Max;
                                string y = z.ToString().Replace("-", "");
                                string bin = Convert.ToString(int.Parse(y), 2);
                                if(z == (ptype.Min * (-1))) bin += "1";
                                ptype.SizeInBit = bin.Length;
                                maxsize = (int)Math.Pow(2, ptype.SizeInBit);
                            }
                            if(ptype.Min > ptype.Max) actions.Add(new PublishAction() { Text = $"    ParameterType Int {ptype.Name} ({ptype.UId}): Min ({ptype.Min}) ist größer als Max ({ptype.Max})", State = PublishState.Fail });
                            if(ptype.Max > ((maxsize/2)-1)) actions.Add(new PublishAction() { Text = $"    ParameterType Int {ptype.Name} ({ptype.UId}): Max ({ptype.Max}) kann nicht größer als das Maximum ({(maxsize/2)-1}) sein", State = PublishState.Fail });
                            if(ptype.Min < ((maxsize/2)*(-1))) actions.Add(new PublishAction() { Text = $"    ParameterType Int {ptype.Name} ({ptype.UId}): Min ({ptype.Min}) kann nicht kleiner als das Minimum ({(maxsize/2)*(-1)}) sein", State = PublishState.Fail });
                            break;

                        case ParameterTypes.Float_DPT9:
                            break;

                        case ParameterTypes.Picture:
                            //actions.Add(new PublishAction() { Text = $"    ParameterTyp Picture für {ptype.Name} ({ptype.UId}) wird nicht exportiert", State = PublishState.Warning });
                            break;

                        case ParameterTypes.None:
                            break;

                        case ParameterTypes.IpAddress:
                            actions.Add(new PublishAction() { Text = $"    ParameterTyp IpAddress für {ptype.Name} ({ptype.UId}) wird nicht exportiert", State = PublishState.Warning });
                            break;

                        default:
                            actions.Add(new PublishAction() { Text = $"    Unbekannter ParameterTyp für {ptype.Name} ({ptype.UId})", State = PublishState.Fail });
                            break;
                    }
                }

                CheckVersion(vers, actions, vers.DefaultLanguage);


                foreach(Module mod in vers.Modules)
                {
                    actions.Add(new PublishAction() { Text = $"Prüfe Module '{mod.Name}'" });
                    CheckVersion(mod, actions, vers.DefaultLanguage);
                }
            }
            #endregion

            //if(EtsVersions.Single(v => v.Number == highestNS).IsEnabled == false)
            //        actions.Add(new PublishAction() { Text = $"Mindestens eine Applikation verwendet einen Namespace ({highestNS}), der auf diesem Rechner nicht erstellt werden kann.", State = PublishState.Fail });
        }


        private static void CheckVersion(IVersionBase vbase, ObservableCollection<PublishAction> actions, string defaultLang)
        {
            foreach(Parameter para in vbase.Parameters) {
                if(para.ParameterTypeObject == null) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Kein ParameterTyp ausgewählt", State = PublishState.Fail });
                else {
                    switch(para.ParameterTypeObject.Type) {
                        case ParameterTypes.Text:
                            if((para.Value.Length*8) > para.ParameterTypeObject.SizeInBit) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert benötigt mehr Speicher ({(para.Value.Length*8)}) als verfügbar ({para.ParameterTypeObject.SizeInBit}) ist", State = PublishState.Fail });
                            break;

                        case ParameterTypes.Enum:
                            int paraval2;
                            if(!int.TryParse(para.Value, out paraval2)) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = PublishState.Fail });
                            else {
                                if(!para.ParameterTypeObject.Enums.Any(e => e.Value == paraval2))
                                    actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist nicht als option in Enum vorhanden", State = PublishState.Fail });
                            }
                            break;

                        case ParameterTypes.NumberUInt:
                        case ParameterTypes.NumberInt:
                            int paraval;
                            if(!int.TryParse(para.Value, out paraval)) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = PublishState.Fail });
                            else {
                                if(paraval > para.ParameterTypeObject.Max || paraval < para.ParameterTypeObject.Min)
                                    actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) fällt nicht in Bereich {para.ParameterTypeObject.Min}-{para.ParameterTypeObject.Max}", State = PublishState.Fail });
                            }
                            break;

                        case ParameterTypes.Float_DPT9:
                        case ParameterTypes.Float_IEEE_Single:
                        case ParameterTypes.Float_IEEE_Double:


                        case ParameterTypes.Picture:
                        case ParameterTypes.None:
                        case ParameterTypes.IpAddress:
                            break;
                    }
                }
                

                if(para.TranslationText) {
                    Translation trans = para.Text.Single(t => t.Language.CultureCode == defaultLang);
                    if(string.IsNullOrEmpty(trans.Text))
                        actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Keine Übersetzung vorhanden ({trans.Language.Text})", State = PublishState.Fail });
                } else {
                    foreach(Translation trans in para.Text)
                        if(string.IsNullOrEmpty(trans.Text))
                            actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Keine Übersetzung vorhanden ({trans.Language.Text})", State = PublishState.Warning });
                }

                if(!para.IsInUnion) {
                    switch(para.SavePath) {
                        case ParamSave.Memory:
                            if(para.MemoryObject == null)
                                actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Kein Speichersegment ausgewählt", State = PublishState.Fail });
                            else {
                                if(!para.MemoryObject.IsAutoPara && para.Offset == -1) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name}: Kein Offset angegeben", State = PublishState.Fail });
                                if(!para.MemoryObject.IsAutoPara && para.OffsetBit == -1) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name}: Kein Bit Offset angegeben", State = PublishState.Fail });

                            }
                            if(para.OffsetBit > 7) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): BitOffset größer als 7 und somit obsolet", State = PublishState.Fail });
                                break;
                    }
                }
            }
        
            foreach(ParameterRef para in vbase.ParameterRefs) {
                if(para.ParameterObject == null) actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Kein Parameter ausgewählt", State = PublishState.Fail });
                else {
                    if(para.ParameterObject.ParameterTypeObject == null || string.IsNullOrEmpty(para.Value))
                        continue;
                    
                    //TODO check value overwrite
                    ParameterType ptype = para.ParameterObject.ParameterTypeObject;

                    switch(ptype.Type) {
                        case ParameterTypes.Text:
                            if((para.Value.Length*8) > ptype.SizeInBit) actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Wert benötigt mehr Speicher ({(para.Value.Length*8)}) als verfügbar ({ptype.SizeInBit}) ist", State = PublishState.Fail });
                            break;

                        case ParameterTypes.Enum:
                            int paraval2;
                            if(!int.TryParse(para.Value, out paraval2)) actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = PublishState.Fail });
                            else {
                                if(!ptype.Enums.Any(e => e.Value == paraval2))
                                    actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Wert ({para.Value}) ist nicht als option in Enum vorhanden", State = PublishState.Fail });
                            }
                            break;

                        case ParameterTypes.NumberUInt:
                        case ParameterTypes.NumberInt:
                            int paraval;
                            if(!int.TryParse(para.Value, out paraval)) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = PublishState.Fail });
                            else {
                                if(paraval > ptype.Max || paraval < ptype.Min)
                                    actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) fällt nicht in Bereich {ptype.Min}-{ptype.Max}", State = PublishState.Fail });
                            }
                            break;

                        case ParameterTypes.Float_DPT9:
                        case ParameterTypes.Float_IEEE_Single:
                        case ParameterTypes.Float_IEEE_Double:


                        case ParameterTypes.Picture:
                        case ParameterTypes.None:
                        case ParameterTypes.IpAddress:
                            break;
                    }
                }
            }
        
            foreach(ComObject com in vbase.ComObjects) {
                if(com.HasDpt && com.Type == null) actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Kein DataPointType angegeben", State = PublishState.Fail });
                if(com.HasDpt && com.Type != null && com.Type.Number == "0") actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Angabe des DPT nur bei Refs", State = PublishState.Fail });
                if(com.HasDpt && com.HasDpts && com.SubType == null) actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Kein DataPointSubType angegeben", State = PublishState.Fail });
            
                //TODO auslagern in Funktion
                if(com.TranslationText) {
                    Translation trans = com.Text.Single(t => t.Language.CultureCode == defaultLang);
                    if(string.IsNullOrEmpty(trans.Text))
                        actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = PublishState.Fail });
                } else {
                    foreach(Translation trans in com.Text)
                        if(string.IsNullOrEmpty(trans.Text))
                            actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = PublishState.Warning });
                }

                if(com.TranslationFunctionText) {
                    Translation trans = com.FunctionText.Single(t => t.Language.CultureCode == defaultLang);
                    if(string.IsNullOrEmpty(trans.Text))
                        actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = PublishState.Fail });
                } else {
                    foreach(Translation trans in com.FunctionText)
                        if(string.IsNullOrEmpty(trans.Text))
                            actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = PublishState.Warning });
                }

                if(com.UseTextParameter && com.ParameterRefObject == null)
                    actions.Add(new PublishAction() { Text = $"    ComObjectRef {com.Name} ({com.UId}): Kein TextParameter angegeben", State = PublishState.Fail });
            }

            foreach(ComObjectRef rcom in vbase.ComObjectRefs) {
                if(rcom.ComObjectObject == null) actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Kein KO-Ref angegeben", State = PublishState.Fail });
                //if(rcom.HasDpts && rcom.Type == null && rcom.Name.ToLower() != "dummy") actions.Add(new PublishAction() { Text = $"    ComObject {rcom.Name}: Kein DataPointSubType angegeben", State = PublishState.Fail });

                if(rcom.OverwriteText) {
                    if(rcom.TranslationText) {
                        Translation trans = rcom.Text.Single(t => t.Language.CultureCode == defaultLang);
                        if(string.IsNullOrEmpty(trans.Text))
                            actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = PublishState.Fail });
                    } else {
                        foreach(Translation trans in rcom.Text)
                            if(string.IsNullOrEmpty(trans.Text))
                                actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für Text vorhanden ({trans.Language.Text})", State = PublishState.Warning });
                    }
                }

                if(rcom.OverwriteFunctionText) {
                    if(rcom.TranslationFunctionText) {
                        Translation trans = rcom.FunctionText.Single(t => t.Language.CultureCode == defaultLang);
                        if(string.IsNullOrEmpty(trans.Text))
                            actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = PublishState.Fail });
                    } else {
                        foreach(Translation trans in rcom.FunctionText)
                            if(string.IsNullOrEmpty(trans.Text))
                                actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Keine Übersetzung für FunktionsText vorhanden ({trans.Language.Text})", State = PublishState.Warning });
                    }
                }
            }
        
            
            //TODO check Modules also
            //check for Argument exist

            //TODO check union size fits parameter+offset

            //TODO check dynamic
            // - separator Text required

            //TODO check ParameterBlockRename only in vers 11


            CheckDynamicItem(vbase.Dynamics[0], actions);
        }

        
        private static void CheckDynamicItem(Models.Dynamic.IDynItems item, ObservableCollection<Models.PublishAction> actions)
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
                    break;
                }

                case DynChoose dco:
                {
                    if(dco.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynChoose {dco.Name} wurde kein ParameterRef zugeordnet", State = PublishState.Fail});
                    break;
                }

                case DynWhen dwh:
                {
                    if(string.IsNullOrEmpty(dwh.Condition) && !dwh.IsDefault)
                        actions.Add(new PublishAction() { Text = $"    DynWhen {dwh.Name} wurde keine Bedingung angegeben", State = PublishState.Fail});
                    
                    if(!string.IsNullOrEmpty(dwh.Condition) && dwh.IsDefault)
                        actions.Add(new PublishAction() { Text = $"    DynWhen {dwh.Name} ist Default, Bedingung wird ignoriert", State = PublishState.Warning});
                    break;
                }

                case DynParameter dpa:
                {
                    if(dpa.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynParameter {dpa.Name} wurde kein ParameterRef zugeordnet", State = PublishState.Fail});
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
                    //TODO add properties to DynSeperator UseTextParameter, ParameterRefObject
                    //if(dse.para == null)
                    //    actions.Add(new PublishAction() { Text = $"    DynComObject {dco.Name} wurde kein ComObjectRef zugeordnet", State = PublishState.Warning});
                    break;
                }

                case DynAssign das:
                {
                    if(das.TargetObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynAssign {das.Name} wurde kein Ziel-Parameter zugeordnet", State = PublishState.Fail});
                        
                    if(string.IsNullOrEmpty(das.Value))
                        actions.Add(new PublishAction() { Text = $"    DynAssign {das.Name} wurde kein Wert zugeordnet", State = PublishState.Fail});
                    
                    if(das.SourceObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynAssign {das.Name} wurde kein Quell-Parameter zugeordnet", State = PublishState.Fail});
                    break;
                }

                default:
                    System.Diagnostics.Debug.WriteLine("Not checked DynElement: " + item.ToString());
                    break;
            }

            if(item.Items == null) return;
            foreach(IDynItems xitem in item.Items)
                CheckDynamicItem(xitem, actions);
        }


    }
}