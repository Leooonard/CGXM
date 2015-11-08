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

namespace Intersect
{
    public abstract class ProjectWindowWrapper
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
        protected ObservableCollection<Label> rasterLabelList; //raster图层需要转换为point. 但是转换之后的图层不在地图内, 所以要直接做成label

        protected ProjectWindowCallback closeCallback;
        protected ProjectWindowCallback confirmCallback;
        public delegate void ProjectWindowCallback(int id);

        public ProjectWindowWrapper(ProjectWindowCallback closeCB, ProjectWindowCallback confirmCB)
        {
            closeCallback = closeCB;
            confirmCallback = confirmCB;

            projectWindow = new ProjectWindow();
            projectWindow.CloseEventHandler += new EventHandler(delegate(object sender, EventArgs e)
                {
                    close();
                    if (closeCallback != null)
                        closeCallback(C.ERROR_INT);
                });
            projectWindow.ConfirmEventHandler += new EventHandler(delegate(object sender, EventArgs e)
                {
                    int id = confirm();
                    if (confirmCallback != null)
                        confirmCallback(id);
                });
            projectWindow.BrowseFileEventHandler += new EventHandler(browseFile);
            projectWindow.comboBoxSelectionChangedEventHandler += new EventHandler(ComboBoxSelectionChanged);
            projectWindow.deleteConditionEventHandler += new EventHandler(deleteCondition);
            projectWindow.uncompleteLabelContentComboBoxTextChangedEventHandler += new EventHandler(uncompleteLabelContentComboBoxTextChanged);
            projectWindow.uncompleteLabelContentComboBoxLostFocusEventHandler += new EventHandler(uncompleteLabelContentComboBoxLostFocus);

            Init();

            projectWindow.CompleteLabelListBox.ItemsSource = completeLabelList;
            projectWindow.UncompleteLabelListBox.ItemsSource = uncompleteLabelList;
            projectWindow.BaseMapLayerComboBox.ItemsSource = villageNameList;
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
                Ut.M("标签内容不能重复.");
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
            if (comboBox.SelectedIndex == C.ERROR_INT)
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
            project = new Project();
            if (mapLayerNameList == null)
                mapLayerNameList = new ObservableCollection<string>();
            else
                mapLayerNameList.Clear();
            if (villageNameList == null)
                villageNameList = new ObservableCollection<string>();
            else
                villageNameList.Clear();
            if (completeLabelList == null)
                completeLabelList = new ObservableCollection<Label>();
            else
                completeLabelList.Clear();
            if (uncompleteLabelList == null)
                uncompleteLabelList = new ObservableCollection<Label>();
            else
                uncompleteLabelList.Clear();
            if (choosedLabelList == null)
                choosedLabelList = new ObservableCollection<Label>();
            else
                choosedLabelList.Clear();
            if (rasterLabelList == null)
                rasterLabelList = new ObservableCollection<Label>();
            else
                rasterLabelList.Clear();

            Ut.bind(project, "name", BindingMode.TwoWay, projectWindow.NameTextBox
                , TextBox.TextProperty, new List<ValidationRule>() { 
                                new StringValidationRule(Project.NAME_MAX_LENGTH)
                            }, "baseBindingGroup");
            Ut.bind(project, "path", BindingMode.TwoWay, projectWindow.PathTextBox
                , TextBox.TextProperty, new List<ValidationRule>() { 
                                new StringValidationRule()
                            }, "baseBindingGroup");
            Ut.bind(project, "baseMapIndex", BindingMode.TwoWay, projectWindow.BaseMapLayerComboBox
                , ComboBox.SelectedIndexProperty, new List<ValidationRule>() { 
                                new NotNegativeDoubleValidationRule("请选择基础图层")
                            }, "baseBindingGroup");
        }

        protected abstract int confirm();

        //private void ResetConditionMapLayerIndex()
        //{
        //    for (int i = 0; i < conditionList.Count; i++)
        //    {
        //        conditionList[i].labelID = -1;
        //    }
        //}

        public void show()
        {
            projectWindow.Show();
        }

