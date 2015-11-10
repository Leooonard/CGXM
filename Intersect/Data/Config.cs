using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Data.SqlClient;
using System.Windows.Controls;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows.Input;

namespace Intersect
{
    public class Config : DataBase
    {
        private int cfID;
        public int id
        {
            get
            {
                return cfID;
            }
            set
            {
                cfID = value;
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

        private int cdID;
        public int conditionID
        {
            get
            {
                return cdID;
            }
            set
            {
                cdID = value;
                onPropertyChanged("conditionID");
            }
        }

        private double cfValue;
        public double value
        {
            get
            {
                return cfValue;
            }
            set
            {
                cfValue = value;
                onPropertyChanged("value");
            }
        }

        private string cfName;
        public string name
        {
            get
            {
                return cfName;
            }
            set
            {
                cfName = value;
                onPropertyChanged("name");
            }
        }

        private string cfRealStandard;
        public string realStandard
        {
            get
            {
                return cfRealStandard;
            }
            set
            {
                cfRealStandard = value;
                onPropertyChanged("realStandard");
            }
        }

        public Config()
        {
            cdID = Const.ERROR_INT;
            prID = Const.ERROR_INT;
            cfID = Const.ERROR_INT;
            cfValue = Const.ERROR_DOUBLE;
            cfName = Const.ERROR_STRING;
            cfRealStandard = Const.ERROR_STRING;
        }

        public static Config GetDefaultConfig()
        {
            Config config = new Config();
            config.value = Const.DEFAULT_NUMBER_VALUE;
            return config;
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("id") && cfID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("programID") && prID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("conditionID") && cdID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("value") && cfValue < 0)
                return "配置值不能小于0";
            return "";
        }

        protected override bool isValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("cfID") && cfID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("prID") && prID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("cdID") && cdID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("cfValue") && cfValue < 0)
                return false;
            return true;
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "cfID"}))
                return false;
            string sqlCommand = String.Format("insert into Config (prID,cdID,cfValue) values({0},{1},{2})", prID, cdID, cfValue);
            Sql sql = new Sql();
            return sql.insertConfig(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format(@"update Config set prID={0},cdID={1},cfValue={2} where cfID={3}"
                , prID, cdID, cfValue, cfID);
            Sql sql = new Sql();
            return sql.updateConfig(sqlCommand);
        }

        public override bool delete()
        {
            if (!isValid(new List<string>() { "prID", "cdID", "cfValue"}))
                return false;
            string sqlCommand = String.Format("delete from Config where cfID={0}", cfID);
            Sql sql = new Sql();
            return sql.deleteConfig(sqlCommand);
        }

        public override bool select()
        {
            if (!isValid(new List<string>() { "prID", "cdID", "cfValue" }))
                return false;
            string sqlCommand = String.Format("select * from Config where cfID={0}", cfID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectConfig(sqlCommand);
            reader.Read();
            InitBySqlDataReader(reader);
            sql.closeConnection();
            return true;
        }

        public void InitBySqlDataReader(SqlDataReader reader)
        {
            id = Int32.Parse(reader[0].ToString());
            programID = Int32.Parse(reader[1].ToString());
            conditionID = Int32.Parse(reader[2].ToString());
            value = Double.Parse(reader[3].ToString());
        }

        public static int GetLastConfigID()
        {
            string sqlCommand = String.Format("select max(cfID) from Config");
            Sql sql = new Sql();
            return sql.selectMaxConfigID(sqlCommand);
        }
    }
}
