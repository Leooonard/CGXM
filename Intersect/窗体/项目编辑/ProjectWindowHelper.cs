using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Shapes;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Carto;
using System.Collections.ObjectModel;
using System.Windows.Data;
using ESRI.ArcGIS.Geodatabase;
using System.Text.RegularExpressions;
using System.IO;
using ESRI.ArcGIS.Geometry;
using Intersect.Lib;

namespace Intersect
{
    /*
       包裹项目创建/编辑窗口的事件操作, 业务相关操作等.
     */
    public abstract class ProjectWindowHelper
    {
        public const string BASE_LAYER_NAME = "行政村界";
        public const string BASE_LAYER_FIELD_NAME = "name";
        private const string SLOPE_LAYER_NAME = "";
        private const string HEIGHT_LAYER_NAME = "";

        protected List<string> specialLayerNameList = new List<string>() { "地形坡度", "地形高程", "行政村界" };

        public Project project
        {
            get;
            set;
        }

        protected ProjectWindow projectWindow;
        public ObservableCollection<string> mapLayerNameList;
        public ObservableCollection<string> villageNameList;
        protected ObservableCollection<Label> completeLabelList;
        protected ObservableCollection<Label> uncompleteLabelList;
        protected ObservableCollection<Label> choosedLabelList;
        protected List<Label> rasterLayerLabelList;

        protected ProjectWindowCallback closeCallback;
        protected ProjectWindowCallback confirmCallback;
        public delegate void ProjectWindowCallback(int id);
        public string sourceMapPath;

        public IMapDocument mapDocument = null;

        public ProjectWindowHelper(ProjectWindowCallback closeCB, ProjectWindowCallback confirmCB)
        {
            closeCallback = closeCB;
            confirmCallback = confirmCB;

            projectWindow = new ProjectWindow();
            projectWindow.CloseEventHandler += new EventHandler(delegate(object sender, EventArgs e)
                {
                    close();
                    if (closeCallback != null)
                    {
                        closeCallback(Const.ERROR_INT);                    
                    }
                });
            projectWindow.ConfirmEventHandler += new EventHandler(delegate(object sender, EventArgs e)
                {
                    int id = confirm();
                    if (confirmCallback != null)
                    {
                        confirmCallback(id);
                    }
                });
            projectWindow.BrowseFileEventHandler += new EventHandler(browseFile);
            projectWindow.baseMapLayerComboBoxSelectionChangedEventHandler += new EventHandler(baseMapLayerComboBoxSelectionChanged);
            projectWindow.uncompleteLabelContentComboBoxTextChangedEventHandler += new EventHandler(uncompleteLabelContentComboBoxTextChanged);
            projectWindow.uncompleteLabelContentComboBoxLostFocusEventHandler += new EventHandler(uncompleteLabelContentComboBoxLostFocus);
            projectWindow.isChoosedCheckBoxClickEventHandler += new EventHandler(isChoosedCheckBoxClick);
            projectWindow.uncompleteLabelIsChoosedCheckBoxClickEventHandler += new EventHandler(isChoosedCheckBoxClick);

            Init();

            projectWindow.CompleteLabelListBox.ItemsSource = completeLabelList;
            projectWindow.UncompleteLabelListBox.ItemsSource = uncompleteLabelList;
            projectWindow.BaseMapLayerComboBox.ItemsSource = villageNameList;
        }

        private void isChoosedCheckBoxClick(object sender, EventArgs e)
        {
            CheckBox isChoosedCheckBox = sender as CheckBox;
            Grid grid = isChoosedCheckBox.Parent as Grid;
            TextBlock mapLayerNameTextBlock = grid.Children[0] as TextBlock;
            string mapLayerName = mapLayerNameTextBlock.Text;
            bool isChoosed = (bool)isChoosedCheckBox.IsChecked;
            ILayer layer = GisTool.getLayerByName(mapLayerName, projectWindow.mapControl);
            if (isChoosed)
            {
                layer.Visible = true;
                IRasterLayer rasterLayer = layer as IRasterLayer;
                if (rasterLayer != null)
                {
                    ILayerEffects layerEffects = rasterLayer as ILayerEffects;
                    layerEffects.Transparency = 60;
                }
            }
            else
            {
                layer.Visible = false;
            }

            projectWindow.mapControl.ActiveView.Refresh();
        }

