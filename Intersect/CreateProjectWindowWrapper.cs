using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using ESRI.ArcGIS.Controls;

namespace Intersect
{
    class CreateProjectWindowWrapper : ProjectWindowWrapper
    {
        public CreateProjectWindowWrapper(ProjectWindowCallback closeCB, ProjectWindowCallback confirmCB)
            : base(closeCB, confirmCB)
        { }

        public int save()
        {
            string validMsg = checkAllUIElementValid();
            if (validMsg != "")
            {
                Ut.M(validMsg);
                return C.ERROR_INT;
            }
            validMsg = project.checkValid(new List<string>() { "id" });
            if (validMsg != "")
            {
                Ut.M(validMsg);
                return C.ERROR_INT;
            }
            foreach (Label label in completeLabelList)
            {
                validMsg = label.checkValid(new List<string>() { "id" , "projectID" });
                if (validMsg != "")
                {
                    Ut.M(validMsg);
                    return C.ERROR_INT;
                }
            }
            foreach (Label label in uncompleteLabelList)
            {
                validMsg = label.checkValid(new List<string>() { "id" , "projectID" });
                if (validMsg != "")
                {
                    Ut.M(validMsg);
                    return C.ERROR_INT;
                }
            }
            project.save();
            int projectID = Project.GetLastProjectID();
            foreach (Label label in completeLabelList)
            {
                label.projectID = projectID;
                label.save();
            }
            foreach (Label label in uncompleteLabelList)
            {
                label.projectID = projectID;
                label.save();
            }
            Ut.M("创建项目成功!");
            close();
            return projectID;
        }

        protected override int confirm()
        {
            return save();
        }
    }
}
