using Kaenx.DataContext.Import.Dynamic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;

namespace Kaenx.Creator.Converter
{
    public class IsHeadlineToObject : IValueConverter
    {
        public object TrueValue { get; set; }
        public object FalseValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            ParamSeparatorHint hint = (ParamSeparatorHint)value;
            return hint == ParamSeparatorHint.Headline ? TrueValue : FalseValue;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
