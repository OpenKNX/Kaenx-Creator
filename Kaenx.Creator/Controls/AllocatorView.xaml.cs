using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class AllocatorView : UserControl
    {
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(AllocatorView), new PropertyMetadata(null));
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }
        public AllocatorView() 
        {
            InitializeComponent();
        }

        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Module.Allocators.Add(new Models.Allocator() { UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Module.Allocators)});
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            Module.Allocators.Remove(AllocatorList.SelectedItem as Models.Allocator);
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.Allocator).Id = -1;
        }
    }
}