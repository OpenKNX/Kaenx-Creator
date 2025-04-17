using Kaenx.Creator.Classes;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Kaenx.Creator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CodeWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string CodeOld { get; set; }
        public string CodeNew { get; set; }

        private bool _canSave = false;
        public bool CanSave {
            get { return _canSave; }
            set { _canSave = value; PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("CanSave")); }
        }

        public CodeWindow(string page, string code)
        {
			InitializeComponent();
            this.DataContext = this;
            CodeOld = code;
            monaco.Source = new Uri(System.IO.Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, "Data", "Monaco", page)); //"index.html"));
            Load();
            this.Closed += Closing;
        }

        new private void Closing(object sender, EventArgs e)
        {
            if(CodeNew == null)
                CodeNew = CodeOld;
        }

        private async void ClickSave(object sender, RoutedEventArgs e)
        {
            object code = await monaco.ExecuteScriptAsync($"editor.getValue();");
            CodeNew = code.ToString();
            CodeNew = CodeNew.Substring(1, CodeNew.Length -2);
            CodeNew = CodeNew.Replace("\\\"", "\"").Replace("\\'", "'").Replace("\\r\\n", "\r\n").Replace("\\n", "\n");
            CodeNew = System.Text.RegularExpressions.Regex.Unescape(CodeNew);
            this.Close();
        }

        private void ClickClose(object sender, RoutedEventArgs e)
        {
            CodeNew = CodeOld;
            this.Close();
        }

        
        private async void Load()
        {
            await monaco.EnsureCoreWebView2Async();
            string code = "null";
            while(code == "null")
            {
                code = await monaco.ExecuteScriptAsync($"editor.getValue();");
                await Task.Delay(20);
            }
            string xml = CodeOld.Replace("\\", @"\\\").Replace("'", "\\'").Replace("\n", "\\n").Replace("\r", "\\r");
            await monaco.ExecuteScriptAsync($"editor.setValue('{xml}');");
            CanSave = true;
        }
    }
}