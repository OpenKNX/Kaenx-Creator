using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class ParameterRefView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(ParameterRefView), new PropertyMetadata(OnModuleChangedCallback));
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }
        
        public ParameterRefView()
		{
            InitializeComponent();
        }

        private static void OnModuleChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ParameterRefView)?.OnModuleChanged(e);
        }

        protected virtual void OnModuleChanged(DependencyPropertyChangedEventArgs e)
        {
            
            if(e.OldValue != null)
                (e.OldValue as IVersionBase).ParameterRefs.CollectionChanged -= RefsChanged;

            if(e.NewValue != null)
                (e.NewValue as IVersionBase).ParameterRefs.CollectionChanged += RefsChanged;

            if(Module == null) return;
            TextFilter filter = new TextFilter(Module.ParameterRefs, query);
        }
        
        private void RefsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.OldItems != null)
            {
                foreach(ParameterRef pref in e.OldItems)
                    DeleteDynamicRef(Module.Dynamics[0], pref);
            }
        }

        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Module.ParameterRefs.Add(new ParameterRef() { UId = AutoHelper.GetNextFreeUId(Module.ParameterRefs) });
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            ParameterRef pref = ParamRefList.SelectedItem as ParameterRef;
            Module.ParameterRefs.Remove(pref);
        }

        private void DeleteDynamicRef(IDynItems item, ParameterRef pref)
        {
            switch(item)
            {
                case DynChannel dc:
                    if(dc.UseTextParameter && dc.ParameterRefObject == pref)
                        dc.ParameterRefObject = null;
                    break;

                case DynParaBlock dpb:
                    if(dpb.UseParameterRef && dpb.ParameterRefObject == pref)
                        dpb.ParameterRefObject = null;
                    if(dpb.UseTextParameter && dpb.TextRefObject == pref)
                        dpb.TextRefObject = null;
                    break;

                case DynParameter dp:
                    if(dp.ParameterRefObject == pref)
                        dp.ParameterRefObject = null;
                    break;

                case DynChoose dch:
                    if(dch.ParameterRefObject == pref)
                        dch.ParameterRefObject = null;
                    break;
            }

            if(item.Items != null)
                foreach(IDynItems ditem in item.Items)
                    DeleteDynamicRef(ditem, pref);
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as ParameterRef).Id = -1;
        }
    }
}