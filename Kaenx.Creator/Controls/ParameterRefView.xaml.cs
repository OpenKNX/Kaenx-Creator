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
    public partial class ParameterRefView : UserControl
    {
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
            (sender as ParameterRefView)?.OnModuleChanged();
        }

        protected virtual void OnModuleChanged()
        {
            InParameter.ItemsSource = Module?.Parameters;
        }
        
        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Module.ParameterRefs.Add(new Models.ParameterRef() { UId = AutoHelper.GetNextFreeUId(Module.ParameterRefs) });
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            Module.ParameterRefs.Remove(ParamRefList.SelectedItem as Models.ParameterRef);
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.ParameterRef).Id = -1;
        }

        private void ClickGenerateRefAuto(object sender, RoutedEventArgs e)
        {
            Module.ParameterRefs.Clear();

            foreach(Models.Parameter para in Module.Parameters)
            {
                Models.ParameterRef pref = new Models.ParameterRef();
                pref.UId = AutoHelper.GetNextFreeUId(Module.ParameterRefs);
                pref.Name = para.Name;
                pref.ParameterObject = para;
                Module.ParameterRefs.Add(pref);
            }
        }
    }
}