using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Kaenx.Creator.Converter
{
    public class EnumToVisibility : IValueConverter
    {
        public bool Negate { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null) return Visibility.Collapsed;

            string name = value.ToString();
            List<string> paras = new List<string>();
            paras.AddRange(parameter.ToString().Split("-"));

            Visibility vis = paras.Contains(name) ? Visibility.Visible : Visibility.Collapsed;

            if (Negate)
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
