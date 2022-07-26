using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{

    //TODO add button "alle ausklappen"

    public partial class DynamicView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(DynamicView), new PropertyMetadata(OnVersionChangedCallback));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(DynamicView), new PropertyMetadata(OnModuleChangedCallback));
        public AppVersion Version
        {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Module
        {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }

        public ObservableCollection<Models.Module> ModulesList { get { return Version?.Modules; } }
        public ObservableCollection<ParameterRef> ParameterRefsList { get { return Module?.ParameterRefs; } }
        public ObservableCollection<ComObjectRef> ComObjectRefsList { get { return Module?.ComObjectRefs; } }

        public DynamicView()
        {
            InitializeComponent();
        }


        private static void OnVersionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as DynamicView)?.OnVersionChanged();
        }

        private static void OnModuleChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as DynamicView)?.OnModuleChanged();
        }

        protected virtual void OnVersionChanged()
        {
            Changed("ModulesList");
        }

        protected virtual void OnModuleChanged()
        {
            Changed("ParameterRefsList");
            Changed("ComObjectRefsList");
        }


        private void ResetId(object sender, RoutedEventArgs e)
        {
            if ((sender as Button).DataContext is Models.Dynamic.DynParaBlock)
            {
                ((sender as Button).DataContext as Models.Dynamic.DynParaBlock).Id = -1;
            }
            else if ((sender as Button).DataContext is Models.Dynamic.DynSeparator)
            {
                ((sender as Button).DataContext as Models.Dynamic.DynSeparator).Id = -1;
            }
            else
            {
                throw new Exception("Unbekannter Typ zum ID löschen: " + sender.GetType().ToString());
            }
        }

        private void ClickAddDynIndep(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems main = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            main.Items.Add(new Models.Dynamic.DynChannelIndependent() { Parent = main });
        }

        private void ClickAddDynChannel(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems main = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            Models.Dynamic.DynChannel channel = new Models.Dynamic.DynChannel() { Parent = main };
            foreach (Models.Language lang in Version.Languages)
                channel.Text.Add(new Models.Translation(lang, ""));
            main.Items.Add(channel);
        }

        private void ClickAddDynBlock(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems main = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            Models.Dynamic.DynParaBlock block = new Models.Dynamic.DynParaBlock() { Parent = main };
            foreach (Models.Language lang in Version.Languages)
                block.Text.Add(new Models.Translation(lang, ""));
            main.Items.Add(block);
        }

        private void ClickAddDynPara(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems block = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            Models.Dynamic.DynParameter para = new Models.Dynamic.DynParameter() { Parent = block };
            block.Items.Add(para);
        }

        private void ClickAddDynSep(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems main = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            Models.Dynamic.DynSeparator separator = new Models.Dynamic.DynSeparator() { Parent = main };
            foreach (Models.Language lang in Version.Languages)
                separator.Text.Add(new Models.Translation(lang, ""));
            main.Items.Add(separator);
        }

        private void ClickAddDynChoose(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems item = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            Models.Dynamic.IDynChoose dch;

            switch(item)
            {
                case Models.Dynamic.DynWhenBlock:
                case Models.Dynamic.DynParaBlock:
                    dch = new Models.Dynamic.DynChooseBlock();
                    break;

                case Models.Dynamic.DynWhenChannel:
                case Models.Dynamic.IDynChannel:
                    dch = new Models.Dynamic.DynChooseChannel();
                    break;

                default:
                    throw new Exception("Not implemented Parent");
            }

            dch.Parent = item;
            item.Items.Add(dch);
        }

        private void ClickAddDynWhen(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems item = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            Models.Dynamic.IDynWhen dw;

            switch(item)
            {
                case Models.Dynamic.DynChooseBlock:
                    dw = new Models.Dynamic.DynWhenBlock();
                    break;

                case Models.Dynamic.DynChooseChannel:
                    dw = new Models.Dynamic.DynWhenChannel();
                    break;

                default:
                    throw new Exception("Impossible Parent");
            }

            dw.Parent = item;
            item.Items.Add(dw);
        }

        private void ClickRemoveDyn(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems item = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            item.Parent.Items.Remove(item);
        }

        private void ClickAddDynCom(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems item = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            Models.Dynamic.DynComObject para = new Models.Dynamic.DynComObject() { Parent = item };
            item.Items.Add(para);
        }

        private void ClickAddDynModule(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems item = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            item.Items.Add(new Models.Dynamic.DynModule() { Parent = item });
        }

        private void ClickAddDynAssign(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems item = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            item.Items.Add(new Models.Dynamic.DynAssign() { Parent = item });
        }


        Dictionary<string, List<string>> SubTypes = new Dictionary<string, List<string>>() {
            {"DynamicMain",
                new List<string>() { 
                    "DynChannel",
                    "DynChannelIndependent",
                    "DynChoose",
                    "DynModule",
                    "DynRepeate" }
            },
            {"DynamicModule",
                new List<string>() { 
                    "DynChannel",
                    "DynChannelIndependent",
                    "DynChoose",
                    "DynModule",
                    "DynRepeate",
                    "DynParaBlock" }
            },
            {"DynChannelIndependent",
                new List<string>() { 
                    "DynParaBlock",
                    "DynComObject",
                    "DynChoose",
                    "DynModule",
                    "DynRepeate" }
            },
            {"DynChannel",
                new List<string>() { 
                    "DynParaBlock",
                    "DynComObject",
                    "DynModule",
                    "DynRepeate",
                    "DynChoose" }
            },
            {"DynParaBlock",
                new List<string>() { 
                    "DynParameter",
                    "DynParaBlock",
                    "DynSeparator",
                    "DynButton",
                    "DynChoose",
                    "DynComObject",
                    "DynModule",
                    "DynRepeate",
                    "DynAssign",
                    "DynChannel" }
            },
            {"DynWhenChannel",
                new List<string>() { 
                    "DynParaBlock",
                    "DynComObject",
                    "DynChoose",
                    "DynRename" }
            },
            {"DynWhenBlock",
                new List<string>() { 
                    "DynParameter",
                    "DynParaBlock",
                    "DynSeparator",
                    "DynButton",
                    "DynComObject",
                    "DynModule",
                    "DynRepeate",
                    "DynRename" }
            },
        };


        private void LoadingContextDynWhen(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            Models.Dynamic.IDynItems parent = menu.DataContext as Models.Dynamic.IDynItems;

            string type = parent.GetType().ToString();
            type = type.Substring(type.LastIndexOf('.') + 1);

            (menu.Items[0] as MenuItem).IsEnabled = SubTypes[type].Contains("DynChannelIndependent");
            (menu.Items[1] as MenuItem).IsEnabled = SubTypes[type].Contains("DynChannel");
            (menu.Items[2] as MenuItem).IsEnabled = SubTypes[type].Contains("DynParaBlock");
            (menu.Items[3] as MenuItem).IsEnabled = SubTypes[type].Contains("DynModule");
            (menu.Items[4] as MenuItem).IsEnabled = SubTypes[type].Contains("DynChoose");
            //Separator
            (menu.Items[6] as MenuItem).IsEnabled = SubTypes[type].Contains("DynParameter");
            (menu.Items[7] as MenuItem).IsEnabled = SubTypes[type].Contains("DynComObject");
            (menu.Items[8] as MenuItem).IsEnabled = SubTypes[type].Contains("DynSeparator");
            (menu.Items[9] as MenuItem).IsEnabled = SubTypes[type].Contains("DynAssign");
            (menu.Items[10] as MenuItem).IsEnabled = SubTypes[type].Contains("DynButton");



            (menu.Items[14] as MenuItem).IsEnabled = _copyItem != null;
            (menu.Items[15] as MenuItem).IsEnabled = type != "DynamicMain" && type != "DynamicModule";
        }


        #region CopyPaste
        private Models.Dynamic.IDynItems _copyItem;

        private void ClickCutDyn(object sender, RoutedEventArgs e)
        {
            _copyItem = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            _copyItem.Parent.Items.Remove(_copyItem);
        }

        private void ClickCopyDyn(object sender, RoutedEventArgs e)
        {
            _copyItem = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
        }

        private void ClickInsertDyn(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems target = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            target.Items.Add(_copyItem);
            _copyItem = null;
        }

        #endregion


        #region DragNDrop
        private Models.Dynamic.IDynItems _draggedItem;
        private Models.Dynamic.IDynItems _target;

        private void TreeDragOver(object sender, DragEventArgs e)
        {
            Models.Dynamic.IDynItems item = GetNearestContainer(e.OriginalSource);

            if(item == null)
            {
                //System.Diagnostics.Debug.WriteLine(e.OriginalSource.GetType().ToString());
                e.Effects = DragDropEffects.None;
            } else {
                e.Effects = CheckDropTarget(item) ? DragDropEffects.Move : DragDropEffects.None;
            }
            
            e.Handled = true;
        }

        private void TreeMouseMove(object sender, MouseEventArgs e)
        {
            if (sender is TreeView && e.LeftButton == MouseButtonState.Pressed)
            {
                _draggedItem = (Models.Dynamic.IDynItems)DynamicList.SelectedItem;
                if (_draggedItem != null)
                {
                    DragDropEffects finalDropEffect = DragDrop.DoDragDrop(DynamicList, DynamicList.SelectedValue, DragDropEffects.Move);
                    //Checking target is not null and item is
                    //dragging(moving) and move drop was accepted
                    
                    if ((finalDropEffect == DragDropEffects.Move) && (_target != null) && (_draggedItem != _target))
                    {
                        //TODO decide to add or insert by pressing shift?
                        //and decide to copy or cut by pressing ctrl?
                        _draggedItem.Parent.Items.Remove(_draggedItem);
                        if(_draggedItem.Parent == _target.Parent)
                        {
                            int index = _target.Parent.Items.IndexOf(_target);
                            _target.Parent.Items.Insert(index, _draggedItem);
                            _draggedItem.Parent = _target.Parent;
                        } else {
                            _draggedItem.Parent = _target;
                            _target.Items.Add(_draggedItem);
                        }
                        _target = null;
                        _draggedItem = null;
                    }
                }
            }
        }
        
        private void TreeDrop(object sender, DragEventArgs e)
        {
            e.Handled = true;
            // Verify that this is a valid drop and then store the drop target
            Models.Dynamic.IDynItems TargetItem = GetNearestContainer(e.OriginalSource);
            if (TargetItem != null && _draggedItem != null)
            {
                _target = TargetItem;
                e.Effects = DragDropEffects.Move;
            } else {
                e.Effects = DragDropEffects.None;
            }
        }

        private bool CheckDropTarget(Models.Dynamic.IDynItems target)
        {
            if(target == _draggedItem) return false;

            if(target.Parent == _draggedItem.Parent) return true;


            string targetType = target.GetType().ToString();
            targetType = targetType.Substring(targetType.LastIndexOf('.') + 1);
            string draggedType = _draggedItem.GetType().ToString();
            draggedType = draggedType.Substring(draggedType.LastIndexOf('.') + 1);
            if(!SubTypes[targetType].Contains(draggedType)) return false;



            Models.Dynamic.IDynItems parent = target.Parent;
            
            System.Diagnostics.Debug.WriteLine(target.GetType());

            while(parent.GetType() != typeof(Models.Dynamic.DynamicMain) && parent.GetType() != typeof(Models.Dynamic.DynamicModule))
            {
                if(parent == _draggedItem.Parent)
                {
                    return false;
                }
                parent = parent.Parent;
            }

            System.Diagnostics.Debug.WriteLine(target.GetType());

            return true;
        }

        private Models.Dynamic.IDynItems GetNearestContainer(object source)
        {
            Models.Dynamic.IDynItems item = (source as System.Windows.Documents.Run)?.DataContext as Models.Dynamic.IDynItems;

            if(item == null)
                item = (source as System.Windows.Controls.Border)?.DataContext as Models.Dynamic.IDynItems;

            if(item == null)
                item = (source as System.Windows.Controls.Image)?.DataContext as Models.Dynamic.IDynItems;

            if(item == null)
                item = (source as System.Windows.Controls.TextBlock)?.DataContext as Models.Dynamic.IDynItems;
            return item;
        }
        
        #endregion



        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}