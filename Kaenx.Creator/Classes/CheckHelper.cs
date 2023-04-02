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
                actions.Add(new PublishAction() { Text = Properties.Messages.check_no_app, State = PublishState.Fail });
                return;
            }

            actions.Add(new PublishAction() { Text = Properties.Messages.check_start });
            actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_start_count, devices?.Count, hardware?.Count, apps.Count, versions.Count) });

            if(General != null)
            {
                if(General.Catalog[0].Items.Any(c => !c.IsSection ))
                    actions.Add(new PublishAction() { Text = Properties.Messages.check_cat_sub, State = PublishState.Fail });

                foreach(CatalogItem citem in General.Catalog[0].Items)
                    CheckCatalogItem(citem, actions);

                if(General.ManufacturerId <= 0 || General.ManufacturerId > 0xFFFF)
                    actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_manu_id, General.ManufacturerId.ToString("X4")), State = PublishState.Fail });


                #region Hardware Check
                actions.Add(new PublishAction() { Text = Properties.Messages.check_hard });
                List<string> serials = new List<string>();

                var check1 = General.Hardware.GroupBy(h => h.Name).Where(h => h.Count() > 1);
                foreach(var group in check1)
                    actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_hard_duplicate_name, group.Key, group.Count()), State = PublishState.Fail });

                check1 = General.Hardware.GroupBy(h => h.SerialNumber).Where(h => h.Count() > 1);
                foreach (var group in check1)
                    actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_hard_duplicate_serial, group.Key, group.Count()), State = PublishState.Fail });

                IEnumerable<Hardware> check2;
                if(!showOnlyErrors)
                {
                    check1 = null;
                    check2 = General.Hardware.Where(h => h.Devices.Count == 0);
                    foreach (var group in check2)
                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_hard_no_devices, group.Name), State = PublishState.Warning });

                    check2 = General.Hardware.Where(h => h.HasApplicationProgram && h.Apps.Count == 0);
                    foreach (var group in check2)
                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_hard_no_apps, group.Name), State = PublishState.Warning });

                    check2 = General.Hardware.Where(h => !h.HasApplicationProgram && h.Apps.Count != 0);
                    foreach (var group in check2)
                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_hard_apps, group.Name), State = PublishState.Warning });
                }
                foreach(Hardware hard in hardware)
                {
                    if(!hard.HasIndividualAddress)
                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_hard_no_physicaladdress, hard.Name), State = PublishState.Fail });
                    if(!hard.HasApplicationProgram)
                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_hard_no_app, hard.Name), State = PublishState.Fail });
                    if(!hard.HasApplicationProgram && hard.HasApplicationProgram2)
                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_hard_no_app2, hard.Name), State = PublishState.Fail });
                }
                #endregion


                #region Applikation Check
                actions.Add(new PublishAction() { Text = Properties.Messages.check_app });

                var check3 = General.Applications.GroupBy(h => h.Name).Where(h => h.Count() > 1);
                foreach (var group in check3)
                    actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_app_duplicate_name, group.Key, group.Count()), State = PublishState.Fail });

                check3 = null;
                var check4 = General.Applications.GroupBy(h => h.Number).Where(h => h.Count() > 1);
                foreach (var group in check4)
                    actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_app_duplicate_number, group.Key.ToString("X4"), group.Count()), State = PublishState.Fail });

                check4 = null;
                foreach(Application app in General.Applications)
                {
                    var check5 = app.Versions.GroupBy(v => v.Number).Where(l => l.Count() > 1);
                    foreach (var group in check5)
                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_app_duplicate_version, app.NameText, Math.Floor(group.Key / 16.0), group.Key % 16, group.Count()), State = PublishState.Fail });
                
                    if(General.IsOpenKnx)
                    {
                        if(app.Number > 0xFF)
                            actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_app_openknx, app.NameText), State = PublishState.Fail });
                    }
                }
                #endregion

                #region Baggages Check

                foreach(Baggage bag in General.Baggages)
                {
                    if(General.IsOpenKnx)
                    {
                        if(bag.TargetPath != "root" && bag.TargetPath != "openknxid" && bag.TargetPath != "openknxapp")
                            actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_baggage_no_target, bag.Name), State = PublishState.Fail });
                    }
                }

                #endregion
            }

            //TODO check hardware/device/app

            foreach(AppVersionModel model in versions)
            {
                AppVersion version = model.Model != null ? model.Model : AutoHelper.GetAppVersion(General, model);
                Application app = apps.Single(a => a.Versions.Any(v => v.Name == version.Name && v.Number == version.Number));
                actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_ver, app.NameText, version.NameText) });
                CheckVersion(General, app, version, devices, actions, showOnlyErrors);
            }
            
            actions.Add(new PublishAction() { Text = Properties.Messages.check_end });
        }


        public static void CheckVersion(
            ModelGeneral General, 
            Application app,
            AppVersion vers, 
            List<Device> devices,
            ObservableCollection<PublishAction> actions, 
            bool showOnlyErrors = false)
        {
            if(string.IsNullOrEmpty(app.Mask.MediumTypes))
            {
                actions.Add(new PublishAction() { Text = Properties.Messages.check_ver_mediumtypes, State = PublishState.Fail });
            }
            
            if(vers.IsModulesActive && vers.NamespaceVersion < 20)
                actions.Add(new PublishAction() { Text = Properties.Messages.check_ver_modules, State = PublishState.Fail });

            if(vers.IsMessagesActive && vers.NamespaceVersion < 14)
                actions.Add(new PublishAction() { Text = Properties.Messages.check_ver_messages, State = PublishState.Fail });

            if(app.Mask.Procedure != ProcedureTypes.Default && string.IsNullOrEmpty(vers.Procedure))
                actions.Add(new PublishAction() { Text = Properties.Messages.check_ver_loadprod, State = PublishState.Fail });

            if(vers.IsHelpActive && vers.NamespaceVersion < 14)
                actions.Add(new PublishAction() { Text = Properties.Messages.check_ver_helptext, State = PublishState.Fail });
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
                                actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_ver_loadprod_size, xele.Name.LocalName), State = PublishState.Warning });
                        }
                    }
                    if(xele.Name.LocalName == "LdCtrlWriteRelMem")
                    {
                        if(xele.Attribute("ObjIdx").Value == "4")
                        {
                            int memsize = int.Parse(xele.Attribute("Size")?.Value ?? "0");
                            if(memsize != vers.Memories[0].Size)
                                actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_ver_loadprod_size, xele.Name.LocalName), State = PublishState.Warning });
                        }
                    }
                }
            }

            foreach(ParameterType ptype in vers.ParameterTypes) {
                long maxsize = (long)Math.Pow(2, ptype.SizeInBit);
    
                if(ptype.UIHint == "CheckBox" && (ptype.Min != "0" || ptype.Max != "1"))
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_checkbox, ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        
                switch(ptype.Type) {
                    case ParameterTypes.Text:
                        if(ptype.SizeInBit % 8 != 0)
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_text, ptype.Name, ptype.UId, ptype.SizeInBit), State = PublishState.Fail, Item = ptype });
                        break;

                    case ParameterTypes.Enum:
                        var x = ptype.Enums.GroupBy(e => e.Value);
                        foreach(var group in x.Where(g => g.Count() > 1))
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_enum, ptype.Name, ptype.UId, group.Key), State = PublishState.Fail, Item = ptype });
                        
                        if(!ptype.IsSizeManual)
                        {
                            int maxValue = -1;
                            foreach(ParameterTypeEnum penum in ptype.Enums)
                                if(penum.Value > maxValue)
                                    maxValue = penum.Value;
                            string bin = Convert.ToString(maxValue, 2);
                            ptype.SizeInBit = bin.Length;
                            maxsize = (long)Math.Pow(2, ptype.SizeInBit);
                        }

                        foreach(ParameterTypeEnum penum in ptype.Enums){
                            if(penum.Value >= maxsize)
                                actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_enum2, ptype.Name, ptype.UId, penum.Value, maxsize-1), State = PublishState.Fail, Item = ptype });

                            if(!penum.Translate) {
                                Translation trans = penum.Text.Single(t => t.Language.CultureCode == vers.DefaultLanguage);
                                if(string.IsNullOrEmpty(trans.Text))
                                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_enum_translation, penum.Name, ptype.Name, ptype.UId, trans.Language.Text), State = PublishState.Fail, Item = ptype });
                            } else {
                                if(!showOnlyErrors)
                                    foreach(Translation trans in penum.Text)
                                        if(string.IsNullOrEmpty(trans.Text))
                                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_enum_translation, penum.Name, ptype.Name, ptype.UId, trans.Language.Text), State = PublishState.Fail, Item = ptype });
                            }

                            if(penum.UseIcon && penum.IconObject == null)
                                actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_enum_icon, penum.Name, ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });

                        }
                        break;

                    case ParameterTypes.NumberUInt:
                    {
                        if(ptype.UIHint == "ProgressBar" && vers.NamespaceVersion < 20)
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_progbar, "UInt", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });

                        long min, max, temp;
                        if(!long.TryParse(ptype.Max, out max)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_max, "UInt", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.Min, out min)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_min, "UInt", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.DisplayOffset, out temp)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_offset, "UInt", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.DisplayFactor, out temp)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_factor, "UInt", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });

                        if(!ptype.IsSizeManual)
                        {
                            string bin = Convert.ToString(max, 2);
                            ptype.SizeInBit = bin.Length;
                            maxsize = (long)Math.Pow(2, ptype.SizeInBit);
                        }
                        if(min < 0) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_uint_min2, ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(min > max) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_minmax, "UInt", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(max >= maxsize) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_max2, "UInt", ptype.Name, ptype.UId, maxsize-1), State = PublishState.Fail, Item = ptype });
                        break;
                    }

                    case ParameterTypes.NumberInt:
                    {
                        if(ptype.UIHint == "Progressbar" && vers.NamespaceVersion < 20)
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_progbar, "UInt", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });

                        long min, max, temp;
                        if(!long.TryParse(ptype.Max, out max)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_min, "Int", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.Min, out min)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_max, "Int", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.DisplayOffset, out temp)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_offset, "Int", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.DisplayFactor, out temp)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_factor, "Int", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });

                        maxsize = (long)Math.Ceiling(maxsize / 2.0);
                        if(!ptype.IsSizeManual)
                        {
                            long z = min * (-1);
                            if(z < (max - 1)) z = max;
                            string y = z.ToString().Replace("-", "");
                            string bin = Convert.ToString(long.Parse(y), 2);
                            if(z == (min * (-1))) bin += "1";
                            if(!z.ToString().StartsWith("-")) bin = "1" + bin;
                            ptype.SizeInBit = bin.Length;
                            maxsize = (long)Math.Pow(2, ptype.SizeInBit);
                        }
                        if(min > max) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_minmax, "Int", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(max > ((maxsize)-1)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_max2, "Int", ptype.Name, ptype.UId, (maxsize/2)-1), State = PublishState.Fail, Item = ptype });
                        if(min < ((maxsize)*(-1))) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_min2, ptype.Name, ptype.UId, (maxsize/2)*(-1)), State = PublishState.Fail, Item = ptype });
                        break;
                    }

                    case ParameterTypes.Float_DPT9:
                    case ParameterTypes.Float_IEEE_Single:
                    case ParameterTypes.Float_IEEE_Double:
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_float, ptype.Name, ptype.UId), State = PublishState.Warning, Item = ptype });
                        break;

                    case ParameterTypes.Picture:
                        if(ptype.BaggageObject == null)
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_picture, ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        break;

                    case ParameterTypes.None:
                        if(!vers.IsPreETS4)
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_none, ptype.Name, ptype.UId), State = PublishState.Warning, Item = ptype });
                        break;

                    case ParameterTypes.Color:
                        if(ptype.UIHint != "RGB" && ptype.UIHint != "RGBW" && ptype.UIHint != "HSV")
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_color1, ptype.Name, ptype.UId, ptype.UIHint), State = PublishState.Fail, Item = ptype });
                        if(ptype.UIHint == "RGBW" && vers.NamespaceVersion < 20)
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_color2, ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        break;

                    case ParameterTypes.IpAddress:
                        //actions.Add(new PublishAction() { Text = $"    ParameterTyp IpAddress für {ptype.Name} ({ptype.UId}) wird nicht exportiert", State = PublishState.Warning, Item = ptype });
                        ptype.SizeInBit = 32;
                        
                        if(ptype.UIHint != "HostAddress" && ptype.UIHint != "GatewayAddress" && ptype.UIHint != "UnicastAddress" && ptype.UIHint != "BroadcastAddress" && ptype.UIHint != "MulticastAddress" && ptype.UIHint != "SubnetMask")
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_ip1, ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                               
                        if(ptype.Increment != "None" && ptype.Increment != "IPv4" && ptype.Increment != "IPv6")
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_ip2, ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        break;

                    case ParameterTypes.RawData:
                    {
                        //long max;
                        //if(!long.TryParse(ptype.Max, out max)) actions.Add(new PublishAction() { Text = $"    ParameterType RawData {ptype.Name} ({ptype.UId}): Maximum ist keine Ganzzahl", State = PublishState.Fail, Item = ptype });
                        //if(max < 1) actions.Add(new PublishAction() { Text = $"    ParameterType RawData {ptype.Name} ({ptype.UId}): Maximum muss 1 oder größer sein", State = PublishState.Fail, Item = ptype });
                        //if(max > 1048572) actions.Add(new PublishAction() { Text = $"    ParameterType RawData {ptype.Name} ({ptype.UId}): Maximum kann nicht größer als 1048572 sein", State = PublishState.Fail, Item = ptype });
                        break;
                    }

                    case ParameterTypes.Date:
                    {
                        if(string.IsNullOrEmpty(ptype.UIHint))actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_date, ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        break;
                    }

                    default:
                        actions.Add(new PublishAction() { Text = $"    Unbekannter ParameterTyp für {ptype.Name} ({ptype.UId})", State = PublishState.Fail, Item = ptype });
                        break;
                }
            }

            //if(app.Mask.Memory == MemoryTypes.Relative && vers.Memories.Count > 1)
            //    actions.Add(new PublishAction() { Text = $"Die Maskenversion unterstützt nur einen Speicher", State = PublishState.Fail });

            CheckVersion(vers, vers, actions, vers.DefaultLanguage, vers.NamespaceVersion, showOnlyErrors);
            if(General != null)
                CheckLanguages(vers, actions, General, devices);

            if (app.Mask.Procedure != ProcedureTypes.Default)
            {
                XElement temp = XElement.Parse(vers.Procedure);
                temp.Attributes().Where((x) => x.IsNamespaceDeclaration).Remove();
                foreach (XElement xele in temp.Descendants())
                {
                    if (xele.Name.LocalName == "OnError")
                    {
                        if (!vers.IsMessagesActive)
                        {
                            actions.Add(new PublishAction() { Text = $"Ladeprozedur: Es werden Meldungen verwendet, obwohl diese in der Applikation nicht aktiviert sind.", State = PublishState.Fail });
                            return;
                        }

                        int id = -1;
                        if (!int.TryParse(xele.Attribute("MessageRef").Value, out id))
                            actions.Add(new PublishAction() { Text = $"Ladeprozedur: MessageRef ist kein Integer.", State = PublishState.Fail });
                        if (id != -1)
                        {
                            if (!vers.Messages.Any(m => m.UId == id))
                                actions.Add(new PublishAction() { Text = $"Ladeprozedur: MessageRef zeigt auf nicht vorhandene Meldung ({id}).", State = PublishState.Fail });
                        }
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

            if(mod != null)
            {
                if(mod.ParameterBaseOffset.Type != ArgumentTypes.Numeric)
                    actions.Add(new PublishAction() { Text = $"    Modul ({mod.Name}): ParameterBaseOffset Argument ist nicht vom Typ Numeric", State = PublishState.Fail });
                if(mod.ComObjectBaseNumber.Type != ArgumentTypes.Numeric)
                    actions.Add(new PublishAction() { Text = $"    Modul ({mod.Name}): ComObjectBaseNumber Argument ist nicht vom Typ Numeric", State = PublishState.Fail });
            }

            if(vbase.Dynamics[0].Items.Count == 0)
            {
                string name = "AppVersion";
                if(mod != null)
                    name = $"Modul {mod.Name}";
                actions.Add(new PublishAction() { Text = $"    {name}: Dynamic enthält keine Elemente", State = PublishState.Fail });
            }

            foreach(Parameter para in vbase.Parameters) {
                if(para.ParameterTypeObject == null) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Kein ParameterTyp ausgewählt", State = PublishState.Fail, Item = para, Module = mod });
                else {
                    switch(para.ParameterTypeObject.Type) {
                        case ParameterTypes.Text:
                            if((para.Value.Length*8) > para.ParameterTypeObject.SizeInBit) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert benötigt mehr Speicher ({(para.Value.Length*8)}) als verfügbar ({para.ParameterTypeObject.SizeInBit}) ist", State = PublishState.Fail, Item = para, Module = mod });
                            break;

                        case ParameterTypes.Enum:
                            int paraval2;
                            if(!int.TryParse(para.Value, out paraval2)) 
                                actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = PublishState.Fail, Item = para, Module = mod });
                            else {
                                if(!para.ParameterTypeObject.Enums.Any(e => e.Value == paraval2))
                                    actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist nicht als option in Enum vorhanden", State = PublishState.Fail, Item = para, Module = mod });
                            }
                            break;

                        case ParameterTypes.NumberUInt:
                        case ParameterTypes.NumberInt:
                            long paraval;
                            if(!long.TryParse(para.Value, out paraval)) 
                                actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = PublishState.Fail, Item = para, Module = mod });
                            else {
                                if(paraval > long.Parse(para.ParameterTypeObject.Max) || paraval < long.Parse(para.ParameterTypeObject.Min))
                                    actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) fällt nicht in Bereich {para.ParameterTypeObject.Min} bis {para.ParameterTypeObject.Max}", State = PublishState.Fail, Item = para, Module = mod });
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
                        {
                            Regex reg = null;
                            string def = "";
                            switch(para.ParameterTypeObject.UIHint)
                            {
                                case "RGB":
                                    reg = new Regex("#([0-9a-fA-F]{6,6})");
                                    def = "#RRGGBB";
                                    break;

                                case "HSV":
                                    reg = new Regex("#([0-9a-fA-F]{6,6})");
                                    def = "#HHSSVV";
                                    break;
                                    
                                case "RGBW":
                                    reg = new Regex("#([0-9a-fA-F]{8,8})");
                                    def = "#RRGGBBWW";
                                    break;
                            }
                            if(reg != null && !reg.IsMatch(para.Value))
                                actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültiger Hexwert für {para.ParameterTypeObject.UIHint} ({def})", State = PublishState.Fail, Item = para, Module = mod });
                            break;
                        }

                        case ParameterTypes.RawData:
                        {
                            if(para.Value.Length % 2 != 0) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert hat eine ungerade Anzahl an Zeichen", State = PublishState.Fail, Item = para, Module = mod });
                            else if((para.Value.Length / 2) > long.Parse(para.ParameterTypeObject.Max)) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert benötigt mehr Speicher ({(para.Value.Length / 2)}) als verfügbar ({para.ParameterTypeObject.Max}) ist", State = PublishState.Fail, Item = para, Module = mod });
                            Regex reg = new Regex("^([0-9A-Fa-f])+$");
                            if(!reg.IsMatch(para.Value)) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültiger Hexwert für RawData", State = PublishState.Fail, Item = para, Module = mod });
                            break;
                        }

                        case ParameterTypes.Date:
                        {
                            Regex reg = new Regex("([0-9]{4}-[0-9]{2}-[0-9]{2})");
                            if(!reg.IsMatch(para.Value))  actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Wert ({para.Value}) muss das Format 'YYYY-MM-DD' haben", State = PublishState.Fail, Item = para, Module = mod });
                            break;                        
                        }
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
                } else {
                    if(para.UnionObject == null) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Es wurde kein Union ausgewählt", State = PublishState.Fail, Item = para, Module = mod });
                }

                if(para.Suffix.Any(t => t.Text.Length > 20)) actions.Add(new PublishAction() { Text = $"    Parameter {para.Name} ({para.UId}): Suffix ist länger als 20 Zeichen", State = PublishState.Fail, Item = para, Module = mod });
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
                                if(!long.TryParse(para.Value, out paraval)) actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Wert ({para.Value}) ist keine gültige Zahl", State = PublishState.Fail, Item = para, Module = mod });
                                else {
                                    if(paraval > long.Parse(ptype.Max) || paraval < long.Parse(ptype.Min))
                                        actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Wert ({para.Value}) fällt nicht in Bereich {ptype.Min} bis {ptype.Max}", State = PublishState.Fail, Item = para, Module = mod });
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
                if(para.Suffix.Any(t => t.Text.Length > 20)) actions.Add(new PublishAction() { Text = $"    ParameterRef {para.Name} ({para.UId}): Suffix ist länger als 20 Zeichen", State = PublishState.Fail, Item = para, Module = mod });
            }
        
            
            var x = vbase.ComObjects.GroupBy((c) => c.Number);
            if(x.Any((c) => c.Count() > 1))
            {
                actions.Add(new PublishAction() { Text = $"    Kommunikationsobjekt-Nummern sind nicht eindeutig. IDs werden aufsteigend vergeben.", State = PublishState.Warning });
            } else {
                foreach(ComObject com in vbase.ComObjects)
                {
                    com.Id = com.Number;
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

                if(ver.IsComObjectRefAuto && com.UseTextParameter)
                {
                    if(com.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): Kein TextParameter angegeben", State = PublishState.Fail, Item = com, Module = mod });
                    if(com.Text.Any(t => !t.Text.Contains("{{0")))
                        actions.Add(new PublishAction() { Text = $"    ComObject {com.Name} ({com.UId}): TextParameter angegeben, aber kein {{0}} im Text", State = PublishState.Fail, Item = com, Module = mod });

                }
                
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
                {
                    if(rcom.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): Kein TextParameter angegeben", State = PublishState.Fail, Item = rcom, Module = mod });
                    if(rcom.Text.Any(t => !t.Text.Contains("{{0")))
                        actions.Add(new PublishAction() { Text = $"    ComObjectRef {rcom.Name} ({rcom.UId}): TextParameter angegeben, aber kein {{0}} im Text", State = PublishState.Fail, Item = rcom, Module = mod });
                }
            }
        
            //TODO check union size fits parameter+offset

            CheckDynamicItem(vbase.Dynamics[0], actions, ns, showOnlyErrors, mod);

            
            foreach(Module xmod in vbase.Modules)
            {
                actions.Add(new PublishAction() { Text = $"Prüfe Module '{xmod.Name}'" });
                CheckVersion(ver, xmod, actions, ver.DefaultLanguage, ver.NamespaceVersion, showOnlyErrors);
                //TODO check for Argument exist
            }

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
                    List<Application> appList = general.Applications.ToList().FindAll(a => a.Versions.Any(v => v.Number == vers.Number));
                    foreach (Application app in appList)
                    {
                        if (devices != null && citem.Hardware.Apps.Contains(app))
                        {
                            foreach (Device dev in citem.Hardware.Devices.Where(d => devices.Contains(d)))
                            {
                                foreach (Language lang in vers.Languages)
                                {
                                    if (!dev.Text.Any(l => l.Language.CultureCode == lang.CultureCode || string.IsNullOrEmpty(l.Text)))
                                        actions.Add(new PublishAction() { Text = $"Geräte: Text enthält nicht alle Sprachen der Applikation.", State = PublishState.Fail });
                                    if (!dev.Description.Any(l => l.Language.CultureCode == lang.CultureCode || string.IsNullOrEmpty(l.Text)))
                                        actions.Add(new PublishAction() { Text = $"Geräte: Beschreibung enthält nicht alle Sprachen der Applikation.", State = PublishState.Fail });
                                }
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
                    
                    if(dpb.UseIcon && dpb.IconObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynParaBlock {dpb.Name} wurde kein Icon zugeordnet", State = PublishState.Fail});
                    break;
                }

                case DynModule dm:
                {
                    if(dm.ModuleObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynModule {dm.Name} wurde kein Module zugeordnet", State = PublishState.Fail});
                        
                    if(dm.Arguments.Any(a => !a.UseAllocator && string.IsNullOrEmpty(a.Value)))
                        actions.Add(new PublishAction() { Text = $"    DynModule {dm.Name} hat Argumente, die leer sind", State = PublishState.Fail});
                    if(dm.Arguments.Any(a => a.UseAllocator && a.Allocator == null))
                        actions.Add(new PublishAction() { Text = $"    DynModule {dm.Name} muss ein Allocator zugewiesen werden", State = PublishState.Fail});
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
                    if(dse.UseIcon && dse.IconObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynSeparator {dse.Name} wurde kein Icon zugeordnet", State = PublishState.Fail});
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

                case DynButton dbtn:
                {
                    if(string.IsNullOrEmpty(dbtn.Name))
                        actions.Add(new PublishAction() { Text = $"    DynButton {dbtn.Name} wurde kein Name zugeordnet", State = PublishState.Fail});
                    if(dbtn.UseTextParameter && dbtn.TextRefObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynButton {dbtn.Name} wurde kein ParameterRef zugeordnet", State = PublishState.Fail});
                    if(dbtn.UseIcon && dbtn.IconObject == null)
                        actions.Add(new PublishAction() { Text = $"    DynButton {dbtn.Name} wurde kein Icon zugeordnet", State = PublishState.Fail});
                    break;
                }

                case DynamicMain:
                case DynamicModule:
                    break;

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

            if(version < 3)
            {   
                Dictionary<int, int> newParaTypeList = new Dictionary<int, int>() 
                {
                    { 0, 12 },
                    { 1, 2 },
                    { 2, 8 },
                    { 3, 9 },
                    { 4, 3 },
                    { 5, 4 },
                    { 6, 5 },
                    { 7, 10 },
                    { 8, 7 },
                    { 9, 6 },
                    { 10, 0 }
                }; 
                Dictionary<int, int> newAccessList = new Dictionary<int, int>()
                {
                    { 0, 2 },
                    { 1, 0 },
                    { 2, 1 },
                    { 3, 2 }
                };

                foreach(JObject app in gen["Applications"])
                {
                    List<AppVersionModel> newVers = new List<AppVersionModel>();
                    foreach(JObject ver in app["Versions"])
                    {
                        JObject jver = JObject.Parse(ver["Version"].ToString());
                        foreach(JObject ptype in jver["ParameterTypes"])
                        {
                            ptype["Type"] = newParaTypeList[int.Parse(ptype["Type"].ToString())];
                        }
                        
                        Update3(jver, newAccessList);
                        foreach(JObject jmod in jver["Modules"])
                            Update3(jmod, newAccessList);
                        
                        ver["Version"] = jver.ToString();
                    }
                }
            }
            
            if(version < 4)
            {
                gen["Guid"] = Guid.NewGuid().ToString();
                foreach(JObject icon in gen["Icons"])
                {
                    icon["LastModified"] = DateTime.Now.ToString("o");
                }
            }
            
            if(version < 5)
            {
                foreach(JObject app in gen["Applications"])
                {
                    foreach(JObject ver in app["Versions"])
                    {
                        JObject jver = JObject.Parse(ver["Version"].ToString());

                        ver["Namespace"] = jver["NamespaceVersion"];
                    }
                }
            }

            return gen.ToString();
        }

        private static void Update3(JObject jver, Dictionary<int, int> newAccessList)
        {
            foreach(JObject para in jver["Parameters"])
            {
                para["Access"] = newAccessList[int.Parse(para["Access"].ToString())];
            }
            foreach(JObject para in jver["ParameterRefs"])
            {
                para["Access"] = newAccessList[int.Parse(para["Access"].ToString())];
            }

            foreach(JObject jdyn in jver["Dynamics"])
            {
                Update3Dyn(jdyn, newAccessList);
            }
        }

        private static void Update3Dyn(JObject jdyn, Dictionary<int, int> newAccessList)
        {
            foreach(JObject jitem in jdyn["Items"])
            {
                switch(jitem["$type"].ToString())
                {
                    case "Kaenx.Creator.Models.Dynamic.DynamicMain, Kaenx.Creator":
                        Update3Dyn(jitem, newAccessList);
                        break;
                    
                    case "Kaenx.Creator.Models.Dynamic.DynChannelIndependent, Kaenx.Creator":
                        Update3Dyn(jitem, newAccessList);
                        break;
                    
                    case "Kaenx.Creator.Models.Dynamic.DynChannel, Kaenx.Creator":
                        if(jitem["Access"] != null)
                            jitem["Access"] = newAccessList[int.Parse(jitem["Access"].ToString())];
                        Update3Dyn(jitem, newAccessList);
                        break;
                    
                    case "Kaenx.Creator.Models.Dynamic.DynParaBlock, Kaenx.Creator":
                        jitem["Access"] = newAccessList[int.Parse(jitem["Access"].ToString())];
                        Update3Dyn(jitem, newAccessList);
                        break;

                    case "Kaenx.Creator.Models.Dynamic.DynChooseBlock, Kaenx.Creator":
                    case "Kaenx.Creator.Models.Dynamic.DynChooseChannel, Kaenx.Creator":
                    case "Kaenx.Creator.Models.Dynamic.DynWhenBlock, Kaenx.Creator":
                    case "Kaenx.Creator.Models.Dynamic.DynWhenChannel, Kaenx.Creator":
                        Update3Dyn(jitem, newAccessList);
                        break;
                }
            }
        }
    }
}
