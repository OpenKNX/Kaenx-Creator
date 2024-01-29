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
    public partial class HelpView : UserControl, INotifyPropertyChanged
    {
        public static readonly System.Windows.DependencyProperty VersionProperty = System.Windows.DependencyProperty.Register("Version", typeof(AppVersion), typeof(HelpView), new System.Windows.PropertyMetadata(null));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        
        public HelpView()
		{
            InitializeComponent();
        }

        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Helptext msg = new Helptext() { 
                UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Version.Helptexts),
                Name = "dummy" 
            };
            Version.Helptexts.Add(msg);

            foreach(Language lang in Version.Languages)
                msg.Text.Add(new Translation(lang, ""));
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            Version.Helptexts.Remove((sender as MenuItem).DataContext as Models.Helptext);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
