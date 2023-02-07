using ICSharpCode.AvalonEdit.CodeCompletion;
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
    public partial class LoadProcedures : UserControl
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(LoadProcedures), new PropertyMetadata());
        public static readonly DependencyProperty MaskProperty = DependencyProperty.Register("Mask", typeof(MaskVersion), typeof(LoadProcedures), new PropertyMetadata());
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Mask {
            get { return (IVersionBase)GetValue(MaskProperty); }
            set { SetValue(MaskProperty, value); }
        }
        
        public LoadProcedures()
		{
			InitializeComponent();
		}

        private void ClickEdit(object sender, RoutedEventArgs e)
        {
            CodeWindow code = new CodeWindow("index_procedure.html", Version.Procedure);
            code.ShowDialog();
            Version.Procedure = code.CodeNew;
        }
    }
}