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
        private string _langCode;

        private Dictionary<string, int> TypeNameToId = new Dictionary<string, int>();
        private Dictionary<long, AppParameter> IdToParameter = new Dictionary<long, AppParameter>();
        
       


        public ImporterCreator(AppVersion version, Application app)
        {
            _version = version;
            _app = app;
            _langCode = _version.DefaultLanguage;
        }


        public void StartImport(CatalogContext context)
        {
            _context = context;
            DoImport();
        }

        public List<string> GetLanguages()
        {
            List<string> langs = new List<string>();
            foreach(Language lang in _version.Languages)
                langs.Add(lang.CultureCode);
            return langs;
        }

        public void SetLanguage(string langCode)
        {
            _langCode = langCode;
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
            ImportParameters(_version, null);
            ImportComObjects(_version, null);

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
    
        private void ImportParameters(IVersionBase vbase, Dictionary<string, string> args)
        {
            foreach(ParameterRef pref in vbase.ParameterRefs)
            {
                AppParameter mpara = new AppParameter()
                {
                    ApplicationId = _model.Id,
                    ParameterId = pref.Id,
                    Text = GetDefaultLang(pref.OverwriteText ? pref.Text : pref.ParameterObject.Text),
                    Value = pref.OverwriteValue ? pref.Value : pref.ParameterObject.Value,
                    SuffixText = GetDefaultLang(pref.ParameterObject.Suffix),
                    Offset = pref.ParameterObject.Offset,
                    OffsetBit = pref.ParameterObject.OffsetBit,
                    ParameterTypeId = TypeNameToId[pref.ParameterObject.ParameterTypeObject.Name]
                };

                if(args != null)
                {
                    //mpara.Offset += int.Parse(args["###para"]);
                }

                ParamAccess paccess = pref.OverwriteAccess ? pref.Access : pref.ParameterObject.Access;

                mpara.Access = paccess switch {
                    ParamAccess.Default => AccessType.Null,
                    ParamAccess.None => AccessType.None,
                    ParamAccess.Read => AccessType.Read,
                    ParamAccess.ReadWrite => AccessType.Full,
                    _ => throw new System.Exception("Unbekannter ParamAccess: " + paccess.ToString())
                };

                if(pref.ParameterObject.SavePath == SavePaths.Memory)
                {
                    mpara.SegmentType = SegmentTypes.Memory;
                    mpara.SegmentId = 0; //TODO get real id
                } else if(pref.ParameterObject.SavePath == SavePaths.Property) {
                    mpara.SegmentType = SegmentTypes.Property;
                    //TODO add info for property (maybe index << 32 | propid)
                } else if(pref.ParameterObject.SavePath == SavePaths.Nowhere) {
                    mpara.SegmentType = SegmentTypes.None;
                } else {
                    throw new System.Exception("ParameterSave " + pref.ParameterObject.SavePath.ToString() + " not supported");
                }

                values.Add(mpara.ParameterId, new Kaenx.DataContext.Import.Values.StandardValues(mpara.Value));
                IdToParameter.Add(mpara.ParameterId, mpara);
                _context.AppParameters.Add(mpara);
            }
        }

        private void ImportComObjects(IVersionBase vbase, Dictionary<string, string> args)
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
                    //com.Number += int.Parse(args["###coms"]);
                }

                CheckForBindings(com, cref, args);

                _context.AppComObjects.Add(com);
            }
        }


        List<IDynChannel> Channels = new List<IDynChannel>();
        Dictionary<long,  Kaenx.DataContext.Import.Values.IValues> values = new Dictionary<long,  Kaenx.DataContext.Import.Values.IValues>();
        List<ComBinding> ComBindings = new List<ComBinding>();
        List<ParamBinding> Bindings = new List<ParamBinding>();
        List<long> defaultComs = new List<long>();

        private void ImportDynamic()
        {
            foreach(Models.Dynamic.IDynItems item in _version.Dynamics[0].Items)
            {
                ParseDynamicItem(item, null, null, new List<ParamCondition>(), null);
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

                    if(para is ParameterTable pt)
                    {
                        foreach(IDynParameter tpara in pt.Parameters)
                        {
                            if(tpara.HasAccess)
                                tpara.IsVisible = Kaenx.DataContext.Import.FunctionHelper.CheckConditions(tpara.Conditions, values);
                        }
                    }
            }

            foreach(ParameterBlock bl in block.Blocks)
                CheckConditions(bl);
        }

        private void ParseDynamicItem(Models.Dynamic.IDynItems ditem, IDynChannel dch, ParameterBlock dblock, List<ParamCondition> conds, Dictionary<string, string> args)
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
                    CheckForBindings(chanb, chan, args);

                    if(string.IsNullOrEmpty(chanb.Text))
                    {
                        ParseDynamicItem(new Models.Dynamic.DynChannelIndependent() { Items = chan.Items }, dch, dblock, conds, args);
                        return;
                    }

                    if(dch == null)
                        Channels.Add(chanb);

                    foreach(Models.Dynamic.IDynItems item in chan.Items)
                        ParseDynamicItem(item, chanb, dblock, conds, args);
                    break;
                }

                case Models.Dynamic.DynChannelIndependent chani:
                {
                    ChannelIndependentBlock chanb = new ChannelIndependentBlock();
                    Channels.Add(chanb);
                    
                    foreach(Models.Dynamic.IDynItems pb in chani.Items)
                        ParseDynamicItem(pb, chanb, dblock, conds, args);
                    break;
                }

                case Models.Dynamic.DynParaBlock dpb:
                {
                    if(args != null)
                        dpb.Id = maxBlockId++;
                    if(dpb.IsInline && dpb.Layout == Models.Dynamic.BlockLayout.Grid)
                    {
                        ParseTable(dpb, dblock, conds, args);
                    } else {
                        ParseBlock(dpb, dch, dblock, conds, args);
                    }
                    break;
                }

                case Models.Dynamic.DynParameter dp:
                {
                    ParseParameter(dp, dblock, conds);
                    break;
                }

                case Models.Dynamic.IDynChoose dcho:
                {
                    foreach(Models.Dynamic.IDynWhen when in dcho.Items)
                    {
                        List<ParamCondition> conds2 = conds.ToList();
                        conds2.Add(ParseCondition(when, dcho.ParameterRefObject.Id));
                        foreach(Models.Dynamic.IDynItems ditem2 in when.Items)
                            ParseDynamicItem(ditem2, dch, dblock, conds2, args);
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

                case Models.Dynamic.DynSeparator ds:
                {
                    ParamSeparator psep = new ParamSeparator() {
                        Id = ds.Id,
                        Text = GetDefaultLang(ds.Text),
                        HasAccess = true,
                        Conditions = conds
                    };
                    dblock.Parameters.Add(psep);
                    break;
                }

                default:
                    throw new Exception("Not Implemented Type: " + ditem.GetType().ToString());
            }
        }

        private void ParseTable(Models.Dynamic.DynParaBlock dpb, ParameterBlock dblock, List<ParamCondition> conds, Dictionary<string, string> args)
        {
            ParameterBlock pb = new ParameterBlock();
                
            foreach(Models.Dynamic.IDynItems item in dpb.Items)
                ParseDynamicItem(item, null, pb, conds, args);

            ParameterTable table = new ParameterTable()
            {
                Id = dpb.Id,
                Conditions = conds,
                Parameters = pb.Parameters
            };
            foreach(Models.Dynamic.ParameterBlockRow row in dpb.Rows)
                table.Rows.Add(1);
            foreach(Models.Dynamic.ParameterBlockColumn col in dpb.Columns)
                table.Columns.Add(col.Width);
            dblock.Parameters.Add(table);
        }

        private void ParseBlock(Models.Dynamic.DynParaBlock dpb, IDynChannel dch, ParameterBlock dblock, List<ParamCondition> conds, Dictionary<string, string> args)
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
            CheckForBindings(pb, dpb, args);

            if(dblock == null) dch.Blocks.Add(pb);
            else dblock.Blocks.Add(pb);

            foreach(Models.Dynamic.IDynItems item in dpb.Items)
                ParseDynamicItem(item, dch, pb, conds, args);

            if(!pb.Parameters.Any(p => p.DisplayOrder == -1))
                pb.Parameters.Sort((a, b) => a.DisplayOrder.CompareTo(b.DisplayOrder));
        }

        private ParamCondition ParseCondition(Models.Dynamic.IDynWhen test, long sourceId)
        {
            ParamCondition cond = new ParamCondition();
            int tempOut;
            if (test.IsDefault)
            {
                //check if choose ist ParameterBlock (happens when vd5 gets converted to knxprods)
                if(test.Parent.Parent is Models.Dynamic.DynParaBlock dpb){
                    if((test.Parent as Models.Dynamic.IDynChoose).ParameterRefObject == dpb.ParameterRefObject)
                        return cond;
                }

                List<string> conds = new List<string>();

                foreach(Models.Dynamic.IDynWhen when in (test.Parent as Models.Dynamic.IDynChoose).Items)
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
            AppParameter mpara = IdToParameter[para.ParameterRefObject.Id];
            
            switch(para.ParameterRefObject.ParameterObject.ParameterTypeObject.Type)
            {
                case ParameterTypes.Enum:
                {
                    if(para.ParameterRefObject.ParameterObject.ParameterTypeObject.Enums.Count != 2)
                    {
                        ParamEnum penu = new ParamEnum()
                        {
                            Id = mpara.ParameterId,
                            Text = mpara.Text, //TODO maybe save space and check overwrite again
                            SuffixText = GetDefaultLang(para.ParameterRefObject.ParameterObject.Suffix),
                            HasAccess = mpara.Access != AccessType.None,
                            Value = mpara.Value,
                            Default = mpara.Value,
                            Conditions = conds,
                            DisplayOrder = para.ParameterRefObject.DisplayOrder
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
                            Conditions = conds,
                            DisplayOrder = para.ParameterRefObject.DisplayOrder
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

                case ParameterTypes.Float_DPT9:
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
                                DisplayOrder = para.ParameterRefObject.DisplayOrder
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
                                Minimum = para.ParameterRefObject.ParameterObject.ParameterTypeObject.Min,
                                Maximum = para.ParameterRefObject.ParameterObject.ParameterTypeObject.Max,
                                DisplayOrder = para.ParameterRefObject.DisplayOrder
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
                                Minimum = para.ParameterRefObject.ParameterObject.ParameterTypeObject.Min,
                                Maximum = para.ParameterRefObject.ParameterObject.ParameterTypeObject.Max,
                                DisplayOrder = para.ParameterRefObject.DisplayOrder
                        };
                        block.Parameters.Add(pnum);
                    }
                    break;
                }

                case ParameterTypes.IpAddress: //TODO check if other control is better with regex
                case ParameterTypes.Text:
                {
                    if(mpara.Access == AccessType.Read){
                        ParamTextRead ptext = new ParamTextRead() {
                            Id = mpara.ParameterId,
                            Text = mpara.Text,
                            HasAccess = mpara.Access != AccessType.None,
                            Value = mpara.Value,
                            Conditions = conds,
                            DisplayOrder = para.ParameterRefObject.DisplayOrder
                        };
                        block.Parameters.Add(ptext);
                    } else {
                        ParamText ptext = new ParamText() {
                            Id = mpara.ParameterId,
                            Text = mpara.Text,
                            HasAccess = mpara.Access != AccessType.None,
                            Value = mpara.Value,
                            Conditions = conds,
                            DisplayOrder = para.ParameterRefObject.DisplayOrder
                        };
                        block.Parameters.Add(ptext);
                    }
                    break;
                }

                case ParameterTypes.Picture:
                {
                    ParamPicture ppic = new ParamPicture(){
                        Id = mpara.ParameterId,
                        BaggageId = para.ParameterRefObject.ParameterObject.ParameterTypeObject.BaggageUId,
                        Text = mpara.Text,
                        HasAccess = mpara.Access != AccessType.None,
                        Value = mpara.Value,
                        Conditions = conds,
                            DisplayOrder = para.ParameterRefObject.DisplayOrder
                    };
                    block.Parameters.Add(ppic);
                    break;
                }

                case ParameterTypes.None:
                    break;

                default:
                    throw new System.Exception("Not implemented! " + para.ParameterRefObject.ParameterObject.ParameterTypeObject.Type);
            }
        }

        long maxParaId = -1;
        long maxComsId = -1;
        int maxBlockId = -1;

        private void ParseModule(Models.Dynamic.DynModule mod, IDynChannel dch, ParameterBlock block, List<ParamCondition> conds)
        {
            Models.Dynamic.DynModuleArg argPara = mod.Arguments.Single(a => a.ArgumentId == mod.ModuleObject.ParameterBaseOffsetUId);
            Models.Dynamic.DynModuleArg argComs = mod.Arguments.Single(a => a.ArgumentId == mod.ModuleObject.ComObjectBaseNumberUId);

            Dictionary<string, string> args = new Dictionary<string, string>();

            foreach(Models.Dynamic.DynModuleArg arg in mod.Arguments)
                args.Add(arg.Argument.Name, arg.Value);

            //args.Add("###para", argPara.Value);
            //args.Add("###coms", argComs.Value);

            if(maxParaId == -1)
            {
                if(_version.ParameterRefs.Count > 0)
                {
                    maxParaId = _version.ParameterRefs.OrderByDescending(p => p.Id).First().Id;
                    maxParaId++;
                } else {
                    maxParaId = 1;
                }
            }

            if(maxComsId == -1)
            {
                if(_version.ComObjectRefs.Count > 0)
                {
                    maxComsId = _version.ComObjectRefs.OrderByDescending(c => c.Id).First().Id;
                    maxComsId++;
                } else {
                    maxComsId = 1;
                }
            }

            if(maxBlockId == -1)
            {
                maxBlockId = FindBiggestBlockId(_version.Dynamics[0], 0);
                maxBlockId++;
            }


            System.Diagnostics.Debug.WriteLine($"Module {mod.Id}: {maxParaId} ParaStart - {maxComsId} ComsStart");
            
            Module nmod = CopyModule(mod.ModuleObject, int.Parse(argPara.Value), int.Parse(argComs.Value));


            ImportParameters(nmod, args);
            ImportComObjects(nmod, args);

            foreach(Models.Dynamic.IDynItems item in nmod.Dynamics[0].Items)
            {
                CopyDynamicItem(item, nmod);
            }

            foreach(Models.Dynamic.IDynItems item in nmod.Dynamics[0].Items)
            {
                ParseDynamicItem(item, dch, block, conds, args);
            }
        }

        private void CopyDynamicItem(Models.Dynamic.IDynItems item, IVersionBase vbase)
        {
            switch(item)
            {
                case Models.Dynamic.DynChannel dc:
                {
                    if(dc.UseTextParameter && dc.ParameterRef != -1)
                        dc.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dc.ParameterRef);
                    break;
                }

                case Models.Dynamic.DynParaBlock dpb:
                {
                    if(dpb.UseParameterRef && dpb.ParameterRef != -1)
                        dpb.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dpb.ParameterRef);
                    if(dpb.UseTextParameter && dpb.TextRef != -1)
                        dpb.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dpb.TextRef);
                    break;
                }

                case Models.Dynamic.DynChooseBlock dch:
                {
                    if(dch.ParameterRef != -1)
                        dch.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dch.ParameterRef);
                    break;
                }

                case Models.Dynamic.DynChooseChannel dch:
                {
                    if(dch.ParameterRef != -1)
                        dch.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dch.ParameterRef);
                    break;
                }

                case Models.Dynamic.DynParameter dp:
                {
                    if(dp.ParameterRef != -1)
                        dp.ParameterRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dp.ParameterRef);
                    break;
                }

                case Models.Dynamic.DynComObject dco:
                {
                    if(dco.ComObjectRef != -1)
                        dco.ComObjectRefObject = vbase.ComObjectRefs.SingleOrDefault(c => c.UId == dco.ComObjectRef);
                    break;
                }

                case Models.Dynamic.DynSeparator dse:
                {
                    if(dse.TextRef != -1)
                        dse.TextRefObject = vbase.ParameterRefs.SingleOrDefault(p => p.UId == dse.TextRef);
                    break;
                }

                case Models.Dynamic.IDynWhen:
                    break;

                default:
                    throw new Exception("Not implemented copy " + item.GetType().ToString());

            }

            if(item.Items == null) return;
            foreach(Models.Dynamic.IDynItems ditem in item.Items)
                CopyDynamicItem(ditem, vbase);
        }

        private int FindBiggestBlockId(Models.Dynamic.IDynItems parent, int current)
        {
            foreach(Models.Dynamic.IDynItems item in parent.Items)
            {
                switch(item)
                {
                    case Models.Dynamic.DynParaBlock pb:
                    {
                        if(pb.Id > current)
                            current = pb.Id;

                        current = FindBiggestBlockId(pb, current);
                        break;
                    }

                    case Models.Dynamic.IDynChannel dch:
                        current = FindBiggestBlockId((Models.Dynamic.IDynItems)dch, current);
                        break;

                    case Models.Dynamic.IDynChoose dc:
                        current = FindBiggestBlockId((Models.Dynamic.IDynItems)dc, current);
                        break;

                    case Models.Dynamic.IDynWhen dw:
                        current = FindBiggestBlockId((Models.Dynamic.IDynItems)dw, current);
                        break;
                }
            }
            return current;
        }

        private Module CopyModule(Module amod, int paraOffset, int comOffset)
        {
            Module bmod = new Module()
            {
                Id = amod.Id,
                Parameters = new ObservableCollection<Parameter>(),
                ParameterRefs = new ObservableCollection<ParameterRef>(),
                ComObjects = new ObservableCollection<ComObject>(),
                ComObjectRefs = new ObservableCollection<ComObjectRef>(),
                Dynamics = new List<Models.Dynamic.IDynamicMain>()
            };

            bmod.Dynamics.Add(new Models.Dynamic.DynamicModule());
            foreach(Parameter para in amod.Parameters)
                bmod.Parameters.Add(para.Copy());
            foreach(ParameterRef pref in amod.ParameterRefs)
                bmod.ParameterRefs.Add(pref.Copy());
            foreach(ComObject com in amod.ComObjects)
                bmod.ComObjects.Add(com.Copy());
            foreach(ComObjectRef cref in amod.ComObjectRefs)
                bmod.ComObjectRefs.Add(cref.Copy());

            foreach(Models.Dynamic.IDynItems item in amod.Dynamics[0].Items)
            {
                bmod.Dynamics[0].Items.Add((Models.Dynamic.IDynItems)item.Copy());
            }

            foreach(Parameter para in bmod.Parameters)
            {
                para.Offset += paraOffset;
            }

            foreach(ParameterRef pref in bmod.ParameterRefs)
            {
                pref.Id = maxParaId++;
            }

            foreach(ComObject com in bmod.ComObjects)
            {
                com.Number += comOffset;
            }

            foreach(ComObjectRef cref in bmod.ComObjectRefs)
            {
                if(cref.ComObject != -1)
                    cref.ComObjectObject = bmod.ComObjects.SingleOrDefault(c => c.UId == cref.ComObject);
                cref.Id = (int)maxComsId++; //TODO change to long
            }


            return bmod;
        }

        private string GetDefaultLang(ObservableCollection<Translation> text)
        {
            if(text == null) return "";
            return text.Single(t => t.Language.CultureCode == _langCode).Text;
        }


        public void CheckForBindings(ChannelBlock cb, Models.Dynamic.DynChannel dpb, Dictionary<string, string> args)
        {
            cb.Text = CheckForBindings(cb.Text, BindingTypes.Channel, cb.Id, (dpb.ParameterRefObject != null ? dpb.ParameterRefObject.ParameterObject.Id : -1), args);
        }
        
        public void CheckForBindings(ParameterBlock pb, Models.Dynamic.DynParaBlock dpb, Dictionary<string, string> args)
        {
            pb.Text = CheckForBindings(pb.Text, BindingTypes.ParameterBlock, pb.Id, (dpb.TextRefObject != null ? dpb.TextRefObject.ParameterObject.Id : -1), args);
        }
        
        public void CheckForBindings(AppComObject com, Models.ComObjectRef dcom, Dictionary<string, string> args)
        {
            com.Text = CheckForBindings(com.Text, BindingTypes.ComObject, com.Id, ((dcom.ComObjectObject.UseTextParameter && dcom.ComObjectObject.ParameterRefObject != null) ? (int)dcom.ComObjectObject.ParameterRefObject.Id : -1), args);
            com.FunctionText = CheckForBindings(com.FunctionText, BindingTypes.ComObject, com.Id, ((dcom.ComObjectObject.UseTextParameter && dcom.ComObjectObject.ParameterRefObject != null) ? (int)dcom.ComObjectObject.ParameterRefObject.Id : -1), args);
        }
        
        public string CheckForBindings(string text, BindingTypes type, long targetId, long sourceId, Dictionary<string, string> args)
        {
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex("{{(.*)}}"); //[A-Za-z0-9: -]
            
            if(reg.IsMatch(text)){
                System.Text.RegularExpressions.Match match = reg.Match(text);
                string g2 = match.Groups[1].Value;
                if(args != null && args.ContainsKey(g2)) {
                    //Argument von Modul einsetzen
                    return text.Replace(match.Groups[0].Value, args[g2]);
                }

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