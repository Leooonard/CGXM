using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;

namespace Intersect
{
    public class Feature : DataBase
    {
        private int fID;
        public int id
        {
            get
            {
                return fID;
            }
            set
            {
                fID = value;
                onPropertyChanged("id");
            }
        }

        private int fInUse;
        public int inUse
        {
            get
            {
                return fInUse;
            }
            set
            {
                fInUse = value;
                onPropertyChanged("inUse");
            }
        }

        private double fScore;
        public double score
        {
            get
            {
                return fScore;
            }
            set
            {
                fScore = value;
                onPropertyChanged("score");
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

        public Feature()
        {
            fID = C.ERROR_INT;
            fInUse = C.ERROR_INT;
            fScore = C.ERROR_DOUBLE;
            prID = C.ERROR_INT;
        }

        private void InitBySqlDataReader(SqlDataReader reader)
        {
            id = Int32.Parse(reader[0].ToString());
            inUse = Int32.Parse(reader[1].ToString());
            score = Double.Parse(reader[2].ToString());
            programID = Int32.Parse(reader[3].ToString());
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("id") && fID == C.ERROR_INT)
                return C.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("inUse") && fInUse == C.ERROR_INT)
                return C.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("score") && fScore == C.ERROR_DOUBLE)
                return C.INNER_ERROR_TIP;
            if (!shieldVariableList.Contains("programID") && prID == C.ERROR_INT)
                return C.INNER_ERROR_TIP;
            return "";
        }

        protected override bool isValid(System.Collections.Generic.List<string> shieldVariableList = null)
        {
            if (shieldVariableList == null)
                shieldVariableList = new List<string>();
            if (!shieldVariableList.Contains("fID") && fID == C.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("fInUse") && fInUse == C.ERROR_INT)
                return false;
            if (!shieldVariableList.Contains("fScore") && fScore == C.ERROR_DOUBLE)
                return false;
            if (!shieldVariableList.Contains("prID") && prID == C.ERROR_INT)
                return false;
            return true;
        }

        public override bool save()
        {
            if (!isValid(new List<string>() { "fID"}))
                return false;
            string sqlCommand = String.Format("insert into Feature (fInUse,fScore,prID) values ({0},{1},{2})", fInUse, fScore, prID);
            Sql sql = new Sql();
            return sql.insertFeature(sqlCommand);
        }

        public override bool update()
        {
            if (!isValid())
                return false;
            string sqlCommand = String.Format("update Feature set fInUse={0},fScore={1},prID={2} where fID={3}", fInUse, fScore, prID, fID);
            Sql sql = new Sql();
            return sql.updateFeature(sqlCommand);
        }

        public override bool delete()
        {
            if (!isValid(new List<string>() { "fInUse", "fScore", "prID"}))
                return false;
            string sqlCommand = String.Format("delete from Feature where fID={0}", fID);
            Sql sql = new Sql();
            return sql.deleteFeature(sqlCommand);
        }

        public override bool select()
        {
            if (!isValid(new List<string>() { "fInUse", "fScore", "prID" }))
                return false;
            string sqlCommand = String.Format("select * from Feature where fID={0}", fID);
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectFeature(sqlCommand);
            reader.Read();
            InitBySqlDataReader(reader);
            sql.closeConnection();
            return true;
        }
    }
}
