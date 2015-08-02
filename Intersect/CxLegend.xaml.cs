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

namespace Intersect
{
    /// <summary>
    /// CxLegend.xaml 的交互逻辑
    /// </summary>
    public partial class CxLegend : UserControl
    {
        /// <summary>
        /// Parent Window
        /// </summary>
        private Window _thisParent = new Window();

        /// <summary>
        /// Map State
        /// </summary>

        /// <summary>
        /// Parent Window Attribute
        /// </summary>
        public Window ThisParent
        {
            get { return _thisParent; }

        }

        public CxLegend()
        {
            InitializeComponent();
            SetWindowProperty();
        }


        /// <summary>
        /// Show Legend
        /// </summary>
        /// <author>Shen Yongyuan</author>
        /// <date>20091111</date>
        public void Show(Window owner)
        {
            this.ThisParent.Show();
            this.ThisParent.Left = owner.Left + owner.ActualWidth - 310;
            this.ThisParent.Top = owner.Top + owner.ActualHeight - 210;
        }

        /// <summary>
        /// Show Modal Legend
        /// </summary>
        /// <author>Shen Yongyuan</author>
        /// <date>20091111</date>
        public void ShowDialog()
        {
            this.ThisParent.ShowDialog();
        }

        /// <summary>
        /// Set Parent Window Attribute
        /// </summary>
        /// <author>Shen Yongyuan</author>
        /// <date>20091111</date>
        private void SetWindowProperty()
        {
            this.ThisParent.AllowsTransparency = true;
            this.ThisParent.WindowStartupLocation = WindowStartupLocation.Manual;
            this.ThisParent.WindowStyle = WindowStyle.None;
            this.ThisParent.Topmost = true;
            this.ThisParent.ShowInTaskbar = false;
            this.ThisParent.Background = null;
            this.ThisParent.SizeToContent = SizeToContent.WidthAndHeight;
            this.ThisParent.Content = this;
            this.ThisParent.MouseLeftButtonDown += new System.Windows.Input.MouseButtonEventHandler(Window1_MouseLeftButtonDown);
        }

        /// <summary>
        /// Mouse Left Button to Drag Window
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <author>Shen Yongyuan</author>
        /// <date>20091111</date>
        void Window1_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            (sender as Window).DragMove();
        }

        /// <summary>
        /// Close Button Click
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <author>Shen Yongyuan</author>
        /// <date>20091111</date>
        private void closeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.ThisParent.Close();
        }

        /// <summary>
        /// Set Legend
        /// </summary>
        /// <param name="legendDict"></param>
        /// <author>Shen Yongyuan</author>
        /// <date>20091111</date>
        public void SetLegend(Dictionary<string, Brush> legendDict)
        {
            LegendPanel.Children.Clear();


            int tag = 1;


            foreach (KeyValuePair<string, Brush> l in legendDict)
            {
                TextBlock text = new TextBlock();
                text.FontSize = 20;
                text.FontWeight = FontWeights.Bold;
                text.Text = l.Key;
                text.Background = l.Value;
                //text.MouseDown += new MouseButtonEventHandler(text_MouseDown);
                text.Tag = tag.ToString();
                LegendPanel.Children.Add(text);
                tag++;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="legendDict"></param>
        /// <param name="state"></param>
        /// <param name="year"></param>
        //public void SetLegend(Dictionary<string, Brush> legendDict)
        //{
        //    LegendPanel.Children.Clear();


        //    int tag = 1;

        //    textContent.Text = "图例";

        //    foreach (KeyValuePair<string, Brush> l in legendDict)
        //    {
        //        TextBlock text = new TextBlock();
        //        text.FontSize = 20;
        //        text.FontWeight = FontWeights.Bold;
        //        text.TextAlignment = TextAlignment.Center;
        //        text.Text = l.Key;
        //        text.Background = l.Value;
        //        //text.MouseDown += new MouseButtonEventHandler(text_MouseDown);
        //        text.Tag = tag.ToString();
        //        LegendPanel.Children.Add(text);
        //        tag++;
        //    }
        //}
        /// <summary>
        /// 
        /// </summary>
        /// <param name="legendDict"></param>
        /// <param name="state"></param>
        /// <param name="year"></param>
        public void SetLegend(String[] texts,Brush[] background)
        {
            LegendPanel.Children.Clear();


            int tag = 1;


            for (int i = 0; i < texts.Length;i++ )
            {
                TextBlock text = new TextBlock();
                text.FontSize = 16;
                text.FontWeight = FontWeights.Bold;
                text.TextAlignment = TextAlignment.Center;
                text.Text = texts[i];
                text.TextWrapping = TextWrapping.Wrap;
                text.Background = background[i];
                //text.MouseDown += new MouseButtonEventHandler(text_MouseDown);
                text.Tag = tag.ToString();
                LegendPanel.Children.Add(text);
                tag++;
            }
        }

        /// <summary>
        /// Show Reference Text
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <author>Shen Yongyuan</author>
        /// <date>20091111</date>
        /*void text_MouseDown(object sender, MouseButtonEventArgs e)
        {
            short rankValue = Convert.ToInt16((sender as TextBlock).Tag);
            string remark = Town.Common.Remark.GetRemark(mapState, rankValue);
            CxExplain.getInstance().SetContent(remark);
            CxExplain.getInstance().Show();
            
        }*/
    }
}
