using Kaenx.Creator.Models.Dynamic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace Kaenx.Creator.Converter
{
    public class DynHasIndependent : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null) return false;
            bool neg = (parameter?.ToString() == "true");
            ObservableCollection<IDynChannel> main = value as ObservableCollection<IDynChannel>;
            if (main.Count == 0) return neg ? true : false;

            bool val = main[0] is DynChannelIndependet;
            return neg ? !val : val;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
