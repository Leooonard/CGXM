using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Intersect
{
    class NotNullValidationRule : ValidationRule
    {
        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (value == null || string.IsNullOrWhiteSpace(value as string))
            {
                return new ValidationResult(false, "value cannot be null");
            }
            return ValidationResult.ValidResult;
        }
    }
}
