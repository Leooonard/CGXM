using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace Intersect
{
    class IntValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            Regex regex = new Regex(@"^\d+$");
            if(regex.IsMatch(value.ToString()))
            {
                return ValidationResult.ValidResult;
            }
            else
            {
                return new ValidationResult(false, "value is not a int");
            }
        }
    }
}
