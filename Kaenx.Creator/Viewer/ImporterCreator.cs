using Kaenx.Creator.Models;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Import.Dynamic;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System;

namespace Kaenx.Creator.Viewer
{
    public class ImporterCreator : IImporter
    {
        private Application _app;
        private AppVersion _version;
        private CatalogContext _context;
        private ApplicationViewModel _model;
        private AppAdditional _adds;

        private Dictionary<string, int> TypeNameToId = new Dictionary<string, int>();
        private Dictionary<int, AppParameter> IdToParameter = new Dictionary<int, AppParameter>();


        public ImporterCreator(AppVersion version, Application app)
        {
            _version = version;
            _app = app;
        }


        public void StartImport(CatalogContext context)
        {
            _context = context;
            DoImport();
        }

        private void DoImport()
        {
            _model = new ApplicationViewModel() {
                Number = _app.Number,
                Version = _version.Number,
                Name = _app.Name
            };
            _context.Applications.Add(_model);
            _context.SaveChanges();

            _adds = new AppAdditional() {
                ApplicationId = _model.Id
            };

            ImportParameterTypes();
            ImportParameters(_version);
            ImportComObjects(_version);

            ImportDynamic();

            _context.AppAdditionals.Add(_adds);
            _context.SaveChanges();
        }

        private void ImportParameterTypes()
        {
            int enumCounter = 1;
            int typeCounter = 1;
            foreach(ParameterType ptype in _version.ParameterTypes)
            {
                AppParameterTypeViewModel mtype = new AppParameterTypeViewModel() {
                    ApplicationId = _model.Id,
                    Name = ptype.Name,
                    Size = ptype.SizeInBit
                };
                TypeNameToId.Add(ptype.Name, typeCounter++);

                switch(ptype.Type)
                {
                    case ParameterTypes.None:
                        mtype.Type = ParamTypes.None;
                        break;

                    case ParameterTypes.Text:
                        mtype.Type = ParamTypes.Text;
                        mtype.Tag1 = enumCounter.ToString();
                        int enumOrder = 0;

                        foreach(ParameterTypeEnum penum in ptype.Enums)
                        {
                            AppParameterTypeEnumViewModel menum = new AppParameterTypeEnumViewModel() {
                                TypeId = enumCounter,
                                //TODO check if ParameterId is necessary
                                Text = penum.Text.Single(t => t.Language.CultureCode == _version.DefaultLanguage).Text,
                                Value = penum.Value.ToString(),
                                Order = enumOrder++
                            };
                            _context.AppParameterTypeEnums.Add(menum);
                        }

                        enumCounter++;
                        break;

                    case ParameterTypes.Enum:
                        mtype.Type = ParamTypes.Enum;
                        break;

                    case ParameterTypes.NumberUInt:
                        if(ptype.UIHint == "CheckBox")
                            mtype.Type = ParamTypes.CheckBox;
                        else
                            mtype.Type = ParamTypes.NumberUInt;
                        mtype.Tag1 = ptype.Min.ToString();
                        mtype.Tag2 = ptype.Max.ToString();
                        break;

                    case ParameterTypes.NumberInt:
                        if(ptype.UIHint == "CheckBox")
                            mtype.Type = ParamTypes.CheckBox;
                        else
                            mtype.Type = ParamTypes.NumberInt;
                        mtype.Tag1 = ptype.Min.ToString();
                        mtype.Tag2 = ptype.Max.ToString();
                        break;

                    case ParameterTypes.Picture:
                        mtype.Type = ParamTypes.Picture;
                        mtype.Tag1 = ptype.BaggageObject.TargetPath;
                        mtype.Tag2 = ptype.BaggageObject.Name;
                        break;

                }

                _context.AppParameterTypes.Add(mtype);

                
                        /*
        -Text,
        -Enum,
        -NumberUInt,
        -NumberInt,
        Float_DPT9,
        Float_IEEE_Single,
        Float_IEEE_Double,
        -Picture,
        -None,
        IpAddress


        
        -Text,
        -Enum,
        -NumberUInt,
        -NumberInt,
        Float9,
        -Picture,
        -None,
        IpAdress,
        Color,
        -CheckBox, //depends on uihint from number
        Time,
        NumberHex,
        Slider
                        */
            }
        }
    
