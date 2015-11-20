using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Intersect
{
    public class HouseResult : DataBase
    {
        private int hrID;
        public int id
        {
            get
            {
                return hrID;
            }
            set
            {
                hrID = value;
                onPropertyChanged("id");
            }
        }

        private string hName;
        public string houseName
        {
            get
            {
                return hName;
            }
            set
            {
                hName = value;
                onPropertyChanged("houseName");
            }
        }

        private double hrArea;
        public double area
        {
            get
            {
                return hrArea;
            }
            set
            {
                hrArea = value;
                onPropertyChanged("area");
            }
        }

        private int hrCount;
        public int count
        {
            get
            {
                return hrCount;
            }
            set
            {
                hrCount = value;
                onPropertyChanged("count");
            }
        }

        private double hrLandArea;
        public double landArea
        {
            get
            {
                return hrLandArea;
            }
            set
            {
                hrLandArea = value;
                onPropertyChanged("landArea");
            }
        }

        private double hrConstructArea;
        public double constructArea
        {
            get
            {
                return hrConstructArea;
            }
            set
            {
                hrConstructArea = value;
                onPropertyChanged("constructArea");
            }
        }

        private double hrRatio;
        public double ratio
        {
            get
            {
                return hrRatio;
            }
            set
            {
                hrRatio = value;
                onPropertyChanged("ratio");
            }
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
            throw new NotImplementedException();
        }
    }
}
