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
        private PlaceManager placeManager;
        private bool divideAreaFlag = true;
        private List<Condition> conditionList;
        private List<string> mapLayerNameList;
        private ObservableCollection<Project> projectAndMapList;
        private ObservableCollection<Program> programList;
        private ObservableCollection<Config> configList;
        private ObservableCollection<House> houseList;
        private CommonHouse commonHouse;

        private CreateProgramWindow createProgramNameWindow;
        private ModifyProgramWindow modifyProgramNameWindow;

        private LoadingWindow loadingWindow;

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


            loadingWindow = new LoadingWindow();
            loadingWindow.parent = this;
            LoadingWindowUC loadingWindowUC = new LoadingWindowUC();
            loadingWindow.content = loadingWindowUC;

            projectAndMapListItemList = new List<ListBoxItem>();
            //configIns = new config(this);
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

        private void MapControl_MouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            if (false)
            {
                placeManager = new PlaceManager(commonHouse, new List<House>(houseList), mapControl);
                //chosenAreaFlag = false;
                IGeometry geom = mapControl.TrackPolygon();
                placeManager.makeArea(geom);
            }
            if (false)
            {
                IGeometry line = mapControl.TrackLine();

                //检测是否产生了分割. 如果是完全不相关的线段, 不在屏幕上进行绘制.
                if (placeManager.splitArea(line))
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
                    GisTool.DrawPolyline(placeManager.innerRoadLine, mapControl);


                    MessageBox.Show("保存成功.");
                    ////最后把路径写入文件.
                    //System.IO.FileStream file = new System.IO.FileStream("C://work//route.txt", FileMode.OpenOrCreate);
                    //StreamWriter sw = new StreamWriter(file);
                    //if (area.splitLineEndPtArray.Count > 0)
                    //{
                    //    lineEndPt line = (lineEndPt)area.splitLineEndPtArray[0];
                    //    IPoint startPt = line.startPt;
                    //    IPoint endPt = line.endPt;
                    //    sw.WriteLine(startPt.X.ToString() + " " + startPt.Y.ToString());
                    //    sw.WriteLine(endPt.X.ToString() + " " + endPt.Y.ToString());
                    //}
                    //else
                    //{
                    //    IGeometry geom = area.areaGeom;
                    //    sw.WriteLine(geom.Envelope.LowerLeft.X.ToString(), geom.Envelope.LowerLeft.Y.ToString());
                    //    sw.WriteLine(geom.Envelope.UpperRight.X.ToString(), geom.Envelope.UpperRight.Y.ToString());
                    //}
                    //sw.Close();
                }
            }
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
                loadingWindow.show();
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

                        loadingWindow.close();
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

        private void DeleteProjectAndMapButton_Click(object sender, RoutedEventArgs e)
        {
            //在projectAndMapList内删除该对象.
            for (int i = 0; i < projectAndMapList.Count; i++)
            {
                if (projectAndMapList[i].id == projectAndMapID)
                {
                    Project pam = projectAndMapList[i];
                    projectAndMapList.RemoveAt(i);
                    pam.delete();
                    Tool.M("删除成功");
                    return;
                }
            }
            Tool.M("删除失败");
        }

        private void ResetProgramList()
        {
            ProgramList.ItemsSource = null;
            ProgramListTitle.Text = "方案列表";
        }

        private UIElement DeepCopy(UIElement element)
        {
            string shapestring = XamlWriter.Save(element);
            UIElement DeepCopyobject = (UIElement)XamlReader.Parse(shapestring);
            return DeepCopyobject;
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

        private bool SaveProgram()
        {
            for (int i = 0; i < ProgramList.Items.Count; i++)
            {
                ListBoxItem item = ProgramList.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                ContentPresenter myContentPresenter = Tool.FindVisualChild<ContentPresenter>(item);
                DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
                StackPanel stackPanel = myDataTemplate.FindName("ProgramDetailStackPanel", myContentPresenter) as StackPanel;
                if (stackPanel.Visibility == System.Windows.Visibility.Visible)
                {
                    StackPanel configStackPanel = stackPanel.FindName("ConfigStackPanel") as StackPanel;
                    BindingGroup configBindingGroup = configStackPanel.BindingGroup;
                    foreach (BindingExpression expression in configBindingGroup.BindingExpressions)
                    {
                        if (expression.HasError)
                        {
                            Tool.M(String.Format("配置信息错误, 错误信息: {0}", expression.ValidationError.ErrorContent));
                            return false;
                        }
                    }

                    //ListBox standardConfigListBox = stackPanel.FindName("StandardConfigListBox") as ListBox;
                    //for (int j = 0; j < standardConfigListBox.Items.Count; j++)
                    //{
                    //    item = standardConfigListBox.ItemContainerGenerator.ContainerFromIndex(j) as ListBoxItem;
                    //    myContentPresenter = Ut.FindVisualChild<ContentPresenter>(item);
                    //    myDataTemplate = myContentPresenter.ContentTemplate;
                    //    TextBox configValueTextBox = myDataTemplate.FindName("ConfigValueTextBox", myContentPresenter) as TextBox;
                    //    NotNegativeDoubleValidationRule rule = new NotNegativeDoubleValidationRule();
                    //    ValidationResult result = rule.Validate(configValueTextBox.Text, null);
                    //    if (!result.IsValid)
                    //    {
                    //        Ut.M(result.ErrorContent);
                    //        return false;
                    //    }
                    //}
                }
            }
            string validMsg;
            foreach (Config config in configList)
            {
                validMsg = config.checkValid(new List<string>() { "id" });
                if (validMsg != "")
                {
                    Tool.M(validMsg);
                    return false;
                }
            }
            for (int i = 0; i < ProgramList.Items.Count; i++)
            {
                ListBoxItem item = ProgramList.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                ContentPresenter myContentPresenter = Tool.FindVisualChild<ContentPresenter>(item);
                DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
                StackPanel stackPanel = myDataTemplate.FindName("ProgramDetailStackPanel", myContentPresenter) as StackPanel;
                if (stackPanel.Visibility == System.Windows.Visibility.Visible)
                {
                    StackPanel houseStackPanel = stackPanel.FindName("HouseStackPanel") as StackPanel;
                    BindingGroup bindingGroup = houseStackPanel.BindingGroup;
                    foreach (BindingExpression expression in bindingGroup.BindingExpressions)
                    {
                        if (expression.HasError)
                        {
                            Tool.M(String.Format("户型信息错误, 错误信息: {0}", expression.ValidationError.ErrorContent));
                            return false;
                        }
                    }
                }
            }
            validMsg = commonHouse.checkValid(new List<string>() { "id" });
            if (validMsg != "")
            {
                Tool.M(validMsg);
                return false;
            }
            foreach (House house in houseList)
            {
                validMsg = house.checkValid(new List<string>() { "id" });
                if (validMsg != "")
                {
                    Tool.M(validMsg);
                    return false;
                }
            }

            foreach (Config config in configList)
            {
                if (config.id == Const.ERROR_INT)
                {
                    config.save();
                    config.id = Config.GetLastConfigID();
                }
                else
                {
                    config.update();
                }
            }
            //这里再补充house保存.
            if (commonHouse.id == Const.ERROR_INT)
            {
                commonHouse.save();
                commonHouse.id = CommonHouse.GetLastCommonHouseID();
            }
            else
            {
                commonHouse.update();
            }
            foreach (House house in houseList)
            {
                if (house.id == Const.ERROR_INT)
                {
                    house.save();
                    house.id = House.GetLastHouseID();
                }
                else
                {
                    house.update();
                }
            }
            return true;
        }

        private void HideAllProgramListItem()
        {
            for (int i = 0; i < ProgramList.Items.Count; i++)
            {
                ListBoxItem item = ProgramList.ItemContainerGenerator.ContainerFromIndex(i) as ListBoxItem;
                ContentPresenter myContentPresenter = Tool.FindVisualChild<ContentPresenter>(item);
                DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
                StackPanel stackPanel = myDataTemplate.FindName("ProgramStackPanel", myContentPresenter) as StackPanel;
                StackPanel detailStackPanel = stackPanel.FindName("ProgramDetailStackPanel") as StackPanel;
                detailStackPanel.Visibility = System.Windows.Visibility.Collapsed;
            }
        }

        private void CreateProgramButtonClick(object sender, RoutedEventArgs e)
        {
            createProgramButtonClickEventHandler(sender, e);
        }

        private void ModifyProgramNameButton_Click(object sender, RoutedEventArgs e)
        {
            modifyProgramNameWindow.show(programID, projectAndMapID);
            HideAllProgramListItem();
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

        private void PrintHouseListItemTextBlack(ListBox listBox)
        {
            List<TextBlock> textBlockList = GetChildObjects<TextBlock>(listBox, typeof(TextBlock));
            foreach (TextBlock textBlock in textBlockList)
            {
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

        public List<T> GetChildObjects<T>(DependencyObject obj, Type typename) where T : FrameworkElement
        {
            DependencyObject child = null;
            List<T> childList = new List<T>();

            for (int i = 0; i <= VisualTreeHelper.GetChildrenCount(obj) - 1; i++)
            {
                child = VisualTreeHelper.GetChild(obj, i);

                if (child is T && (((T)child).GetType() == typename))
                {
                    childList.Add((T)child);
                }
                childList.AddRange(GetChildObjects<T>(child, typename));
            }
            return childList;
        }

        private void SaveProgramButton_Click(object sender, RoutedEventArgs e)
        {
            if (SaveProgram())
            {
                Tool.M("保存成功");
            }
            else
            {
                Tool.M("保存失败");
            }
        }

        private void StartCaculateButton_Click(object sender, RoutedEventArgs e)
        {
            if (!SaveProgram())
                return;

            //开始选址.
            Project pam = new Project();
            pam.id = projectAndMapID;
            pam.select();
            string baseMapName = mapLayerNameList[pam.baseMapIndex];
            //SiteSelector siteSelector = new SiteSelector(mapControl, pam.path, baseMapName, configList);
            //siteSelector.startSelectSite();

            //保存结果.
            //sqlIns.SaveSiteSelectResult(siteSelectResultArray, programID);
        }

        private void CaculateRealStandard()
        {
            double totalValue = 0;
            foreach (Config config in configList)
            {
                Condition condition = new Condition();
                condition.id = config.conditionID;
                condition.select();
                if (condition.type == Const.CONFIG_TYPE_STANDARD)
                {
                    if (config.value == -1)
                    {
                        foreach (Config cf in configList)
                        {
                            cf.realStandard = "真实权重: 错误";
                        }
                        return;
                    }
                    else
                    {
                        totalValue += config.value;
                    }
                }
            }
            foreach (Config config in configList)
            {
                Condition condition = new Condition();
                condition.id = config.conditionID;
                condition.select();
                if (condition.type == Const.CONFIG_TYPE_STANDARD)
                {
                    config.realStandard = String.Format("真实权重: {0:F}", (config.value / totalValue));
                }
            }
        }

        private LinearGradientBrush defaultTextBoxBorderBrush = null;
        private void ConfigValueTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            Config standardConfig = new Config();
            TextBox textBox = sender as TextBox;
            Grid grid = textBox.Parent as Grid;
            TextBlock textBlock = grid.FindName("ConditionIDTextBlock") as TextBlock;
            int conditionID = Int32.Parse(textBlock.Text);
            foreach (Config config in configList)
            {
                if (conditionID == config.conditionID)
                {
                    Condition condition = new Condition();
                    condition.id = conditionID;
                    condition.select();
                    if (condition.type == Const.CONFIG_TYPE_RESTRAINT)
                    {
                        return;
                    }
                    else
                    {
                        standardConfig = config;
                    }
                }
            }
            double value = 0;
            try
            {
                value = Double.Parse(textBox.Text);
                Regex reg = new Regex(@"^\d+|(\d+\.\d+)$");
                if (reg.IsMatch(textBox.Text))
                {
                    standardConfig.value = value;
                    CaculateRealStandard();
                    if (defaultTextBoxBorderBrush != null)
                        textBox.BorderBrush = defaultTextBoxBorderBrush;
                }
                else
                {
                    throw (new Exception());
                }
            }
            catch (Exception)
            {
                if (defaultTextBoxBorderBrush == null)
                    defaultTextBoxBorderBrush = textBox.BorderBrush as LinearGradientBrush;
                textBox.BorderBrush = new SolidColorBrush(Colors.Red);
                standardConfig.value = -1;
                foreach (Config cf in configList)
                {
                    cf.realStandard = "真实权重: 错误";
                }
            }
        }

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
