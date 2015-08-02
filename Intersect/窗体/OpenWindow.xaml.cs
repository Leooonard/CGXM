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
using System.Windows.Shapes;
using System.Collections.ObjectModel;

namespace Intersect
{
    /// <summary>
    /// OpenWindow.xaml 的交互逻辑
    /// </summary>
    public partial class OpenWindow : Window
    {
        private ObservableCollection<Project> projectList;
        private CreateProjectWindowWrapper createProjectWindow = null;
        private ModifyProjectWindowWrapper modifyProjectWindow = null;
        private MainWindowWrapper mainWindowWrapper = null;

        public OpenWindow()
        {
            InitializeComponent();
        }

        private void WindowLoaded(object sender, RoutedEventArgs e)
        {
            projectList = Project.GetAllProject();
            ProjectListBox.ItemsSource = projectList;
        }

        private void CreateProjectButtonClick(object sender, RoutedEventArgs e)
        {
            if (createProjectWindow != null && createProjectWindow.isOpen())
                return;

            createProjectWindow = new CreateProjectWindowWrapper(null, delegate(int id) 
                {
                    if (id == C.ERROR_INT)
                        return;
                    Project project = new Project();
                    project.id = id;
                    project.select();
                    projectList.Add(project);
                });
            createProjectWindow.show();
        }

        private void DeleteButtonClick(object sender, MouseButtonEventArgs e)
        {
            Image image = sender as Image;
            StackPanel stackPanel = image.Parent as StackPanel;
            TextBlock textBlock = stackPanel.FindName("ProjectIDTextBlock") as TextBlock;
            int id = Int32.Parse(textBlock.Text);
            if (Ut.C("确认删除该项目?"))
            {
                Project project = new Project();
                project.id = id;
                project.delete();
                foreach (Project proj in projectList)
                {
                    if (proj.id == id)
                    {
                        projectList.Remove(proj);
                        break;
                    }
                }
                Ut.M("删除成功");
            }
        }

        private void ModifyButtonClick(object sender, MouseButtonEventArgs e)
        {
            Image image = sender as Image;
            StackPanel stackPanel = image.Parent as StackPanel;
            TextBlock textBlock = stackPanel.FindName("ProjectIDTextBlock") as TextBlock;
            int projectID = Int32.Parse(textBlock.Text);

            if (modifyProjectWindow != null && modifyProjectWindow.isOpen())
                return;

            modifyProjectWindow = new ModifyProjectWindowWrapper(null, delegate(int id) 
                { 
                    
                }, projectID);
            modifyProjectWindow.show();
        }

        private void ProjectInfoStackPanelMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                if (mainWindowWrapper != null && mainWindowWrapper.isOpen())
                    return;

                StackPanel stackPanel = sender as StackPanel;
                Grid grid = stackPanel.Parent as Grid;
                TextBlock textBlock = grid.FindName("ProjectIDTextBlock") as TextBlock;
                int projectID = Int32.Parse(textBlock.Text);
                mainWindowWrapper = new MainWindowWrapper(projectID);
                mainWindowWrapper.show();
            }
        }
    }
}
