using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        public ObservableCollection<ParameterRef> ParameterRefsList { get { return Module?.ParameterRefs; } }
        public ObservableCollection<ComObject> ComObjectsList { get { return Module?.ComObjects; } }

        public ComObjectRefView()
		{
            InitializeComponent();
        }

        private static void OnModuleChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ComObjectRefView)?.OnModuleChanged();
        }

        protected virtual void OnModuleChanged()
        {
            Changed("ParameterRefsList");
            Changed("ComObjectsList");
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
