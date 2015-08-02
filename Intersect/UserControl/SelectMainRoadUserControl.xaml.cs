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
        private bool inited = false;
        public bool valid = false;
        public bool dirty = false;
        private List<IPolygon> cachedVillageAreaPolygonList;
        public Intersect.ProgramStepUserControl.OnMapControlMouseDown mapControlMouseDown;

        public ObservableCollection<Village> villageList = null;

        public SelectMainRoadUserControl()
        {
            InitializeComponent();
        }

        public void init(int programID, AxMapControl mc, AxToolbarControl tc)
        {
            inited = true;

            program = new Program();
            program.id = programID;
            program.select();

            mapControl = mc;
            toolbarControl = tc;
            mainRoadList = program.getAllRelatedMainRoad();
            if (mainRoadList == null)
                mainRoadList = new ObservableCollection<MainRoad>();

            foreach (MainRoad mainRoad in mainRoadList)
            {
                GisUtil.DrawPolylineElement(mainRoad.lineElement, mapControl);   
            }

            valid = isValid();
            dirty = false;

            mapControlMouseDown = null;
            MainRoadListBox.ItemsSource = mainRoadList;
        }

        public void reInit()
        {
            inited = false;

            init(program.id, mapControl, toolbarControl);
        }

        public void delete()
        {
            foreach (MainRoad mainRoad in mainRoadList)
            {
                mainRoad.delete();
                GisUtil.ErasePolylineElement(mainRoad.lineElement, mapControl);
            }
        }

        public bool isValid()
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
            if (Ut.checkBindingGroup(bindingGroup))
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public bool isDirty()
        {
            foreach (MainRoad mainRoad in mainRoadList)
            {
                if (mainRoad.needDelete)
                    return true;
            }
            ObservableCollection<MainRoad> tempMainRoadList = program.getAllRelatedMainRoad();
            if (tempMainRoadList == null)
                tempMainRoadList = new ObservableCollection<MainRoad>();
            Ut.M(tempMainRoadList.Count);
            Ut.M(mainRoadList.Count);
            if (tempMainRoadList.Count != mainRoadList.Count)
                return true;
            foreach (MainRoad mainRoad in mainRoadList)
            {
                int mainRoadID = mainRoad.id;
                MainRoad mainRoadCopy = new MainRoad();
                mainRoadCopy.id = mainRoadID;
                mainRoadCopy.select();
                if (!mainRoad.compare(mainRoadCopy))
                    return true;
            }
            return false;
        }

        private void AddMainRoadButtonClick(object sender, RoutedEventArgs e)
        {
            MainRoad mainRoad = new MainRoad();
            mainRoad.programID = program.id;
            mainRoadList.Add(mainRoad);

            //把左栏遮盖, 让用户在右侧画线.
            mapControlMouseDown = delegate(object sender2, IMapControlEvents2_OnMouseDownEvent e2)
            {
                GisUtil.ResetToolbarControl(toolbarControl);
                onMapControlMouseDown();
                return true;
            };
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
                    GisUtil.ErasePolylineElement(mainRoad.lineElement, mapControl);
                    mainRoad.needDelete = true;
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
            ILineElement mainRoadLineElement = new LineElementClass();
            IElement element = mainRoadLineElement as IElement;
            element.Geometry = mainRoadPolyline;
            mainRoadList[mainRoadList.Count - 1].lineElement = mainRoadLineElement;
            mainRoadList[mainRoadList.Count - 1].updatePath();
            GisUtil.DrawPolylineElement(mainRoadLineElement, mapControl);
            mapControlMouseDown = null;
            return true;
        }

        private void MainRoadGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid mainRoadGrid = sender as Grid;
            TextBlock mainRoadIDTextBlock = mainRoadGrid.FindName("MainRoadIDTextBlock") as TextBlock;
            int mainRoadID = Int32.Parse(mainRoadIDTextBlock.Text);
            foreach (MainRoad mainRoad in mainRoadList)
            {
                if (mainRoad.id == mainRoadID)
                {
                    GisUtil.UpdatePolylineElementColor(mainRoad.lineElement, mapControl, 0, 255, 0);
                }
                else
                {
                    GisUtil.RestorePolylineElementColor(mainRoad.lineElement, mapControl);
                }
            }
        }

        private void FinishMainRoadButtonClick(object sender, RoutedEventArgs e)
        {
            if (isValid())
            {
                if (isDirty())
                {
                    if (Ut.C("继续操作会清空之后的数据, 是否继续?"))
                    {
                        dirty = true;
                        valid = true;

                        foreach (MainRoad mainRoad in mainRoadList)
                        {
                            if (mainRoad.needDelete)
                                mainRoad.delete();
                            else
                                mainRoad.saveOrUpdate();
                        }

                        //1. 靠主路生成区域.
                        GisUtil.CreateShapefile(C.PROGRAM_FOLDER, SiteSelectorUserControl.MAINROAD_LIST_SHP_NAME, mapControl.SpatialReference, "polyline");
                        List<IGeometry> mainRoadGeometryList = new List<IGeometry>();
                        foreach (MainRoad mainRoad in mainRoadList)
                        {
                            IElement element = mainRoad.lineElement as IElement;
                            mainRoadGeometryList.Add(element.Geometry);
                        }
                        GisUtil.AddGeometryListToShpFile(mainRoadGeometryList, C.PROGRAM_FOLDER, SiteSelectorUserControl.MAINROAD_LIST_SHP_NAME);
                        cachedVillageAreaPolygonList = GisUtil.GetPolygonListFromPolylineList(
                            Ut.MakePath(C.PROGRAM_FOLDER, SiteSelectorUserControl.MAINROAD_LIST_SHP_NAME),
                            Ut.MakePath(C.PROGRAM_FOLDER, SiteSelectorUserControl.VILLAGE_AREA_SHP_NAME));
                        foreach (IPolygon polygon in cachedVillageAreaPolygonList)
                        {
                            GisUtil.drawPolygon(polygon, mapControl);
                        }

                        //2. 检查生成的区域是否符合标准.
                        bool errorFlag = false;
                        if (cachedVillageAreaPolygonList.Count == 0)
                            errorFlag = true;
                        foreach (IPolygon polygon in cachedVillageAreaPolygonList)
                        {
                            IArea area = polygon as IArea;
                            if (area.Area > Village.VILLAGE_MAX_SIZE)
                            {
                                errorFlag = true;
                                break;
                            }
                        }
                        if (errorFlag)
                            Ut.M("生成的图形中包含错误.");

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

                            InnerRoad innerRoad = new InnerRoad();
                            innerRoad.programID = program.id;
                            innerRoad.villageID = village.id;
                            village.innerRoad = innerRoad;
                            villageList.Add(village);
                        }
                    }
                    else
                    {
                        return;
                    }
                }
                else
                {
                    valid = true;
                    return;
                }
            }
            else
            {
                Ut.M("请完整填写信息");
                return;
            }
        }
    }
}
