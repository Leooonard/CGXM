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
    /// ConfigUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class ConfigUserControl : UserControl
    {
        private bool inited = false;
        private Program program;
        private Project project;
        private AxMapControl mapControl;
        private Intersect.ProgramStepUserControl.OnFinish onFinish;
        public Intersect.ProgramStepUserControl.OnMapControlMouseDown mapControlMouseDown;
        private MainWindow mainWindow;

        public bool valid = false;
        public bool dirty = false;

        public ConfigUserControl()
        {
            InitializeComponent();
        }

        public void init(int programID, AxMapControl mc, Intersect.ProgramStepUserControl.OnFinish of, MainWindow mw)
        {
            inited = true;

            if (program == null)
                program = new Program();
            program.id = programID;
            program.select();

            if (project == null)
                project = new Project();
            project.id = program.projectID;
            project.select();

            NetSizeUserControl.init(program.id);
            ConditionUserControl.init(program.id);

            mapControl = mc;
            onFinish = of;
            mainWindow = mw;

            mapControlMouseDown = null;

            //在初始化时就要对valid进行判断.
            Thread t = new Thread(delegate()
            {
                System.Threading.Thread.Sleep(500);
                Dispatcher.BeginInvoke((ThreadStart)delegate()
                {
                    if (isValid())
                    {
                        valid = true;
                        onFinish(true);
                        SiteSelector siteSelector = new SiteSelector(mapControl, program.id);
                        siteSelector.startSelectSite();
                    }
                });
            });
            t.Start();
        }

        public bool isValid()
        {
            return NetSizeUserControl.isValid() && ConditionUserControl.isValid();
        }

        public bool isDirty()
        {
            return NetSizeUserControl.isDirty() || ConditionUserControl.isDirty();
        }

        private void ConfigGrid_MouseDown(object sender, MouseButtonEventArgs e)
        {

        }

        private void StartCaculateButtonClick(object sender, RoutedEventArgs e)
        {
            if (isValid())
            {
                if (isDirty())
                {
                    if (Ut.C("已对设置做出修改, 继续执行将删除之前的数据, 是否继续?"))
                    {
                        dirty = true;
                    }
                    else
                    {
                        return;
                    }
                }
                mainWindow.mask();
                valid = true;
                onFinish(true);
                save();
                //开始计算.
                SiteSelector siteSelector = new SiteSelector(mapControl, program.id);
                siteSelector.startSelectSite();
                mainWindow.unmask();
            }
            else
            {
                Ut.M("请完整填写信息.");
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
