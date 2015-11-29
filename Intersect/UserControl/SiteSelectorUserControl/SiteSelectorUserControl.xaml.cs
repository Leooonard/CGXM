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
using System.Collections.ObjectModel;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using System.Threading;
using Intersect.Lib;

namespace Intersect
{
    /// <summary>
    /// SiteSelectorUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class SiteSelectorUserControl : UserControl
    {
        private AxMapControl mapControl;
        private AxToolbarControl toolbarControl;
        public Intersect.ProgramStepUserControl.OnMapControlMouseDown mapControlMouseDown;

        public const string MAINROAD_LIST_SHP_NAME = "MainRoadList.shp";
        public const string VILLAGE_AREA_SHP_NAME = "VillageArea.shp";

        public SiteSelectorUserControl()
        {
            InitializeComponent();
        }

        public bool isFinish()
        {
            return SelectMainRoadUserControl.isFinish() && SelectVillageUserControl.isFinish();
        }

        public void clear()
        {
            SelectMainRoadUserControl.clear();
            SelectVillageUserControl.clear();
        }

        public void init(int programID, AxMapControl mc, AxToolbarControl tc)
        {
            //放只初始化一次的代码, 比如注册事件.
            NotificationHelper.Register("SelectVillageUserControlRefresh", new NotificationHelper.NotificationEvent(delegate()
            {
                SelectVillageUserControl.clear();
                SelectVillageUserControl.refresh();
                NotificationHelper.Trigger("HousePlacerUserControlRefresh");
            }));

            NotificationHelper.Register("SelectVillageUserControlFinish", new NotificationHelper.NotificationEvent(delegate()
            {
                NotificationHelper.Trigger("SiteSelectorUserControlFinish");
                NotificationHelper.Trigger("HousePlacerUserControlRefresh");
            }));

            Pager.nowStep = 1;
            Pager.totalStep = 2;
            Pager.update();
            SelectMainRoadUserControl.Visibility = System.Windows.Visibility.Visible;
            SelectVillageUserControl.Visibility = System.Windows.Visibility.Collapsed;

            Pager.nextStepButtonCheck = nextStepCheckValid;
            Pager.nextStepButtonClick += new EventHandler(nextStepClick);
            Pager.previewStepButtonClick += new EventHandler(previewStepClick);

            mapControl = mc;
            toolbarControl = tc;
            mapControlMouseDown = onMapControlMouseDown;

            SelectMainRoadUserControl.init(programID, mapControl, toolbarControl);
            SelectVillageUserControl.init(programID, mapControl, toolbarControl);

            if (isFinish())
            {
                NotificationHelper.Trigger("SiteSelectorUserControlFinish");
            }
            else
            {
                NotificationHelper.Trigger("SiteSelectorUserControlUnFinish");                
            }
        }

        public void refresh()
        {
            Pager.nowStep = 1;
            Pager.totalStep = 2;
            Pager.update();
            SelectMainRoadUserControl.Visibility = System.Windows.Visibility.Visible;
            SelectVillageUserControl.Visibility = System.Windows.Visibility.Collapsed;

            SelectMainRoadUserControl.refresh();
            SelectVillageUserControl.refresh();

            if (isFinish())
            {
                NotificationHelper.Trigger("SiteSelectorUserControlFinish");
            }
            else
            {
                NotificationHelper.Trigger("SiteSelectorUserControlUnFinish");                
            }
        }

        private bool onMapControlMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            bool handled = false;
            if (SelectMainRoadUserControl.mapControlMouseDown != null)
            {
                handled = SelectMainRoadUserControl.mapControlMouseDown(sender, e);
                if (handled)
                {
                    return true;
                }
            }
            if (SelectVillageUserControl.mapControlMouseDown != null)
            {
                handled = SelectVillageUserControl.mapControlMouseDown(sender, e);
                if (handled)
                {
                    return true;
                }
            }
            return false;
        }

        private void previewStepClick(object sender, EventArgs e)
        {
            switch (Pager.nowStep)
            {
                case 1:
                    SelectMainRoadUserControl.Visibility = System.Windows.Visibility.Visible;
                    SelectVillageUserControl.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 2:
                    SelectMainRoadUserControl.Visibility = System.Windows.Visibility.Collapsed;
                    SelectVillageUserControl.Visibility = System.Windows.Visibility.Visible;
                    break;
                default:
                    return;
            }
            return;
        }

        private void nextStepClick(object sender, EventArgs e)
        {
            switch (Pager.nowStep)
            { 
                case 1:
                    SelectMainRoadUserControl.Visibility = System.Windows.Visibility.Visible;
                    SelectVillageUserControl.Visibility = System.Windows.Visibility.Collapsed;
                    break;
                case 2:
                    SelectMainRoadUserControl.Visibility = System.Windows.Visibility.Collapsed;
                    SelectVillageUserControl.Visibility = System.Windows.Visibility.Visible;
                    break;
                default:
                    return;
            }
            return;
        }

        private bool nextStepCheckValid()
        {
            switch (Pager.nowStep)
            { 
                case 1:
                    if (!SelectMainRoadUserControl.isFinish())
                    {
                        Tool.M("请先规划主路。");
                        return false;
                    }
                    break;
                case 2:
                    break;
                default:
                    return false;
            }
            return true;
        }

        private void saveMainRoadList()
        {
            
        }
    }
}
