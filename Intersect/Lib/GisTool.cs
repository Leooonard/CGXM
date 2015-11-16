using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geoprocessor;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.DataSourcesFile;
using System.Collections;
using System.IO;
using System.Drawing;
using System.Threading;
using Intersect.Lib;

namespace Intersect
{
    public class GisTool
    {
        public static void ResetToolbarControl(AxToolbarControl toolbarControl)
        {
            toolbarControl.CurrentTool = null;
        }

        public static IPolygon MakePolygonFromRing(Ring ring)
        {
            object missing = Type.Missing;
            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring as IGeometry, ref missing, missing);
            IPolygon polygon = pointPolygon as IPolygon;
            polygon.SimplifyPreserveFromTo();
            return polygon;
        }

        public static void CreateEnvelopFishnet(double width, double height, string outputPath, Dictionary<string, double> dim)
        {
            Geoprocessor gp = new Geoprocessor();
            ESRI.ArcGIS.DataManagementTools.CreateFishnet fishnetTool = new ESRI.ArcGIS.DataManagementTools.CreateFishnet();
            fishnetTool.number_columns = 0;
            fishnetTool.number_rows = 0;
            fishnetTool.cell_height = height;
            fishnetTool.cell_width = width;

            fishnetTool.out_feature_class = outputPath;
            fishnetTool.origin_coord = (dim["xMin"]).ToString() + " " + (dim["yMin"]).ToString();
            fishnetTool.y_axis_coord = (dim["xMin"]).ToString() + " " + (dim["yMin"] + 1).ToString();
            fishnetTool.corner_coord = (dim["xMax"]).ToString() + " " + (dim["yMax"]).ToString();

            gp.OverwriteOutput = true;
            gp.Execute(fishnetTool, null);
        }

        public static List<IPolygon> GetPolygonListFromPolylineList(string polylineListShpPath, string outputTempPath)
        {
            if (!File.Exists(polylineListShpPath))
            {
                return null;
            }
            if (File.Exists(outputTempPath))
            {
                FileHelper.DeleteSameNameFiles(System.IO.Path.GetDirectoryName(outputTempPath),
                                                System.IO.Path.GetFileNameWithoutExtension(outputTempPath));
            }
            List<IPolygon> polygonList = new List<IPolygon>();
            Geoprocessor geoProcessor = new Geoprocessor();
            ESRI.ArcGIS.DataManagementTools.FeatureToPolygon featureToPolygonTool = new ESRI.ArcGIS.DataManagementTools.FeatureToPolygon();
            featureToPolygonTool.in_features = polylineListShpPath;
            featureToPolygonTool.out_feature_class = outputTempPath;
            geoProcessor.OverwriteOutput = true;
            geoProcessor.Execute(featureToPolygonTool, null);
            IFeatureClass featureClass = getFeatureClass(System.IO.Path.GetDirectoryName(outputTempPath), System.IO.Path.GetFileName(outputTempPath));
            for (int i = 0; i < featureClass.FeatureCount(null); i++)
            {
                IFeature feature = featureClass.GetFeature(i);
                IPolygon polygon = feature.ShapeCopy as IPolygon;
                polygonList.Add(polygon);
            }
            if (polygonList.Count == 0)
                return null;
            else
                return polygonList;
        }

        public static string ConvertRasterLayerToFeatureLayer(string folder, IRasterLayer rLayer)
        {
            string layerName = rLayer.Name;
            string destPath = System.IO.Path.Combine(new string[] { folder, layerName + ".shp" });
            string tempIntFolder = System.IO.Path.Combine(folder, "temp_int");
            if (Directory.Exists(tempIntFolder))
            {
                Directory.Delete(tempIntFolder, true);
            }
            GisTool.Int(rLayer.FilePath, tempIntFolder);
            Geoprocessor gp = new Geoprocessor();
            ESRI.ArcGIS.ConversionTools.RasterToPolygon rasterToPolygon = new ESRI.ArcGIS.ConversionTools.RasterToPolygon();
            if (File.Exists(destPath))
            {
                FileHelper.DeleteSameNameFiles(folder,
                                                System.IO.Path.GetFileNameWithoutExtension(destPath));
            }
            rasterToPolygon.in_raster = tempIntFolder;
            rasterToPolygon.out_polygon_features = destPath;
            rasterToPolygon.raster_field = "VALUE";
            gp.Execute(rasterToPolygon, null);
            if (Directory.Exists(tempIntFolder))
            {
                Directory.Delete(tempIntFolder, true);
            }
            return destPath;
        }

        public static void FeatureToPolygon(string inputPath, string outputPath)
        {
            Geoprocessor gp = new Geoprocessor();
            ESRI.ArcGIS.DataManagementTools.FeatureToPolygon feaToPoly = new ESRI.ArcGIS.DataManagementTools.FeatureToPolygon();
            feaToPoly.in_features = inputPath;
            feaToPoly.out_feature_class = outputPath;
            gp.Execute(feaToPoly, null);
        }

        public static void Int(string rasterPath, string targetFolder)
        {
            Geoprocessor gp = new Geoprocessor();
            ESRI.ArcGIS.SpatialAnalystTools.Int intTool = new ESRI.ArcGIS.SpatialAnalystTools.Int();
            intTool.in_raster_or_constant = rasterPath;
            intTool.out_raster = targetFolder;

            gp.OverwriteOutput = true;
            gp.Execute(intTool, null);
        }

