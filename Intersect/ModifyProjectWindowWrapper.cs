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
    class ModifyProjectWindowWrapper : ProjectWindowWrapper
    {
        public ModifyProjectWindowWrapper(ProjectWindowCallback closeCB, ProjectWindowCallback confirmCB, int pID)
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
                        loadMap(project.path);
                        foreach (string villageName in updateVillageNameList(BASE_LAYER_NAME, BASE_LAYER_FIELD_NAME, projectWindow.mapControl))
                        {
                            villageNameList.Add(villageName);
                        }
                        projectWindow.BaseMapLayerComboBox.SelectedIndex = project.baseMapIndex;
                        updateMapLayerNameList(mapLayerNameList, projectWindow.mapControl);
                        ObservableCollection<Label> labelList = project.getAllRelatedLabel();
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
                Ut.M(validMsg);
                return C.ERROR_INT;
            }
            validMsg = project.checkValid();
            if (validMsg != "")
            {
                Ut.M(validMsg);
                return C.ERROR_INT;
            }
            foreach (Label label in completeLabelList)
            {
                validMsg = label.checkValid();
                if (validMsg != "")
                {
                    Ut.M(validMsg);
                    return C.ERROR_INT;
                }
            }
            foreach (Label label in uncompleteLabelList)
            {
                validMsg = label.checkValid();
                if (validMsg != "")
                {
                    Ut.M(validMsg);
                    return C.ERROR_INT;
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
            Ut.M("更新成功!");
            close();
            return project.id;
        }

        protected override int confirm()
        {
            return update();
        }
    }
}
