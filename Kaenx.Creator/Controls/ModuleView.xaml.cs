using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Kaenx.Creator.Controls
{
    public partial class ModuleView : UserControl, INotifyPropertyChanged, ISelectable
    {
        public static readonly System.Windows.DependencyProperty VersionProperty = System.Windows.DependencyProperty.Register("Version", typeof(AppVersion), typeof(ModuleView), new System.Windows.PropertyMetadata(OnVersionChangedCallback));
        public static readonly DependencyProperty IconsProperty = DependencyProperty.Register("Icons", typeof(ObservableCollection<Icon>), typeof(ModuleView), new PropertyMetadata());
        public AppVersion Version
        {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public ObservableCollection<Icon> Icons
        {
            get { return (ObservableCollection<Icon>)GetValue(IconsProperty); }
            set { SetValue(IconsProperty, value); }
        }

        private int _currentIndex = 0;
        private int CurrentIndex
        {
            get { return _currentIndex; }
            set { 
                _currentIndex = value; 
                while(_currentIndex < (Modules.Count - 1))
                {
                    Modules.RemoveAt(Modules.Count - 1);
                }
                Changed("CurrentIndex"); 
                Changed("CurrentModule");
            }
        }

        public ModuleViewerModel CurrentModule
        {
            get {
                if(Modules == null || CurrentIndex == -1 || Modules.Count <= CurrentIndex) return null;
                return Modules[CurrentIndex]; 
            }
            set { CurrentIndex = Modules.IndexOf(value); }
        }

        private ObservableCollection<ModuleViewerModel> _modules = new ObservableCollection<ModuleViewerModel>();
        public ObservableCollection<ModuleViewerModel> Modules
        {
            get { return _modules; }
            set { _modules = value; Changed("Modules"); }
        }
        
        public ModuleView()
		{
            InitializeComponent();
        }

        private static void OnVersionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ModuleView)?.OnVersionChanged();
        }

        protected virtual void OnVersionChanged()
        {
            Modules.Clear();
            if(Version != null)
                Modules.Add(new ModuleViewerModel("HauptModul", Version.Modules));
            CurrentIndex = 0;
        }

        public void ShowItem(object item)
        {
            if(item is Module)
            {
                ModuleList.ScrollIntoView(item);
                ModuleList.SelectedItem = item;
            } else {
                int index = item switch{
                    Models.Union => 2,
                    Models.Parameter => 3,
                    Models.ParameterRef => 4,
                    Models.ComObject => 5,
                    Models.ComObjectRef => 6,
                    Models.Dynamic.IDynItems => 7,
                    _ => -1
                };

                if(index == -1) return;
                ModuleTabs.SelectedIndex = index;
                ((ModuleTabs.Items[index] as TabItem).Content as ISelectable).ShowItem(item);
            }
        }

        
        private void ResetId(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException("Not Implemented!");
            //((sender as Button).DataContext as Models.Message).Id = -1;
        }

        private void ClickAddModule(object sender, RoutedEventArgs e)
        {
            Models.Module mod = new Models.Module() { UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(CurrentModule.Modules)};
            mod.Arguments.Add(new Models.Argument() { Name = "argParas", UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(mod.Arguments) });
            mod.Arguments.Add(new Models.Argument() { Name = "argComs", UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(mod.Arguments) });
            //mod.Arguments.Add(new Models.Argument() { Name = "argChan", UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(mod.Arguments) });
            mod.ParameterBaseOffset = mod.Arguments[0];
            mod.ComObjectBaseNumber = mod.Arguments[1];
            mod.Dynamics.Add(new Models.Dynamic.DynamicModule());
            CurrentModule.Modules.Add(mod);
        }

        private void ClickRemoveModule(object sender, RoutedEventArgs e)
        {
            Models.Module mod = ModuleList.SelectedItem as Models.Module;
            if(mod.IsOpenKnxModule)
            {
                MessageBox.Show(Properties.Messages.openknx_modules_remove, Properties.Messages.openknx_modules_title);
                return;
            }
            CurrentModule.Modules.Remove(mod);
            RemoveModule(Version.Dynamics[0], mod);
        }

        private void RemoveModule(Models.Dynamic.IDynItems item, Models.Module mod)
        {
            if(item is Models.Dynamic.DynModule dm)
                dm.ModuleObject = null;

            if(item.Items != null)
                foreach(Models.Dynamic.IDynItems ditem in item.Items)
                    RemoveModule(ditem, mod);
        }

        private void OnOpenSubModules(object sender, RoutedEventArgs e)
        {
            Module model = ModuleList.SelectedItem as Module;
            Modules.Add(new ModuleViewerModel(model.Name, model.Modules));
            CurrentIndex++;
        }



        
        private void ClickShowClean(object sender, RoutedEventArgs e)
        {
            Module model = ModuleList.SelectedItem as Module;
            Models.ClearResult res = ClearHelper.ShowUnusedElements(model);

            string msg = Properties.Messages.checkv_not_used + "\r\n" +
                    $"{res.ParameterTypes}\tParameterTypes\r\n" +
                    $"{res.Parameters}\tParameter\r\n" +
                    $"{res.ParameterRefs}\tParameterRefs\r\n" +
                    $"{res.Unions}\tUnions\r\n" +
                    $"{res.ComObjects}\tComObjects\r\n" +
                    $"{res.ComObjectRefs}\tComObjectRefs";
            MessageBox.Show(msg);
        }

        private void ClickDoClean(object sender, RoutedEventArgs e)
        {
            var msgRes = MessageBox.Show(Properties.Messages.checkv_delete, Properties.Messages.checkv_delete_title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            int countParameterTypes = 0;
            int countParameters = 0;
            int countParameterRefs = 0;
            int countUnions = 0;
            int countComObjects = 0;
            int countComObjectRefs = 0;
            
            Module model = ModuleList.SelectedItem as Module;

            Models.ClearResult res;
            if(msgRes == MessageBoxResult.Yes)
            {
                int sum = 0;
                do {
                    res = ClearHelper.ShowUnusedElements(model);
                    countParameterTypes += res.ParameterTypes;
                    countParameters += res.Parameters;
                    countParameterRefs += res.ParameterRefs;
                    countUnions += res.Unions;
                    countComObjects += res.ComObjects;
                    countComObjectRefs += res.ComObjectRefs;
                    sum = res.ParameterTypes + res.Parameters + res.ParameterRefs + res.Unions + res.ComObjects + res.ComObjectRefs;
                    System.Diagnostics.Debug.WriteLine("Summe: " + sum);
                    ClearHelper.RemoveUnusedElements(model);
                } while(sum > 0);
            } else if(msgRes == MessageBoxResult.No) {
                res = ClearHelper.ShowUnusedElements(model);
                countParameterTypes = res.ParameterTypes;
                countParameters = res.Parameters;
                countParameterRefs = res.ParameterRefs;
                countUnions = res.Unions;
                countComObjects = res.ComObjects;
                countComObjectRefs = res.ComObjectRefs;
                ClearHelper.RemoveUnusedElements(Version);
            } else {
                return;
            }

            string msg = Properties.Messages.checkv_deleted + "\r\n" +
                    $"{countParameterTypes}\tParameterTypes\r\n" +
                    $"{countParameters}\tParameter\r\n" +
                    $"{countParameterRefs}\tParameterRefs\r\n" +
                    $"{countUnions}\tUnions\r\n" +
                    $"{countComObjects}\tComObjects\r\n" +
                    $"{countComObjectRefs}\tComObjectRefs";
            MessageBox.Show(msg);
        }
        
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
