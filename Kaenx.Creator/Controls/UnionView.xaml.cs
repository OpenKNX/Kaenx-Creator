using Kaenx.Creator.Classes;
using Kaenx.Creator.Converter;
using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;

namespace Kaenx.Creator.Controls
{
    public partial class UnionView : UserControl, INotifyPropertyChanged, ISelectable
    {
        public static readonly DependencyProperty VersionProperty = DependencyProperty.Register("Version", typeof(AppVersion), typeof(UnionView), new PropertyMetadata(null));
        public static readonly DependencyProperty ModuleProperty = DependencyProperty.Register("Module", typeof(IVersionBase), typeof(UnionView), new PropertyMetadata(null));
        public AppVersion Version {
            get { return (AppVersion)GetValue(VersionProperty); }
            set { SetValue(VersionProperty, value); }
        }
        public IVersionBase Module {
            get { return (IVersionBase)GetValue(ModuleProperty); }
            set { SetValue(ModuleProperty, value); }
        }

        public ObservableCollection<UnionMemory> Memories { get; set; } = new();

        public UnionView()
		{
            InitializeComponent();
        }

        public void ShowItem(object item)
        {
            UnionList.ScrollIntoView(item);
            UnionList.SelectedItem = item;
        }

        private void ClickAddUnion(object sender, RoutedEventArgs e)
        {
            Module.Unions.Add(new Models.Union() { UId = Kaenx.Creator.Classes.Helper.GetNextFreeUId(Module.Unions)});
        }
        
        private void ClickRemoveUnion(object sender, RoutedEventArgs e)
        {
            Module.Unions.Remove(UnionList.SelectedItem as Models.Union);
        }

        private void ClickCalcHeatmap(object sender, RoutedEventArgs e)
        {
            Union union = (Union)UnionList.SelectedItem;
            MemoryGrid.Columns.Clear();
            Memories.Clear();

            BoolToBrush converter = new BoolToBrush()
            {
                TrueValue = new SolidColorBrush(Colors.Orange),
                FalseValue = new SolidColorBrush(Colors.Transparent)
            };

            for(int i = 0; i < union.SizeInBit; i++)
            {
                DataGridTemplateColumn col = new(); 
                col.Header = $"{Math.Floor(i/8.0)}/{i%8}";
                Style colStyle = new Style();
                colStyle.TargetType = typeof(DataGridCell);
                colStyle.Setters.Add(new Setter(DataGridCell.BackgroundProperty, new Binding($"Memory[{i}]") { Converter = converter }));
                col.CellStyle = colStyle;
                MemoryGrid.Columns.Add(col);
            }

            foreach(Parameter para in Module.Parameters.Where(p => p.IsInUnion && p.UnionId == union.UId))
            {
                Memories.Add(new(para.Name, union.SizeInBit, para.Offset, para.OffsetBit, para.ParameterTypeObject.SizeInBit));
            }
        }

        private void CurrentCellChanged(object sender, EventArgs e)
        {
            DataGridCellInfo cell = (sender as DataGrid).CurrentCell;
            Models.MemorySection sec = cell.Item as Models.MemorySection;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void Changed(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
