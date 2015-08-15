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
    /// SelectVillageUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class SelectVillageUserControl : UserControl
    {
        private int MIN_VILLAGE_COUNT = 1;
        private Program program;
        private ObservableCollection<Village> villageList;
        private bool inited;
        private AxMapControl mapControl;
        private AxToolbarControl toolbarControl;
        private VillageColorRandomer villageColorRandomer;
        private MainWindow mainWindow;

        public Intersect.ProgramStepUserControl.OnMapControlMouseDown mapControlMouseDown;
        public bool valid = false;
        public bool dirty = false;

        public Intersect.ProgramStepUserControl.OnFinish onFinish;

        public SelectVillageUserControl()
        {
            InitializeComponent();
        }

        public void init(int programID, AxMapControl mc, AxToolbarControl tc, Intersect.ProgramStepUserControl.OnFinish of, MainWindow mw)
        {
            inited = true;

            villageColorRandomer = new VillageColorRandomer();
            program = new Program();
            program.id = programID;
            program.select();
            villageList = program.getAllRelatedVillage();
            if (villageList == null)
            {
                villageList = new ObservableCollection<Village>();
            }
            else
            {
                foreach (Village village in villageList)
                {
                    village.polygonElementColorString = villageColorRandomer.randomColor();
                    InnerRoad innerRoad = village.getRelatedInnerRoad();
                    village.innerRoad = innerRoad;
                }
            }

            mapControl = mc;
            toolbarControl = tc;
            mainWindow = mw;
            foreach (Village village in villageList)
            {
                GisUtil.drawPolygonElement(village.polygonElement, mapControl);
                GisUtil.UpdatePolygonElementColor(village.polygonElement, mapControl
                    , VillageColorRandomer.GetRedFromColorString(village.polygonElementColorString)
                    , VillageColorRandomer.GetGreenFromColorString(village.polygonElementColorString)
                    , VillageColorRandomer.GetBlueFromColorString(village.polygonElementColorString));
                if (village.innerRoad.lineElement != null)
                    GisUtil.DrawPolylineElement(village.innerRoad.lineElement, mapControl);
                if (village.inUse)
                {
                    string reverseColorString = VillageColorRandomer.GetReverseVillageColorString(village.polygonElementColorString);
                    GisUtil.UpdatePolygonElementOutline(village.polygonElement, mapControl
                        , VillageColorRandomer.GetRedFromColorString(reverseColorString)
                        , VillageColorRandomer.GetGreenFromColorString(reverseColorString)
                        , VillageColorRandomer.GetBlueFromColorString(reverseColorString));
                }
            }

            valid = isValid();
            dirty = false;

            onFinish = of;

            mapControlMouseDown = null;
            VillageListBox.ItemsSource = villageList;
        }

        public void delete()
        {
            foreach (Village village in villageList)
            {
                village.delete();
                GisUtil.ErasePolygonElement(village.polygonElement, mapControl);
                //内部路不用删除, 在删除village时, 数据库级联删除.
                if(village.innerRoad.lineElement != null)
                    GisUtil.ErasePolylineElement(village.innerRoad.lineElement, mapControl);
            }
            villageList = new ObservableCollection<Village>();
        }

        public void mergeTempVillage(ObservableCollection<Village> tempVillageList)
        { 
            //把用户新弄得village合进来, 这些village还没有进到数据库中.
            foreach (Village village in tempVillageList)
            {
                village.saveWithoutCheck();
                int villageID = Village.GetLastVillageID();
                village.id = villageID;
                village.innerRoad.villageID = villageID;
                village.innerRoad.saveWithoutCheck();
                village.innerRoad.id = InnerRoad.GetLastInnerRoadID();
                village.polygonElementColorString = villageColorRandomer.randomColor();
                GisUtil.drawPolygonElement(village.polygonElement, mapControl);
                GisUtil.UpdatePolygonElementColor(village.polygonElement, mapControl
                    , VillageColorRandomer.GetRedFromColorString(village.polygonElementColorString)
                    , VillageColorRandomer.GetGreenFromColorString(village.polygonElementColorString)
                    , VillageColorRandomer.GetBlueFromColorString(village.polygonElementColorString));
                if (village.innerRoad.lineElement != null)
                    GisUtil.DrawPolylineElement(village.innerRoad.lineElement, mapControl);
                if (village.inUse)
                {
                    string reverseColorString = VillageColorRandomer.GetReverseVillageColorString(village.polygonElementColorString);
                    GisUtil.UpdatePolygonElementOutline(village.polygonElement, mapControl
                        , VillageColorRandomer.GetRedFromColorString(reverseColorString)
                        , VillageColorRandomer.GetGreenFromColorString(reverseColorString)
                        , VillageColorRandomer.GetBlueFromColorString(reverseColorString));
                }
                villageList.Add(village);
            }
        }

        public bool isValid()
        {
            if (villageList.Count < MIN_VILLAGE_COUNT)
            {
                return false;
            }
            int usedVillageCount = 0;
            foreach (Village village in villageList)
            {
                if (village.inUse)
                    usedVillageCount++;
            }
            if (usedVillageCount == 0)
                return false;

            BindingGroup bindingGroup = SelectVillageStackPanel.BindingGroup;
            if (!Ut.checkBindingGroup(bindingGroup))
            {
                return false;
            }
            foreach (Village village in villageList)
            {
                if (village.inUse)
                {
                    if (village.innerRoad.checkValid() != "")
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        public bool isDirty()
        {
            ObservableCollection<Village> tempVillageList = program.getAllRelatedVillage();
            if (villageList.Count != tempVillageList.Count)
                return true;

            foreach (Village village in villageList)
            {
                int villageID = village.id;
                Village villageCopy = new Village();
                villageCopy.id = villageID;
                villageCopy.select();
                if (!village.compare(villageCopy))
                {
                    return true;
                }

                int innerRoadID = village.innerRoad.id;
                InnerRoad innerRoadCopy = new InnerRoad();
                innerRoadCopy.id = innerRoadID;
                innerRoadCopy.select();
                if (!village.innerRoad.compare(innerRoadCopy))
                {
                    return true;
                }
            }

            return false;
        }

        public void reInit()
        {
            inited = false;

            init(program.id, mapControl, toolbarControl, onFinish, mainWindow);
        }

        private void VillageGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid grid = sender as Grid;
            TextBlock villageIDTextBlock = grid.FindName("VillageIDTextBlock") as TextBlock;
            int villageID = Int32.Parse(villageIDTextBlock.Text);
            foreach (Village village in villageList)
            {
                if (village.id == villageID)
                {
                    GisUtil.UpdatePolygonElementOutline(village.polygonElement, mapControl, 255, 255, 0);
                }
                else
                {
                    GisUtil.RestorePolygonElementOutline(village.polygonElement, mapControl);
                }
            }
            return;
        }

        private void FinishButtonClick(object sender, RoutedEventArgs e)
        {
            if (isValid())
            {
                if (isDirty())
                {
                    if (Ut.C("将改变之后的数据. 是否继续?"))
                    {
                        valid = true;
                        dirty = true;

                        foreach (Village village in villageList)
                        {
                            village.saveOrUpdate();
                            village.innerRoad.saveOrUpdate();
                        }

                        onFinish(true);
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
                Ut.M("请完整填写信息.");
                return;
            }
        }

        private void onMapControlMouseDown(Village village)
        {
            IPolyline innerRoadPolyline = mapControl.TrackLine() as IPolyline;
            //这里是不是要对画好的线做一下检查, 比如保证内部路穿过了小区区域.
            IPolygonElement villagePolygonElement = village.polygonElement;
            IPolygon villagePolygon = (villagePolygonElement as IElement).Geometry as IPolygon;
            IRelationalOperator innerRoadPolylineRelationalOperator = innerRoadPolyline as IRelationalOperator;
            IRelationalOperator villagePolygonRelationOperator = villagePolygon as IRelationalOperator;
            ITopologicalOperator villagePolygonTopologicalOperator = villagePolygon as ITopologicalOperator;
            IPolyline villageBoundaryPolyline = villagePolygonTopologicalOperator.Boundary as IPolyline;
            ITopologicalOperator villageBoundaryPolylineTopologicalOperator = villageBoundaryPolyline as ITopologicalOperator;
            IPointCollection pointCollection = villageBoundaryPolylineTopologicalOperator.Intersect(innerRoadPolyline, esriGeometryDimension.esriGeometry0Dimension) as IPointCollection;
            if (pointCollection.PointCount < 2)
            {
                Ut.M("内部路必须穿过小区");
                return;
            }
            innerRoadPolyline = villagePolygonTopologicalOperator.Intersect(innerRoadPolyline, esriGeometryDimension.esriGeometry1Dimension) as IPolyline;

            if (village.innerRoad.lineElement != null)
            {
                GisUtil.ErasePolylineElement(village.innerRoad.lineElement, mapControl);
            }
            ILineElement innerRoadLineElement = new LineElementClass();
            IElement element = innerRoadLineElement as IElement;
            element.Geometry = innerRoadPolyline;
            village.innerRoad.lineElement = innerRoadLineElement;
            village.innerRoad.updatePath();
            GisUtil.DrawPolylineElement(innerRoadLineElement, mapControl);
            mainWindow.unmask();
        }

        private bool isVillageTooBig(IPolygonElement polygonElement)
        {
            IElement element = polygonElement as IElement;
            IPolygon polygon = element.Geometry as IPolygon;
            IArea area = polygon as IArea;
            return area.Area > Village.VILLAGE_MAX_SIZE;
        }

        private void VillageInUseCheckBoxClick(object sender, RoutedEventArgs e)
        {
            CheckBox checkBox = sender as CheckBox;
            bool isChecked = (bool)checkBox.IsChecked;
            Grid grid = checkBox.Parent as Grid;
            TextBlock villageIDTextBlock = grid.FindName("VillageIDTextBlock") as TextBlock;
            int villageID = Int32.Parse(villageIDTextBlock.Text);
            foreach (Village village in villageList)
            {
                if (village.id == villageID)
                {
                    if (isVillageTooBig(village.polygonElement))
                    {
                        Ut.M("所选区域过大, 无法选择");
                        checkBox.IsChecked = false;
                        e.Handled = true;
                        return;
                    }
                    village.inUse = isChecked;
                    if (isChecked)
                    {
                        string reverseColorString = VillageColorRandomer.GetReverseVillageColorString(village.polygonElementColorString);
                        GisUtil.UpdatePolygonElementOutline(village.polygonElement, mapControl
                            , VillageColorRandomer.GetRedFromColorString(reverseColorString)
                            , VillageColorRandomer.GetGreenFromColorString(reverseColorString)
                            , VillageColorRandomer.GetBlueFromColorString(reverseColorString));
                    }
                    else
                    {
                        GisUtil.RestorePolygonElementOutline(village.polygonElement, mapControl);
                    }
                }
            }
        }

        private void InnerRoadRedrawButtonClick(object sender, RoutedEventArgs e)
        {
            mainWindow.mask();
            Button innerRoadRedrawButton = sender as Button;
            Grid innerRoadGrid = innerRoadRedrawButton.Parent as Grid;
            Grid villageGrid = innerRoadGrid.Parent as Grid;
            TextBlock villageIDTextBlock = villageGrid.FindName("VillageIDTextBlock") as TextBlock;
            int villageID = Int32.Parse(villageIDTextBlock.Text);
            foreach (Village village in villageList)
            {
                if (village.id == villageID)
                {
                    mapControlMouseDown = delegate(object sender2, IMapControlEvents2_OnMouseDownEvent e2)
                    {
                        GisUtil.ResetToolbarControl(toolbarControl);
                        onMapControlMouseDown(village);
                        mapControlMouseDown = null;
                        return true;
                    };
                    return;
                }
            }
        }
    }
}
