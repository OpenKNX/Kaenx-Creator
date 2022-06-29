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
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(ParameterTypeView), new PropertyMetadata(OnVersionChangedCallback));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
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
            TextFilter x = new TextFilter(Version.ParameterTypes, query);
        }

        private void ClickAddParamType(object sender, RoutedEventArgs e)
        {
            Version.ParameterTypes.Add(new ParameterType() { UId = AutoHelper.GetNextFreeUId(Version.ParameterTypes) });
        }

        private void ClickAddParamEnum(object sender, RoutedEventArgs e)
        {
            Models.ParameterType ptype = (sender as Button).DataContext as Models.ParameterType;
            Models.ParameterTypeEnum penum = new Models.ParameterTypeEnum();
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
