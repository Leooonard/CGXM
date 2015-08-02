using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Data.SqlClient;
using System.Windows;
using System.Windows.Input;
using System.Collections.ObjectModel;

namespace Intersect
{
    public class Condition : DataBase
    {
        private int CDNAME_MAX_LENGTH = 20;

        private int cdID;
        public int id
        {
            get
            {
                return cdID;
            }
            set
            {
                cdID = value;
                onPropertyChanged("id");
            }
        }
        private string cdName;
        public string name
        {
            get
            {
                return cdName;
            }
            set
            {
                cdName = value;
                onPropertyChanged("name");
            }
        }
        private int cdType;
        public int type
        {
            get
            {
                return cdType;
            }
            set
            {
                cdType = value;
                onPropertyChanged("type");
            }
        }
        private int cdCategory;
        public int category
        {
            get
            {
                return cdCategory;
            }
            set
            {
                cdCategory = value;
                onPropertyChanged("category");
            }
        }
        private int lID;
        public int labelID
        {
            get
            {
                return lID;
            }
            set
            {
                lID = value;
                onPropertyChanged("labelID");
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
        private double cdValue;
        public double value
        {
            get
            {
                return cdValue;
            }
            set
            {
                cdValue = value;
                onPropertyChanged("value");
            }
        }

        public ObservableCollection<Label> labelList
        {
            get;
            set;
        }
        private int _labelIndex;
        public int labelIndex
        {
            get
            {
                if (_labelIndex == C.ERROR_INT && lID == C.ERROR_INT)
                    return C.ERROR_INT;
                if (_labelIndex != C.ERROR_INT)
                    return _labelIndex;
                for (int i = 0; i < labelList.Count; i++)
                {
                    Label label = labelList[i];
                    if (label.id == lID)
                    {
                        return i;
                    }
                }
                return C.ERROR_INT;
            }
            set 
            {
                _labelIndex = value;
                onPropertyChanged("labelIndex");
            }
        }

        public Condition()
        {
            cdID = C.ERROR_INT;
            cdName = C.ERROR_STRING;
            cdType = C.ERROR_INT;
            cdCategory = C.ERROR_INT;
            labelID = C.ERROR_INT;
            programID = C.ERROR_INT;
            cdValue = C.ERROR_DOUBLE;
            labelList = new ObservableCollection<Label>();
            _labelIndex = C.ERROR_INT;
        }

        public Condition(int programID):this()
        {
            prID = programID;
        }

        public void InitBySqlDataReader(SqlDataReader reader)
        {
            id = Int32.Parse(reader[0].ToString());
            name = reader[1].ToString();
            type = Int32.Parse(reader[2].ToString());
            category = Int32.Parse(reader[3].ToString());
            labelID = Int32.Parse(reader[4].ToString());
            programID = Int32.Parse(reader[5].ToString());
            value = Double.Parse(reader[6].ToString());
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("id") && cdID == C.ERROR_INT)
                return C.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("name") && (cdName.Length == 0 || cdName.Length > CDNAME_MAX_LENGTH))
                return String.Format("条件名长度须在0-{0}之间", CDNAME_MAX_LENGTH);
            if (!shieldVariableList.Contains("type") && cdType == C.ERROR_INT)
                return "条件类型不能为空";
            if (!shieldVariableList.Contains("category") && cdCategory == C.ERROR_INT)
                return "条件种类不能为空";
            if (!shieldVariableList.Contains("labelID") && labelID == C.ERROR_INT)
                return "条件关联图层不能为空";
            if (!shieldVariableList.Contains("programID") && prID == C.ERROR_INT)
                return C.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("value") && cdValue == C.ERROR_DOUBLE)
                return "条件值不能为空";
            return "";
        }

        protected override bool isValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("cdID") && cdID == C.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("cdName") && (cdName.Length == 0 || cdName.Length > CDNAME_MAX_LENGTH))
                return false;
            if (!shieldVariableList.Contains("cdType") && cdType == C.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("cdCategory") && cdCategory == C.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("lID") && lID == C.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("prID") && prID == C.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("cdValue") && cdValue == C.ERROR_DOUBLE)
                return false;
            return true;
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "cdID"}))
                return false;
            string sqlCommand = String.Format("insert into Condition (cdName,cdType,cdCategory,lID,prID,cdValue) values('{0}',{1},{2},{3},{4},{5})"
                , cdName, cdType, cdCategory, labelID, prID, cdValue);
            Sql sql = new Sql();
            return sql.insertCondition(sqlCommand);
        }

        public bool saveWithoutCheck()
        {
            string sqlCommand = String.Format("insert into Condition (cdName,cdType,cdCategory,lID,prID,cdValue) values('{0}',{1},{2},{3},{4},{5})"
                , cdName, cdType, cdCategory, labelID, prID, cdValue);
            Sql sql = new Sql();
            return sql.insertCondition(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format("update Condition set cdName='{0}',cdType={1},cdCategory={2},lID={3},prID={4},cdValue={5} where cdID={6}"
                , cdName, cdType, cdCategory, labelID, prID, cdValue, cdID);
            Sql sql = new Sql();
            return sql.updateCondition(sqlCommand);
        }

        public override bool delete()
        {
            if (cdID == C.ERROR_INT)
                return false;
            string sqlCommand = String.Format("delete from Condition where cdID={0}", cdID);
            Sql sql = new Sql();
            return sql.deleteCondition(sqlCommand);
        }

        public override bool select()
        {
            if (cdID == C.ERROR_INT)
                return false;
            string sqlCommand = String.Format("select * from Condition where cdID={0}", cdID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectCondition(sqlCommand);
            reader.Read();
            InitBySqlDataReader(reader);
            sql.closeConnection();
            return true;
        }

        public bool compare(Condition condition)
        {
            if (cdID != condition.id)
                return false;
            if (cdName != condition.name)
                return false;
            if (cdType != condition.type)
                return false;
            if (cdCategory != condition.category)
                return false;
            if (lID != condition.labelID)
                return false;
            if (prID != condition.programID)
                return false;
            if (cdValue != condition.value)
                return false;
            return true;
        }

        public static int GetLastConditionID()
        {
            string sqlCommand = String.Format("select max(cdID) from Condition");
            Sql sql = new Sql();
            return sql.selectMaxConditionID(sqlCommand);
        }
    }
}
