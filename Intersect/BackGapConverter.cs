using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Text.RegularExpressions;

namespace Intersect
{
    public class BackGapConverter : IMultiValueConverter
    {
        public BackGapConverter()
        { 
        
        }

        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double result = 1;
            for (int i = 0; i < values.Count(); i++)
            {
                try
                {
                    Regex reg = new Regex(@"(\d+\.\d+)|\d+$");
                    if(!reg.IsMatch(values[i].ToString()))
                        return "错误";
                    result *= Double.Parse(values[i].ToString());
                }
                catch (Exception)
                {
                    return "错误";
                }
            }
            return String.Format("{0:F}", result);
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
