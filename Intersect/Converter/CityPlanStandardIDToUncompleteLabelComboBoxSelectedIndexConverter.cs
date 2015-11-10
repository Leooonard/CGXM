using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Collections.ObjectModel;

namespace Intersect
{
    public class CityPlanStandardIDToUncompleteLabelComboBoxSelectedIndexConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            int cpsID = (int)value;
            if (cpsID == Const.ERROR_INT)
                return Const.ERROR_INT;

            ObservableCollection<CityPlanStandard> tempList = CityPlanStandard.GetAllCityPlanStandard();
            for (int i = 0; i < tempList.Count; i++)
            {
                if (tempList[i].id == cpsID)
                    return i;
            }

            return Const.ERROR_INT;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
