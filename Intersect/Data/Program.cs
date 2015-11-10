using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections.ObjectModel;

namespace Intersect
{
    public class Program : DataBase
    {
        public const int PRNAME_MAX_LENGTH = 50;
        public const string PROGRAM_DEFAULT_NAME = "方案";

        private string prName;
        public string name
        {
            get
            {
                return prName;
            }
            set
            {
                prName = value;
                onPropertyChanged("name");
            }
        }
        private int prID;
        public int id
        {
            get
            {
                return prID;
            }
            set
            {
                prID = value;
                onPropertyChanged("id");
            }
        }
        private int pID;
        public int projectID
        {
            get
            {
                return pID;
            }
            set
            {
                pID = value;
                onPropertyChanged("projectID");
            }
        }

        private bool _visible;
        public bool visible
        {
            get
            {
                return _visible;
            }
            set
            {
                _visible = value;
                onPropertyChanged("visible");
            }
        }
        public int step;

        public Program()
        {
            prID = Const.ERROR_INT;
            prName = Const.ERROR_STRING;
            pID = Const.ERROR_INT;

            _visible = false;
            step = 0;
        }

        public void InitBySqlDataReader(SqlDataReader reader)
        {
            id = Int32.Parse(reader[0].ToString());
            name = reader[1].ToString();
            projectID = Int32.Parse(reader[2].ToString());
        }

        public static int GetLastProgramID()
        {
            string sqlCommand = String.Format("select max(prID) from Program");
            Sql sql = new Sql();
            return sql.selectMaxProgramID(sqlCommand);
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("id") && prID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("name") && (prName.Length == 0 || prName.Length > PRNAME_MAX_LENGTH))
                return String.Format("方案名长度须在0-{0}之间", PRNAME_MAX_LENGTH);
            if (!shieldVariableList.Contains("projectID") && pID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            return "";
        }

