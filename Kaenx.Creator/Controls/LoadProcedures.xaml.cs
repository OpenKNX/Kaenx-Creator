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
            monaco.Source = new Uri(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Data", "Monaco", "index.html"));
            Load();
		}

        


        private async void Load()
        {
            System.Console.WriteLine("Load");
            await monaco.EnsureCoreWebView2Async();
            await System.Threading.Tasks.Task.Delay(1000);
            System.Console.WriteLine("Ausgeführt");
            string xml = Version.Procedure.Replace("'", "\\'").Replace("\r\n", "\\r\\n");
            await monaco.ExecuteScriptAsync($"editor.setValue('{xml}');");
            System.Console.WriteLine($"editor.setValue('{xml}');");
        }
    }
}