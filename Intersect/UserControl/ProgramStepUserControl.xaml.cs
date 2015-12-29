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
using System.Threading;
using System.Text.RegularExpressions;
using Intersect.Lib;

namespace Intersect
{
    /// <summary>
    /// ProgramStepUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class ProgramStepUserControl : UserControl
    {
        private Program program;
        private AxMapControl mapControl;
        private AxToolbarControl toolbarControl;
        private AxTOCControl tocControl;

        private Dictionary<string, int> TabNameToNumberDict = new Dictionary<string, int>() 
        { 
            { "ConfigTabItem" , 0 },
            { "SiteSelectorTabItem" , 1 },
            { "HousePlacerTabItem" , 2 } 
        };

        public ProgramStepUserControl()
        {
            InitializeComponent();
        }

        public bool isValid()
        {
            return ConfigUserControl.isFinish() && SiteSelectorUserControl.isFinish();
        }

        public void init(int programID, AxMapControl mc, AxToolbarControl tc, AxTOCControl toc)
        {
            NotificationHelper.Register("ConfigUserControlFinish", new NotificationHelper.NotificationEvent(delegate()
            {
                TabItem configTabItem = ProgramTabControl.FindName("ConfigTabItem") as TabItem;
                Grid grid = configTabItem.Header as Grid;
                TextBlock textBlock = grid.Children[0] as TextBlock;

                textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7CFC00"));
            }));
            NotificationHelper.Register("ConfigUserControlUnFinish", new NotificationHelper.NotificationEvent(delegate()
            {
                TabItem configTabItem = ProgramTabControl.FindName("ConfigTabItem") as TabItem;
                Grid grid = configTabItem.Header as Grid;
                TextBlock textBlock = grid.Children[0] as TextBlock;

                textBlock.Foreground = new SolidColorBrush(Colors.Black);
            }));

            NotificationHelper.Register("SiteSelectorUserControlFinish", new NotificationHelper.NotificationEvent(delegate()
            {
                TabItem configTabItem = ProgramTabControl.FindName("SiteSelectorTabItem") as TabItem;
                Grid grid = configTabItem.Header as Grid;
                TextBlock textBlock = grid.Children[0] as TextBlock;

                textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7CFC00"));
            }));
            NotificationHelper.Register("SiteSelectorUserControlUnFinish", new NotificationHelper.NotificationEvent(delegate()
            {
                TabItem configTabItem = ProgramTabControl.FindName("SiteSelectorTabItem") as TabItem;
                Grid grid = configTabItem.Header as Grid;
                TextBlock textBlock = grid.Children[0] as TextBlock;

                textBlock.Foreground = new SolidColorBrush(Colors.Black);
            }));

            NotificationHelper.Register("HousePlacerUserControlFinish", new NotificationHelper.NotificationEvent(delegate()
            {
                TabItem housePlacerTabItem = ProgramTabControl.FindName("HousePlacerTabItem") as TabItem;
                Grid grid = housePlacerTabItem.Header as Grid;
                TextBlock textBlock = grid.Children[0] as TextBlock;

                textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7CFC00"));
            }));
            NotificationHelper.Register("HousePlacerUserControlUnFinish", new NotificationHelper.NotificationEvent(delegate()
            {
                TabItem housePlacerTabItem = ProgramTabControl.FindName("HousePlacerTabItem") as TabItem;
                Grid grid = housePlacerTabItem.Header as Grid;
                TextBlock textBlock = grid.Children[0] as TextBlock;

                textBlock.Foreground = new SolidColorBrush(Colors.Black);
            }));

            NotificationHelper.Register("SiteSelectorUserControlRefresh", new NotificationHelper.NotificationEvent(delegate()
            {
                SiteSelectorUserControl.clear();
                SiteSelectorUserControl.refresh();

                HousePlacerUserControl.clear();
                HousePlacerUserControl.refresh();
            }));

            NotificationHelper.Register("HousePlacerUserControlRefresh", new NotificationHelper.NotificationEvent(delegate()
            {
                HousePlacerUserControl.clear();
                HousePlacerUserControl.refresh();
            }));

            program = new Program();
            program.id = programID;
            program.select();

            mapControl = mc;
            toolbarControl = tc;
            tocControl = toc;

            ConfigUserControl.init(programID, mapControl, tocControl);
            SiteSelectorUserControl.init(programID, mapControl, toolbarControl);
            HousePlacerUserControl.init(programID, mapControl);
        }

        public void clear()
        { 
        }

        public void refresh()
        {
            ConfigUserControl.refresh();
            SiteSelectorUserControl.refresh();
            HousePlacerUserControl.refresh();
        }

        private void ConfigGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid grid = sender as Grid;
            bool result = onTabChange(grid, e);
            if (!result)
            {
                Tool.M("请先完成评价计算步骤.");
            }
        }

        private void SiteSelectorGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid grid = sender as Grid;
            bool result = onTabChange(grid, e);
            if (!result)
            {
                Tool.M("请先完成评价计算步骤.");
            }
        }

        private void HousePlacerGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid grid = sender as Grid;
            bool result = onTabChange(grid, e);
            if (result)
            {
                //HousePlacerUserControl.initAxComponents();
                SiteSelectorUserControl.SelectVillageUserControl.transparentVillage();
            }
            else
            {
                Tool.M("请先完成主路和内部路规划.");
            }
        }

        private bool onTabChange(Grid grid, MouseButtonEventArgs e)
        {
            TabItem tabItem = grid.Parent as TabItem;
            TabControl tabControl = tabItem.Parent as TabControl;
            int result = TabChange(tabControl, tabItem.Name);
            if (result == Const.ERROR_INT)
            {
                e.Handled = true;
                return false;
            }
            return true;
        }

        public delegate bool OnMapControlMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e);
        public void mapControlMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            bool handled = false;
            if (ConfigUserControl.mapControlMouseDown != null)
            {
                handled = ConfigUserControl.mapControlMouseDown(sender, e);
                if(handled)
                    return;
            }
            if (SiteSelectorUserControl.mapControlMouseDown != null)
            {
                handled = SiteSelectorUserControl.mapControlMouseDown(sender, e);
                if(handled)
                    return;
            }
        }

        private int TabChange(TabControl tabControl, string tabName)
        {
            for (int i = 0; i < tabControl.Items.Count; i++)
            {
                TabItem tabItem = tabControl.Items[i] as TabItem;
                if (tabItem.IsSelected)
                {
                    if (TabNameToNumberDict[tabName] - TabNameToNumberDict[tabItem.Name] <= 0)
                    {
                        return 0;
                    }
                    if (TabNameToNumberDict[tabName] - TabNameToNumberDict[tabItem.Name] > 1)
                    {
                        if (!ConfigUserControl.isFinish() || !SiteSelectorUserControl.isFinish())
                        {
                            return Const.ERROR_INT;
                        }
                        else
                        {
                            return 0;
                        }
                    }
                    switch (tabItem.Name)
                    {
                        case "ConfigTabItem":
                            if (!ConfigUserControl.isFinish())
                            {
                                return Const.ERROR_INT;
                            }
                            break;
                        case "SiteSelectorTabItem":
                            if (!SiteSelectorUserControl.isFinish())
                            {
                                return Const.ERROR_INT;
                            }
                            break;
                        case "HousePlacerTabItem":
                            break;
                        default:
                            return Const.ERROR_INT;
                    }
                }
            }
            return 0;
        }
    }
}
