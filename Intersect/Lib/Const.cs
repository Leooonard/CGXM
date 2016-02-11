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
        public static Dictionary<string, List<HouseDecoration>> DECORATION;

        private LineFileReader reader;
        private const string CONFIG_FILE_PATH = @"./TSLO.config";

        private const string DECORATION_FILE_PATH = @"./DECO.config";

        public Const()
        {
            CONFIG = new Dictionary<string,string>();
            try
            {
                reader = new LineFileReader(CONFIG_FILE_PATH);
            }
            catch (Exception exp)
            {
                Tool.M("配置文件丢失");
                throw new Exception("配置文件丢失");
            }
            List<string> configs = reader.readByLine();
            foreach (string config in configs)
            {
                parseConfig(config);
            }

            DECORATION = new Dictionary<string, List<HouseDecoration>>();
            try
            {
                reader = new LineFileReader(DECORATION_FILE_PATH);
            }
            catch (Exception exp)
            {
                Tool.M("房屋装饰文件丢失");
                throw new Exception("房屋装饰文件丢失");
            }
            List<string> decorations = reader.readByLine();
            parseDecoration(decorations, DECORATION);
            return;
        }

        private void parseDecoration(List<string> decorations, Dictionary<string, List<HouseDecoration>> decorationDict)
        {
            List<List<string>> decorationTypeList = new List<List<string>>();

            for (int i = 0; i < decorations.Count; i++)
            { 
                string line = decorations[i];
                if (isDecorationType(line))
                {
                    List<string> decorationType = new List<string>();
                    decorationType.Add(getDecorationType(line));
                    if (i == decorations.Count - 1)
                    {
                        decorationTypeList.Add(decorationType);
                        break;
                    }
                    string nextLine = decorations[++i];
                    while (!isDecorationType(nextLine))
                    {
                        decorationType.Add(nextLine);
                        if (i == decorations.Count - 1)
                        {
                            break;
                        }
                        nextLine = decorations[++i];
                    }
                    if (i != decorations.Count - 1)
                    {
                        i--;                    
                    }
                    decorationTypeList.Add(decorationType);
                }
            }

            foreach (List<string> decorationType in decorationTypeList)
            {
                string typeName = decorationType[0];
                List<string> typeElementList = new List<string>();
                for (int i = 1; i < decorationType.Count; i++)
                {
                    typeElementList.Add(decorationType[i]);
                }
                List<HouseDecoration> houseDecorationList = parseDecorationType(typeElementList);
                DECORATION.Add(typeName, houseDecorationList);
            }
        }

        private string getDecorationType(string line)
        {
            Regex parser = new Regex(@"^\[(.+?)\]$");
            Match match = parser.Match(line);
            string type = match.Groups[1].Value;
            return type;
        }

        private bool isDecorationType(string line)
        {
            Regex parser = new Regex(@"^\[.+?\]$");
            return parser.IsMatch(line);
        }

        private List<HouseDecoration> parseDecorationType(List<string> typeList)
        {
            List<HouseDecoration> houseDecorationList = new List<HouseDecoration>();
            foreach (string type in typeList)
            {
                HouseDecoration houseDecoration = parseDecorationElement(type);
                houseDecorationList.Add(houseDecoration);
            }
            return houseDecorationList;
        }

        private HouseDecoration parseDecorationElement(string element)
        {
            HouseDecoration houseDecoration;
            Regex parser = new Regex(@"^((?:[^;]|(?<=\\);)+);((?:[^;]|(?<=\\);)+);((?:[^;]|(?<=\\);)+)$");
            Match match = parser.Match(element);
            string name = match.Groups[1].Value;
            string value = match.Groups[2].Value;
            string path = match.Groups[3].Value;
            if (checkConfigField(name) == false || checkConfigField(value) == false || checkConfigField(path) == false)
            {
                Tool.M("装饰文件错误");
                throw new Exception("装饰文件错误");
            }

            houseDecoration = new HouseDecoration(name, value, path);
            return houseDecoration;
        }

        private void parseConfig(string config)
        {
            Regex parser = new Regex(@"^(\S+)\s+(.+)$");
            Match match = parser.Match(config);
            string configName = match.Groups[1].Value;
            string configValue = match.Groups[2].Value;
            if (checkConfigField(configName) == false || checkConfigField(configValue) == false)
            {
                Tool.M("配置文件错误");
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
