using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class ComObjectRefView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(ComObjectRefView), new PropertyMetadata(null));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(ComObjectRefView), new PropertyMetadata(OnModuleChangedCallback));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }

        public ObservableCollection<ComObject> ComObjectsList { get { return Module?.ComObjects; } }

        public ComObjectRefView()
		{
            InitializeComponent();
        }

        private static void OnModuleChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ComObjectRefView)?.OnModuleChanged(e);
        }

        protected virtual void OnModuleChanged(DependencyPropertyChangedEventArgs e)
        {
            Changed("ComObjectsList");

            if(e.OldValue != null)
                (e.OldValue as IVersionBase).ComObjectRefs.CollectionChanged -= RefsChanged;

            if(e.NewValue != null)
                (e.NewValue as IVersionBase).ComObjectRefs.CollectionChanged += RefsChanged;
        }

        private void RefsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.OldItems != null)
            {
                foreach(ComObjectRef cref in e.OldItems)
                    DeleteDynamicRef(Module.Dynamics[0], cref);
            }
        }

        private void DeleteDynamicRef(IDynItems item, ComObjectRef cref)
        {
            switch(item)
            {
                case DynComObject dc:
                    if(dc.ComObjectRefObject == cref)
                        dc.ComObjectRefObject = null;
                    break;
            }

            if(item.Items != null)
                foreach(IDynItems ditem in item.Items)
                    DeleteDynamicRef(ditem, cref);
        }
        
        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Models.ComObjectRef cref = new Models.ComObjectRef() { UId = AutoHelper.GetNextFreeUId(Module.ComObjectRefs) };
            foreach(Models.Language lang in Version.Languages) {
                cref.Text.Add(new Models.Translation(lang, ""));
                cref.FunctionText.Add(new Models.Translation(lang, ""));
            }
            Module.ComObjectRefs.Add(cref);
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            Module.ComObjectRefs.Remove(ComobjectRefList.SelectedItem as Models.ComObjectRef);
        }

        private void ClickGenerateRefAuto(object sender, RoutedEventArgs e)
        {
            Module.ComObjectRefs.Clear();

            foreach(Models.ComObject com in Module.ComObjects)
            {
                Models.ComObjectRef cref = new Models.ComObjectRef();
                cref.UId = AutoHelper.GetNextFreeUId(Module.ComObjectRefs);
                cref.Name = com.Name;
                cref.ComObjectObject = com;
                Module.ComObjectRefs.Add(cref);
            }
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.ComObjectRef).Id = -1;
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