        private void ImportParameters(IVersionBase vbase, Dictionary<string, string> args = null)
        {
            foreach(ParameterRef pref in vbase.ParameterRefs)
            {
                AppParameter mpara = new AppParameter()
                {
                    ApplicationId = _model.Id,
                    ParameterId = (int)pref.Id, //TODO caution creator uses long, viewer only int!
                    Text = GetDefaultLang(pref.OverwriteText ? pref.Text : pref.ParameterObject.Text),
                    Value = pref.OverwriteValue ? pref.Value : pref.ParameterObject.Value,
                    SuffixText = pref.ParameterObject.Suffix,
                    Offset = pref.ParameterObject.Offset,
                    OffsetBit = pref.ParameterObject.OffsetBit,
                    ParameterTypeId = TypeNameToId[pref.ParameterObject.ParameterTypeObject.Name]
                };

                if(args != null)
                {
                    mpara.Offset += int.Parse(args["###para"]);
                    //TODO replace all arguments in Text
                }

                ParamAccess paccess = pref.OverwriteAccess ? pref.Access : pref.ParameterObject.Access;

                mpara.Access = paccess switch {
                    ParamAccess.Default => AccessType.Null,
                    ParamAccess.None => AccessType.None,
                    ParamAccess.Read => AccessType.Read,
                    ParamAccess.ReadWrite => AccessType.Full,
                    _ => throw new System.Exception("Unbekannter ParamAccess: " + paccess.ToString())
                };

                if(pref.ParameterObject.SavePath == ParamSave.Memory)
                {
                    mpara.SegmentType = SegmentTypes.Memory;
                    mpara.SegmentId = 0; //TODO get real id
                } else if(pref.ParameterObject.SavePath == ParamSave.Property) {
                    throw new System.Exception("ParameterSave Property not supported");
                } else if(pref.ParameterObject.SavePath == ParamSave.Nowhere) {
                    mpara.SegmentType = SegmentTypes.None;
                } else {
                    throw new System.Exception("ParameterSave " + pref.ParameterObject.SavePath.ToString() + " not supported");
                }

                values.Add(mpara.ParameterId, new Kaenx.DataContext.Import.Values.StandardValues(mpara.Value));
                IdToParameter.Add(mpara.ParameterId, mpara);
                _context.AppParameters.Add(mpara);
            }
        }

        private void ImportComObjects(IVersionBase vbase, Dictionary<string, string> args = null)
        {
            foreach(ComObjectRef cref in vbase.ComObjectRefs)
            {
                AppComObject com = new AppComObject()
                {
                    ApplicationId = _model.Id,
                    Id = cref.Id,
                    Text = GetDefaultLang(cref.OverwriteText ? cref.Text : cref.ComObjectObject.Text),
                    FunctionText = GetDefaultLang(cref.OverwriteFunctionText ? cref.FunctionText : cref.ComObjectObject.FunctionText),
                    Number = cref.ComObjectObject.Number,
                    Size = cref.ComObjectObject.ObjectSize,

                    Flag_Communicate = (cref.OverwriteFC ? cref.FlagComm : cref.ComObjectObject.FlagComm) == FlagType.Enabled,
                    Flag_Read = (cref.OverwriteFR ? cref.FlagRead : cref.ComObjectObject.FlagRead) == FlagType.Enabled,
                    Flag_ReadOnInit = (cref.OverwriteFOI ? cref.FlagOnInit : cref.ComObjectObject.FlagOnInit) == FlagType.Enabled,
                    Flag_Transmit = (cref.OverwriteFT ? cref.FlagTrans : cref.ComObjectObject.FlagTrans) == FlagType.Enabled,
                    Flag_Update = (cref.OverwriteFU ? cref.FlagUpdate : cref.ComObjectObject.FlagUpdate) == FlagType.Enabled,
                    Flag_Write = (cref.OverwriteFW ? cref.FlagWrite : cref.ComObjectObject.FlagWrite) == FlagType.Enabled,
                };

                if(args != null)
                {
                    com.Number += int.Parse(args["###coms"]);
                    //TODO replace args in Text and FunctionText
                }

                CheckForBindings(com, cref);

                _context.AppComObjects.Add(com);
            }
        }


        List<IDynChannel> Channels = new List<IDynChannel>();
        Dictionary<int,  Kaenx.DataContext.Import.Values.IValues> values = new Dictionary<int,  Kaenx.DataContext.Import.Values.IValues>();
        List<ComBinding> ComBindings = new List<ComBinding>();
        List<ParamBinding> Bindings = new List<ParamBinding>();
        List<int> defaultComs = new List<int>();

