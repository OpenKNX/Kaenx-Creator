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
    public partial class ArgumentView : UserControl
    {
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(Module), typeof(ArgumentView), new PropertyMetadata(null));
        public Module Module {
            get { return (Module)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }
        public ArgumentView() 
        {
            InitializeComponent();
        }

        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Module.Arguments.Add(new Models.Argument() { UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Module.Arguments)});
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            Module.Arguments.Remove(ArgumentList.SelectedItem as Models.Argument);
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.Argument).Id = -1;
        }
    }
}