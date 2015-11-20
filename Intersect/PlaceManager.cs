using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Display;
using System.IO;
using ESRI.ArcGIS.Carto;

namespace Intersect
{
    class PlaceManager
    {
        public List<IGeometry> drawnHouseList;
        public IPolyline innerRoadLine;
        private CommonHouse commonHouse;
        private List<_House> _houseList;
        private AxMapControl mapControl;
        private Area area;
        private double totalWeight;

        private class _House
        {
            public House house;
            public int count;

            public _House(House h, int c)
            {
                house = h;
                count = c;
            }
        }

        public PlaceManager(CommonHouse ch, List<House> hList, AxMapControl mc)
        {
            foreach (House house in hList)
            {
                totalWeight += house.weight;
            }

            commonHouse = ch;
            _houseList = new List<_House>();
            foreach (House house in hList)
            {
                _House _house = new _House(house, (int)(commonHouse.minNumber * house.weight / totalWeight));
                _houseList.Add(_house);
            }
            //由小到大排序.
            _houseList.Sort(delegate(_House _house1, _House _house2)
            {
                return Comparer<double>.Default.Compare(_house1.house.width * _house1.house.unit,
                    _house2.house.width * _house2.house.unit);
            });
            mapControl = mc;

        }

        public bool place()
        {
            double roadHeight = commonHouse.height + commonHouse.frontGap + commonHouse.backGap;
            IGeometry upperHouseArea;
            IGeometry upperRoadHouseArea;
            IGeometry lowerHouseArea;
            IGeometry lowerRoadHouseArea;

            //先沿着插入和切割线排房子.
            ITopologicalOperator tpOp = area.splitLine as ITopologicalOperator;
            tpOp.Simplify();
            IPointCollection pts = tpOp.Boundary as IPointCollection;
            IPoint startPt = pts.get_Point(0);
            IPoint endPt = pts.get_Point(1);
            IPoint tempStartPt = pts.get_Point(0);
            IPoint tempEndPt = pts.get_Point(1);
            //先计算两点间距离.
            double distanceBetweenPoints = Math.Pow((startPt.X - endPt.X) * (startPt.X - endPt.X) + (startPt.Y - endPt.Y) * (startPt.Y - endPt.Y), 0.5);
            double cosA = Math.Abs(startPt.X - endPt.X) / distanceBetweenPoints;
            IPolyline line = new PolylineClass();
            IPointCollection ptCol = line as IPointCollection;
            ptCol.AddPoint(startPt);
            ptCol.AddPoint(endPt);
            tpOp = area.areaGeom as ITopologicalOperator;
            IGeometry polygon1;
            IGeometry polygon2;
            tpOp.Simplify();
            tpOp.Cut(line, out polygon1, out polygon2);
            IRgbColor color1 = new RgbColorClass();
            color1.Red = 255;
            color1.Green = 255;
            color1.Blue = 0;
            IRgbColor color2 = new RgbColorClass();
            color2.Red = 255;
            color2.Green = 0;
            color2.Blue = 255;
            //GisTool.drawPolygon(polygon1, mapControl, color1);
            //GisTool.drawPolygon(polygon2, mapControl, color2);
            if (startPt.X < endPt.X)
            {
                //从左至右的线, polygon1在上方.
                tempStartPt.Y += roadHeight / cosA;
                tempEndPt.Y += roadHeight / cosA;
                IPolyline tempLine = new PolylineClass();
                ptCol = tempLine as IPointCollection;
                ptCol.AddPoint(tempStartPt);
                ptCol.AddPoint(tempEndPt);
                tpOp = polygon1 as ITopologicalOperator;
                tpOp.Simplify();
                tpOp.Cut(tempLine, out upperHouseArea, out upperRoadHouseArea);
                tempStartPt.Y -= roadHeight / cosA * 2;
                tempEndPt.Y -= roadHeight / cosA * 2;
                tempLine = new PolylineClass();
                ptCol = tempLine as IPointCollection;
                ptCol.AddPoint(tempStartPt);
                ptCol.AddPoint(tempEndPt);
                tpOp = polygon2 as ITopologicalOperator;
                tpOp.Simplify();
                tpOp.Cut(tempLine, out lowerRoadHouseArea, out lowerHouseArea);
            }
            else
            {
                //从右至左的线, polygon1在下方.
                tempStartPt.Y += roadHeight / cosA;
                tempEndPt.Y += roadHeight / cosA;
                IPolyline tempLine = new PolylineClass();
                ptCol = tempLine as IPointCollection;
                ptCol.AddPoint(tempStartPt);
                ptCol.AddPoint(tempEndPt);
                tpOp = polygon2 as ITopologicalOperator;
                tpOp.Simplify();
                tpOp.Cut(tempLine, out upperHouseArea, out upperRoadHouseArea);
                tempStartPt.Y -= roadHeight / cosA * 2;
                tempEndPt.Y -= roadHeight / cosA * 2;
                tempLine = new PolylineClass();
                ptCol = tempLine as IPointCollection;
                ptCol.AddPoint(tempStartPt);
                ptCol.AddPoint(tempEndPt);
                tpOp = polygon1 as ITopologicalOperator;
                tpOp.Simplify();
                tpOp.Cut(tempLine, out lowerRoadHouseArea, out lowerHouseArea);
            }
            //把upperHouseArea, lowerHouseArea细分成条状.
            List<IGeometry> areaList = new List<IGeometry>();
            areaList = StripArea(upperHouseArea, roadHeight, true);
            List<IGeometry> tempList = StripArea(lowerHouseArea, roadHeight, false);
            for (int i = 0; i < tempList.Count; i++)
            {
                areaList.Add(tempList[i]);
            }
            List<stripedRow> stripedRowList = new List<stripedRow>();
            for (int i = 0; i < areaList.Count; i++)
            {
                stripedRowList.Add(new stripedRow(areaList[i], roadHeight));
            }
            for (int i = 0; i < stripedRowList.Count; i++)
            {
                stripedRowList[i].formatRow();
            }
            double maxHouseNumber = caculateMaxHouseNumber(stripedRowList);
            if (maxHouseNumber < commonHouse.minNumber)
            {
                Tool.M("排放失败，放不下。");
                //return false;
            }

            //往stripedrow里填坑.
            drawnHouseList = new List<IGeometry>();
            for (int i = 0; i < stripedRowList.Count; i++)
            {
                PlaceHouse(stripedRowList[i], drawnHouseList);
            }
            foreach (_House _house in _houseList)
            {
                if (_house.count > 0)
                {
                    Tool.M("排放失败，放不下。");
                    //return false;
                }
            }

            return true;
            innerRoadLine = new PolylineClass();
            tpOp = innerRoadLine as ITopologicalOperator;
            for (int i = 0; i < stripedRowList.Count; i++)
            {
                IGeometry tempGeom = stripedRowList[i].stripedrow;
                ITopologicalOperator tempTopo = tempGeom as ITopologicalOperator;
                innerRoadLine = tpOp.Union(tempTopo.Boundary) as IPolyline;
                tpOp = innerRoadLine as ITopologicalOperator;
            }
            tpOp.Simplify();
            //沿路填坑.

            //PlaceHouseByRoad(new roadStripedRow(upperRoadHouseArea, roadHeight), drawnHouseList);
            //PlaceHouseByRoad(new roadStripedRow(lowerRoadHouseArea, roadHeight), drawnHouseList);

            return true;
        }

