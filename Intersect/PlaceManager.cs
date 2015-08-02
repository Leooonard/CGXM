using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Display;

namespace Intersect
{
    class PlaceManager
    {
        private int MAX_AREA = 40000;

        public List<HouseManager> drawnHouseList;
        public IPolyline innerRoadLine;
        private CommonHouse commonHouse;
        private List<House> houseList;
        private AxMapControl mapControl;
        private Area area;

        public PlaceManager(CommonHouse ch, List<House> hList, AxMapControl mc)
        {
            commonHouse = ch;
            houseList = hList;
            mapControl = mc;
        }

        public void place()
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
            tpOp.Cut(line, out polygon1, out polygon2);
            IRgbColor color1 = new RgbColorClass();
            color1.Red = 255;
            color1.Green = 255;
            color1.Blue = 0;
            IRgbColor color2 = new RgbColorClass();
            color2.Red = 255;
            color2.Green = 0;
            color2.Blue = 255;
            GisUtil.drawPolygon(polygon1, mapControl, color1);
            GisUtil.drawPolygon(polygon2, mapControl, color2);
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
                tpOp.Cut(tempLine, out upperHouseArea, out upperRoadHouseArea);
                tempStartPt.Y -= roadHeight / cosA * 2;
                tempEndPt.Y -= roadHeight / cosA * 2;
                tempLine = new PolylineClass();
                ptCol = tempLine as IPointCollection;
                ptCol.AddPoint(tempStartPt);
                ptCol.AddPoint(tempEndPt);
                tpOp = polygon2 as ITopologicalOperator;
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
                tpOp.Cut(tempLine, out upperHouseArea, out upperRoadHouseArea);
                tempStartPt.Y -= roadHeight / cosA * 2;
                tempEndPt.Y -= roadHeight / cosA * 2;
                tempLine = new PolylineClass();
                ptCol = tempLine as IPointCollection;
                ptCol.AddPoint(tempStartPt);
                ptCol.AddPoint(tempEndPt);
                tpOp = polygon1 as ITopologicalOperator;
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

            //往stripedrow里填坑.
            drawnHouseList = new List<HouseManager>();
            for (int i = 0; i < stripedRowList.Count; i++)
            {
                PlaceHouse(stripedRowList[i], drawnHouseList);
            }
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

            PlaceHouseByRoad(new roadStripedRow(upperRoadHouseArea, roadHeight), drawnHouseList);
            PlaceHouseByRoad(new roadStripedRow(lowerRoadHouseArea, roadHeight), drawnHouseList);
        }

        public bool makeArea(IGeometry geom)
        {
            IArea iarea = geom as IArea;
            if (Math.Abs(iarea.Area) > MAX_AREA)
            {
                Ut.M("所选择区域过大, 请重新选择");
                return false;
            }
            IPolygon centerArea = GisUtil.PreProcessArea(geom); //中间用来规划的地块.
            ITopologicalOperator tpOp = geom as ITopologicalOperator;
            tpOp.Simplify();
            tpOp = centerArea as ITopologicalOperator;
            tpOp.Simplify();
            tpOp = geom as ITopologicalOperator;
            IPolygon aroundArea = tpOp.Difference(centerArea) as IPolygon; //周围那块地块.

            //将该图形添加到areaList中.
            area = new Area(centerArea, aroundArea, geom, new PolylineClass());
            GisUtil.drawPolygon(centerArea, mapControl, GisUtil.RandomRgbColor());
            GisUtil.drawPolygon(aroundArea, mapControl, GisUtil.RandomRgbColor());
            return true;
        }

        public string getTotalHouseInfo()
        {
            int totalHouseHold = 0;
            double totalHouseArea = 0;
            foreach (HouseManager houseManager in drawnHouseList)
            {
                totalHouseHold += houseManager.house.houseHold;
                totalHouseArea += houseManager.house.width * houseManager.commonHouse.height;
            }
            string totalHouseHoldInfo = String.Format("总户数: {0}(户)", totalHouseHold);
            string totalHouseAreaInfo = String.Format("总建筑面积: {0}(平方米)", totalHouseArea);
            string totalHouseInfo = String.Format("{0}\n{1}", totalHouseHoldInfo, totalHouseAreaInfo);
            return totalHouseInfo;
        }

