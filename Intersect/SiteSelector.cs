using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Controls;
using System.IO;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.DataSourcesFile;
using System.Windows;
using System.Collections;
using ESRI.ArcGIS.Display;
using System.Data.SqlClient;
using System.Collections.ObjectModel;
using Intersect.Lib;

namespace Intersect
{
    class SiteSelector
    {
        private Geoprocessor gp;
        private AxMapControl mapControl;
        private string mapFolder; //mxd所在目录.
        private string targetFolder; //我生成文件存放的目标目录.
        private string fishnetPolygonName;
        private string fishnetName;
        private double fishnetWidth;
        private double fishnetHeight;
        private List<Feature> featureList;
        private ObservableCollection<Condition> conditionList;
        private NetSize netSize;
        private Program program;
        private Project project;
        private List<string> mapLayerNameList;
        private double totalStandardValue;
        private IFeature baseFeature;
        private static List<IElement> drawnElementList = new List<IElement>();
        private IGeometry rootGeometry;

        public SiteSelector(AxMapControl mc, int programID)
        {
            Init(mc, programID);
        }

        public SiteSelector(AxMapControl mc, List<Feature> feaList, int programID)
            :this(mc, programID)
        {
            featureList = feaList;
        }

        private string generateFolder(string basePath)
        {
            TimeSpan ts = DateTime.Now - DateTime.Parse("1970-1-1");
            targetFolder = basePath + "/" + ts.TotalMilliseconds.ToString();
            //生成文件夹.
            if (!Directory.Exists(targetFolder))
            {
                Directory.CreateDirectory(targetFolder);
            }
            return targetFolder;
        }

        private void Init(AxMapControl mc, int programID)
        {
            //这里是window.show()的一个坑. show其实等同于设置窗口的visibility:visible.
            drawnElementList = new List<IElement>();

            gp = new Geoprocessor();
            mapControl = mc;
            UpdateMapLayerNameList(mapLayerNameList, mapControl);
            program = new Program();
            program.id = programID;
            program.select();
            project = new Project();
            project.id = program.projectID;
            project.select();
            netSize = program.getRelatedNetSize();
            conditionList = program.getAllRelatedCondition();
            baseFeature = GisTool.GetBaseFeature(mapControl, project.baseMapIndex);
            mapFolder = System.IO.Path.GetDirectoryName(project.path);
            targetFolder = generateFolder(mapFolder);
            foreach (Condition condition in conditionList)
            {
                if (condition.type == Const.CONFIG_TYPE_STANDARD)
                {
                    totalStandardValue += condition.value;
                }
            }
            fishnetPolygonName = "polygon.shp";
            fishnetName = "fishnet.shp";
            fishnetWidth = netSize.width;
            fishnetHeight = netSize.height;

            featureList = new List<Feature>();
        }

        private string getFullPath(string folder, string name)
        {
            return folder + "/" + name;
        }