        private double caculateMaxHouseNumber(List<stripedRow> stripedRowList)
        {
            double maxNumber = 0;
            foreach (stripedRow row in stripedRowList)
            {
                maxNumber += caculateMaxHouseNumberInRow(row.rowWidth);
            }
            return maxNumber;
        }

        private double caculateMaxHouseNumberInRow(double rowWidth)
        {
            List<double> houseWidthList = new List<double>();
            foreach (_House _house in _houseList)
            {
                houseWidthList.Add(_house.house.width * _house.house.unit);
            }
            double horizontalGap = commonHouse.horizontalGap;

            List<double> resultList = new List<double>();
            for (int i = 0; i < houseWidthList.Count; i++)
            {
                resultList.Add(0);
            }
            for (int i = 0; i < houseWidthList.Count; i++)
            {
                double houseWidth = houseWidthList[i];
                double result = 0;
                if (houseWidth < rowWidth)
                {
                    //动态规划
                    result++;
                    result += caculateMaxHouseNumberInRow(rowWidth - houseWidth - horizontalGap);
                    resultList[i] = result;
                }
                else
                {
                    resultList[i] = result;
                }
            }

            return Tool.GetMax(resultList);
        }

        private IPolygon PreProcessArea(IGeometry geom)
        {
            /*
             *  预处理地区, 从地区中心挖出一块土地.
             *  返回被挖出的地块. 
             */
            object missing = Type.Missing;
            IPoint envelopLeftTopPt = geom.Envelope.UpperLeft;
            IPoint envelopRightTopPt = geom.Envelope.UpperRight;
            IPoint envelopLeftBottomPt = geom.Envelope.LowerLeft;
            IPoint envelopRightBottomPt = geom.Envelope.LowerRight;
            double initialDist = 5; //第一次各边向内移动5M;
            double dist = 1; //之后每次尝试向内移动1M;

            envelopLeftTopPt.X += initialDist;
            envelopLeftTopPt.Y -= initialDist;
            envelopRightTopPt.X -= initialDist;
            envelopRightTopPt.Y -= initialDist;
            envelopLeftBottomPt.X += initialDist;
            envelopLeftBottomPt.Y += initialDist;
            envelopRightBottomPt.X -= initialDist;
            envelopRightBottomPt.Y += initialDist;

            Ring ring = new RingClass();
            ring.AddPoint(envelopLeftTopPt);
            ring.AddPoint(envelopRightTopPt);
            ring.AddPoint(envelopRightBottomPt);
            ring.AddPoint(envelopLeftBottomPt);

            IPolygon areaPolygon = GisTool.MakePolygonFromRing(ring);
            IRelationalOperator reOp = geom as IRelationalOperator;

            while (!reOp.Contains(areaPolygon))
            {
                envelopLeftTopPt.X += dist;
                envelopLeftTopPt.Y -= dist;
                envelopRightTopPt.X -= dist;
                envelopRightTopPt.Y -= dist;
                envelopLeftBottomPt.X += dist;
                envelopLeftBottomPt.Y += dist;
                envelopRightBottomPt.X -= dist;
                envelopRightBottomPt.Y += dist;

                ring = new RingClass();
                ring.AddPoint(envelopLeftTopPt);
                ring.AddPoint(envelopRightTopPt);
                ring.AddPoint(envelopRightBottomPt);
                ring.AddPoint(envelopLeftBottomPt);

                areaPolygon = GisTool.MakePolygonFromRing(ring);
            }

            envelopLeftTopPt.X += initialDist;
            envelopLeftTopPt.Y -= initialDist;
            envelopRightTopPt.X -= initialDist;
            envelopRightTopPt.Y -= initialDist;
            envelopLeftBottomPt.X += initialDist;
            envelopLeftBottomPt.Y += initialDist;
            envelopRightBottomPt.X -= initialDist;
            envelopRightBottomPt.Y += initialDist;

            ring = new RingClass();
            ring.AddPoint(envelopLeftTopPt);
            ring.AddPoint(envelopRightTopPt);
            ring.AddPoint(envelopRightBottomPt);
            ring.AddPoint(envelopLeftBottomPt);

            areaPolygon = GisTool.MakePolygonFromRing(ring);

            return areaPolygon;
        }


