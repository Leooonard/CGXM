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
using ESRI.ArcGIS.esriSystem;
using System.Threading;

namespace Intersect
{
    class SiteSelector
    {
        private Geoprocessor gp;
        private AxMapControl mapControl;
        private AxTOCControl tocControl;
        private const string fishnetPolygonName = "polygon.shp";
        private const string fishnetName = "fishnet.shp";
        private List<Feature> featureList;
        private ObservableCollection<Condition> conditionList;
        private List<Condition> restraintConditionList;
        private List<Condition> standardConditionList;
        private NetSize netSize;
        private Program program;
        private Project project;
        private double totalStandardValue;
        private IFeature baseFeature;
        private IGeometry rootGeometry;

        public SiteSelector(AxMapControl mc, AxTOCControl tc, int programID)
        {
            Init(mc, tc, programID);
        }

        public SiteSelector(AxMapControl mc, AxTOCControl tc, List<Feature> feaList, int programID)
            :this(mc, tc, programID)
        {
            featureList = feaList;
        }

        private void Init(AxMapControl mc, AxTOCControl tc, int programID)
        {
            //这里是window.show()的一个坑. show其实等同于设置窗口的visibility:visible.
            gp = new Geoprocessor();
            mapControl = mc;
            tocControl = tc;

            program = new Program();
            program.id = programID;
            program.select();

            project = new Project();
            project.id = program.projectID;
            project.select();

            netSize = program.getRelatedNetSize();

            conditionList = program.getAllRelatedCondition();
            restraintConditionList = new List<Condition>();
            standardConditionList = new List<Condition>();
            for (int i = 0; i < conditionList.Count; i++)
            {
                Condition condition = conditionList[i];
                if (condition.type == Const.CONFIG_TYPE_RESTRAINT)
                {
                    restraintConditionList.Add(condition);
                }
                else
                {
                    totalStandardValue += condition.value;
                    standardConditionList.Add(condition);
                }
            }

            //对总的权值做个检查, 如果不是100就怎么样?

            baseFeature = GisTool.GetBaseFeature(mapControl, project.baseMapIndex);

            featureList = new List<Feature>();
        }

        private IFeatureClass createFishnetFeatureClass(IGeometry baseGeometry)
        {
            Dictionary<string, double> dict = GisTool.GetExternalRectDimension(baseGeometry);
            GisTool.CreateEnvelopFishnet(netSize.width, netSize.height, System.IO.Path.Combine(program.path, fishnetName), dict);

            GisTool.FeatureToPolygon(System.IO.Path.Combine(program.path, fishnetName), System.IO.Path.Combine(program.path, fishnetPolygonName));
            GisTool.DeleteShapeFile(System.IO.Path.Combine(program.path, fishnetName)); //删除中间文件.

            IFeatureClass featureClass = GisTool.getFeatureClass(program.path, fishnetPolygonName); //获得网格类, 其中的网格已成面.
            return featureClass;
        }

        private void deleteFishnetFeatureClass()
        {
            GisTool.DeleteShapeFile(System.IO.Path.Combine(program.path, fishnetPolygonName));
        }

