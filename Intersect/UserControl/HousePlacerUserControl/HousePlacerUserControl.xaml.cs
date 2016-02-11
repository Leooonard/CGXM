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
using ESRI.ArcGIS.Geodatabase;

namespace Intersect
{
    /// <summary>
    /// HousePlacerUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class HousePlacerUserControl : UserControl
    {
        private Program program;
        private ObservableCollection<HouseResult> houseResultList;
        private ObservableCollection<Village> villageList;
        private ObservableCollection<PlaceHelper> placeHelperList;
        private AxMapControl mapControl;

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
            placeHelperList = new ObservableCollection<PlaceHelper>();

            villageList = program.getAllRelatedVillage();
            if (villageList == null)
            {
                villageList = new ObservableCollection<Village>();
                houseResultList = new ObservableCollection<HouseResult>();
                HousePlacerListBox.ItemsSource = villageList;
                HouseResultListBox.ItemsSource = houseResultList;
                finish = false;
                return;
            }

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
                        houseList[i].name = "户型" + NumberToAlphaBeta(i);
                    }
                    village.houseList = houseList;
                    village.innerRoad = village.getRelatedInnerRoad();
                }
            }
            villageList = inUseVillageList;

            HousePlacerListBox.ItemsSource = villageList;

            houseResultList = new ObservableCollection<HouseResult>();
            HouseResultListBox.ItemsSource = houseResultList;

            foreach (Village village in villageList)
            {
                PlaceHelper placeHelper = new PlaceHelper(village, village.commonHouse, village.houseList.ToList<House>(), mapControl);
                if (!placeHelper.check(program.path))
                {
                    finish = false;
                    return;
                }
                placeHelper.add(program.path);
                foreach (HouseResult houseResult in placeHelper.report(program.path))
                { 
                    houseResultList.Add(houseResult);
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
        }

        public void clear()
        {
            foreach (Village village in villageList)
            {
                PlaceHelper placeHelper = new PlaceHelper(village, village.commonHouse, village.houseList.ToList<House>(), mapControl);
                placeHelper.clear(program.path);
                village.commonHouse.delete();
                foreach (House house in village.houseList)
                {
                    house.delete();
                }
            }
            houseResultList.Clear();
        }

        private bool isValid()
        {
            if (villageList.Count == 0)
            {
                return false;
            }

            foreach (Village village in villageList)
            {
                foreach (House house in village.houseList)
                {
                    if (house.landWidth < 0 || house.height < 0)
                    {
                        return false;
                    }
                }
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
                        village.houseList[i].name = "户型" + NumberToAlphaBeta(i);
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
            //每次计算前, 都算计算一次宅基地面宽.
            foreach (Village village in villageList)
            {
                foreach (House house in village.houseList)
                {
                    house.updateData(village.commonHouse);
                }
            }
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
            houseResultList.Clear();
            foreach (Village village in villageList)
            {
                PlaceHelper placeHelper = new PlaceHelper(village, village.commonHouse, village.houseList.ToList<House>(), mapControl);
                placeHelper.clear(program.path);
                placeHelper.place();
                placeHelper.save(program.path);
                placeHelper.add(program.path);
                foreach (HouseResult houseResult in placeHelper.report(program.path))
                {
                    houseResultList.Add(houseResult);
                }
                placeHelperList.Add(placeHelper);
            }
        }

        private void DeleteHouseButton_Click(object sender, RoutedEventArgs e)
        {
            Button houseDeleteButton = sender as Button;
            Grid grid = houseDeleteButton.Parent as Grid;
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
                                village.houseList[j].name = "户型" + NumberToAlphaBeta(j);
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

            return alphaBetaList[number].ToUpper();
        }


        private Village selectedVillage = null;
        private void ExportHouseButtonClick(object sender, RoutedEventArgs e)
        {
            HouseDecorationBorder.Visibility = System.Windows.Visibility.Visible;
            RoofTypeComboBox.ItemsSource = Const.DECORATION["roof"];
            WindowTypeComboBox.ItemsSource = Const.DECORATION["window"];
            DoorTypeComboBox.ItemsSource = Const.DECORATION["door"];
            Button exportHouseButton = sender as Button;
            int villageID = Int32.Parse(exportHouseButton.Tag.ToString());
            foreach (Village village in villageList)
            {
                if (village.id == villageID)
                {
                    selectedVillage = village;
                }
            }
        }

        private void DecorationFinishButton_Click(object sender, RoutedEventArgs e)
        {
            HouseDecoration selectedRoof = RoofTypeComboBox.SelectedItem as HouseDecoration;
            HouseDecoration selectedDoor = DoorTypeComboBox.SelectedItem as HouseDecoration;
            HouseDecoration selectedWindow = WindowTypeComboBox.SelectedItem as HouseDecoration;
            Dictionary<string, HouseDecoration> houseDecorationMap = new Dictionary<string,HouseDecoration>();
            houseDecorationMap.Add("roof", selectedRoof);
            houseDecorationMap.Add("window", selectedWindow);
            houseDecorationMap.Add("door", selectedDoor);
            exportHouse(selectedVillage, houseDecorationMap);
        }

        private void exportHouse(Village village, Dictionary<string, HouseDecoration> houseDecorationMap)
        {
            foreach (PlaceHelper placeHelper in placeHelperList)
            {
                if (placeHelper.isThisVillage(village))
                {
                    placeHelper.exportHouse(houseDecorationMap);
                    Tool.M("导出完成。");
                    return;
                }
            }
            Tool.M("该区域还未进行排放，请先进行排放再导出结果文件。");
        }

        private void DecorationCloseButton_click(object sender, RoutedEventArgs e)
        {
            HouseDecorationBorder.Visibility = System.Windows.Visibility.Collapsed;
        }
    }
}