        private void baseMapLayerComboBoxSelectionChanged(object sender, EventArgs e)
        {
            ComboBox baseMapLayerComboBox = sender as ComboBox;
            if (baseMapLayerComboBox.SelectedValue == null)
            {
                return;
            }
            string villageName = baseMapLayerComboBox.SelectedValue.ToString();
            ILayer layer = GisTool.getLayerByName(BASE_LAYER_NAME, projectWindow.mapControl);
            GisTool.HighlightFeature(layer, String.Format("Name='{0}'", villageName), projectWindow.mapControl);
        }

        private void uncompleteLabelContentComboBoxTextChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            string content = comboBox.Text;
            Grid grid = comboBox.Parent as Grid;
            TextBlock mapLayerNameTextBlock = grid.FindName("MapLayerNameTextBlock") as TextBlock;
            string mapLayerName = mapLayerNameTextBlock.Text;
            Label targetLabel = null;
            foreach (Label label in uncompleteLabelList)
            {
                if (label.mapLayerName == mapLayerName)
                {
                    targetLabel = label;
                }
            }
            if (!targetLabel.uncomleteLabelContentManager.getIsInited())
            {
                targetLabel.uncomleteLabelContentManager.Inited();
                //做初始化工作.
                modifyUncompleteLabelContent(targetLabel, content, comboBox);
                return;
            }

            //常规工作. 即记录是否有输入的改变.
            targetLabel.uncomleteLabelContentManager.textChanged = true;
        }

        private void uncompleteLabelContentComboBoxLostFocus(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            string content = comboBox.Text;
            if (comboBox.IsDropDownOpen)
                return; //是打开下拉框那一下动作, 不做操作.

            Grid grid = comboBox.Parent as Grid;
            TextBlock mapLayerNameTextBlock = grid.FindName("MapLayerNameTextBlock") as TextBlock;
            string mapLayerName = mapLayerNameTextBlock.Text;
            Label targetLabel = null;
            foreach (Label label in uncompleteLabelList)
            {
                if (label.mapLayerName == mapLayerName)
                {
                    targetLabel = label;
                }
            }
            if (!targetLabel.uncomleteLabelContentManager.textChanged)
                return; //只做了下拉动作, 没有修改.

            modifyUncompleteLabelContent(targetLabel, content, comboBox);
            targetLabel.uncomleteLabelContentManager.textChanged = false;
            return;
        }

        private void modifyUncompleteLabelContent(Label targetLabel, string content, ComboBox comboBox)
        {
            if (UncompleteLabelComboBoxManager.IsUncompleteLabelComboBoxTextRepeat(uncompleteLabelList, targetLabel, content))
            {
                Tool.M("标签内容不能重复.");
                if (targetLabel.content != "")
                {
                    comboBox.Text = content;
                    return;
                }
            }
            while (UncompleteLabelComboBoxManager.IsUncompleteLabelComboBoxTextRepeat(uncompleteLabelList, targetLabel, content))
            {
                content = content.Substring(0, content.Length - 1);
            }
            comboBox.Text = content;
            if (comboBox.SelectedIndex == Const.ERROR_INT)
            {
                //没有匹配到.
                ;
            }
            else
            {
                //匹配到.
                UncompleteLabelComboBoxManager.HideUncompleteLabelComboBoxItem(uncompleteLabelList, targetLabel, content);
            }

            string prevContent = targetLabel.content; //老的内容.
            if (UncompleteLabelComboBoxManager.IsContentCityPlanStandard(prevContent))
            {
                //是下拉菜单选项的内容.
                UncompleteLabelComboBoxManager.ShowHiddenUncompleteLabelComboBoxItem(uncompleteLabelList, targetLabel, prevContent);
            }
            targetLabel.content = content;
        }

