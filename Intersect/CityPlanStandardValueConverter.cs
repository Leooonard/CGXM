using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Intersect
{
    public class CityPlanStandardValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int cpsID = (int)value;
            CityPlanStandard cityPlanStandard = new CityPlanStandard();
            cityPlanStandard.id = cpsID;
            cityPlanStandard.select();
            return String.Format("{0}-{1}", cityPlanStandard.number, cityPlanStandard.shortDescription);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
