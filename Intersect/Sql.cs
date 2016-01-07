using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections;
using System.Windows;

namespace Intersect
{
    class Sql
    {
        private SqlConnection openConn()
        {
            string connectionString = String.Format(@"server=(local);database={0};Integrated Security=True;MultipleActiveResultSets=true;User ID={1};Password={2}",
                Const.CONFIG["DATABASE_NAME"], Const.CONFIG["DATABASE_USERNAME"], Const.CONFIG["DATABASE_PASSWORD"]);
            SqlConnection conn = new SqlConnection(connectionString);
            conn.Open();
            return conn;
        }

        private void closeConnection(SqlConnection conn)
        {
            conn.Close();
        }
        public void closeConnection()
        {
            if (openingConnectionList.Count > 0)
            {
                openingConnectionList[openingConnectionList.Count - 1].Close();
                openingConnectionList.RemoveAt(openingConnectionList.Count - 1);
            }
        }

        public int InsertProjectAndMap(string name, string path, List<Condition> conditionList)
        {
            SqlConnection conn = openConn();
            string comm = "insert into ProjectAndMap (pmName,pmPath) values('"+ name+ "','"+ path+"')";
            SqlCommand com = new SqlCommand(comm, conn);
            int result = com.ExecuteNonQuery();

            if (result == 0)
            {
                closeConnection(conn);
                return -1;
            }
            else
            {
                comm = "select * from ProjectAndMap where pmName='" + name + "'";
                com = new SqlCommand(comm, conn);
                SqlDataReader reader = com.ExecuteReader();
                reader.Read();
                result = Int32.Parse(reader[0].ToString());
                for (int i = 0; i < conditionList.Count; i++)
                {
                    //insertCondition(result, conditionList[i]);
                }
                closeConnection(conn);
                return result;
            }
        }

        public SqlDataReader SelectAllProjectAndMap(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            return reader;
        }

        public Project SelectProjectAndMapById(int id)
        {
            SqlConnection conn = openConn();

            string comm = "select * from ProjectAndMap where pmID=" + id.ToString();
            SqlCommand com = new SqlCommand(comm, conn);
            SqlDataReader reader = com.ExecuteReader();

            if (reader.HasRows)
            {
                reader.Read();
                id = Int32.Parse(reader[0].ToString());
                string name = reader[1].ToString();
                string path = reader[2].ToString();
                //ProjectAndMap pam = new ProjectAndMap(id, name, path);
                Project pam = null;

                closeConnection(conn);
                return pam;
            }
            else
            {
                closeConnection(conn);
                return null;
            }
        }

        public bool UpdateProjectAndMap(int id, string name, string path, List<Condition> conditionList)
        {
            if (name == null || path == null)
            {
                return false;
            }

            SqlConnection conn = openConn();

            string comm = "update ProjectAndMap set pmName='" + name + "',pmPath='" + path + "' where pmID=" + id.ToString();
            SqlCommand com = new SqlCommand(comm, conn);
            int result = com.ExecuteNonQuery();

            UpdateConditionByPmID(id, conditionList);

            closeConnection(conn);
            return result != 0;
        }

        public bool DeleteProjectAndMapById(int id)
        {
            SqlConnection conn = openConn();

            string comm = "delete from ProjectAndMap where pmID=" + id.ToString();
            SqlCommand com = new SqlCommand(comm, conn);
            int result = com.ExecuteNonQuery();

            List<Program> programList = SelectAllProgramByPmID(id);
            for (int i = 0; i < programList.Count; i++)
            {
                DeleteProgramByID(programList[i].id);
            }

            closeConnection(conn);
            return result != 0;
        }


        public List<Condition> SelectConditionByPmID(int pmID)
        {
            List<Condition> conditionList = new List<Condition>();
            SqlConnection conn = openConn();
            string comm = "select * from Condition where pmID=" + pmID.ToString();
            SqlCommand com = new SqlCommand(comm, conn);
            SqlDataReader reader = com.ExecuteReader();

            if (!reader.HasRows)
            {
                conditionList = null;
            }
            else
            {
                while (reader.Read())
                {
                    Condition condition = new Condition();
                    condition.InitBySqlDataReader(reader);
                    conditionList.Add(condition);
                }
            }

            closeConnection(conn);
            return conditionList;
        }

        public void UpdateConditionByPmID(int pmID, List<Condition> conditionList)
        { 
            //先删除所有与该pmID相关的condition.
            DeleteConditionByPmID(pmID);

            for (int i = 0; i < conditionList.Count; i++)
            {
                //InsertCondition(pmID, conditionList[i]);
            }
        }

