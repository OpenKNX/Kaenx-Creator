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
    public partial class UnionView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(UnionView), new PropertyMetadata(null));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(UnionView), new PropertyMetadata(null));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }

        public ObservableCollection<Memory> MemoriesList { get { return Version?.Memories; } }

        public UnionView()
		{
            InitializeComponent();
        }

        private static void OnVersionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as UnionView)?.OnVersionChanged();
        }

        protected virtual void OnVersionChanged() {
            Changed("MemoriesList");
        }
        

        private void ClickAddUnion(object sender, RoutedEventArgs e)
        {
            Models.IVersionBase ver = Module as Models.IVersionBase;
            ver.Unions.Add(new Models.Union() { UId = AutoHelper.GetNextFreeUId(ver.Unions)});
        }
        
        private void ClickRemoveUnion(object sender, RoutedEventArgs e)
        {
            Models.IVersionBase ver = Module as Models.IVersionBase;
            ver.Unions.Remove(UnionList.SelectedItem as Models.Union);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
