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
    public partial class DynamicView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(DynamicView), new PropertyMetadata(OnVersionChangedCallback));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(DynamicView), new PropertyMetadata(OnModuleChangedCallback));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Module {
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
            if((sender as Button).DataContext is Models.Dynamic.DynParaBlock) {
                ((sender as Button).DataContext as Models.Dynamic.DynParaBlock).Id = -1;
            }
            else if((sender as Button).DataContext is Models.Dynamic.DynSeparator) {
                ((sender as Button).DataContext as Models.Dynamic.DynSeparator).Id = -1;
            } else
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
            foreach(Models.Language lang in Version.Languages)
                channel.Text.Add(new Models.Translation(lang, ""));
            main.Items.Add(channel);
        }

        private void ClickAddDynBlock(object sender, RoutedEventArgs e)
        {
            Models.Dynamic.IDynItems main = (sender as MenuItem).DataContext as Models.Dynamic.IDynItems;
            Models.Dynamic.DynParaBlock block = new Models.Dynamic.DynParaBlock() { Parent = main };
            foreach(Models.Language lang in Version.Languages)
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
            foreach(Models.Language lang in Version.Languages)
                separator.Text.Add(new Models.Translation(lang, ""));
            main.Items.Add(separator);
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

        private void LoadingContextDynWhen(object sender, RoutedEventArgs e)
        {
            ContextMenu menu = sender as ContextMenu;
            Models.Dynamic.DynWhen when = menu.DataContext as Models.Dynamic.DynWhen;

            switch(menu.DataContext)
            {
                case Models.Dynamic.DynamicMain:
                    (menu.Items[0] as MenuItem).IsEnabled = true;
                    (menu.Items[1] as MenuItem).IsEnabled = true;
                    (menu.Items[2] as MenuItem).IsEnabled = (Version != Module);
                    (menu.Items[3] as MenuItem).IsEnabled = (Version == Module);
                    (menu.Items[4] as MenuItem).IsEnabled = true;
                    (menu.Items[6] as MenuItem).IsEnabled = false;
                    (menu.Items[7] as MenuItem).IsEnabled = false;
                    (menu.Items[8] as MenuItem).IsEnabled = false;
                    (menu.Items[9] as MenuItem).IsEnabled = false;
                    (menu.Items[10] as MenuItem).IsEnabled = false;
                    (menu.Items[12] as MenuItem).IsEnabled = false;
                    break;

                case Models.Dynamic.DynChannelIndependent:
                    (menu.Items[0] as MenuItem).IsEnabled = false;
                    (menu.Items[1] as MenuItem).IsEnabled = false;
                    (menu.Items[2] as MenuItem).IsEnabled = true;
                    (menu.Items[3] as MenuItem).IsEnabled = (Version == Module);
                    (menu.Items[4] as MenuItem).IsEnabled = true;
                    (menu.Items[6] as MenuItem).IsEnabled = false;
                    (menu.Items[7] as MenuItem).IsEnabled = false;
                    (menu.Items[8] as MenuItem).IsEnabled = false;
                    (menu.Items[9] as MenuItem).IsEnabled = false;
                    (menu.Items[10] as MenuItem).IsEnabled = false;
                    (menu.Items[12] as MenuItem).IsEnabled = true;
                    break;

                case Models.Dynamic.DynChannel:
                    (menu.Items[0] as MenuItem).IsEnabled = false;
                    (menu.Items[1] as MenuItem).IsEnabled = false;
                    (menu.Items[2] as MenuItem).IsEnabled = true;
                    (menu.Items[3] as MenuItem).IsEnabled = (Version == Module);
                    (menu.Items[4] as MenuItem).IsEnabled = true;
                    (menu.Items[6] as MenuItem).IsEnabled = false;
                    (menu.Items[7] as MenuItem).IsEnabled = true;
                    (menu.Items[8] as MenuItem).IsEnabled = false;
                    (menu.Items[9] as MenuItem).IsEnabled = false;
                    (menu.Items[10] as MenuItem).IsEnabled = false;
                    (menu.Items[12] as MenuItem).IsEnabled = true;
                    break;
                
                case Models.Dynamic.DynParaBlock:
                    (menu.Items[0] as MenuItem).IsEnabled = false;
                    (menu.Items[1] as MenuItem).IsEnabled = true;
                    (menu.Items[2] as MenuItem).IsEnabled = true;
                    (menu.Items[3] as MenuItem).IsEnabled = (Version == Module);
                    (menu.Items[4] as MenuItem).IsEnabled = true;
                    (menu.Items[6] as MenuItem).IsEnabled = true;
                    (menu.Items[7] as MenuItem).IsEnabled = true;
                    (menu.Items[8] as MenuItem).IsEnabled = true;
                    (menu.Items[9] as MenuItem).IsEnabled = false;
                    (menu.Items[10] as MenuItem).IsEnabled = false;
                    (menu.Items[12] as MenuItem).IsEnabled = true;
                    break;
                    
                case Models.Dynamic.DynWhen:
                    (menu.Items[0] as MenuItem).IsEnabled = false;
                    (menu.Items[1] as MenuItem).IsEnabled = false;
                    (menu.Items[2] as MenuItem).IsEnabled = true;
                    (menu.Items[3] as MenuItem).IsEnabled = (Version == Module);
                    (menu.Items[4] as MenuItem).IsEnabled = true;
                    (menu.Items[6] as MenuItem).IsEnabled = true;
                    (menu.Items[7] as MenuItem).IsEnabled = true;
                    (menu.Items[8] as MenuItem).IsEnabled = true;
                    (menu.Items[9] as MenuItem).IsEnabled = false;
                    (menu.Items[10] as MenuItem).IsEnabled = false;
                    (menu.Items[12] as MenuItem).IsEnabled = true;
                    break;
            }
        }

        
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}