        public static Dictionary<string, double> GetExternalRectDimension(IGeometry geom)
        {
            IEnvelope envelop = geom.Envelope;
            double xMin = envelop.XMin;
            double xMax = envelop.XMax;
            double yMin = envelop.YMin;
            double yMax = envelop.YMax;
            Dictionary<string, double> dim = new Dictionary<string, double>();
            dim.Add("xMin", xMin);
            dim.Add("xMax", xMax);
            dim.Add("yMin", yMin);
            dim.Add("yMax", yMax);
            return dim;
        }

        public static IGeometry unionAllFeature(List<IGeometry> geometryList)
        {
            if (geometryList.Count == 0)
            {
                return new PolygonClass();
            }
            else if (geometryList.Count == 1)
            {
                return geometryList[0] as IGeometry;
            }
            else
            {
                IGeometry geom = geometryList[0] as IGeometry;

                ITopologicalOperator topoOp = geom as ITopologicalOperator;
                IGeometryCollection pGeoCol = new GeometryBagClass();//定义Geometry类集合
                for (int i = 1; i < geometryList.Count; i++)
                {
                    IGeometry tempGeom = geometryList[i] as IGeometry;
                    pGeoCol.AddGeometry(tempGeom);
                }
                IEnumGeometry enumGeom = pGeoCol as IEnumGeometry;
                topoOp.ConstructUnion(enumGeom);

                return geom;
            }
        }

        public static IPolygon MakePolygon(double upperLeftX, double upperLeftY, double width, double height)
        {
            IPoint upperLeftPt = new PointClass();
            upperLeftPt.X = upperLeftX;
            upperLeftPt.Y = upperLeftY;
            IPoint upperRightPt = new PointClass();
            upperRightPt.X = upperLeftPt.X + width;
            upperRightPt.Y = upperLeftPt.Y;
            IPoint lowerLeftPt = new PointClass();
            lowerLeftPt.X = upperLeftPt.X;
            lowerLeftPt.Y = upperLeftPt.Y - height;
            IPoint lowerRightPt = new PointClass();
            lowerRightPt.X = lowerLeftPt.X + width;
            lowerRightPt.Y = lowerLeftPt.Y;
            Ring ring = new RingClass();
            ring.AddPoint(upperLeftPt);
            ring.AddPoint(upperRightPt);
            ring.AddPoint(lowerRightPt);
            ring.AddPoint(lowerLeftPt);
            IPolygon polygon = GisTool.MakePolygonFromRing(ring);
            return polygon;
        }


        public static IRgbColor RandomRgbColor()
        {
            IRgbColor color = new RgbColor();
            Random rand = new Random();
            color.Red = rand.Next(0, 256);
            color.Green = rand.Next(0, 256);
            color.Blue = rand.Next(0, 256);
            Thread.Sleep(15);

            return color;
        }

        public static void DrawLineWithText(IPoint startPt, IPoint endPt, string text, AxMapControl mapControl)
        {
            double textAdjustGap = 1;
            IRgbColor textColor = new RgbColor();
            textColor.Red = 255;
            textColor.Green = 0;
            textColor.Blue = 0;

            IPolyline line = new PolylineClass();
            IPointCollection ptCol = line as IPointCollection;
            ptCol.AddPoint(startPt);
            ptCol.AddPoint(endPt);
            IPoint midPt = new PointClass();
            midPt.X = (startPt.X + endPt.X) / 2;
            midPt.Y = (startPt.Y + endPt.Y) / 2;
            if (startPt.Y == endPt.Y)
            {
                midPt.Y = midPt.Y + textAdjustGap;
            }
            GisTool.DrawPolyline(line, mapControl);
            GisTool.drawText(text, midPt, textColor, mapControl);
        }

        public static void drawText(string text, IPoint pt, IRgbColor color, AxMapControl mapControl)
        {
            ITextSymbol pTextSymbol = new TextSymbolClass();
            pTextSymbol.Size = 10;
            pTextSymbol.Color = color;
            ITextElement pTextElement = new TextElementClass();
            pTextElement.Text = text;
            pTextElement.ScaleText = false;
            pTextElement.Symbol = pTextSymbol;

            IElement pElement = pTextElement as IElement;
            pElement.Geometry = pt;

            IGraphicsContainer pGraphicsContainer = mapControl.Map as IGraphicsContainer;
            IActiveView pActiveView = mapControl.ActiveView;
            pGraphicsContainer.AddElement(pElement, 0);
            pActiveView.PartialRefresh(esriViewDrawPhase.esriViewGraphics, null, null);
        }

        private static IRgbColor GetDefaultRgbColor()
        {
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = 255;
            rgbColor.Green = 0;
            rgbColor.Blue = 0;
            return rgbColor;
        }

        public static void drawPolygon(IGeometry geom, AxMapControl mapControl, IRgbColor color = null)
        {
            ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
            simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSHollow;
            if(color != null)
            {
                simpleFillSymbol.Color = color;
            }
            else
            {
                simpleFillSymbol.Color = GetDefaultRgbColor();
            }
            IFillShapeElement fillShapeElement = new PolygonElementClass();
            fillShapeElement.Symbol = simpleFillSymbol;
            IElement element = fillShapeElement as IElement;
            element.Geometry = geom;
            IGraphicsContainer pGraphics = mapControl.Map as IGraphicsContainer;
            IActiveView pActiveView = mapControl.ActiveView;
            try
            {
                pGraphics.UpdateElement(element);
            }
            catch (Exception updateExp)
            {
                pGraphics.AddElement(element, 0);
            }
            pActiveView.Refresh();
        }

