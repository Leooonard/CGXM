using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using System.Collections.ObjectModel;

namespace Intersect
{
    public class Village : DataBase
    {
        public const int VILLAGE_MAX_SIZE = 40000;
        private int VNAME_MAX_LENGTH = 50;
        private const string VNAME_DEFAULT_VALUE = "小区";

        private int vID;
        public int id
        {
            get
            {
                return vID;
            }
            set
            {
                vID = value;
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
        private string vName;
        public string name
        {
            get
            {
                return vName;
            }
            set
            {
                vName = value;
                onPropertyChanged("name");
            }
        }
        private string vBoundary;
        public string boundary
        {
            get
            {
                return vBoundary;
            }
            set
            {
                vBoundary = value;
                onPropertyChanged("boundary");
            }
        }
        private bool vInUse;
        public bool inUse
        {
            get
            {
                return vInUse;
            }
            set
            {
                vInUse = value;
                onPropertyChanged("inUse");
            }
        }
        public IPolygonElement polygonElement;
        public string polygonElementColorString
        {
            get;
            set;
        }
        public string boundaryArea
        {
            get;
            set;
        }
        public InnerRoad innerRoad
        {
            get;
            set;
        }
        public CommonHouse commonHouse
        {
            get;
            set;
        }
        public ObservableCollection<House> houseList
        {
            get;
            set;
        }

        public Village()
        {
            vID = Const.ERROR_INT;
            prID = Const.ERROR_INT;
            vName = VNAME_DEFAULT_VALUE;
            vBoundary = Const.ERROR_STRING;
            vInUse = Const.ERROR_BOOL;
            polygonElementColorString = Const.ERROR_STRING;
            boundaryArea = Const.ERROR_STRING;
        }
        
        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("id") && vID == Const.ERROR_INT)
                return "区域ID为空";
            if (!shieldVariableList.Contains("programID") && prID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("name") && (vName.Length == 0 || vName.Length > VNAME_MAX_LENGTH))
                return String.Format("区域名长度须在0-{0}之间", VNAME_MAX_LENGTH);
            if (!shieldVariableList.Contains("boundary") && vBoundary.Length == 0)
                return "区域路径不能为空";
            return "";
        }

        protected override bool isValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("vID") && vID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("prID") && prID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("vName") && (vName.Length == 0 || vName.Length > VNAME_MAX_LENGTH))
                return false;
            if (!shieldVariableList.Contains("vBoundary") && vBoundary.Length == 0)
                return false;
            return true;
        }

        public void initBySqlDataReader(SqlDataReader reader)
        {
            reader.Read();
            vID = Int32.Parse(reader[0].ToString());
            prID = Int32.Parse(reader[1].ToString());
            vName = reader[2].ToString();
            vBoundary = reader[3].ToString();
            vInUse = Boolean.Parse(reader[4].ToString());
            List<Point> pointList = Village.ConvertStringToPointList(vBoundary);
            polygonElement = GisTool.getIPolygonElementFromPointList(pointList);
            boundaryArea = ((polygonElement as IElement).Geometry as IArea).Area.ToString("F2");
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "vID" }))
                return false;
            string sqlCommand = String.Format(@"Insert into Village (prID,vName,vBoundary,vInUse) values({0},'{1}','{2}',{3})"
                , prID, vName, vBoundary, vInUse ? 1 : 0);
            Sql sql = new Sql();
            return sql.insertVillage(sqlCommand);
        }

        public bool saveWithoutCheck()
        {
            string sqlCommand = String.Format(@"Insert into Village (prID,vName,vBoundary,vInUse) values({0},'{1}','{2}',{3})"
                , prID, vName, vBoundary, vInUse ? 1 : 0);
            Sql sql = new Sql();
            return sql.insertVillage(sqlCommand);
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

        public void updateBoundary()
        {
            List<Point> pointList = GisTool.getPointListFromIPolygonElement(polygonElement);
            vBoundary = Village.ConvertPointListToString(pointList);
            boundaryArea = ((polygonElement as IElement).Geometry as IArea).Area.ToString("F2");
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format(@"update Village set prID={0},vName='{1}',vBoundary='{2}',vInUse={3} where vID={4}"
                , prID, vName, vBoundary, vInUse ? 1 : 0, vID);
            Sql sql = new Sql();
            return sql.updateVillage(sqlCommand);
        }

        public override bool delete()
        {
            if (!isValid(new List<string>() { "prID", "vName", "vBoundary", "vInUse" }))
                return false;
            string sqlCommand = String.Format(@"delete from Village where vID={0}", vID);
            Sql sql = new Sql();
            return sql.deleteVillage(sqlCommand);
        }

        public override bool select()
        {
            if (!isValid(new List<string>() { "prID", "vName", "vBoundary", "vInUse" }))
                return false;
            string sqlCommand = String.Format("select * from Village where vID={0}", vID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectVillage(sqlCommand);
            initBySqlDataReader(reader);
            sql.closeConnection();
            return true;
        }

        public bool compare(Village village)
        {
            if (id != village.id)
                return false;
            if (prID != village.programID)
                return false;
            if (vName != village.name)
                return false;
            if (vBoundary != village.boundary)
                return false;
            if (vInUse != village.inUse)
                return false;
            return true;
        }

        public InnerRoad getRelatedInnerRoad()
        {
            if (!isValid(new List<string>() { "prID", "vName", "vBoundary", "vInUse" }))
                return null;
            string sqlCommand = String.Format("select irID from InnerRoad where vID={0}", vID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectInnerRoadIDByVillageID(sqlCommand);
            if (reader.HasRows)
            {
                reader.Read();
                int innerRoadID = Int32.Parse(reader[0].ToString());
                InnerRoad innerRoad = new InnerRoad();
                innerRoad.id = innerRoadID;
                innerRoad.select();
                return innerRoad;
            }
            else
            {
                return null;
            }
        }

        public CommonHouse getRelatedCommonHouse()
        {
            if (!isValid(new List<string>() { "prID", "vName", "vBoundary", "vInUse" }))
                return null;
            string sqlCommand = String.Format("select chID from CommonHouse where vID={0}", vID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectCommonHouseIDByVID(sqlCommand);
            if (reader.HasRows)
            {
                reader.Read();
                int commonHouseID = Int32.Parse(reader[0].ToString());
                CommonHouse commonHouse = new CommonHouse();
                commonHouse.id = commonHouseID;
                commonHouse.select();
                return commonHouse;
            }
            else
            {
                return null;
            }
        }

        public ObservableCollection<House> getAllRelatedHouse()
        {
            if (!isValid(new List<string>() { "prID", "vName", "vBoundary", "vInUse" }))
                return null;
            ObservableCollection<House> houseList = new ObservableCollection<House>();
            string sqlCommand = String.Format("select hID from House where vID={0}", vID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectHouseIDByVID(sqlCommand);
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    int hID = Int32.Parse(reader[0].ToString());
                    House house = new House();
                    house.id = hID;
                    house.select();
                    houseList.Add(house);
                }
                sql.closeConnection();
                return houseList;
            }
            else
            {
                sql.closeConnection();
                return null;
            }
        }

        public static int GetLastVillageID()
        {
            string sqlCommand = String.Format("select max(vID) from Village");
            Sql sql = new Sql();
            return sql.selectMaxVillageID(sqlCommand);
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
    }
}
