using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class ParameterView : UserControl, IFilterable, ISelectable
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(ParameterView), new PropertyMetadata(OnVersionChangedCallback));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(ParameterView), new PropertyMetadata(OnModuleChangedCallback));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }

        public List<ObjectType> ObjectTypes { get; set; } = new List<ObjectType>();

        private TextFilter _filter;
        private object _selectedItem = null;

        public void FilterShow()
        {
            _filter.Show();
            ParamList.SelectedItem = _selectedItem;
        }

        public void FilterHide()
        {
            _filter.Hide();
            _selectedItem = ParamList.SelectedItem;
            ParamList.SelectedItem = null;
        }

        public void ShowItem(object item)
        {
            ParamList.ScrollIntoView(item);
            ParamList.SelectedItem = item;
        }

        public ParameterView()
		{
            InitializeComponent();
            _filter = new TextFilter(query);
            
            List<ObjectProperty> defaultProperties = new List<ObjectProperty>() {
                new ObjectProperty(1, "Interface Object Type", "PID_OBJECT_TYPE"),
                new ObjectProperty(2, "Interface Object Name", "PID_OBJECT_NAME"),
                new ObjectProperty(3, "Semaphor", "PID_SEMAPHOR"),
                new ObjectProperty(4, "Group Object Reference", "PID_GROUP_OBJECT_REFERENCE"),
                new ObjectProperty(5, "Load Control", "PID_LOAD_STATE_CONTROL"),
                new ObjectProperty(6, "Run Control", "PID_RUN_STATE_CONTROL"),
                new ObjectProperty(7, "Table Reference", "PID_TABLE_REFERENCE"),
                new ObjectProperty(8, "Service Control", "PID_SERVICE_CONTROL"),
                new ObjectProperty(9, "Firmware Revision", "PID_FIRMWARE_REVISION"),
                new ObjectProperty(10, "Services Supported", "PID_SERVICES_SUPPORTED"),
            };

            ObjectTypes.Add(new ObjectType(0, "Device Object", "OT_DEVICE", defaultProperties, new List<ObjectProperty>() {
                new ObjectProperty(51, "Routing Count", "PID_ROUTING_COUNT"),
                new ObjectProperty(52, "Maximum Retry Count", "PID_MAX_RETRY_COUNT"),
                new ObjectProperty(53, "Error Flags", "PID_ERROR_FLAGS"),
                new ObjectProperty(54, "Programming Mode", "PID_PROGMODE"),
                new ObjectProperty(55, "Product Identification", "PID_PRODUCT_ID"),
                new ObjectProperty(56, "Max. APDU-Length", "PID_MAX_APDULENGTH"),
                new ObjectProperty(57, "PID_SUBNET_ADDR", "Subnetwork Address"),
                new ObjectProperty(58, "Device Address", "PID_DEVICE_ADDR"),
                new ObjectProperty(59, "Config Link", "PID_PB_CONFIG")
                //noch mehr
            }));

            ObjectTypes.Add(new ObjectType(1, "Addresstable Object", "OT_ADDRESS_TABLE", defaultProperties));
            ObjectTypes.Add(new ObjectType(2, "Associationtable Object", "OT_ASSOCIATION_TABLE", defaultProperties));
            ObjectTypes.Add(new ObjectType(3, "Applicationprogram Object", "OT_APPLICATION_PROGRAM", defaultProperties));
            ObjectTypes.Add(new ObjectType(4, "Interfaceprogram Object", "OT_INTERACE_PROGRAM", defaultProperties));
            ObjectTypes.Add(new ObjectType(5, "KNX-Object Associationtable Object", "OT_EIBOBJECT_ASSOCIATATION_TABLE", defaultProperties));
            
            ObjectTypes.Add(new ObjectType(9, "KNX-Object Associationtable Object", "OT_EIBOBJECT_ASSOCIATATION_TABLE", defaultProperties, new List<ObjectProperty>() {
                new ObjectProperty(51, "Groupobject Table", "PID_GRPOBJTABLE"),
                new ObjectProperty(52, "Groupobject Table Extended", "PID_EXT_GRPOBJREFERENCE")
            }));
        }

        private static void OnVersionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ParameterView)?.OnVersionChanged();
        }

        private static void OnModuleChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ParameterView)?.OnModuleChanged(e);
        }

        protected virtual void OnVersionChanged() {
            //nothing to do
        }
        
        protected virtual void OnModuleChanged(DependencyPropertyChangedEventArgs e) {
            if(e.NewValue != null)
            {
                _filter.ChangeView((e.NewValue as IVersionBase).Parameters);
            }
        }

        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Models.Parameter para = new Models.Parameter() {
                UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Module.Parameters)
            };
            foreach(Models.Language lang in Version.Languages) {
                para.Text.Add(new Models.Translation(lang, "Dummy"));
                para.Suffix.Add(new Models.Translation(lang, ""));
            }
            Module.Parameters.Add(para);
            ParamList.ScrollIntoView(para);
            ParamList.SelectedItem = para;

            if(Module.IsParameterRefAuto)
            {
                Models.ParameterRef pref = new Models.ParameterRef(para) { UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Module.ParameterRefs) };
                foreach(Models.Language lang in Version.Languages) {
                    pref.Text.Add(new Models.Translation(lang, ""));
                    pref.Suffix.Add(new Models.Translation(lang, ""));
                }
                Module.ParameterRefs.Add(pref);
            }
        }

        private void ClickClone(object sender, RoutedEventArgs e)
        {
            Parameter para = ParamList.SelectedItem as Models.Parameter;
            Parameter clonedPara = para.Copy();
            clonedPara.Id = -1;
            clonedPara.UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Module.Parameters);

            Module.Parameters.Add(clonedPara);

            if (Module.IsParameterRefAuto)
            {
                Models.ParameterRef pref = new Models.ParameterRef(clonedPara) { UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Module.ParameterRefs) };
                Module.ParameterRefs.Add(pref);
                foreach(Models.Language lang in Version.Languages) {
                    pref.Text.Add(new Models.Translation(lang, ""));
                    pref.Suffix.Add(new Models.Translation(lang, ""));
                }
            }
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            Parameter para = ParamList.SelectedItem as Models.Parameter;

            if(Module.IsParameterRefAuto)
            {
                ParameterRef pref = Module.ParameterRefs.Single(p => p.ParameterObject == para);
                List<int> uids = new List<int>();
                ClearHelper.GetIDs(Module.Dynamics[0], uids, true);
                if(uids.Contains(pref.UId))
                {
                    if(MessageBoxResult.No == MessageBox.Show(Properties.Messages.para_delete1, Properties.Messages.para_delete_title, MessageBoxButton.YesNo, MessageBoxImage.Warning))
                        return;
                    
                    ClearHelper.ClearIDs(Module.Dynamics[0], pref);
                }
                Module.ParameterRefs.Remove(pref);
            } else {
                if(Module.ParameterRefs.Any(p => p.ParameterObject == para))
                {
                    if(MessageBoxResult.No == MessageBox.Show(Properties.Messages.para_delete2, Properties.Messages.para_delete_title, MessageBoxButton.YesNo, MessageBoxImage.Warning))
                        return;

                    foreach(ParameterRef pref in Module.ParameterRefs.Where(p => p.ParameterObject == para))
                        pref.ParameterObject = null;
                }
            }

            Module.Parameters.Remove(para);

        }

        private void ClickCheckHyperlink(object sender, RoutedEventArgs e)
        {
            Models.Parameter para = (sender as System.Windows.Documents.Hyperlink).DataContext as Models.Parameter;
            MainWindow.Instance.GoToItem(para.ParameterTypeObject, null);
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.Parameter).Id = -1;
        }

        private void ManuelId(object sender, RoutedEventArgs e)
        {
            PromptDialog diag = new PromptDialog(Properties.Messages.para_prompt_id, Properties.Messages.prompt_id);
            if(diag.ShowDialog() == true)
            {
                int id;
                if(!int.TryParse(diag.Answer, out id))
                {
                    MessageBox.Show(Properties.Messages.prompt_error, Properties.Messages.prompt_error_title);
                    return;
                }
                Parameter para = Module.Parameters.SingleOrDefault(p => p.Id == id);
                if(para != null)
                {
                    MessageBox.Show(string.Format(Properties.Messages.prompt_double, id, para.Name), Properties.Messages.prompt_double_title);
                    return;
                }
                ((sender as Button).DataContext as Models.Parameter).Id = id;
            }
        }
    
        private void AutoId(object sender, RoutedEventArgs e)
        {
            Models.Parameter ele = (sender as Button).DataContext as Models.Parameter;
            long oldId = ele.Id;
            ele.Id = -1;
            ele.Id = Kaenx.Creator.Classes.Helper.GetNextFreeId(Module, "Parameters");
            if(ele.Id == oldId)
                MessageBox.Show(Properties.Messages.prompt_auto_error, Properties.Messages.prompt_auto_error_title);
        }

    
        #region DragNDrop

        private Parameter _draggedItem;
        private Parameter _target;

        private void ListMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is ListBox && e.LeftButton == MouseButtonState.Pressed && !e.OriginalSource.GetType().Equals(typeof(System.Windows.Controls.Primitives.Thumb)))
            {
                _draggedItem = (Parameter)ParamList.SelectedItem;
                if (_draggedItem != null)
                {
                    DragDropEffects finalDropEffect = DragDrop.DoDragDrop(ParamList, ParamList.SelectedValue, DragDropEffects.Move);
                    //Checking target is not null and item is
                    //dragging(moving) and move drop was accepted
                    
                    if ((finalDropEffect == DragDropEffects.Move) && (_target != null) && (_draggedItem != _target))
                    {
                        Module.Parameters.Remove(_draggedItem);
                        Module.Parameters.Insert(Module.Parameters.IndexOf(_target), _draggedItem);

                        _target = null;
                        _draggedItem = null;
                    }
                }
            }
        }

        private void ListDragOver(object sender, DragEventArgs e)
        {
            if(sender != ParamList)
            {
                e.Effects = DragDropEffects.None;
                e.Handled = true;
                return;
            }

            Parameter item = GetNearestContainer(e.OriginalSource);

            if(item == null)
            {
                System.Diagnostics.Debug.WriteLine(e.OriginalSource.GetType().ToString());
                e.Effects = DragDropEffects.None;
            } else {
                e.Effects = DragDropEffects.Move;
            }
            
            e.Handled = true;
        }

        private void ListDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            // Verify that this is a valid drop and then store the drop target
            Parameter TargetItem = GetNearestContainer(e.OriginalSource);
            if (TargetItem != null && _draggedItem != null)
            {
                _target = TargetItem;
                e.Effects = DragDropEffects.Move;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private Parameter GetNearestContainer(object source)
        {
            Parameter item = (source as System.Windows.Documents.Run)?.DataContext as Parameter;

            if(item == null)
                item = (source as System.Windows.Controls.Border)?.DataContext as Parameter;

            if(item == null)
                item = (source as System.Windows.Controls.Image)?.DataContext as Parameter;

            if(item == null)
                item = (source as System.Windows.Controls.TextBlock)?.DataContext as Parameter;
            return item;
        }

        #endregion
    }

    public class ObjectType
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }
        public List<ObjectProperty> Properties { get; set; }

        public ObjectType(int num, string text, string name, List<ObjectProperty> defaults, List<ObjectProperty> added = null)
        {
            Number = num;
            Name = name;
            Text = text;
            Properties = new List<ObjectProperty>(defaults);
            if(added != null)
                Properties.AddRange(added);
        }

        public override string ToString()
        {
            return $"{Number} - {Text} - {Name}";
        }
    }

    public class ObjectProperty
    {
        public int Number { get; set; }
        public string Name { get; set; }
        public string Text { get; set; }

        public ObjectProperty(int num, string text, string name)
        {
            Number = num;
            Name = name;
            Text = text;
        }
    }
}