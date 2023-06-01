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
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_unknown, ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        break;
                }
            }

            foreach(OpenKnxModule mod in vers.OpenKnxModules)
            {
                if(string.IsNullOrEmpty(mod.Prefix))
                    actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_open_noprefix, mod.Name), State = PublishState.Fail });
            
                Module omod = vers.Modules.SingleOrDefault(m => m.Name == mod.Name + " Templ");

                if(omod != null)
                {
                    List<DynModule> lmods = new List<DynModule>();
                    AutoHelper.GetModules(vers.Dynamics[0], lmods);
                    int count = lmods.Count(m => m.ModuleUId == omod.UId);

                    foreach(Module xmod in vers.Modules.Where(m => m.IsOpenKnxModule && m.Name.StartsWith(mod.Name)))
                    {
                        foreach(OpenKnxNum onum in mod.NumChannels)
                        {
                            switch(onum.Type)
                            {
                                case NumberType.ParameterType:
                                {
                                    ParameterType ptype = vers.ParameterTypes.Single(p => p.Name == onum.UId);
                                    if(onum.Property == "Minimum")
                                        ptype.Min = count.ToString();
                                    else if(onum.Property == "Maximum")
                                        ptype.Max = count.ToString();
                                    else
                                        throw new Exception("Not Implemented Property for ParmeterType: " + onum.Property);
                                    break;
                                }

                                case NumberType.Parameter:
                                {
                                    Parameter para = xmod.Parameters.SingleOrDefault(p => p.Name == onum.UId);
                                    if(para != null)
                                    {
                                        if(onum.Property == "Value")
                                            para.Value = count.ToString();
                                        else
                                            throw new Exception("Not Implemented Property for Parmeter: " + onum.Property);
                                    }
                                    break;
                                }
                            }
                        }
                    }
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
                            actions.Add(new PublishAction() { Text = "\t" + Properties.Messages.check_ver_loadprod_msg, State = PublishState.Fail });
                            return;
                        }

                        int id = -1;
                        if (!int.TryParse(xele.Attribute("MessageRef").Value, out id))
                            actions.Add(new PublishAction() { Text = "\t" + Properties.Messages.check_ver_loadprod_msgref, State = PublishState.Fail });
                        if (id != -1)
                        {
                            if (!vers.Messages.Any(m => m.UId == id))
                                actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_loadprod_msgref_error, id), State = PublishState.Fail });
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
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_cat_number, item.Name), State = PublishState.Fail });

                foreach(CatalogItem citem in item.Items)
                    CheckCatalogItem(citem, actions);
            } else {
                int number = -1;
                if(!int.TryParse(item.Number, out number))
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_cat_prodnumber, item.Name), State = PublishState.Fail }); 
                if(item.Hardware == null)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_cat_hardware, item.Name), State = PublishState.Fail });
            }
        }

        private static void CheckVersion(AppVersion ver, IVersionBase vbase, ObservableCollection<PublishAction> actions, string defaultLang, int ns, bool showOnlyErrors)
        {
            Module mod = vbase as Module;

            if(mod != null)
            {
                if(ver.NamespaceVersion < 20 && mod.Allocators.Count > 0)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_mod_allocs, mod.Name), State = PublishState.Fail });
                if(!mod.IsOpenKnxModule && string.IsNullOrEmpty(mod.Prefix))
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_mod_noprefix, mod.Name), State = PublishState.Fail });
                if(mod.ParameterBaseOffset.Type != ArgumentTypes.Numeric)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_mod_paraoff, mod.Name), State = PublishState.Fail });
                if(mod.ComObjectBaseNumber.Type != ArgumentTypes.Numeric)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_mod_combase, mod.Name), State = PublishState.Fail });
            }

            if(vbase.Dynamics[0].Items.Count == 0)
            {
                string name = "AppVersion";
                if(mod != null)
                    name = $"Modul {mod.Name}";
                actions.Add(new PublishAction() { Text = $"\t{name}: " + Properties.Messages.check_ver_dyn_items, State = PublishState.Fail });
            }

            foreach(Parameter para in vbase.Parameters) {
                if(para.ParameterTypeObject == null) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_type, para.Name, para.UId), State = PublishState.Fail, Item = para, Module = mod });
                else {
                    CheckValue(para, mod, actions);
                }
                
                if(!showOnlyErrors)
                    CheckText(para, defaultLang, mod, actions);

                CheckSuffix(para, showOnlyErrors, defaultLang, mod, actions);

                if(!para.IsInUnion) {
                    switch(para.SavePath) {
                        case SavePaths.Memory:
                        {
                            if(para.SaveObject == null)
                                actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_save_error, para.Name, para.UId), State = PublishState.Fail, Item = para, Module = mod });
                            else {
                                if(!(para.SaveObject as Memory).IsAutoPara && para.Offset == -1) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_save_offset, para.Name, para.UId), State = PublishState.Fail, Item = para, Module = mod });
                                if(!(para.SaveObject as Memory).IsAutoPara && para.OffsetBit == -1) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_save_offsetbit, para.Name, para.UId), State = PublishState.Fail, Item = para, Module = mod });

                            }
                            if(para.OffsetBit > 7) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_save_obsolet, para.Name, para.UId), State = PublishState.Fail, Item = para, Module = mod });
                            break;
                        }

                        case SavePaths.Property:
                        {
                            if((para.SaveObject as Property).ObjectType == -1) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_save_objectindex, para.Name, para.UId), State = PublishState.Fail, Item = para, Module = mod });
                            if((para.SaveObject as Property).PropertyId == -1) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_save_propertyid, para.Name, para.UId), State = PublishState.Fail, Item = para, Module = mod });
                            break;
                        }
                    }
                } else {
                    if(para.UnionObject == null) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_save_union, para.Name, para.UId), State = PublishState.Fail, Item = para, Module = mod });
                }
            }
        
            foreach(ParameterRef para in vbase.ParameterRefs) {
                if(para.ParameterObject == null) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_pararef_no_para, para.Name, para.UId), State = PublishState.Fail, Item = para, Module = mod });
                else {
                    if(para.ParameterObject.ParameterTypeObject == null || string.IsNullOrEmpty(para.Value))
                        continue;
                    
                    ParameterType ptype = para.ParameterObject.ParameterTypeObject;

                    if(para.OverwriteText)
                        CheckText(para, defaultLang, mod, actions);

                    if(para.OverwriteValue)
                        CheckValue(para, mod, actions);

                    if(para.OverwriteSuffix)
                        CheckSuffix(para, showOnlyErrors, defaultLang, mod, actions);
                }
                
            }
        
            
            var x = vbase.ComObjects.GroupBy((c) => c.Number);
            if(x.Any((c) => c.Count() > 1))
            {
                actions.Add(new PublishAction() { Text = "\t" + Properties.Messages.check_ver_com_not_unique, State = PublishState.Warning });
            } else {
                foreach(ComObject com in vbase.ComObjects)
                {
                    com.Id = com.Number;
                }
            }

            foreach(ComObject com in vbase.ComObjects) {
                if(com.HasDpt && com.Type == null) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_no_dpt, com.Name, com.UId), State = PublishState.Fail, Item = com, Module = mod });
                if(com.HasDpt && com.Type != null && com.Type.Number == "0") actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_no_dpt_ref, com.Name, com.UId), State = PublishState.Fail, Item = com, Module = mod });
                if(com.HasDpt && com.HasDpts && com.SubType == null) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_no_dpst, com.Name, com.UId), State = PublishState.Fail, Item = com, Module = mod });
            
                if(com.TranslationText) {
                    Translation trans = com.Text.Single(t => t.Language.CultureCode == defaultLang);
                    if(string.IsNullOrEmpty(trans.Text))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_com_lang_text, "ComObject", com.Name, com.UId, trans.Language.Text), State = PublishState.Fail, Item = com, Module = mod });
                } else {
                    if(!showOnlyErrors)
                    {
                        if(com.Text.Count == com.Text.Count(t => string.IsNullOrEmpty(t.Text)))
                                actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_com_lang_text, "ComObject", com.Name, com.UId, ""), State = PublishState.Warning, Item = com, Module = mod });
                        else
                            foreach(Translation trans in com.Text)
                                if(string.IsNullOrEmpty(trans.Text))
                                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_com_lang_text, "ComObject", com.Name, com.UId, trans.Language.Text), State = PublishState.Warning, Item = com, Module = mod });
                    }
                }
                

                if(com.TranslationFunctionText) {
                    Translation trans = com.FunctionText.Single(t => t.Language.CultureCode == defaultLang);
                    if(string.IsNullOrEmpty(trans.Text))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_com_lang_functext, "ComObject", com.Name, com.UId, trans.Language.Text), State = PublishState.Fail, Item = com, Module = mod });
                } else {
                    if(!showOnlyErrors)
                    {
                        if(com.FunctionText.Count == com.FunctionText.Count(t => string.IsNullOrEmpty(t.Text)))
                                actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_com_lang_text, "ComObject", com.Name, com.UId, ""), State = PublishState.Warning, Item = com, Module = mod });
                        else
                            foreach(Translation trans in com.FunctionText)
                                if(string.IsNullOrEmpty(trans.Text))
                                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_com_lang_text, "ComObject", com.Name, com.UId, trans.Language.Text), State = PublishState.Warning, Item = com, Module = mod });
                    }
                }

                if(vbase.IsComObjectRefAuto && com.UseTextParameter)
                {
                    if(com.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_no_textpara, "ComObject", com.Name, com.UId), State = PublishState.Fail, Item = com, Module = mod });
                    if(com.Text.Any(t => !t.Text.Contains("{{0")))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_no_used_textpara, "ComObject", com.Name, com.UId), State = PublishState.Fail, Item = com, Module = mod });
                }
                
                if(com.FlagComm == FlagType.Undefined)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagComm, "ComObject", com.Name, com.UId), State = PublishState.Fail, Item = com, Module = mod });
                if(com.FlagOnInit == FlagType.Undefined)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagOnInit, "ComObject", com.Name, com.UId), State = PublishState.Fail, Item = com, Module = mod });
                if(com.FlagRead == FlagType.Undefined)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagRead, "ComObject", com.Name, com.UId), State = PublishState.Fail, Item = com, Module = mod });
                if(com.FlagTrans == FlagType.Undefined)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagTrans, "ComObject", com.Name, com.UId), State = PublishState.Fail, Item = com, Module = mod });
                if(com.FlagUpdate == FlagType.Undefined)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagUpdate, "ComObject", com.Name, com.UId), State = PublishState.Fail, Item = com, Module = mod });
                if(com.FlagWrite == FlagType.Undefined)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagWrite, "ComObject", com.Name, com.UId), State = PublishState.Fail, Item = com, Module = mod });
            }

            foreach(ComObjectRef rcom in vbase.ComObjectRefs) {
                if(rcom.ComObjectObject == null) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_comref_no_com, rcom.Name, rcom.UId), State = PublishState.Fail, Item = rcom, Module = mod });
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

                if(!vbase.IsComObjectRefAuto && rcom.UseTextParameter && rcom.ParameterRefObject == null)
                {
                    if(rcom.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_no_textpara, "ComObjectRef", rcom.Name, rcom.UId), State = PublishState.Fail, Item = rcom, Module = mod });
                    if(rcom.Text.Any(t => !t.Text.Contains("{{0")))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_no_used_textpara, "ComObjectRef", rcom.Name, rcom.UId), State = PublishState.Fail, Item = rcom, Module = mod });
                }

                if(rcom.OverwriteFC && rcom.FlagComm == FlagType.Undefined)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagComm, "ComObjectRef", rcom.Name, rcom.UId), State = PublishState.Fail, Item = rcom, Module = mod });
                if(rcom.OverwriteFOI && rcom.FlagOnInit == FlagType.Undefined)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagOnInit, "ComObjectRef", rcom.Name, rcom.UId), State = PublishState.Fail, Item = rcom, Module = mod });
                if(rcom.OverwriteFR && rcom.FlagRead == FlagType.Undefined)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagRead, "ComObjectRef", rcom.Name, rcom.UId), State = PublishState.Fail, Item = rcom, Module = mod });
                if(rcom.OverwriteFT && rcom.FlagTrans == FlagType.Undefined)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagTrans, "ComObjectRef", rcom.Name, rcom.UId), State = PublishState.Fail, Item = rcom, Module = mod });
                if(rcom.OverwriteFU && rcom.FlagUpdate == FlagType.Undefined)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagUpdate, "ComObjectRef", rcom.Name, rcom.UId), State = PublishState.Fail, Item = rcom, Module = mod });
                if(rcom.OverwriteFW && rcom.FlagWrite == FlagType.Undefined)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_com_flagWrite, "ComObjectRef", rcom.Name, rcom.UId), State = PublishState.Fail, Item = rcom, Module = mod });
            }
        
            foreach(Union union in vbase.Unions)
            {
                foreach(Parameter para in vbase.Parameters.Where(p => p.IsInUnion && p.UnionId == union.UId))
                {
                    if(para.ParameterTypeObject.SizeInBit + (para.Offset * 8) + para.OffsetBit >union.SizeInBit)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_union_size, union.Name, para.Name, para.UId), State = PublishState.Fail });
                }
            }

            CheckDynamicItem(vbase.Dynamics[0], actions, ns, showOnlyErrors, mod);

            
            foreach(Module xmod in vbase.Modules)
            {
                actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_ver_mods, xmod.Name) });
                CheckVersion(ver, xmod, actions, ver.DefaultLanguage, ver.NamespaceVersion, showOnlyErrors);
                //TODO check for Argument exist
            }

        }

        private static void CheckValue(object item, object mod, ObservableCollection<PublishAction> actions)
        {
            ParameterType type = null;
            string name = "";
            string value = "";
            string stype = item.GetType().Name;
            int uid = 0;

            if(item is Parameter parameter)
            {
                name = parameter.Name;
                uid = parameter.UId;
                type = parameter.ParameterTypeObject;
                value = parameter.Value;
            } else if(item is ParameterRef parameterref)
            {
                name = parameterref.Name;
                uid = parameterref.UId;
                type = parameterref.ParameterObject.ParameterTypeObject;
                value = parameterref.Value;
            }

            switch(type.Type) {
                case ParameterTypes.Text:
                    if((value.Length*8) > type.SizeInBit) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_text, stype, value.Length*8, type.SizeInBit), State = PublishState.Fail, Item = item, Module = mod });
                    break;

                case ParameterTypes.Enum:
                    int paraval2;
                    if(!int.TryParse(value, out paraval2)) 
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_enum1, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    else {
                        if(!type.Enums.Any(e => e.Value == paraval2))
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_enum2, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    }
                    break;

                case ParameterTypes.NumberUInt:
                case ParameterTypes.NumberInt:
                    long paraval;
                    if(!long.TryParse(value, out paraval)) 
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_number1, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    else {
                        if(paraval > long.Parse(type.Max) || paraval < long.Parse(type.Min))
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_number2, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
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
                    switch(type.UIHint)
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
                    if(reg != null && !reg.IsMatch(value))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_color1, stype, name, uid, def), State = PublishState.Fail, Item = item, Module = mod });
                    break;
                }

                case ParameterTypes.RawData:
                {
                    if(value.Length % 2 != 0) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_raw1, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    else if((value.Length / 2) > long.Parse(type.Max)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_raw2, stype, name, uid, value.Length / 2, type.Max), State = PublishState.Fail, Item = item, Module = mod });
                    Regex reg = new Regex("^([0-9A-Fa-f])+$");
                    if(!reg.IsMatch(value)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_raw3, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    break;
                }

                case ParameterTypes.Date:
                {
                    Regex reg = new Regex("([0-9]{4}-[0-9]{2}-[0-9]{2})");
                    if(!reg.IsMatch(value))  actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_date, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    break;                        
                }
            }
        }

        private static void CheckText(object item, string defaultLang, object mod, ObservableCollection<PublishAction> actions)
        {
            bool translate = false;
            string name = "";
            string stype = item.GetType().Name;
            int uid = 0;
            ObservableCollection<Translation> text = null;

            if(item is Parameter parameter)
            {
                name = parameter.Name;
                uid = parameter.UId;
                text = parameter.Suffix;
                translate = parameter.TranslationText;
            } else if(item is ParameterRef parameterref)
            {
                name = parameterref.Name;
                uid = parameterref.UId;
                text = parameterref.Suffix;
                translate = parameterref.TranslationText;
            }
            
            /*if(translate) {
                Translation trans = text.Single(t => t.Language.CultureCode == defaultLang);
                if(string.IsNullOrEmpty(trans.Text))
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_lang_no_translation, stype, name, uid), State = PublishState.Warning, Item = item, Module = mod });
            } else {
                if(text.Any(s => string.IsNullOrEmpty(s.Text)))
                {
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_lang_not_all, stype, name, uid), State = PublishState.Warning, Item = item, Module = mod });
                }
            }*/
        }

        private static void CheckSuffix(object item, bool showOnlyErrors, string defaultLang, object mod, ObservableCollection<PublishAction> actions)
        {
            bool translate = false;
            string name = "";
            string stype = item.GetType().Name;
            int uid = 0;
            ObservableCollection<Translation> text = null;

            if(item is Parameter parameter)
            {
                name = parameter.Name;
                uid = parameter.UId;
                text = parameter.Suffix;
                translate = parameter.TranslationSuffix;
            } else if(item is ParameterRef parameterref)
            {
                name = parameterref.Name;
                uid = parameterref.UId;
                text = parameterref.Suffix;
                translate = parameterref.TranslationSuffix;
            }

            /*if(!showOnlyErrors)
            {
                if(translate) {
                    Translation trans = text.Single(t => t.Language.CultureCode == defaultLang);
                    if(string.IsNullOrEmpty(trans.Text))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_lang_no_translation, stype, name, uid), State = PublishState.Warning, Item = item, Module = mod });
                } else {
                    if(text.Any(s => string.IsNullOrEmpty(s.Text)))
                    {
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_lang_not_all, stype, name, uid), State = PublishState.Warning, Item = item, Module = mod });
                    }
                }
            }*/
            
            if(text.Any(t => t.Text.Length > 20)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_lang_suffix_length, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
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
                                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_cat_dev_text_not_all, dev.Name), State = PublishState.Fail });
                                    if (!dev.Description.Any(l => l.Language.CultureCode == lang.CultureCode || string.IsNullOrEmpty(l.Text)))
                                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_cat_dev_desc_not_all), State = PublishState.Fail });
                                }
                            }
                        }
                    }
                } else {
                    foreach(Language lang in vers.Languages)
                    {
                        if(!citem.Text.Any(l => l.Language.CultureCode == lang.CultureCode || string.IsNullOrEmpty(l.Text)))
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_cat_cat_text_not_all, citem.Name), State = PublishState.Fail });
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
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_channel_no_number, dc.Name), State = PublishState.Fail});

                    if(dc.UseTextParameter && dc.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_channel_textpara), State = PublishState.Fail});
                    break;
                }

                case DynParaBlock dpb:
                {
                    if(vbase is AppVersion av)
                    {
                        if(!av.IsPreETS4 && dpb.UseParameterRef)
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_block_pref_error, dpb.Name), State = PublishState.Warning});
                    }
                    if(dpb.UseParameterRef && dpb.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_block_no_pref, dpb.Name), State = PublishState.Fail});
                        
                    if(dpb.UseTextParameter && dpb.TextRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_block_no_textpara, dpb.Name), State = PublishState.Fail});
                    
                    if(dpb.UseIcon && dpb.IconObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_block_no_icon, dpb.Name), State = PublishState.Fail});
                    break;
                }

                case DynModule dm:
                {
                    if(dm.ModuleObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_mod_no_module, dm.Name), State = PublishState.Fail});
                        
                    if(dm.Arguments.Any(a => !a.UseAllocator && string.IsNullOrEmpty(a.Value)))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_mod_empty_arg, dm.Name), State = PublishState.Fail});
                    if(dm.Arguments.Any(a => a.UseAllocator && a.Allocator == null))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_mod_no_alloc, dm.Name), State = PublishState.Fail});
                    if(dm.Arguments.Any(a => a.Argument.Type != ArgumentTypes.Numeric && a.UseAllocator))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_mod_alloc_error), State = PublishState.Fail});
                    break;
                }

                case IDynChoose dco:
                {
                    if(dco.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_choose_no_pararef, dco.Name), State = PublishState.Fail});
                    break;
                }

                case IDynWhen dwh:
                {
                    if(string.IsNullOrEmpty(dwh.Condition) && !dwh.IsDefault)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_when_no_cond, dwh.Name), State = PublishState.Fail});
                    
                    if(!showOnlyErrors && !string.IsNullOrEmpty(dwh.Condition) && dwh.IsDefault)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_when_default, dwh.Name), State = PublishState.Warning});
                    break;
                }

                case DynParameter dpa:
                {
                    if(dpa.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_para_no_ref, dpa.Name), State = PublishState.Fail, Item = dpa, Module = vbase });
                    if(dpa.HasHelptext && dpa.Helptext == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_para_no_help, dpa.Name), State = PublishState.Fail, Item = dpa, Module = vbase });
                    break;
                }

                case DynComObject dco:
                {
                    if(dco.ComObjectRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_com_no_ref, dco.Name), State = PublishState.Fail});
                    break;
                }

                case DynSeparator dse:
                {
                    if(dse.UseTextParameter && dse.TextRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_sep_no_ref, dse.Name), State = PublishState.Fail});
                    if(dse.Hint != SeparatorHint.None && ns < 14)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_sep_uihint, dse.Name), State = PublishState.Fail});
                    if(dse.UseIcon && dse.IconObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_sep_no_icon, dse.Name), State = PublishState.Fail});
                    break;
                }

                case DynAssign das:
                {
                    if(das.TargetObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_assign_no_target, das.Name), State = PublishState.Fail});
                        
                    if(das.SourceObject == null && string.IsNullOrEmpty(das.Value))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_assign_no_source, das.Name), State = PublishState.Fail});
                    break;
                }

                case DynButton dbtn:
                {
                    if(string.IsNullOrEmpty(dbtn.Name))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_btn_no_name, dbtn.Name), State = PublishState.Fail});
                    if(dbtn.UseTextParameter && dbtn.TextRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_btn_no_ref, dbtn.Name), State = PublishState.Fail});
                    if(dbtn.UseIcon && dbtn.IconObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_btn_no_icon, dbtn.Name), State = PublishState.Fail});
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
