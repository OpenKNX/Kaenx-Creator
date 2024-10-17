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
            _filter = new TextFilter(query);
        }

        private static void OnModuleChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ComObjectRefView)?.OnModuleChanged(e);
        }

        protected virtual void OnModuleChanged(DependencyPropertyChangedEventArgs e)
        {
            if(Module == null) return;
            _filter.ChangeView(Module.ComObjectRefs);
        }

        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Models.ComObjectRef cref = new Models.ComObjectRef() { UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Module.ComObjectRefs) };
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

            List<int> uids = new List<int>();
            ClearHelper.GetIDs(Module.Dynamics[0], uids, false);
            if(uids.Contains(cref.UId) && MessageBoxResult.No == MessageBox.Show(Properties.Messages.comref_delete, Properties.Messages.comref_delete_title, MessageBoxButton.YesNo, MessageBoxImage.Warning))
                    return;

            Module.ComObjectRefs.Remove(cref);
            ClearHelper.ClearIDs(Module.Dynamics[0], cref);
        }

        private void ComObjParaRefHyperlink(object sender, RoutedEventArgs e)
        {
            Models.ComObjectRef com = (sender as System.Windows.Documents.Hyperlink).DataContext as Models.ComObjectRef;
            MainWindow.Instance.GoToItem(com.ParameterRefObject, Module);
        }

        private void ComObjectHyperlink(object sender, RoutedEventArgs e)
        {
            Models.ComObjectRef com = (sender as System.Windows.Documents.Hyperlink).DataContext as Models.ComObjectRef;
            MainWindow.Instance.GoToItem(com.ComObjectObject, Module);
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.ComObjectRef).Id = -1;
        }
        
        private void ManuelId(object sender, RoutedEventArgs e)
        {
            PromptDialog diag = new PromptDialog(Properties.Messages.comref_prompt_id, Properties.Messages.prompt_id);
            if(diag.ShowDialog() == true)
            {
                long id;
                if(!long.TryParse(diag.Answer, out id))
                {
                    MessageBox.Show(Properties.Messages.prompt_error, Properties.Messages.prompt_error_title);
                    return;
                }
                ComObjectRef ele = Module.ComObjectRefs.SingleOrDefault(p => p.Id == id);
                if(ele != null)
                {
                    MessageBox.Show(string.Format(Properties.Messages.prompt_double, id, ele.Name), Properties.Messages.prompt_double_title);
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
            ele.Id = Kaenx.Creator.Classes.Helper.GetNextFreeId(Module, "ComObjectRefs");
            if(ele.Id == oldId)
                MessageBox.Show(Properties.Messages.prompt_auto_error, Properties.Messages.prompt_auto_error_title);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}