        public List<Feature> startSelectSite()
        {
            deleteShapeFile("评价结果");
            EraseDrawnGeometryList(mapControl);
            IGeometry allGeom = baseFeature.Shape;
            rootGeometry = baseFeature.Shape;

            Dictionary<string, double> dict = GisTool.GetExternalRectDimension(allGeom);
            GisTool.CreateEnvelopFishnet(fishnetWidth, fishnetHeight, getFullPath(targetFolder, fishnetName), dict);
            GisTool.FeatureToPolygon(getFullPath(targetFolder, fishnetName), getFullPath(targetFolder, fishnetPolygonName));

            IFeatureClass featureClass = GisTool.getFeatureClass(targetFolder, fishnetPolygonName); //获得网格类, 其中的网格已成面.
            ISpatialFilter filter = new SpatialFilterClass();
            filter.Geometry = rootGeometry;
            filter.GeometryField = "SHAPE";
            filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            IFeatureCursor cursor = featureClass.Search(filter, false);
            IFeature feature;
            while ((feature = cursor.NextFeature()) != null)
            {
                Feature fea = new Feature();
                fea.inUse = 1;
                fea.score = -1;
                fea.relativeFeature = feature;
                featureList.Add(fea);
            }
            
            for (int i = 0; i < conditionList.Count; i++)
            {
                Condition condition = conditionList[i];
                Label label = new Label();
                label.id = condition.labelID;
                label.select();
                string targetLayerName = label.mapLayerName;
                string targetLayerPath = label.mapLayerPath;
                if (condition.type == Const.CONFIG_TYPE_RESTRAINT)
                {
                    if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_DISTANCE_BIGGER)
                    {
                        distanceRestraint(targetLayerName, condition.value, featureList,
                            new DistanceFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                            {
                                if (filteredGeometry == null)
                                {
                                    return false;
                                }
                                return true;
                            }),
                            new DistanceRestraintHandler(delegate(double distance, double restraint, Feature fea, List<Feature> feaList)
                            {
                                if (distance <= restraint)
                                {
                                    feaList.Remove(fea);
                                    return true;
                                }
                                return false;
                            }));
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_DISTANCE_BIGGEREQUAL)
                    {
                        distanceRestraint(targetLayerName, condition.value, featureList,
                            new DistanceFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                            {
                                if (filteredGeometry == null)
                                {
                                    return false;
                                }
                                return true;
                            }),
                            new DistanceRestraintHandler(delegate(double distance, double restraint, Feature fea, List<Feature> feaList)
                            {
                                if (distance < restraint)
                                {
                                    feaList.Remove(fea);
                                    return true;
                                }
                                return false;
                            }));
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_DISTANCE_SMALLER)
                    {
                        distanceRestraint(targetLayerName, condition.value, featureList,
                            new DistanceFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                            {
                                if (filteredGeometry == null)
                                {
                                    feaList.Clear();
                                    return false;
                                }
                                return true;
                            }),
                            new DistanceRestraintHandler(delegate(double distance, double restraint, Feature fea, List<Feature> feaList)
                            {
                                if (distance >= restraint)
                                {
                                    feaList.Remove(fea);
                                    return true;
                                }
                                return false;
                            }));
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_DISTANCE_SMALLEREQUAL)
                    {
                        distanceRestraint(targetLayerName, condition.value, featureList,
                            new DistanceFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                            {
                                if (filteredGeometry == null)
                                {
                                    feaList.Clear();
                                    return false;
                                }
                                return true;
                            }),
                            new DistanceRestraintHandler(delegate(double distance, double restraint, Feature fea, List<Feature> feaList)
                            {
                                if (distance > restraint)
                                {
                                    feaList.Remove(fea);
                                    return true;
                                }
                                return false;
                            }));
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_INTERSECT_BIGGER)
                    {
                        IntersectRestraint(targetLayerName, condition.value, featureList,
                            new IntersectFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                            {
                                if (filteredGeometry == null)
                                {
                                    feaList.Clear();
                                    return false;
                                }
                                return true;
                            }),
                            new IntersectRestraintHandler(delegate(double ratio, double restraint, Feature fea, List<Feature> feaList)
                            {
                                if (ratio <= restraint)
                                {
                                    feaList.Remove(fea);
                                    return true;
                                }
                                return false;
                            }));
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_INTERSECT_BIGGEREQUAL)
                    {
                        IntersectRestraint(targetLayerName, condition.value, featureList,
                            new IntersectFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                            {
                                if (filteredGeometry == null)
                                {
                                    feaList.Clear();
                                    return false;
                                }
                                return true;
                            }),
                            new IntersectRestraintHandler(delegate(double ratio, double restraint, Feature fea, List<Feature> feaList)
                            {
                                if (ratio < restraint)
                                {
                                    feaList.Remove(fea);
                                    return true;
                                }
                                return false;
                            }));
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_INTERSECT_SMALLER)
                    {
                        IntersectRestraint(targetLayerName, condition.value, featureList,
                            new IntersectFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                            {
                                if (filteredGeometry == null)
                                {
                                    return false;
                                }
                                return true;
                            }),
                            new IntersectRestraintHandler(delegate(double ratio, double restraint, Feature fea, List<Feature> feaList)
                            {
                                if (ratio >= restraint)
                                {
                                    feaList.Remove(fea);
                                    return true;
                                }
                                return false;
                            }));
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_INTERSECT_SMALLEREQUAL)
                    {
                        IntersectRestraint(targetLayerName, condition.value, featureList,
                            new IntersectFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                            {
                                if (filteredGeometry == null)
                                {
                                    return false;
                                }
                                return true;
                            }),
                            new IntersectRestraintHandler(delegate(double ratio, double restraint, Feature fea, List<Feature> feaList)
                            {
                                if (ratio > restraint)
                                {
                                    feaList.Remove(fea);
                                    return true;
                                }
                                return false;
                            }));                            
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_OVERLAP_BIGGER)
                    {
                        biggerOverlapRestraint(targetLayerPath, condition.value, featureList);                                                    
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_OVERLAP_BIGGEREQUAL)
                    {
                        biggerEqualOverlapRestraint(targetLayerPath, condition.value, featureList);                                                                            
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_OVERLAP_SMALLER)
                    {
                        smallerOverlapRestraint(targetLayerPath, condition.value, featureList);
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_OVERLAP_SMALLEREQUAL)
                    {
                        smallerEqualOverlapRestraint(targetLayerPath, condition.value, featureList);                                                                           
                    }
                }
                else if(condition.type == Const.CONFIG_TYPE_STANDARD)
                {
                    if (condition.category == Const.CONFIG_CATEGORY_STANDARD_DISTANCE_NEGATIVE)
                    {
                        negativeDistanceStandard(targetLayerName, condition.value / totalStandardValue, featureList);
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_STANDARD_DISTANCE_POSITIVE)
                    {
                        positiveDistanceStandard(targetLayerName, condition.value / totalStandardValue, featureList);
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_STANDARD_OVERLAP_NEGATIVE)
                    {
                        negativeOverlapStandard(targetLayerPath, condition.value / totalStandardValue, featureList);
                    }
                    else if (condition.category == Const.CONFIG_CATEGORY_STANDARD_OVERLAP_POSITIVE)
                    {
                        positiveOverlapStandard(targetLayerPath, condition.value / totalStandardValue, featureList);
                    }
                }
            }

            saveShapeFile("评价结果.shp");
            addShapeFile("评价结果.shp", "评价结果");
            
            return featureList;
        }

        private void saveShapeFile(string shpName)
        {
            GisTool.CreateShapefile(program.path, shpName, mapControl.SpatialReference);
            IFeatureClass resultFeatureClass = GisTool.getFeatureClass(program.path, shpName);
            GisTool.addFeatureLayerField(resultFeatureClass, "rslt", esriFieldType.esriFieldTypeDouble, 10);

            GisTool.AddFeaturesToFeatureClass(featureList, resultFeatureClass, "rslt");
        }

        private void deleteShapeFile(string layerName)
        {
            int index = GisTool.getLayerIndexByName(layerName, mapControl);
            if (index == -1)
            {
                return;
            }

            mapControl.DeleteLayer(index);
            GisTool.DeleteShapeFile(System.IO.Path.Combine(program.path, layerName + ".shp"));
        }

        public bool addShapeFile(string shpName, string layerName)
        {
            if (File.Exists(System.IO.Path.Combine(program.path, shpName)))
            {
                IFeatureClass resultFeatureClass = GisTool.getFeatureClass(program.path, shpName);
                IFeatureLayer resultFeatureLayer = new FeatureLayerClass();
                resultFeatureLayer.FeatureClass = resultFeatureClass;
                resultFeatureLayer.Name = layerName;
                ILayerEffects layerEffects = resultFeatureLayer as ILayerEffects;
                layerEffects.Transparency = 60;
                mapControl.AddLayer(resultFeatureLayer);
                mapControl.ActiveView.Refresh();

                return true;
            }

            return false;
        }

        public List<string> UpdateMapLayerNameList(List<string> mapLayerNameList, AxMapControl mapControl)
        {
            mapLayerNameList = new List<string>();
            //每次读取地图, 都要更新图层列表中的图层名.
            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                ILayer layer = mapControl.get_Layer(i);
                ICompositeLayer compositeLayer = layer as ICompositeLayer;
                if (compositeLayer == null)
                {
                    //说明不是一个组合图层, 直接获取图层名.
                    mapLayerNameList.Add(layer.Name);
                }
                else
                {
                    for (int j = 0; j < compositeLayer.Count; j++)
                    {
                        ILayer ly = compositeLayer.get_Layer(j);
                        mapLayerNameList.Add(ly.Name);
                    }
                }
            }
            return mapLayerNameList;
        }

        private delegate bool IntersectFilterResultHandler(IGeometry filteredGeometry, List<Feature> featureList);
        private delegate bool IntersectRestraintHandler(double ratio, double restraint, Feature feature, List<Feature> featureList);

        private IGeometry IntersectRestraintFilter(IFeatureClass targetFeatureClass, IGeometry baseGeometry = null)
        {
            if (baseGeometry == null)
            {
                baseGeometry = rootGeometry;
            }
            IGeometry filteredGeometry = null;
            ISpatialFilter filter = new SpatialFilterClass();
            filter.Geometry = baseGeometry;
            filter.GeometryField = "SHAPE";
            filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            IFeatureCursor cursor = targetFeatureClass.Search(filter, false);
            IFeature feature = cursor.NextFeature();
            if (feature == null)
            {
                return null;
            }
            filteredGeometry = feature.Shape;
            ITopologicalOperator tpop = filteredGeometry as ITopologicalOperator;
            IGeometryCollection geomCol = new GeometryBagClass();

            while((feature = cursor.NextFeature()) != null)
            {
                geomCol.AddGeometry(feature.Shape);
            }

            tpop.ConstructUnion(geomCol as IEnumGeometry);

            return filteredGeometry;
        }
        
        private void IntersectRestraint(string targetLayerName, double restraint, List<Feature> featureList,
            IntersectFilterResultHandler filterResultHandler,
            IntersectRestraintHandler restraintHandler)
        {
            IFeatureClass targetFeatureClass = GisTool.getFeatureClass(mapControl, targetLayerName);
            IGeometry filteredGeometry = IntersectRestraintFilter(targetFeatureClass);
            bool resumeProcess = filterResultHandler(filteredGeometry, featureList); //返回值决定是否继续往下算。
            if (resumeProcess == false)
            {
                return;
            }
            for (int i = 0; i < featureList.Count; i++)
            {
                Feature feature = featureList[i];
                double ratio = IntersectRatio(feature.relativeFeature.Shape, filteredGeometry);
                bool isRemove = restraintHandler(ratio, restraint, feature, featureList);
                if (isRemove == true)
                {
                    i--;
                }
            }
        }
        
        private double IntersectRatio(IGeometry srcGeometry, IGeometry targetGeometry)
        {
            double ratio = 0;
            srcGeometry.SpatialReference = mapControl.SpatialReference;
            ITopologicalOperator toOp = srcGeometry as ITopologicalOperator;
            IGeometry intersectedGeom = toOp.Intersect(targetGeometry, esriGeometryDimension.esriGeometry2Dimension);
            //测量相交图形的面积, 超过原图形的一半, 有效.
            IArea area = intersectedGeom as IArea;
            IArea srcArea = srcGeometry as IArea;
            ratio = (area.Area / srcArea.Area) * 100;
            return ratio;
        }

        private delegate bool DistanceFilterResultHandler(IGeometry filteredGeometry, List<Feature> featureList);
        private delegate bool DistanceRestraintHandler(double distance, double restraint, Feature feature, List<Feature> featureList);
             
        private IGeometry distanceRestraintFilter(IFeatureClass targetFeatureClass, double distance)
        {
            ITopologicalOperator tpop = rootGeometry as ITopologicalOperator;
            IGeometry bufferedGeometry = tpop.Buffer(distance);
            return IntersectRestraintFilter(targetFeatureClass, bufferedGeometry);
        }

        private void distanceRestraint(string targetLayerName, double restraint, List<Feature> featureList,
            DistanceFilterResultHandler filterResultHandler,
            DistanceRestraintHandler restraintHandler)
        {
            IFeatureClass targetFeatureClass = GisTool.getFeatureClass(mapControl, targetLayerName);
            IGeometry filteredGeometry = distanceRestraintFilter(targetFeatureClass, restraint);
            bool resumeProcess = filterResultHandler(filteredGeometry, featureList);
            if (resumeProcess == false)
            {
                return;
            }

            for (int i = 0; i < featureList.Count; i++)
            {
                Feature feature = featureList[i];
                double distance = DistanceValue(feature.relativeFeature.Shape, filteredGeometry);
                bool isRemove = restraintHandler(distance, restraint, feature, featureList);
                if (isRemove == true)
                {
                    i--;
                }
            }
        }

        private double DistanceValue(IGeometry srcGeometry, IGeometry targetGeometry)
        {
            IProximityOperator proOp = srcGeometry as IProximityOperator;
            srcGeometry.SpatialReference = mapControl.SpatialReference;
            targetGeometry.SpatialReference = mapControl.SpatialReference;
            return proOp.ReturnDistance(targetGeometry); //获取两者间的距离.
        }

        private delegate bool OverlapFilterResultHandler(List<IGeometry> filteredGeometryList, List<Feature> featureList);
        private delegate bool OverlapRestraintHandler(double value, double restraint, Feature feature, List<Feature> featureList);

        private List<IGeometry> OverlapRestraintFilter(IFeatureClass targetFeatureClass, IGeometry baseGeometry = null)
        {
            if (baseGeometry == null)
            {
                baseGeometry = rootGeometry;
            }
            List<IGeometry> geometryList = new List<IGeometry>();
            baseGeometry.SpatialReference = mapControl.SpatialReference;
            ISpatialFilter cityFilter = new SpatialFilterClass();
            cityFilter.Geometry = baseGeometry;
            cityFilter.GeometryField = "SHAPE";
            cityFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            IFeatureCursor feaCursor = targetFeatureClass.Search(cityFilter, false);
            IFeature feature = feaCursor.NextFeature();
            if (feature == null)
            {
                return null;
            }
            geometryList.Add(feature.Shape);
            while ((feature = feaCursor.NextFeature()) != null)
            {
                geometryList.Add(feature.Shape);
            }
            return geometryList;
        }

        private double overlapRestraint(IGeometry srcGeometry, IFeatureClass targetFeatureClass)
        {
            srcGeometry.SpatialReference = mapControl.SpatialReference;
            ITopologicalOperator tpOp = srcGeometry as ITopologicalOperator;
            IPolygon srcPolygon = srcGeometry as IPolygon;
            IArea area = srcGeometry as IArea;
            ISpatialFilter cityFilter = new SpatialFilterClass();
            cityFilter.Geometry = srcGeometry;
            cityFilter.GeometryField = "SHAPE";
            cityFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            IFeatureCursor feaCursor = targetFeatureClass.Search(cityFilter, false);

            //获取所有与该块相交的要素, 然后检测其相交的面积, 依照比例计算最终的值.
            IFeature cityFea = feaCursor.NextFeature();
            if (cityFea == null)
            {
                return 0;
            }

            double overlap = 0;
            while (cityFea != null)
            {
                IGeometry cityGeom = cityFea.Shape;
                cityGeom.SpatialReference = mapControl.SpatialReference;
                IGeometry intersectedGeom = tpOp.Intersect(cityGeom, esriGeometryDimension.esriGeometry2Dimension);
                IArea intersectedGeomArea = intersectedGeom as IArea;
                //获取gridcode.
                ITable table = cityFea.Table;
                IRow row = table.GetRow(0);

                int fieldNum = table.FindField("GRIDCODE");
                double value = (double)cityFea.get_Value(fieldNum);
                overlap = overlap + value * (intersectedGeomArea.Area / area.Area);
                cityFea = feaCursor.NextFeature();
            }
            return overlap;
        }

        private void biggerOverlapRestraint(string targetLayerPath, double restraint, List<Feature> featureList)
        {
            IFeatureClass targetFeatureClass = GisTool.getFeatureClass(System.IO.Path.GetDirectoryName(targetLayerPath),
                                                                        System.IO.Path.GetFileName(targetLayerPath)); 
            for (int i = 0; i < featureList.Count; i++)
            {
                Feature feature = featureList[i];
                double overlap = overlapRestraint(feature.relativeFeature.Shape, targetFeatureClass);
                if (overlap <= restraint)
                {
                    featureList.Remove(feature);
                    i--;
                }
            }
        }

        private void smallerOverlapRestraint(string targetLayerPath, double restraint, List<Feature> featureList)
        {
            IFeatureClass targetFeatureClass = GisTool.getFeatureClass(System.IO.Path.GetDirectoryName(targetLayerPath),
                                                                        System.IO.Path.GetFileName(targetLayerPath));
            for (int i = 0; i < featureList.Count; i++)
            {
                Feature feature = featureList[i];
                double overlap = overlapRestraint(feature.relativeFeature.Shape, targetFeatureClass);
                if (overlap >= restraint)
                {
                    featureList.Remove(feature);
                    i--;
                }
            }
        }

        private void biggerEqualOverlapRestraint(string targetLayerPath, double restraint, List<Feature> featureList)
        {
            IFeatureClass targetFeatureClass = GisTool.getFeatureClass(System.IO.Path.GetDirectoryName(targetLayerPath),
                                                                        System.IO.Path.GetFileName(targetLayerPath));
            for (int i = 0; i < featureList.Count; i++)
            {
                Feature feature = featureList[i];
                double overlap = overlapRestraint(feature.relativeFeature.Shape, targetFeatureClass);
                if (overlap < restraint)
                {
                    featureList.Remove(feature);
                    i--;
                } 
            }
        }

        private void smallerEqualOverlapRestraint(string targetLayerPath, double restraint, List<Feature> featureList)
        {
            IFeatureClass targetFeatureClass = GisTool.getFeatureClass(System.IO.Path.GetDirectoryName(targetLayerPath),
                                                                        System.IO.Path.GetFileName(targetLayerPath));
            for (int i = 0; i < featureList.Count; i++)
            {
                Feature feature = featureList[i];
                double overlap = overlapRestraint(feature.relativeFeature.Shape, targetFeatureClass);
                if (overlap > restraint)
                {
                    featureList.Remove(feature);
                    i--;
                }
            }
        }

        private double distanceStandard(IGeometry srcGeometry, IGeometry targetGeometry)
        {
            srcGeometry.SpatialReference = mapControl.SpatialReference;
            targetGeometry.SpatialReference = mapControl.SpatialReference;
            IProximityOperator proOp = srcGeometry as IProximityOperator;
            return proOp.ReturnDistance(targetGeometry);
        }

        private void positiveDistanceStandard(string targetLayerName, double standard, List<Feature> featureList)
        {
            IFeatureClass targetFeatureClass = GisTool.getFeatureClass(mapControl, targetLayerName);
            List<IGeometry> geometryList = new List<IGeometry>();
            for (int i = 0; i < targetFeatureClass.FeatureCount(null); i++)
            {
                geometryList.Add(targetFeatureClass.GetFeature(i).ShapeCopy);
            }
            IGeometry unionGeometry = GisTool.unionAllFeature(geometryList);
            List<double> distanceList = new List<double>();
            for (int i = 0; i < featureList.Count; i++)
            {
                Feature feature = featureList[i];
                double distance = DistanceValue(feature.relativeFeature.Shape, unionGeometry);
                distanceList.Add(distance);
            }

            distanceList = normalize(distanceList, true);
            for (int i = 0; i < featureList.Count; i++)
            {
                featureList[i].score += standard * distanceList[i];
            }
        }

        private void negativeDistanceStandard(string targetLayerName, double standard, List<Feature> featureList)
        {
            IFeatureClass targetFeatureClass = GisTool.getFeatureClass(mapControl, targetLayerName);
            List<IGeometry> geometryList = new List<IGeometry>();
            for (int i = 0; i < targetFeatureClass.FeatureCount(null); i++)
            {
                geometryList.Add(targetFeatureClass.GetFeature(i).ShapeCopy);
            }
            IGeometry unionGeometry = GisTool.unionAllFeature(geometryList);
            List<double> distanceList = new List<double>();
            for (int i = 0; i < featureList.Count; i++)
            {
                Feature feature = featureList[i];
                double distance = DistanceValue(feature.relativeFeature.Shape, unionGeometry);
                distanceList.Add(distance);
            }

            distanceList = normalize(distanceList, false);
            for (int i = 0; i < featureList.Count; i++)
            {
                featureList[i].score += standard * distanceList[i];
            }
        }

        private double overlapStandard(IGeometry srcGeometry, IFeatureClass targetFeatureClass)
        {
            srcGeometry.SpatialReference = mapControl.SpatialReference;
            ITopologicalOperator tpOp = srcGeometry as ITopologicalOperator;
            IPolygon srcPolygon = srcGeometry as IPolygon;
            IArea area = srcGeometry as IArea;
            ISpatialFilter cityFilter = new SpatialFilterClass();
            cityFilter.Geometry = srcGeometry;
            cityFilter.GeometryField = "SHAPE";
            cityFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            IFeatureCursor feaCursor = targetFeatureClass.Search(cityFilter, false);

            //获取所有与该块相交的要素, 然后检测其相交的面积, 依照比例计算最终的值.
            IFeature cityFea = feaCursor.NextFeature();
            
            double overlap = 0;
            while (cityFea != null)
            {
                IGeometry cityGeom = cityFea.Shape;
                cityGeom.SpatialReference = mapControl.SpatialReference;
                IGeometry intersectedGeom = tpOp.Intersect(cityGeom, esriGeometryDimension.esriGeometry2Dimension);
                IArea intersectedGeomArea = intersectedGeom as IArea;
                //获取gridcode.
                ITable table = cityFea.Table;
                IRow row = table.GetRow(0);
               
                int fieldNum = table.FindField("GRIDCODE");
                double value = (double)cityFea.get_Value(fieldNum);
                overlap = overlap + value * (intersectedGeomArea.Area / area.Area);
                cityFea = feaCursor.NextFeature();
            }
            return overlap;
        }

        private void positiveOverlapStandard(string targetLayerPath, double standard, List<Feature> featureList)
        {
            IFeatureClass targetFeatureClass = GisTool.getFeatureClass(System.IO.Path.GetDirectoryName(targetLayerPath),
                                                                        System.IO.Path.GetFileName(targetLayerPath));
            List<double> overlapList = new List<double>();
            for (int i = 0; i < featureList.Count; i++)
            {
                overlapList.Add(overlapStandard(featureList[i].relativeFeature.Shape, targetFeatureClass));
            }

            overlapList = normalize(overlapList, true);
            for (int i = 0; i < featureList.Count; i++)
            {
                featureList[i].score += standard * overlapList[i];
            }
        }

        private void negativeOverlapStandard(string targetLayerPath, double standard, List<Feature> featureList)
        {
            IFeatureClass targetFeatureClass = GisTool.getFeatureClass(System.IO.Path.GetDirectoryName(targetLayerPath),
                                                                        System.IO.Path.GetFileName(targetLayerPath));
            List<double> overlapList = new List<double>();
            for (int i = 0; i < featureList.Count; i++)
            {
                overlapList.Add(overlapStandard(featureList[i].relativeFeature.Shape, targetFeatureClass));
            }

            overlapList = normalize(overlapList, false);
            for (int i = 0; i < featureList.Count; i++)
            {
                featureList[i].score += standard * overlapList[i];
            }
        }

        private void DrawSelectResult(List<Feature> featureList)
        {
            //第八, 按不同的成绩用不同的颜色画方块.
            for (int i = 0; i < featureList.Count; i++)
            {
                IElement element;
                element = GisTool.drawPolygonByScore(featureList[i].relativeFeature.ShapeCopy, featureList[i].score, mapControl);
                drawnElementList.Add(element);
            }
        }

        private static void EraseDrawnGeometryList(AxMapControl mapControl)
        {
            foreach (IElement element in drawnElementList)
            {
                GisTool.EraseElement(element, mapControl);
            }
            drawnElementList.Clear();
        }

        private double getMax(List<double> list)
        {
            double max = 0;
            if (list.Count == 0)
            {
                return 0;
            }
            int start = 0;
            while (list[start] == null)
            {
                start = start + 1;
            }
            max = (double)list[start];
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    continue;
                }
                if ((double)list[i] > max)
                {
                    max = (double)list[i];
                }
            }
            return max;
        }

        private double getMin(List<double> list)
        {
            double min = 0;
            if (list.Count == 0)
            {
                return 0;
            }
            int start = 0;
            while (list[start] == null)
            {
                start = start + 1;
            }
            min = (double)list[start];
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    continue;
                }
                if ((double)list[i] < min)
                {
                    min = (double)list[i];
                }
            }
            return min;
        }

        private List<double> normalize(List<double> list, bool mmax)
        {
            double max = getMax(list);
            double min = getMin(list);
            double score = 0;

            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] == null)
                {
                    continue;
                }
                if (mmax)
                {
                    //采用公式1, 目标最大化.
                    score = ((double)list[i] - min) / (max - min);
                }
                else
                {
                    //采用公式2, 目标最小化.
                    score = (max - (double)list[i]) / (max - min);
                }
                list[i] = score;
            }

            return list;
        }
    }
}
