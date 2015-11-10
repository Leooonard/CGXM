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
        private ObservableCollection<Condition> extraConditionList;

        public ConditionUserControl()
        {
            InitializeComponent();
        }

        public void init(int programID)
        {
            if (program == null)
                program = new Program();
            program.id = programID;
            program.select();

            if (project == null)
                project = new Project();
            project.id = program.projectID;
            project.select();

            ObservableCollection<Label> labelList;
            ObservableCollection<Condition> tempList;
            restraintConditionList = new ObservableCollection<Condition>();
            standardConditionList = new ObservableCollection<Condition>();
            extraConditionList = new ObservableCollection<Condition>();
            tempList = program.getAllRelatedCondition();
            if (tempList.Count == 0)
            {
                //新建的方案, 没有条件. 把标签中使用的添加进去.
                labelList = project.getAllRelatedLabel();
                foreach (Label label in labelList)
                {
                    if (label.isChoosed)
                    {
                        Condition condition = new Condition();
                        condition.labelID = label.id;
                        condition.programID = program.id;
                        if (label.type == Const.LABEL_TYPE_RESTRAINT)
                        {
                            condition.type = Const.CONFIG_TYPE_RESTRAINT;                            
                        }
                        else if (label.type == Const.LABEL_TYPE_STANDARD)
                        {
                            condition.type = Const.CONFIG_TYPE_STANDARD;
                        }
                        condition.saveWithoutCheck();
                        condition.id = Condition.GetLastConditionID();
                        if (label.type == Const.LABEL_TYPE_RESTRAINT)
                            restraintConditionList.Add(condition);
                        else
                            standardConditionList.Add(condition);
                    }
                }
            }
            else
            {
                foreach (Condition condition in tempList)
                {
                    int labelID = condition.labelID;
                    Label label = new Label();
                    label.id = labelID;
                    label.select();
                    if (label.isChoosed)
                    {
                        if (label.type == Const.LABEL_TYPE_RESTRAINT)
                            restraintConditionList.Add(condition);
                        else
                            standardConditionList.Add(condition);
                    }
                    else
                    {
                        labelList = project.getAllRelatedLabel();
                        foreach (Label l in labelList)
                        {
                            condition.labelList.Add(l);
                        }
                        extraConditionList.Add(condition);
                    }
                }
            }

            RestraintConditionListBox.ItemsSource = restraintConditionList;
            StandardConditionListBox.ItemsSource = standardConditionList;
            ExtraConditionListBox.ItemsSource = extraConditionList;
        }

        public List<Condition> getTotalConditionList()
        {
            List<Condition> totalConditionList = new List<Condition>();
            foreach (Condition condition in restraintConditionList)
            {
                totalConditionList.Add(condition);
            }
            foreach (Condition condition in standardConditionList)
            {
                totalConditionList.Add(condition);
            }
            foreach (Condition condition in extraConditionList)
            {
                totalConditionList.Add(condition);
            }
            return totalConditionList;
        }

        private void ConditionDeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Button deleteButton = sender as Button;
            Grid grid = deleteButton.Parent as Grid;
            TextBlock idTextBlock = grid.FindName("ID") as TextBlock;
            string id = idTextBlock.Text;
            for (int i = 0 ; i < restraintConditionList.Count ; i++)
            {
                Condition condition = restraintConditionList[i];
                if (condition.id.ToString() == id)
                {
                    restraintConditionList.Remove(condition);
                    return;
                }
            }
            for (int i = 0; i < standardConditionList.Count; i++)
            {
                Condition condition = standardConditionList[i];
                if (condition.id.ToString() == id)
                {
                    standardConditionList.Remove(condition);
                    return;
                }
            }
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            Grid grid = comboBox.Parent as Grid;
            TextBlock conditionIDTextBlock = grid.FindName("ConditionIDTextBlock") as TextBlock;
            int conditionID = Int32.Parse(conditionIDTextBlock.Text);
            foreach (Condition condition in extraConditionList)
            {
                if (condition.id == conditionID)
                {
                    condition.delete();
                    extraConditionList.Remove(condition);
                    return;
                }
            }
        }

        public bool isValid()
        {
            BindingGroup bindingGroup = ConditionStepStackPanel.BindingGroup;
            if (!Tool.checkBindingGroup(bindingGroup))
            {
                return false;
            }
            return true;
        }

        public bool isDirty()
        {
            ObservableCollection<Condition> tempConditionList = program.getAllRelatedCondition();
            if (restraintConditionList.Count + extraConditionList.Count != tempConditionList.Count)
                return true;

            foreach (Condition condition in restraintConditionList)
            {
                int conditionID = condition.id;
                Condition conditionCopy = new Condition();
                conditionCopy.id = conditionID;
                conditionCopy.select();
                if (!condition.compare(conditionCopy))
                    return true;
            }

            foreach (Condition condition in standardConditionList)
            {
                int conditionID = condition.id;
                Condition conditionCopy = new Condition();
                conditionCopy.id = conditionID;
                conditionCopy.select();
                if (!condition.compare(conditionCopy))
                    return true;
            }

            foreach (Condition condition in extraConditionList)
            {
                int conditionID = condition.id;
                Condition conditionCopy = new Condition();
                conditionCopy.id = conditionID;
                conditionCopy.select();
                if (!condition.compare(conditionCopy))
                    return true;
            }
            return false;
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
            foreach (Condition condition in extraConditionList)
            {
                Label label = condition.labelList[condition.labelIndex];
                condition.labelID = label.id;
                condition.update();
            }
        }
    }
}
