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
using ESRI.ArcGIS.Controls;
using System.Collections.ObjectModel;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;

namespace Intersect
{
    /// <summary>
    /// SelectInnerRoadUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class SelectInnerRoadUserControl : UserControl
    {
        private AxMapControl mapControl;
        private AxToolbarControl toolbarControl;
        private ObservableCollection<InnerRoad> innerRoadList;
        private Program program;
        private MainWindow mainWindow;
        private bool inited = false;

        public bool valid = false;
        public bool dirty = false;
        public Intersect.ProgramStepUserControl.OnMapControlMouseDown mapControlMouseDown;

        public SelectInnerRoadUserControl()
        {
            InitializeComponent();
        }

        public void init(int programID, AxMapControl mc, AxToolbarControl tc, MainWindow mw)
        {
            if (inited)
                return;
            inited = true;

            program = new Program();
            program.id = programID;
            program.select();

            mapControl = mc;
            toolbarControl = tc;
            mainWindow = mw;

            innerRoadList = program.getAllRelatedInnerRoad();
            if (innerRoadList == null)
            {
                innerRoadList = new ObservableCollection<InnerRoad>();
                ObservableCollection<Village> villageList = program.getAllRelatedVillage();
                foreach (Village village in villageList)
                {
                    if (village.inUse)
                    {
                        InnerRoad innerRoad = new InnerRoad();
                        innerRoad.programID = program.id;
                        innerRoad.villageID = village.id;
                        innerRoadList.Add(innerRoad);
                    }
                }
            }
            else
            {
                foreach (InnerRoad innerRoad in innerRoadList)
                {
                    GisUtil.DrawPolylineElement(innerRoad.lineElement, mapControl);
                }
            }

            valid = isValid();
            dirty = false;

            mapControlMouseDown = null;
            InnerRoadListBox.ItemsSource = innerRoadList;
        }

        public bool isValid()
        {
            foreach (InnerRoad innerRoad in innerRoadList)
            {
                if (innerRoad.checkValid(new List<string>() { "irID"}) != "")
                    return false;
            }

            BindingGroup bindingGroup = SelectInnerRoadStackPanel.BindingGroup;
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
            foreach (InnerRoad innerRoad in innerRoadList)
            {
                int innerRoadID = innerRoad.id;
                InnerRoad innerRoadCopy = new InnerRoad();
                innerRoadCopy.id = innerRoadID;
                innerRoadCopy.select();
                if (!innerRoad.compare(innerRoadCopy))
                {
                    return true;
                }
            }
            return false;
        }

        public void reInit()
        {
            inited = false;

            foreach (InnerRoad innerRoad in innerRoadList)
            {
                innerRoad.delete();
                GisUtil.ErasePolylineElement(innerRoad.lineElement, mapControl);
            }

            init(program.id, mapControl, toolbarControl, mainWindow);
        }

        private void InnerRoadRedrawButtonClick(object sender, RoutedEventArgs e)
        {
            Button innerRoadRedrawButton = sender as Button;
            Grid innerRoadGrid = innerRoadRedrawButton.Parent as Grid;
            TextBlock innerRoadIDTextBlock = innerRoadGrid.FindName("InnerRoadIDTextBlock") as TextBlock;
            int innerRoadID = Int32.Parse(innerRoadIDTextBlock.Text);
            mainWindow.mask();
            foreach (InnerRoad innerRoad in innerRoadList)
            {
                if (innerRoad.id == innerRoadID)
                {
                    if(innerRoad.lineElement != null)
                        GisUtil.ErasePolylineElement(innerRoad.lineElement, mapControl);
                    mapControlMouseDown = delegate(object sender2, IMapControlEvents2_OnMouseDownEvent e2)
                    {
                        onMapControlMouseDown(sender2, e2, innerRoad);
                        mapControlMouseDown = null;
                        return true;
                    };
                }
            }
        }

        public void onMapControlMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e, InnerRoad innerRoad)
        {
            IPolyline innerRoadPolyline = mapControl.TrackLine() as IPolyline;
            //这里是不是要对画好的线做一下检查, 比如保证内部路穿过了小区区域.

            ILineElement innerRoadLineElement = new LineElementClass();
            IElement element = innerRoadLineElement as IElement;
            element.Geometry = innerRoadPolyline;
            innerRoad.lineElement = innerRoadLineElement;
            innerRoad.updatePath();
            GisUtil.DrawPolylineElement(innerRoadLineElement, mapControl);
            mainWindow.unmask();
        }

        private void InnerRoadGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid innerRoadGrid = sender as Grid;
            TextBlock innerRoadIDTextBlock = innerRoadGrid.FindName("InnerRoadIDTextBlock") as TextBlock;
            int innerRoadID = Int32.Parse(innerRoadIDTextBlock.Text);
            foreach (InnerRoad innerRoad in innerRoadList)
            {
                if (innerRoad.id == innerRoadID)
                {
                    if(innerRoad.lineElement != null)
                        GisUtil.UpdatePolylineElementColor(innerRoad.lineElement, mapControl, 0, 255, 0);
                }
                else
                {
                    if(innerRoad.lineElement != null)
                        GisUtil.RestorePolylineElementColor(innerRoad.lineElement, mapControl);
                }
            }
        }
    }
}