        public static void drawPolygonElement(IPolygonElement polygonElement, AxMapControl mapControl)
        {
            ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
            simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSHollow;
            simpleFillSymbol.Color = GetDefaultRgbColor();
            IFillShapeElement fillShapeElement = polygonElement as IFillShapeElement;
            fillShapeElement.Symbol = simpleFillSymbol;
            
            IMap map = mapControl.Map;
            IGraphicsContainer graphicsContainer = map as IGraphicsContainer;
            IActiveView activeView = mapControl.ActiveView;
            graphicsContainer.AddElement(polygonElement as IElement, 0);
            activeView.Refresh();
        }

        public static void UpdatePolygonElementColor(IPolygonElement polygonElement, AxMapControl mapControl, int red, int green, int blue)
        {
            IFillShapeElement fillShapeElement = polygonElement as IFillShapeElement;
            ILineSymbol oldLineSymbol = fillShapeElement.Symbol.Outline;
            ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = red;
            rgbColor.Green = green;
            rgbColor.Blue = blue;
            simpleFillSymbol.Color = rgbColor;
            simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSHollow;
            simpleFillSymbol.Outline = oldLineSymbol;
            fillShapeElement.Symbol = simpleFillSymbol;
            IMap map = mapControl.Map;
            IGraphicsContainer graphicsContainer = map as IGraphicsContainer;
            IActiveView activeView = mapControl.ActiveView;
            graphicsContainer.UpdateElement(fillShapeElement as IElement);
            activeView.Refresh();
        }

        public static void UpdatePolygonElementTransparentColor(IPolygonElement polygonElement, AxMapControl mapControl, int red, int green, int blue)
        {
            IFillShapeElement fillShapeElement = polygonElement as IFillShapeElement;
            ILineSymbol oldLineSymbol = fillShapeElement.Symbol.Outline;
            ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = red;
            rgbColor.Green = green;
            rgbColor.Blue = blue;
            rgbColor.Transparency = 0;
            simpleFillSymbol.Color = rgbColor;
            simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSHollow;
            simpleFillSymbol.Outline = oldLineSymbol;
            fillShapeElement.Symbol = simpleFillSymbol;
            IMap map = mapControl.Map;
            IGraphicsContainer graphicsContainer = map as IGraphicsContainer;
            IActiveView activeView = mapControl.ActiveView;
            graphicsContainer.UpdateElement(fillShapeElement as IElement);
            activeView.Refresh();
        }

        public static void UpdatePolygonElementOutline(IPolygonElement polygonElement, AxMapControl mapControl, int red, int green, int blue)
        {
            IFillShapeElement fillShapeElement = polygonElement as IFillShapeElement;
            IRgbColor oldFillColor = fillShapeElement.Symbol.Color as IRgbColor;
            ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
            ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = red;
            rgbColor.Green = green;
            rgbColor.Blue = blue;
            simpleLineSymbol.Color = rgbColor;
            simpleLineSymbol.Width = 3;
            simpleLineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;
            simpleFillSymbol.Outline = simpleLineSymbol;
            simpleFillSymbol.Color = oldFillColor;
            simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSSolid;
            fillShapeElement.Symbol = simpleFillSymbol;
            IMap map = mapControl.Map;
            IGraphicsContainer graphicsContainer = map as IGraphicsContainer;
            IActiveView activeView = mapControl.ActiveView;
            graphicsContainer.UpdateElement(fillShapeElement as IElement);
            activeView.Refresh();
        }

        public static void RestorePolygonElementColor(IPolygonElement polygonElement, AxMapControl mapControl)
        {
            IFillShapeElement fillShapeElement = polygonElement as IFillShapeElement;
            ILineSymbol oldLineSymbol = fillShapeElement.Symbol.Outline;
            ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
            simpleFillSymbol.Color = GetDefaultRgbColor();
            simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSSolid;
            simpleFillSymbol.Outline = oldLineSymbol;
            fillShapeElement.Symbol = simpleFillSymbol;
            IMap map = mapControl.Map;
            IGraphicsContainer graphicsContainer = map as IGraphicsContainer;
            IActiveView activeView = mapControl.ActiveView;
            graphicsContainer.UpdateElement(fillShapeElement as IElement);
            activeView.Refresh();
        }

        public static void RestorePolygonElementOutline(IPolygonElement polygonElement, AxMapControl mapControl)
        { 
            IFillShapeElement fillShapeElement = polygonElement as IFillShapeElement;
            IRgbColor oldFillColor = fillShapeElement.Symbol.Color as IRgbColor;
            ISimpleFillSymbol simpleFillSymbol = new SimpleFillSymbolClass();
            ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
            simpleLineSymbol.Color = GetDefaultRgbColor();
            simpleLineSymbol.Width = 0;
            simpleLineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;
            simpleFillSymbol.Outline = simpleLineSymbol;
            simpleFillSymbol.Color = oldFillColor;
            simpleFillSymbol.Style = esriSimpleFillStyle.esriSFSSolid;
            fillShapeElement.Symbol = simpleFillSymbol;
            IMap map = mapControl.Map;
            IGraphicsContainer graphicsContainer = map as IGraphicsContainer;
            IActiveView activeView = mapControl.ActiveView;
            graphicsContainer.UpdateElement(fillShapeElement as IElement);
            activeView.Refresh();
        }

