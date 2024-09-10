using Kaenx.Creator.Classes;
using Kaenx.Creator.Viewer;
using Kaenx.DataContext.Catalog;
using Kaenx.DataContext.Import;
using Kaenx.DataContext.Import.Dynamic;
using Kaenx.DataContext.Import.Values;
using Kaenx.DataContext.Local;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Kaenx.Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class ViewerWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private IImporter _importer;

        private ApplicationViewModel _app;


        private Dictionary<long, IValues> values = new Dictionary<long, IValues>();
        private Dictionary<string, Kaenx.DataContext.Catalog.Baggage> Baggages = new Dictionary<string, Kaenx.DataContext.Catalog.Baggage>();
        private List<ComBinding> _comBindings;
        private List<AppComObject> _comObjects;
        private List<AssignParameter> Assignments;
        private List<ParamBinding> Bindings;
        private List<IDynParameter> Parameters = new List<IDynParameter>();

        public ObservableCollection<IDynChannel> Channels { get; set; }
        public ObservableCollection<DeviceComObject> ComObjects { get; set; } = new ObservableCollection<DeviceComObject>();



        private ParameterBlock _selectedBlock;
        public ParameterBlock SelectedBlock
        {
            get { return _selectedBlock; }
            set
            {
                if (value == null) return;
                _selectedBlock = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedBlock"));
            }
        }

        private long _selectedBlockId;
        public long SelectedBlockId
        {
            get { return _selectedBlockId; }
            set
            {
                _selectedBlockId = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("SelectedBlockId"));
            }
        }

        private string _helptext;
        public string Helptext
        {
            get { return _helptext; }
            set
            {
                _helptext = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Helptext"));
            }
        }



        public ViewerWindow(IImporter importer)
        {
            InitializeComponent();
            this.DataContext = this;
            _importer = importer;

            Load();
        }

        private async void Load()
        {
            AppAdditional adds;
            string comsDefault;
            using (CatalogContext context = new CatalogContext(new LocalConnectionCatalog() { Type = LocalConnectionCatalog.DbConnectionType.Memory }))
            {
                context.Database.Migrate();
                context.AppAdditionals.RemoveRange(context.AppAdditionals.ToList());
                context.AppComObjects.RemoveRange(context.AppComObjects.ToList());
                context.Applications.RemoveRange(context.Applications.ToList());
                context.AppParameters.RemoveRange(context.AppParameters.ToList());
                context.AppParameterTypeEnums.RemoveRange(context.AppParameterTypeEnums.ToList());
                context.AppParameterTypes.RemoveRange(context.AppParameterTypes.ToList());
                context.AppSegments.RemoveRange(context.AppSegments.ToList());
                context.Hardware2App.RemoveRange(context.Hardware2App.ToList());
                context.Sections.RemoveRange(context.Sections.ToList());
                context.Devices.RemoveRange(context.Devices.ToList());
                context.Baggages.RemoveRange(context.Baggages.ToList());

                var x = _importer.GetLanguages();
                string threadCulture = System.Threading.Thread.CurrentThread.CurrentUICulture.IetfLanguageTag;
                if(System.Threading.Thread.CurrentThread.CurrentUICulture.IsNeutralCulture)
                {
                    if(x.Any(l => l.StartsWith(threadCulture + "-")))
                    {
                        string lang = x.First(l => l.StartsWith(threadCulture + "-"));
                        _importer.SetLanguage(lang);
                        System.Diagnostics.Debug.WriteLine("Importing Language: " + lang);
                    }
                } else {
                    if(x.Contains(threadCulture))
                    {
                        _importer.SetLanguage(threadCulture);
                        System.Diagnostics.Debug.WriteLine("Importing Language: " + threadCulture);
                    }
                }
                await System.Threading.Tasks.Task.Run(() => _importer.StartImport(context)).WaitAsync(TimeSpan.FromMinutes(60));

                _app = context.Applications.First();

                foreach(var y in context.Baggages)
                {
                    Baggages.Add(y.Id, y);
                }

                adds = context.AppAdditionals.Single(a => a.ApplicationId == _app.Id);
                _comBindings = FunctionHelper.ByteArrayToObject<List<ComBinding>>(adds.ComsAll, true);
                Channels = FunctionHelper.ByteArrayToObject<ObservableCollection<IDynChannel>>(adds.ParamsHelper, true, "Kaenx.DataContext.Import.Dynamic");
                comsDefault = System.Text.Encoding.UTF8.GetString(adds.ComsDefault);
                _comObjects = context.AppComObjects.Where(c => c.ApplicationId == _app.Id).ToList();
            }

            if (!string.IsNullOrEmpty(comsDefault))
            {
                foreach (string comIdStr in comsDefault.Split(','))
                {
                    int comId = int.Parse(comIdStr);
                    AppComObject comObj = _comObjects.Single(c => c.Id == comId);
                    ComObjects.Add(new DeviceComObject(comObj));
                }
                comsDefault = null;
            }

            Bindings = FunctionHelper.ByteArrayToObject<List<ParamBinding>>(adds.Bindings, true);
            Assignments = FunctionHelper.ByteArrayToObject<List<AssignParameter>>(adds.Assignments, true);



            foreach (IDynChannel ch in Channels)
            {
                if (!ch.HasAccess)
                {
                    ch.IsVisible = false;
                    continue;
                }

                foreach (ParameterBlock block in ch.Blocks)
                {
                    if (!block.HasAccess)
                    {
                        block.IsVisible = false;
                        continue;
                    }

                    foreach (IDynParameter para in block.Parameters)
                    {
                        if (!para.HasAccess)
                        {
                            para.IsVisible = false;
                            continue;
                        }
                    }
                }
            }

            foreach (IDynChannel ch in Channels)
            {
                foreach (ParameterBlock block in ch.Blocks)
                {
                    foreach (IDynParameter para in block.Parameters)
                    {
                        para.PropertyChanged += Para_PropertyChanged;
                        Parameters.Add(para);

                        if (para is ParamSeparator || para is ParamSeparatorBox) continue;

                        if (!values.ContainsKey(para.Id))
                            values.Add(para.Id, new ParameterValues());

                        ((ParameterValues)values[para.Id]).Parameters.Add(para);
                        if (para is ParamPicture pc)
                        {
                            pc.OnPictureRequest += Pc_OnPictureRequest;
                        }

                        if(para is ParameterTable pt)
                        {
                            foreach(IDynParameter tpara in pt.Parameters)
                            {
                                tpara.PropertyChanged += Para_PropertyChanged;
                                Parameters.Add(para);

                                if (tpara is ParamSeparator || tpara is ParamSeparatorBox) continue;
                                if (!values.ContainsKey(tpara.Id))
                                    values.Add(tpara.Id, new ParameterValues());

                                ((ParameterValues)values[tpara.Id]).Parameters.Add(tpara);
                                if (tpara is ParamPicture pc2)
                                {
                                    pc2.OnPictureRequest += Pc_OnPictureRequest;
                                }
                            }
                        }
                    }
                }
            }

            foreach (AssignParameter assign in Assignments)
            {
                ParameterValues val = (ParameterValues)values[assign.Target];
                if (FunctionHelper.CheckConditions(assign.Conditions, values))
                    val.Assignment = assign;
                else
                {
                    if (val.Assignment == assign)
                        val.Assignment = null;
                }
            }

            Changed("ComObjects");
            Changed("Channels");
        }

        private object Pc_OnPictureRequest(string BaggageId)
        {
            Kaenx.DataContext.Catalog.Baggage bag = Baggages[BaggageId];
            BitmapImage image = new BitmapImage();
            MemoryStream ms = new MemoryStream(bag.Data);
            image.BeginInit();
            image.StreamSource = ms;
            image.EndInit();
            return image;
        }

        private void TypeClicked(object sender, MouseButtonEventArgs e)
        {
            IDynParameter para;
            switch(sender)
            {
                case Grid g:
                {
                    para = g.DataContext as IDynParameter;
                    break;
                }

                default:
                    throw new Exception("Not supported Helptext Parent: " + sender.GetType().ToString());
            }

            Helptext = para.Helptext;
        }

        private void Para_PropertyChanged(object sender, PropertyChangedEventArgs e = null)
        {
            if (e != null && e.PropertyName != "Value" && e.PropertyName != "ParamVisibility") return;

            IDynParameter para = (IDynParameter)sender;

            if (e.PropertyName == "Value")
            {
                System.Diagnostics.Debug.WriteLine("Wert geändert! " + para.Id + " -> " + para.Value);
                LogViewer.Text = string.Format(Properties.Messages.viewer_logger, para.Id, para.Value) + $"\r\n{LogViewer.Text}";
            } else if(e.PropertyName == "ParamVisibility")
            {
                System.Diagnostics.Debug.WriteLine("Sichtbarkeit geändert! " + para.Id + " -> " + para.IsVisible);
            }

            CalculateVisibilityParas(para);
            CalculateVisibilityComs(para);
        }

        private void CalculateVisibilityParas(IDynParameter para)
        {
            List<ChannelBlock> list = new List<ChannelBlock>();
            List<ParameterBlock> list2 = new List<ParameterBlock>();
            List<int> list5 = new List<int>();

            foreach (IDynChannel ch in Channels)
            {
                if (ch.HasAccess) ch.IsVisible = FunctionHelper.CheckConditions(ch.Conditions, values);
                else ch.IsVisible = false;
                if (!ch.IsVisible) continue;

                foreach (ParameterBlock block in ch.Blocks)
                {
                    if (block.HasAccess)
                        block.IsVisible = FunctionHelper.CheckConditions(block.Conditions, values);
                    else
                        block.IsVisible = false;
                }
            }

            IEnumerable<IDynParameter> list3 = Parameters.Where(p => p.Conditions.Any(c => c.SourceId == para.Id)); // || list5.Contains(c.SourceId)));

            var x = list3.Where(p => p.Id == 5618);
            foreach (IDynParameter par in x)
                if (par.HasAccess)
                    par.IsVisible = FunctionHelper.CheckConditions(par.Conditions, values);
            foreach (IDynParameter par in list3)
                if (par.HasAccess)
                    par.IsVisible = FunctionHelper.CheckConditions(par.Conditions, values);

            foreach (IDynChannel ch in Channels)
            {
                if (ch.IsVisible)
                {
                    ch.IsVisible = ch.Blocks.Any(b => b.IsVisible);
                }
            }


            foreach(ParamBinding bind in Bindings.Where(b => b.SourceId == para.Id))
            {
                switch(bind.Type)
                {
                    case BindingTypes.ParameterBlock:
                    {
                        ParameterBlock pb = GetDynamicParameterBlock(bind.TargetId);
                        if(pb == null)
                            throw new Exception("ParameterBlock could not be found: " + bind.TargetId);

                        pb.Text = bind.FullText.Replace("{d}", string.IsNullOrEmpty(para.Value) ? bind.DefaultText : para.Value);
                        break;
                    }

                    case BindingTypes.Channel:
                    {
                        break;
                    }

                    case BindingTypes.ComObject:
                    {
                        DeviceComObject com = ComObjects.SingleOrDefault(c => c.Id == bind.TargetId);
                        if(com != null)
                            com.Name = bind.FullText.Replace("{d}", string.IsNullOrEmpty(para.Value) ? bind.DefaultText : para.Value);
                        break;
                    }
                }
            }
        }

        private ParameterBlock GetDynamicParameterBlock(long id)
        {
            foreach(IDynChannel chan in Channels)
            {
                var x = GetDynamicParameterBlock(id, chan.Blocks);
                if(x != null) return x;
            }
            return null;
        }

        private ParameterBlock GetDynamicParameterBlock(long id, List<ParameterBlock> blocks)
        {
            foreach(ParameterBlock pb in blocks)
            {
                if(id == pb.Id) return pb;

                GetDynamicParameterBlock(id, pb.Blocks);
            }
            return null;
        }

        private void CalculateVisibilityComs(IDynParameter para)
        {
            IEnumerable<ComBinding> list = _comBindings.Where(co => co.Conditions.Any(c => c.SourceId == para.Id));

            foreach(IGrouping<long, ComBinding> bindings in list.GroupBy(cb => cb.ComId))
            {
                if(bindings.Any(cond => FunctionHelper.CheckConditions(cond.Conditions, values)))
                {
                    if (!ComObjects.Any(c => c.Id == bindings.Key))
                    {
                        AppComObject acom = _comObjects.Single(a => a.ApplicationId == _app.Id && a.Id == bindings.Key);
                        DeviceComObject dcom = new DeviceComObject(acom);
                        ParamBinding bind = Bindings.SingleOrDefault(b => b.TargetId == dcom.Id && b.Type == BindingTypes.ComObject);
                        if(bind != null)
                        {
                            string source = values[bind.SourceId].Value;
                            string val = source == "x" ? bind.DefaultText : source;
                            dcom.Name = bind.FullText.Replace("{d}", val);
                        }
                        ComObjects.Add(dcom);
                    }
                } else
                {
                    if (ComObjects.Any(c => c.Id == bindings.Key))
                    {
                        DeviceComObject dcom = ComObjects.Single(co => co.Id == bindings.Key);
                        ComObjects.Remove(dcom);
                    }
                }
            }

            //TODO allow to sort for name, function, etc
            //ComObjects.Sort(c => c.Number);
        }

        private void ClickGetValue(object sender, RoutedEventArgs e)
        {
            Controls.ProdVParameterDialog diag = new Controls.ProdVParameterDialog(Properties.Messages.viewer_get_prompt, Properties.Messages.viewer_get_title, _importer.Modules);
            if(diag.ShowDialog() == true)
            {
                if(values.ContainsKey(diag.Answer))
                {
                    string value = values[diag.Answer].Value;
                    if(value == "x")
                        MessageBox.Show(string.Format(Properties.Messages.viewer_get_inactive, diag.Answer), Properties.Messages.viewer_get_title);
                    else
                        MessageBox.Show(string.Format(Properties.Messages.viewer_get_value, diag.Answer, value), Properties.Messages.viewer_get_title);
                }
                else
                    MessageBox.Show(Properties.Messages.viewer_get_error, Properties.Messages.viewer_get_title);
            }
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}