        public bool startSelectSite()
        {
            deleteShapeFile("评价结果");

            rootGeometry = baseFeature.Shape;

            IFeatureClass fishnetFeatureClass = createFishnetFeatureClass(rootGeometry);
            ISpatialFilter filter = new SpatialFilterClass();
            filter.Geometry = rootGeometry;
            filter.GeometryField = "SHAPE";
            filter.SpatialRel = esriSpatialRelEnum.esriSpatialRelIntersects;
            IFeatureCursor cursor = fishnetFeatureClass.Search(filter, false);
            IFeature feature;
            while ((feature = cursor.NextFeature()) != null)
            {
                Feature fea = new Feature();
                fea.inUse = 1;
                fea.score = 0;
                fea.relativeFeature = feature;
                featureList.Add(fea);
            }

            for (int i = 0; i < restraintConditionList.Count; i++)
            {
                Condition condition = restraintConditionList[i];
                Label label = new Label();
                label.id = condition.labelID;
                label.select();
                string targetLayerName = label.mapLayerName;
                string targetLayerPath = label.mapLayerPath;
                if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_DISTANCE_BIGGER)
                {
                    distanceRestraint(targetLayerName, condition.value, featureList,
                        new DistanceFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                        {
                            return filteredGeometry != null;
                        }),
                        new DistanceRestraintHandler(delegate(double distance, double restraint)
                        {
                            return distance <= restraint;
                        }));
                }
                else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_DISTANCE_BIGGEREQUAL)
                {
                    distanceRestraint(targetLayerName, condition.value, featureList,
                        new DistanceFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                        {
                            return filteredGeometry != null;
                        }),
                        new DistanceRestraintHandler(delegate(double distance, double restraint)
                        {
                            return distance < restraint;
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
                        new DistanceRestraintHandler(delegate(double distance, double restraint)
                        {
                            return distance >= restraint;
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
                        new DistanceRestraintHandler(delegate(double distance, double restraint)
                        {
                            return distance > restraint;
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
                        new IntersectRestraintHandler(delegate(double ratio, double restraint)
                        {
                            return ratio <= restraint;
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
                        new IntersectRestraintHandler(delegate(double ratio, double restraint)
                        {
                            return ratio < restraint;
                        }));
                }
                else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_INTERSECT_SMALLER)
                {
                    IntersectRestraint(targetLayerName, condition.value, featureList,
                        new IntersectFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                        {
                            return filteredGeometry != null;
                        }),
                        new IntersectRestraintHandler(delegate(double ratio, double restraint)
                        {
                            return ratio >= restraint;
                        }));
                }
                else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_INTERSECT_SMALLEREQUAL)
                {
                    IntersectRestraint(targetLayerName, condition.value, featureList,
                        new IntersectFilterResultHandler(delegate(IGeometry filteredGeometry, List<Feature> feaList)
                        {
                            return filteredGeometry != null;
                        }),
                        new IntersectRestraintHandler(delegate(double ratio, double restraint)
                        {
                            return ratio > restraint;
                        }));
                }
                else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_HEIGHT_BIGGER)
                {
                    biggerOverlapRestraint(targetLayerPath, condition.value, featureList);
                }
                else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_HEIGHT_BIGGEREQUAL)
                {
                    biggerEqualOverlapRestraint(targetLayerPath, condition.value, featureList);
                }
                else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_HEIGHT_SMALLER)
                {
                    smallerOverlapRestraint(targetLayerPath, condition.value, featureList);
                }
                else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_HEIGHT_SMALLEREQUAL)
                {
                    smallerEqualOverlapRestraint(targetLayerPath, condition.value, featureList);
                }
                else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_SLOPE_BIGGER)
                {
                    biggerOverlapRestraint(targetLayerPath, condition.value / 100, featureList);
                }
                else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_SLOPE_BIGGEREQUAL)
                {
                    biggerEqualOverlapRestraint(targetLayerPath, condition.value / 100, featureList);
                }
                else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_SLOPE_SMALLER)
                {
                    smallerOverlapRestraint(targetLayerPath, condition.value / 100, featureList);
                }
                else if (condition.category == Const.CONFIG_CATEGORY_RESTRAINT_SLOPE_SMALLEREQUAL)
                {
                    smallerEqualOverlapRestraint(targetLayerPath, condition.value / 100, featureList);
                }
            }

            for (int i = 0; i < standardConditionList.Count; i++)
            {
                Condition condition = standardConditionList[i];
                Label label = new Label();
                label.id = condition.labelID;
                label.select();
                string targetLayerName = label.mapLayerName;
                string targetLayerPath = label.mapLayerPath;

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

            if (featureList.Count == 0)
            {
                return false;
            }

            deleteFishnetFeatureClass();
            saveShapeFile("评价结果.shp");
            addShapeFile("评价结果.shp", "评价结果");

            return true;
        }

        private void saveShapeFile(string shpName)
        {
            GisTool.CreateShapefile(program.path, shpName, mapControl.SpatialReference);
            IFeatureClass resultFeatureClass = GisTool.getFeatureClass(program.path, shpName);
            GisTool.addFeatureLayerField(resultFeatureClass, "评价值", esriFieldType.esriFieldTypeDouble, 3);

            GisTool.AddFeaturesToFeatureClass(featureList, resultFeatureClass, "评价值");
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
                classBreakRender(layerName, "评价值");
                mapControl.ActiveView.Refresh();

                return true;
            }

            return false;
        }

        private void classBreakRender(string layerName, string fieldName)
        {
            IGeoFeatureLayer pGeoFeatureLayer;
            ITable pTable;
            ITableHistogram pTableHistogram;
            IBasicHistogram pBasicHistogram = new BasicTableHistogramClass();
            object dataFrequency;
            object dataValues;

            pGeoFeatureLayer = (IGeoFeatureLayer)GisTool.getLayerByName(layerName, mapControl);
           pTable = (ITable)pGeoFeatureLayer;

            pTableHistogram = (ITableHistogram)pBasicHistogram;
            pTableHistogram.Field = fieldName;
            pTableHistogram.Table = pTable;
            pBasicHistogram.GetHistogram(out dataValues, out dataFrequency);

            IClassifyGEN pClassifyGen = new EqualIntervalClass();
            double[] Classes = new double[10000];
            int ClassesCount;
            int i = 6;
            pClassifyGen.Classify(dataValues, dataFrequency, ref i);
            Classes = (double[])pClassifyGen.ClassBreaks;
            ClassesCount = int.Parse(Classes.GetUpperBound(0).ToString());

            IClassBreaksRenderer pClassBreaksRender = new ClassBreaksRendererClass();
            pClassBreaksRender.Field = fieldName;
            pClassBreaksRender.BreakCount = ClassesCount;
            pClassBreaksRender.SortClassesAscending = true;

            IRgbColor pFromColor = new RgbColorClass();
            pFromColor = GisTool.getColor(255, 255, 255);
            IRgbColor pToColor = new RgbColorClass();
            pToColor = GisTool.getColor(255, 0, 0);
            IAlgorithmicColorRamp pColorRamp = new AlgorithmicColorRampClass();
            pColorRamp.FromColor = pFromColor;
            pColorRamp.ToColor = pToColor;
            pColorRamp.Size = ClassesCount;
            bool o = true;
            pColorRamp.CreateRamp(out o);

            IEnumColors pEnumColors;
            pEnumColors = pColorRamp.Colors;
            pEnumColors.Reset();
            IColor pColor;
            ISimpleFillSymbol pFillSymbol;
            int breakIndex;
            for (breakIndex = 0; breakIndex < ClassesCount - 1; breakIndex++)
            {
                pColor = pEnumColors.Next();
                pFillSymbol = new SimpleFillSymbolClass();
                pFillSymbol.Color = pColor;
                pFillSymbol.Style = esriSimpleFillStyle.esriSFSSolid;
                pClassBreaksRender.set_Symbol(breakIndex, (ISymbol)pFillSymbol);
                pClassBreaksRender.set_Break(breakIndex, Classes[breakIndex + 1]);
            }
            pGeoFeatureLayer.Renderer = (IFeatureRenderer)pClassBreaksRender;
            tocControl.SetBuddyControl(mapControl);
            tocControl.Refresh();
            mapControl.ActiveView.Refresh();
        }

        private delegate bool IntersectFilterResultHandler(IGeometry filteredGeometry, List<Feature> featureList);
        private delegate bool IntersectRestraintHandler(double ratio, double restraint);

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
                bool isRemove = restraintHandler(ratio, restraint);
                if (isRemove == true)
                {
                    featureList.Remove(feature);
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
        private delegate bool DistanceRestraintHandler(double distance, double restraint);
             
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
                bool isRemove = restraintHandler(distance, restraint);
                if (isRemove == true)
                {
                    featureList.Remove(feature);
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

        

        private List<double> normalize(List<double> list, bool mmax)
        {
            double max = Tool.GetMax(list);
            double min = Tool.GetMin(list);
            double score = 0;

            if (max == min)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    list[i] = 1;
                }
                return list;
            }

            for (int i = 0; i < list.Count; i++)
            {
                if (mmax)
                {
                    //采用公式1, 目标最大化.
                    score = (list[i] - min) / (max - min);
                }
                else
                {
                    //采用公式2, 目标最小化.
                    score = (max - list[i]) / (max - min);
                }
                list[i] = score;
            }

            return list;
        }
    }
}
