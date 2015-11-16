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

        private double chHorizontalGap;
        public double horizontalGap
        {
            get
            {
                return chHorizontalGap;
            }
            set
            {
                chHorizontalGap = value;
                onPropertyChanged("horizontalGap");
            }
        }

        private int chMinNumber;
        public int minNumber
        {
            get
            {
                return chMinNumber;
            }
            set
            {
                chMinNumber = value;
                onPropertyChanged("minNumber");
            }
        }

        private double chBackGap;
        public double backGap
        {
            get
            {
                if (chBackGap != -1)
                    return chBackGap;
                if (chBackGapRatio == -1 || chFloorHeight == -1 || chFloor == -1)
                    return -1;
                return chBackGapRatio * chFloorHeight * chFloor;
            }
            set
            {
                chBackGap = value;
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

        public CommonHouse()
        {
            chID = Const.ERROR_INT;
            chHeight = Const.ERROR_DOUBLE;
            chFrontGap = Const.ERROR_DOUBLE;
            chFloor = Const.ERROR_INT;
            chFloorHeight = Const.ERROR_DOUBLE;
            chBackGapRatio = Const.ERROR_DOUBLE;
            chHorizontalGap = Const.ERROR_DOUBLE;
            chBackGap = Const.ERROR_DOUBLE;
            chMinNumber = Const.ERROR_INT;
            vID = Const.ERROR_INT;
        }

        public static CommonHouse GetDefaultCommonHouse()
        {
            CommonHouse commonHouse = new CommonHouse();
            commonHouse.height = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.frontGap = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.floor = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.floorHeight = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.backGapRatio = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.horizontalGap = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.backGap = Const.ERROR_INT;
            commonHouse.minNumber = Const.DEFAULT_NUMBER_VALUE;
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
            if (!shieldVariableList.Contains("horizontalGap") && chHorizontalGap < 0)
            {
                return "户型间隔必须大于0";
            }
            if (!shieldVariableList.Contains("minNumber") && chMinNumber < 0)
            {
                return "最小户数必须大于0";
            }
            if (!shieldVariableList.Contains("villageID") && vID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
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
            if (!shieldVariableList.Contains("chHorizontalGap") && chHorizontalGap < 0)
            {
                return false;
            }
            if (!shieldVariableList.Contains("chMinNumber") && chMinNumber < 0)
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
            string sqlCommand = String.Format(@"insert into CommonHouse (chHeight,chFrontGap,chFloor,chFloorHeight,chBackGapRatio,chBackGap,vID,chHorizontalGap,chMinNumber)
                    values({0},{1},{2},{3},{4},{5},{6},{7},{8})", chHeight, chFrontGap, chFloor, chFloorHeight, chBackGapRatio, chBackGap, vID, chHorizontalGap, chMinNumber);
            Sql sql = new Sql();
            return sql.insertCommonHouse(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format(@"update CommonHouse set chHeight={0},chFrontGap={1},chFloor={2},chFloorHeight={3},chBackGapRatio={4}
                ,chBackGap={5},vID={6},chHorizontalGap={7},chMinNumber={8} where chID={9}", chHeight, chFrontGap, chFloor, chFloorHeight, chBackGapRatio, chBackGap, vID,chHorizontalGap, chMinNumber, chID);
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
            chHorizontalGap = Double.Parse(reader[8].ToString());
            chMinNumber = Int32.Parse(reader[9].ToString());
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
