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
    public partial class ParameterTypeView : UserControl, INotifyPropertyChanged, IFilterable, ISelectable
    {
        public static readonly DependencyProperty BaggagesProperty = DependencyProperty.Register("Baggages", typeof(ObservableCollection<Baggage>), typeof(ParameterTypeView), new PropertyMetadata(null));
        public static readonly DependencyProperty IconsProperty = DependencyProperty.Register("Icons", typeof(ObservableCollection<Icon>), typeof(ParameterTypeView), new PropertyMetadata(null));
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(ParameterTypeView), new PropertyMetadata(OnVersionChangedCallback));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public ObservableCollection<Baggage> Baggages {
            get { return (ObservableCollection<Baggage>)GetValue(BaggagesProperty); }
            set { SetValue(BaggagesProperty, value); }
        }
        public ObservableCollection<Icon> Icons {
            get { return (ObservableCollection<Icon>)GetValue(IconsProperty); }
            set { SetValue(IconsProperty, value); }
        }

        public ParameterTypeView()
		{
            InitializeComponent();
            _filter = new TextFilter(query);
        }

        private TextFilter _filter;
        private object _selectedItem = null;

        public void FilterShow()
        {
            _filter.Show();
            ListParamTypes.SelectedItem = _selectedItem;
        }

        public void FilterHide()
        {
            _filter.Hide();
            _selectedItem = ListParamTypes.SelectedItem;
            ListParamTypes.SelectedItem = null;
        }

        public void ShowItem(object item)
        {
            ListParamTypes.ScrollIntoView(item);
            ListParamTypes.SelectedItem = item;
        }

        private static void OnVersionChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            (sender as ParameterTypeView)?.OnVersionChanged(e);
        }

        protected virtual void OnVersionChanged(DependencyPropertyChangedEventArgs e)
        {
            if(Version == null) return;
            _filter.ChangeView(Version.ParameterTypes);
        }

        private void ClickAddParamType(object sender, RoutedEventArgs e)
        {
            ParameterType type = new ParameterType() { UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Version.ParameterTypes) };
            Version.ParameterTypes.Add(type);
            ListParamTypes.ScrollIntoView(type);
            ListParamTypes.SelectedItem = type;
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
            ParameterType type = ListParamTypes.SelectedItem as ParameterType;

            if(CheckType(type))
            {
                if(MessageBoxResult.No == MessageBox.Show(Properties.Messages.paratype_delete, Properties.Messages.paratype_delete_title, MessageBoxButton.YesNo, MessageBoxImage.Warning))
                    return;

                RemoveType(type);
            }

            Version.ParameterTypes.Remove(type);
        }

        private bool CheckType(ParameterType type)
        {
            bool flag = false;

            if(Version.Parameters.Any(p => p.ParameterTypeObject == type))
                flag = true;

            if(flag) return true;

            foreach(Models.Module mod in Version.Modules)
            {
                if(mod.Parameters.Any(p => p.ParameterTypeObject == type))
                {
                    flag = true;
                    break;
                }
            }

            return flag;
        }

        private void RemoveType(ParameterType type)
        {
            foreach(Parameter para in Version.Parameters.Where(p => p.ParameterTypeObject == type))
                para.ParameterTypeObject = null;

            foreach(Models.Module mod in Version.Modules)
                foreach(Parameter para in mod.Parameters.Where(p => p.ParameterTypeObject == type))
                    para.ParameterTypeObject = null;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
