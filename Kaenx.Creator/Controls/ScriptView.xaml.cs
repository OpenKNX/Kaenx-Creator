using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;

namespace Kaenx.Creator.Controls
{
    public partial class ScriptView : UserControl, INotifyPropertyChanged
    {
        public static readonly System.Windows.DependencyProperty VersionProperty = System.Windows.DependencyProperty.Register("Version", typeof(AppVersion), typeof(ScriptView), new System.Windows.PropertyMetadata(null));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        
        public ScriptView()
		{
            InitializeComponent();
        }

        private void ClickEdit(object sender, RoutedEventArgs e)
        {
            CodeWindow code = new CodeWindow("index_script.html", Version.Script);
            code.ShowDialog();
            Version.Script = code.CodeNew;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
