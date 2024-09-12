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
            decimal majorVersion = Math.Floor((decimal)((int)value/16));
            int minorVersion = (int)value % 16;
            return $"{majorVersion}.{minorVersion}";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int majorVersion;
            int minorVersion;

            if(value.ToString().Contains('.'))
            {
                string[] versions = value.ToString().Split('.');
                
                if(!int.TryParse(versions[0], out majorVersion))
                    majorVersion = 0;

                if(!int.TryParse(versions[1], out minorVersion))
                    minorVersion = 0;   
                    
                if(minorVersion > 15)
                {
                    minorVersion /= 10;
                    if(minorVersion > 15)
                        minorVersion = 15;
                }             
            }else
            {
                if(!int.TryParse(value.ToString(), out majorVersion))
                    majorVersion = 0;

                minorVersion = 0;
            }

            majorVersion *= 16;

            return majorVersion + minorVersion;
        }
    }
}
