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
    public partial class ComObjectRefView : UserControl, INotifyPropertyChanged, IFilterable, ISelectable
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

        private TextFilter _filter;
        private object _selectedItem = null;

        public void FilterShow()
        {
            _filter.Show();
            ComobjectRefList.SelectedItem = _selectedItem;
        }

        public void FilterHide()
        {
            _filter.Hide();
            _selectedItem = ComobjectRefList.SelectedItem;
            ComobjectRefList.SelectedItem = null;
        }

        public void ShowItem(object item)
        {
            ComobjectRefList.ScrollIntoView(item);
            ComobjectRefList.SelectedItem = item;
        }

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
            if(e.OldValue != null)
                (e.OldValue as IVersionBase).ComObjectRefs.CollectionChanged -= RefsChanged;

            if(e.NewValue != null)
                (e.NewValue as IVersionBase).ComObjectRefs.CollectionChanged += RefsChanged;

            if(Module == null) return;
            _filter = new TextFilter(Module.ComObjectRefs, query);
        }

        private void RefsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.OldItems != null)
            {
                foreach(ComObjectRef cref in e.OldItems)
                    DeleteDynamicRef(Module.Dynamics[0], cref);
            }
        }

        private bool CheckDynamicRef(IDynItems item, ComObjectRef cref)
        {
            bool flag = false;

            switch(item)
            {
                case DynComObject dc:
                    if(dc.ComObjectRefObject == cref)
                        flag = true;
                    break;
            }

            if(flag)
                return true;

            if(item.Items != null)
                foreach(IDynItems ditem in item.Items)
                    if(CheckDynamicRef(ditem, cref))
                        flag = true;

            return flag;
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
            ComobjectRefList.ScrollIntoView(cref);
            ComobjectRefList.SelectedItem = cref;
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            ComObjectRef cref = ComobjectRefList.SelectedItem as Models.ComObjectRef;

             if(CheckDynamicRef(Module.Dynamics[0], cref)
                && MessageBoxResult.No == MessageBox.Show("Dieser ComObjectRef wird mindestens ein mal im Dynamic benutzt. Wirklich löschen?", "ComObjectRef löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                    return;

            Module.ComObjectRefs.Remove(cref);
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.ComObjectRef).Id = -1;
        }
        
        private void ManuelId(object sender, RoutedEventArgs e)
        {
            PromptDialog diag = new PromptDialog("Neue ComObjectRef ID", "ID Manuell");
            if(diag.ShowDialog() == true)
            {
                long id;
                if(!long.TryParse(diag.Answer, out id))
                {
                    MessageBox.Show("Bitte geben Sie eine Ganzzahl ein.", "Eingabefehler");
                    return;
                }
                ComObjectRef ele = Module.ComObjectRefs.SingleOrDefault(p => p.Id == id);
                if(ele != null)
                {
                    MessageBox.Show($"Die ID {id} wird bereits von ComObjectRef {ele.Name} verwendet.", "Doppelte ID");
                    return;
                }
                ((sender as Button).DataContext as Models.ComObjectRef).Id = id;
            }
        }
    
        private void AutoId(object sender, RoutedEventArgs e)
        {
            Models.ComObjectRef ele = (sender as Button).DataContext as Models.ComObjectRef;
            long oldId = ele.Id;
            ele.Id = -1;
            ele.Id = AutoHelper.GetNextFreeId(Module, "ComObjectRefs");
            if(ele.Id == oldId)
                MessageBox.Show("Das Element hat bereits die erste freie ID", "Automatische ID");
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