        public bool makeArea(IGeometry geom, IGeometry splitLine)
        {
            IArea iarea = geom as IArea;
            IPolygon centerArea = PreProcessArea(geom); //中间用来规划的地块.
            ITopologicalOperator tpOp = centerArea as ITopologicalOperator;
            tpOp.Simplify();
            tpOp = geom as ITopologicalOperator;
            tpOp.Simplify();
            IPolygon aroundArea = tpOp.Difference(centerArea) as IPolygon; //周围那块地块.

            //将该图形添加到areaList中.
            area = new Area(centerArea, aroundArea, geom, splitLine as IPolyline);
            return true;
        }

        public string getTotalHouseInfo()
        {
            int totalHouseHold = 0;
            double totalHouseArea = 0;
            foreach (HouseManager houseManager in drawnHouseList)
            {
                //totalHouseHold += houseManager.house.houseHold;
                totalHouseArea += houseManager.house.width * houseManager.commonHouse.height;
            }
            string totalHouseHoldInfo = String.Format("总户数: {0}(户)", totalHouseHold);
            string totalHouseAreaInfo = String.Format("总建筑面积: {0}(平方米)", totalHouseArea);
            string totalHouseInfo = String.Format("{0}\n{1}", totalHouseHoldInfo, totalHouseAreaInfo);
            return totalHouseInfo;
        }

        public void save(string folder, string shpName)
        {
            GisTool.CreateShapefile(folder, shpName, mapControl.SpatialReference);
            IFeatureClass houseFeatureClass = GisTool.getFeatureClass(folder, shpName);
            GisTool.AddHouseToFeatureClass(drawnHouseList, houseFeatureClass);
        }

