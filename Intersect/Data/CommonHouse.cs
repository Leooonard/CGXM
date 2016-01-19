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
                onPropertyChanged("landHeight");
            }
        }

        public CommonHouse()
        {
            chID = Const.ERROR_INT;
            chFloor = Const.ERROR_INT;
            chFloorHeight = Const.ERROR_DOUBLE;
            vID = Const.ERROR_INT;
            chLandHeight = Const.ERROR_DOUBLE;
        }

        public static CommonHouse GetDefaultCommonHouse()
        {
            CommonHouse commonHouse = new CommonHouse();
            commonHouse.floor = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.floorHeight = Const.DEFAULT_NUMBER_VALUE;
            commonHouse.landHeight = Const.DEFAULT_NUMBER_VALUE;
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
            if (!shieldVariableList.Contains("floor") && chFloor < 0)
            {
                return "户型楼层须大于0";
            }
            if (!shieldVariableList.Contains("floorHeight") && chFloorHeight < 0)
            {
                return "户型层高须大于0";
            }
            if (!shieldVariableList.Contains("landHeight") && chLandHeight <= 0)
            {
                return "宅基地进深须大于0";
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
            if (!shieldVariableList.Contains("chFloor") && chFloor < 0)
            {
                return false;
            }
            if (!shieldVariableList.Contains("chFloorHeight") && chFloorHeight < 0)
            {
                return false;
            }
            if (!shieldVariableList.Contains("chLandHeight") && chLandHeight < 0)
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
            string sqlCommand = String.Format(@"insert into CommonHouse (chFloor,chFloorHeight,vID,chLandHeight)
                    values({0},{1},{2},{3})", chFloor, chFloorHeight, vID, chLandHeight);
            Sql sql = new Sql();
            return sql.insertCommonHouse(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format(@"update CommonHouse set chFloor={0},chFloorHeight={1},vID={2},
                chLandHeight={3} where chID={4}", chFloor, chFloorHeight, vID, chLandHeight, chID);
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
            chFloor = Int32.Parse(reader[2].ToString());
            chFloorHeight = Double.Parse(reader[3].ToString());
            chLandHeight = Double.Parse(reader[4].ToString());
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
