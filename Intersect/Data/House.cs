using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Intersect
{
    public class House : DataBase
    {
        private int HNAME_MAX_LENGTH = 20;
        private const string DEFAULT_HOUSE_NAME = "户型";

        private int hID;
        public int id
        {
            get
            {
                return hID;
            }
            set
            {
                hID = value;
                onPropertyChanged("id");
            }
        }

        private double hWidth;
        public double width
        {
            get
            {
                return hWidth;
            }
            set
            {
                hWidth = value;
                onPropertyChanged("width");
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

        private int hUnit;
        public int unit
        {
            get
            {
                return hUnit;
            }
            set
            {
                hUnit = value;
                onPropertyChanged("unit");
            }
        }

        private double hWeight;
        public double weight
        {
            get
            {
                return hWeight;
            }
            set
            {
                hWeight = value;
                onPropertyChanged("houseWeight");
            }
        }

        private string hName;
        public string name
        {
            get
            {
                return hName;
            }
            set
            {
                hName = value;
                onPropertyChanged("name");
            }
        }

        private double hLandWidth;
        public double landWidth
        {
            get
            {
                return hLandWidth;
            }
            set
            {
                hLandWidth = value;
                onPropertyChanged("landWidth");
            }
        }

        public House()
        {
            hID = Const.ERROR_INT;
            hWidth = Const.ERROR_DOUBLE;
            hLandWidth = Const.ERROR_DOUBLE;
            vID = Const.ERROR_INT;
            hUnit = Const.ERROR_INT;
        }

        public static House GetDefaultHouse()
        {
            House house = new House();
            house.width = Const.DEFAULT_NUMBER_VALUE;
            house.landWidth = Const.DEFAULT_NUMBER_VALUE;
            house.unit = Const.DEFAULT_NUMBER_VALUE;
            return house;
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
            {
                shieldVariableList = new List<string>();
            }
            if (!shieldVariableList.Contains("id") && hID == Const.ERROR_INT)
            {
                return Const.INNER_ERROR_TIP;
            }
            if (!shieldVariableList.Contains("width") && hWidth <= 0 || hWidth > hLandWidth)
            {
                return "户型面宽须大于0，且小于宅基地面宽";
            }
            if (!shieldVariableList.Contains("villageID") && vID == Const.ERROR_INT)
            {
                return Const.INNER_ERROR_TIP;
            }
            if (!shieldVariableList.Contains("unit") && hUnit <= 0)
            {
                return "户型拼数须大于0";
            }
            if (!shieldVariableList.Contains("landWidth") && hLandWidth <= hWidth)
            {
                return "宅基地面块必须大于户型面宽";
            }
            return "";
        }

        protected override bool isValid(System.Collections.Generic.List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
            {
                shieldVariableList = new List<string>();
            }
            if (!shieldVariableList.Contains("hID") && hID == Const.ERROR_INT)
            {
                return false;
            }
            if (!shieldVariableList.Contains("hWidth") && hWidth <= 0 || hWidth > hLandWidth)
            {
                return false;
            }
            if (!shieldVariableList.Contains("vID") && vID == Const.ERROR_INT)
            {
                return false;
            }
            if (!shieldVariableList.Contains("hUnit") && hUnit <= 0)
            {
                return false;
            }
            if (!shieldVariableList.Contains("hLandWidth") && hLandWidth <= hWidth)
            {
                return false;
            }
            return true;
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "hID"}))
                return false;
            string sqlCommand = String.Format(@"insert into House (hWidth,vID,hUnit,hWeight,hLandWidth) values ({0},{1},{2},{3},{4})"
                , hWidth, vID, hUnit, hWeight, hLandWidth);
            Sql sql = new Sql();
            return sql.insertHouse(sqlCommand);
        }

        public bool saveWithoutCheck()
        {
            string sqlCommand = String.Format(@"insert into House (hWidth,vID,hUnit,hWeight,hLandWidth) values ({0},{1},{2},{3},{4})"
                , hWidth, vID, hUnit, hWeight, hLandWidth);
            Sql sql = new Sql();
            return sql.insertHouse(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format(@"update House set hWidth={0},vID={1},hUnit={2},hWeight={3},hLandWidth={4} where hID={5}"
                , hWidth, vID, hUnit, hWeight, hLandWidth, hID);
            Sql sql = new Sql();
            return sql.updateHouse(sqlCommand);
        }

        public bool saveOrUpdate()
        {
            if (hID == Const.ERROR_INT)
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
            if (hID == Const.ERROR_INT)
                return false;
            string sqlCommand = String.Format(@"delete from House where hID={0}", hID);
            Sql sql = new Sql();
            return sql.deleteHouse(sqlCommand);
        }

        private void initBySqlDataReader(SqlDataReader reader)
        {
            reader.Read();
            hID = Int32.Parse(reader[0].ToString());
            vID = Int32.Parse(reader[1].ToString());
            hWidth = Double.Parse(reader[2].ToString());
            hUnit = Int32.Parse(reader[3].ToString());
            hWeight = Double.Parse(reader[4].ToString());
            hLandWidth = Double.Parse(reader[5].ToString());
        }

        public override bool select()
        {
            if (hID == Const.ERROR_INT)
                return false;
            string sqlCommand = String.Format(@"select * from House where hID={0}", hID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectHouse(sqlCommand);
            initBySqlDataReader(reader);
            sql.closeConnection();
            return true;
        }

        public static int GetLastHouseID()
        {
            string sqlCommand = String.Format("select max(hID) from House");
            Sql sql = new Sql();
            return sql.selectMaxHouseID(sqlCommand);
        }
    }
}
