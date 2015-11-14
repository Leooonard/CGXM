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
using System.Windows.Media.Animation;
using Intersect.Lib;

namespace Intersect
{
    /// <summary>
    /// Pager.xaml 的交互逻辑
    /// </summary>
    public partial class Pager : UserControl
    {
        private const double ANIMATION_DURATION = 150;

        public int nowStep = 1;
        public int totalStep = 0;
        private string stepInfo
        {
            get
            {
                return String.Format("{0}/{1}", nowStep, totalStep);
            }
        }
        public event EventHandler previewStepButtonClick = null;
        public event EventHandler nextStepButtonClick = null;
        public double pagerWidth
        {
            get
            {
                return PagerGrid.Width;
            }
            set
            {
                PagerGrid.Width = value;
            }
        }

        public delegate bool checkValid();
        public checkValid previewStepButtonCheck = null;
        public checkValid nextStepButtonCheck = null;

        public Pager()
        {
            InitializeComponent();
        }

        public void update()
        {
            PreviewStepButton.IsEnabled = true;
            NextStepButton.IsEnabled = true;
            if (nowStep == 1)
            {
                PreviewStepButton.IsEnabled = false;
            }
            if (nowStep == totalStep)
            {
                NextStepButton.IsEnabled = false;
            }
            if (nowStep < 1 || nowStep > totalStep)
            {
                PreviewStepButton.IsEnabled = false;
                NextStepButton.IsEnabled = false;
                StepInfoTextBlock.Text = "错误";
            }
            else
            {
                StepInfoTextBlock.Text = stepInfo;
                double totalWidth = 0;
                totalWidth = PagerGrid.Width;
                StepRectangle.Width = (double)nowStep / (double)totalStep * totalWidth;
            }
        }

        private void PreviewStepButton_Click(object sender, RoutedEventArgs e)
        {
            int lastStep = 0, newStep = 0;
            double totalWidth = 0;
            totalWidth = PagerGrid.ActualWidth;
            if (nowStep <= 1)
            {
                PreviewStepButton.IsEnabled = false;
                return;
            }
            if (previewStepButtonCheck != null && !previewStepButtonCheck())
                return;
            if(nowStep == 2)
                PreviewStepButton.IsEnabled = false;
            lastStep = nowStep;
            nowStep--;
            newStep = nowStep;
            StepInfoTextBlock.Text = stepInfo;
            NextStepButton.IsEnabled = true;
            AnimationHelper.startDoubleAnimation((double)lastStep / (double)totalStep * totalWidth, (double)newStep / (double)totalStep * totalWidth, ANIMATION_DURATION, StepRectangle, new PropertyPath("Width"));
            if(previewStepButtonClick != null)
                previewStepButtonClick(sender, e);
        }

        private void NextStepButton_Click(object sender, RoutedEventArgs e)
        {
            int lastStep = 0, newStep = 0;
            double totalWidth = 0;
            totalWidth = PagerGrid.ActualWidth;
            if (nowStep >= totalStep)
            {
                NextStepButton.IsEnabled = false;
                return;
            }
            if (nextStepButtonCheck != null && !nextStepButtonCheck())
                return;
            if (nowStep == totalStep - 1)
                NextStepButton.IsEnabled = false;
            lastStep = nowStep;
            nowStep++;
            newStep = nowStep;
            StepInfoTextBlock.Text = stepInfo;
            PreviewStepButton.IsEnabled = true;
            AnimationHelper.startDoubleAnimation((double)lastStep / (double)totalStep * totalWidth, (double)newStep / (double)totalStep * totalWidth, ANIMATION_DURATION, StepRectangle, new PropertyPath("Width"));
            if(nextStepButtonClick != null)
                nextStepButtonClick(sender, e);
        }
    }
}
