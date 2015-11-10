using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Data.SqlClient;
using System.Collections.ObjectModel;

namespace Intersect
{
    public class CityPlanStandard : DataBase
    {
        private int cpsID;
        public int id
        {
            get
            {
                return cpsID;
            }
            set
            {
                cpsID = value;
            }
        }
        private string cpsNumber;
        public string number
        {
            get 
            {
                return cpsNumber;
            }
        }
        private string cpsShortDescription;
        public string shortDescription
        {
            get
            {
                return cpsShortDescription;
            }
        }
        private string cpsLongDescription;
        public string longDescription
        {
            get
            {
                return cpsLongDescription;
            }
        }

        public CityPlanStandard()
        {
            cpsID = Const.ERROR_INT;
            cpsNumber = Const.ERROR_STRING;
            cpsShortDescription = Const.ERROR_STRING;
            cpsLongDescription = Const.ERROR_STRING;
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            throw new NotImplementedException();
        }

        protected override bool isValid(List<string> shieldVariableList = null)
        {
            throw new NotImplementedException();
        }

        public override bool save()
        {
            throw new NotImplementedException();
        }

        public override bool update()
        {
            throw new NotImplementedException();
        }

        public override bool delete()
        {
            throw new NotImplementedException();
        }

        public override bool select()
        {
            if (cpsID == Const.ERROR_INT)
                return false;
            string sqlCommand = String.Format("select * from CityPlanStandard where cpsID = {0}", cpsID.ToString());
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectCityPlanStandard(sqlCommand);
            reader.Read();
            cpsID = Int32.Parse(reader[0].ToString());
            cpsNumber = reader[1].ToString();
            cpsShortDescription = reader[2].ToString();
            cpsLongDescription = reader[3].ToString();
            sql.closeConnection();
            return true;
        }

        private static ObservableCollection<CityPlanStandard> allCityPlanStandardList;
        public static ObservableCollection<CityPlanStandard> GetAllCityPlanStandard()
        {
            if (allCityPlanStandardList != null)
                return allCityPlanStandardList;
            allCityPlanStandardList = new ObservableCollection<CityPlanStandard>();
            string sqlCommand = String.Format(@"select cpsID from CityPlanStandard");
            Sql sql = new Sql();
            SqlDataReader reader = sql.selectAllCityPlanStandard(sqlCommand);
            while (reader.Read())
            {
                int cpsID = Int32.Parse(reader[0].ToString());
                CityPlanStandard cityPlanStandard = new CityPlanStandard();
                cityPlanStandard.id = cpsID;
                cityPlanStandard.select();
                allCityPlanStandardList.Add(cityPlanStandard);
            }
            return allCityPlanStandardList;
        }

        public static bool IsCityPlanStandardNumberAndShortDescription(string number, string shortDescription)
        {
            string SqlCommand = String.Format(@"select cpsID from CityPlanStandard where cpsNumber='{0}'", number);
            Sql sql = new Sql();
            int cpsID = sql.selectCityPlanStandardIDByCpsNumber(SqlCommand);
            if (cpsID == Const.ERROR_INT)
                return false;
            CityPlanStandard cityPlanStandard = new CityPlanStandard();
            cityPlanStandard.id = cpsID;
            cityPlanStandard.select();
            return cityPlanStandard.shortDescription == shortDescription;
        }

        public static CityPlanStandard GetCityPlanStandardByNumber(string number)
        {
            string sqlCommand = String.Format(@"select cpsID from CityPlanStandard where cpsNumber='{0}'", number);
            Sql sql = new Sql();
            int cpsID = sql.selectCityPlanStandardIDByCpsNumber(sqlCommand);
            if (cpsID == Const.ERROR_INT)
                return null;
            CityPlanStandard cityPlanStandard = new CityPlanStandard();
            cityPlanStandard.id = cpsID;
            cityPlanStandard.select();
            return cityPlanStandard;
        }

        public string getCityPlanStandardInfo()
        {
            return String.Format("{0}-{1}", number, shortDescription);
        }
    }
}
