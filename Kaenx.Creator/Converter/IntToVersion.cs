using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Linq;

namespace Kaenx.Creator.Converter
{
    public class IntToVersion : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string hexString = ((int)value).ToString("X");
            if(hexString.All(char.IsDigit)){
                if(hexString.Length > 1)
                    hexString = hexString.Insert(hexString.Length - 1, ".");
            }else{
                hexString = "";
            }
            return hexString;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string hexString="";
            if(value.ToString().Contains('.'))
            {
                string[] versions = value.ToString().Split('.');
                hexString = versions[0] + versions[1];
            }else
            {
                hexString = value.ToString();
            }

            int number;
            if(!int.TryParse(hexString, NumberStyles.HexNumber, null, out number))
                return 0;
            return number;
        }
    }
}
