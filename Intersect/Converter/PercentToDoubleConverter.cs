using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Intersect
{
    public class PercentToDoubleConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return value;  
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return (double)value / 100;
        }
    }
}
