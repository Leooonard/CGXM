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
using System.ComponentModel;

namespace Intersect
{
    /// <summary>
    /// HouseItemUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class HouseItemUserControl : UserControl, INotifyPropertyChanged
    {
        public string Title
        {
            get
            {
                return (string)GetValue(TitleProperty);
            }
            set
            {
                SetValue(TitleProperty, value);
            }
        }
        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(HouseItemUserControl));
        private string houseItemValue;
        public string Value
        {
            get
            {
                return (string)GetValue(ValueProperty);
            }
            set
            {
                SetValue(ValueProperty, value);
            }
        }
        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(string), typeof(HouseItemUserControl)); 


        public HouseItemUserControl()
        {
            InitializeComponent();
            Ut.bind(this, "Title", BindingMode.TwoWay, HouseItemTitleTextBlock, TextBlock.TextProperty, new List<ValidationRule>());
            Ut.bind(this, "Value", BindingMode.TwoWay, HouseItemValueTextBox, TextBox.TextProperty, new List<ValidationRule>());
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void onPropertyChanged(string value)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(value));
            }
        }
    }
}
