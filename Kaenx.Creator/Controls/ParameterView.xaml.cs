using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class ParameterView : UserControl, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;


        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(ParameterView), new PropertyMetadata(OnVersionChangedCallback));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(ParameterView), new PropertyMetadata(OnModuleChangedCallback));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }

        private ObservableCollection<Parameter> _queryList = new ObservableCollection<Parameter>();
        public ObservableCollection<Parameter> QueryList {
            get { return _queryList; }
            set { _queryList = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("QueryList")); }
        }
        
        public ParameterView()
		{
            InitializeComponent();
        }

        private void QueryChanged(object sender, TextChangedEventArgs e)
        {
            SearchQuery((sender as TextBox).Text);
        }

        private void SearchQuery(string query)
        {
            QueryList.Clear();
            query = query.ToLower();
            foreach(Parameter para in Module.Parameters.Where(p => p.Name.ToLower().Contains(query)))
                QueryList.Add(para);
        }

        private static void OnVersionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ParameterView)?.OnVersionChanged();
        }

        private static void OnModuleChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ParameterView)?.OnModuleChanged();
        }

        protected virtual void OnVersionChanged() {
            
        }
        
        protected virtual void OnModuleChanged() {
            if(Module != null)
            {
                query.Text = "";
                QueryList.Clear();
                foreach(Parameter para in Module.Parameters)
                    QueryList.Add(para);

                Module.Parameters.CollectionChanged += CollectionChanged;
            }
        }

        private void CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.NewItems != null)
                foreach(Parameter para in e.NewItems)
                    QueryList.Add(para);

            if(e.OldItems != null)
                foreach(Parameter para in e.OldItems)
                    QueryList.Remove(para);

            SearchQuery(query.Text);
        }
        
        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Models.Parameter para = new Models.Parameter() {
                UId = AutoHelper.GetNextFreeUId(Module.Parameters)
            };
            foreach(Models.Language lang in Version.Languages) {
                para.Text.Add(new Models.Translation(lang, "Dummy"));
            }
            Module.Parameters.Add(para);

            if(Version.IsParameterRefAuto){
                Module.ParameterRefs.Add(new Models.ParameterRef(para) { UId = AutoHelper.GetNextFreeUId(Module.ParameterRefs) });
            }
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            Parameter para = ParamList.SelectedItem as Models.Parameter;
            Module.Parameters.Remove(para);

            if(Version.IsParameterRefAuto)
            {
                foreach(ParameterRef pref in Module.ParameterRefs.Where(p => p.ParameterObject == para).ToList())
                    Module.ParameterRefs.Remove(pref);
            }
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.Parameter).Id = -1;
        }
    }
}