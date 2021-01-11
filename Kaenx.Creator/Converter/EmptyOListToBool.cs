using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Kaenx.Creator.Converter
{
    public class EmptyOListToBool : IValueConverter
    {
        public bool Negate { get; set; } = false;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool val = (value as int?) == 0;
            return parameter?.ToString().ToLower() == "true" ? !val : val;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