        public void deleteShapeFile(string folder, string layerName)
        {
            int index = GisTool.getLayerIndexByName(layerName, mapControl);
            if (index == -1)
            {
                return;
            }

            mapControl.DeleteLayer(index);
            GisTool.DeleteShapeFile(System.IO.Path.Combine(folder, layerName + ".shp"));
        }

        public bool addShapeFile(string folder, string shpName, string layerName)
        {
            if (File.Exists(System.IO.Path.Combine(folder, shpName)))
            {
                IFeatureClass houseFeatureClass = GisTool.getFeatureClass(folder, shpName);
                IFeatureLayer houseFeatureLayer = new FeatureLayerClass();
                houseFeatureLayer.FeatureClass = houseFeatureClass;
                houseFeatureLayer.Name = layerName;
                ILayerEffects layerEffects = houseFeatureLayer as ILayerEffects;
                layerEffects.Transparency = 60;
                mapControl.AddLayer(houseFeatureLayer);
                mapControl.ActiveView.Refresh();

                return true;
            }

            return false;
        }

        private void saveShp(string folder, string name, IGeometry geom, string geometryType = "polygon")
        {
            GisTool.CreateShapefile(folder, name, mapControl.SpatialReference, geometryType);
            IFeatureClass feaCls = GisTool.getFeatureClass(folder, name);
            IFeature fea = feaCls.CreateFeature();

            IWorkspaceEdit wEdit = (feaCls as IDataset).Workspace as IWorkspaceEdit;
            wEdit.StartEditing(true);
            wEdit.StartEditOperation();

            IFeature chosenAreaFea = feaCls.CreateFeature();
            fea.Shape = geom;
            fea.Store();

            wEdit.StopEditOperation();
            wEdit.StopEditing(true);
        }

        public void saveOuterGround(string path)
        {
            string folder = System.IO.Path.GetDirectoryName(path);
            string name = System.IO.Path.GetFileName(path);
            saveShp(folder, name, area.aroundGeom);
        }

        public void saveCenterGround(string path)
        {
            string folder = System.IO.Path.GetDirectoryName(path);
            string name = System.IO.Path.GetFileName(path);
            saveShp(folder, name, area.areaGeom);
        }

        public void saveHouse(string path)
        {
            //string folder = System.IO.Path.GetDirectoryName(path);
            //string name = System.IO.Path.GetFileName(path);
            //GisTool.CreateShapefile(folder, name, mapControl.SpatialReference);
            //IFeatureClass feaCls = GisTool.getFeatureClass(folder, name);
            //for (int i = 0; i < drawnHouseList.Count; i++)
            //{
            //    List<IGeometry> housePolygonList = drawnHouseList[i].makeHousePolygon()[1] as List<IGeometry>;
            //    foreach (IGeometry geom in housePolygonList)
            //    {
            //        IFeature fea = feaCls.CreateFeature();

            //        IWorkspaceEdit wEdit = (feaCls as IDataset).Workspace as IWorkspaceEdit;
            //        wEdit.StartEditing(true);
            //        wEdit.StartEditOperation();

            //        fea.Shape = geom;
            //        fea.Store();

            //        wEdit.StopEditOperation();
            //        wEdit.StopEditing(true);
            //    }
            //}
        }

        public void saveInnerRoad(string path)
        {
            string folder = System.IO.Path.GetDirectoryName(path);
            string name = System.IO.Path.GetFileName(path);
            saveShp(folder, name, innerRoadLine, "polyline");
        }

        public void saveRoad(string path)
        {
            string folder = System.IO.Path.GetDirectoryName(path);
            string name = System.IO.Path.GetFileName(path);
            saveShp(folder, name, area.splitLine, "polyline");
        }