        public bool splitArea(IGeometry line)
        {
            IGeometry geom = area.areaGeom;
            IRelationalOperator reOp = geom as IRelationalOperator;
            if (!reOp.Crosses(line))
            {
                //没有相交关系. 
                return false;
            }
            //再开始求交点. 交点通过boundary方法获取图形的边界线. 再使用线与线的交集. 
            geom = area.areaGeom;
            ITopologicalOperator tpOp = geom as ITopologicalOperator;
            reOp = geom as IRelationalOperator;
            IGeometry geomBoundary = tpOp.Boundary;

            //将线与边界线转为点集.
            IPointCollection boundaryPtCol = geomBoundary as IPointCollection;
            IPointCollection linePtCol = line as IPointCollection;
            tpOp = boundaryPtCol as ITopologicalOperator;
            IGeometry intersectedGeom = tpOp.Intersect(linePtCol as IGeometry, esriGeometryDimension.esriGeometry0Dimension);
            IPointCollection intersectedPtCol = intersectedGeom as IPointCollection;

            //查看交集结果点的个数.
            int intersectedPtCount = intersectedPtCol.PointCount;
            //2个点说明是切割关系, 1个点说明是插入关系, 0个点说明没有关系.
            if (intersectedPtCount != 2)
            {
                return false;
            }
            //2个点. 说明是切割关系, 直接将图形内的切割线放入数组.
            GisUtil.DrawPolyline(line, mapControl);
            IPoint startPt = intersectedPtCol.get_Point(0);
            IPoint endPt = intersectedPtCol.get_Point(1);
            line = new PolylineClass();
            IPointCollection ptCol = line as IPointCollection;
            ptCol.AddPoint(startPt);
            ptCol.AddPoint(endPt);
            area.splitLine = line;
            return true;
        }

        private void saveShp(string folder, string name, IGeometry geom)
        {
            GisUtil.CreateShapefile(folder, name, mapControl.SpatialReference);
            IFeatureClass feaCls = GisUtil.getFeatureClass(folder, name);
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
            string folder = System.IO.Path.GetDirectoryName(path);
            string name = System.IO.Path.GetFileName(path);
            GisUtil.CreateShapefile(folder, name, mapControl.SpatialReference);
            IFeatureClass feaCls = GisUtil.getFeatureClass(folder, name);
            for (int i = 0; i < drawnHouseList.Count; i++)
            {
                List<IGeometry> housePolygonList = drawnHouseList[i].makeHousePolygon()[1] as List<IGeometry>;
                foreach (IGeometry geom in housePolygonList)
                {
                    IFeature fea = feaCls.CreateFeature();

                    IWorkspaceEdit wEdit = (feaCls as IDataset).Workspace as IWorkspaceEdit;
                    wEdit.StartEditing(true);
                    wEdit.StartEditOperation();

                    fea.Shape = geom;
                    fea.Store();

                    wEdit.StopEditOperation();
                    wEdit.StopEditing(true);
                }
            }
        }

        public void saveInnerRoad(string path)
        {
            string folder = System.IO.Path.GetDirectoryName(path);
            string name = System.IO.Path.GetFileName(path);
            saveShp(folder, name, innerRoadLine);
        }

        public void saveRoad(string path)
        {
            string folder = System.IO.Path.GetDirectoryName(path);
            string name = System.IO.Path.GetFileName(path);
            saveShp(folder, name, area.splitLine);
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
                    tpOp.Cut(stripLine, out tempGeom, out cutedGeom);
                }
                else
                {
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

        private void PlaceHouse(stripedRow row, List<HouseManager>drawnHouseList)
        {
            List<House> tempHouseList = new List<House>();
            foreach (House house in houseList)
            {
                tempHouseList.Add(house);
            }
            tempHouseList.Sort(delegate(House house1, House house2)
            {
                return Comparer<double>.Default.Compare(house1.leftGap + house1.width + house1.rightGap
                    , house2.leftGap + house2.width + house2.rightGap);
            });
            //将房子放在格子中, 如果没有被chosenAreaFeaCls包含, 则不保留.
            double restRowWidth = row.rowWidth; //剩余的行宽.
            IPoint currentPoint = row.stripedrow.Envelope.LowerLeft; //开始的左下角.
            int round = 0;
            while (restRowWidth > tempHouseList[tempHouseList.Count - 1].leftGap + tempHouseList[tempHouseList.Count - 1].width + tempHouseList[tempHouseList.Count - 1].rightGap)
            {
                while (true)
                {
                    if (tempHouseList[0].leftGap + tempHouseList[0].width + tempHouseList[0].rightGap > restRowWidth)
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
                houseManager.move(currentPoint);
                drawnHouseList.Add(houseManager);
                restRowWidth -= houseManager.totalHouseWidth;
                currentPoint.X += houseManager.totalHouseWidth;
            }
        }

        private void PlaceHouseByRoad(roadStripedRow row, List<HouseManager> drawnHouseList)
        {
            List<House> tempHouseList = new List<House>();
            foreach (House house in houseList)
            {
                tempHouseList.Add(house);
            }
            tempHouseList.Sort(delegate(House house1, House house2)
            {
                return Comparer<double>.Default.Compare(house1.leftGap + house1.width + house1.rightGap
                    , house2.leftGap + house2.width + house2.rightGap);
            });
            row.formatRow();
            double restRowWidth = row.rowWidth; //剩余的行宽.
            IPoint currentPoint = row.lowerLeftPt; //开始的左下角.
            int round = 0;
            while (restRowWidth > tempHouseList[tempHouseList.Count - 1].leftGap + tempHouseList[tempHouseList.Count - 1].width + tempHouseList[tempHouseList.Count - 1].rightGap)
            {
                while (true)
                {
                    if (tempHouseList[0].leftGap + tempHouseList[0].width + tempHouseList[0].rightGap > restRowWidth)
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
