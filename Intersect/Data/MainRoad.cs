using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using ESRI.ArcGIS.Carto;

namespace Intersect
{
    public class MainRoad : DataBase
    {
        private const int MRNAME_MAX_LENGTH = 50;
        private const string MRNAME_DEFAULT_NAME = "主路";

        private int mrID;
        public int id
        {
            get
            {
                return mrID;
            }
            set
            {
                mrID = value;
                onPropertyChanged("id");
            }
        }
        private int prID;
        public int programID
        {
            get
            {
                return prID;
            }
            set
            {
                prID = value;
                onPropertyChanged("programID");
            }
        }
        private string mrName;
        public string name
        {
            get
            {
                return mrName;
            }
            set
            {
                mrName = value;
                onPropertyChanged("name");
            }
        }
        private string mrPath;
        public string path
        {
            get
            {
                return mrPath;
            }
            set
            {
                mrPath = value;
                onPropertyChanged("path");
            }
        }
        public ILineElement lineElement;

        public MainRoad()
        {
            mrID = Const.ERROR_INT;
            prID = Const.ERROR_INT;
            mrName = MRNAME_DEFAULT_NAME;
            mrPath = Const.ERROR_STRING;
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("id") && mrID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("programID") && prID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("name") && (mrName.Length == 0 || mrName.Length > MRNAME_MAX_LENGTH))
                return String.Format("主路名长度须在0-{0}之间.", MRNAME_MAX_LENGTH);
            if (!shieldVariableList.Contains("path") && mrPath.Length == 0)
                return "主路路径不能为空";
            return "";
        }

        protected override bool isValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("mrID") && mrID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("prID") && prID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("mrName") && (mrName.Length == 0 || mrName.Length > MRNAME_MAX_LENGTH))
                return false;
            if (!shieldVariableList.Contains("mrPath") && mrPath.Length == 0)
                return false;
            return true;
        }

        public void initBySqlDataReader(SqlDataReader reader)
        {
            reader.Read();
            mrID = Int32.Parse(reader[0].ToString());
            prID = Int32.Parse(reader[1].ToString());
            mrName = reader[2].ToString();
            mrPath = reader[3].ToString();
            List<Point> pointList = MainRoad.ConvertStringToPointList(mrPath);
            lineElement = GisTool.getILineElementFromPointList(pointList);
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "mrID" }))
                return false;
            string sqlCommand = String.Format(@"insert into MainRoad (prID,mrName,mrPath) values ({0}, '{1}', '{2}')"
                , prID, mrName, mrPath);
            Sql sql = new Sql();
            return sql.insertMainRoad(sqlCommand);
        }

        public bool saveWithoutCheck()
        {
            string sqlCommand = String.Format(@"insert into MainRoad (prID,mrName,mrPath) values ({0}, '{1}', '{2}')"
                , prID, mrName, mrPath);
            Sql sql = new Sql();
            return sql.insertMainRoad(sqlCommand);
        }

        public void updatePath()
        {
            List<Point> pointList = GisTool.getPointListFromILineElement(lineElement);
            mrPath = MainRoad.ConvertPointListToString(pointList);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format(@"update MainRoad set prID={0},mrName='{1}',mrPath='{2}' where mrID={3}"
                , prID, mrName, mrPath, mrID);
            Sql sql = new Sql();
            return sql.updateMainRoad(sqlCommand);
        }

        public bool saveOrUpdate()
        {
            if (id == Const.ERROR_INT)
            {
                return save();
            }
            else
            {
                return update();
            }
        }

        public override bool delete()
        {
            if (!isValid(new List<string>() { "prID", "mrName", "mrPath" }))
                return false;
            string sqlCommand = String.Format(@"delete from MainRoad where mrID={0}", mrID);
            Sql sql = new Sql();
            return sql.deleteMainRoad(sqlCommand);
        }

        public override bool select()
        {
            if (!isValid(new List<string>() { "prID", "mrName", "mrPath" }))
                return false;
            string sqlCommand = String.Format(@"select * from MainRoad where mrID={0}", mrID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectMainRoad(sqlCommand);
            initBySqlDataReader(reader);
            sql.closeConnection();
            return true;
        }

        public static string ConvertPointListToString(List<Point> pointList)
        {
            string pointString = "";
            foreach (Point point in pointList)
            {
                pointString += String.Format(@"{0},{1} ", point.x, point.y);
            }
            pointString = pointString.Substring(0, pointString.Length - 1);
            return pointString;
        }

        public static List<Point> ConvertStringToPointList(string pointString)
        {
            List<Point> pointList = new List<Point>();
            List<string> singlePointStringList = new List<string>(pointString.Split(' '));
            foreach (string singlePointString in singlePointStringList)
            {
                string[] singlePointArray = singlePointString.Split(',');
                Point point = new Point();
                point.x = Double.Parse(singlePointArray[0]);
                point.y = Double.Parse(singlePointArray[1]);
                pointList.Add(point);
            }
            return pointList;
        }

        public static int GetLastMainRoadID()
        {
            string sqlCommand = String.Format("select max(mrID) from MainRoad");
            Sql sql = new Sql();
            return sql.selectMaxMainRoadID(sqlCommand);
        }

    }
}