        protected override bool isValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("prID") && prID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("prName") && (prName.Length == 0 || prName.Length > PRNAME_MAX_LENGTH))
                return false;
            if (!shieldVariableList.Contains("pID") && pID == Const.ERROR_INT)
                return false;
            return true;
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "prID"}))
                return false;
            string sqlCommand = String.Format("insert into Program (prName,pID) values('{0}',{1})"
                , prName, pID);
            Sql sql = new Sql();
            return sql.insertProgram(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format("update Program set prName='{0}',pID={1} where prID={2}"
                , prName, pID, prID);
            Sql sql = new Sql();
            return sql.updateProgram(sqlCommand);
        }

        public override bool delete()
        {
            if (!isValid(new List<string>() { "prName", "pID"}))
                return false;
            string sqlCommand = String.Format("delete from Program where prID={0}", prID);
            Sql sql = new Sql();
            return sql.deleteProgram(sqlCommand);
        }

        public override bool select()
        {
            if (!isValid(new List<string>() { "prName", "pID" }))
                return false;
            string sqlCommand = String.Format("select * from Program where prID={0}", prID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectProgram(sqlCommand);
            reader.Read();
            InitBySqlDataReader(reader);
            sql.closeConnection();
            return true;
        }

        public ObservableCollection<Config> selectAllRelatedConfig()
        {
            if (!isValid(new List<string>() { "prName", "pID" }))
                return null;
            ObservableCollection<Config> configList = new ObservableCollection<Config>();
            string sqlCommand = String.Format("select cfID from Config where prID={0}", prID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectConfigIDByPrID(sqlCommand);
            while (reader.Read())
            {
                int cfID = Int32.Parse(reader[0].ToString());
                Config config = new Config();
                config.id = cfID;
                config.select();
                configList.Add(config);
            }
            sql.closeConnection();
            return configList;
        }

        public ObservableCollection<Condition> getAllRelatedCondition()
        {
            if (!isValid(new List<string>() { "prName", "pID" }))
                return null;
            ObservableCollection<Condition> conditionList = new ObservableCollection<Condition>();
            string sqlCommand = String.Format("select cdID from Condition where prID={0}", prID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectConditionIDByPrID(sqlCommand);
            while (reader.Read())
            {
                int cdID = Int32.Parse(reader[0].ToString());
                Condition condition = new Condition();
                condition.id = cdID;
                condition.select();
                conditionList.Add(condition);
            }
            sql.closeConnection();
            return conditionList;
        }

        public NetSize getRelatedNetSize()
        {
            if (!isValid(new List<string>() { "prName", "pID" }))
                return null;
            string sqlCommand = String.Format(@"select nsID from NetSize where prID={0}", prID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectNetSizeIDByPrID(sqlCommand);
            if (!reader.HasRows)
                return null;
            reader.Read();
            NetSize netSize = new NetSize();
            netSize.id = Int32.Parse(reader[0].ToString());
            netSize.select();
            return netSize;
        }

        public CommonHouse selectRelatedCommonHouse()
        {
            if (!isValid(new List<string>() { "prName", "pID" }))
                return null;
            CommonHouse commonHouse = new CommonHouse();
            string sqlCommand = String.Format("select chID from CommonHouse where prID={0}", prID);
            Sql sql = new Sql();
            int chID = sql.selectCommonHouseIDByPrID(sqlCommand);
            if (chID == -1)
                return null;
            commonHouse.id = chID;
            commonHouse.select();
            return commonHouse;
        }

        public ObservableCollection<House> selectAllRelatedHouse()
        {
            if (!isValid(new List<string>() { "prName", "pID" }))
                return null;
            ObservableCollection<House> houseList = new ObservableCollection<House>();
            string sqlCommand = String.Format("select hID from House where prID={0}", prID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectHouseIDByPrID(sqlCommand);
            if (!reader.HasRows)
            {
                sql.closeConnection();
                return null;
            }
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

        public ObservableCollection<MainRoad> getAllRelatedMainRoad()
        {
            if (!isValid(new List<string>() { "prName", "pID" }))
                return null;

            ObservableCollection<MainRoad> mainRoadList = new ObservableCollection<MainRoad>();
            string sqlCommand = String.Format(@"select mrID from MainRoad where prID={0}", prID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectAllMainRoadIDByPrID(sqlCommand);
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    int mrID = Int32.Parse(reader[0].ToString());
                    MainRoad mainRoad = new MainRoad();
                    mainRoad.id = mrID;
                    mainRoad.select();
                    mainRoadList.Add(mainRoad);
                }
                sql.closeConnection();
                return mainRoadList;
            }
            else
            {
                sql.closeConnection();
                return null;
            }
        }

        public ObservableCollection<Village> getAllRelatedVillage()
        {
            if (!isValid(new List<string>() { "prName", "pID" }))
                return null;

            ObservableCollection<Village> villageList = new ObservableCollection<Village>();
            string sqlCommand = String.Format(@"select vID from Village where prID={0}", prID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectAllVillageByPrID(sqlCommand);
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    int vID = Int32.Parse(reader[0].ToString());
                    Village village = new Village();
                    village.id = vID;
                    village.select();
                    villageList.Add(village);
                }
                sql.closeConnection();
                return villageList;
            }
            else
            {
                sql.closeConnection();
                return null;
            }
        }

        public ObservableCollection<InnerRoad> getAllRelatedInnerRoad()
        {
            if (!isValid(new List<string>() { "prName", "pID" }))
                return null;

            ObservableCollection<InnerRoad> innerRoadList = new ObservableCollection<InnerRoad>();
            string sqlCommand = String.Format(@"select irID from InnerRoad where prID={0}", prID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectAllInnerRoadByPrID(sqlCommand);
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    int irID = Int32.Parse(reader[0].ToString());
                    InnerRoad innerRoad = new InnerRoad();
                    innerRoad.id = irID;
                    innerRoad.select();
                    innerRoadList.Add(innerRoad);
                }
                sql.closeConnection();
                return innerRoadList;
            }
            else
            {
                sql.closeConnection();
                return null;
            }
        }
    }
}
