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
        private ObservableCollection<Models.MaskVersion> _bcus;
        private ZipArchive Archive { get; set; }
        private Models.ModelGeneral _general;
        private string _path;
        
        private Models.Application currentApp = null;
        private Models.AppVersion currentVers = null;

        public ImportHelper(string path, ObservableCollection<Models.MaskVersion> bcus) {
            _path = path;
            _bcus = bcus;
        }

        public void Start(Models.ModelGeneral general) {
            _general = general;
            Archive = ZipFile.OpenRead(_path);

            foreach (ZipArchiveEntry entryTemp in Archive.Entries)
            {
                if(entryTemp.FullName.Contains("M-")) {
                    int manuId = int.Parse(entryTemp.FullName.Substring(2,4), System.Globalization.NumberStyles.HexNumber);
                    if(_general.ManufacturerId == -1) {
                        _general.ManufacturerId = manuId;
                    } else if(_general.ManufacturerId != manuId) {
                        if (System.Windows.MessageBox.Show("Hersteller der Produktdatenbank stimmt nicht mit dem Hersteller des Projekts über ein.\r\nSoll trotzdem importiert werden?", "Question", System.Windows.MessageBoxButton.YesNo, System.Windows.MessageBoxImage.Warning) == System.Windows.MessageBoxResult.No) {
                            Archive.Dispose();
                            return;
                        }
                        break;
                    }
                }
            }
            
            foreach (ZipArchiveEntry entryTemp in Archive.Entries)
            {
                if (entryTemp.FullName.Contains("_A-"))
                {
                    using (Stream entryStream = entryTemp.Open())
                    {
                        XDocument xdoc = XDocument.Load(entryStream);
                        _namespace = xdoc.Root.Attribute("xmlns").Value;
                        XElement xapp = xdoc.Root.Element(GetXName("ManufacturerData")).Element(GetXName("Manufacturer")).Element(GetXName("ApplicationPrograms")).Element(GetXName("ApplicationProgram"));
                        ImportApplication(xapp);
                    }
                }
            }
        }

        public void ImportApplication(XElement xapp) {
#region "Create/Get Application and Version"
            currentApp = null;
            currentVers = null;
            int appNumber = int.Parse(xapp.Attribute("ApplicationNumber").Value);
            int versNumber = int.Parse(xapp.Attribute("ApplicationVersion").Value);

            foreach(Models.Application app in _general.Applications) {
                if(app.Number == appNumber) {
                    currentApp = app;
                    break;
                }
            }

            if(currentApp == null) {
                currentApp = new Models.Application() {
                    Number = appNumber,
                    Name = xapp.Attribute("Name").Value,
                    Mask = _bcus.Single(b => b.Id == xapp.Attribute("MaskVersion").Value)
                };
                _general.Applications.Add(currentApp);
            }

            foreach(Models.AppVersion vers in currentApp.Versions) {
                if(vers.Number == versNumber) {
                    currentVers = vers;
                    break;
                }
            }

            if(currentVers == null) {
                currentVers = new Models.AppVersion() {
                    Number = versNumber,
                    Name = "Imported",
                    IsParameterRefAuto = false,
                    IsComObjectRefAuto = false,
                    IsMemSizeAuto = false
                };
                currentApp.Versions.Add(currentVers);
            }
#endregion
            XElement xstatic = xapp.Element(GetXName("Static"));
            ImportSegments(xstatic.Element(GetXName("Code")));
            ImportParameterTypes(xstatic.Element(GetXName("ParameterTypes")));
            ImportParameter(xstatic.Element(GetXName("Parameters")));
            ImportParameterRefs(xstatic.Element(GetXName("ParameterRefs")));
            ImportComObjects(xstatic.Element(GetXName("ComObjectTable")));
        }

        public void ImportSegments(XElement xcodes) {
            foreach(XElement xcode in xcodes.Elements()) {
                if(xcode.Name.LocalName == "AbsoluteSegment") {
                    currentVers.Memories.Add(new Models.Memory() {
                        Address = int.Parse(xcode.Attribute("Address").Value),
                        Size = int.Parse(xcode.Attribute("Size").Value),
                        Name = GetLastSplit(xcode.Attribute("Id").Value) +  " " + (xcode.Attribute("Name")?.Value ?? "Unnamed"),
                        Type = MemoryTypes.Absolute,
                        IsAutoSize = false,
                        IsAutoPara = false
                    });
                } else if(xcode.Name.LocalName == "RelativeSegment") {
                    currentVers.Memories.Add(new Models.Memory() {
                        Size = int.Parse(xcode.Attribute("Size").Value),
                        Offset = int.Parse(xcode.Attribute("Offset")?.Value ?? "0"),
                        Name = GetLastSplit(xcode.Attribute("Id").Value) + (xcode.Attribute("Name").Value ?? ""),
                        Type = MemoryTypes.Relative,
                        IsAutoSize = false,
                        IsAutoPara = false
                    });
                } else {
                    throw new Exception("Masks Memory Type is not supported! " + currentApp.Mask.Memory);
                }
            }
        }

        public void ImportParameterTypes(XElement xparatypes) {
            foreach(XElement xparatype in xparatypes.Elements()) {
                Models.ParameterType ptype = new Models.ParameterType() {
                    Name = xparatype.Attribute("Name").Value,
                    IsSizeAuto = false
                };

                XElement xsub = xparatype.Elements().ElementAt(0);
                switch(xsub.Name.LocalName) {
                    case "TypeNone":
                        ptype.Type = ParameterTypes.None;
                        break;

                    case "TypeNumber":
                        ptype.Type = xsub.Attribute("Type").Value switch {
                            "unsignedInt" => ParameterTypes.NumberUInt,
                            "signedInt" => ParameterTypes.NumberInt,
                            "float9" => ParameterTypes.Float9,
                            _ => throw new Exception("Unbekannter TypeNumber Type: " + xsub.Attribute("Type").Value)
                        };
                        ptype.SizeInBit = int.Parse(xsub.Attribute("SizeInBit").Value);
                        ptype.Min = int.Parse(xsub.Attribute("minInclusive").Value);
                        ptype.Max = int.Parse(xsub.Attribute("maxInclusive").Value);
                        break;

                    case "TypeRestriction":
                        ptype.Type = ParameterTypes.Enum;
                        ptype.SizeInBit = int.Parse(xsub.Attribute("SizeInBit").Value);
                        foreach(XElement xenum in xsub.Elements()) {
                            ptype.Enums.Add(new Models.ParameterTypeEnum() {
                                Name = xenum.Attribute("Text").Value,
                                Value = int.Parse(xenum.Attribute("Value").Value)
                            });
                        }
                        break;

                    case "TypeText":
                        ptype.Type = ParameterTypes.Text;
                        ptype.SizeInBit = int.Parse(xsub.Attribute("SizeInBit").Value);
                        break;

                    case "TypeIPAddress":
                        ptype.Type = ParameterTypes.IpAddress;
                        //TODO read if ipv4 or ipv6
                        break;

                    default:
                        throw new Exception("Unbekannter ParameterType: " + xsub.Name.LocalName);
                }

                currentVers.ParameterTypes.Add(ptype);
            }
        }

        public void ImportParameter(XElement xparas) {
            //TODO also import unions!
            foreach(XElement xpara in xparas.Elements(GetXName("Parameter"))) {
                ParseParameter(xpara, null);
            }

            int unionId = 1;
            foreach(XElement xunion in xparas.Elements(GetXName("Union"))) {
                XElement xmem = xunion.Elements().ElementAt(0);
                foreach(XElement xpara in xunion.Elements(GetXName("Parameter"))) {
                    ParseParameter(xpara, xmem);
                }
            }
        }

        public void ParseParameter(XElement xpara, XElement xmemory) {
            Models.Parameter para = new Models.Parameter() {
                Name = xpara.Attribute("Name").Value,
                Text = xpara.Attribute("Text").Value,
                Value = xpara.Attribute("Value").Value,
                IsOffsetAuto = false,
                Suffix = xpara.Attribute("SuffixText")?.Value ?? "",
                IsInMemory = false
            };
            string id = GetLastSplit(xpara.Attribute("Id").Value, 2);
            if(id.StartsWith("-"))
                id = id.Substring(1);
            para.Id = int.Parse(id);

            para.Access = (xpara.Attribute("Access")?.Value ?? "ReadWrite") switch {
                "None" => ParamAccess.None,
                "Read" => ParamAccess.Read,
                "ReadWrite" => ParamAccess.ReadWrite,
                _ => throw new Exception("Unbekannter AccesType für Parameter: " + xpara.Attribute("Access").Value)
            };

            string typeName = Unescape(GetLastSplit(xpara.Attribute("ParameterType").Value, 3));
            para.ParameterTypeObject = currentVers.ParameterTypes.Single(t => t.Name == typeName);

            if(xmemory != null || xpara.Elements().Count() > 0) {
                XElement xmem = xmemory ?? xpara.Elements().ElementAt(0);
                para.IsInMemory = true;
                if(xmem.Name.LocalName == "Memory") {
                    string memName = GetLastSplit(xmem.Attribute("CodeSegment").Value);
                    para.MemoryObject = currentVers.Memories.Single(m => m.Name.StartsWith(memName));
                    para.Offset = int.Parse(xmem.Attribute("Offset").Value);
                    para.OffsetBit = int.Parse(xmem.Attribute("BitOffset").Value);
                } else {
                    throw new Exception("Unbekannter MemoryTyp für Parameter: " + xmem.Name.LocalName);
                }
            }

            currentVers.Parameters.Add(para);
        }

        public void ImportParameterRefs(XElement xrefs) {
            foreach(XElement xref in xrefs.Elements()) {
                //TODO also import DisplayOrder and Tag
                Models.ParameterRef pref = new Models.ParameterRef();

                pref.Id = int.Parse(GetLastSplit(xref.Attribute("Id").Value, 2));
                
                pref.Value = xref.Attribute("Value")?.Value ?? "";
                pref.Access = xref.Attribute("Access")?.Value switch {
                    "None" => ParamAccess.None,
                    "Read" => ParamAccess.Read,
                    "ReadWrite" => ParamAccess.ReadWrite,
                    null => ParamAccess.Default,
                    _ => throw new Exception("Unbekannter Access Typ für ParameterRef: " + xref.Attribute("Access")?.Value)
                };
                string id = GetLastSplit(xref.Attribute("RefId").Value, 2);
                if(id.StartsWith("-"))
                    id = id.Substring(1);
                int paraId = int.Parse(id);
                pref.ParameterObject = currentVers.Parameters.Single(p => p.Id == paraId);
                pref.Name = pref.Id + " " + pref.ParameterObject.Name;

                currentVers.ParameterRefs.Add(pref);
            }
        }

        public void ImportComObjects(XElement xcoms) {
            foreach(XElement xcom in xcoms.Elements()) {
                Models.ComObject com = new Models.ComObject() {
                    Name = xcom.Attribute("Name")?.Value ?? "",
                    Text = xcom.Attribute("Text")?.Value ?? "",
                    FunctionText = xcom.Attribute("FunctionText")?.Value ?? "",
                    Description = xcom.Attribute("VisibleDescription")?.Value ?? "",
                    Number = int.Parse(xcom.Attribute("Number").Value)
                };

                com.FlagRead = ParseFlagType(xcom.Attribute("ReadFlag")?.Value);
                com.FlagWrite = ParseFlagType(xcom.Attribute("WriteFlag")?.Value);
                com.FlagComm = ParseFlagType(xcom.Attribute("CommunicationFlag")?.Value);
                com.FlagTrans = ParseFlagType(xcom.Attribute("TransmitFlag")?.Value);
                com.FlagUpdate = ParseFlagType(xcom.Attribute("UpdateFlag")?.Value);
                com.FlagOnInit = ParseFlagType(xcom.Attribute("ReadOnInitFlag")?.Value);

                string type = xcom.Attribute("DatapointType")?.Value;

                if(type != null) {
                    if(type.StartsWith("DPST-")) {
                        string[] xtype = type.Split("-");
                        com.TypeParentValue = xtype[1];
                        com.TypeValue = xtype[2];
                        com.HasSub = true;
                    } else if(type.StartsWith("DPT-")) {
                        string[] xtype = type.Split("-");
                        com.TypeParentValue = xtype[1];
                        com.HasSub = false;
                    } else {
                        throw new Exception("Unbekanntes DPT Format für KO: " + type);
                    }
                } else {

                }

                currentVers.ComObjects.Add(com);
            }
        }

        public FlagType ParseFlagType(string type) {
            return type switch {
                    "Enabled" => FlagType.Enabled,
                    "Disabled" => FlagType.Disabled,
                    null => FlagType.Default,
                    _ => throw new Exception("Unbekannter FlagTyp: " + type)
                };
        }

        public string Unescape(string input) {
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

            input = input.Replace(".2E", ".");
            return input;
        }

        public string GetLastSplit(string input, int offset = 0) {
            return input.Substring(input.LastIndexOf('_')+1+offset);
        }

        public XName GetXName(string name) {
            return XName.Get(name, _namespace);
        }
    }
}