        public static void DrawPolyline(IGeometry geom, AxMapControl mapControl)
        {
            ILineElement PolygonElement = new LineElementClass();
            IElement pElement = PolygonElement as IElement;
            pElement.Geometry = geom;
            IMap pMap = mapControl.Map;
            IGraphicsContainer pGraphicsContainer;
            IActiveView pActiveView = mapControl.ActiveView;
            pGraphicsContainer = pMap as IGraphicsContainer;
            pGraphicsContainer.AddElement((IElement)PolygonElement, 0);
            pActiveView.Refresh();
        }

        public static void DrawPolylineElement(ILineElement lineElement, AxMapControl mapControl)
        {
            ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
            simpleLineSymbol.Color = GetDefaultRgbColor();
            simpleLineSymbol.Width = 1;
            simpleLineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;
            lineElement.Symbol = simpleLineSymbol;
            IMap pMap = mapControl.Map;
            IActiveView pActiveView = mapControl.ActiveView;
            IGraphicsContainer pGraphicsContainer = pMap as IGraphicsContainer;
            pGraphicsContainer.AddElement((IElement)lineElement, 0);
            pActiveView.Refresh();
        }

        public static void UpdatePolylineElementColor(ILineElement lineElement, AxMapControl mapControl, int red, int green, int blue)
        {
            ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = red;
            rgbColor.Green = green;
            rgbColor.Blue = blue;
            simpleLineSymbol.Color = rgbColor;
            simpleLineSymbol.Width = 3;
            simpleLineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;
            lineElement.Symbol = simpleLineSymbol;
            IMap map = mapControl.Map;
            IActiveView activeView = mapControl.ActiveView;
            IGraphicsContainer graphicsContainer = map as IGraphicsContainer;
            graphicsContainer.UpdateElement((IElement)lineElement);
            activeView.Refresh();
        }

        public static void RestorePolylineElementColor(ILineElement lineElement, AxMapControl mapControl)
        {
            ISimpleLineSymbol simpleLineSymbol = new SimpleLineSymbolClass();
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = 255;
            rgbColor.Green = 0;
            rgbColor.Blue = 0;
            simpleLineSymbol.Color = rgbColor;
            simpleLineSymbol.Width = 1;
            simpleLineSymbol.Style = esriSimpleLineStyle.esriSLSSolid;
            lineElement.Symbol = simpleLineSymbol;
            IMap map = mapControl.Map;
            IActiveView activeView = mapControl.ActiveView;
            IGraphicsContainer graphicsContainer = map as IGraphicsContainer;
            graphicsContainer.UpdateElement((IElement)lineElement);
            activeView.Refresh();
        }

        public static void EraseElement(IElement element, AxMapControl mapControl)
        {
            IMap map = mapControl.Map;
            IActiveView activeView = mapControl.ActiveView;
            IGraphicsContainer graphicsContainer = map as IGraphicsContainer;
            graphicsContainer.DeleteElement(element);
            activeView.Refresh();
        }

        public static void ErasePolylineElement(ILineElement targetLineElement,  AxMapControl mapControl)
        {
            IMap map = mapControl.Map;
            IActiveView activeView = mapControl.ActiveView;
            IGraphicsContainer graphicsContainer = map as IGraphicsContainer;
            graphicsContainer.DeleteElement((IElement)targetLineElement);
            activeView.Refresh();
        }

        public static void ErasePolygonElement(IPolygonElement polygonElement, AxMapControl mapControl)
        {
            IMap map = mapControl.Map;
            IActiveView activeView = mapControl.ActiveView;
            IGraphicsContainer graphicsContainer = map as IGraphicsContainer;
            graphicsContainer.DeleteElement((IElement)polygonElement);
            activeView.Refresh();
        }

        public static List<Point> getPointListFromILineElement(ILineElement lineElement)
        {
            IPolyline polyline = (lineElement as IElement).Geometry as IPolyline;
            IPointCollection pointCollection = polyline as IPointCollection;
            List<Point> pointList = new List<Point>();
            for (int i = 0; i < pointCollection.PointCount; i++)
            {
                IPoint point = pointCollection.get_Point(i);
                Point pt = new Point();
                pt.x = point.X;
                pt.y = point.Y;
                pointList.Add(pt);
            }
            return pointList;
        }

        public static ILineElement getILineElementFromPointList(List<Point> pointList)
        {
            ILineElement lineElement = new LineElementClass();
            IElement element = lineElement as IElement;
            IPolyline polyline = new PolylineClass();
            IPointCollection pointCollection = polyline as IPointCollection;
            foreach (Point point in pointList)
            {
                IPoint pt = new PointClass();
                pt.X = point.x;
                pt.Y = point.y;
                pointCollection.AddPoint(pt);
            }
            element.Geometry = polyline;
            return lineElement;
        }

