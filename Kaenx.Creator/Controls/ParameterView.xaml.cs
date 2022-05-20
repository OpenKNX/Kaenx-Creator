using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class ParameterView : UserControl
    {
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
        
        public ParameterView()
		{
            InitializeComponent();
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
            InType.ItemsSource = Version?.ParameterTypes;
            InMemory.ItemsSource = Version?.Memories;
        }
        
        protected virtual void OnModuleChanged() {
            InUnion.ItemsSource = Module?.Unions;
            //InArgument.ItemsSource = (Module as Models.Module)?.Arguments;
            InBaseOffset.Visibility = (Module is Models.Module) ? Visibility.Visible : Visibility.Collapsed;
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
            Module.Parameters.Remove(ParamList.SelectedItem as Models.Parameter);
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.Parameter).Id = -1;
        }
    }
}