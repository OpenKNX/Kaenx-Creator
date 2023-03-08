using Kaenx.Creator.Classes;
using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Kaenx.Creator.Controls
{
    public partial class MemorySectionsView : UserControl, INotifyPropertyChanged
    {
        public static readonly DependencyProperty MemoryProperty = DependencyProperty.Register("Memory", typeof(Memory), typeof(MemorySectionsView), new PropertyMetadata(null));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(MemorySectionsView), new PropertyMetadata(null));
        public Memory Memory {
            get { return (Memory)GetValue(MemoryProperty); }
            set { SetValue(MemoryProperty, value); }
        }
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }

        private Models.MemoryByte _currentMemoryByte;
        public Models.MemoryByte CurrentMemoryByte
        {
            get { return _currentMemoryByte; }
            set { _currentMemoryByte = value; Changed("CurrentMemoryByte"); }
        }


        public MemorySectionsView() 
        {
            InitializeComponent();
        }



        private void CurrentCellChanged(object sender, EventArgs e)
        {
            DataGridCellInfo cell = (sender as DataGrid).CurrentCell;
            Models.MemorySection sec = cell.Item as Models.MemorySection;
            if(!cell.IsValid || (cell.Column.DisplayIndex > (sec.Bytes.Count - 1))) return;

            CurrentMemoryByte = sec.Bytes[cell.Column.DisplayIndex];
        }

        private void ClickGoTo(object sender, RoutedEventArgs e)
        {
            object context = (sender as Button).DataContext;
            if(context is MemoryUnion mu)
                context = mu.UnionObject;
            MainWindow.Instance.GoToItem(context, Module);
        }

        
        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}