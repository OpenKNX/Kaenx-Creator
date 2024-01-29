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
                UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Version.Messages),
                Name = "dummy" 
            };
            Version.Messages.Add(msg);

            foreach(Language lang in Version.Languages)
                msg.Text.Add(new Translation(lang, ""));
        }

        private void ClickRemove(object sender, RoutedEventArgs e)
        {
            Version.Messages.Remove((sender as MenuItem).DataContext as Models.Message);
        }

        private void ResetId(object sender, RoutedEventArgs e)
        {
            ((sender as Button).DataContext as Models.Message).Id = -1;
        }

        private void ManuelId(object sender, RoutedEventArgs e)
        {
            PromptDialog diag = new PromptDialog(Properties.Messages.message_prompt_id, Properties.Messages.prompt_id);
            if(diag.ShowDialog() == true)
            {
                int id;
                if(!int.TryParse(diag.Answer, out id))
                {
                    MessageBox.Show(Properties.Messages.prompt_error, Properties.Messages.prompt_error_title);
                    return;
                }
                Message msg = Version.Messages.SingleOrDefault(p => p.Id == id);
                if(msg != null)
                {
                    MessageBox.Show(string.Format(Properties.Messages.prompt_double, id, msg.Name), Properties.Messages.prompt_double_title);
                    return;
                }
                ((sender as Button).DataContext as Models.Message).Id = id;
            }
        }
    
        private void AutoId(object sender, RoutedEventArgs e)
        {
            Models.Message msg = (sender as Button).DataContext as Models.Message;
            long oldId = msg.Id;
            msg.Id = -1;
            msg.Id = Kaenx.Creator.Classes.Helper.GetNextFreeId(Version, "Messages");
            if(msg.Id == oldId)
                MessageBox.Show(Properties.Messages.prompt_auto_error, Properties.Messages.prompt_auto_error_title);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
