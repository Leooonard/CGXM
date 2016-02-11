using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace Intersect
{
    public class HouseDecoration :　DataBase
    {
        private string dName;
        public string decorationName
        {
            get
            {
                return dName;
            }
            set
            {
                dName = value;
                onPropertyChanged("decorationName");
            }
        }

        private string dValue;
        public string decorationValue
        {
            get
            {
                return dValue;
            }
            set
            {
                dValue = value;
                onPropertyChanged("decorationValue");
            }
        }

        private string dPath;
        public string decorationPath
        {
            get
            {
                return dPath;
            }
            set
            {
                dPath = value;
                onPropertyChanged("decorationPath");
            }
        }

        public HouseDecoration(string name, string value, string path)
        {
            dName = name;
            dValue = value;
            dPath = Path.GetFullPath(path);
        }

        public override string checkValid(List<string> shieldVariableList = null)
        {
            return null;
        }

        protected override bool isValid(List<string> shieldVariableList = null)
        {
            return false;
        }

        public override bool save()
        {
            return false;
        }

        public override bool update()
        {
            return false;
        }

        public override bool delete()
        {
            return false;
        }

        public override bool select()
        {
            return false;
        }
    }
}
