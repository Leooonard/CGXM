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

using ESRI.ArcGIS.esriSystem;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.PublisherControls;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.DataSourcesFile;
using System.Collections;
using System.IO;
using ESRI.ArcGIS.ADF.BaseClasses;
using ESRI.ArcGIS.SystemUI;

using Intersect;
using System.Threading;
using System.Xml;
using System.Windows.Markup;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using Intersect.Lib;

namespace Intersect
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>

    public partial class MainWindow : Window
    {
        public AxMapControl mapControl;
        public AxToolbarControl toolbarControl;
        public AxTOCControl tocControl;
        private Geoprocessor gp;
        private bool chosenAreaFlag = true;
        private int projectAndMapID; //指现在被光标选中的pm.
        private int chosenProjectAndMapID; //指当前programlist所属的pm.
        private int programID;
        private Project projectAndMap;
        private List<Feature> siteSelectResultArray; //将记录每次选址之后的结果.
        private string lastFocusedFormName;
        private List<ListBoxItem> projectAndMapListItemList;
        private bool divideAreaFlag = true;
        private List<Condition> conditionList;
        private List<string> mapLayerNameList;
        private ObservableCollection<Project> projectAndMapList;
        private ObservableCollection<Program> programList;
        private ObservableCollection<House> houseList;
        private CommonHouse commonHouse;

        private int configId;
        private Exception exp;

        public event EventHandler createProgramButtonClickEventHandler;
        public event MouseButtonEventHandler programNameTextBlockMouseDownEventHandler;
        public event EventHandler programNameButtonClickEventHandler;
        public event MouseButtonEventHandler configGridMouseDownEventHandler;
        public event MouseButtonEventHandler siteSelectorGridMouseDownEventHandler;
        public event MouseButtonEventHandler housePlaceGridMouseDownEventHandler;
        
        public MainWindow()
        {
            InitializeComponent();

            mapControl = new AxMapControl();
            toolbarControl = new AxToolbarControl();
            tocControl = new AxTOCControl();

            chosenAreaFlag = false;
            conditionList = new List<Condition>();

            projectAndMapListItemList = new List<ListBoxItem>();
            //configIns = new config(this);

            //注册mask，unmask事件。
            NotificationHelper.Register("mask", new NotificationHelper.NotificationEvent(delegate()
                {
                    mask();
                }));
            NotificationHelper.Register("unmask", new NotificationHelper.NotificationEvent(delegate() 
                {
                    unmask();
                }));
        }

        public void mask()
        {
            ProgramListMask.Visibility = System.Windows.Visibility.Visible;
        }

        public void unmask()
        {
            ProgramListMask.Visibility = System.Windows.Visibility.Collapsed;
        }

        private void UpdateProgramList()
        {
            Sql sqlIns = new Sql();
            List<Program> list = sqlIns.SelectAllProgramByPmID(chosenProjectAndMapID);
            ProgramList.ItemsSource = list;
        }

        public bool checkMap(string path)
        {
            return mapControl.CheckMxFile(path);
        }

        public void LoadMap(string path)
        {
            toolbarControl.SetBuddyControl(mapControl);
            tocControl.SetBuddyControl(mapControl);
            //添加命令按钮到toolbarControl
            if (toolbarControl.Count == 0)
            {
                toolbarControl.AddItem("esriControls.ControlsMapNavigationToolbar");
            }

            mapControl.LoadMxFile(path, 0, "");
            mapControl.MoveLayerTo(0, mapControl.LayerCount - 1);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            mapHost.Child = mapControl;
            toolbarHost.Child = toolbarControl;
            tocHost.Child = tocControl;
        }

        private void ProjectAndMapStackPanel_MouseDown(object sender, MouseButtonEventArgs e)
        {
            int clickCount = e.ClickCount;
            StackPanel stackPanel = sender as StackPanel;
            TextBlock textBlock = stackPanel.FindName("ProjectAndMapID") as TextBlock;
            projectAndMapID = Int32.Parse(textBlock.Text);
            if (clickCount == 1)
            {
            }
            else if (clickCount == 2)
            {
                //双击时的逻辑.
                //通过pmID查询相关的所有config, 放入列表.
                chosenProjectAndMapID = projectAndMapID;
                Thread t = new Thread(delegate()
                {
                    System.Threading.Thread.Sleep(500);
                    this.Dispatcher.BeginInvoke((ThreadStart)delegate()
                    {
                        projectAndMap = new Project();
                        projectAndMap.id = chosenProjectAndMapID;
                        projectAndMap.select();
                        programList = projectAndMap.getAllRelatedProgram();
                        ProgramList.ItemsSource = programList;

                        ProgramListTitle.Text = projectAndMap.name + "-方案列表";

                        CreateProgramButton.IsEnabled = true;
                    });
                });
                t.Start();
            }
        }

        private void HideMapControl()
        {
            mapHost.Visibility = System.Windows.Visibility.Hidden;
            toolbarHost.Visibility = System.Windows.Visibility.Hidden;
            tocHost.Visibility = System.Windows.Visibility.Hidden;
        }

        private void ShowMapControl()
        {
            mapHost.Visibility = System.Windows.Visibility.Visible;
            toolbarHost.Visibility = System.Windows.Visibility.Visible;
            tocHost.Visibility = System.Windows.Visibility.Visible;
        }

        private List<string> getMaplayerNameList(AxMapControl mapControl)
        {
            List<string> mapLayerNameList = new List<string>();
            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                ILayer layer = mapControl.get_Layer(i);
                ICompositeLayer compositeLayer = layer as ICompositeLayer;
                if (compositeLayer == null)
                {
                    //说明不是一个组合图层, 直接获取图层名.
                    mapLayerNameList.Add(layer.Name);
                }
                else
                {
                    for (int j = 0; j < compositeLayer.Count; j++)
                    {
                        ILayer ly = compositeLayer.get_Layer(j);
                        mapLayerNameList.Add(ly.Name);
                    }
                }
            }
            return mapLayerNameList;
        }

        private void ConfigGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid grid = sender as Grid;
            TextBlock textBlock = grid.FindName("ConditionIDTextBlock") as TextBlock;
            int conditionID = Int32.Parse(textBlock.Text);
            Condition condition = new Condition();
            condition.id = conditionID;
            condition.select();

            //将地图中对应的那层进行单独显示.

            //先隐藏所有层.
            GisTool.HideAllLayerInMap(mapControl);

            //依靠名字找层, 再显示.
            string layerName = mapLayerNameList[condition.labelID];
            ILayer layer = GisTool.getLayerByName(layerName, mapControl);
            layer.Visible = true;

            mapControl.ActiveView.Refresh();
        }

        private void CreateProgramButtonClick(object sender, RoutedEventArgs e)
        {
            createProgramButtonClickEventHandler(sender, e);
        }

        private void DeleteProgramButton_Click(object sender, RoutedEventArgs e)
        {
            Sql sqlIns = new Sql();
            if (sqlIns.DeleteProgramByID(programID))
            {
                Tool.M("删除成功");
                UpdateProgramList();
            }
            else
            {
                Tool.M("删除失败");
            }
        }

        private void PrintRestrainAndStandardTextBlack(StackPanel stackPanel)
        {
            StackPanel restraintStackPanel = stackPanel.FindName("RestrainConfigStackPanel") as StackPanel;
            StackPanel standardStackPanel = stackPanel.FindName("StandardConfigStackPanel") as StackPanel;
            for (int i = 0; i < restraintStackPanel.Children.Count; i++)
            {
                Grid grid = restraintStackPanel.Children[i] as Grid;
                TextBlock textBlock = grid.Children[0] as TextBlock;
                textBlock.Foreground = new SolidColorBrush(Colors.Black);
                textBlock.FontWeight = FontWeights.Normal;
            }
            for (int i = 0; i < standardStackPanel.Children.Count; i++)
            {
                Grid grid = standardStackPanel.Children[i] as Grid;
                TextBlock textBlock = grid.Children[0] as TextBlock;
                textBlock.Foreground = new SolidColorBrush(Colors.Black);
                textBlock.FontWeight = FontWeights.Normal;
            }
        }

        private void HouseListItem_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid grid = sender as Grid;
            StackPanel stackPanel = grid.Parent as StackPanel;
            TextBlock textBlock = stackPanel.FindName("ID") as TextBlock;
            int hID = Int32.Parse(textBlock.Text);
            House house = new House();
            foreach (House h in houseList)
            {
                if (hID == h.id)
                {
                    house = h;
                }
            }
        }

        private LinearGradientBrush defaultTextBoxBorderBrush = null;
        private void ProgramNameTextBlockMouseDown(object sender, MouseButtonEventArgs e)
        {
            if(programNameTextBlockMouseDownEventHandler != null)
                programNameTextBlockMouseDownEventHandler(sender, e);
        }

        private void ProgramNameButtonClick(object sender, RoutedEventArgs e)
        {
            if (programNameButtonClickEventHandler != null)
                programNameButtonClickEventHandler(sender, e);
        }

        private void ConfigGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            configGridMouseDownEventHandler(sender, e);
        }

        private void SiteSelectorGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            siteSelectorGridMouseDownEventHandler(sender, e);
        }

        private void HousePlaceGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            housePlaceGridMouseDownEventHandler(sender, e);
        }

        
    }
}
