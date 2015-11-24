using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows;
using System.Windows.Media;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geometry;
using System.Threading;
using ESRI.ArcGIS.Geodatabase;
using System.Text.RegularExpressions;
using Intersect.Lib;
using System.IO;

namespace Intersect
{
    public class MainWindowHelper
    {
        private AxMapControl mapControl;
        private MainWindow mainWindow;
        private Project project;
        private ObservableCollection<Program> programList;
        private ProgramStepUserControl programStepUserControl;

        public static string PROJECT_FOLDER_NAME; //进入mainwindow后，项目文件夹名也就确定了，需要暴露出去。

        public MainWindowHelper(int projectID)
        {
            project = new Project();
            project.id = projectID;
            project.select();

            PROJECT_FOLDER_NAME = Regex.Replace(project.name, @"\s+", "_");

            Init();
            mainWindow = new MainWindow();
            mainWindow.createProgramButtonClickEventHandler += new EventHandler(delegate(object sender, EventArgs e)
                {
                    createProgram();
                });

            mainWindow.deleteProgramButtonClickEventHandler += new EventHandler(delegate(object sender, EventArgs e)
                {
                    deleteProgram(sender, e);
                });
            mainWindow.programNameTextBlockMouseDownEventHandler += new MouseButtonEventHandler(delegate(object sender, MouseButtonEventArgs e)
                {
                    if (e.ClickCount == 2)
                    {
                        TextBlock textBlock = sender as TextBlock;
                        Grid grid = textBlock.Parent as Grid;
                        StackPanel stackPanel = grid.Parent as StackPanel;
                        programDetailMode(stackPanel);
                    }
                    else if (e.ClickCount == 3)
                    {
                        TextBlock textBlock = sender as TextBlock;
                        Grid grid = textBlock.Parent as Grid;
                        programNameInputMode(grid);
                    }
                });
            mainWindow.programNameButtonClickEventHandler += new EventHandler(delegate(object sender, EventArgs e)
                {
                    Button button = sender as Button;
                    Grid grid = button.Parent as Grid;
                    grid = grid.Parent as Grid;
                    programNameViewMode(grid);
                });
            mainWindow.ProgramList.ItemsSource = programList;

            mainWindow.mapControl.OnMouseDown += mapControlMouseDown;
        }

        private void mapControlMouseDown(object sender, IMapControlEvents2_OnMouseDownEvent e)
        {
            if (programStepUserControl != null)
                programStepUserControl.mapControlMouseDown(sender, e);
        }

        private void createProgram()
        {
            Program program = new Program();
            program.projectID = project.id;
            program.save(new List<string>() { "name", "path"});
            program.id = Program.GetLastProgramID();
            program.name = "方案#" + program.id.ToString();

            //再创建工作目录.
            int randomCount = 0;
            string folderName = String.Format("{0}_{1}", program.name, Tool.random(10));
            while (Directory.Exists(System.IO.Path.Combine(
                project.path,
                Const.PROGRAMS_FOLDER_NAME,
                folderName
            )))
            {
                if (++randomCount > 20)
                {
                    Tool.M("工作目录创建失败。");
                    program.delete();
                    return;
                }
                folderName = String.Format("{0}_{1}", program.name, Tool.random(10));
            }
            string programPath = System.IO.Path.Combine(
                project.path,
                Const.PROGRAMS_FOLDER_NAME,
                folderName
            );
            Directory.CreateDirectory(programPath);

            //再写入path属性
            program.path = programPath;

            program.update();
            programList.Add(program);
        }

        //先只删除数据库，不删除文件夹。文件夹删了会报错，以后找解决方案。
        private void deleteProgram(object sender, EventArgs e)
        {
            if (!Tool.C("确定删除该方案？"))
            {
                return;
            }
            Image deleteProgramButton = sender as Image;
            int programID = Int32.Parse(deleteProgramButton.Tag.ToString());
            foreach (Program program in programList)
            {
                if (program.id == programID)
                {
                    program.delete();
                    programList.Remove(program);
                    return;
                }
            }

        }

        private void programDetailMode(StackPanel parentStackPanel)
        {
            NotificationHelper.Trigger("mask");
            Thread t = new Thread(delegate()
            {
                mainWindow.Dispatcher.BeginInvoke((ThreadStart)delegate()
                {
                    System.Threading.Thread.Sleep(500);

                    string mapPath = FileHelper.FindExtension(System.IO.Path.Combine(project.path, Const.SOURCE_FOLDER_NAME), ".mxd");
                    mainWindow.LoadMap(mapPath); //每次打开时, 重新读一下地图.
                    IFeature baseFeature = GisTool.GetBaseFeature(mainWindow.mapControl, project.baseMapIndex);
                    GisTool.ExpandToMapView(baseFeature, mainWindow.mapControl);

                    ProgramStepUserControl programStepUserControl = parentStackPanel.FindName("ProgramStepUserControl") as ProgramStepUserControl;
                    if (programStepUserControl.Visibility == System.Windows.Visibility.Visible)
                    {
                        hideProgramDetailMode(parentStackPanel);
                        //programStepUserControl.clear();
                        programStepUserControl = null;
                    }
                    else
                    {
                        showProgramDetailMode(parentStackPanel);
                        this.programStepUserControl = programStepUserControl;
                        TextBlock programIDTextBlock = parentStackPanel.FindName("ProgramIDTextBlock") as TextBlock;
                        int programID = Int32.Parse(programIDTextBlock.Text);

                        IFeatureClass baseFeatureClass = GisTool.getFeatureClass(mainWindow.mapControl, Const.BASE_LAYER_NAME);
                        string villageName = GisTool.getValueFromFeatureClass(baseFeatureClass, project.baseMapIndex, Const.BASE_FIELD_NAME);
                        ILayer layer = GisTool.getLayerByName(Const.BASE_LAYER_NAME, mainWindow.mapControl);
                        GisTool.HighlightFeature(layer, String.Format("Name='{0}'", villageName), mainWindow.mapControl);
                        initProgramDetailMode(programStepUserControl, programID);
                    }

                    NotificationHelper.Trigger("unmask");
                });
            });
            t.Start();
        }

