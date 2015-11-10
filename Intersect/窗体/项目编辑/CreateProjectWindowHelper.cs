using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;
using System.Windows;
using System.Windows.Controls;
using System.Collections.ObjectModel;
using System.IO;
using ESRI.ArcGIS.Controls;
using Intersect.Lib;
using System.Text.RegularExpressions;
using ESRI.ArcGIS.Carto;

namespace Intersect
{
    class CreateProjectWindowHelper : ProjectWindowHelper
    {
        public CreateProjectWindowHelper(ProjectWindowCallback closeCB, ProjectWindowCallback confirmCB)
            : base(closeCB, confirmCB)
        { }

        public int check()
        {
            if (Project.ProjectNameExist(project.name))
            {
                Tool.M("项目名已存在，不能重复。请修改。");
                return Const.ERROR_INT;
            }
            string validMsg = checkAllUIElementValid();
            if (validMsg != "")
            {
                Tool.M(validMsg);
                return Const.ERROR_INT;
            }
            validMsg = project.checkValid(new List<string>() { "id" });
            if (validMsg != "")
            {
                Tool.M(validMsg);
                return Const.ERROR_INT;
            }
            foreach (Label label in completeLabelList)
            {
                validMsg = label.checkValid(new List<string>() { "id", "projectID" });
                if (validMsg != "")
                {
                    Tool.M(validMsg);
                    return Const.ERROR_INT;
                }
            }
            foreach (Label label in uncompleteLabelList)
            {
                validMsg = label.checkValid(new List<string>() { "id", "projectID" });
                if (validMsg != "")
                {
                    Tool.M(validMsg);
                    return Const.ERROR_INT;
                }
            }
            return 1;
        }

        
        public int save()
        {
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
            Tool.M("创建项目成功!");
            close();
            return projectID;
        }

        /*
         * 1.创建项目文件夹（已有就删）
         * 2.将栅格图转要素图，并存在项目文件夹下。
         * 3.将地图存到项目文件夹下。
         */
        public int fileSave()
        {
            string newDirectoryPath = Path.Combine((new string[] {
                Const.WORKSPACE_PATH,
                Regex.Replace(project.name, @"\s+", "_")
            }));
            
            if (Directory.Exists(newDirectoryPath))
            {
                Directory.Delete(newDirectoryPath, true);
            }
            Directory.CreateDirectory(newDirectoryPath);

            string rasterFolder = Path.Combine((new string[] {
                    newDirectoryPath,
                    "raster"
                }));
            if (Directory.Exists(rasterFolder))
            {
                Directory.Delete(rasterFolder, true);
            }
            Directory.CreateDirectory(rasterFolder);
            foreach(Label rasterLabel in rasterLayerLabelList)
            {
                string rasterPath = GisTool.ConvertRasterLayerToFeatureLayer(rasterFolder, GisTool.getLayerByName(rasterLabel.mapLayerName, projectWindow.mapControl) as IRasterLayer);
                rasterLabel.mapLayerName = rasterPath;
            }

            string newMapPath = Path.Combine((new string[] {
                newDirectoryPath,
                "map.mxd"
            }));
            project.path = newMapPath;
            saveAsMap(newMapPath);

            return 0;
        }

        /*
         * 1. 检查正确性，完整性。出错退出。
         * 2. 存文件。
         * 3. 存数据库。
         */
        protected override int confirm()
        {
            if (check() == Const.ERROR_INT)
            {
                return Const.ERROR_INT;
            }
            fileSave();
            return save();
        }
    }
}
