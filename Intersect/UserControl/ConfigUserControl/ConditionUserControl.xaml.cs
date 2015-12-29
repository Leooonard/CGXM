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
using System.Collections.ObjectModel;

namespace Intersect
{
    /// <summary>
    /// ConditionUserControl.xaml 的交互逻辑
    /// </summary>
    public partial class ConditionUserControl : UserControl
    {
        private Project project;
        private Program program;
        private ObservableCollection<Condition> restraintConditionList;
        private ObservableCollection<Condition> standardConditionList;

        public ConditionUserControl()
        {
            InitializeComponent();
        }

        public void clear()
        { 
            
        }

        public void init(int programID)
        {
            program = new Program();
            program.id = programID;
            program.select();

            project = new Project();
            project.id = program.projectID;
            project.select();

            restraintConditionList = new ObservableCollection<Condition>();
            standardConditionList = new ObservableCollection<Condition>();

            load();
        }

        public void refresh()
        {
            load();
        }

        private void load()
        {
            ObservableCollection<Label> labelList;
            ObservableCollection<Condition> tempList;
            tempList = program.getAllRelatedCondition();
            if (tempList.Count == 0)
            {
                //新建的方案, 没有条件. 把标签中使用的添加进去.
                labelList = project.getAllRelatedLabel();
                foreach (Label label in labelList)
                {
                    if (!label.isChoosed)
                    {
                        continue;
                    }

                    if (label.type == Const.LABEL_TYPE_RESTRAINT)
                    {
                        Condition condition = new Condition();
                        condition.labelID = label.id;
                        condition.programID = program.id;
                        condition.type = Const.CONFIG_TYPE_RESTRAINT;
                        condition.saveWithoutCheck();
                        condition.id = Condition.GetLastConditionID();

                        restraintConditionList.Add(condition);
                    }
                    else if (label.type == Const.LABEL_TYPE_STANDARD)
                    {
                        Condition condition = new Condition();
                        condition.labelID = label.id;
                        condition.programID = program.id;
                        condition.type = Const.CONFIG_TYPE_STANDARD;
                        condition.saveWithoutCheck();
                        condition.id = Condition.GetLastConditionID();

                        standardConditionList.Add(condition);
                    }
                    else if (label.type == Const.LABEL_TYPE_BOTH)
                    {
                        Condition restraintCondition = new Condition();
                        restraintCondition.labelID = label.id;
                        restraintCondition.programID = program.id;
                        restraintCondition.type = Const.CONFIG_TYPE_RESTRAINT;
                        restraintCondition.saveWithoutCheck();
                        restraintCondition.id = Condition.GetLastConditionID();

                        restraintConditionList.Add(restraintCondition);


                        Condition standardCondition = new Condition();
                        standardCondition.labelID = label.id;
                        standardCondition.programID = program.id;
                        standardCondition.type = Const.CONFIG_TYPE_STANDARD;
                        standardCondition.saveWithoutCheck();
                        standardCondition.id = Condition.GetLastConditionID();

                        standardConditionList.Add(standardCondition);
                    }
                }
            }
            else
            {
                foreach (Condition condition in tempList)
                {
                    if (condition.type == Const.CONFIG_TYPE_RESTRAINT)
                    {
                        restraintConditionList.Add(condition);
                    }
                    else if (condition.type == Const.CONFIG_TYPE_STANDARD)
                    {
                        standardConditionList.Add(condition);
                    }
                }
            }

            RestraintConditionListBox.ItemsSource = restraintConditionList;
            StandardConditionListBox.ItemsSource = standardConditionList;
        }

        public bool isValid()
        {
            //需要判断百分比单位的值, 是否超过了一百, 坡度是例外, 可以任何值.
            foreach (Condition restraintCondition in restraintConditionList)
            {
                if (restraintCondition.category <= 3 && restraintCondition.category >= 0 && restraintCondition.value > 100)
                {
                    //这个类别下的条件值不能超过100.
                    return false;
                }
            }

            double totalStandardValue = 0;
            foreach (Condition standardCondition in standardConditionList)
            {
                totalStandardValue += standardCondition.value;
            }
            if (totalStandardValue == 0)
            {
                return false;
            }
            BindingGroup bindingGroup = ConditionStepStackPanel.BindingGroup;
            if (!Tool.checkBindingGroup(bindingGroup))
            {
                return false;
            }
            return true;
        }

        public void save()
        {
            foreach (Condition condition in restraintConditionList)
            {
                condition.update();
            }
            foreach (Condition condition in standardConditionList)
            {
                condition.update();
            }
        }
    }
}