        public bool commonOperation(string comm)
        {
            //对于增删改3个操作, 流程完全相同, 抽成一个函数.
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            int result = com.ExecuteNonQuery();
            closeConnection(conn);
            return result != 0;
        }

        public SqlDataReader commonSelectOperation(string comm)
        {
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            SqlDataReader reader = com.ExecuteReader();
            openingConnectionList.Add(conn);
            return reader;
        }

        private List<SqlConnection> openingConnectionList = new List<SqlConnection>();

        //-----Project-----
        public bool insertProjectAndMap(string comm)
        {
            return commonOperation(comm);
        }

        public bool updateProjectAndMap(string comm)
        {
            return commonOperation(comm);
        }

        public bool deleteProjectAndMap(string comm)
        {
            return commonOperation(comm);
        }

        public SqlDataReader selectProjectAndMap(string comm)
        {
            return commonSelectOperation(comm);
        }

        public int selectMaxProjectAndMapID(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            reader.Read();
            return Int32.Parse(reader[0].ToString());
        }

        //-----Project-----

        //-----Condition-----
        public bool insertCondition(string comm)
        {
            return commonOperation(comm);
        }

        public bool updateCondition(string comm)
        {
            return commonOperation(comm);
        }

        public bool deleteCondition(string comm)
        {
            return commonOperation(comm);
        }

        public SqlDataReader selectCondition(string comm)
        {
            return commonSelectOperation(comm);
        }

        public int selectMaxConditionID(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            reader.Read();
            return Int32.Parse(reader[0].ToString());
        }

        public SqlDataReader selectConditionIDByPmID(string comm)
        {
            return commonSelectOperation(comm);
        }
        //-----Condition-----