        public static IPolygonElement getIPolygonElementFromPointList(List<Point> pointList)
        {
            IPolygonElement polygonElement = new PolygonElementClass();
            IElement element = polygonElement as IElement;
            IPolygon polygon = new PolygonClass();
            Ring ring = polygon as Ring;
            foreach (Point point in pointList)
            {
                IPoint pt = new PointClass();
                pt.X = point.x;
                pt.Y = point.y;
                ring.AddPoint(pt);
            }
            element.Geometry = polygon;
            return polygonElement;
        }

        public static List<Point> getPointListFromIPolygonElement(IPolygonElement polygonElement)
        {
            List<Point> pointList = new List<Point>();
            IElement element = polygonElement as IElement;
            IPolygon polygon = element.Geometry as IPolygon;
            Ring ring = polygon as Ring;
            for (int i = 0; i < ring.PointCount; i++)
            {
                IPoint pt = ring.get_Point(i);
                Point point = new Point();
                point.x = pt.X;
                point.y = pt.Y;
                pointList.Add(point);
            }
            return pointList;
        }

        public static void drawPoint(IGeometry geom, AxMapControl mapControl)
        {
            IMarkerElement PolygonElement = new MarkerElementClass();
            IElement pElement = PolygonElement as IElement;
            pElement.Geometry = geom;
            IMap pMap = mapControl.Map;
            IGraphicsContainer pGraphicsContainer;
            IActiveView pActiveView = mapControl.ActiveView;
            pGraphicsContainer = pMap as IGraphicsContainer;
            pGraphicsContainer.AddElement((IElement)PolygonElement, 0);
            pActiveView.Refresh();
        }

        public static void HighlightFeature(ILayer targetLayer, string whereClause, AxMapControl mapControl)
        {
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = whereClause;
            IFeatureLayer featureLayer = targetLayer as IFeatureLayer;
            IFeatureSelection featureSelection = featureLayer as IFeatureSelection;
            featureSelection.SelectFeatures(filter, esriSelectionResultEnum.esriSelectionResultNew, false);
            mapControl.Refresh();
        }

        public static double GetAngle(IPolyline pPolyline)
        {
            //IPolycurve pPolycurve;  
            ILine pTangentLine = new ESRI.ArcGIS.Geometry.Line();
            pPolyline.QueryTangent(esriSegmentExtension.esriNoExtension, 0.5, true, pPolyline.Length, pTangentLine);
            Double radian = pTangentLine.Angle;
            //Double angle = radian * 180 / Math.PI;  
            //// 如果要设置正角度执行以下方法  
            //while (angle < 0)  
            //{  
            //    angle = angle + 360;  
            //}  
            //// 返回角度  
            //return angle;  

            // 返回弧度  
            return radian;
        }

        public static IPoint GetPointFrom2Points(IPoint point1, IPoint point2, int distance)
        {
            /*
                该函数用于从两点间求出矩形另一边的点. 
             *  p1 --------- p2
             *  |            |-distance
             *  所求点
            */

            double x1 = point1.X;
            double y1 = point1.Y;
            double x2 = point2.X;
            double y2 = point2.Y;

            double a = (x2 - x1) / (y1 - y2);
            double b = (x1 - x2) / (y1 - y2) * x1 + y1;

            double a2 = a * a + 1;
            double b2 = 2 * a * b - 2 * x1 - 2 * a * y1;
            double c = x1 * x1 + b * b - 2 * b * y1 + y1 * y1 - (distance * distance);

            double resultX1 = (-1 * b2 + Math.Sqrt(b2 * b2 - 4 * a2 * c)) / (2 * a2);
            double resultY1 = a * resultX1 + b;

            double resultX2 = (-1 * b2 - Math.Sqrt(b2 * b2 - 4 * a2 * c)) / (2 * a2);
            double resultY2 = a * resultX2 + b;

            IPoint resultPt;
            if (distance > 0)
            {
                if (resultY1 > y1)
                {
                    resultPt = new PointClass();
                    resultPt.PutCoords(resultX2, resultY2);
                }
                else
                {
                    resultPt = new PointClass();
                    resultPt.PutCoords(resultX1, resultY1);
                }
            }
            else
            {
                if (resultY1 < y1)
                {
                    resultPt = new PointClass();
                    resultPt.PutCoords(resultX2, resultY2);
                }
                else
                {
                    resultPt = new PointClass();
                    resultPt.PutCoords(resultX1, resultY1);
                }
            }

            return resultPt;
        }

        public static string getValueFromFeatureClass(IFeatureClass feaCls, int fid, string fieldName)
        {
            IFeatureLayer feaLy = new FeatureLayerClass();
            feaLy.FeatureClass = feaCls;
            IWorkspaceEdit wEdit = (feaLy.FeatureClass as IDataset).Workspace as IWorkspaceEdit;
            wEdit.StartEditing(true);
            wEdit.StartEditOperation();

            ITable pTable = (ITable)feaLy;
            int fieldNumber = pTable.FindField(fieldName);
            IRow pRow = pTable.GetRow(fid);
            string type = pRow.get_Value(fieldNumber).ToString();

            wEdit.StopEditOperation();
            wEdit.StopEditing(true);

            return type;
        }

        public static IRgbColor getColor(int r, int g, int b)
        {
            IRgbColor rgbColor = new RgbColorClass();
            rgbColor.Red = r;
            rgbColor.Green = g;
            rgbColor.Blue = b;
            return rgbColor;
        }