        private void ImportDynamic()
        {
            foreach(Models.Dynamic.IDynItems item in _version.Dynamics[0].Items)
            {
                ParseDynamicItem(item, null, null, new List<ParamCondition>());
            }

            foreach(IDynChannel chan in Channels)
            {
                if(chan.HasAccess)
                    chan.IsVisible = Kaenx.DataContext.Import.FunctionHelper.CheckConditions(chan.Conditions, values);

                foreach(ParameterBlock block in chan.Blocks)
                    CheckConditions(block);
            }
            
            foreach(ComBinding bind in ComBindings)
            {
                if(!defaultComs.Contains(bind.ComId) && Kaenx.DataContext.Import.FunctionHelper.CheckConditions(bind.Conditions, values))
                    defaultComs.Add(bind.ComId);
            }

            _adds.ParamsHelper = Kaenx.DataContext.Import.FunctionHelper.ObjectToByteArray(Channels, true, "Kaenx.DataContext.Import.Dynamic");
            _adds.Bindings = Kaenx.DataContext.Import.FunctionHelper.ObjectToByteArray(Bindings, true);
            _adds.Assignments = Kaenx.DataContext.Import.FunctionHelper.ObjectToByteArray(new List<string>(), true);
            _adds.ComsAll = Kaenx.DataContext.Import.FunctionHelper.ObjectToByteArray(ComBindings, true);
            _adds.ComsDefault = System.Text.Encoding.UTF8.GetBytes(string.Join(',', defaultComs));
        }

        private void CheckConditions(ParameterBlock block)
        {
            if (block.HasAccess)
                block.IsVisible = Kaenx.DataContext.Import.FunctionHelper.CheckConditions(block.Conditions, values);

            foreach(IDynParameter para in block.Parameters)
            {
                if(para.HasAccess)
                    para.IsVisible = Kaenx.DataContext.Import.FunctionHelper.CheckConditions(para.Conditions, values);
            }

            foreach(ParameterBlock bl in block.Blocks)
                CheckConditions(bl);
        }

        private void ParseDynamicItem(Models.Dynamic.IDynItems ditem, IDynChannel dch, ParameterBlock dblock, List<ParamCondition> conds, Dictionary<string, string> args = null)
        {
            switch(ditem)
            {
                case Models.Dynamic.DynChannel chan:
                {
                    ChannelBlock chanb = new ChannelBlock()
                    {
                        Name = chan.Name,
                        Conditions = conds
                    };
                    chanb.Text = GetDefaultLang(chan.Text);

                    if(string.IsNullOrEmpty(chanb.Text))
                    {
                        ParseDynamicItem(new Models.Dynamic.DynChannelIndependent() { Items = chan.Items }, dch, dblock, conds);
                        return;
                    }

                    if(dch == null)
                        Channels.Add(chanb);

                    foreach(Models.Dynamic.IDynItems item in chan.Items)
                        ParseDynamicItem(item, chanb, dblock, conds);
                    break;
                }

                case Models.Dynamic.DynChannelIndependent chani:
                {
                    ChannelIndependentBlock chanb = new ChannelIndependentBlock();
                    Channels.Add(chanb);
                    
                    foreach(Models.Dynamic.IDynItems pb in chani.Items)
                        ParseDynamicItem(pb, chanb, dblock, conds);
                    break;
                }

                case Models.Dynamic.DynParaBlock dpb:
                {
                    ParameterBlock pb = new ParameterBlock() {
                        Id = dpb.Id,
                        Conditions = conds
                    };
                    if(dpb.UseParameterRef)
                    {
                        pb.Text = GetDefaultLang(dpb.ParameterRefObject.OverwriteText ? dpb.ParameterRefObject.Text : dpb.ParameterRefObject.ParameterObject.Text);
                    } else {
                        pb.Text = GetDefaultLang(dpb.Text);
                    }
                    CheckForBindings(pb, dpb);

                    if(dblock == null) dch.Blocks.Add(pb);
                    else dblock.Blocks.Add(pb);

                    foreach(Models.Dynamic.IDynItems item in dpb.Items)
                        ParseDynamicItem(item, dch, pb, conds);
                    break;
                }

                case Models.Dynamic.DynParameter dp:
                {
                    ParseParameter(dp, dblock, conds);
                    break;
                }

                case Models.Dynamic.DynChoose dcho:
                {
                    foreach(Models.Dynamic.DynWhen when in dcho.Items)
                    {
                        List<ParamCondition> conds2 = conds.ToList();
                        conds2.Add(ParseCondition(when, (int)dcho.ParameterRefObject.Id)); //TODO use long
                        foreach(Models.Dynamic.IDynItems ditem2 in when.Items)
                            ParseDynamicItem(ditem2, dch, dblock, conds2);
                    }
                    break;
                }

                case Models.Dynamic.DynComObject dcom:
                {
                    if(conds.Count > 0)
                    {
                        ComBindings.Add(new ComBinding()
                        {
                            ComId = dcom.ComObjectRefObject.Id,
                            Conditions = conds
                        });
                    } else {
                        if(!defaultComs.Contains(dcom.ComObjectRefObject.Id))
                            defaultComs.Add(dcom.ComObjectRefObject.Id);
                    }
                    break;
                }
            
                case Models.Dynamic.DynModule dmod:
                {
                    ParseModule(dmod, dch, dblock, conds);
                    break;
                }
            }
        }