        protected string checkAllUIElementValid()
        {
            BindingGroup bindingGroup;
            StackPanel baseStackPanel = projectWindow.BaseStackPanel;
            bindingGroup = baseStackPanel.BindingGroup;
            if (!Ut.checkBindingGroup(bindingGroup))
                return "请完整填写项目信息.";
            return "";
        }

        public void deleteCondition(object sender, EventArgs e)
        {
            Button button = sender as Button;
            Grid grid = button.Parent as Grid;
            TextBlock textBlock = grid.FindName("ID") as TextBlock;
            int id = Int32.Parse(textBlock.Text);
            //for (int i = 0; i < conditionList.Count; i++)
            //{
            //    if (conditionList[i].id == id)
            //    {
            //        conditionList.RemoveAt(i);
            //        break;
            //    }
            //}
        }

        public bool isOpen()
        {
            return projectWindow.IsVisible;
        }

        public void close()
        {
            projectWindow.Close();
        }

        public void ComboBoxSelectionChanged(object sender, EventArgs e)
        {
            return; //这个功能先不用.
            try
            {
                ComboBox comboBox = sender as ComboBox;
                int index = comboBox.SelectedIndex;
                string selectedMapLayerName = mapLayerNameList[index];
                //先隐藏所有层.
                GisUtil.HideAllLayerInMap(projectWindow.mapControl);

                //依靠名字找层, 再显示.
                ILayer layer = GisUtil.getLayerByName(selectedMapLayerName, projectWindow.mapControl);
                layer.Visible = true;
                projectWindow.mapControl.ActiveView.Refresh();
            }
            catch (Exception ex)
            { }
        }

