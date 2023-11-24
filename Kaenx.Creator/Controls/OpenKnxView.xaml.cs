using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Xml.Linq;

namespace Kaenx.Creator.Controls
{
    public partial class OpenKnxView : UserControl
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(OpenKnxView), new PropertyMetadata(null));
        public static readonly DependencyProperty GeneralProperty = DependencyProperty.Register("General", typeof(MainModel), typeof(OpenKnxView), new PropertyMetadata(null));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public MainModel General {
            get { return (MainModel)GetValue(GeneralProperty); }
            set { SetValue(GeneralProperty, value); }
        }

        public OpenKnxView() 
        {
            InitializeComponent();
        }

        private string parameterN { get; set; } = "";

        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            PromptDialog diag = new PromptDialog("URL", "Neues OpenKnx Modul");
            diag.ShowDialog();
            if(string.IsNullOrEmpty(diag.Answer)) return;

            string[] parts = diag.Answer.Split('/');
            if(!parts[4].StartsWith("OFM-") && parts[4] != "OGM-Common")
            {
                MessageBox.Show("Das Repo ist kein OFM!");
                return;
            }

            OpenKnxModule mod = new OpenKnxModule()
            {
                UId = AutoHelper.GetNextFreeUId(Version.OpenKnxModules)
            };

            if(parts.Length > 5)
                mod.Url = string.Join('/', parts.Take(5));
            else 
                mod.Url = diag.Answer;

            mod.Name = mod.Url.Substring(mod.Url.IndexOf('-') + 1);
            mod.Branch = "master";

            if(Version.OpenKnxModules.Any(o => o.Name == mod.Name))
            {
                MessageBox.Show("Es existiert bereits ein OpenKnxModul mit dem Name '" + mod.Name + "'");
                return;
            }
            

            Version.OpenKnxModules.Add(mod);
            OpenKnxList.SelectedItem = mod;
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            OpenKnxModule mod = OpenKnxList.SelectedItem as OpenKnxModule;
            Version.OpenKnxModules.Remove(mod);

            Module xmod = Version.Modules.SingleOrDefault(m => m.IsOpenKnxModule && m.Name == mod.Name + " Part");
            if(xmod != null) Version.Modules.Remove(xmod);
            xmod = Version.Modules.SingleOrDefault(m => m.IsOpenKnxModule && m.Name == mod.Name + " Share");
            if(xmod != null) Version.Modules.Remove(xmod);
            xmod = Version.Modules.SingleOrDefault(m => m.IsOpenKnxModule && m.Name == mod.Name + " Templ");
            if(xmod != null) Version.Modules.Remove(xmod);
        }

        private void ClickUpdate(object sender, RoutedEventArgs e)
        {
            OpenKnxModule mod = OpenKnxList.SelectedItem as OpenKnxModule;
            Update(mod);
        }

        private async void Update(OpenKnxModule mod)
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "OpenKnxModules", mod.Name);
            System.Console.WriteLine(path);

            if(Directory.Exists(path))
                Directory.Delete(path, true);

            Directory.CreateDirectory(path);

            mod.State = "";
            Version.IsModulesActive = true;

            mod.AddState("Branch " + mod.Branch);
            
            HttpClient client = new HttpClient();

            string response = await client.GetStringAsync(mod.Url + "/commits/" + mod.Branch + "/commits_list_item");
            Regex regex = new Regex(" ([0-9a-f]{7})");
            Match m = regex.Match(response);
            mod.Commit = m.Groups[1].Value;

            Stream httpFile = await client.GetStreamAsync(mod.Url + "/archive/refs/heads/" + mod.Branch + ".zip");

            ZipArchive zip = new ZipArchive(httpFile, ZipArchiveMode.Read);

            mod.HasPart = zip.Entries.Any(f => f.Name.EndsWith(".parts.xml"));
            mod.AddState("Has Parts: " + mod.HasPart.ToString());
            mod.HasShare = zip.Entries.Any(f => f.Name.EndsWith(".share.xml"));
            mod.AddState("Has Share: " + mod.HasShare.ToString());
            mod.HasTemplate = zip.Entries.Any(f => f.Name.EndsWith(".templ.xml"));
            mod.AddState("Has Template: " + mod.HasTemplate.ToString());

            AppVersion currentVers = new AppVersion() { DefaultLanguage = "de-DE", ParameterTypes = Version.ParameterTypes };
            foreach(ParameterType type in currentVers.ParameterTypes)
                type.ImportHelperName = "%AID%_PT-" + type.Name;
            currentVers.Memories = Version.Memories;
            currentVers.Languages = Version.Languages;

            MainModel gen = new MainModel();
            gen.Icons = General.Icons;
            
            ImportHelper helper = new ImportHelper();
            helper.SetCurrentVers(currentVers);
            helper.SetGeneral(gen);
            helper.SetDPTs(MainWindow.DPTs);
            mod.NumChannels.Clear();

            if(mod.HasShare)
            {
                ZipArchiveEntry entry = zip.Entries.Single(e => e.Name.EndsWith(".share.xml"));
                XElement xele;

                using(StreamReader reader = new StreamReader(entry.Open()))
                {
                    xele = XElement.Parse(await reader.ReadToEndAsync());
                }
                helper.SetNamespace(xele.Name.NamespaceName);
                ChangeFile(xele, mod);

                if(mod.HasPart)
                {
                    entry = zip.Entries.Single(e => e.Name.EndsWith(".parts.xml"));
                    XElement xele2;

                    using(StreamReader reader = new StreamReader(entry.Open()))
                    {
                        xele2 = XElement.Parse(await reader.ReadToEndAsync());
                    }
                    ChangeFile(xele2, mod);
                    CopyFile(xele, xele2);
                }
            
                DoImport(mod, "Share", xele, helper);
                //Import ParameterTypes, Parameter, ParameterRefs, Coms, ComRefs, Dynamic
            }

            if(mod.HasTemplate)
            {
                ZipArchiveEntry entry = zip.Entries.Single(e => e.Name.EndsWith(".templ.xml"));
                XElement xele;

                using(StreamReader reader = new StreamReader(entry.Open()))
                {
                    xele = XElement.Parse(await reader.ReadToEndAsync());
                }
                helper.SetNamespace(xele.Name.NamespaceName);
                ChangeFile(xele, mod);

                DoImport(mod, "Templ", xele, helper);
                //helper.ImportParameter(xele.Descendants(XName.Get("Parameters", xele.Name.NamespaceName)).ElementAt(0), xmod);
            }

            if(mod.HasPart)
            {
                //await DoImport(mod, "Parts", zip, helper);
                //Import ParameterRefs, Dynamic
            }

            foreach(Module xmod in Version.Modules.Where(m => m.Name.StartsWith(mod.Name)))
            {
                int sum = 0;
                do {
                    ClearResult res = ClearHelper.ShowUnusedElements(xmod);
                    sum = res.ParameterTypes + res.Parameters + res.ParameterRefs + res.Unions + res.ComObjects + res.ComObjectRefs;
                    System.Diagnostics.Debug.WriteLine("Summe: " + sum);
                    ClearHelper.RemoveUnusedElements(xmod);
                } while(sum > 0);
            }

            if(mod.NumChannels.Count > 0)
            {
                List<string> names = new List<string>();
                foreach(OpenKnxNum onum in mod.NumChannels)
                    names.Add(onum.Type.ToString() + " " + onum.UId);
                    
                MessageBox.Show("Folgende Namen dürfen nicht geändert werden:\r\n\r\n" + string.Join("\r\n", names));
            }

            httpFile.Close();
            httpFile.Dispose();
            zip.Dispose();
        }

        private void CopyFile(XElement target, XElement source)
        {
            XElement tStatic = target.Descendants(XName.Get("Static", target.Name.NamespaceName)).ElementAt(0);
            XElement sStatic = source.Descendants(XName.Get("Static", source.Name.NamespaceName)).ElementAt(0);
            XElement tTemp;

            if(sStatic.Elements(XName.Get("Parameters", source.Name.NamespaceName)).Count() == 1)
            {
                tTemp = tStatic.Element(XName.Get("Parameters", source.Name.NamespaceName));
                foreach(XElement sTemp in sStatic.Element(XName.Get("Parameters", source.Name.NamespaceName)).Elements())
                {
                    tTemp.Add(sTemp);
                }
            }

            if(sStatic.Elements(XName.Get("ParameterRefs", source.Name.NamespaceName)).Count() == 1)
            {
                tTemp = tStatic.Element(XName.Get("ParameterRefs", source.Name.NamespaceName));
                foreach(XElement sTemp in sStatic.Element(XName.Get("ParameterRefs", source.Name.NamespaceName)).Elements())
                {
                    tTemp.Add(sTemp);
                }
            }


            if(source.Descendants(XName.Get("Dynamic", source.Name.NamespaceName)).Count() == 1)
            {
                XElement tDynamic = target.Descendants(XName.Get("Dynamic", target.Name.NamespaceName)).ElementAt(0);
                XElement sDynamic = source.Descendants(XName.Get("Dynamic", source.Name.NamespaceName)).ElementAt(0);

                foreach(XElement sDyn in sDynamic.Elements())
                    tDynamic.Add(sDyn);
            }
        }

        private void DoImport(OpenKnxModule mod, string name, XElement xele, ImportHelper helper)
        {
            mod.AddState("Importing " + name);

            string modname = mod.Name + " " + name;
            Module xmod = Version.Modules.SingleOrDefault(m => m.IsOpenKnxModule && m.Name == modname);
            if(xmod == null)
            {
                xmod = CreateModule(modname);
                Version.Modules.Add(xmod);
            } else {
                xmod.Parameters.Clear();
                xmod.ParameterRefs.Clear();
                xmod.ComObjects.Clear();
                xmod.ComObjectRefs.Clear();
                xmod.Unions.Clear();
                xmod.Dynamics[0].Items.Clear();
            }
            
            if(name == "Parts")
            {
                string modname2 = mod.Name + " Share";
                Module xmod2 = Version.Modules.SingleOrDefault(m => m.IsOpenKnxModule && m.Name == modname2);
                foreach(Parameter para in xmod2.Parameters)
                    xmod.Parameters.Add(para.Copy());
                foreach(ParameterRef pref in xmod2.ParameterRefs)
                    xmod.ParameterRefs.Add(pref.Copy());
                foreach(ComObject cref in xmod2.ComObjects)
                    xmod.ComObjects.Add(cref.Copy());
                foreach(ComObjectRef cref in xmod2.ComObjectRefs)
                    xmod.ComObjectRefs.Add(cref.Copy());
                foreach(Union union in xmod2.Unions)
                    xmod.Unions.Add(union.Copy());
                helper.SetParas(xmod.Parameters);
            }

            if(name == "Share")
            {
                if(xele.Descendants(XName.Get("ParameterTypes", xele.Name.NamespaceName)).Count() > 0)
                {
                    mod.AddState("  - ParameterTypes");
                    helper.ImportParameterTypes(xele.Descendants(XName.Get("ParameterTypes", xele.Name.NamespaceName)).ElementAt(0), Version, true);
                }
            }
            var x = xele.Descendants(XName.Get("Parameters", xele.Name.NamespaceName));
            if(x.Count() > 0)
            {
                mod.AddState("  - Parameters");
                helper.ImportParameter(x?.ElementAt(0), xmod);
            } else {
                if(name != "Parts")
                    helper.ImportParameter(null, xmod);
            }

            

            x = xele.Descendants(XName.Get("ParameterRefs", xele.Name.NamespaceName));
            if(x.Count() > 0)
            {
                mod.AddState("  - ParameterRefs");
                helper.ImportParameterRefs(x.ElementAt(0), xmod);
            }
            Dictionary<string, long> idMapper = new Dictionary<string, long>();
            x = xele.Descendants(XName.Get("ComObjectTable", xele.Name.NamespaceName));
            if(x.Count() > 0)
            {
                mod.AddState("  - ComObjects");
                helper.ImportComObjects(x.ElementAt(0), xmod, ref idMapper, false);
            }
            x = xele.Descendants(XName.Get("ComObjectRefs", xele.Name.NamespaceName));
            if(x.Count() > 0)
            {
                mod.AddState("  - ComObjectRefs");
                helper.ImportComObjectRefs(x.ElementAt(0), xmod);
            }
            x = xele.Descendants(XName.Get("Dynamic", xele.Name.NamespaceName));
            if(x.Count() > 0)
            {
                mod.AddState("  - Dynamic");
                helper.ImportDynamic(x.ElementAt(0), xmod);
            }


            if(name == "Parts")
            {
                int uid = 1;
                foreach(Parameter para in xmod.Parameters)
                    para.UId = uid++;
                uid = 1;
                foreach(ParameterRef pref in xmod.ParameterRefs)
                    pref.UId = uid++;
                uid = 1;
                foreach(ComObject cref in xmod.ComObjects)
                    cref.UId = uid++;
                uid = 1;
                foreach(ComObjectRef cref in xmod.ComObjectRefs)
                    cref.UId = uid++;
            }

            if(name == "Share")
            {
                int uid = 1;
                foreach(ParameterType ptype in Version.ParameterTypes)
                    ptype.UId = uid++;
            }
        }

        private void ChangeFile(XElement xroot, OpenKnxModule mod)
        {
            foreach(XElement xrest in xroot.Descendants(XName.Get("TypeNumber", xroot.Name.NamespaceName)))
            {
                if(xrest.Attribute("minInclusive").Value == "%N%")
                {
                    string name = xrest.Parent.Attribute("Name").Value;
                    name = name.Substring(name.LastIndexOf('-') + 1);
                    name = ImportHelper.Unescape(name);
                    OpenKnxNum num = new OpenKnxNum()
                    {
                        UId = name,
                        Property = "Minimum",
                        Type = NumberType.ParameterType
                    };
                    mod.NumChannels.Add(num);
                    xrest.Attribute("minInclusive").Value = "0";
                }
                
                if(xrest.Attribute("maxInclusive").Value == "%N%")
                {
                    string name = xrest.Parent.Attribute("Name").Value;
                    name = name.Substring(name.LastIndexOf('-') + 1);
                    name = ImportHelper.Unescape(name);
                    OpenKnxNum num = new OpenKnxNum()
                    {
                        UId = name,
                        Property = "Maximum",
                        Type = NumberType.ParameterType
                    };
                    mod.NumChannels.Add(num);
                    xrest.Attribute("maxInclusive").Value = "0";
                }
            }
            foreach(XElement xrest in xroot.Descendants(XName.Get("TypeRestriction", xroot.Name.NamespaceName)))
            {
                foreach(XElement xenu in xrest.Elements())
                {
                    xenu.Attribute("Id").Value = "_EN-" + xenu.Attribute("Value").Value;
                }
            }
            foreach(XElement xrest in xroot.Descendants(XName.Get("TypePicture", xroot.Name.NamespaceName)))
            {
                XElement xbaggage = xroot.Descendants(XName.Get("Baggage", xroot.Name.NamespaceName)).Single(b => b.Attribute("Id").Value == xrest.Attribute("RefId").Value);
                xrest.Attribute("RefId").Value = $"M-00FA_BG-{ExportHelper.GetEncoded(xbaggage.Attribute("TargetPath").Value)}-{ExportHelper.GetEncoded(xbaggage.Attribute("Name").Value)}";
                xbaggage.Attribute("Id").Value = xrest.Attribute("RefId").Value;
            }

            Regex regex = new Regex("%[A-Z]+%");
            List<XElement> elements = new List<XElement>();
            elements.AddRange(xroot.Descendants(XName.Get("Parameter", xroot.Name.NamespaceName)));
            elements.AddRange(xroot.Descendants(XName.Get("ParameterRef", xroot.Name.NamespaceName)));
            elements.AddRange(xroot.Descendants(XName.Get("ComObject", xroot.Name.NamespaceName)));
            elements.AddRange(xroot.Descendants(XName.Get("ComObjectRef", xroot.Name.NamespaceName)));
            //Dynamic
            elements.AddRange(xroot.Descendants(XName.Get("ParameterRefRef", xroot.Name.NamespaceName)));
            elements.AddRange(xroot.Descendants(XName.Get("ComObjectRefRef", xroot.Name.NamespaceName)));
            elements.AddRange(xroot.Descendants(XName.Get("choose", xroot.Name.NamespaceName)));
            elements.AddRange(xroot.Descendants(XName.Get("when", xroot.Name.NamespaceName)));
            elements.AddRange(xroot.Descendants(XName.Get("Assign", xroot.Name.NamespaceName)));

            foreach(XElement xele in elements)
            {
                if(xele.Attribute("Value") != null && xele.Attribute("Value").Value == "%N%")
                {
                    OpenKnxNum num = new OpenKnxNum()
                    {
                        UId = xele.Attribute("Name").Value,
                        Property = "Value",
                        Type = NumberType.Parameter
                    };
                    mod.NumChannels.Add(num);
                    xele.Attribute("Value").Value = "0";
                }
                if(xele.Attribute("Id") != null)
                    xele.Attribute("Id").Value = regex.Replace(xele.Attribute("Id").Value, "1");
                if(xele.Attribute("Name") != null)
                {
                    xele.Attribute("Name").Value = regex.Replace(xele.Attribute("Name").Value, "");
                }
                if(xele.Attribute("RefId") != null)
                    xele.Attribute("RefId").Value = regex.Replace(xele.Attribute("RefId").Value, "1");
                if(xele.Attribute("ParamRefId") != null)
                    xele.Attribute("ParamRefId").Value = regex.Replace(xele.Attribute("ParamRefId").Value, "1");
                if(xele.Attribute("TextParameterRefId") != null)
                    xele.Attribute("TextParameterRefId").Value = regex.Replace(xele.Attribute("TextParameterRefId").Value, "1");
                if(xele.Attribute("TargetParamRefRef") != null)
                    xele.Attribute("TargetParamRefRef").Value = regex.Replace(xele.Attribute("TargetParamRefRef").Value, "1");
                if(xele.Attribute("SourceParamRefRef") != null)
                    xele.Attribute("SourceParamRefRef").Value = regex.Replace(xele.Attribute("SourceParamRefRef").Value, "1");

                if(xele.Attribute("FunctionText") != null)
                    xele.Attribute("FunctionText").Value = regex.Replace(xele.Attribute("FunctionText").Value, "{{argChan}}");
                if(xele.Attribute("test") != null)
                {
                    Match m = regex.Match(xele.Attribute("test").Value);
                    if(m.Success)
                    {
                        string xwhen = xele.ToString();
                        if(xwhen.Length > 300)
                            xwhen = xwhen.Substring(0, 100);
                        MessageBox.Show("Test darf kein '%xxx%' enthalten und wird daher nicht importiert.\r\n\r\n" + xwhen);
                        xele.Remove();
                    }
                }
                
                    
                if(xele.Attribute("Number") != null)
                {
                    Regex reg = new Regex("%K([0-9]+)%");
                    Match m = reg.Match(xele.Attribute("Number").Value);
                    if(m.Success)
                    {
                        xele.Attribute("Number").Value = reg.Replace(xele.Attribute("Number").Value, m.Groups[1].Value);
                    }
                }
            }

            int counter = 1;
            foreach(XElement xblock in xroot.Descendants(XName.Get("ParameterBlock", xroot.Name.NamespaceName)))
            {
                xblock.Attribute("Id").Value = "_PB-" + counter++;
                if(xblock.Attribute("TextParameterRefId") != null)
                    xblock.Attribute("TextParameterRefId").Value = regex.Replace(xblock.Attribute("TextParameterRefId").Value, "1");
                if(xblock.Attribute("Name") != null)
                    xblock.Attribute("Name").Value = regex.Replace(xblock.Attribute("Name").Value, "");
                if(xblock.Attribute("Text") != null)
                    xblock.Attribute("Text").Value = regex.Replace(xblock.Attribute("Text").Value, "{{argChan}}");
            }
            counter = 1;
            foreach(XElement xblock in xroot.Descendants(XName.Get("Channel", xroot.Name.NamespaceName)))
            {
                xblock.Attribute("Id").Value = "_CH-" + counter++;
                if(xblock.Attribute("TextParameterRefId") != null)
                    xblock.Attribute("TextParameterRefId").Value = regex.Replace(xblock.Attribute("TextParameterRefId").Value, "1");
                if(xblock.Attribute("Name") != null)
                    xblock.Attribute("Name").Value = regex.Replace(xblock.Attribute("Name").Value, "");
                if(xblock.Attribute("Number") != null)
                {
                    Regex regex2 = new Regex("%[C]+%");
                    xblock.Attribute("Number").Value = regex2.Replace(xblock.Attribute("Name").Value, "{{argChan}}");
                }
                if(xblock.Attribute("Text") != null)
                {
                    Regex regex2 = new Regex("%[C]+%");
                    xblock.Attribute("Text").Value = regex2.Replace(xblock.Attribute("Text").Value, "{{argChan}}");
                }

            }
            counter = 1;
            foreach(XElement xblock in xroot.Descendants(XName.Get("ParameterSeparator", xroot.Name.NamespaceName)))
            {
                xblock.Attribute("Id").Value = "_PS-" + counter++;
            }
        }

        private Module CreateModule(string name)
        {
            Models.Module mod = new Models.Module() { 
                Name = name, 
                UId = AutoHelper.GetNextFreeUId(Version.Modules), 
                IsOpenKnxModule = true,
                IsParameterRefAuto = false,
                IsComObjectRefAuto = false
            };
            mod.Arguments.Add(new Models.Argument() { Name = "argParas", UId = AutoHelper.GetNextFreeUId(mod.Arguments) });
            mod.Arguments.Add(new Models.Argument() { Name = "argComs", UId = AutoHelper.GetNextFreeUId(mod.Arguments) });
            mod.Arguments.Add(new Models.Argument() { Name = "argChan", UId = AutoHelper.GetNextFreeUId(mod.Arguments) });
            mod.ParameterBaseOffset = mod.Arguments[0];
            mod.ComObjectBaseNumber = mod.Arguments[1];
            mod.Dynamics.Add(new Models.Dynamic.DynamicModule());
            return mod;
        }

        CancellationTokenSource source = null;
        string lastLine = "";

        private async Task WaitOutput(Process p, string command = null, int timeout = 10000)
        {
            if(command != null)
                p.StandardInput.WriteLine(command);

            source = new CancellationTokenSource();
            try{
                await Task.Delay(timeout, source.Token);
            } catch {}
            source = null;
        }

        private async Task<string> GetOutput(Process p, string command = null, int timeout = 10000)
        {
            if(command != null)
                p.StandardInput.WriteLine(command);

            source = new CancellationTokenSource();
            try{
                await Task.Delay(timeout, source.Token);
            } catch {}
            source = null;
            return lastLine;
        }

        private async Task GetOutput(Process p)
        {
            try{
                while(!p.HasExited)
                {
                    string x = await p.StandardOutput.ReadLineAsync();

                    if(string.IsNullOrEmpty(x))
                    {
                        if(source != null)
                            source.Cancel();
                    }
                    else
                    {
                        lastLine = x;
                    }

                    Console.WriteLine(x);
                }
            } catch{}
            System.Console.WriteLine("Exited");
        }
        
        private async Task GetOutput2(Process p)
        {
            try{
                while(!p.HasExited)
                {
                    string x = await p.StandardError.ReadLineAsync();
                    Console.WriteLine(x);
                    if(string.IsNullOrEmpty(x))
                        throw new Exception("Fatal error");
                }
            } catch{}
            System.Console.WriteLine("Exited2");
        }
    }
}