        private ParamCondition ParseCondition(Models.Dynamic.DynWhen test, int sourceId)
        {
            ParamCondition cond = new ParamCondition();
            int tempOut;
            if (test.IsDefault)
            {
                //TODO implement default
                //TODO check if it works
                //check if choose ist ParameterBlock (happens when vd5 gets converted to knxprods)
                /*if(xele.Parent.Parent.Name.LocalName == "ParameterBlock"){
                    string refid = xele.Parent.Attribute("ParamRefId").Value;
                    if(GetAttributeAsString(xele.Parent.Parent, "TextParameterRefId") == refid)
                        break;
                }

                List<string> values = new List<string>();
                IEnumerable<XElement> whens = xele.Parent.Elements();
                foreach (XElement w in whens)
                {
                    if (w == xele)
                        continue;

                    values.AddRange(w.Attribute("test").Value.Split(' '));
                }
                cond.Values = string.Join(",", values);
                cond.Operation = ConditionOperation.Default;

                if (cond.Values == "")
                {
                    continue;
                }*/

                List<string> conds = new List<string>();

                foreach(Models.Dynamic.DynWhen when in (test.Parent as Models.Dynamic.DynChoose).Items)
                {
                    if(when == test) continue;
                    conds.Add(when.Condition);
                }
                cond.Values = string.Join(',', conds);
                cond.Operation = ConditionOperation.Default;
            }
            else if (test.Condition.Contains(" ") == true || int.TryParse(test.Condition, out tempOut))
            {
                cond.Values = string.Join(",", test.Condition.Split(' '));
                cond.Operation = ConditionOperation.IsInValue;
            }
            else if (test.Condition.StartsWith("<") == true)
            {
                if (test.Condition.Contains("="))
                {
                    cond.Operation = ConditionOperation.LowerEqualThan;
                    cond.Values = test.Condition.Substring(2);
                }
                else
                {
                    cond.Operation = ConditionOperation.LowerThan;
                    cond.Values = test.Condition.Substring(1);
                }
            }
            else if (test.Condition.StartsWith(">") == true)
            {
                if (test.Condition.Contains("="))
                {
                    cond.Operation = ConditionOperation.GreatherEqualThan;
                    cond.Values = test.Condition.Substring(2);
                }
                else
                {
                    cond.Operation = ConditionOperation.GreatherThan;
                    cond.Values = test.Condition.Substring(1);
                }
            }
            else if (test.Condition.StartsWith("!=") == true)
            {
                cond.Operation = ConditionOperation.NotEqual;
                cond.Values = test.Condition.Substring(2);
            }
            else if (test.Condition.StartsWith("=") == true)
            {
                cond.Operation = ConditionOperation.Equal;
                cond.Values = test.Condition.Substring(1);
            }
            else {
                //Log.Warning("Unbekanntes when! " + attrs);
                throw new System.Exception("Unbekanntes when! ");
            }

            cond.SourceId = sourceId;
            return cond;
        }


