using Kaenx.Creator.Models;
using Kaenx.DataContext.Catalog;
using System.Collections.Generic;
using System.Linq;

namespace Kaenx.Creator.Viewer
{
    public class ImporterCreator : IImporter
    {
        private Application _app;
        private AppVersion _version;
        private CatalogContext _context;
        private ApplicationViewModel _model;

        private Dictionary<string, int> TypeNameToId = new Dictionary<string, int>();

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

            ImportParameterTypes();
            ImportParameters();
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
    
        private void ImportParameters()
        {
            foreach(ParameterRef pref in _version.ParameterRefs)
            {
                AppParameter mpara = new AppParameter()
                {
                    ApplicationId = _model.Id,
                    ParameterId = (int)pref.Id, //TODO caution creator uses long, viewer only int!
                    Text = (pref.OverwriteText ? pref.Text : pref.ParameterObject.Text).Single(t => t.Language.CultureCode == _version.DefaultLanguage).Text,
                    Value = pref.OverwriteValue ? pref.Value : pref.ParameterObject.Value,
                    SuffixText = pref.ParameterObject.Suffix,
                    Offset = pref.ParameterObject.Offset,
                    OffsetBit = pref.ParameterObject.OffsetBit,
                    ParameterTypeId = TypeNameToId[pref.ParameterObject.ParameterTypeObject.Name]
                };

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


                _context.AppParameters.Add(mpara);
            }
        }
    }
}