using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Animation;
using System.Windows;

namespace Intersect.Lib
{
    public class AnimationHelper
    {
        public static void startDoubleAnimation(double from, double to, double duration, DependencyObject obj, PropertyPath path)
        {
            DoubleAnimation doubleAnimation = new DoubleAnimation();
            doubleAnimation.From = from;
            doubleAnimation.To = to;
            doubleAnimation.Duration = TimeSpan.FromMilliseconds(duration);
            Storyboard.SetTarget(doubleAnimation, obj);
            Storyboard.SetTargetProperty(doubleAnimation, path);
            Storyboard storyBoard = new Storyboard();
            storyBoard.Children.Add(doubleAnimation);
            storyBoard.Begin();
        }

        public static void startThicknessAnimation(Thickness from, Thickness to, double duration, DependencyObject obj, PropertyPath path)
        {
            ThicknessAnimation thicknessAnimation = new ThicknessAnimation();
            thicknessAnimation.From = from;
            thicknessAnimation.To = to;
            thicknessAnimation.Duration = TimeSpan.FromMilliseconds(duration);
            Storyboard.SetTarget(thicknessAnimation, obj);
            Storyboard.SetTargetProperty(thicknessAnimation, path);
            Storyboard storyBoard = new Storyboard();
            storyBoard.Children.Add(thicknessAnimation);
            storyBoard.Begin();
        }
    }
}
