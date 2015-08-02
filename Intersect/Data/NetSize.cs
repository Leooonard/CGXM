using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Intersect
{
    public class NetSize : DataBase
    {
        private int nsID;
        public int id
        {
            get
            {
                return nsID;
            }
            set
            {
                nsID = value;
                onPropertyChanged("id");
            }
        }
        private double nsWidth;
        public double width
        {
            get
            {
                return nsWidth;
            }
            set
            {
                nsWidth = value;
                onPropertyChanged("width");
            }
        }
        private double nsHeight;
        public double height
        {
            get
            {
                return nsHeight;
            }
            set
            {
                nsHeight = value;
                onPropertyChanged("height");
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

        public NetSize()
        {
            nsID = C.ERROR_INT;
            nsWidth = C.ERROR_DOUBLE;
            nsHeight = C.ERROR_DOUBLE;
            prID = C.ERROR_INT;
        }

        public void initBySqlDataReader(SqlDataReader reader)
        {
            reader.Read();
            nsID = Int32.Parse(reader[0].ToString());
            nsWidth = Double.Parse(reader[1].ToString());
            nsHeight = Double.Parse(reader[2].ToString());
            prID = Int32.Parse(reader[3].ToString());
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("id") && nsID == C.ERROR_INT)
                return C.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("width") && nsWidth == C.ERROR_DOUBLE)
                return "网格宽度须大于0";
            if (!shieldVariableList.Contains("height") && nsHeight == C.ERROR_DOUBLE)
                return "网格高度须大于0";
            if (!shieldVariableList.Contains("programID") && prID == C.ERROR_INT)
                return C.INNER_ERROR_TIP;
            return "";
        }

        protected override bool isValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("nsID") && nsID == C.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("nsWidth") && nsWidth == C.ERROR_DOUBLE)
                return false;
            if (!shieldVariableList.Contains("nsHeight") && nsHeight == C.ERROR_DOUBLE)
                return false;
            if (!shieldVariableList.Contains("prID") && prID == C.ERROR_INT)
                return false;
            return true;
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "nsID"}))
                return false;
            string sqlCommand = String.Format(@"insert into NetSize (nsWidth,nsHeight,prID) values({0},{1},{2})", 
                nsWidth, nsHeight, prID);
            Sql sql = new Sql();
            return sql.insertNetSize(sqlCommand);
        }

        public bool saveWithoutCheck()
        {
            string sqlCommand = String.Format(@"insert into NetSize (nsWidth,nsHeight,prID) values({0},{1},{2})",
                nsWidth, nsHeight, prID);
            Sql sql = new Sql();
            return sql.insertNetSize(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format(@"update NetSize set nsWidth={0},nsHeight={1},prID={2} where nsID={3}",
                nsWidth, nsHeight, prID, nsID);
            Sql sql = new Sql();
            return sql.updateNetSize(sqlCommand);
        }

        public override bool delete()
        {
            if (!isValid(new List<string>() { "nsWidth", "nsHeight", "prID"}))
                return false;
            string sqlCommand = String.Format(@"delete from NetSize where nsID={0}", nsID);
            Sql sql = new Sql();
            return sql.deleteNetSize(sqlCommand);
        }

        public override bool select()
        {
            if (!isValid(new List<string> { "nsWidth", "nsHeight", "prID"}))
                return false;
            string sqlCommand = String.Format(@"select * from NetSize where nsID={0}", nsID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectNetSize(sqlCommand);
            if (!reader.HasRows)
                return false;
            initBySqlDataReader(reader);
            sql.closeConnection();
            return true;
        }

        public bool compare(NetSize netSize)
        {
            if (id != netSize.id)
                return false;
            if (prID != netSize.programID)
                return false;
            if (nsWidth != netSize.width)
                return false;
            if (nsHeight != netSize.height)
                return false;
            return true;
        }

        public static int GetLastNetSizeID()
        {
            string sqlCommand = String.Format("select max(nsID) from NetSize");
            Sql sql = new Sql();
            return sql.selectMaxProjectAndMapID(sqlCommand);
        }
    }
}
