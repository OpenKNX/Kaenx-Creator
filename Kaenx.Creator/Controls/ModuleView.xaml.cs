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
    public partial class ModuleView : UserControl, INotifyPropertyChanged
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

        
        private void ResetId(object sender, RoutedEventArgs e)
        {
            throw new NotImplementedException("Nicht implementiert");
            //((sender as Button).DataContext as Models.Message).Id = -1;
        }


        private void ClickAddModule(object sender, RoutedEventArgs e)
        {
            Models.Module mod = new Models.Module() { UId = AutoHelper.GetNextFreeUId(CurrentModule.Modules)};
            mod.Arguments.Add(new Models.Argument() { Name = "argParas", UId = AutoHelper.GetNextFreeUId(mod.Arguments) });
            mod.Arguments.Add(new Models.Argument() { Name = "argComs", UId = AutoHelper.GetNextFreeUId(mod.Arguments) });
            //mod.Arguments.Add(new Models.Argument() { Name = "argChan", UId = AutoHelper.GetNextFreeUId(mod.Arguments) });
            mod.ParameterBaseOffset = mod.Arguments[0];
            mod.ComObjectBaseNumber = mod.Arguments[1];
            mod.Dynamics.Add(new Models.Dynamic.DynamicModule());
            CurrentModule.Modules.Add(mod);
        }

        private void ClickRemoveModule(object sender, RoutedEventArgs e)
        {
            Models.Module mod = ModuleList.SelectedItem as Models.Module;
            CurrentModule.Modules.Remove(mod);
            //RemoveModule(SelectedVersion.Model.Dynamics[0], mod);
            //TODO make it work again
        }

        private void RemoveModule(Models.Dynamic.IDynItems item, Models.Module mod)
        {/*
            if(item is Models.Dynamic.DynModule dm)
                dm.ModuleObject = null;

            if(item.Items != null)
                foreach(Models.Dynamic.IDynItems ditem in item.Items)
                    RemoveModule(ditem, mod);
            */
        }

        private void CurrentCellChanged(object sender, EventArgs e)
        {
            Models.Memory mem = (sender as DataGrid).DataContext as Models.Memory;
            if(mem == null) return;
            DataGridCellInfo cell = (sender as DataGrid).CurrentCell;
            Models.MemorySection sec = cell.Item as Models.MemorySection;
            if(!cell.IsValid || (cell.Column.DisplayIndex > (sec.Bytes.Count - 1))) return;

            mem.CurrentMemoryByte = sec.Bytes[cell.Column.DisplayIndex];
        }

        private void OnOpenSubModules(object sender, RoutedEventArgs e)
        {
            Module model = ModuleList.SelectedItem as Module;
            Modules.Add(new ModuleViewerModel(model.Name, model.Modules));
            CurrentIndex++;
        }
        

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
