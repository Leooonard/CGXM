using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections.ObjectModel;

namespace Intersect
{
    public class Label : DataBase
    {
        private const int MAX_LCONTENT_LENGTH = 50;
        private const int MAX_LMAPLAYERNAME_LENGTH = 50;
        private const int MAX_LMAPLAYERPATH_LENGTH = 300;
        public const string UNUSE_CHOOSEABLE_CITY_PLAN_STANDARD_INFO_HEAD = "unuse_";

        private int lID;
        public int id
        {
            get
            {
                return lID;
            }
            set
            {
                lID = value;
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
                onPropertyChanged("pID");
            }
        }
        private string lContent;
        public string content
        {
            get
            {
                return lContent;
            }
            set
            {
                lContent = value;
                onPropertyChanged("content");
            }
        }

        private string lMapLayerName;
        public string mapLayerName
        {
            get
            {
                return lMapLayerName;
            }
            set
            {
                lMapLayerName = value;
                onPropertyChanged("mapLayerName");
            }
        }
        private bool lIsChoosed;
        public bool isChoosed
        {
            get
            {
                return lIsChoosed;
            }
            set
            {
                lIsChoosed = value;
                onPropertyChanged("isChoosed");
            }
        }

        private int lType;
        public int type
        {
            get
            {
                return lType;
            }
            set
            {
                lType = value;
                onPropertyChanged("type");
            }
        }

        private bool lisRaster;
        public bool isRaster
        {
            get
            {
                return lisRaster;
            }
            set
            {
                lisRaster = value;
                onPropertyChanged("isRaster");
            }
        }

        public UncompleteLabelComboBoxManager uncomleteLabelContentManager
        {
            get;
            set;
        }

        public Label()
        {
            lID = Const.ERROR_INT;
            pID = Const.ERROR_INT;
            lContent = Const.ERROR_STRING;
            lMapLayerName = Const.ERROR_STRING;
            lIsChoosed = Const.ERROR_BOOL;
            lType = 1;
            lisRaster = Const.ERROR_BOOL;
            uncomleteLabelContentManager = new UncompleteLabelComboBoxManager();
        }

        public void initBySqlDataReader(SqlDataReader reader)
        {
            reader.Read();
            lID = Int32.Parse(reader[0].ToString());
            pID = Int32.Parse(reader[1].ToString());
            lContent = reader[2].ToString();
            lMapLayerName = reader[3].ToString();
            lIsChoosed = Boolean.Parse(reader[4].ToString());
            lType = Int32.Parse(reader[5].ToString());
            lisRaster = Boolean.Parse(reader[6].ToString());
            uncomleteLabelContentManager = new UncompleteLabelComboBoxManager();
        }

        public static Label GetDefaultLabel()
        {
            Label label = new Label();
            return label;
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("id") && lID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("projectID") && pID == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("content") && lIsChoosed && (lContent.Length == 0 || lContent.Length > MAX_LCONTENT_LENGTH))
                return String.Format("类别名不能为空, 或长度大于{0}个字。", MAX_LCONTENT_LENGTH);
            if (!shieldVariableList.Contains("mapLayerName") && (lMapLayerName.Length == 0 || lMapLayerName.Length > MAX_LMAPLAYERNAME_LENGTH))
                return "关联地图图层为空";
            if (!shieldVariableList.Contains("type") && lType == Const.ERROR_INT)
                return Const.INNER_ERROR_TIP;
            return "";
        }

        protected override bool isValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("lID") && lID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("pID") && pID == Const.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("lContent") && lIsChoosed && (lContent.Length == 0 || lContent.Length > MAX_LCONTENT_LENGTH))
                return false;
            if (!shieldVariableList.Contains("lMapLayerName") && (lMapLayerName.Length == 0 || lMapLayerName.Length > MAX_LMAPLAYERNAME_LENGTH))
                return false;
            if (!shieldVariableList.Contains("lType") && lType == Const.ERROR_INT)
                return false;
            return true;
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "lID"}))
                return false;
            string sqlCommand = String.Format(@"insert into Label (pID,lContent,lMapLayerName,lIsChoosed,lType, lisRaster) values ({0}, '{1}', '{2}', {3}, {4}, {5})"
                , pID, lContent, lMapLayerName, lIsChoosed ? 1 : 0, lType, lisRaster ? 1 : 0);
            Sql sql = new Sql();
            return sql.insertLabel(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format(@"update Label set pID={0},lContent='{1}',lMapLayerName='{2}',lIsChoosed={3}, lType='{5}', lisRaster={6} where lID={7}"
                , pID, lContent, lMapLayerName, lIsChoosed ? 1 : 0, lType, lisRaster ? 1 : 0, lID);
            Sql sql = new Sql();
            return sql.updateLabel(sqlCommand);
        }

        public override bool delete()
        {
            if (!isValid(new List<string>() { "pID", "lContent", "lMapLayerName", "lIsChoosed", "lType", "lisRaster"}))
                return false;
            string sqlCommand = String.Format(@"delete from Label where lID={0}", lID);
            Sql sql = new Sql();
            return sql.deleteLabel(sqlCommand);
        }

        public override bool select()
        {
            if (!isValid(new List<string>() { "pID", "lContent", "lMapLayerName", "lIsChoosed", "lType", "lisRaster"}))
                return false;
            string sqlCommand = String.Format(@"select * from Label where lID={0}", lID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectLabel(sqlCommand);
            this.initBySqlDataReader(reader);
            sql.closeConnection();
            return true;
        }

        public static Label GetLabelByMapLayerName(string mapLayerName)
        {
            string sqlCommand = String.Format(@"select * from Label where lMapLayerName='{0}'", mapLayerName);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectLabelByMapLayerName(sqlCommand);
            Label label = new Label();
            label.initBySqlDataReader(reader);
            sql.closeConnection();
            return label;
        }
    }
}