        public ObservableCollection<string> updateVillageNameList(string villageLayerName, string villageLayerFieldName, AxMapControl mapControl)
        {
            ILayer baseLayer = GisUtil.getLayerByName(villageLayerName, mapControl);
            IFeatureLayer fBaseLayer = baseLayer as IFeatureLayer;
            if (fBaseLayer == null)
                return null;
            IFeatureClass fBaseFeatureClass = fBaseLayer.FeatureClass;
            ObservableCollection<string> villageNameList = new ObservableCollection<string>(GisUtil.GetValueListFromFeatureClass(fBaseFeatureClass, villageLayerFieldName));
            if (villageNameList == null)
                return null;
            //在list中去除重复
            villageNameList = new ObservableCollection<string>(villageNameList.Distinct().ToList<string>());
            return villageNameList;
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

        protected void mapMapLayerLabel(ObservableCollection<Label> completeLabelList, ObservableCollection<Label> uncompleteLabelList, string folder, AxMapControl mapControl)
        {
            /*
                匹配成功的图层, 将填写图层名和cpsID字段.
             * 匹配不成功的图层, 将填写图层名字段.
             */

            ObservableCollection<string> cityPlanStandardInfoList = new ObservableCollection<string>();
            ObservableCollection<CityPlanStandard> cityPlanStandardList = CityPlanStandard.GetAllCityPlanStandard();
            //1.把图层名规范的直接做掉.
            for (int i = 0; i < mapLayerNameList.Count; i++)
            {
                string mapLayerName = mapLayerNameList[i];
                bool complete = false;
                Label label = new Label();
                label.mapLayerName = mapLayerName;
                label.mapLayerPath = folder + "\\" + GisUtil.GetShpNameByMapLayerName(mapControl, mapLayerName);

                CityPlanStandard cityPlanStandard = checkMapLayerNameValid(mapLayerName);
                if (cityPlanStandard != null)
                {
                    label.content = cityPlanStandard.getCityPlanStandardInfo();
                    completeLabelList.Add(label);
                    cityPlanStandardInfoList.Add(cityPlanStandard.getCityPlanStandardInfo());
                    complete = true;
                }
                else if(specialLayerNameList.Contains(mapLayerName))
                {
                    complete = true;
                }

                if (!complete)
                {
                    uncompleteLabelList.Add(label);
                }
            }

            foreach (Label rasterLabel in rasterLabelList)
            {
                completeLabelList.Add(rasterLabel);
            }

            foreach (Label label in uncompleteLabelList)
            {
                label.uncomleteLabelContentManager.pruningChooseableCityPlanStandardInfoList(cityPlanStandardInfoList);
                label.uncomleteLabelContentManager.Inited();
            }
        }

        public void loadMap(string path)
        {
            projectWindow.toolbarControl.SetBuddyControl(projectWindow.mapControl);
            if (projectWindow.toolbarControl.Count == 0)
            {
                projectWindow.toolbarControl.AddItem("esriControls.ControlsMapNavigationToolbar");
            }
            projectWindow.mapControl.LoadMxFile(path);
            projectWindow.mapControl.MoveLayerTo(0, projectWindow.mapControl.LayerCount - 1);
            ILayer layer = projectWindow.mapControl.get_Layer(projectWindow.mapControl.LayerCount - 1);
            IEnvelope extent = layer.AreaOfInterest;
            extent.Expand(1, 1, true);
            projectWindow.mapControl.Extent = extent;
            projectWindow.mapControl.ActiveView.Refresh();
        }

        protected void updateMapLayerNameList(ObservableCollection<string> mapLayerNameList, AxMapControl mapControl)
        {
            //每次读取地图, 都要更新图层列表中的图层名.
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
        }

        private void checkMapIntegrity(string path, AxMapControl mapControl)
        {
            if (!GisUtil.CheckMapIntegrity(path, mapControl))
            {
                throw new Exception("地图图层不完整");
            }
        }

        private void checkNecessaryLayer(AxMapControl mapControl)
        {
            List<string> necessaryLayerNameList = new List<string>() 
            {
                "行政村界"
            };

            foreach (string layerName in necessaryLayerNameList)
            {
                if (GisUtil.getLayerByName(layerName, mapControl) == null)
                {
                    throw new Exception("地图缺少行政村街图层");
                }
            }
        }

        private bool convertRasterToPolygon(AxMapControl mapControl, string folder)
        {
            List<IRasterLayer> rasterLayerList = GisUtil.GetRasterLayer(mapControl);
            for (int i = 0; i < rasterLayerList.Count; i++)
            {
                IRasterLayer layer = rasterLayerList[i];
                string layerName = layer.Name;
                string destPath = folder + "\\" + layerName + ".shp";
                GisUtil.RasterToFeature(layer.FilePath, destPath);
                Label label = new Label();
                label.mapLayerName = layerName;
                label.mapLayerPath = destPath;
                label.content = layerName;
                label.isChoosed = true;
                rasterLabelList.Add(label);
            }
            return true;
        }

        public void processFile(string housePath)
        {
            if (!projectWindow.mapControl.CheckMxFile(housePath))
            {
                throw new Exception();
            }

            Init();
            project.path = housePath;
            project.baseMapIndex = -1;
            loadMap(housePath);

            checkMapIntegrity(System.IO.Path.GetDirectoryName(housePath), projectWindow.mapControl);
            checkNecessaryLayer(projectWindow.mapControl);
            convertRasterToPolygon(projectWindow.mapControl, System.IO.Path.GetDirectoryName(housePath));
            foreach (string villageName in updateVillageNameList(BASE_LAYER_NAME, BASE_LAYER_FIELD_NAME, projectWindow.mapControl))
            {
                villageNameList.Add(villageName);
            }
            projectWindow.BaseMapLayerComboBox.SelectedIndex = -1;
            updateMapLayerNameList(mapLayerNameList, projectWindow.mapControl);
            mapMapLayerLabel(completeLabelList, uncompleteLabelList, System.IO.Path.GetDirectoryName(housePath), projectWindow.mapControl);
        }

        public void browseFile(object sender, EventArgs e)
        {
            System.Windows.Forms.OpenFileDialog fileDialog = new System.Windows.Forms.OpenFileDialog();
            fileDialog.InitialDirectory = "C:\\";
            fileDialog.Filter = "mxd files|*.mxd";
            if (fileDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                projectWindow.mask();
                string housePath = fileDialog.FileName;
                try
                {
                    processFile(housePath);
                }
                catch (Exception mapException)
                {
                    Ut.M("地图文件错误, 错误信息: " + mapException.Message);
                    project.path = "";
                    projectWindow.mapControl.ClearLayers();
                }
                finally
                {
                    projectWindow.unmask();
                }
            }
        }
    }
}
