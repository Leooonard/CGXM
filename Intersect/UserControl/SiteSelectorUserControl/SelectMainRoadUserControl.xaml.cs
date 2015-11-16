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
using ESRI.ArcGIS.Display;
using Intersect.Lib;

namespace Intersect
{
    /// <summary>
    /// SelectMainRoadUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class SelectMainRoadUserControl : UserControl
    {
        private const int MIN_MAINROAD_COUNT = 2;

        private Program program;
        private ObservableCollection<MainRoad> mainRoadList;
        private AxMapControl mapControl;
        private AxToolbarControl toolbarControl;
        private List<IPolygon> cachedVillageAreaPolygonList;
        public Intersect.ProgramStepUserControl.OnMapControlMouseDown mapControlMouseDown;
        public bool finish;

        public ObservableCollection<Village> villageList = null;

        public SelectMainRoadUserControl()
        {
            InitializeComponent();
        }

        public void init(int programID, AxMapControl mc, AxToolbarControl tc)
        {
            program = new Program();
            program.id = programID;
            program.select();

            mapControl = mc;
            toolbarControl = tc;

            mainRoadList = program.getAllRelatedMainRoad();
            if (mainRoadList == null)
            {
                mainRoadList = new ObservableCollection<MainRoad>();
            }

            foreach (MainRoad mainRoad in mainRoadList)
            {
                GisTool.DrawPolylineElement(mainRoad.lineElement, mapControl);   
            }

            finish = isValid();

            mapControlMouseDown = null;
            MainRoadListBox.ItemsSource = mainRoadList;
        }

        public void unInit()
        { 
            
        }

        public bool isFinish()
        {
            return finish;
        }

        private bool isValid()
        {
            if (mainRoadList.Count < MIN_MAINROAD_COUNT)
            {
                return false;
            }

            foreach (MainRoad mainRoad in mainRoadList)
            {
                if (mainRoad.checkValid(new List<string>() { "id"}) != "")
                    return false;
            }

            BindingGroup bindingGroup = SelectMainRoadStackPanel.BindingGroup;
            if (Tool.checkBindingGroup(bindingGroup))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private void AddMainRoadButtonClick(object sender, RoutedEventArgs e)
        {
            MainRoad mainRoad = new MainRoad();
            mainRoad.programID = program.id;
            mainRoad.saveWithoutCheck();
            mainRoad.id = MainRoad.GetLastMainRoadID();
            mainRoad.name = "主路#" + mainRoad.id.ToString();
            mainRoadList.Add(mainRoad);

            //把左栏遮盖, 让用户在右侧画线.
            NotificationHelper.Trigger("mask");
            mapControlMouseDown = delegate(object sender2, IMapControlEvents2_OnMouseDownEvent e2)
            {
                GisTool.ResetToolbarControl(toolbarControl);
                onMapControlMouseDown();
                return true;
            };
        }

        public void delete()
        {
            for (int i = 0; i < mainRoadList.Count; i++)
            {
                MainRoad mainRoad = mainRoadList[i];
                GisTool.ErasePolylineElement(mainRoad.lineElement, mapControl);
                mainRoad.delete();
            }
        }

        private void DeleteMainRoadButtonClick(object sender, RoutedEventArgs e)
        {
            Button deleteMainRoadButton = sender as Button;
            Grid grid = deleteMainRoadButton.Parent as Grid;
            TextBlock mainRoadIDTextBlock = grid.FindName("MainRoadIDTextBlock") as TextBlock;
            int mrID = Int32.Parse(mainRoadIDTextBlock.Text);
            for (int i = 0; i < mainRoadList.Count; i++)
            {
                MainRoad mainRoad = mainRoadList[i];
                if (mainRoad.id == mrID)
                {
                    GisTool.ErasePolylineElement(mainRoad.lineElement, mapControl);
                    mainRoad.delete();
                    mainRoadList.Remove(mainRoad);
                    return;
                }
            }
        }

        public ObservableCollection<MainRoad> getMainRoadList()
        {
            return mainRoadList;
        }

        public bool onMapControlMouseDown()
        {
            IPolyline mainRoadPolyline = mapControl.TrackLine() as IPolyline;
            if (mainRoadPolyline == null)
            {
                Tool.M("画主路出现错误，请重画。");
                return onMapControlMouseDown();
            }
            ILineElement mainRoadLineElement = new LineElementClass();
            IElement element = mainRoadLineElement as IElement;
            element.Geometry = mainRoadPolyline;
            mainRoadList[mainRoadList.Count - 1].lineElement = mainRoadLineElement;
            mainRoadList[mainRoadList.Count - 1].updatePath();
            GisTool.DrawPolylineElement(mainRoadLineElement, mapControl);
            mapControlMouseDown = null;
            NotificationHelper.Trigger("unmask");
            return true;
        }

        private void MainRoadGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid mainRoadGrid = sender as Grid;
            TextBlock mainRoadIDTextBlock = mainRoadGrid.FindName("MainRoadIDTextBlock") as TextBlock;
            int mainRoadID = Int32.Parse(mainRoadIDTextBlock.Text);
            foreach (MainRoad mainRoad in mainRoadList)
            {
                if (mainRoad.lineElement == null)
                {
                    continue;
                }
                if (mainRoad.id == mainRoadID)
                {
                    GisTool.UpdatePolylineElementColor(mainRoad.lineElement, mapControl, 0, 255, 0);
                }
                else
                {
                    GisTool.RestorePolylineElementColor(mainRoad.lineElement, mapControl);
                }
            }
        }

        private void FinishMainRoadButtonClick(object sender, RoutedEventArgs e)
        {
            if (!isValid())
            {
                Tool.M("主路没有构成完整区域，请重试。");
                return;
            }
            if(!Tool.C("继续操作会清空之后的数据, 是否继续?"))
            {
                return;
            }

            finish = true;

            foreach (MainRoad mainRoad in mainRoadList)
            {
                mainRoad.update();
            }

            //1. 靠主路生成区域.
            GisTool.CreateShapefile(program.path, SiteSelectorUserControl.MAINROAD_LIST_SHP_NAME, mapControl.SpatialReference, "polyline");
            List<IGeometry> mainRoadGeometryList = new List<IGeometry>();
            foreach (MainRoad mainRoad in mainRoadList)
            {
                IElement element = mainRoad.lineElement as IElement;
                mainRoadGeometryList.Add(element.Geometry);
            }
            GisTool.AddGeometryListToShpFile(mainRoadGeometryList, program.path, SiteSelectorUserControl.MAINROAD_LIST_SHP_NAME);
            cachedVillageAreaPolygonList = GisTool.GetPolygonListFromPolylineList(
                System.IO.Path.Combine(program.path, SiteSelectorUserControl.MAINROAD_LIST_SHP_NAME),
                System.IO.Path.Combine(program.path, SiteSelectorUserControl.VILLAGE_AREA_SHP_NAME));
            if (cachedVillageAreaPolygonList == null)
            {
                Tool.M("生成区域失败，请重试。");
                finish = false;

                GisTool.DeleteShapeFile(System.IO.Path.Combine(program.path, SiteSelectorUserControl.MAINROAD_LIST_SHP_NAME));
                GisTool.DeleteShapeFile(System.IO.Path.Combine(program.path, SiteSelectorUserControl.VILLAGE_AREA_SHP_NAME));

                return;
            }


            //2. 检查生成的区域是否符合标准.
            foreach (IPolygon polygon in cachedVillageAreaPolygonList)
            {
                IArea area = polygon as IArea;
                if (area.Area > Village.VILLAGE_MAX_SIZE)
                {
                    Tool.M("生成的区域中包含过大区域。");
                    break;
                }
            }

            //3. 区域形成village对象和相应的内部路对象.
            villageList = new ObservableCollection<Village>();
            foreach (IPolygon polygon in cachedVillageAreaPolygonList)
            {
                Village village = new Village();
                village.programID = program.id;
                IPolygonElement polygonElement = new PolygonElementClass();
                IElement element = polygonElement as IElement;
                element.Geometry = polygon;
                village.polygonElement = polygonElement;
                village.updateBoundary();
                village.inUse = false;
                village.saveWithoutCheck();
                village.id = Village.GetLastVillageID();
                village.name = "区域#" + Village.GetLastVillageID().ToString();
                village.update();

                InnerRoad innerRoad = new InnerRoad();
                innerRoad.programID = program.id;
                innerRoad.villageID = village.id;
                innerRoad.saveWithoutCheck();
                innerRoad.id = InnerRoad.GetLastInnerRoadID();
                innerRoad.name = String.Format(@"内部路#{0}", innerRoad.id);
                innerRoad.update();

                village.innerRoad = innerRoad;
                villageList.Add(village);
            }

            //4. 删除中间文件.
            GisTool.DeleteShapeFile(System.IO.Path.Combine(program.path, SiteSelectorUserControl.MAINROAD_LIST_SHP_NAME));
            GisTool.DeleteShapeFile(System.IO.Path.Combine(program.path, SiteSelectorUserControl.VILLAGE_AREA_SHP_NAME));

            //5. 更新后续数据.
            NotificationHelper.Trigger("SelectMainRoadUserControlRefresh");
        }
    }
}
