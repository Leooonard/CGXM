using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections.ObjectModel;

namespace Intersect
{
    public class Project : DataBase
    {
        private const int PMNAME_MAX_LENGTH = 50;
        public const int NAME_MAX_LENGTH = PMNAME_MAX_LENGTH;

        private int pID;
        public int id
        {
            get
            {
                return pID; 
            }
            set
            {
                pID = value;
                onPropertyChanged("id");
            }
        }
        private string pName;
        public string name
        {
            get
            {
                return pName;
            }
            set
            {
                pName = value;
                onPropertyChanged("name");
            }
        }
        private string pPath;
        public string path
        {
            get
            {
                return pPath;
            }
            set
            {
                pPath = value;
                onPropertyChanged("path");
            }
        }
        private int pBaseMapIndex;
        public int baseMapIndex
        {
            get
            {
                return pBaseMapIndex;
            }
            set
            {
                pBaseMapIndex = value;
                onPropertyChanged("baseMapIndex");
            }
        }

        public Project()
        {
            pID = Const.ERROR_INT;
            pName = Const.ERROR_STRING;
            pPath = Const.ERROR_STRING;
            pBaseMapIndex = Const.ERROR_INT;
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("id") && pID == Const.ERROR_INT)
                return "项目ID为空";
            if (!shieldVariableList.Contains("name") && (pName.Length == 0 || pName.Length > PMNAME_MAX_LENGTH))
                return String.Format("项目名长度须在0-{0}之间", PMNAME_MAX_LENGTH);
            if (!shieldVariableList.Contains("path") && pPath.Length == 0)
                return "项目路径不能为空";
            if (!shieldVariableList.Contains("baseMapIndex") && pBaseMapIndex == Const.ERROR_INT)
                return "项目基础图层不能为空";
            return "";
        }

        protected override bool isValid(System.Collections.Generic.List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("pID") && pID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("pName") && (pName.Length == 0 || pName.Length > PMNAME_MAX_LENGTH))
                return false;
            if (!shieldVariableList.Contains("pPath") && pPath.Length == 0)
                return false;
            if (!shieldVariableList.Contains("pBaseMapIndex") && pBaseMapIndex == Const.ERROR_INT)
                return false;
            return true;
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "pID" }))
                return false;
            string sqlCommand = String.Format("Insert into Project (pName,pPath,pBaseMapIndex) values('{0}','{1}',{2})"
                , pName, pPath, pBaseMapIndex.ToString());
            Sql sql = new Sql();
            return sql.insertProjectAndMap(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format("update Project set pName='{0}',pPath='{1}',pBaseMapIndex={2} where pID={3}"
                , pName, pPath, pBaseMapIndex.ToString(), pID.ToString());
            Sql sql = new Sql();
            return sql.updateProjectAndMap(sqlCommand);
        }

        public override bool delete()
        {
            if (!isValid(new List<string>() { "pName" , "pPath" , "pBaseMapIndex" }))
                return false;
            string sqlCommand = String.Format("delete from Project where pID={0}", pID.ToString());
            Sql sql = new Sql();
            return sql.deleteProjectAndMap(sqlCommand);
        }

        public override bool select()
        {
            if (!isValid(new List<string>() { "pName", "pPath", "pBaseMapIndex" }))
                return false;
            string sqlCommand = String.Format("select * from Project where pID={0}", pID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectProjectAndMap(sqlCommand);
            reader.Read();
            id = Int32.Parse(reader[0].ToString());
            name = reader[1].ToString();
            path = reader[2].ToString();
            baseMapIndex = Int32.Parse(reader[3].ToString());
            sql.closeConnection();
            return true;
        }

        public ObservableCollection<Condition> selectAllRelatedCondition()
        {
            if (!isValid())
                return null;
            ObservableCollection<Condition> conditionList = new ObservableCollection<Condition>();
            string sqlCommand = String.Format("select cdID from Condition where pID = {0}", pID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectConditionIDByPmID(sqlCommand);
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

        public ObservableCollection<Program> getAllRelatedProgram()
        {
            if (!isValid())
                return null;
            ObservableCollection<Program> programList = new ObservableCollection<Program>();
            string sqlCommand = String.Format("select prID from Program where pID = {0}", pID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectProgramIDByPmID(sqlCommand);
            while (reader.Read())
            {
                int prID = Int32.Parse(reader[0].ToString());
                Program program = new Program();
                program.id = prID;
                program.select();
                programList.Add(program);
            }
            sql.closeConnection();
            return programList;
        }

        public ObservableCollection<Label> getAllRelatedLabel()
        {
            if (!isValid())
                return null;
            ObservableCollection<Label> labelList = new ObservableCollection<Label>();
            string sqlCommand = String.Format("select lID from Label where pID = {0}", pID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectAllLabelIDByPID(sqlCommand);
            while (reader.Read())
            {
                int lID = Int32.Parse(reader[0].ToString());
                Label label = new Label();
                label.id = lID;
                label.select();
                labelList.Add(label);
            }
            sql.closeConnection();
            return labelList;
        }

        public static int GetLastProjectID()
        {
            string sqlCommand = String.Format("select max(pID) from Project");
            Sql sql = new Sql();
            return sql.selectMaxProjectAndMapID(sqlCommand);
        }

        public static bool ProjectNameExist(string projectName)
        {
            string sqlCommand = String.Format("select * from Project where pName = '{0}'", projectName);
            Sql sql = new Sql();
            SqlDataReader reader = sql.commonSelectOperation(sqlCommand);
            while(reader.Read())
            {
                return true;
            }
            return false;
        }

        public static ObservableCollection<Project> GetAllProject()
        {
            ObservableCollection<Project> pamList = new ObservableCollection<Project>();
            string sqlCommand = String.Format("select pID from Project");
            Sql sql = new Sql();
            SqlDataReader reader = sql.SelectAllProjectAndMap(sqlCommand);
            while (reader.Read())
            {
                int pmID = Int32.Parse(reader[0].ToString());
                Project pam = new Project();
                pam.pID = pmID;
                pam.select();
                pamList.Add(pam);
            }
            return pamList;
        }
    }
}
