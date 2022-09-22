using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Kaenx.Creator.Converter
{
    public class EmptyToVisibility : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val;
            if(value == null)
                val = false;
            else
                val = !string.IsNullOrEmpty(value.ToString());
                
            val = (parameter?.ToString().ToLower() == "true") ? !val : val;
            return val ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
