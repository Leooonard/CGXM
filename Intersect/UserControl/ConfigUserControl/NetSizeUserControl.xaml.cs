using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Intersect
{
    /// <summary>
    /// NetSizeUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class NetSizeUserControl : UserControl
    {
        private NetSize netSize;
        private Program program;

        public NetSizeUserControl()
        {
            InitializeComponent();
        }

        public void save()
        {
            if (netSize == null)
            {
                return;
            }
            if (netSize.id == Const.ERROR_INT)
            {
                netSize.save();
            }
            else
            {
                netSize.update();
            }
        }

        public bool isValid()
        {
            BindingGroup bindingGroup = NetSizeGrid.BindingGroup;
            if (!Tool.checkBindingGroup(bindingGroup))
            {
                return false;
            }
            return true;
        }

        public void init(int programID)
        {
            program = new Program();
            program.id = programID;
            program.select();

            load();
        }

        public void refresh()
        {
            load();
        }

        private void load()
        {
            netSize = program.getRelatedNetSize();
            if (netSize == null)
            {
                netSize = new NetSize();
                netSize.programID = program.id;
                netSize.saveWithoutCheck();
                netSize.id = NetSize.GetLastNetSizeID();
            }

            Tool.bind(netSize, "width", BindingMode.TwoWay, NetSizeWidthTextBox, TextBox.TextProperty
                , new List<ValidationRule>() { new PositiveDoubleValidationRule() }, "NetSizeBindingGroup");
            Tool.bind(netSize, "height", BindingMode.TwoWay, NetSizeHeightTextBox, TextBox.TextProperty
                , new List<ValidationRule>() { new PositiveDoubleValidationRule() }, "NetSizeBindingGroup");
        }
    }
}
