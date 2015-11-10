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
using System.Windows.Media.Animation;
using System.Threading;

namespace Intersect
{
    /// <summary>
    /// CreateProjectAndMapWindow.xaml 的交互逻辑
    /// </summary>
    public partial class ProjectWindow : Window
    {
        private const int ANIMATION_DURATION = 300;

        public event EventHandler CloseEventHandler;
        public event EventHandler ConfirmEventHandler;
        public event EventHandler BrowseFileEventHandler;
        public event EventHandler comboBoxSelectionChangedEventHandler;
        public event EventHandler uncompleteLabelContentComboBoxTextChangedEventHandler;
        public event EventHandler uncompleteLabelContentComboBoxLostFocusEventHandler;
        public AxMapControl mapControl;
        public AxToolbarControl toolbarControl;

        public ProjectWindow()
        {
            InitializeComponent();

            mapControl = new AxMapControl();
            toolbarControl = new AxToolbarControl();

            windowMapHost.Child = mapControl;
            windowToolbarHost.Child = toolbarControl;
        }

        public void mask()
        {
            ProjectMask.Visibility = System.Windows.Visibility.Visible;
        }

        public void unmask()
        {
            Thread t = new Thread(delegate()
            {
                System.Threading.Thread.Sleep(500);
                this.Dispatcher.BeginInvoke((ThreadStart)delegate()
                {
                    ProjectMask.Visibility = System.Windows.Visibility.Collapsed;
                });
            });
            t.Start();
        }

        public void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.CloseEventHandler(sender, e);
        }

        public void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            this.ConfirmEventHandler(sender, e);
        }

        public void BrowseFileButton_Click(object sender, RoutedEventArgs e)
        {
            this.BrowseFileEventHandler(sender, e);
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            comboBoxSelectionChangedEventHandler(sender, e);
        }

        private void UncompleteLabelContentComboBoxTextChanged(object sender, TextChangedEventArgs e)
        {
            uncompleteLabelContentComboBoxTextChangedEventHandler(sender, e);
        }

        private void UncompleteLabelContentComboBoxLostFocus(object sender, RoutedEventArgs e)
        {
            uncompleteLabelContentComboBoxLostFocusEventHandler(sender, e);
        }
    }
}
