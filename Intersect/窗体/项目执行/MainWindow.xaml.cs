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

        public event EventHandler createProgramButtonClickEventHandler;
        public event EventHandler deleteProgramButtonClickEventHandler;
        public event MouseButtonEventHandler programNameTextBlockMouseDownEventHandler;
        public event EventHandler programNameButtonClickEventHandler;
        
        public MainWindow()
        {
            InitializeComponent();

            mapControl = new AxMapControl();
            toolbarControl = new AxToolbarControl();
            tocControl = new AxTOCControl();

            //注册mask，unmask事件。
            NotificationHelper.Register("mask", new NotificationHelper.NotificationEvent(delegate()
                {
                    ProgramListMask.Visibility = System.Windows.Visibility.Visible;
                }));
            NotificationHelper.Register("unmask", new NotificationHelper.NotificationEvent(delegate() 
                {
                    ProgramListMask.Visibility = System.Windows.Visibility.Collapsed;
                }));
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

        private void CreateProgramButtonClick(object sender, RoutedEventArgs e)
        {
            createProgramButtonClickEventHandler(sender, e);
        }

        private void DeleteProgramButton_Click(object sender, RoutedEventArgs e)
        {
            deleteProgramButtonClickEventHandler(sender, e);
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
    }
}
