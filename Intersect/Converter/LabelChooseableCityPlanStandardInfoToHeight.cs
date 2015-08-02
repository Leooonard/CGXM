using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace Intersect
{
    public class LabelChooseableCityPlanStandardInfoToHeight : IValueConverter
    {
        private int infoHeight = 15;

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            string info = (string)value;
            if (info.StartsWith(UncompleteLabelComboBoxManager.HIDDEN_TITLE))
                return 0;
            else
                return infoHeight;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
