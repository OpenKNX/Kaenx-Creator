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
    public partial class DynamicView : UserControl, INotifyPropertyChanged, ISelectable
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(DynamicView), new PropertyMetadata());
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(DynamicView), new PropertyMetadata());
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

        public DynamicView()
        {
            InitializeComponent();
        }

        public void ShowItem(object item)
        {
            var tvi = FindTviFromObjectRecursive(DynamicList, item);
            if (tvi != null) tvi.IsSelected = true;
        }

        public static TreeViewItem FindTviFromObjectRecursive(ItemsControl ic, object o) {
            //Search for the object model in first level children (recursively)
            TreeViewItem tvi = ic.ItemContainerGenerator.ContainerFromItem(o) as TreeViewItem;
            if (tvi != null) return tvi;
            
            //Loop through user object models
            foreach (object i in ic.Items) {
                //Get the TreeViewItem associated with the iterated object model
                TreeViewItem tvi2 = ic.ItemContainerGenerator.ContainerFromItem(i) as TreeViewItem;
                tvi = FindTviFromObjectRecursive(tvi2, o);
                if (tvi != null) return tvi;
            }
            return null;
        }



        private void ClickOpenDyn(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems main = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            SetIsExpaned(main, true);
        }

        private void ClickCloseDyn(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems main = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            SetIsExpaned(main, false);
        }


        private void SetIsExpaned(Models.Dynamic.IDynItems item, bool isExpanded)
        {
            item.IsExpanded = isExpanded;

            switch(item)
            {
                case Models.Dynamic.DynamicMain:
                case Models.Dynamic.DynamicModule:
                case Models.Dynamic.DynChannel:
                case Models.Dynamic.DynChannelIndependent:
                case Models.Dynamic.DynChooseBlock:
                case Models.Dynamic.DynChooseChannel:
                case Models.Dynamic.DynParaBlock:
                case Models.Dynamic.DynWhenBlock:
                case Models.Dynamic.DynWhenChannel:
                    foreach(Models.Dynamic.IDynItems ditem in item.Items)
                        SetIsExpaned(ditem, isExpanded);
                    break;
            }
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
            else if ((sender as Button).DataContext is Models.Dynamic.DynModule)
            {
                ((sender as Button).DataContext as Models.Dynamic.DynModule).Id = -1;
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

        private void ClickAddDynRepeat(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems item = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            item.Items.Add(new Models.Dynamic.DynRepeat() { Parent = item });
        }

        Dictionary<string, List<string>> SubTypes = new Dictionary<string, List<string>>() {
            {"DynamicMain",
                new List<string>() { 
                    "DynChannel",
                    "DynChannelIndependent",
                    "DynChoose",
                    "DynModule",
                    "DynRepeat" }
            },
            {"DynamicModule",
                new List<string>() { 
                    "DynChannel",
                    "DynChannelIndependent",
                    "DynChoose",
                    "DynModule",
                    "DynRepeat",
                    "DynParaBlock" }
            },
            {"DynChannelIndependent",
                new List<string>() { 
                    "DynParaBlock",
                    "DynComObject",
                    "DynChoose",
                    "DynModule",
                    "DynRepeat" }
            },
            {"DynChannel",
                new List<string>() { 
                    "DynParaBlock",
                    "DynComObject",
                    "DynModule",
                    "DynRepeat",
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
                    "DynRepeat",
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
                    "DynChoose",
                    "DynComObject",
                    "DynModule",
                    "DynAssign",
                    "DynRepeat",
                    "DynRename" }
            },
            {"DynRepeat",
                new List<string>() { 
                    "DynModule",
                    "DynChoose",
                    "DynRepeat" }
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
            (menu.Items[3] as MenuItem).IsEnabled = Version.IsModulesActive && SubTypes[type].Contains("DynModule");
            (menu.Items[4] as MenuItem).IsEnabled = SubTypes[type].Contains("DynChoose");
            //Separator
            (menu.Items[6] as MenuItem).IsEnabled = SubTypes[type].Contains("DynParameter");
            (menu.Items[7] as MenuItem).IsEnabled = SubTypes[type].Contains("DynComObject");
            (menu.Items[8] as MenuItem).IsEnabled = SubTypes[type].Contains("DynSeparator");
            (menu.Items[9] as MenuItem).IsEnabled = SubTypes[type].Contains("DynAssign");
            (menu.Items[10] as MenuItem).IsEnabled = SubTypes[type].Contains("DynRepeat");
            (menu.Items[11] as MenuItem).IsEnabled = SubTypes[type].Contains("DynButton");



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
            if(_copyItem == null)
            {
                MessageBox.Show("Es wurde kein Element vorher ausgeschnitten", "Element einfügen", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            Models.Dynamic.IDynItems target = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            target.Items.Add(_copyItem);
            _copyItem.Parent = target;
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
                        if(_draggedItem.Parent == _target.Parent)
                        {
                            _draggedItem.Parent.Items.Remove(_draggedItem);
                            int index = _target.Parent.Items.IndexOf(_target);
                            _target.Parent.Items.Insert(index, _draggedItem);
                            _draggedItem.Parent = _target.Parent;
                        } else {
                            //_draggedItem.Parent = _target;
                            //_target.Items.Add(_draggedItem);
                            MessageBox.Show("Not supported");
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

            return false;
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