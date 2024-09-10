using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Kaenx.Creator.Converter
{
    public class IntToHex : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string length = parameter != null ? parameter.ToString() : "2";
            return ((int)value).ToString("X" + length);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int number;
            if(!int.TryParse(value.ToString(), NumberStyles.HexNumber, null, out number))
                return 0;
            return number;
        }
    }
}
