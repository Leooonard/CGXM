using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Intersect
{
    public class C
    {
        public const int CONFIG_TYPE_RESTRAINT = 0;
        public const int CONFIG_TYPE_STANDARD = 1;
        public const int CONFIG_CATEGORY_INTERSECT_POSITIVE = 0;
        public const int CONFIG_CATEGORY_INTERSECT_NEGATIVE = 1;
        public const int CONFIG_CATEGORY_DISTANCE_POSITIVE = 2;
        public const int CONFIG_CATEGORY_DISTANCE_NEGATIVE = 3;
        public const int CONFIG_CATEGORY_OVERLAP_POSITIVE = 4;
        public const int CONFIG_CATEGORY_OVERLAP_NEGATIVE = 5;

        public const int ERROR_INT = -1;
        public const double ERROR_DOUBLE = -1;
        public const string ERROR_STRING = "";
        public const bool ERROR_BOOL = false;

        public const string INNER_ERROR_TIP = "内部错误";

        public const int DEFAULT_NUMBER_VALUE = 0;

        public const string PROGRAM_FOLDER = @"C://CGXM";
    }
}