        //-----Program-----
        public bool insertProgram(string comm)
        {
            return commonOperation(comm);
        }
        public bool updateProgram(string comm)
        {
            return commonOperation(comm);
        }
        public bool deleteProgram(string comm)
        {
            return commonOperation(comm);
        }
        public SqlDataReader selectProgram(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectProgramIDByPmID(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectConditionIDByPrID(string comm)
        {
            return commonSelectOperation(comm);
        }
        public int selectMaxProgramID(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            reader.Read();
            int id = Int32.Parse(reader[0].ToString());
            closeConnection();
            return id;
        }
        //-----Program-----

        //-----Config-----
        public bool insertConfig(string comm)
        {
            return commonOperation(comm);
        }
        public bool updateConfig(string comm)
        {
            return commonOperation(comm);
        }
        public bool deleteConfig(string comm)
        {
            return commonOperation(comm);
        }
        public SqlDataReader selectConfig(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectConfigIDByPrID(string comm)
        {
            return commonSelectOperation(comm);
        }
        public int selectMaxConfigID(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            reader.Read();
            int id = Int32.Parse(reader[0].ToString());
            closeConnection();
            return id;
        }
        //-----Config-----

        //-----CommonHouse-----
        public bool insertCommonHouse(string comm)
        {
            return commonOperation(comm);
        }
        public bool updateCommonHouse(string comm)
        {
            return commonOperation(comm);
        }
        public bool deleteCommonHouse(string comm)
        {
            return commonOperation(comm);
        }
        public SqlDataReader selectCommonHouse(string comm)
        {
            return commonSelectOperation(comm);
        }
        public int selectCommonHouseIDByPrID(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            if (reader.HasRows)
            {
                reader.Read();
                int id = Int32.Parse(reader[0].ToString());
                closeConnection();
                return id;
            }
            else
            {
                //没有记录.
                closeConnection();
                return -1;
            }
        }
        public SqlDataReader selectCommonHouseIDByVID(string comm)
        {
            return commonSelectOperation(comm);
        }
        public int selectMaxCommonHouseID(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            reader.Read();
            int id = Int32.Parse(reader[0].ToString());
            closeConnection();
            return id;
        }
        //-----CommonHouse-----

        //-----House-----
        public bool insertHouse(string comm)
        {
            return commonOperation(comm);
        }
        public bool updateHouse(string comm)
        {
            return commonOperation(comm);
        }
        public bool deleteHouse(string comm)
        {
            return commonOperation(comm);
        }
        public SqlDataReader selectHouse(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectHouseIDByPrID(string comm)
        {
            return commonSelectOperation(comm);
        }
        public int selectMaxHouseID(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            reader.Read();
            int id = Int32.Parse(reader[0].ToString());
            closeConnection();
            return id;
        }
        public SqlDataReader selectHouseIDByVID(string comm)
        {
            return commonSelectOperation(comm);
        }
        //-----House-----

        //-----Feature-----
        public bool insertFeature(string comm)
        {
            return commonOperation(comm);
        }
        public bool updateFeature(string comm)
        {
            return commonOperation(comm);
        }
        public bool deleteFeature(string comm)
        {
            return commonOperation(comm);
        }
        public SqlDataReader selectFeature(string comm)
        {
            return commonSelectOperation(comm);
        }
        //-----Feature-----

        //-----CityPlanStandard-----
        public SqlDataReader selectCityPlanStandard(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectAllCityPlanStandard(string comm)
        {
            return commonSelectOperation(comm);
        }
        public int selectCityPlanStandardIDByCpsNumber(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            if (!reader.HasRows)
                return Const.ERROR_INT;
            reader.Read();
            int id = Int32.Parse(reader[0].ToString());
            closeConnection();
            return id;
        }
        //-----CityPlanStandard-----

        //-----Label-----
        public bool insertLabel(string comm)
        {
            return commonOperation(comm);
        }
        public bool updateLabel(string comm)
        {
            return commonOperation(comm);
        }
        public bool deleteLabel(string comm)
        {
            return commonOperation(comm);
        }
        public SqlDataReader selectLabel(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectLabelByMapLayerName(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectAllLabelIDByPID(string comm)
        {
            return commonSelectOperation(comm);
        }
        //-----Label-----

        //-----NetSize-----
        public bool insertNetSize(string comm)
        {
            return commonOperation(comm);
        }
        public bool updateNetSize(string comm)
        {
            return commonOperation(comm);
        }
        public bool deleteNetSize(string comm)
        {
            return commonOperation(comm);
        }
        public SqlDataReader selectNetSize(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectNetSizeIDByPrID(string comm)
        {
            return commonSelectOperation(comm);
        }
        public int selectMaxNetSizeID(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            if (!reader.HasRows)
                return Const.ERROR_INT;
            reader.Read();
            int id = Int32.Parse(reader[0].ToString());
            closeConnection();
            return id;
        }
        //-----NetSize-----

        //-----MainRoad-----
        public bool insertMainRoad(string comm)
        {
            return commonOperation(comm);
        }
        public bool updateMainRoad(string comm)
        {
            return commonOperation(comm);
        }
        public bool deleteMainRoad(string comm)
        {
            return commonOperation(comm);
        }
        public SqlDataReader selectMainRoad(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectAllMainRoadIDByPrID(string comm)
        {
            return commonSelectOperation(comm);
        }
        public int selectMaxMainRoadID(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            if (!reader.HasRows)
                return Const.ERROR_INT;
            reader.Read();
            int id = Int32.Parse(reader[0].ToString());
            closeConnection();
            return id;
        }
        //-----MainRoad-----

        //-----Village-----
        public bool insertVillage(string comm)
        {
            return commonOperation(comm);
        }
        public bool updateVillage(string comm)
        {
            return commonOperation(comm);
        }
        public bool deleteVillage(string comm)
        {
            return commonOperation(comm);
        }
        public SqlDataReader selectVillage(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectAllVillageByPrID(string comm)
        {
            return commonSelectOperation(comm);
        }
        public int selectMaxVillageID(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            reader.Read();
            return Int32.Parse(reader[0].ToString());
        }
        //-----Village-----

        //-----InnerRoad-----
        public bool insertInnerRoad(string comm)
        {
            return commonOperation(comm);
        }
        public bool updateInnerRoad(string comm)
        {
            return commonOperation(comm);
        }
        public bool deleteInnerRoad(string comm)
        {
            return commonOperation(comm);
        }
        public SqlDataReader selectInnerRoad(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectAllInnerRoadByPrID(string comm)
        {
            return commonSelectOperation(comm);
        }
        public SqlDataReader selectInnerRoadIDByVillageID(string comm)
        {
            return commonSelectOperation(comm);
        }
        public int selectMaxInnerRoadID(string comm)
        {
            SqlDataReader reader = commonSelectOperation(comm);
            reader.Read();
            return Int32.Parse(reader[0].ToString());
        }
        //-----InnerRoad-----

        public bool DeleteConditionByPmID(int pmID)
        {
            SqlConnection conn = openConn();
            string comm = "delete from Condition where pmID=" + pmID.ToString();
            SqlCommand com = new SqlCommand(comm, conn);
            int result = com.ExecuteNonQuery();

            closeConnection(conn);
            return result != 0;
        }

        public Condition SelectConditionByID(int id)
        {
            Condition condition = new Condition();
            SqlConnection conn = openConn();
            string comm = "select * from Condition where cID = " + id.ToString();
            SqlCommand com = new SqlCommand(comm, conn);
            SqlDataReader reader = com.ExecuteReader();

            if (!reader.HasRows)
            {
                condition = null;
            }
            else
            { 
                reader.Read();
                condition.InitBySqlDataReader(reader);
            }

            closeConnection(conn);
            return condition;
        }

        public List<Program> SelectAllProgramByPmID(int pmID)
        {
            List<Program> programList = new List<Program>();

            string comm = "select * from Program where prPmID=" + pmID.ToString();
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            SqlDataReader reader = com.ExecuteReader();

            while (reader.Read())
            {
                Program program = new Program();
                program.InitBySqlDataReader(reader);
                programList.Add(program);
            }

            closeConnection(conn);
            return programList;
        }

        public bool CreateProgram(int pmID, string name)
        {
            int result;
            string comm = "insert into Program(prName, prPmID) values('" + name + "'," + pmID.ToString() + ")";
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            result = com.ExecuteNonQuery();

            return result != 0;
        }

        public bool UpdateProgramNameByID(int programID, string name)
        {
            int result;
            string comm = "update Program set prName='" + name + "' where prID=" + programID.ToString();
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            result = com.ExecuteNonQuery();

            return result != 0;
        }

        public bool DeleteProgramByID(int programID)
        { 
            //planID是唯一的.
            int result;
            string comm = "delete from Program where prID=" + programID.ToString();
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            result = com.ExecuteNonQuery();

            //删除plan后， 还要级联删除plan下的config, house, result.
            DeleteConfigByProgramID(programID);
            DeleteHouseByProgramID(programID);
            DeleteSiteSelectResultByProgramID(programID);

            return result != 0;
        }

        private void DeleteConfigByProgramID(int programID)
        {
            string comm = "delete from Config where prID=" + programID.ToString();
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            com.ExecuteNonQuery();
        }

        private void DeleteHouseByProgramID(int programID)
        {
            string comm = "delete from House where prID=" + programID.ToString();
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            com.ExecuteNonQuery();
        }

        private void DeleteSiteSelectResultByProgramID(int programID)
        {
            string comm = "delete from SiteResult where prID=" + programID.ToString();
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            com.ExecuteNonQuery();
        }

        public bool DeleteConfigByCfID(int cfID)
        {
            string comm = "delete from Config where cfID=" + cfID.ToString();
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            int result = com.ExecuteNonQuery();

            closeConnection(conn);
            return result != 0;
        }

        public bool DeleteConfigByPmId(int pmID)
        {
            string comm = "delete from Config where pmID=" + pmID.ToString();
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            int result = com.ExecuteNonQuery();

            closeConnection(conn);
            return result != 0;
        }

        public bool DeleteSiteSelectResultByPrID(int prID)
        {
            string comm = "delete from SiteResult where prID=" + prID.ToString();
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            int result = com.ExecuteNonQuery();

            closeConnection(conn);
            return result != 0;
        }

        public List<Feature> SelectSiteSelectResultByPrID(int prID)
        {
            List<Feature> feaInfoList = new List<Feature>();
            string comm = "select * from SiteResult where prID=" + prID.ToString();
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            SqlDataReader reader = com.ExecuteReader();
            if (reader.HasRows)
            {
                while (reader.Read())
                {
                    Feature fea = new Feature();
                    feaInfoList.Add(fea);
                }
                closeConnection(conn);
                return feaInfoList;
            }
            else
            {
                closeConnection(conn);
                return null;
            }
        }

        public bool SaveSiteSelectResult(List<Feature> feaInfoList, int prID)
        {
            //先判断该方案之前是否已经存有数据, 如果有, 删除所有该方案数据.
            DeleteSiteSelectResultByPrID(prID);

            string comm;
            SqlConnection conn = openConn();
            SqlCommand com;
            int result;

            foreach (Feature info in feaInfoList)
            {
            }

            closeConnection(conn);
            return true;
        }

        public bool DeleteSiteSelectResultByPmId(int pmID)
        {
            string comm = "delete from SiteResult where pmID=" + pmID.ToString();
            SqlConnection conn = openConn();
            SqlCommand com = new SqlCommand(comm, conn);
            int result = com.ExecuteNonQuery();

            closeConnection(conn);
            return result != 0;
        }
    }
}
