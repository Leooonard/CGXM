using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Controls;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.Data.SqlClient;

namespace Intersect
{
    class ModifyProjectWindowHelper : ProjectWindowHelper
    {
        public ModifyProjectWindowHelper(ProjectWindowCallback closeCB, ProjectWindowCallback confirmCB, int pID)
            : base(closeCB, confirmCB)
        {
            project.id = pID;
            project.select();
        }

        public void show()
        {
            base.show();
            Thread t = new Thread(delegate()
            {
                System.Threading.Thread.Sleep(500);
                projectWindow.Dispatcher.BeginInvoke(
                    (ThreadStart)delegate()
                    {
                        projectWindow.mask();
                        //processFile会把地图资源全部读整齐。然后把用户的自定义数据再修改即可。
                        processFile(project.path);

                        projectWindow.BaseMapLayerComboBox.SelectedIndex = project.baseMapIndex;

                        //从地图中读出来的label，没有选中等信息，label清空后再从数据库中读。
                        ObservableCollection<Label> labelList = project.getAllRelatedLabel();
                        completeLabelList.Clear();
                        uncompleteLabelList.Clear();
                        foreach (Label label in labelList)
                        {
                            if (checkMapLayerNameValid(label.mapLayerName) != null)
                            {
                                completeLabelList.Add(label);
                            }
                            else if (specialLayerNameList.Contains(label.mapLayerName))
                            {
                                completeLabelList.Add(label);
                            }
                            else
                            {
                                uncompleteLabelList.Add(label);
                            }
                        }
                        projectWindow.CompleteLabelListBox.ItemsSource = completeLabelList;
                        projectWindow.UncompleteLabelListBox.ItemsSource = uncompleteLabelList; //这里一定要重新设定一遍, 更新combobox中的选择.
                        projectWindow.unmask();
                    }
                );
            });
            t.Start();
        }

        public int update()
        {
            string validMsg = checkAllUIElementValid();
            if (validMsg != "")
            {
                Tool.M(validMsg);
                return Const.ERROR_INT;
            }
            validMsg = project.checkValid();
            if (validMsg != "")
            {
                Tool.M(validMsg);
                return Const.ERROR_INT;
            }
            foreach (Label label in completeLabelList)
            {
                validMsg = label.checkValid();
                if (validMsg != "")
                {
                    Tool.M(validMsg);
                    return Const.ERROR_INT;
                }
            }
            foreach (Label label in uncompleteLabelList)
            {
                validMsg = label.checkValid();
                if (validMsg != "")
                {
                    Tool.M(validMsg);
                    return Const.ERROR_INT;
                }
            }
            project.update();
            foreach (Label label in completeLabelList)
            {
                label.update();
            }
            foreach (Label label in uncompleteLabelList)
            {
                label.update();
            }
            Tool.M("更新成功!");
            close();
            return project.id;
        }

        protected override int confirm()
        {
            return update();
        }
    }
}
