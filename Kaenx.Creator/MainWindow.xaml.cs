using Kaenx.Creator.Classes;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
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
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        private Models.ModelGeneral _general;
        private string filePath = "";

        public Models.ModelGeneral General
        {
            get { return _general; }
            set { _general = value; Changed("General"); }
        }

        private ObservableCollection<Models.MaskVersion> bcus;
        public ObservableCollection<Models.MaskVersion> BCUs
        {
            get { return bcus; }
            set { bcus = value; Changed("BCUs"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;


        public MainWindow()
        {
            InitializeComponent();
            this.DataContext = this;
            LoadBcus();
        }

        private void ClickNew(object sender, RoutedEventArgs e)
        {
            General = new Models.ModelGeneral();
            General.Catalog.Add(new Models.CatalogItem() { Name = "Hauptkategorie (wird nicht exportiert)" });
            SetButtons(true);
        }

        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        private void LoadBcus()
        {
            string jsonPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "maskversion.json");
            string xmlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data", "maskversion.xml");
            if (System.IO.File.Exists(jsonPath))
            {
                BCUs = Newtonsoft.Json.JsonConvert.DeserializeObject<ObservableCollection<Models.MaskVersion>>(System.IO.File.ReadAllText(jsonPath));
            } else
            {
                BCUs = new ObservableCollection<Models.MaskVersion>();
                XDocument xdoc = XDocument.Load(xmlPath);
                foreach(XElement xmask in xdoc.Root.Elements())
                {
                    Models.MaskVersion mask = new Models.MaskVersion();
                    mask.Id = xmask.Attribute("Id").Value;

                    string eleStr = xmask.ToString();
                    if (eleStr.Contains("<Procedure ProcedureType=\"Load\""))
                    {
                        XElement prodLoad = xmask.Descendants(XName.Get("Procedure")).First(p => p.Attribute("ProcedureType")?.Value == "Load");
                        if (prodLoad.ToString().Contains("<LdCtrlMerge"))
                            mask.Procedure = Models.ProcedureTypes.Merge;
                        else
                            mask.Procedure = Models.ProcedureTypes.Default;
                    } else
                    {
                        mask.Procedure = Models.ProcedureTypes.Application;
                    }


                    if(mask.Procedure != Models.ProcedureTypes.Application)
                    {
                        if (eleStr.Contains("<LdCtrlAbsSegment"))
                        {
                            mask.Memory = Models.MemoryTypes.Absolute;
                        }
                        else if (eleStr.Contains("<LdCtrlWriteRelMem"))
                        {
                            mask.Memory = Models.MemoryTypes.Relative;
                        }
                        else if (eleStr.Contains("<LdCtrlWriteMem"))
                        {
                            mask.Memory = Models.MemoryTypes.Relative;
                        }
                        else
                        {
                            continue;
                        }
                    }

                    BCUs.Add(mask);
                }

                System.IO.File.WriteAllText(jsonPath, Newtonsoft.Json.JsonConvert.SerializeObject(BCUs));
            }
        }


        #region Clicks

        #region Clicks Add/Remove

        private void ClickAddHardDevice(object sender, RoutedEventArgs e)
        {
            Models.Hardware hard = (sender as Button).DataContext as Models.Hardware;
            hard.Devices.Add(new Models.Device());
        }

        private void ClickAddHardApp(object sender, RoutedEventArgs e)
        {
            Models.Hardware hard = (sender as Button).DataContext as Models.Hardware;
            if(!hard.Apps.Contains(InHardApp.SelectedItem as Models.Application)) {
                hard.Apps.Add(InHardApp.SelectedItem as Models.Application);
            }
            InHardApp.SelectedItem = null;
        }

        /*private void ClickRemoveDevice(object sender, RoutedEventArgs e)
        {
            if(DeviceList.SelectedItem == null) return;

            Models.Device dev = DeviceList.SelectedItem as Models.Device;
            General.Devices.Remove(dev);
        }*/

        private void ClickAddParamRef(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            ver.ParameterRefs.Add(new Models.ParameterRef());
        }

        private void ClickRemoveParamRef(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;

            Models.ParameterRef dev = ParamRefList.SelectedItem as Models.ParameterRef;
            ver.ParameterRefs.Remove(dev);
        }

        private void ClickAddVersion(object sender, RoutedEventArgs e)
        {
            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion newVer = new Models.AppVersion() { Name = app.Name };

            if(app.Versions.Count > 0){
                Models.AppVersion ver = app.Versions.OrderByDescending(v => v.Number).ElementAt(0);
                newVer.Number = ver.Number + 1;
            }

            newVer.Dynamics.Add(new Models.Dynamic.DynamicMain());
            app.Versions.Add(newVer);
        }

        private void ClickAddParamType(object sender, RoutedEventArgs e)
        {
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.ParameterTypes.Add(new Models.ParameterType());
        }

        private void ClickAddMemory(object sender, RoutedEventArgs e)
        {
            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.Memories.Add(new Models.Memory() { Type = app.Mask.Memory });
        }

        private void ClickRemoveParamType(object sender, RoutedEventArgs e)
        {
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.ParameterTypes.Remove(ListParamTypes.SelectedItem as Models.ParameterType);
        }

        private void ClickRemoveMemory(object sender, RoutedEventArgs e)
        {
            Models.AppVersion version = (sender as Button).DataContext as Models.AppVersion;
            version.Memories.Remove(ListMemories.SelectedItem as Models.Memory);
        }

        private void ClickRemoveVersion(object sender, RoutedEventArgs e)
        {
            if(AppList.SelectedItem == null || VersionList.SelectedItem == null) return;

            Models.Application app = AppList.SelectedItem as Models.Application;
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;

            app.Versions.Remove(ver);
        }

        private void ClickAddApp(object sender, RoutedEventArgs e)
        {
            General.Applications.Add(new Models.Application());
        }

        private void ClickRemoveApp(object sender, RoutedEventArgs e)
        {
            if(AppList.SelectedItem == null) return;

            Models.Application app = AppList.SelectedItem as Models.Application;
            General.Applications.Remove(app);
        }


        private void ClickAddParamEnum(object sender, RoutedEventArgs e)
        {
            Models.ParameterType type = ListParamTypes.SelectedItem as Models.ParameterType;

            type.Enums.Add(new Models.ParameterTypeEnum() { Name = "Name", Value = 0 });
        }

        private void ClickAddParam(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            ver.Parameters.Add(new Models.Parameter());
        }


        private void ClickRemoveParam(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            ver.Parameters.Remove(ParamList.SelectedItem as Models.Parameter);
        }


        private void ClickAddHardware(object sender, RoutedEventArgs e)
        {
            General.Hardware.Add(new Models.Hardware());
        }


        private void ClickRemoveHardware(object sender, RoutedEventArgs e)
        {
            General.Hardware.Remove(HardwareList.SelectedItem as Models.Hardware);
        }

        /*private void ClickAddHardwareApp(object sender, RoutedEventArgs e)
        {
            Models.Hardware hard = HardwareList.SelectedItem as Models.Hardware;
            Models.HardwareApp happ = new Models.HardwareApp();
            happ.AppObject = InHardwareApp.SelectedItem as Models.Application;
            happ.AppVersionObject = InHardwareVer.SelectedItem as Models.AppVersion;
            if (happ.AppObject == null || happ.AppVersionObject == null) return;
            hard.Apps.Add(happ);
        }


        private void ClickRemoveHardwareApp(object sender, RoutedEventArgs e)
        {
            Models.Hardware hard = HardwareList.SelectedItem as Models.Hardware;
            if (hard == null) return;
            hard.Apps.Remove(HardwareAppList.SelectedItem as Models.HardwareApp);
        }*/

        #endregion

        private void ClickSave(object sender, RoutedEventArgs e)
        {
            string general = Newtonsoft.Json.JsonConvert.SerializeObject(General, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto });

            if(filePath != "") {
                System.IO.File.WriteAllText(filePath, general);
                return;
            }

            SaveFileDialog diag = new SaveFileDialog();
            diag.FileName = General.ProjectName;
            diag.Title = "Projekt speichern";
            diag.Filter = "Kaenx Hersteller Projekt (*.ae-manu)|*.ae-manu";
            
            if(diag.ShowDialog() == true)
            {
                System.IO.File.WriteAllText(diag.FileName, general);
                filePath = diag.FileName;
            }
        }

        private void ClickOpen(object sender, RoutedEventArgs e)
        {
            OpenFileDialog diag = new OpenFileDialog();
            diag.Title = "Projekt öffnen";
            diag.Filter = "Kaenx Hersteller Projekt (*.ae-manu)|*.ae-manu";
            if(diag.ShowDialog() == true)
            {
                string general = System.IO.File.ReadAllText(diag.FileName);
                General = Newtonsoft.Json.JsonConvert.DeserializeObject<Models.ModelGeneral>(general, new Newtonsoft.Json.JsonSerializerSettings() { TypeNameHandling = Newtonsoft.Json.TypeNameHandling.Auto });
                filePath = diag.FileName;

                foreach(Models.Application app in General.Applications)
                {
                    foreach(Models.AppVersion ver in app.Versions)
                    {
                        foreach(Models.Parameter para in ver.Parameters)
                        {
                            if (!string.IsNullOrEmpty(para._memory))
                            {
                                Models.Memory mem = ver.Memories.Single(m => m.Name == para._memory);
                                para.MemoryObject = mem;
                            }
                            if (!string.IsNullOrEmpty(para._parameterType))
                            {
                                Models.ParameterType pt = ver.ParameterTypes.Single(p => p.Name == para._parameterType);
                                para.ParameterTypeObject = pt;
                            }
                        }

                        foreach(Models.ParameterRef pref in ver.ParameterRefs)
                        {
                            if (!string.IsNullOrEmpty(pref._parameter))
                            {
                                Models.Parameter para = ver.Parameters.Single(p => p.Name == pref._parameter);
                                pref.ParameterObject = para;
                            }
                        }

                        LoadSubDyn(ver.Dynamics[0], ver.ParameterRefs.ToList());
                    }


                    string mid = app._maskId;
                    if (string.IsNullOrEmpty(mid)) continue;

                    Models.MaskVersion mask = BCUs.Single(bcu => bcu.Id == mid);
                    app.Mask = mask;
                }


                foreach(Models.Hardware hard in General.Hardware){
                    if(string.IsNullOrEmpty(hard._appsString)) continue;
                    
                    foreach(string name in hard._appsString.Split(',')){
                        try{
                            hard.Apps.Add(General.Applications.Single(app => app.Name == name));
                        } catch{}
                    }
                }

                SetSubCatalogItems(General.Catalog[0]);

                SetButtons(true);
            }
        }

        private void LoadSubDyn(Models.Dynamic.IDynItems dyn, List<Models.ParameterRef> paras)
        {
            foreach (Models.Dynamic.IDynItems item in dyn.Items)
            {
                item.Parent = dyn;

                if (item is Models.Dynamic.DynParameter)
                {
                    Models.Dynamic.DynParameter para = item as Models.Dynamic.DynParameter;
                    if (!string.IsNullOrEmpty(para._parameter))
                    {
                        Models.ParameterRef pr = paras.Single(p => p.Name == para._parameter);
                        para.ParameterRefObject = pr;
                    }
                } else if(item is Models.Dynamic.DynChoose)
                {
                    Models.Dynamic.DynChoose ch = item as Models.Dynamic.DynChoose;
                    if (!string.IsNullOrEmpty(ch._parameterRef))
                    {
                        Models.ParameterRef pr = paras.Single(p => p.Name == ch._parameterRef);
                        ch.ParameterRefObject = pr;
                    }
                }

                if (!(item is Models.Dynamic.DynParameter) && item.Items != null)
                    LoadSubDyn(item, paras);
            }
        }


        private void SetSubCatalogItems(Models.CatalogItem parent)
        {
            foreach(Models.CatalogItem item in parent.Items)
            {
                item.Parent = parent;

                if (!string.IsNullOrEmpty(item._hardwareName))
                {
                    item.Hardware = General.Hardware.Single(h => h.Name == item._hardwareName);
                }

                SetSubCatalogItems(item);
            }
        }

        private void SetButtons(bool enable)
        {
            MenuSave.IsEnabled = enable;
            MenuClose.IsEnabled = enable;
            MenuPublish.IsEnabled = enable;
            MenuImport.IsEnabled = enable;
            TabsEdit.IsEnabled = enable;
        }

        private void ClickExportEts(object sender, RoutedEventArgs e)
        {
            ExportHelper helper = new ExportHelper();
            helper.ExportEts(General);
            helper.SignOutput();
        }

        private void ClickGenerateRefAuto(object sender, RoutedEventArgs e)
        {
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            ver.ParameterRefs.Clear();

            foreach(Models.Parameter para in ver.Parameters)
            {
                Models.ParameterRef pref = new Models.ParameterRef();
                pref.Name = para.Name;
                pref.ParameterObject = para;
                ver.ParameterRefs.Add(pref);
            }
        }

        private void ClickExportSign(object sender, RoutedEventArgs e)
        {
            ExportHelper helper = new ExportHelper();
            helper.SignOutput();
        }

        private void ClickCatalogContext(object sender, RoutedEventArgs e)
        {
            Models.CatalogItem item = (sender as MenuItem).DataContext as Models.CatalogItem;
            item.Items.Add(new Models.CatalogItem() { Name = "Neue Kategorie", Parent = item });
        }

        private void ClickCatalogContextRemove(object sender, RoutedEventArgs e)
        {
            Models.CatalogItem item = (sender as MenuItem).DataContext as Models.CatalogItem;
            item.Parent.Items.Remove(item);
        }




        #endregion

        private void ClickCalcHeatmap(object sender, RoutedEventArgs e)
        {
            Models.Memory mem = (sender as Button).DataContext as Models.Memory;
            Models.AppVersion ver = VersionList.SelectedItem as Models.AppVersion;
            byte[] data = AutoHelper.GetMemorySize(ver, mem);

            int height = Convert.ToInt32(Math.Ceiling(data.Length / 16.0));
            Debug.WriteLine("Höhe: " + height);

            WriteableBitmap map = new WriteableBitmap(16, height, 1, 1, PixelFormats.Indexed8, BitmapPalettes.WebPalette);

            int stride = (map.PixelWidth * map.Format.BitsPerPixel + 7) / 8;
            byte[] pixelByteArray = new byte[map.PixelHeight * stride];

            map.CopyPixels(pixelByteArray, stride, 0);

            for(int i = 0; i < pixelByteArray.Length; i++)
            {
                int val = (i >= data.Length) ? 0 : data[i];
                switch (val)
                {
                    case 0:
                        pixelByteArray[i] = 18;
                        break;

                    case 1:
                    case 2:
                    case 3:
                    case 4:
                        pixelByteArray[i] = 205;
                        break;

                    case 5:
                    case 6:
                    case 7:
                        pixelByteArray[i] = 193;
                        break;

                    case 8:
                        pixelByteArray[i] = 180;
                        break;

                    default:
                        pixelByteArray[i] = 0;
                        break;
                }
                
            }

            map.WritePixels(new Int32Rect(0, 0, map.PixelWidth, map.PixelHeight), pixelByteArray, stride, 0);
            OutHeatmap.Source = map;
        }

        #region Clicks Dyn
        private void ClickAddDynIndep(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems main = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            main.Items.Add(new Models.Dynamic.DynChannelIndependet() { Parent = main });
        }

        private void ClickAddDynBlock(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems main = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            main.Items.Add(new Models.Dynamic.DynParaBlock() { Parent = main });
        }

        private void ClickAddDynPara(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems block = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            Models.Dynamic.DynParameter para = new Models.Dynamic.DynParameter() { Parent = block };
            if ((VersionList.SelectedItem as Models.AppVersion).IsParameterRefAuto)
            {
                Models.ParameterRef pref = new Models.ParameterRef() { Name = "Ref" + (new Random().Next(0, 10000)) };
                (VersionList.SelectedItem as Models.AppVersion).ParameterRefs.Add(pref);
                para.ParameterRefObject = pref;
            }
            block.Items.Add(para);
        }

        private void ClickAddDynChoose(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems item = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            item.Items.Add(new Models.Dynamic.DynChoose() { Parent = item });
        }

        private void ClickAddDynWhen(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems item = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            item.Items.Add(new Models.Dynamic.DynWhen() { Parent = item });
        }

        private void ClickRemoveDyn(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems item = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            item.Parent.Items.Remove(item);
        }

        private void LoadingContextDynWhen(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            Models.Dynamic.DynWhen when = menu.DataContext as Models.Dynamic.DynWhen;
            bool chann = when.CanAddIndependent;
            (menu.Items[0] as MenuItem).IsEnabled = chann;
            (menu.Items[1] as MenuItem).IsEnabled = chann;
            (menu.Items[2] as MenuItem).IsEnabled = when.CanAddBlock;

            bool subs = when.CanAddPara;
            (menu.Items[5] as MenuItem).IsEnabled = subs;
            (menu.Items[6] as MenuItem).IsEnabled = subs;
        }
        #endregion

        private void ClickRemoveDeviceApp(object sender, RoutedEventArgs e)
        {
            (HardwareList.SelectedItem as Models.Hardware).Apps.Remove(DeviceAppList.SelectedItem as Models.Application);
        }

        private void ClickAddCom(object sender, RoutedEventArgs e)
        {
            (VersionList.SelectedItem as Models.AppVersion).ComObjects.Add(new Models.ComObject());
        }
    }
}
