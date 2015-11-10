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

        private double hLeftGap;
        public double leftGap
        {
            get
            {
                return hLeftGap;
            }
            set
            {
                hLeftGap = value;
                onPropertyChanged("leftGap");
            }
        }

        private double hRightGap;
        public double rightGap
        {
            get
            {
                return hRightGap;
            }
            set
            {
                hRightGap = value;
                onPropertyChanged("rightGap");
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

        private int hHouseHold;
        public int houseHold
        {
            get
            {
                return hHouseHold;
            }
            set
            {
                hHouseHold = value;
                onPropertyChanged("houseHold");
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

        public int housePlacerListIndex
        {
            get;
            set;
        }

        public House()
        {
            hID = Const.ERROR_INT;
            hWidth = Const.ERROR_DOUBLE;
            hLeftGap = Const.ERROR_DOUBLE;
            hRightGap = Const.ERROR_DOUBLE;
            vID = Const.ERROR_INT;
            hUnit = Const.ERROR_INT;
            hHouseHold = Const.ERROR_INT;
            hName = Const.ERROR_STRING;
        }

        public static House GetDefaultHouse()
        {
            House house = new House();
            house.width = Const.DEFAULT_NUMBER_VALUE;
            house.leftGap = Const.DEFAULT_NUMBER_VALUE;
            house.rightGap = Const.DEFAULT_NUMBER_VALUE;
            house.unit = Const.DEFAULT_NUMBER_VALUE;
            house.houseHold = Const.DEFAULT_NUMBER_VALUE;
            house.name = DEFAULT_HOUSE_NAME;
            return house;
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("id") && hID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("width") && hWidth < 0)
                return "户型面宽须大于0";
            if (!shieldVariableList.Contains("leftGap") && hLeftGap < 0)
                return "户型左间距须大于0";
            if (!shieldVariableList.Contains("rightGap") && hRightGap < 0)
                return "户型面右间距大于0";
            if (!shieldVariableList.Contains("villageID") && vID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("unit") && hUnit < 0)
                return "户型户数须大于0";
            if (!shieldVariableList.Contains("houseHold") && hHouseHold < 0)
                return "户型人俗须大于0";
            if (!shieldVariableList.Contains("name") && (hName.Length == 0 || hName.Length > HNAME_MAX_LENGTH))
                return String.Format("户型名长度须在0-{0}之间.", HNAME_MAX_LENGTH);
            return "";
        }

        protected override bool isValid(System.Collections.Generic.List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("hID") && hID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("hWidth") && hWidth < 0)
                return false;
            if (!shieldVariableList.Contains("hLeftGap") && hLeftGap < 0)
                return false;
            if (!shieldVariableList.Contains("hRightGap") && hRightGap < 0)
                return false;
            if (!shieldVariableList.Contains("vID") && vID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("hUnit") && hUnit < 0)
                return false;
            if (!shieldVariableList.Contains("hHouseHold") && hHouseHold < 0)
                return false;
            if (!shieldVariableList.Contains("hName") && (hName.Length == 0 || hName.Length > HNAME_MAX_LENGTH))
                return false;
            return true;
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "hID"}))
                return false;
            string sqlCommand = String.Format(@"insert into House (hWidth,hLeftGap,hRightGap,vID,hUnit,hHouseHold,hName) values ({0},{1},{2},{3},{4},{5},'{6}')"
                , hWidth, hLeftGap, hRightGap, vID, hUnit, hHouseHold, hName);
            Sql sql = new Sql();
            return sql.insertHouse(sqlCommand);
        }

        public bool saveWithoutCheck()
        {
            string sqlCommand = String.Format(@"insert into House (hWidth,hLeftGap,hRightGap,vID,hUnit,hHouseHold,hName) values ({0},{1},{2},{3},{4},{5},'{6}')"
                , hWidth, hLeftGap, hRightGap, vID, hUnit, hHouseHold, hName);
            Sql sql = new Sql();
            return sql.insertHouse(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format(@"update House set hWidth={0},hLeftGap={1},hRightGap={2},vID={3},hUnit={4},hHouseHold={5},hName='{6}' where hID={7}"
                , hWidth, hLeftGap, hRightGap, vID, hUnit, hHouseHold, hName, hID);
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
            hLeftGap = Double.Parse(reader[3].ToString());
            hRightGap = Double.Parse(reader[4].ToString());
            hUnit = Int32.Parse(reader[5].ToString());
            hHouseHold = Int32.Parse(reader[6].ToString());
            hName = reader[7].ToString();
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

        public bool compare(House house)
        {
            if (house == null)
                return false;
            if (hID != house.id)
                return false;
            if (hWidth != house.width)
                return false;
            if (hLeftGap != house.leftGap)
                return false;
            if (hRightGap != house.rightGap)
                return false;
            if (hUnit != house.unit)
                return false;
            if (hHouseHold != house.houseHold)
                return false;
            if (hName != house.name)
                return false;
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
