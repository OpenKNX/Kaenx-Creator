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
    public partial class CheckView : UserControl, INotifyPropertyChanged
    {
        public static readonly System.Windows.DependencyProperty GeneralProperty = System.Windows.DependencyProperty.Register("General", typeof(MainModel), typeof(CheckView), new System.Windows.PropertyMetadata(null));
        public static readonly System.Windows.DependencyProperty VersionProperty = System.Windows.DependencyProperty.Register("Version", typeof(AppVersion), typeof(CheckView), new System.Windows.PropertyMetadata(null));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public MainModel General {
            get { return (MainModel)GetValue(GeneralProperty); }
            set { SetValue(GeneralProperty, value); }
        }
        
        public ObservableCollection<Models.PublishAction> Actions { get; set; } = new ObservableCollection<Models.PublishAction>();

        public CheckView()
		{
            InitializeComponent();
        }

        public void ResetActions()
        {
            Actions.Clear();
        }

        private void ClickShowClean(object sender, RoutedEventArgs e)
        { 
            Actions.Clear();
            Models.ClearResult res = ClearHelper.ShowUnusedElements(Version);

            Actions.Add(new PublishAction() { Text = Properties.Messages.checkv_not_used });
            Actions.Add(new PublishAction() { Text = $"{res.ParameterTypes}\tParameterTypes", State = res.ParameterTypes > 0 ? PublishState.Warning : PublishState.Info });
            Actions.Add(new PublishAction() { Text = $"{res.Parameters}\tParameter", State = res.Parameters > 0 ? PublishState.Warning : PublishState.Info });
            Actions.Add(new PublishAction() { Text = $"{res.ParameterRefs}\tParameterRefs", State = res.ParameterRefs > 0 ? PublishState.Warning : PublishState.Info });
            Actions.Add(new PublishAction() { Text = $"{res.Unions}\tUnions", State = res.Unions > 0 ? PublishState.Warning : PublishState.Info });
            Actions.Add(new PublishAction() { Text = $"{res.ComObjects}\tComObjects", State = res.ComObjects > 0 ? PublishState.Warning : PublishState.Info });
            Actions.Add(new PublishAction() { Text = $"{res.ComObjectRefs}\tComObjectRefs", State = res.ComObjectRefs > 0 ? PublishState.Warning : PublishState.Info });
        }

        private void ClickDoClean(object sender, RoutedEventArgs e)
        {
            Actions.Clear();
            var msgRes = MessageBox.Show(Properties.Messages.checkv_delete, Properties.Messages.checkv_delete_title, MessageBoxButton.YesNoCancel, MessageBoxImage.Question);

            int countParameterTypes = 0;
            int countParameters = 0;
            int countParameterRefs = 0;
            int countUnions = 0;
            int countComObjects = 0;
            int countComObjectRefs = 0;

            Models.ClearResult res;
            if(msgRes == MessageBoxResult.Yes)
            {
                int sum = 0;
                do {
                    res = ClearHelper.ShowUnusedElements(Version);
                    countParameterTypes += res.ParameterTypes;
                    countParameters += res.Parameters;
                    countParameterRefs += res.ParameterRefs;
                    countUnions += res.Unions;
                    countComObjects += res.ComObjects;
                    countComObjectRefs += res.ComObjectRefs;
                    sum = res.ParameterTypes + res.Parameters + res.ParameterRefs + res.Unions + res.ComObjects + res.ComObjectRefs;
                    System.Diagnostics.Debug.WriteLine("Summe: " + sum);
                    ClearHelper.RemoveUnusedElements(Version);
                } while(sum > 0);
            } else if(msgRes == MessageBoxResult.No) {
                res = ClearHelper.ShowUnusedElements(Version);
                countParameterTypes = res.ParameterTypes;
                countParameters = res.Parameters;
                countParameterRefs = res.ParameterRefs;
                countUnions = res.Unions;
                countComObjects = res.ComObjects;
                countComObjectRefs = res.ComObjectRefs;
                ClearHelper.RemoveUnusedElements(Version);
            } else {
                return;
            }

            Actions.Add(new PublishAction() { Text = Properties.Messages.checkv_deleted });
            Actions.Add(new PublishAction() { Text = $"{countParameterTypes}\tParameterTypes", State = countParameterTypes > 0 ? PublishState.Warning : PublishState.Info });
            Actions.Add(new PublishAction() { Text = $"{countParameters}\tParameter", State = countParameters > 0 ? PublishState.Warning : PublishState.Info });
            Actions.Add(new PublishAction() { Text = $"{countParameterRefs}\tParameterRefs", State = countParameterRefs > 0 ? PublishState.Warning : PublishState.Info });
            Actions.Add(new PublishAction() { Text = $"{countUnions}\tUnions", State = countUnions > 0 ? PublishState.Warning : PublishState.Info });
            Actions.Add(new PublishAction() { Text = $"{countComObjects}\tComObjects", State = countComObjects > 0 ? PublishState.Warning : PublishState.Info });
            Actions.Add(new PublishAction() { Text = $"{countComObjectRefs}\tComObjectRefs", State = countComObjectRefs > 0 ? PublishState.Warning : PublishState.Info });
        }

        private void ClickCheckVersion(object sender, RoutedEventArgs e)
        {
            Actions.Clear();
            bool showOnlyErrors = MessageBoxResult.Yes == MessageBox.Show(Properties.Messages.checkv_warnings, Properties.Messages.checkv_warnings_title, MessageBoxButton.YesNo, MessageBoxImage.Question);
            Actions.Add(new PublishAction() { Text = Properties.Messages.checkv_started });
            CheckHelper.CheckVersion(General, Actions, showOnlyErrors);
            Actions.Add(new PublishAction() { Text = Properties.Messages.checkv_fin });
        }
        
        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
           PublishAction action = (sender as System.Windows.Documents.Hyperlink).DataContext as PublishAction;

           MainWindow.Instance.GoToItem(action.Item, action.Module);
        }


        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
