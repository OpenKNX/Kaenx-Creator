using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class ComObjectView : UserControl
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(ComObjectView), new PropertyMetadata(OnVersionChangedCallback));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(ComObjectView), new PropertyMetadata(OnModuleChangedCallback));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }
        
        public ComObjectView()
		{
            InitializeComponent();
        }
        
        private static void OnVersionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ComObjectView)?.OnVersionChanged();
        }

        private static void OnModuleChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ComObjectView)?.OnModuleChanged();
        }

        protected virtual void OnModuleChanged()
        {
            //InParameter.ItemsSource = Module?.Parameters;
            InBaseNumber.Visibility = (Module is Models.Module) ? Visibility.Visible : Visibility.Collapsed;
        }
        
        protected virtual void OnVersionChanged() {
            //InType.ItemsSource = Version?.ParameterTypes;
            //InMemory.ItemsSource = Version?.Memories;
        }
        
        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Models.ComObject com = new Models.ComObject() { UId = AutoHelper.GetNextFreeUId(Module.ComObjects) };
            foreach(Models.Language lang in Version.Languages) {
                com.Text.Add(new Models.Translation(lang, "Dummy"));
                com.FunctionText.Add(new Models.Translation(lang, "Dummy"));
            }
            Module.ComObjects.Add(com);

            if(Version.IsComObjectRefAuto){
                Models.ComObjectRef cref = new Models.ComObjectRef(com) { UId = AutoHelper.GetNextFreeUId(Module.ComObjectRefs) };
                foreach(Models.Language lang in Version.Languages) {
                    cref.Text.Add(new Models.Translation(lang, ""));
                    cref.FunctionText.Add(new Models.Translation(lang, ""));
                }
                Module.ComObjectRefs.Add(cref);
            }
        }
        
        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            Models.ComObject com = ComobjectList.SelectedItem as Models.ComObject;
            Module.ComObjects.Remove(com);

            if(Module.IsComObjectRefAuto){
                Module.ComObjectRefs.Remove(Module.ComObjectRefs.Single(c => c.ComObjectObject == com));
            }
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.ComObject).Id = -1;
        }
    }
}