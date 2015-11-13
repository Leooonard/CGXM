using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Intersect
{
    public class Const
    {
        public const int CONFIG_TYPE_RESTRAINT = 0;
        public const int CONFIG_TYPE_STANDARD = 1;
        public const int CONFIG_CATEGORY_STANDARD_DISTANCE_POSITIVE = 0;
        public const int CONFIG_CATEGORY_STANDARD_DISTANCE_NEGATIVE = 1;
        public const int CONFIG_CATEGORY_STANDARD_OVERLAP_POSITIVE = 2;
        public const int CONFIG_CATEGORY_STANDARD_OVERLAP_NEGATIVE = 3;

        public const int CONFIG_CATEGORY_RESTRAINT_INTERSECT_BIGGER = 0;
        public const int CONFIG_CATEGORY_RESTRAINT_INTERSECT_SMALLER = 1;
        public const int CONFIG_CATEGORY_RESTRAINT_INTERSECT_BIGGEREQUAL = 2;
        public const int CONFIG_CATEGORY_RESTRAINT_INTERSECT_SMALLEREQUAL = 3;
        public const int CONFIG_CATEGORY_RESTRAINT_DISTANCE_BIGGER = 4;
        public const int CONFIG_CATEGORY_RESTRAINT_DISTANCE_SMALLER = 5;
        public const int CONFIG_CATEGORY_RESTRAINT_DISTANCE_BIGGEREQUAL = 6;
        public const int CONFIG_CATEGORY_RESTRAINT_DISTANCE_SMALLEREQUAL = 7;
        public const int CONFIG_CATEGORY_RESTRAINT_OVERLAP_BIGGER = 8;
        public const int CONFIG_CATEGORY_RESTRAINT_OVERLAP_SMALLER = 9;
        public const int CONFIG_CATEGORY_RESTRAINT_OVERLAP_BIGGEREQUAL = 10;
        public const int CONFIG_CATEGORY_RESTRAINT_OVERLAP_SMALLEREQUAL = 11;

        public const int LABEL_TYPE_RESTRAINT = 0;
        public const int LABEL_TYPE_STANDARD = 1;
        public const int LABEL_TYPE_BOTH = 2;

        public const int ERROR_INT = -1;
        public const double ERROR_DOUBLE = -1;
        public const string ERROR_STRING = "";
        public const bool ERROR_BOOL = false;

        public const string INNER_ERROR_TIP = "内部错误";

        public const int DEFAULT_NUMBER_VALUE = 0;

        public const string PROGRAM_FOLDER = @"C://CGXM";

        public const string WORKSPACE_PATH = @"C:\CGXM\";

        public const string DEFAULT_FILE_DIALOG_FOLDER = @"C:\Users\Mavericks\Desktop\适宜性分析数据\landuseLayers";

        public const string SOURCE_FOLDER_NAME = @"source";
        public const string PROGRAMS_FOLDER_NAME = @"programs";
    }
}
