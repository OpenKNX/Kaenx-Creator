using Kaenx.Creator.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace Kaenx.Creator.Converter
{
    public class StateToBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            PublishState state = (PublishState)value;
            switch(state) {
                case PublishState.Fail:
                    return new SolidColorBrush(Colors.Red);

                case PublishState.Success:
                    return new SolidColorBrush(Colors.Green);

                case PublishState.Warning:
                    return new SolidColorBrush(Colors.Orange);
            }

            return new SolidColorBrush(Colors.Black);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
