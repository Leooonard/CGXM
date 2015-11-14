﻿using System;
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
        private int housePlacerIndexCount;

        public Intersect.ProgramStepUserControl.OnMapControlMouseDown mapControlMouseDown;
        public bool valid = false;
        private bool prePlaced = false;

        public HousePlacerUserControl()
        {
            InitializeComponent();
        }

        public void init(int programID, AxMapControl mc)
        {
            program = new Program();
            program.id = programID;
            program.select();

            housePlacerIndexCount = 0;

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
                        else
                        {
                            foreach (House house in houseList)
                            {
                                house.housePlacerListIndex = housePlacerIndexCount++;
                            }
                        }
                        village.houseList = houseList;
                        village.innerRoad = village.getRelatedInnerRoad();
                    }
                }
                villageList = inUseVillageList;
            }

            mapControl = mc;

            prePlaced = false;

            mapControlMouseDown = null;
            HousePlacerListBox.ItemsSource = villageList;

            Thread t = new Thread(delegate()
            {
                System.Threading.Thread.Sleep(500);
                Dispatcher.BeginInvoke((ThreadStart)delegate()
                {
                    if (isValid())
                    {
                        valid = true;
                        NotificationHelper.Trigger("HousePlacerUserControlFinish");
                    }
                });
            });
            t.Start();
        }

        public void unInit()
        { 
            
        }

        public void prePlace()
        {
            if (prePlaced)
            {
                return;
            }
            prePlaced = true;
            if (valid)
            {
                place();
            }
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
                return false;
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
            foreach (Village village in villageList)
            {
                if (village.id == villageID)
                {
                    house.housePlacerListIndex = housePlacerIndexCount++;
                    village.houseList.Add(house);
                }
            }
        }

        private void HouseGroupBoxMouseDown(object sender, MouseButtonEventArgs e)
        {
            GroupBox houseGroupBox = sender as GroupBox;
            TextBlock houseListIndexTextBlock = houseGroupBox.FindName("HouseListIndexTextBlock") as TextBlock;
            int houseListIndex = Int32.Parse(houseListIndexTextBlock.Text);
            TextBlock villageIDTextBlock = houseGroupBox.FindName("VillageIDTextBlock") as TextBlock;
            int villageID = Int32.Parse(villageIDTextBlock.Text);
            foreach (Village village in villageList)
            {
                if (village.id == villageID)
                {
                    foreach (House house in village.houseList)
                    {
                        if (house.housePlacerListIndex == houseListIndex)
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
                valid = true;
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
                placeManager.makeArea(GisTool.ConvertIPolygonElementToIPolygon(village.polygonElement));
                if (placeManager.splitArea(GisTool.ConvertILineElementToIPolyline(village.innerRoad.lineElement)))
                {
                    placeManager.place();
                    for (int i = 0; i < placeManager.drawnHouseList.Count; i++)
                    {
                        HouseManager houseManager = placeManager.drawnHouseList[i];
                        ArrayList housePolygonArrayList = houseManager.makeHousePolygon();
                        GisTool.drawPolygon(houseManager.makeHousePolygon()[0] as IPolygon, mapControl, GisTool.RandomRgbColor());
                        foreach (IGeometry geom in housePolygonArrayList[1] as List<IGeometry>)
                        {
                            GisTool.drawPolygon(geom as IPolygon, mapControl, GisTool.RandomRgbColor());
                        }
                    }
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
}