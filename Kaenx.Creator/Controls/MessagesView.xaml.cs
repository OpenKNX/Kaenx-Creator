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
    public partial class MessagesView : UserControl, INotifyPropertyChanged
    {
        public static readonly System.Windows.DependencyProperty VersionProperty = System.Windows.DependencyProperty.Register("Version", typeof(AppVersion), typeof(MessagesView), new System.Windows.PropertyMetadata(null));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        
        public MessagesView()
		{
            InitializeComponent();
        }

        private void ClickAdd(object sender, RoutedEventArgs e)
        {
            Message msg = new Message() { 
                UId = AutoHelper.GetNextFreeUId(Version.Messages),
                Name = "dummy" 
            };
            Version.Messages.Add(msg);

            foreach(Language lang in Version.Languages)
                msg.Text.Add(new Translation(lang, ""));
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {

        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.Message).Id = -1;
        }

        private void ManuelId(object sender, RoutedEventArgs e)
        {
            PromptDialog diag = new PromptDialog("Neue Nachricht ID", "ID Manuell");
            if(diag.ShowDialog() == true)
            {
                int id;
                if(!int.TryParse(diag.Answer, out id))
                {
                    MessageBox.Show("Bitte geben Sie eine Ganzzahl ein.", "Eingabefehler");
                    return;
                }
                Message msg = Version.Messages.SingleOrDefault(p => p.Id == id);
                if(msg != null)
                {
                    MessageBox.Show($"Die ID {id} wird bereits von der Nachricht {msg.Name} verwendet.", "Doppelte ID");
                    return;
                }
                ((sender as Button).DataContext as Models.Message).Id = id;
            }
        }
    
        private void AutoId(object sender, RoutedEventArgs e)
        {
            Models.Message msg = (sender as Button).DataContext as Models.Message;
            msg.Id = AutoHelper.GetNextFreeId(Version, "Messages");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
