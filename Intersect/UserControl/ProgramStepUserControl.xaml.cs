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

namespace Intersect
{
    /// <summary>
    /// ProgramStepUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class ProgramStepUserControl : UserControl
    {
        private Program program;
        private bool inited = false;
        private AxMapControl mapControl;
        private AxToolbarControl toolbarControl;

        private Dictionary<string, int> TabNameToNumberDict = new Dictionary<string, int>() 
        { 
            { "ConfigTabItem" , 0 },
            { "SiteSelectorTabItem" , 1 },
            { "HousePlacerTabItem" , 2 } 
        };
        public delegate void OnFinish(bool finish); //子控件使用这个函数通知父控件, 自身已经填写完毕, 父控件帮助子控件改变tab header的样式.

        public ProgramStepUserControl()
        {
            InitializeComponent();
        }

        public bool isValid()
        {
            return ConfigUserControl.isValid() && SiteSelectorUserControl.isValid();
        }

        public bool isDirty()
        {
            return ConfigUserControl.isDirty() || SiteSelectorUserControl.isDirty();
        }

        public void init(int programID, AxMapControl mc, AxToolbarControl tc)
        {
            inited = true;

            program = new Program();
            program.id = programID;
            program.select();

            mapControl = mc;
            toolbarControl = tc;

            OnFinish configUserControlOnFinish = delegate(bool finish)
            {
                TabItem configTabItem = ProgramTabControl.FindName("ConfigTabItem") as TabItem;
                Grid grid = configTabItem.Header as Grid;
                TextBlock textBlock = grid.Children[0] as TextBlock;
                if(finish)
                {
                    textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7CFC00"));                    
                }
                else
                {
                    textBlock.Foreground = new SolidColorBrush(Colors.Black);                                        
                }
            };
            ConfigUserControl.init(programID, mapControl, configUserControlOnFinish);

            OnFinish siteSelectorUserControlOnFinish = delegate(bool finish)
            {
                TabItem configTabItem = ProgramTabControl.FindName("SiteSelectorTabItem") as TabItem;
                Grid grid = configTabItem.Header as Grid;
                TextBlock textBlock = grid.Children[0] as TextBlock;
                if (finish)
                {
                    textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7CFC00"));
                }
                else
                {
                    textBlock.Foreground = new SolidColorBrush(Colors.Black);
                }
            };
            SiteSelectorUserControl.init(programID, siteSelectorUserControlOnFinish, mapControl, toolbarControl);
            OnFinish housePlacerUserControl = delegate(bool finish)
            {
                TabItem housePlacerTabItem = ProgramTabControl.FindName("HousePlacerTabItem") as TabItem;
                Grid grid = housePlacerTabItem.Header as Grid;
                TextBlock textBlock = grid.Children[0] as TextBlock;
                if (finish)
                {
                    textBlock.Foreground = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#7CFC00"));
                }
                else
                {
                    textBlock.Foreground = new SolidColorBrush(Colors.Black);
                }
            };
            HousePlacerUserControl.init(programID, mapControl, housePlacerUserControl);
        }

        private void ConfigGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid grid = sender as Grid;
            onTabChange(grid, e);
        }

        private void SiteSelectorGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid grid = sender as Grid;
            onTabChange(grid, e);
        }

        private void HousePlacerGridMouseDown(object sender, MouseButtonEventArgs e)
        {
            Grid grid = sender as Grid;
            bool result = onTabChange(grid, e);
            if (result)
            {
                Thread t = new Thread(delegate()
                {
                    System.Threading.Thread.Sleep(1000);
                    this.Dispatcher.BeginInvoke((ThreadStart)delegate()
                    {
                        HousePlacerUserControl.initAxComponents();
                    });
                });
                t.Start();
            }
        }

        private bool onTabChange(Grid grid, MouseButtonEventArgs e)
        {
            TabItem tabItem = grid.Parent as TabItem;
            TabControl tabControl = tabItem.Parent as TabControl;
            int result = TabChange(tabControl, tabItem.Name);
            if (result == C.ERROR_INT)
            {
                Ut.M("有错误");
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
                        return 0;
                    if (TabNameToNumberDict[tabName] - TabNameToNumberDict[tabItem.Name] > 1)
                        return C.ERROR_INT;
                    switch (tabItem.Name)
                    {
                        case "ConfigTabItem":
                            if (ConfigUserControl.valid)
                            {
                                if (ConfigUserControl.dirty)
                                {
                                    //删除其他两个tab下的数据.
                                    SiteSelectorUserControl.delete();
                                    SiteSelectorUserControl.reInit();
                                    //这里不用为houseplacerUsercontrol做删除, 因为数据已经被级联删除了. 只做reinit即可.
                                    HousePlacerUserControl.reInit();
                                    ConfigUserControl.dirty = false;
                                }
                            }
                            else
                                return C.ERROR_INT;
                            break;
                        case "SiteSelectorTabItem":
                            if (SiteSelectorUserControl.valid)
                            {
                                if (SiteSelectorUserControl.dirty)
                                {
                                    //删除最后一个tab下的数据.
                                    HousePlacerUserControl.delete();
                                    HousePlacerUserControl.reInit();
                                    SiteSelectorUserControl.dirty = false;
                                }
                                else
                                {
                                    HousePlacerUserControl.prePlace();
                                }
                            }
                            else
                                return C.ERROR_INT;
                            break;
                        case "HousePlacerTabItem":
                            break;
                        default:
                            return C.ERROR_INT;
                    }
                }
            }
            return 0;
        }
    }
}
