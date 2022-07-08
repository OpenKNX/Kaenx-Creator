using Kaenx.DataContext.Import.Dynamic;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;

namespace Kaenx.Creator.Converter
{
    public class SeparatorHintToObject : IValueConverter
    {
        public object NoneValue { get; set; }
        public object HeadlineValue { get; set; }
        public object HorizontalRulerValue { get; set; }
        public object InformationValue { get; set; }
        public object ErrorValue { get; set; }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch ((ParamSeparatorHint)value)
            {
                case ParamSeparatorHint.None:
                    return NoneValue;

                case ParamSeparatorHint.Headline:
                    return HeadlineValue;

                case ParamSeparatorHint.HorizontalRuler:
                    return HorizontalRulerValue;

                case ParamSeparatorHint.Information:
                    return InformationValue;

                case ParamSeparatorHint.Error:
                    return ErrorValue;
            }

            throw new NotImplementedException();
        }


        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int output;
            int.TryParse(value.ToString(), out output);
            return output;
        }
    }
}