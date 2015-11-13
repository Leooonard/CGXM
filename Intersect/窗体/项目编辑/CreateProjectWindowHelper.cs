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
            string newDirectoryPath = System.IO.Path.Combine((new string[] {
                Const.WORKSPACE_PATH,
                FileHelper.FormatName(project.name)
            }));
            

            if (Directory.Exists(newDirectoryPath))
            {
                Tool.M("项目名已存在，不能重复。请修改。");
                return Const.ERROR_INT;
            }
            try
            {
                Directory.CreateDirectory(newDirectoryPath);
            }
            catch (Exception writeInException)
            {
                Tool.M("工作目录无法写入。请确保目录没有正在使用。");
                return Const.ERROR_INT;
            }

            string programsFolder = System.IO.Path.Combine(
                newDirectoryPath,
                Const.PROGRAMS_FOLDER_NAME
            );
            Directory.CreateDirectory(programsFolder);

            string sourceFolder = System.IO.Path.Combine(new string[] { 
                newDirectoryPath,
                Const.SOURCE_FOLDER_NAME
            });
            Directory.CreateDirectory(sourceFolder);
            FileHelper.DirectoryCopy(Path.GetDirectoryName(sourceMapPath), sourceFolder, true);

            string rasterFolder = System.IO.Path.Combine((new string[] {
                newDirectoryPath,
                "raster"
            }));
            
            Directory.CreateDirectory(rasterFolder);
            foreach (Label rasterLabel in rasterLayerLabelList)
            {
                string rasterPath = GisTool.ConvertRasterLayerToFeatureLayer(rasterFolder, GisTool.getLayerByName(rasterLabel.mapLayerName, projectWindow.mapControl) as IRasterLayer);
                rasterLabel.mapLayerPath = rasterPath;
            }

            project.path = newDirectoryPath;

            return 0;
        }

        /*
         * 1. 检查正确性，完整性。出错退出。
         * 2. 存文件。
         * 3. 存数据库。
         */
        protected override int confirm()
        {
            projectWindow.ConfirmButton.IsEnabled = false;
            projectWindow.CloseButton.IsEnabled = false;
            if (check() == Const.ERROR_INT)
            {
                projectWindow.ConfirmButton.IsEnabled = true;
                projectWindow.CloseButton.IsEnabled = true;
                return Const.ERROR_INT;
            }
            Tool.M("系统将创建工作目录，请保证当前地图文件目录不移动。");
            fileSave();
            Tool.M("完成。");
            projectWindow.ConfirmButton.IsEnabled = true;
            projectWindow.CloseButton.IsEnabled = true;
            return save();
        }
    }
}
