using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Text.RegularExpressions;

namespace Intersect
{
    public class PositiveDoubleValidationRule : ValidationRule
    {
        private string validationMessage = "请输入正数";

        public PositiveDoubleValidationRule() { }

        public PositiveDoubleValidationRule(string msg = null)
        {
            if(msg != null)
                validationMessage = msg;
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            try
            {
                if (Double.Parse(value.ToString()) > 0)
                {
                    return ValidationResult.ValidResult;
                }
                else
                {
                    return new ValidationResult(false, validationMessage);
                }
            }
            catch (Exception)
            {
                return new ValidationResult(false, validationMessage);            
            }
        }
    }
}