        private List<IGeometry> StripArea(IGeometry geom, double height, bool direction)
        {
            //把一块图形划分为条状. direction为true时, 上部为规整的边, 否则下部为规整的边.
            List<IGeometry> stripedAreaList = new List<IGeometry>();
            IPolyline stripLine = new PolylineClass();
            IPointCollection ptCol = stripLine as IPointCollection;
            IPoint startPt, endPt; //startPt一定在endPt的左边.
            IGeometry cutedGeom = geom;
            ITopologicalOperator tpOp;
            IRelationalOperator reOp;
            if (direction)
            {
                startPt = geom.Envelope.UpperLeft;
                endPt = geom.Envelope.UpperRight;
            }
            else
            {
                startPt = geom.Envelope.LowerLeft;
                endPt = geom.Envelope.LowerRight;
                height = -1 * height;
            }
            startPt.Y -= height;
            endPt.Y -= height;
            ptCol.AddPoint(startPt);
            ptCol.AddPoint(endPt);
            tpOp = cutedGeom as ITopologicalOperator;
            reOp = cutedGeom as IRelationalOperator;
            while (reOp.Crosses(stripLine) || reOp.Contains(stripLine))
            {
                IGeometry tempGeom;
                if (direction)
                {
                    tpOp.Simplify();
                    tpOp.Cut(stripLine, out tempGeom, out cutedGeom);
                }
                else
                {
                    tpOp.Simplify();
                    tpOp.Cut(stripLine, out cutedGeom, out tempGeom);
                }
                stripedAreaList.Add(tempGeom);
                startPt.Y -= height;
                endPt.Y -= height;
                stripLine = new PolylineClass();
                ptCol = stripLine as IPointCollection;
                ptCol.AddPoint(startPt);
                ptCol.AddPoint(endPt);
                tpOp = cutedGeom as ITopologicalOperator;
                reOp = cutedGeom as IRelationalOperator;
            }

            return stripedAreaList;
        }

        private void PlaceHouse(stripedRow row, List<IGeometry> drawnHouseList)
        {
            //将房子放在格子中, 如果没有被chosenAreaFeaCls包含, 则不保留.
            double restRowWidth = row.rowWidth; //剩余的行宽.
            IPoint currentPoint = row.stripedrow.Envelope.UpperLeft; //开始的左上角.

            //由大到小往里放，放的下且有额度就往里放一个。
            for (int i = _houseList.Count - 1; i >= 0; i--)
            {
                while (restRowWidth > _houseList[i].house.width * _houseList[i].house.unit)
                {
                    if (_houseList[i].count == 0)
                    {
                        //额度用完了。给下一个房型排。
                        //break;
                    }

                    for (int j = 0; j < _houseList[i].house.unit; j++)
                    {
                        IPolygon housePolygon = GisTool.MakePolygon(currentPoint.X + _houseList[i].house.width * j,
                            currentPoint.Y, _houseList[i].house.width, commonHouse.height);
                        drawnHouseList.Add(housePolygon);
                    }

                    restRowWidth -= _houseList[i].house.width * _houseList[i].house.unit;
                    currentPoint.X += _houseList[i].house.width * _houseList[i].house.unit;
                    _houseList[i].count--;

                    if (restRowWidth >= commonHouse.horizontalGap)
                    {
                        restRowWidth -= commonHouse.horizontalGap;
                        currentPoint.X += commonHouse.horizontalGap;
                    }
                    else
                    { 
                        //放不下一个间距了，这个stripedrow排放结束。
                        return;
                    }
                }
            }
        }

        private void PlaceHouseByRoad(roadStripedRow row, List<HouseManager> drawnHouseList)
        {
            List<House> tempHouseList = new List<House>();
            foreach (_House _house in _houseList)
            {
                tempHouseList.Add(_house.house);
            }
            tempHouseList.Sort(delegate(House house1, House house2)
            {
                return Comparer<double>.Default.Compare(house1.width ,  house2.width);
            });
            row.formatRow();
            double restRowWidth = row.rowWidth; //剩余的行宽.
            IPoint currentPoint = row.lowerLeftPt; //开始的左下角.
            int round = 0;
            while (restRowWidth > commonHouse.horizontalGap + tempHouseList[tempHouseList.Count - 1].width)
            {
                while (true)
                {
                    if (commonHouse.horizontalGap + tempHouseList[0].width > restRowWidth)
                    {
                        tempHouseList.RemoveAt(0);
                        round = 0;
                        continue;
                    }
                    else
                    {
                        break;
                    }
                }
                HouseManager houseManager = new HouseManager(currentPoint, tempHouseList[round], commonHouse);
                round++;
                if (round == tempHouseList.Count)
                    round = 0;
                restRowWidth -= houseManager.totalHouseWidth;
                houseManager.rotate(houseManager.lowerLeftPt, row.rotateAngle);
                drawnHouseList.Add(houseManager);
                currentPoint = new PointClass();
                currentPoint.X = houseManager.lowerRightPt.X;
                currentPoint.Y = houseManager.lowerRightPt.Y;
            }
        }
    }
}
