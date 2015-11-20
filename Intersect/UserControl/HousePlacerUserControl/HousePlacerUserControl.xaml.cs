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
            
            mapControl = mc;
            mapControlMouseDown = null;

            load();

            if (finish)
            {
                NotificationHelper.Trigger("HousePlacerUserControlFinish");
            }
            else
            {
                NotificationHelper.Trigger("HousePlacerUserControlUnFinish");
            }
        }

        public void refresh()
        {
            load();

            if (finish)
            {
                NotificationHelper.Trigger("HousePlacerUserControlFinish");
            }
            else
            {
                NotificationHelper.Trigger("HousePlacerUserControlUnFinish");
            }
        }

        private void load()
        {
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
                        for (int i = 0; i < houseList.Count; i++)
                        {
                            houseList[i].name = NumberToAlphaBeta(i);
                        }
                        village.houseList = houseList;
                        village.innerRoad = village.getRelatedInnerRoad();
                    }
                }
                villageList = inUseVillageList;
            }

            HousePlacerListBox.ItemsSource = villageList;

            foreach (Village village in villageList)
            {
                if (File.Exists(System.IO.Path.Combine(program.path, "摆放结果_" + village.name + ".shp")))
                {
                    PlaceManager placeManager = new PlaceManager(village.commonHouse, new List<House>(village.houseList), mapControl);
                    placeManager.addShapeFile(program.path, "摆放结果_" + village.name + ".shp", "摆放结果_" + village.name);
                }
                else
                {
                    finish = false;
                    return;
                }
            }
            finish = true;
        }

        public void initAxComponents()
        {
            if (houseMapControl != null)
            {
                return;
            }
            houseMapControl = new AxMapControl();
            HouseMapHost.Child = houseMapControl;
        }

        public void clear()
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
            Grid grid = addHouseButton.Parent as Grid;
            StackPanel houseStackPanel = grid.Parent as StackPanel;
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
                    for (int i = 0; i < village.houseList.Count; i++)
                    {
                        village.houseList[i].name = NumberToAlphaBeta(i);
                    }
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

        private void DeleteHouseButton_Click(object sender, RoutedEventArgs e)
        {
            Button houseDeleteButton = sender as Button;
            Grid grid = houseDeleteButton.Parent as Grid;
            grid = grid.Parent as Grid;
            TextBlock houseIDTextBlock = grid.FindName("HouseIDTextBlock") as TextBlock;
            int houseID = Int32.Parse(houseIDTextBlock.Text);
            TextBlock villageIDTextBlock = grid.FindName("VillageIDTextBlock") as TextBlock;
            int villageID = Int32.Parse(villageIDTextBlock.Text);

            foreach(Village village in villageList)
            {
                if (village.id == villageID)
                {
                    for (int i = 0; i < village.houseList.Count; i++)
                    {
                        if (village.houseList[i].id == houseID)
                        {
                            village.houseList[i].delete();
                            village.houseList.RemoveAt(i);
                            for (int j = 0; j < village.houseList.Count; j++)
                            {
                                village.houseList[j].name = NumberToAlphaBeta(j);
                            }
                        }
                    }
                }
            }
        }

        private string NumberToAlphaBeta(int number)
        {
            string[] alphaBetaList = new string[] { 
                "a", "b", "c", "d", "e", "f", "g", "h",
                "i", "j", "k", "l", "m", "n", "o", "p",
                "q", "r", "s", "t", "u", "v", "w", "x", "y", "z"
            };

            return alphaBetaList[number];
        }
    }
}
