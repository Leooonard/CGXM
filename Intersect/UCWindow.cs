using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Intersect
{
    public class UCWindow
    {
        protected Rectangle maskRectangle = new Rectangle { Fill = new SolidColorBrush(Colors.Black), Opacity = 0.3 };
        public FrameworkElement parent
        {
            get;
            set;
        }
        public FrameworkElement content
        {
            get;
            set;
        }

        private Grid GetRootGrid()
        {
            FrameworkElement root = parent;

            while (root is FrameworkElement && root.Parent != null)
            {
                FrameworkElement rootElement = root as FrameworkElement;

                if (rootElement.Parent is FrameworkElement)
                {
                    root = rootElement.Parent as FrameworkElement;
                }
            }

            ContentControl contentControl = root as ContentControl;
            return contentControl.Content as Grid;
        }

        public void show()
        {
            Grid grid = GetRootGrid();

            if (grid != null)
            {
                grid.Children.Add(maskRectangle);
                if (grid.RowDefinitions.Count > 0)
                {
                    Grid.SetRowSpan(maskRectangle, grid.RowDefinitions.Count);
                }
                if (grid.ColumnDefinitions.Count > 0)
                {
                    Grid.SetColumnSpan(maskRectangle, grid.ColumnDefinitions.Count);
                }

                grid.Children.Add(content);
                if (grid.RowDefinitions.Count > 0)
                {
                    Grid.SetRowSpan(content, grid.RowDefinitions.Count);
                }
                if (grid.ColumnDefinitions.Count > 0)
                {
                    Grid.SetColumnSpan(content, grid.ColumnDefinitions.Count);
                }
            }
        }

        public void close()
        {
            Grid grid = GetRootGrid();
            if (grid != null)
            {
                grid.Children.Remove(maskRectangle);
                grid.Children.Remove(content);
            }
        }
        
    }
}
