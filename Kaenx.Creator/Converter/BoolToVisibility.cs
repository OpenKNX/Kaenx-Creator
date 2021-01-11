using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Kaenx.Creator.Converter
{
    public class BoolToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Visibility vis = (value as bool?) == true ? Visibility.Visible : Visibility.Collapsed;
            if (parameter?.ToString() == "true")
            {
                if (vis == Visibility.Visible)
                    vis = Visibility.Collapsed;
                else
                    vis = Visibility.Visible;
            }
            return vis;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
