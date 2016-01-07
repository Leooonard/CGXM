using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Intersect.Lib;
using System.Text.RegularExpressions;
using System.IO;

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
        public const int CONFIG_CATEGORY_RESTRAINT_SLOPE_BIGGER = 8;
        public const int CONFIG_CATEGORY_RESTRAINT_SLOPE_SMALLER = 9;
        public const int CONFIG_CATEGORY_RESTRAINT_SLOPE_BIGGEREQUAL = 10;
        public const int CONFIG_CATEGORY_RESTRAINT_SLOPE_SMALLEREQUAL = 11;
        public const int CONFIG_CATEGORY_RESTRAINT_HEIGHT_BIGGER = 12;
        public const int CONFIG_CATEGORY_RESTRAINT_HEIGHT_SMALLER = 13;
        public const int CONFIG_CATEGORY_RESTRAINT_HEIGHT_BIGGEREQUAL = 14;
        public const int CONFIG_CATEGORY_RESTRAINT_HEIGHT_SMALLEREQUAL = 15;


        public const int LABEL_TYPE_RESTRAINT = 0;
        public const int LABEL_TYPE_STANDARD = 1;
        public const int LABEL_TYPE_BOTH = 2;

        public const int ERROR_INT = -1;
        public const double ERROR_DOUBLE = -1;
        public const string ERROR_STRING = "";
        public const bool ERROR_BOOL = false;

        public const string INNER_ERROR_TIP = "内部错误";

        public const int DEFAULT_NUMBER_VALUE = 0;

        public const string DEFAULT_FILE_DIALOG_FOLDER = @"C:\Users\Mavericks\Desktop\适宜性分析数据\landuseLayers";

        public const string SOURCE_FOLDER_NAME = @"source";
        public const string PROGRAMS_FOLDER_NAME = @"programs";

        public const string BASE_LAYER_NAME = @"行政村界";
        public const string BASE_FIELD_NAME = @"Name";

        public static Dictionary<string, string> CONFIG;

        private LineFileReader reader;
        private const string CONFIG_FILE_PATH = @"../../TSLO.config";

        public Const()
        {
            CONFIG = new Dictionary<string,string>();
            try
            {
                reader = new LineFileReader(CONFIG_FILE_PATH);
            }
            catch (FileNotFoundException exp)
            {
                throw new Exception("配置文件丢失");
            }
            List<string> configs = reader.readByLine();
            foreach (string config in configs)
            {
                parseConfig(config);
            }
        }

        private void parseConfig(string config)
        {
            Regex parser = new Regex(@"^(\S+)\s+(.+)$");
            Match match = parser.Match(config);
            string configName = match.Groups[1].Value;
            string configValue = match.Groups[2].Value;
            if (checkConfigField(configName) == false || checkConfigField(configValue) == false)
            {
                throw new Exception("配置文件错误");
            }
            CONFIG.Add(configName, configValue);
        }

        private bool checkConfigField(string field)
        {
            if (field == "")
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
