using System;
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

namespace Intersect
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public const string TEMP_PATH = @"C:\CGXM\";

        public App()
        {
            InitArcGISLicence();

            //开一个专门用来放临时文件的文件夹。
            if (!Directory.Exists(TEMP_PATH))
            {
                Directory.CreateDirectory(TEMP_PATH);
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