        public static List<string> GetValueListFromFeatureClass(IFeatureClass featureClass, string fieldName)
        {
            List<string> valueList = new List<string>();

            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit wEdit = (featureLayer.FeatureClass as IDataset).Workspace as IWorkspaceEdit;
            wEdit.StartEditing(true);
            wEdit.StartEditOperation();

            ITable pTable = (ITable)featureLayer;
            int fieldNumber = pTable.FindField(fieldName);
            if (fieldNumber < 0)
                return null;
            for (int i = 0;  i < pTable.RowCount(null); i++)
            {
                valueList.Add(pTable.GetRow(i).get_Value(fieldNumber).ToString());
            }

            wEdit.StopEditOperation();
            wEdit.StopEditing(true);

            return valueList;
        }

        public static Boolean addFeatureLayerField(IFeatureClass fs, string name, esriFieldType type, int fieldLength)
        {
            IFeatureLayer fl = new FeatureLayer();
            fl.FeatureClass = fs;
            try
            {
                ITable pTable = (ITable)fl;

                if (pTable.FindField(name) > 0)  //如果已经存在该字段, 直接返回. 
                {
                    return true;
                }

                IFields pFields = fl.FeatureClass.Fields;
                IFieldEdit pFieldEdit = new FieldClass();
                if (name.Length > 5)
                    pFieldEdit.Name_2 = name.Substring(0, 5);
                else
                    pFieldEdit.Name_2 = name;

                pFieldEdit.Type_2 = type;
                pFieldEdit.Editable_2 = true;
                pFieldEdit.AliasName_2 = name;
                pFieldEdit.Length_2 = fieldLength;
                pTable.AddField(pFieldEdit);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public static void setValueToFeatureClass(IFeatureClass feaCls, int fid, string key, string value)
        {
            IFeatureLayer feaLy = new FeatureLayerClass();
            feaLy.FeatureClass = feaCls;
            IWorkspaceEdit wEdit = (feaLy.FeatureClass as IDataset).Workspace as IWorkspaceEdit;
            wEdit.StartEditing(true);
            wEdit.StartEditOperation();

            ITable pTable = (ITable)feaLy;
            IRow pRow = pTable.GetRow(fid);
            pRow.set_Value(pTable.FindField(key), value);
            pRow.Store();

            wEdit.StopEditOperation();
            wEdit.StopEditing(true);
        }
        
        public static IGroupLayer getGroupLayerFromName(string name, AxMapControl mapControl)
        {
            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                if (mapControl.get_Layer(i).Name == name)
                {
                    IGroupLayer layer = mapControl.get_Layer(i) as IGroupLayer;
                    return layer;
                }
            }
            return null;
        }

        public static IFeatureClass getFeatureClass(AxMapControl mapControl, string layerName)
        {
            ILayer layer = GisTool.getLayerByName(layerName, mapControl);
            IFeatureLayer featureLayer = layer as IFeatureLayer;
            if (featureLayer == null)
            {
                return null;
            }
            return featureLayer.FeatureClass;
        }

        public static IFeatureClass getFeatureClass(string dir, string shp)
        {
            IWorkspaceFactory wsf = new ShapefileWorkspaceFactoryClass();
            IWorkspace ws = wsf.OpenFromFile(dir, 0);
            IFeatureWorkspace fw = ws as IFeatureWorkspace;
            try
            {
                return fw.OpenFeatureClass(shp);
            }
            catch (Exception e)
            {
                return null;
            }
        }

        public static void AddGeometryListToShpFile(List<IGeometry> geometryList, string folderPath, string fileName)
        {
            IFeatureClass featureClass = GisTool.getFeatureClass(folderPath, fileName);
            foreach (IGeometry geometry in geometryList)
            {
                GisTool.AddGeometryToFeatureClass(geometry, featureClass);
            }
        }

        public static void AddGeometryToFeatureClass(IGeometry geometry, IFeatureClass featureClass)
        {
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            IFeature feature = featureClass.CreateFeature();
            feature.Shape = geometry;
            feature.Store();

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        public static void AddFeaturesToFeatureClass(List<Feature> featureList, IFeatureClass featureClass, string fieldName)
        {
            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            foreach (Feature feature in featureList)
            {
                IFeature fea = featureClass.CreateFeature();
                fea.Shape = feature.relativeFeature.Shape;
                fea.Store();
                ITable pTable = (ITable)featureLayer;
                IRow pRow = pTable.GetRow(featureClass.FeatureCount(null) - 1);
                pRow.set_Value(pTable.FindField(fieldName), feature.score.ToString());
                pRow.Store();
            }

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        public static void AddHouseToFeatureClass(List<IGeometry> drawnHouseList, IFeatureClass featureClass)
        {
            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            foreach (IGeometry drawnHouse in drawnHouseList)
            {
                IFeature fea = featureClass.CreateFeature();
                fea.Shape = drawnHouse;
                fea.Store();
            }

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        public static void DeleteShapeFile(string path)
        {
            IWorkspaceFactory workspaceFactory = new ShapefileWorkspaceFactoryClass();
            IFeatureWorkspace featureWorkspace = workspaceFactory.OpenFromFile(System.IO.Path.GetDirectoryName(path), 0) as IFeatureWorkspace;//pathName为路径名
            IFeatureClass featureClass = featureWorkspace.OpenFeatureClass(System.IO.Path.GetFileName(path));//fileName为文件名(不包含路径)
            IDataset dataSet = featureClass as IDataset;
            dataSet.Delete();
        }

        public static void CreateShapefile(string strShapeFolder, string strShapeName, ISpatialReference spatialRef, string geometryType = "polygon")
        {
            //如果该文件已经存在, 删除该文件名开头的所有不同后缀的文件.
            if (File.Exists(System.IO.Path.Combine(strShapeFolder, strShapeName)))
            {
                FileHelper.DeleteSameNameFiles(strShapeFolder, strShapeName);
            }
            //打开工作空间  
            const string strShapeFieldName = "shape";
            IWorkspaceFactory pWSF = new ShapefileWorkspaceFactoryClass();
            IFeatureWorkspace pWS = (IFeatureWorkspace)pWSF.OpenFromFile(strShapeFolder, 0);

            //设置字段集  
            IFields pFields = new FieldsClass();
            IFieldsEdit pFieldsEdit = (IFieldsEdit)pFields;

            //设置字段  
            IField pField = new FieldClass();
            IFieldEdit pFieldEdit = (IFieldEdit)pField;

            //创建类型为几何类型的字段  
            pFieldEdit.Name_2 = strShapeFieldName;
            pFieldEdit.Type_2 = esriFieldType.esriFieldTypeGeometry;

            //为esriFieldTypeGeometry类型的字段创建几何定义，包括类型和空间参照   
            IGeometryDef pGeoDef = new GeometryDefClass();     //The geometry definition for the field if IsGeometry is TRUE.  
            IGeometryDefEdit pGeoDefEdit = (IGeometryDefEdit)pGeoDef;
            if (geometryType == "polygon")
            {
                pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolygon;
            }
            else if (geometryType == "polyline")
            {
                pGeoDefEdit.GeometryType_2 = esriGeometryType.esriGeometryPolyline;
            }
            pGeoDefEdit.SpatialReference_2 = spatialRef;

            pFieldEdit.GeometryDef_2 = pGeoDef;
            pFieldsEdit.AddField(pField);

            //创建shapefile  
            pWS.CreateFeatureClass(strShapeName, pFields, null, null, esriFeatureType.esriFTSimple, strShapeFieldName, "");
        }

        public static void HideAllLayerInMap(AxMapControl mapControl)
        {
            if (mapControl == null)
            {
                return;
            }

            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                ILayer layer = mapControl.get_Layer(i);
                layer.Visible = false;   
            }

            mapControl.ActiveView.Refresh();
        }

        public static void ShowAllLayerInMap(AxMapControl mapControl)
        {
            if (mapControl == null)
            {
                return;
            }

            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                mapControl.get_Layer(i).Visible = true;
            }

            mapControl.ActiveView.Refresh();
        }

        public static string GetShpNameByLayerIndex(AxMapControl mapControl, int index)
        {
            List<string> shpNameList = new List<string>();
            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                ICompositeLayer cLayer = mapControl.get_Layer(i) as ICompositeLayer;
                if (cLayer == null)
                {
                    ILayer layer = mapControl.get_Layer(i);
                    IFeatureLayer fLayer = layer as IFeatureLayer;
                    if (fLayer == null)
                    {
                        shpNameList.Add("");
                        continue;
                    }
                    IFeatureClass featureClass = fLayer.FeatureClass;
                    if (featureClass == null)
                    {
                        shpNameList.Add("");
                        continue;
                    }
                    string fileName = featureClass.AliasName;
                    shpNameList.Add(fileName);
                }
                else
                {
                    for (int j = 0; j < cLayer.Count; j++)
                    {
                        ILayer layer = cLayer.get_Layer(j);
                        IFeatureLayer fLayer = layer as IFeatureLayer;
                        if (fLayer == null)
                            continue;
                        string fileName = fLayer.FeatureClass.AliasName;
                        shpNameList.Add(fileName);
                    }
                }
            }
            return shpNameList[index];
        }

        public static string GetShpNameByMapLayerName(AxMapControl mapControl, string mapLayerName)
        {
            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                ICompositeLayer cLayer = mapControl.get_Layer(i) as ICompositeLayer;
                if (cLayer == null)
                {
                    ILayer layer = mapControl.get_Layer(i);
                    IFeatureLayer fLayer = layer as IFeatureLayer;
                    if (fLayer == null)
                    {
                        continue;
                    }
                    IFeatureClass featureClass = fLayer.FeatureClass;
                    if (featureClass == null)
                    {
                        continue;
                    }
                    string fileName = featureClass.AliasName;
                    if (layer.Name == mapLayerName)
                        return fileName;
                }
                else
                {
                    for (int j = 0; j < cLayer.Count; j++)
                    {
                        ILayer layer = cLayer.get_Layer(j);
                        IFeatureLayer fLayer = layer as IFeatureLayer;
                        if (fLayer == null)
                            continue;
                        string fileName = fLayer.FeatureClass.AliasName;
                        if (layer.Name == mapLayerName)
                            return fileName;
                    }
                }
            }
            return "";
        }

