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
using System.Collections;
using ESRI.ArcGIS.Geometry;
using System.Threading;
using Intersect.Lib;
using ESRI.ArcGIS.Carto;
using System.IO;

namespace Intersect
{
    /// <summary>
    /// HousePlacerUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class HousePlacerUserControl : UserControl
    {
        private Program program;
        private ObservableCollection<Village> villageList;
        private AxMapControl mapControl;
        private HouseShowcaseManager houseShowcaseManager;

        private AxMapControl houseMapControl;
        private AxToolbarControl houseToolbarControl;

        public Intersect.ProgramStepUserControl.OnMapControlMouseDown mapControlMouseDown;
        public bool finish = false;

        public HousePlacerUserControl()
        {
            InitializeComponent();
        }

        public void init(int programID, AxMapControl mc)
        {
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
                ObservableCollection<Village> inUseVillageList = new ObservableCollection<Village>();
                foreach (Village village in villageList)
                {
                    if (village.inUse)
                    {
                        inUseVillageList.Add(village);
                        CommonHouse commonHouse = village.getRelatedCommonHouse();
                        if (commonHouse == null)
                        {
                            commonHouse = CommonHouse.GetDefaultCommonHouse();
                            commonHouse.villageID = village.id;
                        }
                        village.commonHouse = commonHouse;
                        ObservableCollection<House> houseList = village.getAllRelatedHouse();
                        if (houseList == null)
                        {
                            houseList = new ObservableCollection<House>();
                        }
                        village.houseList = houseList;
                        village.innerRoad = village.getRelatedInnerRoad();
                    }
                }
                villageList = inUseVillageList;
            }

            mapControl = mc;

            mapControlMouseDown = null;
            HousePlacerListBox.ItemsSource = villageList;

            Thread t = new Thread(delegate()
            {
                System.Threading.Thread.Sleep(500);
                Dispatcher.BeginInvoke((ThreadStart)delegate()
                {
                    foreach (Village village in villageList)
                    {
                        if (File.Exists(System.IO.Path.Combine(program.path, "摆放结果_" + village.name + ".shp")))
                        {
                            PlaceManager placeManager = new PlaceManager(village.commonHouse, new List<House>(village.houseList), mapControl);
                            placeManager.addShapeFile(program.path, "摆放结果_" + village.name + ".shp", "摆放结果_" + village.name);
                        }
                        else
                        {
                            return;
                        }
                    }
                    finish = true;
                    NotificationHelper.Trigger("HousePlacerUserControlFinish");
                });
            });
            t.Start();
        }

        public void unInit()
        { 
            
        }

        public void initAxComponents()
        {
            if (houseMapControl != null)
            {
                return;
            }
            houseMapControl = new AxMapControl();
            houseToolbarControl = new AxToolbarControl();
            HouseMapHost.Child = houseMapControl;
            HouseToolbarHost.Child = houseToolbarControl;
            houseToolbarControl.SetBuddyControl(houseMapControl);
            if (houseToolbarControl.Count == 0)
            {
                houseToolbarControl.AddItem("esriControls.ControlsMapNavigationToolbar");
            }
        }

        public void delete()
        {
            foreach (Village village in villageList)
            {
                PlaceManager placeManager = new PlaceManager(village.commonHouse, new List<House>(village.houseList), mapControl);
                placeManager.deleteShapeFile(program.path, "摆放结果_" + village.name);
                village.commonHouse.delete();
                foreach (House house in village.houseList)
                {
                    house.delete();
                }
            }
        }

        private bool isValid()
        {
            if (villageList.Count == 0)
            {
                return false;
            }

            BindingGroup bindingGroup = HousePlacerStackPanel.BindingGroup;
            if (!Tool.checkBindingGroup(bindingGroup)) 
            {
                return false;
            }
            return true;
        }

        private void AddHouseButtonClick(object sender, RoutedEventArgs e)
        {
            Button addHouseButton = sender as Button;
            StackPanel houseStackPanel = addHouseButton.Parent as StackPanel;
            Grid housePlacerGrid = houseStackPanel.Parent as Grid;
            TextBlock villageIDTextBlock = housePlacerGrid.FindName("VillageIDTextBlock") as TextBlock;
            int villageID = Int32.Parse(villageIDTextBlock.Text);

            House house = House.GetDefaultHouse();
            house.villageID = villageID;
            house.saveWithoutCheck();
            house.id = House.GetLastHouseID();

            foreach (Village village in villageList)
            {
                if (village.id == villageID)
                {
                    village.houseList.Add(house);
                }
            }
        }

        private void HouseGroupBoxMouseDown(object sender, MouseButtonEventArgs e)
        {
            GroupBox houseGroupBox = sender as GroupBox;
            TextBlock houseIDTextBlock = houseGroupBox.FindName("HouseIDTextBlock") as TextBlock;
            int houseID = Int32.Parse(houseIDTextBlock.Text.ToString());
            TextBlock villageIDTextBlock = houseGroupBox.FindName("VillageIDTextBlock") as TextBlock;
            int villageID = Int32.Parse(villageIDTextBlock.Text);

            foreach (Village village in villageList)
            {
                if (village.id == villageID)
                {
                    foreach (House house in village.houseList)
                    {
                        if (house.id == houseID)
                        {
                            houseShowcaseManager = new HouseShowcaseManager(houseMapControl);
                            houseShowcaseManager.ShowHouse(house, village.commonHouse);
                        }
                    }
                }
            }
        }

        private void save()
        {
            foreach (Village village in villageList)
            {
                village.commonHouse.saveOrUpdate();
                foreach (House house in village.houseList)
                {
                    house.saveOrUpdate();
                }
            }
        }

        private void StartCaculateButtonClick(object sender, RoutedEventArgs e)
        {
            if (isValid())
            {
                NotificationHelper.Trigger("mask");
                finish = true;
                NotificationHelper.Trigger("HousePlacerUserControlFinish");
                save();
                //开始计算.
                place();

                NotificationHelper.Trigger("unmask");
            }
            else
            {
                Tool.M("请完整填写信息");
                return;
            }
        }

        private void place()
        {
            foreach (Village village in villageList)
            {
                PlaceManager placeManager = new PlaceManager(village.commonHouse, new List<House>(village.houseList), mapControl);
                placeManager.deleteShapeFile(program.path, "摆放结果_" + village.name);
                placeManager.makeArea((village.polygonElement as IElement).Geometry,
                    (village.innerRoad.lineElement as IElement).Geometry);
                if (!placeManager.place())
                {
                    return;
                }
                placeManager.save(program.path, "摆放结果_" + village.name + ".shp");
                placeManager.addShapeFile(program.path, "摆放结果_" + village.name + ".shp", "摆放结果_" + village.name);
                NotificationHelper.Trigger("unmask");
                return;
                
                string path = System.IO.Path.Combine(program.path, "outerground.shp");
                placeManager.saveOuterGround(path);

                path = System.IO.Path.Combine(program.path, "centerground.shp");
                placeManager.saveCenterGround(path);

                path = System.IO.Path.Combine(program.path, "result.shp");
                placeManager.saveHouse(path);

                path = System.IO.Path.Combine(program.path, "innerroad.shp");
                placeManager.saveInnerRoad(path);

                path = System.IO.Path.Combine(program.path, "road.shp");
                placeManager.saveRoad(path);
                Tool.M("摆放完成");
            }
        }
    }
}
