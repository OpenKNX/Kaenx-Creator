using Kaenx.DataContext.Import.Dynamic;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// Die Elementvorlage "Benutzersteuerelement" wird unter https://go.microsoft.com/fwlink/?LinkId=234236 dokumentiert.

namespace Kaenx.Creator.Viewer.Controls
{
    public sealed partial class Table : UserControl
    {
        public Table()
        {
            this.InitializeComponent();
            CheckDataContext();
        }

        private async void CheckDataContext()
        {
            while (this.DataContext == null)
                await Task.Delay(10);

            ParameterTable table = (ParameterTable)DataContext;

            int percentage = 100;
            foreach (int col in table.Columns)
            {
                percentage -= col;
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(col, GridUnitType.Star) });
            }
            if(percentage > 0)
            {
                MainGrid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(percentage, GridUnitType.Star) });
            }


            foreach (int row in table.Rows)
            {
                MainGrid.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(0, GridUnitType.Auto) });
            }

            int counter = 0;
            foreach (IDynParameter para in table.Parameters)
            {
                ContentControl ctrl = new ContentControl()
                {
                    VerticalAlignment = VerticalAlignment.Stretch,
                    VerticalContentAlignment = VerticalAlignment.Stretch,
                    HorizontalAlignment = HorizontalAlignment.Stretch,
                    HorizontalContentAlignment = HorizontalAlignment.Stretch
                };

                /*switch (para)
                {
                    case ParamNumber pn:
                        ctrl.ContentTemplate = (DataTemplate)this.Resources["TypeNumber"];
                        break;

                    case ParamEnum pe:
                        ctrl.ContentTemplate = (DataTemplate)this.Resources["TypeEnums"];
                        break;

                    case ParamSeparator ps:
                        ctrl.ContentTemplate = (DataTemplate)this.Resources["TypeSeparator"];
                        break;

                    default:
                        throw new Exception("Not implemented Type: " + para.GetType().ToString());
                }*/
                ctrl.ContentTemplateSelector = (Viewer.Selectors.ParameterTypeSelector)this.Resources["ParaTypeSelector"];

                MainGrid.Children.Add((UIElement)ctrl);
                //TablePosition pos = table.Positions[counter++];
                Grid.SetColumn(ctrl, counter++);
                //Grid.SetRow(ctrl, pos.Row - 1);
                ctrl.Content = para;
            }
        }
    }
}