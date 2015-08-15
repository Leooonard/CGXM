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

namespace Intersect
{
    /// <summary>
    /// SiteSelectorUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class SiteSelectorUserControl : UserControl
    {
        private bool inited = false;
        private Intersect.ProgramStepUserControl.OnFinish onFinish;
        private AxMapControl mapControl;
        private AxToolbarControl toolbarControl;
        private MainWindow mainWindow;
        public Intersect.ProgramStepUserControl.OnMapControlMouseDown mapControlMouseDown;

        public bool valid
        {
            get
            {
                return isValid();
            }
            set
            {
                ;
            }
        }
        public bool dirty
        {
            get
            {
                return isDirty();
            }
            set
            {
                SelectMainRoadUserControl.dirty = value;
                SelectVillageUserControl.dirty = value;
            }
        }

        public const string MAINROAD_LIST_SHP_NAME = "MainRoadList.shp";
        public const string VILLAGE_AREA_SHP_NAME = "VillageArea.shp";

        public SiteSelectorUserControl()
        {
            InitializeComponent();
        }

        public bool isValid()
        {
            return SelectMainRoadUserControl.valid && SelectVillageUserControl.valid;
        }

        public bool isDirty()
        {
            return SelectMainRoadUserControl.dirty || SelectVillageUserControl.dirty;
        }

        public void delete()
        {
            SelectMainRoadUserControl.delete();
            SelectVillageUserControl.delete();
        }

        public void reInit()
        {
            SelectMainRoadUserControl.reInit();
            SelectVillageUserControl.reInit();
            if (valid)
                onFinish(true);
        }

        public void init(int programID, Intersect.ProgramStepUserControl.OnFinish of, AxMapControl mc, AxToolbarControl tc, MainWindow mainWindow)
        {
            inited = true;
            onFinish = of;

            Pager.nowStep = 1;
            Pager.totalStep = 2;
            Pager.update();

            Pager.nextStepButtonCheck = nextStepCheckValid;
            Pager.nextStepButtonClick += new EventHandler(nextStepClick);
            Pager.previewStepButtonClick += new EventHandler(previewStepClick);

            mapControl = mc;
            toolbarControl = tc;
            mapControlMouseDown = onMapControlMouseDown;
            SelectMainRoadUserControl.init(programID, mapControl, toolbarControl, mainWindow);
            SelectVillageUserControl.init(programID, mapControl, toolbarControl, of, mainWindow);

            Thread t = new Thread(delegate()
            {
                System.Threading.Thread.Sleep(500);
                Dispatcher.BeginInvoke((ThreadStart)delegate()
                {
                    if (isValid())
                    {
                        onFinish(true);
                    }
                });
            });
            t.Start();
        }

        private bool onMapControlMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            bool handled = false;
            if (SelectMainRoadUserControl.mapControlMouseDown != null)
            {
                handled = SelectMainRoadUserControl.mapControlMouseDown(sender, e);
                if (handled)
                    return true;
            }
            if (SelectVillageUserControl.mapControlMouseDown != null)
            {
                handled = SelectVillageUserControl.mapControlMouseDown(sender, e);
                if (handled)
                    return true;
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
                    if (SelectMainRoadUserControl.dirty)
                    {
                        SelectVillageUserControl.delete();
                        SelectVillageUserControl.reInit();
                        SelectMainRoadUserControl.dirty = false;
                        SelectVillageUserControl.valid = false;
                        SelectVillageUserControl.mergeTempVillage(SelectMainRoadUserControl.villageList);
                        onFinish(false);
                    }
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
                    if (!SelectMainRoadUserControl.valid)
                    {
                        Ut.M("请完整填写主路信息.");
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
