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
        public static void CheckThis(MainModel General, ObservableCollection<PublishAction> actions, bool showOnlyErrors = false)
        {
            actions.Add(new PublishAction() { Text = Properties.Messages.check_start });
            
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

                if(!General.Info.HasIndividualAddress)
                    actions.Add(new PublishAction() { Text = Properties.Messages.check_hard_no_physicaladdress, State = PublishState.Fail });
                if(!General.Info.HasApplicationProgram)
                    actions.Add(new PublishAction() { Text = Properties.Messages.check_hard_no_app, State = PublishState.Fail });
                if(!General.Info.HasApplicationProgram && General.Info.HasApplicationProgram2)
                    actions.Add(new PublishAction() { Text = Properties.Messages.check_hard_no_app2, State = PublishState.Fail });
                #endregion


                #region Applikation Check
                actions.Add(new PublishAction() { Text = Properties.Messages.check_app });

                if(General.IsOpenKnx)
                {
                    if(General.Application.Number > 0xFF)
                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_app_openknx, General.Application.NameText), State = PublishState.Fail });
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

            actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_ver, General.Application.NameText) });
            CheckVersion(General, actions, showOnlyErrors);
            
            actions.Add(new PublishAction() { Text = Properties.Messages.check_end });
        }


        public static void CheckVersion(
            MainModel General,
            ObservableCollection<PublishAction> actions, 
            bool showOnlyErrors = false)
        {
            if(string.IsNullOrEmpty(General.Info.Mask.MediumTypes))
            {
                actions.Add(new PublishAction() { Text = Properties.Messages.check_ver_mediumtypes, State = PublishState.Fail });
            }
            
            if(General.Application.IsModulesActive && General.Application.NamespaceVersion < 20)
                actions.Add(new PublishAction() { Text = Properties.Messages.check_ver_modules, State = PublishState.Fail });

            if(General.Application.IsMessagesActive && General.Application.NamespaceVersion < 14)
                actions.Add(new PublishAction() { Text = Properties.Messages.check_ver_messages, State = PublishState.Fail });

            if(General.Info.Mask.Procedure != ProcedureTypes.Default && string.IsNullOrEmpty(General.Application.Procedure))
                actions.Add(new PublishAction() { Text = Properties.Messages.check_ver_loadprod, State = PublishState.Fail });

            if(General.Application.IsHelpActive && General.Application.NamespaceVersion < 14)
                actions.Add(new PublishAction() { Text = Properties.Messages.check_ver_helptext, State = PublishState.Fail });
            
            
            IEnumerable<IGrouping<int, Memory>> mems;
            if(General.Info.Mask.Memory == MemoryTypes.Relative)
                mems = General.Application.Memories.GroupBy(m => m.Offset);
            else
                mems = General.Application.Memories.GroupBy(m => m.Address);

            foreach(var memg in mems.Where((c) => c.Count() > 1)) {
                actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_ver_mem_not_unique, memg.Key), State = PublishState.Fail });
            }
            if(General.Application.Memories.Count > 0 && !General.Application.Memories[0].IsAutoLoad)
            {
                XElement xtemp = XElement.Parse(General.Application.Procedure);
                foreach(XElement xele in xtemp.Descendants())
                {
                    if(xele.Name.LocalName == "LdCtrlRelSegment")
                    {
                        if(xele.Attribute("LsmIdx").Value == "4")
                        {
                            int memsize = int.Parse(xele.Attribute("Size")?.Value ?? "0");
                            if(memsize != General.Application.Memories[0].Size)
                                actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_ver_loadprod_size, xele.Name.LocalName), State = PublishState.Warning });
                        }
                    }
                    if(xele.Name.LocalName == "LdCtrlWriteRelMem")
                    {
                        if(xele.Attribute("ObjIdx").Value == "4")
                        {
                            int memsize = int.Parse(xele.Attribute("Size")?.Value ?? "0");
                            if(memsize != General.Application.Memories[0].Size)
                                actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_ver_loadprod_size, xele.Name.LocalName), State = PublishState.Warning });
                        }
                    }
                }
            }

            foreach(var x in General.Application.ParameterTypes.GroupBy(p => p.Name).Where(p => p.Count() > 1))
            {
                actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_ver_parat_name_duplicate, x.ElementAt(0).Name, x.Count()), State = PublishState.Fail, Item = x.ElementAt(1) });
            }

            foreach(ParameterType ptype in General.Application.ParameterTypes)
            {
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
                                Translation trans = penum.Text.Single(t => t.Language.CultureCode == General.Application.DefaultLanguage);
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
                        if(ptype.UIHint == "ProgressBar" && General.Application.NamespaceVersion < 20)
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
                        if(min < 0) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_uint_min2, "UInt", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(min > max) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_minmax, "UInt", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(max >= maxsize) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_max2, "UInt", ptype.Name, ptype.UId, maxsize-1), State = PublishState.Fail, Item = ptype });
                        break;
                    }

                    case ParameterTypes.NumberInt:
                    {
                        if(ptype.UIHint == "Progressbar" && General.Application.NamespaceVersion < 20)
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_progbar, "UInt", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });

                        long min, max, temp;
                        if(!long.TryParse(ptype.Max, out max)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_min, "Int", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.Min, out min)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_max, "Int", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.DisplayOffset, out temp)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_offset, "Int", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.DisplayFactor, out temp)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_factor, "Int", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });

                        maxsize = (long)Math.Ceiling(maxsize / 2.0);
                        if(!ptype.IsSizeManual)
                        {
                            int lmax = Convert.ToString(max, 2).Length + 1;
                            string smin = Convert.ToString(min, 2);
                            int pos = smin.IndexOf('0');
                            int lmin = smin.Length - pos + 1;
                            ptype.SizeInBit = (lmax > lmin) ? lmax : lmin;
                            //TODO make possible to use smaller ints
                            ptype.SizeInBit = (int)(Math.Ceiling((ptype.SizeInBit-1) / 8.0) * 8);
                            maxsize = (long)(Math.Pow(2, ptype.SizeInBit) / 2);
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
                        if(!General.Application.IsPreETS4)
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_none, ptype.Name, ptype.UId), State = PublishState.Warning, Item = ptype });
                        break;

                    case ParameterTypes.Color:
                        if(ptype.UIHint != "RGB" && ptype.UIHint != "RGBW" && ptype.UIHint != "HSV")
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_color1, ptype.Name, ptype.UId, ptype.UIHint), State = PublishState.Fail, Item = ptype });
                        if(ptype.UIHint == "RGBW" && General.Application.NamespaceVersion < 20)
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

                    case ParameterTypes.Time:
                    {
                        long min, max;
                        if(!long.TryParse(ptype.Max, out max)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_max, "Time", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(!long.TryParse(ptype.Min, out min)) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_min, "Time", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        
                        if(!ptype.IsSizeManual)
                        {
                            if(ptype.Increment == "PackedSecondsAndMilliseconds") {
                                ptype.SizeInBit = 2*8;
                                maxsize = 64999;
                            } else if(ptype.Increment == "PackedDaysHoursMinutesAndSeconds") {
                                ptype.SizeInBit = 3*8;
                                maxsize = 777599;
                            } else if(ptype.Increment == "PackedMinutesSecondsAndMilliseconds") {
                                ptype.SizeInBit = 3*8;
                                maxsize = 15359999;
                            } else {
                                string bin = Convert.ToString(max, 2);
                                ptype.SizeInBit = bin.Length;
                                maxsize = (long)Math.Pow(2, ptype.SizeInBit);
                            }
                        }
                        
                        if(ptype.SizeInBit > 64) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_size, "UInt", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(min < 0) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_uint_min2, "Time", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        if(min > max) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_minmax, "Time", ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                            
                        if(max >= maxsize) actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_int_max2, "Time", ptype.Name, ptype.UId, maxsize), State = PublishState.Fail, Item = ptype });
                        break;
                    }

                    default:
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_parat_unknown, ptype.Name, ptype.UId), State = PublishState.Fail, Item = ptype });
                        break;
                }
            }

            foreach(OpenKnxModule mod in General.Application.OpenKnxModules)
            {
                if(string.IsNullOrEmpty(mod.Prefix))
                    actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_open_noprefix, mod.Name), State = PublishState.Fail });
            
                Module omod = General.Application.Modules.SingleOrDefault(m => m.Name == mod.Name + " Templ");

                if(omod != null)
                {
                    List<DynModule> lmods = new List<DynModule>();
                    Kaenx.Creator.Classes.Helper.GetModules(General.Application.Dynamics[0], lmods);
                    int count = lmods.Count(m => m.ModuleUId == omod.UId);

                    foreach(Module xmod in General.Application.Modules.Where(m => m.IsOpenKnxModule && m.Name.StartsWith(mod.Name)))
                    {
                        foreach(OpenKnxNum onum in mod.NumChannels)
                        {
                            switch(onum.Type)
                            {
                                case NumberType.ParameterType:
                                {
                                    ParameterType ptype = General.Application.ParameterTypes.Single(p => p.Name == onum.UId);
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

            if(General.IsOpenKnx)
            {
                if(General.Info.AppNumber > 0xFF)
                {
                    actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_app_openknx, General.Application.NameText), State = PublishState.Fail });
                    General.Info.AppNumber = 0;
                }
            } else {
                if(General.Info.AppNumber > 0xFFFF) {
                    actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_app_number, General.Application.NameText), State = PublishState.Fail });
                    General.Info.AppNumber = 0;
                }
            }

            CheckVersion(General.Application, General.Application, actions, General.Application.DefaultLanguage, General.Application.NamespaceVersion, showOnlyErrors);
            if(General != null)
                CheckLanguages(General, actions);

            if (General.Info.Mask.Procedure != ProcedureTypes.Default)
            {
                if(string.IsNullOrEmpty(General.Application.Procedure))
                {
                    actions.Add(new PublishAction() { Text = "\t" + Properties.Messages.check_ver_loadprod_empty, State = PublishState.Fail });
                } else {
                    XElement temp = null;
                    try{
                        temp = XElement.Parse(General.Application.Procedure);
                        temp.Attributes().Where((x) => x.IsNamespaceDeclaration).Remove();
                        foreach (XElement xele in temp.Descendants())
                        {
                            if (xele.Name.LocalName == "OnError")
                            {
                                if (!General.Application.IsMessagesActive)
                                {
                                    actions.Add(new PublishAction() { Text = "\t" + Properties.Messages.check_ver_loadprod_msg, State = PublishState.Fail });
                                    return;
                                }

                                int id = -1;
                                string mref = xele.Attribute("MessageRef").Value;
                                if (!int.TryParse(mref, out id))
                                {
                                    if(!General.Application.Messages.Any(m => m.Name == mref))
                                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_loadprod_msgref, mref), State = PublishState.Fail });
                                    else
                                        id = -1;
                                }
                                if (id != -1)
                                {
                                    if (!General.Application.Messages.Any(m => m.UId == id))
                                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_loadprod_msgref_error, id), State = PublishState.Fail });
                                }
                            }
                        }
                    } catch(Exception ex) {
                        actions.Add(new PublishAction() { Text = $"\t{Properties.Messages.check_ver_loadprod_failed} ({ex.Message})", State = PublishState.Fail });
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
                if(mod.ParameterBaseOffset == null)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_mod_paraoff_null, mod.Name), Item = mod, State = PublishState.Fail });
                else if(mod.ParameterBaseOffset.Type != ArgumentTypes.Numeric)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_mod_paraoff, mod.Name), Item = mod, State = PublishState.Fail });
                if(mod.ComObjectBaseNumber == null)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_mod_combase_null, mod.Name), Item = mod, State = PublishState.Fail });
                else if(mod.ComObjectBaseNumber.Type != ArgumentTypes.Numeric)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_mod_combase, mod.Name), Item = mod, State = PublishState.Fail });
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
                if(ver.IsPreETS4 && para.DisplayOrder == -1)
                    actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_pararef_no_display_order, para.Name, para.UId), State = PublishState.Fail, Item = para, Module = mod });
                
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
                {
                    int paraval2;
                    if(!int.TryParse(value, out paraval2)) 
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_enum1, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    else {
                        if(!type.Enums.Any(e => e.Value == paraval2))
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_enum2, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    }
                    break;
                }

                case ParameterTypes.NumberUInt:
                case ParameterTypes.NumberInt:
                {
                    long paraval;
                    if(!long.TryParse(value, out paraval)) 
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_number1, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    else {
                        if(paraval > long.Parse(type.Max) || paraval < long.Parse(type.Min))
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_number2, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    }
                    break;
                }

                case ParameterTypes.Float_DPT9:
                case ParameterTypes.Float_IEEE_Single:
                case ParameterTypes.Float_IEEE_Double:
                {
                    double paraval;
                    if(!double.TryParse(value.Replace(".", ","), out paraval))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_float, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    else {
                            if(paraval > double.Parse(type.Max.Replace(".", ",")) || paraval < double.Parse(type.Min.Replace(".", ",")))
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_number2, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                    }
                    break;
                }

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
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_color, stype, name, uid, def), State = PublishState.Fail, Item = item, Module = mod });
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

                case ParameterTypes.Time:
                {
                    //TODO implement check
                    if(type.Increment == "PackedSecondsAndMilliseconds") {
                        //TODO
                    } else if(type.Increment == "PackedDaysHoursMinutesAndSeconds") {
                        //TODO
                    } else if(type.Increment == "PackedMinutesSecondsAndMilliseconds") {
                        //TODO
                    } else {
                        long paraval;
                        if(!long.TryParse(value, out paraval)) 
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_number1, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                        else {
                            if(paraval > long.Parse(type.Max) || paraval < long.Parse(type.Min))
                                actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_ver_para_number2, stype, name, uid), State = PublishState.Fail, Item = item, Module = mod });
                        }
                    }
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

        private static void CheckLanguages(MainModel general, ObservableCollection<PublishAction> actions)
        {
            List<CatalogItem> toCheck = new List<CatalogItem>();
            CheckCatalog(general.Catalog[0], general, actions);
        }

        private static void CheckCatalog(CatalogItem item, MainModel general,  ObservableCollection<PublishAction> actions)
        {
            foreach(CatalogItem citem in item.Items)
            {
                if(!citem.IsSection)
                {
                    //todo move to general check for device info
                    /*List<Application> appList = general.Applications.ToList().FindAll(a => a.Versions.Any(v => v.Number == vers.Number));
                    foreach (Application app in appList)
                    {
                        if (devices != null && citem.Hardware.Apps.Contains(app))
                        {
                            foreach (Device dev in citem.Hardware.Devices.Where(d => devices.Contains(d)))
                            {
                                foreach (Language lang in general.Languages)
                                {
                                    if (!dev.Text.Any(l => l.Language.CultureCode == lang.CultureCode || string.IsNullOrEmpty(l.Text)))
                                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_cat_dev_text_not_all, dev.Name), State = PublishState.Fail });
                                    if (!dev.Description.Any(l => l.Language.CultureCode == lang.CultureCode || string.IsNullOrEmpty(l.Text)))
                                        actions.Add(new PublishAction() { Text = string.Format(Properties.Messages.check_cat_dev_desc_not_all), State = PublishState.Fail });
                                }
                            }
                        }
                    }*/
                } else {
                    foreach(Language lang in general.Application.Languages)
                    {
                        if(!citem.Text.Any(l => l.Language.CultureCode == lang.CultureCode || string.IsNullOrEmpty(l.Text)))
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_cat_cat_text_not_all, citem.Name), State = PublishState.Fail });
                    }
                    CheckCatalog(citem, general, actions);
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
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_channel_no_number, dc.Name), Item = dc, Module = vbase, State = PublishState.Fail});

                    if(dc.UseTextParameter && dc.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_channel_textpara), Item = dc, Module = vbase, State = PublishState.Fail});
                    break;
                }

                case DynParaBlock dpb:
                {
                    if(vbase is AppVersion av)
                    {
                        if(!av.IsPreETS4 && dpb.UseParameterRef)
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_block_pref_error, dpb.Name), Item = dpb, Module = vbase, State = PublishState.Warning});
                    }
                    if(dpb.UseParameterRef && dpb.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_block_no_pref, dpb.Name), Item = dpb, Module = vbase, State = PublishState.Fail});
                        
                    if(dpb.UseTextParameter && dpb.TextRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_block_no_textpara, dpb.Name), Item = dpb, Module = vbase, State = PublishState.Fail});
                    
                    if(dpb.UseIcon && dpb.IconObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_block_no_icon, dpb.Name), Item = dpb, Module = vbase, State = PublishState.Fail});
                    break;
                }

                case DynModule dm:
                {
                    if(dm.ModuleObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_mod_no_module, dm.Name), Item = dm, Module = vbase, State = PublishState.Fail});
                        
                    if(dm.Arguments.Any(a => !a.UseAllocator && string.IsNullOrEmpty(a.Value)))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_mod_empty_arg, dm.Name), Item = dm, Module = vbase, State = PublishState.Fail});
                    if(dm.Arguments.Any(a => a.UseAllocator && a.Allocator == null))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_mod_no_alloc, dm.Name), Item = dm, Module = vbase, State = PublishState.Fail});
                    if(dm.Arguments.Any(a => a.Argument.Type != ArgumentTypes.Numeric && a.UseAllocator))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_mod_alloc_error), Item = dm, Module = vbase, State = PublishState.Fail});
                    break;
                }

                case IDynChoose dco:
                {
                    if(dco.ParameterRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_choose_no_pararef, dco.Name), Item = dco, Module = vbase, State = PublishState.Fail});
                    break;
                }

                case IDynWhen dwh:
                {
                    if(string.IsNullOrEmpty(dwh.Condition) && !dwh.IsDefault)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_when_no_cond, dwh.Name), Item = dwh, Module = vbase, State = PublishState.Fail});
                    
                    if(!showOnlyErrors && !string.IsNullOrEmpty(dwh.Condition) && dwh.IsDefault)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_when_default, dwh.Name), Item = dwh, Module = vbase, State = PublishState.Warning});
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
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_com_no_ref, dco.Name), Item = dco, Module = vbase, State = PublishState.Fail});
                    break;
                }

                case DynSeparator dse:
                {
                    if(dse.UseTextParameter && dse.TextRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_sep_no_ref, dse.Name), Item = dse, Module = vbase, State = PublishState.Fail});
                    if(dse.Hint != SeparatorHint.None && ns < 14)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_sep_uihint, dse.Name), Item = dse, Module = vbase, State = PublishState.Fail});
                    if(dse.UseIcon && dse.IconObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_sep_no_icon, dse.Name), Item = dse, Module = vbase, State = PublishState.Fail});
                    break;
                }

                case DynAssign das:
                {
                    if(das.TargetObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_assign_no_target, das.Name), Item = das, Module = vbase, State = PublishState.Fail});
                        
                    if(das.SourceObject == null && string.IsNullOrEmpty(das.Value))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_assign_no_source, das.Name), Item = das, Module = vbase, State = PublishState.Fail});
                    break;
                }

                case DynButton dbtn:
                {
                    if(string.IsNullOrEmpty(dbtn.Name))
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_btn_no_name, dbtn.Name), Item = dbtn, Module = vbase, State = PublishState.Fail});
                    if(dbtn.UseTextParameter && dbtn.TextRefObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_btn_no_ref, dbtn.Name), Item = dbtn, Module = vbase, State = PublishState.Fail});
                    if(dbtn.UseIcon && dbtn.IconObject == null)
                        actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_btn_no_icon, dbtn.Name), Item = dbtn, Module = vbase, State = PublishState.Fail});
                    break;
                }

                case DynRepeat dr:
                {
                    if(dr.UseParameterRef)
                    {
                        if(dr.ParameterRefObject == null)
                            actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_dr_pararef_null, dr.Name), Item = dr, Module = vbase, State = PublishState.Fail});
                        else if(dr.ParameterRefObject.ParameterObject != null && dr.ParameterRefObject.ParameterObject.ParameterTypeObject != null)
                        {
                            if(dr.ParameterRefObject.ParameterObject.ParameterTypeObject.Type != ParameterTypes.Enum && dr.ParameterRefObject.ParameterObject.ParameterTypeObject.Type != ParameterTypes.NumberUInt)
                                actions.Add(new PublishAction() { Text = "\t" + string.Format(Properties.Messages.check_dyn_dr_type, dr.Name), Item = dr, Module = vbase, State = PublishState.Fail});
                        }
                    }       
                    break;
                }

                case DynChannelIndependent:
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
    }
}
