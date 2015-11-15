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
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using Intersect.Lib;

namespace Intersect
{
    /// <summary>
    /// ConfigUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigUserControl : UserControl
    {
        private Program program;
        private Project project;
        private AxMapControl mapControl;
        public Intersect.ProgramStepUserControl.OnMapControlMouseDown mapControlMouseDown;

        public bool finish;

        public ConfigUserControl()
        {
            InitializeComponent();
        }

        public void unInit()
        {
            NetSizeUserControl.unInit();
            ConditionUserControl.unInit();
        }

        public void init(int programID, AxMapControl mc)
        {
            program = new Program();                
            program.id = programID;
            program.select();

            project = new Project();                
            project.id = program.projectID;
            project.select();

            NetSizeUserControl.init(program.id);
            ConditionUserControl.init(program.id);

            mapControl = mc;
            mapControlMouseDown = null;
            finish = false;

            //在初始化时就要对valid进行判断.
            Thread t = new Thread(delegate()
            {
                System.Threading.Thread.Sleep(500);
                Dispatcher.BeginInvoke((ThreadStart)delegate()
                {
                    if (isValid())
                    {
                        finish = true;
                        NotificationHelper.Trigger("ConfigUserControlFinish");
                        SiteSelector siteSelector = new SiteSelector(mapControl, program.id);
                        try
                        {
                            siteSelector.addShapeFile("评价结果.shp", "评价结果");
                        }
                        catch(Exception shpFileException)
                        {
                            //可能存在数据库中数据正确, 但是shpfile不见的情况. 这种情况下, 重新计算一遍.
                            siteSelector.startSelectSite();
                        }
                    }
                });
            });
            t.Start();
        }

        public bool isFinish()
        {
            return finish;
        }

        private bool isValid()
        {
            return NetSizeUserControl.isValid() && ConditionUserControl.isValid();
        }

        private void StartCaculateButtonClick(object sender, RoutedEventArgs e)
        {
            if (isValid())
            {
                NotificationHelper.Trigger("mask");
                if (!Tool.C("重新计算将导致已有结果丢失，是否继续？"))
                {
                    NotificationHelper.Trigger("unmask");
                    return;
                }

                finish = true;
                NotificationHelper.Trigger("ConfigUserControlFinish");
                NotificationHelper.Trigger("ConfigUserControlRefresh");
                save();

                //开始计算.
                SiteSelector siteSelector = new SiteSelector(mapControl, program.id);
                bool success = siteSelector.startSelectSite();
                if (!success)
                {
                    //计算结果失败.
                    Tool.M("评价失败, 该地区没有符合条件的结果。");
                    finish = false;
                    NotificationHelper.Trigger("ConfigUserControlUnFinish");
                }
                NotificationHelper.Trigger("unmask");
            }
            else
            {
                Tool.M("请完整填写信息.");
                return;
            }
        }

        private void save()
        {
            NetSizeUserControl.save();
            ConditionUserControl.save();
        }
    }
}
