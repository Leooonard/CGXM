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

namespace Intersect
{
    public class MainWindowWrapper
    {
        private AxMapControl mapControl;
        private MainWindow mainWindow;
        private Project project;
        private ObservableCollection<Program> programList;
        private ProgramStepUserControl programStepUserControl;

        public MainWindowWrapper(int projectID)
        {
            project = new Project();
            project.id = projectID;
            project.select();

            Init();
            mainWindow = new MainWindow();
            mainWindow.createProgramButtonClickEventHandler += new EventHandler(delegate(object sender, EventArgs e)
                {
                    createProgram();
                });
            mainWindow.programNameTextBlockMouseDownEventHandler += new MouseButtonEventHandler(delegate(object sender, MouseButtonEventArgs e)
                {
                    if (e.ClickCount == 1)
                    {
                        TextBlock textBlock = sender as TextBlock;
                        Grid grid = textBlock.Parent as Grid;
                        StackPanel stackPanel = grid.Parent as StackPanel;
                        programDetailMode(stackPanel);
                    }
                    else if (e.ClickCount == 2)
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
            program.name = Program.PROGRAM_DEFAULT_NAME;
            program.save();
            program.id = Program.GetLastProgramID();
            programList.Add(program);
        }

        private void programDetailMode(StackPanel parentStackPanel)
        {
            mainWindow.mask();
            Thread t = new Thread(delegate()
            {
                mainWindow.Dispatcher.BeginInvoke((ThreadStart)delegate()
                {
                    System.Threading.Thread.Sleep(5000);

                    mainWindow.LoadMap(project.path); //每次打开时, 重新读一下地图.
                    ProgramStepUserControl programStepUserControl = parentStackPanel.FindName("ProgramStepUserControl") as ProgramStepUserControl;
                    if (programStepUserControl.Visibility == System.Windows.Visibility.Visible)
                    {
                        hideProgramDetailMode(parentStackPanel);
                        this.programStepUserControl = null;
                    }
                    else
                    {
                        showProgramDetailMode(parentStackPanel);
                        this.programStepUserControl = programStepUserControl;
                        TextBlock programIDTextBlock = parentStackPanel.FindName("ProgramIDTextBlock") as TextBlock;
                        int programID = Int32.Parse(programIDTextBlock.Text);
                        initProgramDetailMode(programStepUserControl, programID);
                    }


                    mainWindow.unmask();
                });
            });
            t.Start();
        }

        private void initProgramDetailMode(ProgramStepUserControl programStepUserControl, int programID)
        {
            programStepUserControl.init(programID, mainWindow.mapControl, mainWindow.toolbarControl, mainWindow);
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
            if (mainWindow.checkMap(project.path))
            {
                mainWindow.LoadMap(project.path);
                IFeature baseFeature = GisUtil.GetBaseFeature(mainWindow.mapControl, project.baseMapIndex);
                //移动地图视角.
                IEnvelope extent = baseFeature.Shape.Envelope;
                extent.Expand(1, 1, true);
                mainWindow.mapControl.Extent = extent;
                mainWindow.mapControl.ActiveView.Refresh();

                if (!GisUtil.CheckMapIntegrity(System.IO.Path.GetDirectoryName(project.path), mainWindow.mapControl))
                {
                    Ut.M("地图数据错误");
                    mainWindow.Close();
                }
                mainWindow.ProgramListTitle.Text = String.Format("{0}-{1}", project.name, "方案列表");
                ObservableCollection<Program> tempProgramList = project.getAllRelatedProgram();
                foreach (Program program in tempProgramList)
                {
                    programList.Add(program);
                }
            }
            else
            {
                Ut.M("地图数据错误");
                mainWindow.Close();
            }
        }
    }
}
