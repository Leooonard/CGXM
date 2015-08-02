using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Data.SqlClient;
using System.Threading;

namespace Intersect
{
    abstract public class DataBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void onPropertyChanged(string value)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(value));
            }
        }

        private bool _needDelete = false; //为true表示该对象需要被删除.
        public bool needDelete
        {
            get
            {
                return _needDelete;
            }
            set
            {
                _needDelete = value;
                onPropertyChanged("needDelete");
            }
        }

        //如果数据出现问题, 返回null.
        abstract public string checkValid(List<string> shieldVariableList = null);
        abstract protected bool isValid(List<string> shieldVariableList = null);
        abstract public bool save();
        abstract public bool update();
        abstract public bool delete();
        abstract public bool select();
    }
}
