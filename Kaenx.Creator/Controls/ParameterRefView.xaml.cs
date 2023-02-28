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
    public partial class ParameterRefView : UserControl, IFilterable, ISelectable
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(ParameterRefView), new PropertyMetadata(OnModuleChangedCallback));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(ParameterRefView), new PropertyMetadata(OnModuleChangedCallback));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }

        private TextFilter _filter;
        private object _selectedItem = -1;

        public void FilterShow()
        {
            _filter.Show();
            ParamRefList.SelectedItem = _selectedItem;
        }

        public void FilterHide()
        {
            _filter.Hide();
            _selectedItem = ParamRefList.SelectedItem;
            ParamRefList.SelectedItem = null;
        }

        public void ShowItem(object item)
        {
            ParamRefList.ScrollIntoView(item);
            ParamRefList.SelectedItem = item;
        }

        public ParameterRefView()
		{
            InitializeComponent();
            _filter = new TextFilter(query);
        }

        private static void OnModuleChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ParameterRefView)?.OnModuleChanged(e);
        }

        protected virtual void OnModuleChanged(DependencyPropertyChangedEventArgs e)
        {
            if(e.NewValue != null)
            {
                _filter.ChangeView((e.NewValue as IVersionBase).ParameterRefs);
            }

        }
        
        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            ParameterRef pref = new ParameterRef() { UId = AutoHelper.GetNextFreeUId(Module.ParameterRefs) };
            foreach(Language lang in Version.Languages)
            {
                pref.Text.Add(new Translation(lang, ""));
                pref.Suffix.Add(new Translation(lang, ""));
            }

            Module.ParameterRefs.Add(pref);
            ParamRefList.ScrollIntoView(pref);
            ParamRefList.SelectedItem = pref;
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            ParameterRef pref = ParamRefList.SelectedItem as ParameterRef;

            List<int> uids = new List<int>();
            ClearHelper.GetIDs(Module.Dynamics[0], uids, true);
            if(uids.Contains(pref.UId) && MessageBoxResult.No == MessageBox.Show("Dieser ParameterRef wird mindestens ein mal im Dynamic benutzt. Wirklich löschen?", "ParameterRef löschen", MessageBoxButton.YesNo, MessageBoxImage.Warning))
                return;

            Module.ParameterRefs.Remove(pref);
            ClearHelper.ClearIDs(Module.Dynamics[0], pref);
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as ParameterRef).Id = -1;
        }
        
        private void ManuelId(object sender, RoutedEventArgs e)
        {
            PromptDialog diag = new PromptDialog("Neue ParameterRef ID", "ID Manuell");
            if(diag.ShowDialog() == true)
            {
                long id;
                if(!long.TryParse(diag.Answer, out id))
                {
                    MessageBox.Show("Bitte geben Sie eine Ganzzahl ein.", "Eingabefehler");
                    return;
                }
                ParameterRef ele = Module.ParameterRefs.SingleOrDefault(p => p.Id == id);
                if(ele != null)
                {
                    MessageBox.Show($"Die ID {id} wird bereits von ParameterRef {ele.Name} verwendet.", "Doppelte ID");
                    return;
                }
                ((sender as Button).DataContext as Models.ParameterRef).Id = id;
            }
        }
    
        private void AutoId(object sender, RoutedEventArgs e)
        {
            Models.ParameterRef ele = (sender as Button).DataContext as Models.ParameterRef;
            long oldId = ele.Id;
            ele.Id = -1;
            ele.Id = AutoHelper.GetNextFreeId(Module, "ParameterRefs");
            if(ele.Id == oldId)
                MessageBox.Show("Das Element hat bereits die erste freie ID", "Automatische ID");
        }
    }
}