        private void ParseParameter(Models.Dynamic.DynParameter para, ParameterBlock block, List<ParamCondition> conds)
        {
            AppParameter mpara = IdToParameter[(int)para.ParameterRefObject.Id]; //TODO use long
            
            switch(para.ParameterRefObject.ParameterObject.ParameterTypeObject.Type)
            {
                case ParameterTypes.Enum:
                {
                    if(para.ParameterRefObject.ParameterObject.ParameterTypeObject.Enums.Count > 2)
                    {
                        ParamEnum penu = new ParamEnum()
                        {
                            Id = mpara.ParameterId,
                            Text = mpara.Text, //TODO maybe save space and check overwrite again
                            SuffixText = para.ParameterRefObject.ParameterObject.Suffix,
                            HasAccess = mpara.Access != AccessType.None,
                            Value = mpara.Value,
                            Default = mpara.Value,
                            Conditions = conds
                        };
                        foreach(Models.ParameterTypeEnum ptenu  in para.ParameterRefObject.ParameterObject.ParameterTypeObject.Enums)
                            penu.Options.Add(new ParamEnumOption() { Text = GetDefaultLang(ptenu.Text), Value = ptenu.Value.ToString() });
                        block.Parameters.Add(penu);
                    } else {
                        ParamEnumTwo penu = new ParamEnumTwo() {
                            Id = mpara.ParameterId,
                            Text = mpara.Text,
                            HasAccess = mpara.Access != AccessType.None,
                            Value = mpara.Value,
                            Default = mpara.Value,
                            Conditions = conds
                        };
                        penu.Option1 = new ParamEnumOption() {
                            Text = GetDefaultLang(para.ParameterRefObject.ParameterObject.ParameterTypeObject.Enums[0].Text),
                            Value = para.ParameterRefObject.ParameterObject.ParameterTypeObject.Enums[0].Value.ToString()
                        };
                        penu.Option2 = new ParamEnumOption() {
                            Text = GetDefaultLang(para.ParameterRefObject.ParameterObject.ParameterTypeObject.Enums[1].Text),
                            Value = para.ParameterRefObject.ParameterObject.ParameterTypeObject.Enums[1].Value.ToString()
                        };
                        block.Parameters.Add(penu);
                    }
                    break;
                }

                case ParameterTypes.NumberUInt:
                case ParameterTypes.NumberInt:
                {
                    if(para.ParameterRefObject.ParameterObject.ParameterTypeObject.UIHint == "CheckBox")
                    {
                        ParamCheckBox pcheck = new ParamCheckBox() {
                                Id = mpara.ParameterId,
                                Text = mpara.Text,
                                HasAccess = mpara.Access != AccessType.None,
                                Value = mpara.Value,
                                Default = mpara.Value,
                                Conditions = conds,
                        };
                        block.Parameters.Add(pcheck);
                    } else if(para.ParameterRefObject.ParameterObject.ParameterTypeObject.UIHint == "Slider")
                    {
                        ParamSlider pslide = new ParamSlider() {
                                Id = mpara.ParameterId,
                                Text = mpara.Text,
                                HasAccess = mpara.Access != AccessType.None,
                                Value = mpara.Value,
                                Default = mpara.Value,
                                Conditions = conds,
                                Increment = para.ParameterRefObject.ParameterObject.ParameterTypeObject.Increment,
                                Minimum = (int)para.ParameterRefObject.ParameterObject.ParameterTypeObject.Min, //TODO also allo double
                                Maximum = (int)para.ParameterRefObject.ParameterObject.ParameterTypeObject.Max
                        };
                        block.Parameters.Add(pslide);
                    } else {
                        ParamNumber pnum = new ParamNumber() {
                                Id = mpara.ParameterId,
                                Text = mpara.Text,
                                HasAccess = mpara.Access != AccessType.None,
                                Value = mpara.Value,
                                Default = mpara.Value,
                                Conditions = conds,
                                Increment = para.ParameterRefObject.ParameterObject.ParameterTypeObject.Increment,
                                Minimum = (int)para.ParameterRefObject.ParameterObject.ParameterTypeObject.Min, //TODO also allo double
                                Maximum = (int)para.ParameterRefObject.ParameterObject.ParameterTypeObject.Max
                        };
                        block.Parameters.Add(pnum);
                    }
                    break;
                }

                case ParameterTypes.Text:
                {
                    if(mpara.Access == AccessType.Read){
                        ParamTextRead ptext = new ParamTextRead() {
                            Id = mpara.ParameterId,
                            Text = mpara.Text,
                            HasAccess = mpara.Access != AccessType.None,
                            Value = mpara.Value,
                            Conditions = conds,
                        };
                        block.Parameters.Add(ptext);
                    } else {
                        ParamText ptext = new ParamText() {
                            Id = mpara.ParameterId,
                            Text = mpara.Text,
                            HasAccess = mpara.Access != AccessType.None,
                            Value = mpara.Value,
                            Conditions = conds,
                        };
                        block.Parameters.Add(ptext);
                    }
                    break;
                }

                case ParameterTypes.None:
                    break;


                default:
                    throw new System.Exception("Not implemented!");
            }
        }