        public static int getLayerIndexByName(string name, AxMapControl mapControl)
        {
            if (mapControl == null)
            {
                return -1;
            }

            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                ILayer layer = mapControl.get_Layer(i);
                ICompositeLayer compositeLayer = layer as ICompositeLayer;
                if (compositeLayer == null)
                {
                    //说明不是一个组合图层, 直接获取图层名.
                    if (layer.Name == name)
                    {
                        return i;
                    }
                }
                else
                {
                    for (int j = 0; j < compositeLayer.Count; j++)
                    {
                        ILayer ly = compositeLayer.get_Layer(j);
                        if (ly.Name == name)
                        {
                            return i;
                        }
                    }
                }
            }
            return -1;
        }

        public static ILayer getLayerByName(string name, AxMapControl mapControl)
        {
            if (mapControl == null)
            {
                return null;
            }

            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                ILayer layer = mapControl.get_Layer(i);
                ICompositeLayer compositeLayer = layer as ICompositeLayer;
                if (compositeLayer == null)
                {
                    //说明不是一个组合图层, 直接获取图层名.
                    if (layer.Name == name)
                    {
                        return layer;
                    }
                }
                else
                {
                    for (int j = 0; j < compositeLayer.Count; j++)
                    {
                        ILayer ly = compositeLayer.get_Layer(j);
                        if (ly.Name == name)
                        {
                            return compositeLayer as ILayer;
                        }
                    }
                }
            }
            return null;
        }

        public static void ExpandToMapView(IFeature feature, AxMapControl mapControl)
        {
            //移动地图视角.
            IEnvelope extent = feature.Shape.Envelope;
            extent.Expand(1, 1, true);
            mapControl.Extent = extent;
            mapControl.ActiveView.Refresh();
        }

        public static void ExpandToMapView(string layerName, AxMapControl mapControl)
        {
            ILayer layer = GisTool.getLayerByName(layerName, mapControl);
            IEnvelope extent = layer.AreaOfInterest;
            extent.Expand(1, 1, true);
            mapControl.Extent = extent;
            mapControl.ActiveView.Refresh();
        }

        public static bool isRasterLayer(ILayer layer)
        {
            IRasterLayer rLayer = layer as IRasterLayer;
            return rLayer != null;
        }

        public static List<IRasterLayer> GetRasterLayer(AxMapControl mapControl)
        {
            List<IRasterLayer> rasterLayerList = new List<IRasterLayer>();
            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                ILayer layer = mapControl.get_Layer(i);
                ICompositeLayer cLayer = layer as ICompositeLayer;
                if (cLayer == null)
                {
                    IRasterLayer rLayer = layer as IRasterLayer;
                    if (rLayer != null)
                    {
                        rasterLayerList.Add(rLayer);
                    }
                }
                else
                {
                    for (int j = 0; j < cLayer.Count; j++)
                    {
                        IRasterLayer rLayer = cLayer.get_Layer(j) as IRasterLayer;
                        if (rLayer == null)
                        {
                            rasterLayerList.Add(rLayer);
                        }
                    }
                }
            }
            return rasterLayerList;
        }



        public static List<IPolygon> MakePolygonListFromPolylineList(List<IPolyline> polylineList)
        {
            List<IPolygon> polygonList = new List<IPolygon>();
            return polygonList;
        }

        /*
        private static bool IsFeatureLayerExists(IFeatureLayer fLayer, string folder)
        {
            IFeatureClass featureClass = fLayer.FeatureClass;
            if (featureClass == null)
            {
                return false;
            }

            string fileName = featureClass.AliasName;
            if (!File.Exists(folder + "//" + fileName + ".shp"))
            {
                return false;
            }

            return true;
        }

        private static bool IsRasterLayerExists(IRasterLayer rLayer)
        {
            string filePath = rLayer.FilePath;
            if (!Directory.Exists(filePath))
            {
                return false;
            }

            return true;
        }

        public static bool CheckLayerIntegrity(ILayer layer, string folder)
        {
            IFeatureLayer fLayer = layer as IFeatureLayer;
            if (fLayer == null)
            {
                IRasterLayer rLayer = layer as IRasterLayer;
                if (rLayer == null || !IsRasterLayerExists(rLayer))
                {
                    return false;
                }
            }
            else
            {
                if (!IsFeatureLayerExists(fLayer, folder))
                {
                    return false;
                }
            }

            return true;
        }

        public static bool CheckMapIntegrity(string folder, AxMapControl mapControl)
        {
            for (int i = 0; i < mapControl.LayerCount; i++)
            {
                ICompositeLayer cLayer = mapControl.get_Layer(i) as ICompositeLayer;
                if (cLayer == null)
                {
                    ILayer layer = mapControl.get_Layer(i);
                    if (!CheckLayerIntegrity(layer, folder))
                    {
                        return false;
                    }
                }
                else
                {
                    for (int j = 0; j < cLayer.Count; j++)
                    {
                        ILayer layer = cLayer.get_Layer(j);
                        if (!CheckLayerIntegrity(layer, folder))
                        {
                            return false;
                        }
                    }
                }
            } 

            return true;
        }
         */

        public static IFeature GetBaseFeature(AxMapControl mapControl, int baseMapIndex)
        {
            ILayer baseLayer = GisTool.getLayerByName(ProjectWindowHelper.BASE_LAYER_NAME, mapControl);
            IFeatureLayer fBaseLayer = baseLayer as IFeatureLayer;
            if (fBaseLayer == null)
                return null;
            IFeatureClass fBaseFeatureClass = fBaseLayer.FeatureClass;
            return fBaseFeatureClass.GetFeature(baseMapIndex);
        }
    }
}
