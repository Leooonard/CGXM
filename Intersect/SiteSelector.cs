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
            baseFeature = GisUtil.GetBaseFeature(mapControl, project.baseMapIndex);
            mapFolder = System.IO.Path.GetDirectoryName(project.path);
            targetFolder = generateFolder(mapFolder);
            foreach (Condition condition in conditionList)
            {
                if (condition.type == C.CONFIG_TYPE_STANDARD)
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
            IGeometry allGeom = baseFeature.Shape;

            Dictionary<string, double> dict = GisUtil.GetExternalRectDimension(allGeom);
            GisUtil.CreateEnvelopFishnet(fishnetWidth, fishnetHeight, getFullPath(targetFolder, fishnetName), dict);
            GisUtil.FeatureToPolygon(getFullPath(targetFolder, fishnetName), getFullPath(targetFolder, fishnetPolygonName));

            IFeatureClass featureClass = GisUtil.getFeatureClass(targetFolder, fishnetPolygonName); //获得网格类, 其中的网格已成面.
            for (int i = 0; i < featureClass.FeatureCount(null); i++)
            {
                Feature feature = new Feature();
                featureList.Add(feature);
            }
            for (int i = 0; i < conditionList.Count; i++)
            {
                Condition condition = conditionList[i];
                Label label = new Label();
                label.id = condition.labelID;
                label.select();
                string targetLyName = GisUtil.GetShpNameByMapLayerName(mapControl, label.mapLayerName) + ".shp";
                if (condition.type == C.CONFIG_TYPE_RESTRAINT)
                {
                    if (condition.category == C.CONFIG_CATEGORY_DISTANCE_NEGATIVE)
                    {
                        negativeDistanceRestraint(featureClass, targetLyName, condition.value, featureList);
                    }
                    else if (condition.category == C.CONFIG_CATEGORY_DISTANCE_POSITIVE)
                    {
                        positiveDistanceRestraint(featureClass, targetLyName, condition.value, featureList);
                    }
                    else if (condition.category == C.CONFIG_CATEGORY_INTERSECT_NEGATIVE)
                    {
                        negativeIntersectRestraint(featureClass, targetLyName, condition.value, featureList);
                    }
                    else if (condition.category == C.CONFIG_CATEGORY_INTERSECT_POSITIVE)
                    {
                        positiveIntersectRestraint(featureClass, targetLyName, condition.value, featureList);
                    }
                    else if (condition.category == C.CONFIG_CATEGORY_OVERLAP_NEGATIVE)
                    {
                        negativeOverlapRestraint(featureClass, targetLyName, condition.value, featureList);
                    }
                    else if (condition.category == C.CONFIG_CATEGORY_OVERLAP_POSITIVE)
                    {
                        positiveOverlapRestraint(featureClass, targetLyName, condition.value, featureList);
                    }
                }
                else if(condition.type == C.CONFIG_TYPE_STANDARD)
                {
                    if (condition.category == C.CONFIG_CATEGORY_DISTANCE_NEGATIVE)
                    {
                        negativeDistanceStandard(featureClass, targetLyName, condition.value / totalStandardValue, featureList);
                    }
                    else if (condition.category == C.CONFIG_CATEGORY_DISTANCE_POSITIVE)
                    {
                        positiveDistanceStandard(featureClass, targetLyName, condition.value / totalStandardValue, featureList);
                    }
                    else if (condition.category == C.CONFIG_CATEGORY_OVERLAP_NEGATIVE)
                    {
                        negativeOverlapStandard(featureClass, targetLyName, condition.value / totalStandardValue, featureList);
                    }
                    else if (condition.category == C.CONFIG_CATEGORY_OVERLAP_POSITIVE)
                    {
                        positiveOverlapStandard(featureClass, targetLyName, condition.value / totalStandardValue, featureList);
                    }
                }
            }

            DrawSelectResult(featureClass);
            return featureList;
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

        private List<double> IntersectRestraint(IFeatureClass featureClass, string targetLayerName)
        {
            List<double> ratioList = new List<double>();
            IFeatureClass targetFeatureClass = GisUtil.getFeatureClass(mapFolder, targetLayerName);
            List<IGeometry> geometryList = new List<IGeometry>();
            for (int i = 0; i < targetFeatureClass.FeatureCount(null); i++)
            {
                geometryList.Add(targetFeatureClass.GetFeature(i).Shape);
            }
            IGeometry geometry = GisUtil.unionAllFeature(geometryList); //e0图层所有的几何图形.
            ITopologicalOperator toOp = geometry as ITopologicalOperator;

            for (int i = 0; i < featureClass.FeatureCount(null); i++)
            {
                IFeature feature = featureClass.GetFeature(i);
                IGeometry srcGeom = feature.ShapeCopy;
                srcGeom.SpatialReference = mapControl.SpatialReference;
                IGeometry intersectedGeom = toOp.Intersect(srcGeom, esriGeometryDimension.esriGeometry2Dimension);
                //测量相交图形的面积, 超过原图形的一半, 有效.
                IArea area = intersectedGeom as IArea;
                IArea srcArea = srcGeom as IArea;
                double ratio = (area.Area / srcArea.Area) * 100;
                ratioList.Add(ratio);
            }
            return ratioList;
        }

        private void positiveIntersectRestraint(IFeatureClass featureClass, string targetLayerName, double restraint, List<Feature> featureList)
        {
            List<double> ratioList = IntersectRestraint(featureClass, targetLayerName);
            for (int i = 0; i < featureList.Count; i++)
            {
                if (ratioList[i] > restraint)
                {
                    featureList[i].inUse = 1;
                }
                else
                {
                    featureList[i].inUse = 0;
                }
            }
        }

        private void negativeIntersectRestraint(IFeatureClass featureClass, string targetLayerName, double restraint, List<Feature> featureList)
        {
            List<double> ratioList = IntersectRestraint(featureClass, targetLayerName);
            for (int i = 0; i < featureList.Count; i++)
            {
                if (ratioList[i] < restraint)
                {
                    featureList[i].inUse = 1;
                }
                else
                {
                    featureList[i].inUse = 0;
                }
            }
        }

        private List<double> distanceRestraint(IFeatureClass featureClass, string targetLayerName)
        {
            List<double> distanceList = new List<double>();
            IFeatureClass targetFeatureClass = GisUtil.getFeatureClass(mapFolder, targetLayerName);
            //将所有的geometry放入一个arraylist.
            List<IGeometry> geometryList = new List<IGeometry>();
            for (int i = 0; i < targetFeatureClass.FeatureCount(null); i++)
            {
                geometryList.Add(targetFeatureClass.GetFeature(i).ShapeCopy);
            }
            IGeometry geometry = GisUtil.unionAllFeature(geometryList);
            IProximityOperator proOp = geometry as IProximityOperator;

            for (int i = 0; i < featureClass.FeatureCount(null); i++)
            {
                IFeature fea = featureClass.GetFeature(i);
                IGeometry geom = fea.ShapeCopy;
                geom.SpatialReference = mapControl.SpatialReference;
                double distance = proOp.ReturnDistance(geom); //获取两者间的距离.
                distanceList.Add(distance);
            }
            return distanceList;
        }

        private void positiveDistanceRestraint(IFeatureClass featureClass, string targetLayerName, double restraint, List<Feature> featureList)
        {
            List<double> distanceList = distanceRestraint(featureClass, targetLayerName);
            for (int i = 0; i < featureList.Count; i++)
            {
                if (featureList[i].inUse == 1)
                {
                    if (distanceList[i] > restraint)
                    {
                        featureList[i].inUse = 1;
                    }
                    else
                    {
                        featureList[i].inUse = 0;
                    }
                }
            }
        }

        private void negativeDistanceRestraint(IFeatureClass featureClass, string targetLayerName, double restraint, List<Feature> featureList)
        {
            List<double> distanceList = distanceRestraint(featureClass, targetLayerName);
            for (int i = 0; i < featureList.Count; i++)
            {
                if (featureList[i].inUse == 1)
                {
                    if (distanceList[i] < restraint)
                    {
                        featureList[i].inUse = 1;
                    }
                    else
                    {
                        featureList[i].inUse = 0;
                    }
                }
            }
        }

        private List<double> overlapRestraint(IFeatureClass featureClass, string targetLayerName)
        {
            List<double> overlapList = new List<double>();
            IFeatureClass targetFeatureClass = GisUtil.getFeatureClass(mapFolder, targetLayerName);

            for (int i = 0; i < featureClass.FeatureCount(null); i++)
            {
                IFeature fea = featureClass.GetFeature(i);
                IGeometry geom = fea.ShapeCopy;
                geom.SpatialReference = mapControl.SpatialReference;
                ITopologicalOperator geomTopoOp = geom as ITopologicalOperator;
                IArea geomArea = geom as IArea;
                ISpatialFilter cityFilter = new SpatialFilterClass();
                cityFilter.Geometry = geom;
                cityFilter.GeometryField = "SHAPE";
                cityFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                IFeatureCursor feaCursor = targetFeatureClass.Search(cityFilter, false);

                //获取所有与该块相交的要素, 然后检测其相交的面积, 依照比例计算最终的值.
                IFeature overlapFeature = feaCursor.NextFeature();
                double overlapFeatureValue = 0;
                while (overlapFeature != null)
                {
                    if (overlapFeature != null)
                    {
                        IGeometry intersectedGeom = geomTopoOp.Intersect(overlapFeature.ShapeCopy, esriGeometryDimension.esriGeometry2Dimension);
                        IArea intersectedGeomArea = intersectedGeom as IArea;
                        //获取gridcode.
                        ITable table = overlapFeature.Table;
                        IRow row = table.GetRow(0);
                        int fieldNum = table.FindField("GRIDCODE");
                        double value = (double)overlapFeature.get_Value(fieldNum);
                        overlapFeatureValue = overlapFeatureValue + value * (intersectedGeomArea.Area / geomArea.Area);
                    }
                    overlapFeature = feaCursor.NextFeature();
                }
                overlapList.Add(overlapFeatureValue);
            }
            return overlapList;
        }

        private void positiveOverlapRestraint(IFeatureClass featureClass, string targetLayerName, double restraint, List<Feature> featureList)
        {
            List<double> overlapList = overlapRestraint(featureClass, targetLayerName);
            for (int i = 0; i < featureList.Count; i++)
            {
                if (featureList[i].inUse == 1)
                {
                    if (overlapList[i] > restraint)
                    {
                        featureList[i].inUse = 1;
                    }
                    else
                    {
                        featureList[i].inUse = 0;
                    }
                }
            }
        }

        private void negativeOverlapRestraint(IFeatureClass featureClass, string targetLayerName, double restraint, List<Feature> featureList)
        {
            List<double> overlapList = overlapRestraint(featureClass, targetLayerName);
            for (int i = 0; i < featureList.Count; i++)
            {
                if (featureList[i].inUse == 1)
                {
                    if (overlapList[i] < restraint && overlapList[i] != 0)
                    {
                        featureList[i].inUse = 1;
                    }
                    else
                    {
                        featureList[i].inUse = 0;
                    }
                }
            }
        }

        private List<double> distanceStandard(IFeatureClass featureClass, string targetLayerName)
        {
            List<double> distanceList = new List<double>();
            IFeatureClass targetFeatureClass = GisUtil.getFeatureClass(mapFolder, targetLayerName);
            //将所有的geometry放入一个arraylist.
            List<IGeometry> geometryList = new List<IGeometry>();
            for (int i = 0; i < targetFeatureClass.FeatureCount(null); i++)
            {
                geometryList.Add(targetFeatureClass.GetFeature(i).ShapeCopy);
            }
            IGeometry geometry = GisUtil.unionAllFeature(geometryList);
            IProximityOperator proOp = geometry as IProximityOperator;

            //求到路的距离.
            for (int i = 0; i < featureClass.FeatureCount(null); i++)
            {
                IFeature fea = featureClass.GetFeature(i);
                IGeometry geom = fea.ShapeCopy;
                geom.SpatialReference = mapControl.SpatialReference;
                double distance = proOp.ReturnDistance(geom);
                distanceList.Add(distance);
            }
            return distanceList;
        }

        private void positiveDistanceStandard(IFeatureClass featureClass, string targetLayerName, double standard, List<Feature> featureList)
        {
            List<double> distanceList = distanceStandard(featureClass, targetLayerName);
            distanceList = normalize(distanceList, true);
            for (int i = 0; i < featureList.Count; i++)
            {
                if (featureList[i].inUse == 1)
                {
                    featureList[i].score += standard * distanceList[i];
                }
            }
        }

        private void negativeDistanceStandard(IFeatureClass featureClass, string targetLayerName, double standard, List<Feature> featureList)
        {
            List<double> distanceList = distanceStandard(featureClass, targetLayerName);
            distanceList = normalize(distanceList, false);
            for (int i = 0; i < featureList.Count; i++)
            {
                if (featureList[i].inUse == 1)
                {
                    featureList[i].score += standard * distanceList[i];
                }
            }
        }

        private List<double> overlapStandard(IFeatureClass featureClass, string targetLayerName)
        {
            List<double> overlapList = new List<double>();
            IFeatureClass targetFeatureClass = GisUtil.getFeatureClass(mapFolder, targetLayerName);
            for (int i = 0; i < featureClass.FeatureCount(null); i++)
            {
                IFeature feature = featureClass.GetFeature(i);
                IGeometry geometry = feature.ShapeCopy;
                geometry.SpatialReference = mapControl.SpatialReference;
                ITopologicalOperator tpOp = geometry as ITopologicalOperator;
                IArea area = geometry as IArea;
                ISpatialFilter cityFilter = new SpatialFilterClass();
                cityFilter.Geometry = geometry;
                cityFilter.GeometryField = "SHAPE";
                cityFilter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
                IFeatureCursor feaCursor = targetFeatureClass.Search(cityFilter, false);

                //获取所有与该块相交的要素, 然后检测其相交的面积, 依照比例计算最终的值.
                IFeature cityFea = feaCursor.NextFeature();
                double overlap = 0;
                while (cityFea != null)
                {
                    if (cityFea != null)
                    {
                        IGeometry intersectedGeom = tpOp.Intersect(cityFea.ShapeCopy, esriGeometryDimension.esriGeometry2Dimension);
                        IArea intersectedGeomArea = intersectedGeom as IArea;
                        //获取gridcode.
                        ITable table = cityFea.Table;
                        IRow row = table.GetRow(0);
                        int fieldNum = table.FindField("GRIDCODE");
                        double value = (double)cityFea.get_Value(fieldNum);
                        overlap = overlap + value * (intersectedGeomArea.Area / area.Area);
                    }
                    cityFea = feaCursor.NextFeature();
                }
                overlapList.Add(overlap);
            }
            return overlapList;
        }

        private void positiveOverlapStandard(IFeatureClass featureClass, string targetLayerName, double standard, List<Feature> featureList)
        {
            List<double> overlapList = overlapStandard(featureClass, targetLayerName);
            overlapList = normalize(overlapList, true);
            for (int i = 0; i < featureList.Count; i++)
            {
                if (featureList[i].inUse == 1)
                {
                    featureList[i].score += standard * overlapList[i];
                }
            }
        }

        private void negativeOverlapStandard(IFeatureClass featureClass, string targetLayerName, double standard, List<Feature> featureList)
        {
            List<double> overlapList = overlapStandard(featureClass, targetLayerName);
            overlapList = normalize(overlapList, false);
            for (int i = 0; i < featureList.Count; i++)
            {
                if (featureList[i].inUse == 1)
                {
                    featureList[i].score += standard * overlapList[i];
                }
            }
        }

        private void DrawSelectResult(IFeatureClass fishnetFeaCls)
        {
            //第八, 按不同的成绩用不同的颜色画方块.
            for (int i = 0; i < featureList.Count; i++)
            {
                if (((Feature)featureList[i]).inUse == 1)
                {
                    GisUtil.drawPolygonByScore(fishnetFeaCls.GetFeature(i).ShapeCopy, ((Feature)featureList[i]).score, mapControl);
                }
                else
                {
                    GisUtil.drawPolygonByScore(fishnetFeaCls.GetFeature(i).ShapeCopy, 0, mapControl);
                }
            }
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