        protected void Init()
        {
            string oldProjectName = "";
            if (project != null)
            {
                oldProjectName = project.name;
            }
            project = new Project();
            project.name = oldProjectName;
            if (mapLayerNameList == null)
            {
                mapLayerNameList = new ObservableCollection<string>();
            }
            else
            {
                mapLayerNameList.Clear();
            }
            if (villageNameList == null)
            {
                villageNameList = new ObservableCollection<string>();
            }
            else
            {
                villageNameList.Clear();                
            }
            if (completeLabelList == null)
            {
                completeLabelList = new ObservableCollection<Label>();
            }
            else
            {
                completeLabelList.Clear();                
            }
            if (uncompleteLabelList == null)
            {
                uncompleteLabelList = new ObservableCollection<Label>();
            }
            else
            {
                uncompleteLabelList.Clear();                
            }
            if (choosedLabelList == null)
            {
                choosedLabelList = new ObservableCollection<Label>();
            }
            else
            {
                choosedLabelList.Clear();                
            }
            if (rasterLayerLabelList == null)
            {
                rasterLayerLabelList = new List<Label>();
            }
            else
            {
                rasterLayerLabelList.Clear();                
            }
            if (mapDocument == null)
            {
                mapDocument = new MapDocumentClass();
            }

            Tool.bind(project
                 , "name"
                 , BindingMode.TwoWay, projectWindow.NameTextBox
                 , TextBox.TextProperty
                 , new List<ValidationRule>() { 
                     new StringValidationRule(Project.NAME_MAX_LENGTH)
                 }
                 , "baseBindingGroup");

            Tool.bind(project
                 , "path"
                 , BindingMode.TwoWay
                 , projectWindow.PathTextBox
                 , TextBox.TextProperty, new List<ValidationRule>() { 
                     new StringValidationRule()
                 }
                 , "baseBindingGroup");

            Tool.bind(project
                 , "baseMapIndex"
                 , BindingMode.TwoWay
                 , projectWindow.BaseMapLayerComboBox
                 , ComboBox.SelectedIndexProperty, new List<ValidationRule>() { 
                     new NotNegativeDoubleValidationRule("请选择基础图层")
                 }
                 , "baseBindingGroup");
        }

        protected abstract int confirm();

        public void show()
        {
            projectWindow.Show();
        }

        public void close()
        {
            projectWindow.Close();
        }

        protected string checkAllUIElementValid()
        {
            BindingGroup bindingGroup;
            StackPanel baseStackPanel = projectWindow.BaseStackPanel;
            bindingGroup = baseStackPanel.BindingGroup;
            if (!Tool.checkBindingGroup(bindingGroup))
                return "请完整填写项目信息.";
            return "";
        }

        public bool isOpen()
        {
            return projectWindow.IsVisible;
        }

        public ObservableCollection<string> updateVillageNameList(string villageLayerName, string villageLayerFieldName, AxMapControl mapControl)
        {
            ILayer baseLayer = GisTool.getLayerByName(villageLayerName, mapControl);
            if (baseLayer == null)
            {
                throw new Exception("地图不完整。");
            }
            IFeatureLayer fBaseLayer = baseLayer as IFeatureLayer;
            if (fBaseLayer == null)
            {
                throw new Exception("地图不完整。");
            }
            IFeatureClass fBaseFeatureClass = fBaseLayer.FeatureClass;
            ObservableCollection<string> villageNameList = new ObservableCollection<string>(GisTool.GetValueListFromFeatureClass(fBaseFeatureClass, villageLayerFieldName));
            if (villageNameList == null)
            {
                throw new Exception("地图不完整。");
            }
            //在list中去除重复
            return new ObservableCollection<string>(villageNameList.Distinct().ToList<string>());
        }

        protected CityPlanStandard checkMapLayerNameValid(string mapLayerName)
        {
            ObservableCollection<CityPlanStandard> cityPlanStandardList = CityPlanStandard.GetAllCityPlanStandard();
            foreach (CityPlanStandard cityPlanStandard in cityPlanStandardList)
            {
                Regex regex = new Regex(String.Format(@"^{0}[\u4E00-\u9FFF]+$", cityPlanStandard.number));
                if (regex.IsMatch(mapLayerName))
                {
                    return cityPlanStandard;
                }
            }
            return null;
        }

