using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class ParameterTypeView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty BaggagesProperty = DependencyProperty.Register("Baggages", typeof(ObservableCollection<Baggage>), typeof(ParameterTypeView), new PropertyMetadata(null));
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(ParameterTypeView), new PropertyMetadata(OnVersionChangedCallback));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public ObservableCollection<Baggage> Baggages {
            get { return (ObservableCollection<Baggage>)GetValue(BaggagesProperty); }
            set { SetValue(BaggagesProperty, value); }
        }

        public ParameterTypeView()
		{
            InitializeComponent();
        }

        private static void OnVersionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ParameterTypeView)?.OnVersionChanged(e);
        }

        protected virtual void OnVersionChanged(DependencyPropertyChangedEventArgs e)
        {
            if(Version == null) return;
            TextFilter x = new TextFilter(Version.ParameterTypes, query);
        }

        private void ClickAddParamType(object sender, RoutedEventArgs e)
        {
            Version.ParameterTypes.Add(new ParameterType() { UId = AutoHelper.GetNextFreeUId(Version.ParameterTypes) });
        }

        private void ClickAddParamEnum(object sender, RoutedEventArgs e)
        {
            ParameterType ptype = (sender as Button).DataContext as ParameterType;
            ParameterTypeEnum penum = new ParameterTypeEnum();
            foreach(Models.Language lang in Version.Languages)
                penum.Text.Add(new Models.Translation(lang, ""));
            if(ptype.Enums.Count > 0)
                penum.Value = ptype.Enums.OrderByDescending(e => e.Value).First().Value + 1;
            ptype.Enums.Add(penum);
        }

        private void ClickRemoveParamType(object sender, RoutedEventArgs e)
        {
            Version.ParameterTypes.Remove(ListParamTypes.SelectedItem as ParameterType);
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
