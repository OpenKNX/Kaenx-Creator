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
            ParameterRef pref = new ParameterRef() { UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Module.ParameterRefs) };
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
            if(uids.Contains(pref.UId) && MessageBoxResult.No == MessageBox.Show(Properties.Messages.pararef_delete, Properties.Messages.pararef_delete_title, MessageBoxButton.YesNo, MessageBoxImage.Warning))
                return;

            Module.ParameterRefs.Remove(pref);
            ClearHelper.ClearIDs(Module.Dynamics[0], pref);
        }

        private void ClickCheckHyperlink(object sender, RoutedEventArgs e)
        {
            Models.ParameterRef para = (sender as System.Windows.Documents.Hyperlink).DataContext as Models.ParameterRef;
            MainWindow.Instance.GoToItem(para.ParameterObject, Module);
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as ParameterRef).Id = -1;
        }
        
        private void ManuelId(object sender, RoutedEventArgs e)
        {
            PromptDialog diag = new PromptDialog(Properties.Messages.pararef_prompt_id, Properties.Messages.prompt_id);
            if(diag.ShowDialog() == true)
            {
                long id;
                if(!long.TryParse(diag.Answer, out id))
                {
                    MessageBox.Show(Properties.Messages.prompt_error, Properties.Messages.prompt_error_title);
                    return;
                }
                ParameterRef ele = Module.ParameterRefs.SingleOrDefault(p => p.Id == id);
                if(ele != null)
                {
                    MessageBox.Show(string.Format(Properties.Messages.prompt_double, id, ele.Name), Properties.Messages.prompt_double_title);
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
            ele.Id = Kaenx.Creator.Classes.Helper.GetNextFreeId(Module, "ParameterRefs");
            if(ele.Id == oldId)
                MessageBox.Show(Properties.Messages.prompt_auto_error, Properties.Messages.prompt_auto_error_title);
        }
    }
}