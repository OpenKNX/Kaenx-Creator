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
    public partial class UnionView : UserControl, INotifyPropertyChanged, ISelectable
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

        public UnionView()
		{
            InitializeComponent();
        }

        public void ShowItem(object item)
        {
            UnionList.ScrollIntoView(item);
            UnionList.SelectedItem = item;
        }

        private void ClickAddUnion(object sender, RoutedEventArgs e)
        {
            Module.Unions.Add(new Models.Union() { UId = AutoHelper.GetNextFreeUId(Module.Unions)});
        }
        
        private void ClickRemoveUnion(object sender, RoutedEventArgs e)
        {
            Module.Unions.Remove(UnionList.SelectedItem as Models.Union);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