        public void loadMap(string path)
        {
            projectWindow.toolbarControl.SetBuddyControl(projectWindow.mapControl);
            if (projectWindow.toolbarControl.Count == 0)
            {
                projectWindow.toolbarControl.AddItem("esriControls.ControlsMapNavigationToolbar");
            }
            projectWindow.mapControl.LoadMxFile(path);
        }

        public void processFile(string mapPath)
        {
            if (!projectWindow.mapControl.CheckMxFile(mapPath))
            {
                throw new Exception();
            }

            loadMap(mapPath);

            List<string> necessaryLayerNameList = new List<string>() 
            {
                "行政村界"
            };
            int baseLayerIndex = 0;

            ObservableCollection<string> cityPlanStandardInfoList = new ObservableCollection<string>();
            ObservableCollection<CityPlanStandard> cityPlanStandardList = CityPlanStandard.GetAllCityPlanStandard();

            for (int i = 0; i < projectWindow.mapControl.LayerCount; i++)
            {
                ILayer layer = projectWindow.mapControl.get_Layer(i);
                string layerName = projectWindow.mapControl.get_Layer(i).Name;
                mapLayerNameList.Add(layerName);
                if (necessaryLayerNameList.Contains(layerName))
                {
                    necessaryLayerNameList.Remove(layerName);
                }
                if (layerName == BASE_LAYER_NAME)
                {
                    foreach (string villageName in updateVillageNameList(layerName, BASE_LAYER_FIELD_NAME, projectWindow.mapControl))
                    {
                        villageNameList.Add(villageName);
                    }
                    baseLayerIndex = i;
                    GisTool.ExpandToMapView(layerName, projectWindow.mapControl);
                }
                else
                {
                    /*
                     * 匹配成功的图层, 将填写图层名和cpsID字段.
                     * 匹配不成功的图层, 将填写图层名字段.
                     */
                    bool complete = false;
                    Label label = new Label();
                    label.mapLayerName = layerName;
                    if (GisTool.isRasterLayer(layer))
                    {
                        label.isRaster = true;
                        rasterLayerLabelList.Add(label);
                    }
                    else
                    {
                        label.isRaster = false;
                    }

                    CityPlanStandard cityPlanStandard = checkMapLayerNameValid(layerName);
                    if (cityPlanStandard != null)
                    {
                        label.content = cityPlanStandard.getCityPlanStandardInfo();
                        completeLabelList.Add(label);
                        cityPlanStandardInfoList.Add(cityPlanStandard.getCityPlanStandardInfo());
                        complete = true;
                    }
                    else
                    {
                        uncompleteLabelList.Add(label);                    
                    }
                }
            }

            if (necessaryLayerNameList.Count > 0)
            {
                throw new Exception("地图文件不完整。");
            }

            foreach (Label label in uncompleteLabelList)
            {
                label.uncomleteLabelContentManager.pruningChooseableCityPlanStandardInfoList(cityPlanStandardInfoList);
                label.uncomleteLabelContentManager.Inited();
            }

            projectWindow.mapControl.MoveLayerTo(baseLayerIndex, projectWindow.mapControl.LayerCount - 1);

            project.baseMapIndex = -1;
            project.path = System.IO.Path.GetDirectoryName(mapPath);

            projectWindow.BaseMapLayerComboBox.SelectedIndex = -1;
        }

        public void browseFile(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
            fileDialog.InitialDirectory = Const.DEFAULT_FILE_DIALOG_FOLDER;
            fileDialog.Filter = "mxd files|*.mxd";
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                projectWindow.mask();
                string mapPath = fileDialog.FileName;
                try
                {
                    Init();
                    processFile(mapPath);
                    sourceMapPath = mapPath;
                }
                catch (Exception mapException)
                {
                    Tool.M("地图文件错误, 错误信息: " + mapException.Message);
                    project.path = "";
                    projectWindow.mapControl.ClearLayers();
                    projectWindow.mapControl.ActiveView.Refresh();
                }
                finally
                {
                    projectWindow.unmask();
                }
            }
        }

        
    }
}
