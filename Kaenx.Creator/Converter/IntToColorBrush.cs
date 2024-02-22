using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using System.Windows.Media;

namespace Kaenx.Creator.Converter
{
    public class IntToColorBrush : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value){
                case 0:
                    return new SolidColorBrush(Colors.Gray);
                case 1:
                    return new SolidColorBrush(Colors.Violet);
                case 2: 
                    return new SolidColorBrush(Colors.Brown);
                case 3:
                    return new SolidColorBrush(Colors.Chocolate);
                case 4:
                    return new SolidColorBrush(Colors.Pink);
                case 5:
                    return new SolidColorBrush(Colors.Purple);
                case 6:
                    return new SolidColorBrush(Colors.Red);
                case 7:
                    return new SolidColorBrush(Colors.Green);
                case 8:
                    return new SolidColorBrush(Colors.Orange);
                case 9:
                    return new SolidColorBrush(Colors.Blue);
                case 10:
                    return new SolidColorBrush(Colors.MediumPurple);
            }
            return new SolidColorBrush(Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}