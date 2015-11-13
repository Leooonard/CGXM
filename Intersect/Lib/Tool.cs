using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Data;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Media.Animation;
using System.IO;

namespace Intersect
{
    class Tool
    {
        public static void M(object content)
        {
            MessageBox.Show(content.ToString());
        }

        public static bool C(object content)
        {
            return MessageBox.Show(content.ToString(), "确认", MessageBoxButton.YesNo) == MessageBoxResult.Yes;
        }

        public static void W(string content)
        {
            Console.WriteLine(content);
        }

        public static string random(int length)
        {
            string[] randomList = new string[] { 
                "a", "b", "c", "d", "e", "f", "g", "h",
                "i", "j", "k", "l", "m", "n", "o", "p",
                "q", "r", "s", "t", "u", "v", "w", "x", "y", "z",
                 "0", "1", "2", "3", "4", "5", "6", "7", "8", "9"
            };

            string randomString = "";
            Random randomer = new Random();
            if (length <= 0)
            {
                return "";
            }

            for (int i = 0; i < length; i++)
            {
                randomString += randomList[randomer.Next(0, 36)];
            }

            return randomString;
        }

        public static void bind(object source, string path, BindingMode mode, FrameworkElement element
            , DependencyProperty property, List<ValidationRule> validationRuleList, string bindingGroupName = null)
        {
            Binding binding = new Binding();
            binding.Source = source;
            binding.Path = new PropertyPath(path);
            binding.Mode = mode;
            binding.UpdateSourceTrigger = UpdateSourceTrigger.LostFocus;
            foreach (ValidationRule rule in validationRuleList)
            {
                binding.ValidationRules.Add(rule);
            }
            if (bindingGroupName != null)
                binding.BindingGroupName = bindingGroupName;
            element.SetBinding(property, binding);
        }

        public static childItem FindVisualChild<childItem>(DependencyObject obj) where childItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is childItem)
                    return (childItem)child;
                else
                {
                    childItem childOfChild = FindVisualChild<childItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        public static bool CheckInt(int value)
        {
            if (value != Intersect.Const.ERROR_INT)
                return true;
            else
                return false;
        }

        public static bool CheckDouble(double value)
        {
            if (value != Intersect.Const.ERROR_DOUBLE)
                return true;
            else
                return false;
        }

        public static bool checkBindingGroup(BindingGroup bindingGroup)
        {
            foreach (BindingExpression expression in bindingGroup.BindingExpressions)
            {
                expression.UpdateSource();
                if (expression.HasError)
                    return false;
            }
            return true;
        }
    }
}
