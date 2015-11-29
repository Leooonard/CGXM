using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Intersect
{
    public class CommonHouse : DataBase
    {
        private int chID;
        public int id
        {
            get
            {
                return chID;
            }
            set
            {
                chID = value;
                onPropertyChanged("id");
            }
        }

        private double chHeight;
        public double height
        {
            get
            {
                return chHeight;
            }
            set
            {
                chHeight = value;
                if (chFrontGap != Const.ERROR_DOUBLE && chBackGap != Const.ERROR_DOUBLE && chHeight != Const.ERROR_DOUBLE)
                {
                    chLandHeight = chFrontGap + chBackGap + chHeight;
                }
                onPropertyChanged("height");
            }
        }

        private double chFrontGap;
        public double frontGap
        {
            get
            {
                return chFrontGap;
            }
            set
            {
                chFrontGap = value;
                if (chFrontGap != Const.ERROR_DOUBLE && chBackGap != Const.ERROR_DOUBLE && chHeight != Const.ERROR_DOUBLE)
                {
                    chLandHeight = chFrontGap + chBackGap + chHeight;
                }
                onPropertyChanged("frontGap");
            }
        }

        private int chFloor;
        public int floor
        {
            get
            {
                return chFloor;
            }
            set
            {
                chFloor = value;
                onPropertyChanged("floor");
            }
        }

        private double chFloorHeight;
        public double floorHeight 
        {
            get
            {
                return chFloorHeight;
            }
            set
            {
                chFloorHeight = value;
                onPropertyChanged("floorHeight");
            }
        }

        private double chBackGapRatio;
        public double backGapRatio
        {
            get
            {
                return chBackGapRatio;
            }
            set
            {
                chBackGapRatio = value;
                onPropertyChanged("backGapRatio");
            }
        }

        private double chBackGap;
        public double backGap
        {
            get
            {
                //if (chBackGap != -1)
                //    return chBackGap;
                //if (chBackGapRatio == -1 || chFloorHeight == -1 || chFloor == -1)
                //    return -1;
                //return chBackGapRatio * chFloorHeight * chFloor;
                return chBackGap;
            }
            set
            {
                chBackGap = value;
                if (chFrontGap != Const.ERROR_DOUBLE && chBackGap != Const.ERROR_DOUBLE && chHeight != Const.ERROR_DOUBLE)
                {
                    chLandHeight = chFrontGap + chBackGap + chHeight;
                }
                onPropertyChanged("backGap");
            }
        }

        private int vID;
        public int villageID
        {
            get
            {
                return vID;
            }
            set
            {
                vID = value;
                onPropertyChanged("villageID");
            }
        }

        private double chLandHeight;
        public double landHeight
        {
            get
            {
                return chLandHeight;
            }
            set
            {
                chLandHeight = value;
            }
        }

        private double chRoadWidth;
        public double roadWidth
        {
            get
            {
                return chRoadWidth;
            }
            set
            {
                chRoadWidth = value;
                onPropertyChanged("roadWidth");
            }
        }

        public CommonHouse()
        {
            chID = Const.ERROR_INT;
            chHeight = Const.ERROR_DOUBLE;
            chFrontGap = Const.ERROR_DOUBLE;
            chFloor = Const.ERROR_INT;
            chFloorHeight = Const.ERROR_DOUBLE;
            chBackGapRatio = Const.ERROR_DOUBLE;
            chBackGap = Const.ERROR_DOUBLE;
            vID = Const.ERROR_INT;
            chLandHeight = Const.ERROR_DOUBLE;
            chRoadWidth = Const.ERROR_DOUBLE;
        }

        public static CommonHouse GetDefaultCommonHouse()
        {
            CommonHouse commonHouse = new CommonHouse();
            commonHouse.height = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.frontGap = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.floor = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.floorHeight = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.backGapRatio = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.backGap = Const.ERROR_INT;
            commonHouse.landHeight = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.roadWidth = Const.DEFAULT_NUMBER_VALUE;
            return commonHouse;
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
            {
                shieldVariableList = new List<string>();
            }
            if (!shieldVariableList.Contains("id") && chID == Const.ERROR_INT)
            {
                return Const.INNER_ERROR_TIP;
            }
            if (!shieldVariableList.Contains("height") && chHeight < 0)
            {
                return "户型进深须大于0";
            }
            if (!shieldVariableList.Contains("frontGap") && chFrontGap < 0)
            {
                return "户型前深须大于0";
            }
            if (!shieldVariableList.Contains("floor") && chFloor < 0)
            {
                return "户型楼层须大于0";
            }
            if (!shieldVariableList.Contains("floorHeight") && chFloorHeight < 0)
            {
                return "户型层高须大于0";
            }
            if (!shieldVariableList.Contains("backGapRatio") && chBackGapRatio < 0)
            {
                return "户型后深系数须大于0";
            }
            if (!shieldVariableList.Contains("landHeight") && landHeight < 0 && landHeight < height)
            {
                return "宅基地进深必须大于0，且小于住宅进深";
            }
            if (!shieldVariableList.Contains("villageID") && vID == Const.ERROR_INT)
            {
                return Const.INNER_ERROR_TIP;
            }
            return "";
        }

        protected override bool isValid(System.Collections.Generic.List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
            {
                shieldVariableList = new List<string>();
            }
            if (!shieldVariableList.Contains("chID") && chID == Const.ERROR_INT)
            {
                return false;
            }
            if (!shieldVariableList.Contains("chHeight") && chHeight < 0)
            {
                return false;
            }
            if (!shieldVariableList.Contains("chFrontGap") && chFrontGap < 0)
            {
                return false;
            }
            if (!shieldVariableList.Contains("chFloor") && chFloor < 0)
            {
                return false;
            }
            if (!shieldVariableList.Contains("chFloorHeight") && chFloorHeight < 0)
            {
                return false;
            }
            if (!shieldVariableList.Contains("landHeight") && landHeight < 0 && landHeight < height)
            {
                return false;
            }
            if (!shieldVariableList.Contains("vID") && vID == Const.ERROR_INT)
            {
                return false;
            }
            return true;
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "chID"}))
                return false;
            string sqlCommand = String.Format(@"insert into CommonHouse (chHeight,chFrontGap,chFloor,chFloorHeight,chBackGapRatio,chBackGap,vID,chLandHeight)
                    values({0},{1},{2},{3},{4},{5},{6},{7})", chHeight, chFrontGap, chFloor, chFloorHeight, chBackGapRatio, chBackGap, vID, chLandHeight);
            Sql sql = new Sql();
            return sql.insertCommonHouse(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format(@"update CommonHouse set chHeight={0},chFrontGap={1},chFloor={2},chFloorHeight={3},chBackGapRatio={4}
                ,chBackGap={5},vID={6},chLandHeight={7} where chID={8}", chHeight, chFrontGap, chFloor, chFloorHeight, chBackGapRatio, chBackGap, vID, chLandHeight, chID);
            Sql sql = new Sql();
            return sql.updateCommonHouse(sqlCommand);
        }

        public bool saveOrUpdate()
        {
            if (chID == Const.ERROR_INT)
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
            if (chID == Const.ERROR_INT)
                return false;
            string sqlCommand = String.Format(@"delete from CommonHouse where chID={0}", chID);
            Sql sql = new Sql();
            return sql.deleteCommonHouse(sqlCommand);
        }

        private void initBySqlDataReader(SqlDataReader reader)
        {
            reader.Read();
            chID = Int32.Parse(reader[0].ToString());
            vID = Int32.Parse(reader[1].ToString());
            chHeight = Double.Parse(reader[2].ToString());
            chFrontGap = Double.Parse(reader[3].ToString());
            chFloor = Int32.Parse(reader[4].ToString());
            chFloorHeight = Double.Parse(reader[5].ToString());
            chBackGapRatio = Double.Parse(reader[6].ToString());
            chBackGap = Double.Parse(reader[7].ToString());
            chLandHeight = Double.Parse(reader[8].ToString());
            chRoadWidth = 5;
        }

        public override bool select()
        {
            if (chID == Const.ERROR_INT)
                return false;
            string sqlCommand = String.Format("select * from CommonHouse where chID={0}", chID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectCommonHouse(sqlCommand);
            initBySqlDataReader(reader);
            sql.closeConnection();
            return true;
        }

        public static int GetLastCommonHouseID()
        {
            string sqlCommand = String.Format("select max(chID) from CommonHouse");
            Sql sql = new Sql();
            return sql.selectMaxCommonHouseID(sqlCommand);
        }
    }
}