        private void ParseModule(Models.Dynamic.DynModule mod, IDynChannel dch, ParameterBlock block, List<ParamCondition> conds)
        {
            Models.Dynamic.DynModuleArg argPara = mod.Arguments.Single(a => a.ArgumentId == mod.ModuleObject.ParameterBaseOffsetUId);
            Models.Dynamic.DynModuleArg argComs = mod.Arguments.Single(a => a.ArgumentId == mod.ModuleObject.ComObjectBaseNumberUId);

            Dictionary<string, string> args = new Dictionary<string, string>();

            foreach(Models.Dynamic.DynModuleArg arg in mod.Arguments)
                args.Add(arg.Argument.Name, arg.Value);

            args.Add("###para", argPara.Value);
            args.Add("###coms", argComs.Value);


            long maxId = _version.ParameterRefs.OrderByDescending(p => p.Id).First().Id;
            maxId++;
            args.Add("###lastId", maxId.ToString());

            ImportParameters(mod.ModuleObject, args);


            foreach(Models.Dynamic.IDynItems item in mod.ModuleObject.Dynamics[0].Items)
            {
                ParseDynamicItem(item, dch, block, conds, args);
            }
        }


        private string GetDefaultLang(ObservableCollection<Translation> text)
        {
            return text.Single(t => t.Language.CultureCode == _version.DefaultLanguage).Text;
        }


        public void CheckForBindings(ParameterBlock pb, Models.Dynamic.DynParaBlock dpb)
        {
            pb.Text = CheckForBindings(pb.Text, BindingTypes.ParameterBlock, pb.Id, (dpb.TextRefObject != null ? dpb.TextRefObject.ParameterObject.Id : -1));
        }
        
        public void CheckForBindings(AppComObject com, Models.ComObjectRef dcom)
        {
            com.Text = CheckForBindings(com.Text, BindingTypes.ComObject, com.Id, (dcom.ComObjectObject.UseTextParameter && dcom.ComObjectObject.ParameterRefObject != null) ? (int)dcom.ComObjectObject.ParameterRefObject.Id : -1);
        }
        
        public string CheckForBindings(string text, BindingTypes type, int targetId, int sourceId) //, int targetId, XElement xele, Dictionary<string, string> args, Dictionary<string, int> idMapper) {
        {
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("{{(.*)}}"); //[A-Za-z0-9: -]
            
            if(reg.IsMatch(text)){
                System.Text.RegularExpressions.Match match = reg.Match(text);
                string g2 = match.Groups[1].Value;
                /*if(args != null && args.ContainsKey(g2)) {
                    //Argument von Modul einsetzen
                    return text.Replace(match.Groups[0].Value, args[g2]);
                }*/

                ParamBinding bind = new ParamBinding()
                {
                    Type = type,
                    TargetId = targetId,
                    FullText = text.Replace(match.Groups[0].Value, "{d}")
                };
                //Text beinhaltet ein Binding zu einem Parameter
                
                if(g2.Contains(':')){
                    string[] opts = g2.Split(':');
                    text = text.Replace(match.Groups[0].Value, opts[1]);
                    bind.SourceId = opts[0] == "0" ? -1 : int.Parse(opts[0]);
                    bind.DefaultText = opts[1];
                } else {
                    text = text.Replace(match.Groups[0].Value, "");
                    bind.SourceId = g2 == "0" ? -1 : int.Parse(g2);
                    bind.DefaultText = "";
                }
                
                if(bind.SourceId == -1) {
                    if(sourceId != -1)
                    {
                        bind.SourceId = sourceId;
                    } else
                    {
                        throw new Exception("Object enthält dynamischen Text mit Referenz 0, hat aber kein Attribut mit TextParameterRefId");
                    }
                }
                
                Bindings.Add(bind);
            }
            return text;
        }
    }
}