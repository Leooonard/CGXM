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

        private double hLandArea;
        public double landArea
        {
            get
            {
                return hLandArea;
            }
            set
            {
                hLandArea = value;
                onPropertyChanged("area");
            }
        }

        private double hFrontGap;
        public double frontGap
        {
            get
            {
                return hFrontGap;
            }
            set
            {
                hFrontGap = value;
                onPropertyChanged("frontGap");
            }
        }

        private double hBackGap;
        public double backGap
        {
            get
            {
                return hBackGap;
            }
            set
            {
                hBackGap = value;
                onPropertyChanged("backGap");
            }
        }

        private double hHeight;
        public double height
        {
            get
            {
                return hHeight;
            }
            set
            {
                hHeight = value;
                onPropertyChanged("height");
            }
        }

        public void updateData(CommonHouse commonHouse)
        {
            landWidth = landArea / commonHouse.landHeight;
            height = commonHouse.landHeight - frontGap - backGap;
        }

        private CommonHouse commonHouse
        {
            get
            {
                commonHouse.select();
                return commonHouse;
            }
            set
            {
                commonHouse = value;
            }
        }

        public House()
        {
            hID = Const.ERROR_INT;
            vID = Const.ERROR_INT;
            hWidth = Const.ERROR_DOUBLE;
            hLandWidth = Const.ERROR_DOUBLE;
            hUnit = Const.ERROR_INT;
            hWeight = Const.ERROR_DOUBLE;
            hLandArea = Const.ERROR_DOUBLE;
            hFrontGap = Const.ERROR_DOUBLE;
            hBackGap = Const.ERROR_DOUBLE;
            hHeight = Const.ERROR_DOUBLE;
        }

        public static House GetDefaultHouse()
        {
            House house = new House();
            house.width = Const.DEFAULT_NUMBER_VALUE;
            house.landWidth = Const.DEFAULT_NUMBER_VALUE;
            house.unit = Const.DEFAULT_NUMBER_VALUE;
            house.landArea = Const.DEFAULT_NUMBER_VALUE;
            house.frontGap = Const.DEFAULT_NUMBER_VALUE;
            house.backGap = Const.DEFAULT_NUMBER_VALUE;
            house.height = Const.DEFAULT_NUMBER_VALUE;
            house.weight = Const.DEFAULT_NUMBER_VALUE;
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
                return "住宅面宽须大于0，且小于等于宅基地面宽";
            }
            if (!shieldVariableList.Contains("villageID") && vID == Const.ERROR_INT)
            {
                return Const.INNER_ERROR_TIP;
            }
            if (!shieldVariableList.Contains("unit") && hUnit <= 0)
            {
                return "户型拼数须大于0";
            }
            if (!shieldVariableList.Contains("landWidth") && hLandWidth == Const.ERROR_DOUBLE || hLandWidth < hWidth)
            {
                return "宅基地面块须大于等于住宅面宽";
            }
            if (!shieldVariableList.Contains("frontGap") && hFrontGap == Const.ERROR_DOUBLE)
            {
                return "宅基地前院深须大于0";
            }
            if (!shieldVariableList.Contains("backGap") && hBackGap == Const.ERROR_DOUBLE)
            {
                return "宅基地后院深须大于0";
            }
            if (!shieldVariableList.Contains("weight") && hWeight == Const.ERROR_DOUBLE)
            {
                return "户型占比须大于0";
            }
            if (!shieldVariableList.Contains("landArea") && hLandArea == Const.ERROR_DOUBLE)
            {
                return "宅基地占地面积须大于0";
            }
            if (!shieldVariableList.Contains("height") && hHeight == Const.ERROR_DOUBLE)
            {
                return "住宅进深须大于0";
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
            if (!shieldVariableList.Contains("frontGap") && hFrontGap == Const.ERROR_DOUBLE)
            {
                return false;
            }
            if (!shieldVariableList.Contains("backGap") && hBackGap == Const.ERROR_DOUBLE)
            {
                return false;
            }
            if (!shieldVariableList.Contains("weight") && hWeight == Const.ERROR_DOUBLE)
            {
                return false;
            }
            if (!shieldVariableList.Contains("landArea") && hLandArea == Const.ERROR_DOUBLE)
            {
                return false;
            }
            if (!shieldVariableList.Contains("height") && hHeight == Const.ERROR_DOUBLE)
            {
                return false;
            }
            return true;
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "hID"}))
                return false;
            string sqlCommand = String.Format(@"insert into House (hWidth,vID,hUnit,hWeight,hLandWidth,hLandArea,
                hFrontGap,hBackGap,hHeight) values ({0},{1},{2},{3},{4},{5},{6},{7},{8})"
                , hWidth, vID, hUnit, hWeight, hLandWidth, hLandArea, hFrontGap, hBackGap, hHeight);
            Sql sql = new Sql();
            return sql.insertHouse(sqlCommand);
        }

        public bool saveWithoutCheck()
        {
            string sqlCommand = String.Format(@"insert into House (hWidth,vID,hUnit,hWeight,hLandWidth,hLandArea,
                hFrontGap,hBackGap,hHeight) values ({0},{1},{2},{3},{4},{5},{6},{7},{8})"
                , hWidth, vID, hUnit, hWeight, hLandWidth, hLandArea, hFrontGap, hBackGap, hHeight);
            Sql sql = new Sql();
            return sql.insertHouse(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format(@"update House set hWidth={0},vID={1},hUnit={2},hWeight={3},
                hLandWidth={4},hLandArea={5},hFrontGap={6},hBackGap={7},hHeight={8} where hID={9}"
                , hWidth, vID, hUnit, hWeight, hLandWidth, hLandArea, hFrontGap, hBackGap, hHeight, hID);
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
            hLandArea = Double.Parse(reader[6].ToString());
            hFrontGap = Double.Parse(reader[7].ToString());
            hBackGap = Double.Parse(reader[8].ToString());
            hHeight = Double.Parse(reader[9].ToString());
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
