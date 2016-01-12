﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Windows;
using ESRI.ArcGIS.GISClient;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.PublisherControls;
using ESRI.ArcGIS.esriSystem;
using System.IO;
using Intersect.Lib;

namespace Intersect
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Const constHelper = new Const();

            try
            {
                InitArcGISLicence();
            }
            catch (Exception exp)
            {
                Tool.M(exp.Message);
            }



            //检查工作目录在不在. 不存在就新开一个。
            if (!Directory.Exists(Const.CONFIG["WORKSPACE_PATH"]))
            {
                Directory.CreateDirectory(Const.CONFIG["WORKSPACE_PATH"]);
            }

        }

        public void InitArcGISLicence()
        {
            ESRI.ArcGIS.RuntimeManager.Bind(ESRI.ArcGIS.ProductCode.EngineOrDesktop);
            AoInitialize aoi = new AoInitializeClass();
            esriLicenseProductCode productCode = esriLicenseProductCode.esriLicenseProductCodeAdvanced;
            if (aoi.IsProductCodeAvailable(productCode) == esriLicenseStatus.esriLicenseAvailable)
            {
                aoi.Initialize(productCode);
            }
            aoi.CheckOutExtension(esriLicenseExtensionCode.esriLicenseExtensionCodeSpatialAnalyst);
        }
    }
}
