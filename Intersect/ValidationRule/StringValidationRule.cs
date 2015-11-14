using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;

namespace Intersect
{
    public class StringValidationRule : ValidationRule
    {
        private int stringMaxLength = -1;
        public int maxLength
        {
            get
            {
                return stringMaxLength;
            }
            set
            {
                stringMaxLength = value;
            }
        }
        private int stringMinLength = 0;
        public int minLength
        {
            get
            {
                return stringMinLength;
            }
            set
            {
                stringMinLength = value;
            }
        }
        private string validationMessage = "内容不能为空";
        private string limitedValidationMessage = "内容长度须在0-{0}之间";

        public StringValidationRule() { }

        public StringValidationRule(int maxLength = -1)
        {
            stringMaxLength = maxLength;
        }

        public override ValidationResult Validate(object value, System.Globalization.CultureInfo cultureInfo)
        {
            if (stringMaxLength == -1)
            {
                if (value.ToString().Length == stringMinLength)
                {
                    return new ValidationResult(false, validationMessage);
                }
                else
                {
                    return ValidationResult.ValidResult;
                }
            }
            else
            {
                if (value.ToString().Length == stringMinLength || value.ToString().Length > stringMaxLength)
                {
                    return new ValidationResult(false, String.Format(limitedValidationMessage, stringMaxLength));
                }
                else
                {
                    return ValidationResult.ValidResult;
                }
            }
        }
    }
}