        private void initProgramDetailMode(ProgramStepUserControl programStepUserControl, int programID)
        {
            programStepUserControl.init(programID, mainWindow.mapControl, mainWindow.toolbarControl, mainWindow.tocControl);
        }

        private void showProgramDetailMode(StackPanel programStackPanel)
        {
            TextBlock programIDTextBlock = programStackPanel.FindName("ProgramIDTextBlock") as TextBlock;
            int programID = Int32.Parse(programIDTextBlock.Text);
            foreach (Program program in programList)
            {
                if (program.id == programID)
                    program.visible = true;
                else
                    program.visible = false;
            }
        }

        private void hideProgramDetailMode(StackPanel programStackPanel)
        {
            TextBlock programIDTextBlock = programStackPanel.FindName("ProgramIDTextBlock") as TextBlock;
            int programID = Int32.Parse(programIDTextBlock.Text);
            foreach (Program program in programList)
            {
                if (program.id == programID)
                    program.visible = false;
            }
        }

        private void programNameInputMode(Grid parentGrid)
        { 
            //切换到输入方案名的模式.
            TextBlock programNameTextBlock = parentGrid.FindName("ProgramNameTextBlock") as TextBlock;
            Grid programNameGrid = parentGrid.FindName("ProgramNameGrid") as Grid;
            programNameTextBlock.Visibility = System.Windows.Visibility.Collapsed;
            programNameGrid.Visibility = System.Windows.Visibility.Visible;
            StackPanel stackPanel = parentGrid.Parent as StackPanel;
            TextBlock programIDTextBlock = stackPanel.FindName("ProgramIDTextBlock") as TextBlock;
            int programID = Int32.Parse(programIDTextBlock.Text);
            foreach (Program program in programList)
            {
                if (program.id == programID)
                {
                    TextBox programNameTextBox = programNameGrid.FindName("ProgramNameTextBox") as TextBox;
                    programNameTextBox.Text = program.name;
                    break;
                }
            }
        }

        private void programNameViewMode(Grid parentGrid)
        {
            //切换到查看方案名的模式.
            TextBlock programNameTextBlock = parentGrid.FindName("ProgramNameTextBlock") as TextBlock;
            Grid programNameGrid = parentGrid.FindName("ProgramNameGrid") as Grid;
            TextBox programNameTextBox = programNameGrid.FindName("ProgramNameTextBox") as TextBox;
            string name = programNameTextBox.Text;
            programNameTextBlock.Visibility = System.Windows.Visibility.Visible;
            programNameGrid.Visibility = System.Windows.Visibility.Collapsed;
            StackPanel stackPanel = parentGrid.Parent as StackPanel;
            TextBlock programIDTextBlock = stackPanel.FindName("ProgramIDTextBlock") as TextBlock;
            int programID = Int32.Parse(programIDTextBlock.Text);
            foreach (Program program in programList)
            {
                if (program.id == programID)
                {
                    program.name = name;
                    program.update();
                    break;
                }
            }
        }

        public bool isOpen()
        {
            return mainWindow.IsVisible;
        }

        private void Init()
        {
            if (programList == null)
                programList = new ObservableCollection<Program>();
            else
                programList.Clear();
        }

        public void show()
        {
            mainWindow.Show();
            string mxdPath = FileHelper.FindExtension(System.IO.Path.Combine(project.path, Const.SOURCE_FOLDER_NAME), ".mxd");
            if (mainWindow.checkMap(mxdPath))
            {
                mainWindow.LoadMap(mxdPath);
                IFeature baseFeature = GisTool.GetBaseFeature(mainWindow.mapControl, project.baseMapIndex);
                GisTool.ExpandToMapView(baseFeature, mainWindow.mapControl);

                mainWindow.ProgramListTitle.Text = String.Format("{0}-{1}", project.name, "方案列表");
                ObservableCollection<Program> tempProgramList = project.getAllRelatedProgram();
                foreach (Program program in tempProgramList)
                {
                    programList.Add(program);
                }
            }
            else
            {
                Tool.M("地图数据错误");
                mainWindow.Close();
            }
        }

        


    }
}
