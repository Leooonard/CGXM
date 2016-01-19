using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Geodatabase;
using System.IO;
using System.Collections.ObjectModel;
using System.Windows.Forms;

namespace Intersect
{
    public class PlaceHelper
    {
        private Village village;
        private CommonHouse commonHouse;
        private List<House> houseList;
        private double totalWeight;
        private List<_House> _houseList;
        private AxMapControl mapControl;

        //三维数组, 最外面那维记录哪个area, 里面记录每排, 每个排放.
        private List<List<List<_PlacedHouse>>> totalPlacedHouseList;
        private List<List<List<_PlacedHouse>>> totalPlacedRoadHouseList;

        private List<IGeometry> areaList;
        private List<IPolygon> roadList; //小区每排房子间的小路.

        //内部维护一个适宜计算的数据结构.
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

        //内部维护一个存放已排放房子的数据结构.
        private class _PlacedHouse
        {
            public House house;
            public IGeometry placedLand; //这里放的是宅基地.
            public IGeometry placedHouse;
            public double alpha; //记录房子是否有转向. (沿路的会有这个情况)

            public _PlacedHouse(House h, IGeometry p)
            {
                house = h;
                placedLand = p;
            }
        }

        private class _RoadArea
        {
            public IPolygon roadArea;
            public double alpha;
            public IPoint lowerLeftPoint;

            public _RoadArea(IPolygon ra, double a, IPoint llp)
            {
                roadArea = ra;
                alpha = a;
                lowerLeftPoint = llp;
            }
        }

        public PlaceHelper(Village v, CommonHouse ch, List<House> hl, AxMapControl mc)
        {
            village = v;
            commonHouse = ch;
            houseList = hl;
            mapControl = mc;
            totalWeight = 0; //总的比重.
            foreach (House house in houseList)
            {
                totalWeight += house.weight;
            }
            _houseList = new List<_House>();
            totalPlacedHouseList = new List<List<List<_PlacedHouse>>>();
            totalPlacedRoadHouseList = new List<List<List<_PlacedHouse>>>();
            areaList = new List<IGeometry>();
            roadList = new List<IPolygon>();
        }

        private void orgnizeRoadHouse()
        {
            foreach (List<List<_PlacedHouse>> areaPlacedRoadHouseList in totalPlacedRoadHouseList)
            {
                foreach (List<_PlacedHouse> rowPlacedRoadHouseList in areaPlacedRoadHouseList)
                {
                    bool prevNeedMerge = false;
                    for (int i = 0; i < rowPlacedRoadHouseList.Count; i++)
                    {
                        _PlacedHouse placedHouse = rowPlacedRoadHouseList[i];
                        if (placedHouse == null)
                        {
                            continue;
                        }
                        if (placedHouse.house.unit != 2)
                        {
                            //拼数不为2的没有特殊性, 直接生成自己的房子图形即可.
                            rowPlacedRoadHouseList[i] = orgnizeCommonRoadHouse(placedHouse);
                        }
                        else
                        {
                            //为2的时候, 比较特殊. 要让两个图形靠在一起.
                            //先判断前一个房子是否要求和自己合并.
                            if (prevNeedMerge)
                            {
                                prevNeedMerge = false;
                                //让自己和左边的房子贴着.
                                rowPlacedRoadHouseList[i] = orgnizeRightRoadHouse(placedHouse);
                            }
                            else
                            {
                                //先看下一个方形是否能和自己合并, 如果能, 则要求他合并.
                                if (i == rowPlacedRoadHouseList.Count - 1)
                                {
                                    //没有下一个房子了, 不能合并.
                                    rowPlacedRoadHouseList[i] = orgnizeCommonRoadHouse(placedHouse);
                                }
                                else if (rowPlacedRoadHouseList[i + 1] == null)
                                {
                                    //下一个坑位没有放宅基地, 不能合并.
                                    rowPlacedRoadHouseList[i] = orgnizeCommonRoadHouse(placedHouse);
                                }
                                else if (placedHouse.house.id != rowPlacedRoadHouseList[i + 1].house.id)
                                {
                                    //不是同一种房型, 不能合并.
                                    rowPlacedRoadHouseList[i] = orgnizeCommonRoadHouse(placedHouse);
                                }
                                else
                                {
                                    //有下一个房子, 且是同一个房型, 进行合并.
                                    prevNeedMerge = true;
                                    //和右边贴着.
                                    rowPlacedRoadHouseList[i] = orgnizeLeftRoadHouse(placedHouse);
                                }
                            }
                        }

                    }
                }
            }
        }

        private IPoint findLowerLeftPoint(_PlacedHouse placedHouse)
        { 
            IPolygon landArea = placedHouse.placedLand as IPolygon;
            IEnvelope landEnvelop = landArea.Envelope;

            //分两种情况, 一种是房子本来就是正的, 一种是歪的.
            //正的情况下, 包络线上的四点, 和图形本来四个顶点重合, 验证存在一个点重合即可.
            foreach (IPoint point in GisTool.getIPointListFromIPolygon(landArea))
            {
                if (point.X == landEnvelop.LowerLeft.X && point.Y == landEnvelop.LowerLeft.Y)
                {
                    //是正的， 直接返回0度.
                    return landEnvelop.LowerLeft;
                }
            }

            //走到这里, 即是歪的情况, 所以需要找到底下两个点, 给点集排序即可.
            List<IPoint> pointList = GisTool.getIPointListFromIPolygon(landArea);
            pointList.Sort(delegate(IPoint point1, IPoint point2)
                {
                    return Comparer<double>.Default.Compare(point1.Y, point2.Y);
                });
            IPoint lowerLeftPoint = new PointClass();
            IPoint lowerRightPoint = new PointClass();

            if (pointList[0].X < pointList[1].X)
            {
                return pointList[0];
            }
            else
            {
                return pointList[1];
            }
        }

        private _PlacedHouse orgnizeCommonRoadHouse(_PlacedHouse house)
        {
            IPoint lowerLeftPoint = findLowerLeftPoint(house);

            IElement element = new PolygonElement();
            element.Geometry = house.placedLand;
            ITransform2D transformer = element as ITransform2D;
            transformer.Rotate(lowerLeftPoint, house.alpha * -1); //旋转成正南北.
            IPolygon rotatedPlacedLand = element.Geometry as IPolygon;

            _PlacedHouse tempHouse = new _PlacedHouse(house.house, rotatedPlacedLand);
            tempHouse = orgnizeCommonHouse(tempHouse);

            element = new PolygonElement();
            element.Geometry = tempHouse.placedLand;
            transformer = element as ITransform2D;
            transformer.Rotate(lowerLeftPoint, house.alpha);
            IPolygon orgnizedPlacedLand = element.Geometry as IPolygon;

            element = new PolygonElement();
            element.Geometry = tempHouse.placedHouse;
            transformer = element as ITransform2D;
            transformer.Rotate(lowerLeftPoint, house.alpha);
            IPolygon orgnizedPlacedHouse = element.Geometry as IPolygon;

            _PlacedHouse placedHouse = new _PlacedHouse(house.house, orgnizedPlacedLand);
            placedHouse.placedHouse = orgnizedPlacedHouse;

            return placedHouse;
        }

        public bool isThisVillage(Village thatVillage)
        {
            if (village.id == thatVillage.id)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        private _PlacedHouse orgnizeLeftRoadHouse(_PlacedHouse house)
        {
            IPoint lowerLeftPoint = findLowerLeftPoint(house);

            IElement element = new PolygonElement();
            element.Geometry = house.placedLand;
            ITransform2D transformer = element as ITransform2D;
            transformer.Rotate(lowerLeftPoint, house.alpha * -1); //旋转成正南北.
            IPolygon rotatedPlacedLand = element.Geometry as IPolygon;

            _PlacedHouse tempHouse = new _PlacedHouse(house.house, rotatedPlacedLand);
            tempHouse = orgnizeLeftHouse(tempHouse);

            element = new PolygonElement();
            element.Geometry = tempHouse.placedLand;
            transformer = element as ITransform2D;
            transformer.Rotate(lowerLeftPoint, house.alpha);
            IPolygon orgnizedPlacedLand = element.Geometry as IPolygon;

            element = new PolygonElement();
            element.Geometry = tempHouse.placedHouse;
            transformer = element as ITransform2D;
            transformer.Rotate(lowerLeftPoint, house.alpha);
            IPolygon orgnizedPlacedHouse = element.Geometry as IPolygon;

            _PlacedHouse placedHouse = new _PlacedHouse(house.house, orgnizedPlacedLand);
            placedHouse.placedHouse = orgnizedPlacedHouse;

            return placedHouse;
        }

        private _PlacedHouse orgnizeRightRoadHouse(_PlacedHouse house)
        {
            IPoint lowerLeftPoint = findLowerLeftPoint(house);

            IElement element = new PolygonElement();
            element.Geometry = house.placedLand;
            ITransform2D transformer = element as ITransform2D;
            transformer.Rotate(lowerLeftPoint, house.alpha * -1); //旋转成正南北.
            IPolygon rotatedPlacedLand = element.Geometry as IPolygon;

            _PlacedHouse tempHouse = new _PlacedHouse(house.house, rotatedPlacedLand);
            tempHouse = orgnizeRightHouse(tempHouse);

            element = new PolygonElement();
            element.Geometry = tempHouse.placedLand;
            transformer = element as ITransform2D;
            transformer.Rotate(lowerLeftPoint, house.alpha);
            IPolygon orgnizedPlacedLand = element.Geometry as IPolygon;

            element = new PolygonElement();
            element.Geometry = tempHouse.placedHouse;
            transformer = element as ITransform2D;
            transformer.Rotate(lowerLeftPoint, house.alpha);
            IPolygon orgnizedPlacedHouse = element.Geometry as IPolygon;

            _PlacedHouse placedHouse = new _PlacedHouse(house.house, orgnizedPlacedLand);
            placedHouse.placedHouse = orgnizedPlacedHouse;

            return placedHouse;
        }

        private void orgnizeHouse()
        {
            foreach (List<List<_PlacedHouse>> areaPlacedHouseList in totalPlacedHouseList)
            {
                foreach (List<_PlacedHouse> rowPlacedHouseList in areaPlacedHouseList)
                {
                    bool prevNeedMerge = false; //表示前一个房子要求合并.

                    //以行为单位, 对每行进行操作.
                    for (int i = 0; i < rowPlacedHouseList.Count; i++)
                    {
                        _PlacedHouse placedHouse = rowPlacedHouseList[i];
                        if (placedHouse == null)
                        {
                            continue;
                        }
                        if (placedHouse.house.unit != 2)
                        {
                            //拼数不为2的没有特殊性, 直接生成自己的房子图形即可.
                            rowPlacedHouseList[i] = orgnizeCommonHouse(placedHouse);
                        }
                        else
                        { 
                            //为2的时候, 比较特殊. 要让两个图形靠在一起.
                            //先判断前一个房子是否要求和自己合并.
                            if (prevNeedMerge)
                            {
                                prevNeedMerge = false;
                                //让自己和左边的房子贴着.
                                rowPlacedHouseList[i] = orgnizeRightHouse(placedHouse);
                            }
                            else
                            { 
                                //先看下一个方形是否能和自己合并, 如果能, 则要求他合并.
                                if (i == rowPlacedHouseList.Count - 1)
                                {
                                    //没有下一个房子了, 不能合并.
                                    rowPlacedHouseList[i] = orgnizeCommonHouse(placedHouse);
                                }
                                else if(rowPlacedHouseList[i + 1] == null)
                                {
                                    //下一个坑位没有放宅基地, 不能合并.
                                    rowPlacedHouseList[i] = orgnizeCommonHouse(placedHouse);
                                }
                                else if (placedHouse.house.id != rowPlacedHouseList[i + 1].house.id)
                                {
                                    //不是同一种房型, 不能合并.
                                    rowPlacedHouseList[i] = orgnizeCommonHouse(placedHouse);
                                }
                                else
                                { 
                                    //有下一个房子, 且是同一个房型, 进行合并.
                                    prevNeedMerge = true;
                                    //和右边贴着.
                                    rowPlacedHouseList[i] = orgnizeLeftHouse(placedHouse);
                                }
                            }
                        }

                    }
                }
            }
        }

        private _PlacedHouse orgnizeRightHouse(_PlacedHouse placedHouse)
        {
            double dx = placedHouse.house.landWidth - placedHouse.house.width;
            IEnvelope landEnvelop = placedHouse.placedLand.Envelope;
            IPoint lowerLeftPoint = new PointClass();
            IPoint lowerRightPoint = new PointClass();
            IPoint upperLeftPoint = new PointClass();
            IPoint upperRigthPoint = new PointClass();
            lowerLeftPoint.X = landEnvelop.LowerLeft.X;
            lowerLeftPoint.Y = landEnvelop.LowerLeft.Y + placedHouse.house.frontGap;

            lowerRightPoint.X = landEnvelop.LowerRight.X - dx;
            lowerRightPoint.Y = landEnvelop.LowerRight.Y + placedHouse.house.frontGap;

            upperLeftPoint.X = landEnvelop.UpperLeft.X;
            upperLeftPoint.Y = landEnvelop.UpperLeft.Y - placedHouse.house.backGap;

            upperRigthPoint.X = landEnvelop.UpperRight.X - dx;
            upperRigthPoint.Y = landEnvelop.UpperRight.Y - placedHouse.house.backGap;
            Ring ring = new RingClass();
            ring.AddPoint(lowerLeftPoint);
            ring.AddPoint(upperLeftPoint);
            ring.AddPoint(upperRigthPoint);
            ring.AddPoint(lowerRightPoint);
            IPolygon housePolygon = GisTool.MakePolygonFromRing(ring);
            placedHouse.placedHouse = housePolygon;

            return placedHouse;
        }

        private _PlacedHouse orgnizeLeftHouse(_PlacedHouse placedHouse)
        {
            double dx = placedHouse.house.landWidth - placedHouse.house.width;
            IEnvelope landEnvelop = placedHouse.placedLand.Envelope;
            IPoint lowerLeftPoint = new PointClass();
            IPoint lowerRightPoint = new PointClass();
            IPoint upperLeftPoint = new PointClass();
            IPoint upperRigthPoint = new PointClass();
            lowerLeftPoint.X = landEnvelop.LowerLeft.X + dx;
            lowerLeftPoint.Y = landEnvelop.LowerLeft.Y + placedHouse.house.frontGap;
            lowerRightPoint.X = landEnvelop.LowerRight.X;
            lowerRightPoint.Y = landEnvelop.LowerRight.Y + placedHouse.house.frontGap;
            upperLeftPoint.X = landEnvelop.UpperLeft.X + dx;
            upperLeftPoint.Y = landEnvelop.UpperLeft.Y - placedHouse.house.backGap;
            upperRigthPoint.X = landEnvelop.UpperRight.X;
            upperRigthPoint.Y = landEnvelop.UpperRight.Y - placedHouse.house.backGap;
            Ring ring = new RingClass();
            ring.AddPoint(lowerLeftPoint);
            ring.AddPoint(upperLeftPoint);
            ring.AddPoint(upperRigthPoint);
            ring.AddPoint(lowerRightPoint);
            IPolygon housePolygon = GisTool.MakePolygonFromRing(ring);
            placedHouse.placedHouse = housePolygon;

            return placedHouse;
        }

        private _PlacedHouse orgnizeCommonHouse(_PlacedHouse placedHouse)
        { 
            //对普通的宅基地进行房子的排放, 直接放在正中间即可.
            double dx = (placedHouse.house.landWidth - placedHouse.house.width) / 2;
            //正向的房子可以直接拿包络线上的四个顶角.
            IEnvelope landEnvelop = placedHouse.placedLand.Envelope;
            IPoint lowerLeftPoint = new PointClass();
            IPoint lowerRightPoint = new PointClass();
            IPoint upperLeftPoint = new PointClass();
            IPoint upperRigthPoint = new PointClass();
            lowerLeftPoint.X = landEnvelop.LowerLeft.X + dx;
            lowerLeftPoint.Y = landEnvelop.LowerLeft.Y + placedHouse.house.frontGap;
            lowerRightPoint.X = landEnvelop.LowerRight.X - dx;
            lowerRightPoint.Y = landEnvelop.LowerRight.Y + placedHouse.house.frontGap;
            upperLeftPoint.X = landEnvelop.UpperLeft.X + dx;
            upperLeftPoint.Y = landEnvelop.UpperLeft.Y - placedHouse.house.backGap;
            upperRigthPoint.X = landEnvelop.UpperRight.X - dx;
            upperRigthPoint.Y = landEnvelop.UpperRight.Y - placedHouse.house.backGap;
            Ring ring = new RingClass();
            ring.AddPoint(lowerLeftPoint);
            ring.AddPoint(upperLeftPoint);
            ring.AddPoint(upperRigthPoint);
            ring.AddPoint(lowerRightPoint);
            IPolygon housePolygon = GisTool.MakePolygonFromRing(ring);
            placedHouse.placedHouse = housePolygon;
            return placedHouse;
        }

        public void place()
        {
            //1.把区域根据内部路切成三块.
            //拿到切好后的图形. list中三个元素, 第一个元素是逻辑上的左边区块, 第二个元素是逻辑上的右边区块,
            //第三个元素是内部路扩张后的区块. 三个元素组合起来就是village中的polygon了.
            //所谓逻辑上的左右是因为, 内部路画的时候方向不同, 作为一个向量, 方向相反时, 逻辑上的左右就颠倒了.
            areaList = cut();

            for (int i = 0; i < areaList.Count - 1; i++)
            {
                List<_RoadArea> roadHouseAreaList = new List<_RoadArea>(); //沿路放房子的排.
                IPolygon area = areaList[i] as IPolygon;
                ITopologicalOperator areaTpOp = area as ITopologicalOperator;
                List<IPolyline> southRoadList = findSouthRoadList(area); //南边公路.
                //把南边公路沿路排房子的区域挖出来.
                foreach (IPolyline southRoad in southRoadList)
                {
                    List<IPolyline> extendedSouthRoadList = extendRoad(southRoad, commonHouse.landHeight);
                    foreach (IPolyline extendedSouthRoad in extendedSouthRoadList)
                    {
                        IPointCollection southRoadPointCollection = southRoad as IPointCollection;
                        IPointCollection extendedSouthRoadPointCollection = extendedSouthRoad as IPointCollection;
                        if (southRoadPointCollection.get_Point(0).Y < extendedSouthRoadPointCollection.get_Point(0).Y)
                        { 
                            //找到正确的那条路了. 把两条平行的路拼成一个区域.
                            IPolyline prolongedSouthRoadPolyline = prolongRoad(extendedSouthRoad, area);
                            IPointCollection prolongedSouthRoadPointCollection = prolongedSouthRoadPolyline as IPointCollection;
                            Ring ring = new RingClass();
                            ring.AddPoint(southRoadPointCollection.get_Point(0));
                            ring.AddPoint(southRoadPointCollection.get_Point(1));
                            ring.AddPoint(extendedSouthRoadPointCollection.get_Point(1));
                            ring.AddPoint(extendedSouthRoadPointCollection.get_Point(0));
                            IPolygon roadArea = GisTool.MakePolygonFromRing(ring);
                            roadArea = areaTpOp.Intersect(roadArea, esriGeometryDimension.esriGeometry2Dimension) as IPolygon;
                            double alpha = Math.Atan((southRoadPointCollection.get_Point(0).Y - southRoadPointCollection.get_Point(1).Y) /
                                (southRoadPointCollection.get_Point(0).X - southRoadPointCollection.get_Point(1).X));
                            IPoint lowerLeftPoint = new PointClass();
                            if(southRoadPointCollection.get_Point(0).X < southRoadPointCollection.get_Point(1).X)
                            {
                                lowerLeftPoint.X = southRoadPointCollection.get_Point(0).X;
                                lowerLeftPoint.Y = southRoadPointCollection.get_Point(0).Y;
                            }
                            else
                            {
                                lowerLeftPoint.X = southRoadPointCollection.get_Point(1).X;
                                lowerLeftPoint.Y = southRoadPointCollection.get_Point(1).Y;
                            }
                            roadHouseAreaList.Add(new _RoadArea(roadArea, alpha, lowerLeftPoint));
                            ring = new RingClass();
                            ring.AddPoint(southRoadPointCollection.get_Point(0));
                            ring.AddPoint(southRoadPointCollection.get_Point(1));
                            ring.AddPoint(prolongedSouthRoadPointCollection.get_Point(1));
                            ring.AddPoint(prolongedSouthRoadPointCollection.get_Point(0));
                            roadArea = GisTool.MakePolygonFromRing(ring); //路的那一整块都不能用.
                            area = areaTpOp.Difference(roadArea) as IPolygon;
                            areaTpOp = area as ITopologicalOperator;
                            break;
                        }
                    }
                }

                List<IPolyline> northRoadList = findNorthRoadList(area); //北边公路.
                //把北边公路沿路排房子的区域挖出来.
                foreach (IPolyline northRoad in northRoadList)
                {
                    List<IPolyline> extendedNorthRoadList = extendRoad(northRoad, commonHouse.landHeight);
                    foreach (IPolyline extendedNorthRoad in extendedNorthRoadList)
                    {
                        IPointCollection northRoadPointCollection = northRoad as IPointCollection;
                        IPointCollection extendedNorthRoadPointCollection = extendedNorthRoad as IPointCollection;
                        if (northRoadPointCollection.get_Point(0).Y > extendedNorthRoadPointCollection.get_Point(0).Y)
                        {
                            //找到正确的那条路了. 把两条平行的路拼成一个区域.
                            IPolyline prolongedNorthRoadPolyline = prolongRoad(extendedNorthRoad, area);
                            IPointCollection prolongedNorthRoadPointCollection = prolongedNorthRoadPolyline as IPointCollection;
                            Ring ring = new RingClass();
                            ring.AddPoint(northRoadPointCollection.get_Point(0));
                            ring.AddPoint(northRoadPointCollection.get_Point(1));
                            ring.AddPoint(extendedNorthRoadPointCollection.get_Point(1));
                            ring.AddPoint(extendedNorthRoadPointCollection.get_Point(0));
                            IPolygon roadArea = GisTool.MakePolygonFromRing(ring);
                            roadArea = areaTpOp.Intersect(roadArea, esriGeometryDimension.esriGeometry2Dimension) as IPolygon;
                            double alpha = Math.Atan((northRoadPointCollection.get_Point(0).Y - northRoadPointCollection.get_Point(1).Y) /
                                (northRoadPointCollection.get_Point(0).X - northRoadPointCollection.get_Point(1).X));
                            IPoint lowerLeftPoint = new PointClass();
                            if (extendedNorthRoadPointCollection.get_Point(0).X < extendedNorthRoadPointCollection.get_Point(1).X)
                            {
                                lowerLeftPoint.X = extendedNorthRoadPointCollection.get_Point(0).X;
                                lowerLeftPoint.Y = extendedNorthRoadPointCollection.get_Point(0).Y;
                            }
                            else
                            {
                                lowerLeftPoint.X = extendedNorthRoadPointCollection.get_Point(1).X;
                                lowerLeftPoint.Y = extendedNorthRoadPointCollection.get_Point(1).Y;
                            }
                            roadHouseAreaList.Add(new _RoadArea(roadArea, alpha, lowerLeftPoint));
                            ring = new RingClass();
                            ring.AddPoint(northRoadPointCollection.get_Point(0));
                            ring.AddPoint(northRoadPointCollection.get_Point(1));
                            ring.AddPoint(prolongedNorthRoadPointCollection.get_Point(1));
                            ring.AddPoint(prolongedNorthRoadPointCollection.get_Point(0));
                            roadArea = GisTool.MakePolygonFromRing(ring); //路的那一整块都不能用.
                            area = areaTpOp.Difference(roadArea) as IPolygon;
                            areaTpOp = area as ITopologicalOperator;
                            break;
                        }
                    }
                }

                //开始按包络线, 一排一排划条.
                List<List<IPolygon>> rowList = splitRow(area);
                List<IPolygon> houseRowList = rowList[0];
                foreach (IPolygon roadRow in rowList[1])
                {
                    roadList.Add(roadRow);
                }

                //全部区域截取完了, 算一下理论上能排放的量, 然后开始排放.
                double totalArea = 0;
                foreach (_RoadArea roadHouseArea in roadHouseAreaList)
                {
                    totalArea += (roadHouseArea.roadArea as IArea).Area;
                }
                foreach (IPolygon houseArea in houseRowList)
                {
                    totalArea += (houseArea as IArea).Area;
                }

                //算出理论值, 初始化_House.
                foreach (House house in houseList)
                {
                    _House _house = new _House(house, (int)(((house.weight / totalWeight) * totalArea) / (house.landWidth * commonHouse.landHeight)));
                    _houseList.Add(_house);
                }

                //然后把_houseList里的内容, 按面积从大到小排放.
                _houseList.Sort(delegate(_House _house1, _House _house2)
                {
                    return Comparer<double>.Default.Compare(_house1.house.width, _house2.house.width);
                });

                //沿路排房.
                List<List<_PlacedHouse>> placedRoadHouseList = new List<List<_PlacedHouse>>();
                foreach (_RoadArea roadHouseArea in roadHouseAreaList)
                {
                    List<_PlacedHouse> roadHouseList = placeInRoadRow(roadHouseArea.roadArea, roadHouseArea.alpha, roadHouseArea.lowerLeftPoint);
                    foreach (_PlacedHouse roadHouse in roadHouseList)
                    {
                        roadHouse.alpha = roadHouseArea.alpha;
                    }
                    placedRoadHouseList.Add(roadHouseList);
                }
                totalPlacedRoadHouseList.Add(placedRoadHouseList);

                //二维数组, 每个对象记录一排内放的房子, 所有排的房子构成这个数组.
                List<List<_PlacedHouse>> placedHouseList = new List<List<_PlacedHouse>>();

                //开始排放.
                foreach (IPolygon houseRow in houseRowList)
                { 
                    //先切一下, 切成长条形
                    IPolygon formatedHouseRowPolygon = formatRow(houseRow, area);
                    if (formatedHouseRowPolygon == null)
                    {
                        continue;
                    }

                    //开始在里面排房子.
                    List<_PlacedHouse> placedRowHouseList = placeInRow(formatedHouseRowPolygon, area);
                    placedHouseList.Add(placedRowHouseList);
                }

                totalPlacedHouseList.Add(placedHouseList);
            }


            //把房子按拼数进行整理.
            orgnizeHouse();
            orgnizeRoadHouse();
        }

        public void save(string path)
        {
            saveHouse(path);
            saveLand(path);
            saveArea(path);
            saveInnerRoad(path);
            saveRoad(path);
        }

        public void add(string path)
        {
            string houseFileName = String.Format("房屋_{0}.shp", village.name);
            string houseLayerName = String.Format("房屋_{0}", village.name);
            load(path, houseFileName, houseLayerName);

            string landFileName = String.Format("宅基地_{0}.shp", village.name);
            string landLayerName = String.Format("宅基地_{0}", village.name);
            load(path, landFileName, landLayerName);

            string areaFileName = String.Format("区域_{0}.shp", village.name);
            string areaLayerName = String.Format("区域_{0}", village.name);
            load(path, areaFileName, areaLayerName);

            string roadFileName = String.Format("道路_{0}.shp", village.name);
            string roadLayerName = String.Format("道路_{0}", village.name);
            load(path, roadFileName, roadLayerName);

            string innerRoadFileName = String.Format("内部路_{0}.shp", village.name);
            string innerRoadLayerName = String.Format("内部路_{0}", village.name);
            load(path, innerRoadFileName, innerRoadLayerName);
        }

        public void clear(string path)
        {
            string houseLayerName = String.Format("房屋_{0}", village.name);
            delete(path, houseLayerName);

            string landLayerName = String.Format("宅基地_{0}", village.name);
            delete(path, landLayerName);

            string areaLayerName = String.Format("区域_{0}", village.name);
            delete(path, areaLayerName);

            string roadLayerName = String.Format("道路_{0}", village.name);
            delete(path, roadLayerName);

            string innerRoadLayerName = String.Format("内部路_{0}", village.name);
            delete(path, innerRoadLayerName);
        }

        public bool check(string path)
        {
            string houseLayerName = String.Format("房屋_{0}.shp", village.name);
            if (!File.Exists(System.IO.Path.Combine(path, houseLayerName)))
            {
                return false;
            }

            string landLayerName = String.Format("宅基地_{0}.shp", village.name);
            if (!File.Exists(System.IO.Path.Combine(path, landLayerName)))
            {
                return false;
            }

            string areaLayerName = String.Format("区域_{0}.shp", village.name);
            if (!File.Exists(System.IO.Path.Combine(path, areaLayerName)))
            {
                return false;
            }

            string roadLayerName = String.Format("道路_{0}.shp", village.name);
            if (!File.Exists(System.IO.Path.Combine(path, roadLayerName)))
            {
                return false;
            }

            string innerRoadLayerName = String.Format("内部路_{0}.shp", village.name);
            if (!File.Exists(System.IO.Path.Combine(path, innerRoadLayerName)))
            {
                return false;
            }

            return true;
        }

        public List<HouseResult> report(string path)
        {
            string houseFileName = String.Format("房屋_{0}.shp", village.name);
            IFeatureClass houseFeatureClass = GisTool.getFeatureClass(path, houseFileName);
            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = houseFeatureClass;
            int totalFeatureCount = houseFeatureClass.FeatureCount(null);
            List<HouseResult> houseResultList = new List<HouseResult>();
            double totalConstructArea = 0;

            foreach (House house in village.houseList)
            {
                HouseResult houseResult = new HouseResult();
                IQueryFilter filter = new QueryFilterClass();
                filter.WhereClause = "\"类型\"='" + house.id.ToString() + "'";
                IFeatureSelection featureSelection = featureLayer as IFeatureSelection;
                featureSelection.SelectFeatures(filter, esriSelectionResultEnum.esriSelectionResultNew, false);
                ISelectionSet selSet = featureSelection.SelectionSet;
                houseResult.count = selSet.Count;

                houseResult.villageName = village.name;
                houseResult.houseName = house.name;
                houseResult.area = house.width * house.height * village.commonHouse.floor;
                houseResult.landArea = house.width * house.height * houseResult.count;
                houseResult.constructArea = house.width * house.height * village.commonHouse.floor * houseResult.count;
                totalConstructArea += houseResult.constructArea;
                houseResult.ratio = (double)houseResult.count / totalFeatureCount * 100;
                houseResultList.Add(houseResult);
            }

            HouseResult totalHouseResult = new HouseResult();
            totalHouseResult.villageName = "总计";
            totalHouseResult.houseName = "";
            totalHouseResult.constructArea = totalConstructArea;
            IPolygon villagePolygon = (village.polygonElement as IElement).Geometry as IPolygon;
            IArea area = villagePolygon as IArea;
            totalHouseResult.ratio = Double.Parse(String.Format("{0:F}", totalConstructArea / area.Area * 100));

            houseResultList.Add(totalHouseResult);

            return houseResultList;
        }

        private void delete(string path, string layerName)
        {
            int index = GisTool.getLayerIndexByName(layerName, mapControl);
            if (index == -1)
            {
                return;
            }

            mapControl.DeleteLayer(index);
            GisTool.DeleteShapeFile(System.IO.Path.Combine(path, layerName + ".shp"));
        }

        private void load(string path, string fileName, string layerName)
        {
            if (File.Exists(System.IO.Path.Combine(path, fileName)))
            {
                IFeatureClass houseFeatureClass = GisTool.getFeatureClass(path, fileName);
                IFeatureLayer houseFeatureLayer = new FeatureLayerClass();
                houseFeatureLayer.FeatureClass = houseFeatureClass;
                houseFeatureLayer.Name = layerName;
                ILayerEffects layerEffects = houseFeatureLayer as ILayerEffects;
                layerEffects.Transparency = 60;
                mapControl.AddLayer(houseFeatureLayer);
                mapControl.ActiveView.Refresh();
            }
        }

        public void exportHouse()
        {
            FolderBrowserDialog folderDialog = new FolderBrowserDialog();
            folderDialog.ShowDialog();
            string folderPath = folderDialog.SelectedPath;
            exportArea(folderPath);
            exportInnerRoad(folderPath);
            exportRoad(folderPath);
            exportHouseLand(folderPath);
            exportHouseElement(folderPath);
        }

        private void exportArea(string path)
        {
            string AreaFileName = "quyu.shp";
            GisTool.CreateShapefile(path, AreaFileName, mapControl.SpatialReference);
            IFeatureClass featureClass = GisTool.getFeatureClass(path, AreaFileName);
            GisTool.addFeatureLayerField(featureClass, "role", esriFieldType.esriFieldTypeString, 10);

            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            //最后一个元素是内部路区域.
            for (int i = 0; i < areaList.Count - 1; i++)
            {
                IPolygon area = areaList[i] as IPolygon;
                IFeature fea = featureClass.CreateFeature();
                fea.Shape = area;
                fea.Store();
                ITable pTable = (ITable)featureLayer;
                IRow pRow = pTable.GetRow(featureClass.FeatureCount(null) - 1);
                pRow.set_Value(pTable.FindField("role"), "quyu");
                pRow.Store();
            }

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        private void exportInnerRoad(string path)
        {
            string innerRoadFileName = "neibulu.shp";
            GisTool.CreateShapefile(path, innerRoadFileName, mapControl.SpatialReference);
            IFeatureClass featureClass = GisTool.getFeatureClass(path, innerRoadFileName);
            GisTool.addFeatureLayerField(featureClass, "role", esriFieldType.esriFieldTypeString, 10);

            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            //最后一个元素是内部路区域.
            IPolygon area = areaList[areaList.Count - 1] as IPolygon;
            IFeature fea = featureClass.CreateFeature();
            fea.Shape = area;
            fea.Store();
            ITable pTable = (ITable)featureLayer;
            IRow pRow = pTable.GetRow(featureClass.FeatureCount(null) - 1);
            pRow.set_Value(pTable.FindField("role"), "neibulu");
            pRow.Store();

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        private void exportRoad(string path)
        {
            string roadFileName = "daolu.shp";
            GisTool.CreateShapefile(path, roadFileName, mapControl.SpatialReference);
            IFeatureClass featureClass = GisTool.getFeatureClass(path, roadFileName);
            GisTool.addFeatureLayerField(featureClass, "role", esriFieldType.esriFieldTypeString, 10);

            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            foreach (IPolygon road in roadList)
            {
                IFeature fea = featureClass.CreateFeature();
                fea.Shape = road;
                fea.Store();
                ITable pTable = (ITable)featureLayer;
                IRow pRow = pTable.GetRow(featureClass.FeatureCount(null) - 1);
                pRow.set_Value(pTable.FindField("role"), "daolu");
                pRow.Store();
            }

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        private void exportHouseElement(string path)
        {
            string houseFileName = "fangwu.shp";

            GisTool.CreateShapefile(path, houseFileName, mapControl.SpatialReference);
            IFeatureClass featureClass = GisTool.getFeatureClass(path, houseFileName);
            GisTool.addFeatureLayerField(featureClass, "layer", esriFieldType.esriFieldTypeInteger, 5);
            GisTool.addFeatureLayerField(featureClass, "cate", esriFieldType.esriFieldTypeString, 5);
            GisTool.addFeatureLayerField(featureClass, "role", esriFieldType.esriFieldTypeString, 10);

            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            foreach (List<List<_PlacedHouse>> areaPlacedHouseList in totalPlacedHouseList)
            {
                foreach (List<_PlacedHouse> rowPlacedHouseList in areaPlacedHouseList)
                {
                    foreach (_PlacedHouse placedHouse in rowPlacedHouseList)
                    {
                        if (placedHouse == null)
                        {
                            continue;
                        }
                        IFeature fea = featureClass.CreateFeature();
                        fea.Shape = placedHouse.placedHouse;
                        fea.Store();
                        ITable pTable = (ITable)featureLayer;
                        IRow pRow = pTable.GetRow(featureClass.FeatureCount(null) - 1);
                        pRow.set_Value(pTable.FindField("layer"), commonHouse.floor);
                        pRow.set_Value(pTable.FindField("cate"), placedHouse.house.id);
                        pRow.set_Value(pTable.FindField("role"), "fangwu");
                        pRow.Store();
                    }
                }
            }

            foreach (List<List<_PlacedHouse>> areaPlacedRoadHouseList in totalPlacedRoadHouseList)
            {
                foreach (List<_PlacedHouse> rowPlacedRoadHouseList in areaPlacedRoadHouseList)
                {
                    foreach (_PlacedHouse placedHouse in rowPlacedRoadHouseList)
                    {
                        if (placedHouse == null)
                        {
                            continue;
                        }
                        IFeature fea = featureClass.CreateFeature();
                        fea.Shape = placedHouse.placedHouse;
                        fea.Store();
                        ITable pTable = (ITable)featureLayer;
                        IRow pRow = pTable.GetRow(featureClass.FeatureCount(null) - 1);
                        pRow.set_Value(pTable.FindField("layer"), commonHouse.floor);
                        pRow.set_Value(pTable.FindField("cate"), placedHouse.house.id);
                        pRow.set_Value(pTable.FindField("role"), "fangwu");
                        pRow.Store();
                    }
                }
            }

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        private void exportHouseLand(string path)
        {
            string landFileName = "zhaijidi.shp";

            GisTool.CreateShapefile(path, landFileName, mapControl.SpatialReference);
            IFeatureClass featureClass = GisTool.getFeatureClass(path, landFileName);
            GisTool.addFeatureLayerField(featureClass, "role", esriFieldType.esriFieldTypeString, 10);

            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            foreach (List<List<_PlacedHouse>> areaPlacedHouseList in totalPlacedHouseList)
            {
                foreach (List<_PlacedHouse> rowPlacedHouseList in areaPlacedHouseList)
                {
                    foreach (_PlacedHouse placedHouse in rowPlacedHouseList)
                    {
                        if (placedHouse == null)
                        {
                            continue;
                        }
                        IFeature fea = featureClass.CreateFeature();
                        fea.Shape = placedHouse.placedLand;
                        fea.Store();
                        ITable pTable = (ITable)featureLayer;
                        IRow pRow = pTable.GetRow(featureClass.FeatureCount(null) - 1);
                        pRow.set_Value(pTable.FindField("role"), "zhaijidi");
                        pRow.Store();
                    }
                }
            }

            foreach (List<List<_PlacedHouse>> areaPlacedRoadHouseList in totalPlacedRoadHouseList)
            {
                foreach (List<_PlacedHouse> rowPlacedRoadHouseList in areaPlacedRoadHouseList)
                {
                    foreach (_PlacedHouse placedHouse in rowPlacedRoadHouseList)
                    {
                        if (placedHouse == null)
                        {
                            continue;
                        }
                        IFeature fea = featureClass.CreateFeature();
                        fea.Shape = placedHouse.placedLand;
                        fea.Store();
                        ITable pTable = (ITable)featureLayer;
                        IRow pRow = pTable.GetRow(featureClass.FeatureCount(null) - 1);
                        pRow.set_Value(pTable.FindField("role"), "zhaijidi");
                        pRow.Store();
                    }
                }
            }

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        private void saveRoad(string path) 
        {
            string roadFileName = String.Format("道路_{0}.shp", village.name);
            GisTool.CreateShapefile(path, roadFileName, mapControl.SpatialReference);
            IFeatureClass featureClass = GisTool.getFeatureClass(path, roadFileName);

            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            foreach (IPolygon road in roadList)
            {
                IFeature fea = featureClass.CreateFeature();
                fea.Shape = road;
                fea.Store();
            }

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        private void saveInnerRoad(string path)
        {
            string innerRoadFileName = String.Format("内部路_{0}.shp", village.name);
            GisTool.CreateShapefile(path, innerRoadFileName, mapControl.SpatialReference);
            IFeatureClass featureClass = GisTool.getFeatureClass(path, innerRoadFileName);

            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            //最后一个元素是内部路区域.
            IPolygon area = areaList[areaList.Count - 1] as IPolygon;
            IFeature fea = featureClass.CreateFeature();
            fea.Shape = area;
            fea.Store();

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        private void saveArea(string path)
        {
            string AreaFileName = String.Format("区域_{0}.shp", village.name);
            GisTool.CreateShapefile(path, AreaFileName, mapControl.SpatialReference);
            IFeatureClass featureClass = GisTool.getFeatureClass(path, AreaFileName);

            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            //最后一个元素是内部路区域.
            for (int i = 0; i < areaList.Count - 1; i++ )
            {
                IPolygon area = areaList[i] as IPolygon;
                IFeature fea = featureClass.CreateFeature();
                fea.Shape = area;
                fea.Store();
            }

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        private void saveLand(string path)
        {
            string landFileName = String.Format("宅基地_{0}.shp", village.name);

            GisTool.CreateShapefile(path, landFileName, mapControl.SpatialReference);
            IFeatureClass featureClass = GisTool.getFeatureClass(path, landFileName);

            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            foreach (List<List<_PlacedHouse>> areaPlacedHouseList in totalPlacedHouseList)
            {
                foreach (List<_PlacedHouse> rowPlacedHouseList in areaPlacedHouseList)
                {
                    foreach (_PlacedHouse placedHouse in rowPlacedHouseList)
                    {
                        if (placedHouse == null)
                        {
                            continue;
                        }
                        IFeature fea = featureClass.CreateFeature();
                        fea.Shape = placedHouse.placedLand;
                        fea.Store();
                    }
                }
            }

            foreach (List<List<_PlacedHouse>> areaPlacedRoadHouseList in totalPlacedRoadHouseList)
            {
                foreach (List<_PlacedHouse> rowPlacedRoadHouseList in areaPlacedRoadHouseList)
                {
                    foreach (_PlacedHouse placedHouse in rowPlacedRoadHouseList)
                    {
                        if (placedHouse == null)
                        {
                            continue;
                        }
                        IFeature fea = featureClass.CreateFeature();
                        fea.Shape = placedHouse.placedLand;
                        fea.Store();
                    }
                }
            }

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        private void saveHouse(string path)
        {
            string houseFileName = String.Format("房屋_{0}.shp", village.name);

            GisTool.CreateShapefile(path, houseFileName, mapControl.SpatialReference);
            IFeatureClass featureClass = GisTool.getFeatureClass(path, houseFileName);
            GisTool.addFeatureLayerField(featureClass, "层数", esriFieldType.esriFieldTypeInteger, 5);
            GisTool.addFeatureLayerField(featureClass, "类型", esriFieldType.esriFieldTypeString, 5);

            IFeatureLayer featureLayer = new FeatureLayerClass();
            featureLayer.FeatureClass = featureClass;
            IWorkspaceEdit workspaceEdit = (featureClass as IDataset).Workspace as IWorkspaceEdit;
            workspaceEdit.StartEditing(true);
            workspaceEdit.StartEditOperation();

            foreach (List<List<_PlacedHouse>> areaPlacedHouseList in totalPlacedHouseList)
            {
                foreach (List<_PlacedHouse> rowPlacedHouseList in areaPlacedHouseList)
                {
                    foreach (_PlacedHouse placedHouse in rowPlacedHouseList)
                    {
                        if (placedHouse == null)
                        {
                            continue;
                        }
                        IFeature fea = featureClass.CreateFeature();
                        fea.Shape = placedHouse.placedHouse;
                        fea.Store();
                        ITable pTable = (ITable)featureLayer;
                        IRow pRow = pTable.GetRow(featureClass.FeatureCount(null) - 1);
                        pRow.set_Value(pTable.FindField("层数"), commonHouse.floor);
                        pRow.set_Value(pTable.FindField("类型"), placedHouse.house.id);
                        pRow.Store();
                    }
                }
            }

            foreach (List<List<_PlacedHouse>> areaPlacedRoadHouseList in totalPlacedRoadHouseList)
            {
                foreach (List<_PlacedHouse> rowPlacedRoadHouseList in areaPlacedRoadHouseList)
                {
                    foreach (_PlacedHouse placedHouse in rowPlacedRoadHouseList)
                    {
                        if (placedHouse == null)
                        {
                            continue;
                        }
                        IFeature fea = featureClass.CreateFeature();
                        fea.Shape = placedHouse.placedHouse;
                        fea.Store();
                        ITable pTable = (ITable)featureLayer;
                        IRow pRow = pTable.GetRow(featureClass.FeatureCount(null) - 1);
                        pRow.set_Value(pTable.FindField("层数"), commonHouse.floor);
                        pRow.set_Value(pTable.FindField("类型"), placedHouse.house.id);
                        pRow.Store();
                    }
                }
            }

            workspaceEdit.StopEditOperation();
            workspaceEdit.StopEditing(true);
        }

        private List<_PlacedHouse> placeInRoadRow(IPolygon row, double alpha, IPoint lowerLeftPoint)
        {
            //先旋转成正南北, 再转回去.
            IElement element = new PolygonElement();
            element.Geometry = row;
            ITransform2D transformer = element as ITransform2D;
            transformer.Rotate(lowerLeftPoint, alpha * -1);
            IPolygon rotatedRow = element.Geometry as IPolygon;

            List<_PlacedHouse> roadHouseList = placeInRow(rotatedRow, rotatedRow);
            List<_PlacedHouse> rotatedRoadHouseList = new List<_PlacedHouse>();
            transformer.Rotate(lowerLeftPoint, alpha);
            
            foreach (_PlacedHouse roadPlacedHouse in roadHouseList)
            {
                if (roadPlacedHouse == null)
                {
                    continue;
                }
                IElement roadHouseElement = new PolygonElement();
                roadHouseElement.Geometry = roadPlacedHouse.placedLand;
                ITransform2D roadHouseTransformer = roadHouseElement as ITransform2D;
                roadHouseTransformer.Rotate(lowerLeftPoint, alpha);

                _PlacedHouse rotatedPlacedHouse = new _PlacedHouse(roadPlacedHouse.house, roadHouseElement.Geometry as IPolygon);
                rotatedRoadHouseList.Add(rotatedPlacedHouse);
            }
            return rotatedRoadHouseList;
        }

        //row认为是规范的矩形, 即正南正北的矩形图形.
        private List<_PlacedHouse> placeInRow(IPolygon row, IPolygon baseArea)
        {
            IRelationalOperator baseAreaReOp = baseArea as IRelationalOperator;
            List<_PlacedHouse> placedHouseList = new List<_PlacedHouse>();
            IEnvelope rowEnvelop = row.Envelope;
            double restRowWidth = rowEnvelop.LowerRight.X - rowEnvelop.LowerLeft.X;
            IPoint currentPoint = new PointClass();
            currentPoint.X = rowEnvelop.UpperLeft.X;
            currentPoint.Y = rowEnvelop.UpperLeft.Y;

            //由大到小往里放，放的下且有额度就往里放一个。
            for (int i = _houseList.Count - 1; i >= 0; i--)
            {
                while (restRowWidth > _houseList[i].house.landWidth)
                {
                    if (_houseList[i].count == 0 && i != 0) //最小的那个房型永远可以往下放.
                    {
                        //额度用完了。给下一个房型排。
                        break;
                    }

                    IPolygon housePolygon = GisTool.MakePolygon(currentPoint.X, currentPoint.Y,
                        _houseList[i].house.landWidth, commonHouse.landHeight);
                    if (baseAreaReOp.Contains(housePolygon))
                    {
                        _PlacedHouse _placedHouse = new _PlacedHouse(_houseList[i].house, housePolygon);
                        _houseList[i].count--;
                        placedHouseList.Add(_placedHouse);
                    }
                    else
                    {
                        placedHouseList.Add(null);
                    }

                    restRowWidth -= _houseList[i].house.landWidth;
                    currentPoint.X += _houseList[i].house.landWidth;
                }
            }

            return placedHouseList;
        }

        private IPolygon formatRow(IPolygon area, IPolygon baseArea)
        {
            double yMax = (area.Envelope).UpperLeft.Y;
            double yMin = (area.Envelope).LowerLeft.Y;
            double lowerXMin = (area.Envelope).LowerRight.X;
            double lowerXMax = -1;
            double upperXMin = (area.Envelope).UpperRight.X;
            double upperXMax = -1;

            ITopologicalOperator areaTpOp = area as ITopologicalOperator;
            IPolygon tempPolygon = areaTpOp.Intersect(baseArea, esriGeometryDimension.esriGeometry2Dimension) as IPolygon;
            List<IPoint> pointList = GisTool.getIPointListFromIPolygon(tempPolygon);

            //找到两条线的最左边上的点, 进行比较, 取大的那个. 再找右边的, 取小的那个.
            foreach (IPoint point in pointList)
            {
                if (point.Y == yMax)
                {
                    if (upperXMax < point.X)
                    {
                        upperXMax = point.X;
                    }
                    if (upperXMin > point.X)
                    {
                        upperXMin = point.X;
                    }
                }
                else if (point.Y == yMin)
                {
                    if (lowerXMax < point.X)
                    {
                        lowerXMax = point.X;
                    }
                    if (lowerXMin > point.X)
                    {
                        lowerXMin = point.X;
                    }
                }
            }
            double xMin = 0;
            double xMax = 0;

            if (lowerXMin > upperXMin)
            {
                xMin = lowerXMin;
            }
            else
            {
                xMin = upperXMin;
            }

            if (lowerXMax > upperXMax)
            {
                xMax = upperXMax;
            }
            else
            {
                xMax = lowerXMax;
            }

            if (xMin >= xMax)
            {
                return null;
            }

            IPoint lowerLeftPoint = new PointClass();
            lowerLeftPoint.X = xMin;
            lowerLeftPoint.Y = yMin;
            IPoint lowerRightPoint = new PointClass();
            lowerRightPoint.X = xMax;
            lowerRightPoint.Y = yMin;
            IPoint upperLeftPoint = new PointClass();
            upperLeftPoint.X = xMin;
            upperLeftPoint.Y = yMax;
            IPoint upperRightPoint = new PointClass();
            upperRightPoint.X = xMax;
            upperRightPoint.Y = yMax;
            Ring ring = new RingClass();
            ring.AddPoint(lowerLeftPoint);
            ring.AddPoint(upperLeftPoint);
            ring.AddPoint(upperRightPoint);
            ring.AddPoint(lowerRightPoint);

            IPolygon formatRow = GisTool.MakePolygonFromRing(ring);
            return formatRow;
        }

        private List<List<IPolygon>> splitRow(IPolygon area)
        {
            List<List<IPolygon>> rowList = new List<List<IPolygon>>();
            List<IPolygon> houseRowList = new List<IPolygon>();
            List<IPolygon> roadRowList = new List<IPolygon>();
            IEnvelope areaEnvelop = area.Envelope;
            Ring ring = new RingClass();
            ring.AddPoint(areaEnvelop.LowerLeft);
            ring.AddPoint(areaEnvelop.UpperLeft);
            ring.AddPoint(areaEnvelop.UpperRight);
            ring.AddPoint(areaEnvelop.LowerRight);
            IPolygon areaEnvelopPolygon = GisTool.MakePolygonFromRing(ring);
            IPoint lowerLeftPoint = new PointClass();
            lowerLeftPoint.X = areaEnvelop.LowerLeft.X;
            lowerLeftPoint.Y = areaEnvelop.LowerLeft.Y;
            IPoint lowerRightPoint = new PointClass();
            lowerRightPoint.X = areaEnvelop.LowerRight.X;
            lowerRightPoint.Y = areaEnvelop.LowerRight.Y;
            IPoint upperLeftPoint = new PointClass();
            upperLeftPoint.X = lowerLeftPoint.X;
            upperLeftPoint.Y = lowerLeftPoint.Y + commonHouse.landHeight * 2 + village.roadWidth;
            IPoint upperRightPoint = new PointClass();
            upperRightPoint.X = lowerRightPoint.X;
            upperRightPoint.Y = lowerRightPoint.Y + commonHouse.landHeight * 2 + village.roadWidth;

            while (true)
            {
                if (upperLeftPoint.Y > areaEnvelop.UpperLeft.Y)
                {
                    break;
                }

                //截路那排.
                IPoint startPoint = new PointClass();
                IPoint endPoint = new PointClass();
                startPoint.X = lowerLeftPoint.X;
                startPoint.Y = lowerLeftPoint.Y + village.roadWidth;
                endPoint.X = lowerRightPoint.X;
                endPoint.Y = lowerRightPoint.Y + village.roadWidth;

                ring = new RingClass();
                ring.AddPoint(lowerLeftPoint);
                ring.AddPoint(startPoint);
                ring.AddPoint(endPoint);
                ring.AddPoint(lowerRightPoint);

                IPolygon roadArea = GisTool.MakePolygonFromRing(ring); //划出一条路的空间.
                ITopologicalOperator roadAreaTpOp = roadArea as ITopologicalOperator;
                roadArea = roadAreaTpOp.Intersect(area, esriGeometryDimension.esriGeometry2Dimension) as IPolygon;
                roadRowList.Add(roadArea);

                //截第一排房子.
                lowerLeftPoint.Y = startPoint.Y;
                lowerRightPoint.Y = endPoint.Y;
                startPoint.Y += commonHouse.landHeight;
                endPoint.Y += commonHouse.landHeight;

                ring = new RingClass();
                ring.AddPoint(lowerLeftPoint);
                ring.AddPoint(startPoint);
                ring.AddPoint(endPoint);
                ring.AddPoint(lowerRightPoint);

                IPolygon houseArea = GisTool.MakePolygonFromRing(ring);
                houseRowList.Add(houseArea);

                //截第二排房子.
                ring = new RingClass();
                ring.AddPoint(startPoint);
                ring.AddPoint(upperLeftPoint);
                ring.AddPoint(upperRightPoint);
                ring.AddPoint(endPoint);

                houseArea = GisTool.MakePolygonFromRing(ring);
                houseRowList.Add(houseArea);

                lowerLeftPoint.Y = upperLeftPoint.Y;
                lowerRightPoint.Y = upperRightPoint.Y;
                upperLeftPoint.Y = lowerLeftPoint.Y + commonHouse.landHeight * 2 + village.roadWidth;
                upperRightPoint.Y = lowerRightPoint.Y + commonHouse.landHeight * 2 + village.roadWidth;
            }

            rowList.Add(houseRowList);
            rowList.Add(roadRowList);

            return rowList;
        }

        private List<IPolyline> findNorthRoadList(IPolygon area)
        {
            int maxRoadCount = 2; //最多有两条边能作为南北路.
            int maxMoveNodeCount = 2; //最多尝试移动两次节点位置.
            List<IPoint> pointList = GisTool.getIPointListFromIPolygon(area);
            if (pointList[0].X == pointList[pointList.Count - 1].X && pointList[0].Y == pointList[pointList.Count - 1].Y)
            {
                //获取到的点集中, 头尾可能是一个点, 需要删除.
                pointList.RemoveAt(pointList.Count - 1);
            }

            //1. 找最左点.
            double xMin = pointList[0].X;
            int xMinIndex = 0; //最左点的下标.
            for (int i = 0; i < pointList.Count; i++)
            {
                if (pointList[i].X < xMin)
                {
                    xMin = pointList[i].X;
                    xMinIndex = i;
                }
            }

            //2. 确定顺时针,逆时针方向.
            bool clockWise = true;
            int minLittle, minBigger;
            if (xMinIndex - 1 < 0)
            {
                minLittle = pointList.Count - 1;
            }
            else
            {
                minLittle = xMinIndex - 1;
            }
            if (xMinIndex + 1 > pointList.Count - 1)
            {
                minBigger = 0;
            }
            else
            {
                minBigger = xMinIndex + 1;
            }
            if (pointList[minLittle].Y > pointList[minBigger].Y)
            {
                clockWise = false;
            }

            //3. 开始找路.
            int nowIndex = xMinIndex;
            IPoint nowPoint = pointList[nowIndex];
            IPoint nextPoint = new PointClass();
            List<IPolyline> validRoadList = new List<IPolyline>(); //符合南北向的路的路集合.

            //3. 开始算北边的路.
            while (true)
            {
                //节点移动尝试次数用尽之后, 或者已经找到两条南北向的路时, 就跳出返回.
                if (maxMoveNodeCount == 0 || validRoadList.Count >= maxRoadCount)
                {
                    break;
                }

                if (clockWise)
                {
                    if (++nowIndex > pointList.Count - 1)
                    {
                        nowIndex = 0;
                    }
                    nextPoint.X = pointList[nowIndex].X;
                    nextPoint.Y = pointList[nowIndex].Y;
                }
                else
                {
                    if (--nowIndex < 0)
                    {
                        nowIndex = pointList.Count - 1;
                    }
                    nextPoint.X = pointList[nowIndex].X;
                    nextPoint.Y = pointList[nowIndex].Y;
                }

                if (verifyRoad(nowPoint, nextPoint))
                {
                    validRoadList.Add(GisTool.makePolyline(nowPoint, nextPoint));
                }
                else
                {
                    //如果之前成功的找到了一条. 那么再失败就直接退出.
                    if (validRoadList.Count > 0)
                    {
                        break;
                    }
                    maxMoveNodeCount--;
                }
                nowPoint.X = nextPoint.X;
                nowPoint.Y = nextPoint.Y;
            }

            return validRoadList;
        }

        private List<IPolyline> findSouthRoadList(IPolygon area)
        {
            int maxRoadCount = 2; //最多有两条边能作为南北路.
            int maxMoveNodeCount = 2; //最多尝试移动两次节点位置.
            List<IPolyline> southRoadList = new List<IPolyline>();
            List<IPoint> pointList = GisTool.getIPointListFromIPolygon(area);
            if (pointList[0].X == pointList[pointList.Count - 1].X && pointList[0].Y == pointList[pointList.Count - 1].Y)
            {
                //获取到的点集中, 头尾可能是一个点, 需要删除.
                pointList.RemoveAt(pointList.Count - 1);
            }

            //1. 找最左点.
            double xMin = pointList[0].X;
            int xMinIndex = 0; //最左点的下标.
            for (int i = 0; i < pointList.Count; i++)
            {
                if (pointList[i].X < xMin)
                {
                    xMin = pointList[i].X;
                    xMinIndex = i;
                }
            }

            //2. 确定顺时针,逆时针方向.
            bool clockWise = true;
            int minLittle, minBigger;
            if (xMinIndex - 1 < 0)
            {
                minLittle = pointList.Count - 1;
            }
            else
            {
                minLittle = xMinIndex - 1;
            }
            if (xMinIndex + 1 > pointList.Count - 1)
            {
                minBigger = 0;
            }
            else
            {
                minBigger = xMinIndex + 1;
            }
            if (pointList[minLittle].Y > pointList[minBigger].Y)
            {
                clockWise = false;
            }

            //3. 开始找路.
            int nowIndex = xMinIndex;
            IPoint nowPoint = pointList[nowIndex];
            IPoint nextPoint = new PointClass();
            List<IPolyline> validRoadList = new List<IPolyline>(); //符合南北向的路的路集合.

            //3. 开始算南边的路.
            while (true)
            {
                //节点移动尝试次数用尽之后, 或者已经找到两条南北向的路时, 就跳出返回.
                if (maxMoveNodeCount == 0 || validRoadList.Count >= maxRoadCount)
                {
                    break;
                }

                if (clockWise)
                {
                    if (--nowIndex < 0)
                    {
                        nowIndex = pointList.Count - 1;
                    }
                    nextPoint.X = pointList[nowIndex].X;
                    nextPoint.Y = pointList[nowIndex].Y;
                }
                else
                {
                    if (++nowIndex > pointList.Count - 1)
                    {
                        nowIndex = 0;
                    }
                    nextPoint.X = pointList[nowIndex].X;
                    nextPoint.Y = pointList[nowIndex].Y;
                }

                if (verifyRoad(nowPoint, nextPoint))
                {
                    validRoadList.Add(GisTool.makePolyline(nowPoint, nextPoint));
                }
                else
                {
                    //如果之前成功的找到了一条. 那么再失败就直接退出.
                    if (validRoadList.Count > 0)
                    {
                        break;
                    }
                    maxMoveNodeCount--;
                }
                nowPoint.X = nextPoint.X;
                nowPoint.Y = nextPoint.Y;
            }

            return validRoadList;
        }

        //验证路是否南北向.
        private bool verifyRoad(IPoint startPoint, IPoint endPoint)
        {
            double alpha = Math.Atan((endPoint.Y - startPoint.Y) / (endPoint.X - startPoint.X));
            if (startPoint.X < endPoint.X && alpha < Math.PI / 4 && alpha > Math.PI / 4 * -1)
            {
                //这条直线符合南北向的条件.
                return true;
            }
            else
            {
                return false;
            }
        }

        private List<IGeometry> cut()
        {
            List<IGeometry> areaList = new List<IGeometry>();
            IPolygon areaPolygon = (village.polygonElement as IElement).Geometry as IPolygon;
            ITopologicalOperator areaTpOp = areaPolygon as ITopologicalOperator;
            areaTpOp.Simplify(); //有效防止cut报错.
            IPolyline innerRoadPolyline = (village.innerRoad.lineElement as IElement).Geometry as IPolyline;
            IPointCollection innerRoadPointCollection = innerRoadPolyline as IPointCollection;
            IPoint startPoint = innerRoadPointCollection.get_Point(0);
            IPoint endPoint = innerRoadPointCollection.get_Point(1);
            IGeometry leftArea, rightArea; //area1是左上区域，area2是右下区域。

            areaTpOp.Cut(innerRoadPolyline, out leftArea, out rightArea);

            //再切掉内部路占用的宽度。
            List<IPolyline> extendedInnerRoadList = extendRoad((village.innerRoad.lineElement as IElement).Geometry as IPolyline,
                village.innerRoad.width / 2); //第一条是左边, 第二条是右边.
            extendedInnerRoadList[0] = prolongRoad(extendedInnerRoadList[0], leftArea as IPolygon);
            extendedInnerRoadList[1] = prolongRoad(extendedInnerRoadList[1], rightArea as IPolygon);
            
            IGeometry tempGeometry;
            if (extendedInnerRoadList[0] == null)
            {
                leftArea = null;
            }
            else
            {
                ITopologicalOperator leftAreaTpOp = leftArea as ITopologicalOperator;
                leftAreaTpOp.Simplify();
                leftAreaTpOp.Cut(extendedInnerRoadList[0], out leftArea, out tempGeometry);
            }
            if (extendedInnerRoadList[1] == null)
            {
                rightArea = null;
            }
            else
            {
                ITopologicalOperator rightAreaTpOp = rightArea as ITopologicalOperator;
                rightAreaTpOp.Simplify();
                rightAreaTpOp.Cut(extendedInnerRoadList[1], out tempGeometry, out rightArea);
            }

            areaList.Add(leftArea);
            areaList.Add(rightArea);
            Ring ring = new RingClass();
            IPointCollection extendedLeftInnerRoadPointCollection = extendedInnerRoadList[0] as IPointCollection;
            ring.AddPoint(extendedLeftInnerRoadPointCollection.get_Point(0));
            ring.AddPoint(extendedLeftInnerRoadPointCollection.get_Point(1));
            IPointCollection extendedRightInnerRoadPointCollection = extendedInnerRoadList[1] as IPointCollection;
            ring.AddPoint(extendedRightInnerRoadPointCollection.get_Point(1));
            ring.AddPoint(extendedRightInnerRoadPointCollection.get_Point(0));
            IPolygon innerRoadPolygon = GisTool.MakePolygonFromRing(ring);
            areaList.Add(innerRoadPolygon);

            return areaList;
        }

        private IPolyline prolongRoad(IPolyline road, IPolygon area)
        { 
            //沿线方向进行延长, 直到和面相交, 然后只留下相交部分.
            double distance = 1000; //每次延长距离.
            double multiplier = 2; //每次失败, 就把distance乘以multiplier.
            double maxTimes = 10;
            double nowTimes = 0;
            ITopologicalOperator areaTpOp = area as ITopologicalOperator;
            IPointCollection roadPointCollection = road as IPointCollection;
            IPoint startPoint = roadPointCollection.get_Point(0);
            IPoint endPoint = roadPointCollection.get_Point(1);
            IPolyline tempLine = new PolylineClass();
            IPointCollection tempPointCollection = tempLine as IPointCollection;
            IPoint tempPoint1 = new PointClass();
            IPoint tempPoint2 = new PointClass();

            //先解决竖直和水平的两种情况.
            if (startPoint.X == endPoint.X)
            {
                //竖直情况.
                if (startPoint.Y < endPoint.Y)
                {
                    while (nowTimes < maxTimes)
                    {
                        tempPoint1.X = startPoint.X;
                        tempPoint1.Y = startPoint.Y - distance;
                        tempPoint2.X = endPoint.X;
                        tempPoint2.Y = endPoint.Y + distance;
                        tempLine = GisTool.makePolyline(tempPoint1, tempPoint2);
                        IRelationalOperator tempReOp = tempLine as IRelationalOperator;
                        if (tempReOp.Crosses(area))
                        {
                            return areaTpOp.Intersect(tempLine, esriGeometryDimension.esriGeometry1Dimension) as IPolyline;
                        }
                        nowTimes++;
                        distance *= multiplier;
                    }
                }
                else
                {
                    while (nowTimes < maxTimes)
                    {
                        tempPoint1.X = startPoint.X;
                        tempPoint1.Y = startPoint.Y + distance;
                        tempPoint2.X = endPoint.X;
                        tempPoint2.Y = endPoint.Y - distance;
                        tempLine = GisTool.makePolyline(tempPoint1, tempPoint2);
                        IRelationalOperator tempReOp = tempLine as IRelationalOperator;
                        if (tempReOp.Crosses(area))
                        {
                            return areaTpOp.Intersect(tempLine, esriGeometryDimension.esriGeometry1Dimension) as IPolyline;
                        }
                        nowTimes++;
                        distance *= multiplier;
                    }
                }
            }
            else if (startPoint.Y == endPoint.Y)
            {
                //水平情况.
                if (startPoint.X < endPoint.X)
                {
                    while (nowTimes < maxTimes)
                    {
                        tempPoint1.X = startPoint.X - distance;
                        tempPoint1.Y = startPoint.Y;
                        tempPoint2.X = endPoint.X + distance;
                        tempPoint2.Y = endPoint.Y;
                        tempLine = GisTool.makePolyline(tempPoint1, tempPoint2);
                        IRelationalOperator tempReOp = tempLine as IRelationalOperator;
                        if (tempReOp.Crosses(area))
                        {
                            return areaTpOp.Intersect(tempLine, esriGeometryDimension.esriGeometry1Dimension) as IPolyline;
                        }
                        nowTimes++;
                        distance *= multiplier;
                    }
                }
                else
                {
                    while (nowTimes < maxTimes)
                    {
                        tempPoint1.X = startPoint.X + distance;
                        tempPoint1.Y = startPoint.Y;
                        tempPoint2.X = endPoint.X - distance;
                        tempPoint2.Y = endPoint.Y;
                        tempLine = GisTool.makePolyline(tempPoint1, tempPoint2);
                        IRelationalOperator tempReOp = tempLine as IRelationalOperator;
                        if (tempReOp.Crosses(area))
                        {
                            return areaTpOp.Intersect(tempLine, esriGeometryDimension.esriGeometry1Dimension) as IPolyline;
                        }
                        nowTimes++;
                        distance *= multiplier;
                    }
                }
            }
            else
            {
                //再解决两个斜线情况. 算角度. cos值, sin值.
                double alpha = Math.Atan((endPoint.Y - startPoint.Y) / (endPoint.X - startPoint.X));
                double sinA = Math.Sin(alpha);
                double cosA = Math.Cos(alpha);

                if (startPoint.X < endPoint.X && startPoint.Y < endPoint.Y)
                {
                    /*
                     *   _
                     *   /|
                     *  /
                     * /
                     *-------------- 
                     */
                    while (nowTimes < maxTimes)
                    {
                        double dx = Math.Abs(cosA * distance);
                        double dy = Math.Abs(sinA * distance);
                        tempPoint1.X = startPoint.X - dx;
                        tempPoint1.Y = startPoint.Y - dy;
                        tempPoint2.X = endPoint.X + dx;
                        tempPoint2.Y = endPoint.Y + dy;
                        tempLine = GisTool.makePolyline(tempPoint1, tempPoint2);
                        IRelationalOperator tempReOp = tempLine as IRelationalOperator;
                        if (tempReOp.Crosses(area))
                        {
                            return areaTpOp.Intersect(tempLine, esriGeometryDimension.esriGeometry1Dimension) as IPolyline;
                        }
                        nowTimes++;
                        distance *= multiplier;
                    }
                }
                else if (startPoint.X < endPoint.X && startPoint.Y > endPoint.Y)
                {
                    /*
                     *------------
                     *\
                     * \
                     *  \|
                     *  -
                     */
                    while (nowTimes < maxTimes)
                    {
                        double dx = Math.Abs(cosA * distance);
                        double dy = Math.Abs(sinA * distance);
                        tempPoint1.X = startPoint.X - dx;
                        tempPoint1.Y = startPoint.Y + dy;
                        tempPoint2.X = endPoint.X + dx;
                        tempPoint2.Y = endPoint.Y - dy;
                        tempLine = GisTool.makePolyline(tempPoint1, tempPoint2);
                        IRelationalOperator tempReOp = tempLine as IRelationalOperator;
                        if (tempReOp.Crosses(area))
                        {
                            return areaTpOp.Intersect(tempLine, esriGeometryDimension.esriGeometry1Dimension) as IPolyline;
                        }
                        nowTimes++;
                        distance *= multiplier;
                    }
                }
                else if (startPoint.X > endPoint.X && startPoint.Y < endPoint.Y)
                {
                    /*
                     * _
                     *|\
                     *  \
                     *   \
                     *    ----------   
                     */
                    while (nowTimes < maxTimes)
                    {
                        double dx = Math.Abs(cosA * distance);
                        double dy = Math.Abs(sinA * distance);
                        tempPoint1.X = startPoint.X + dx;
                        tempPoint1.Y = startPoint.Y - dy;
                        tempPoint2.X = endPoint.X - dx;
                        tempPoint2.Y = endPoint.Y + dy;
                        tempLine = GisTool.makePolyline(tempPoint1, tempPoint2);
                        IRelationalOperator tempReOp = tempLine as IRelationalOperator;
                        if (tempReOp.Crosses(area))
                        {
                            return areaTpOp.Intersect(tempLine, esriGeometryDimension.esriGeometry1Dimension) as IPolyline;
                        }
                        nowTimes++;
                        distance *= multiplier;
                    }
                }
                else
                {
                    /*
                     *    ----------
                     *   /
                     *  /
                     *|/
                     * -
                     */
                    while (nowTimes < maxTimes)
                    {
                        double dx = Math.Abs(cosA * distance);
                        double dy = Math.Abs(sinA * distance);
                        tempPoint1.X = startPoint.X + dx;
                        tempPoint1.Y = startPoint.Y + dy;
                        tempPoint2.X = endPoint.X - dx;
                        tempPoint2.Y = endPoint.Y - dy;
                        tempLine = GisTool.makePolyline(tempPoint1, tempPoint2);
                        IRelationalOperator tempReOp = tempLine as IRelationalOperator;
                        if (tempReOp.Crosses(area))
                        {
                            return areaTpOp.Intersect(tempLine, esriGeometryDimension.esriGeometry1Dimension) as IPolyline;
                        }
                        nowTimes++;
                        distance *= multiplier;
                    }
                }
            }

            return null;
        }

        private List<IPolyline> extendRoad(IPolyline roadPolyline, double roadWidth)
        {
            List<IPolyline> extendedInnerRoadList = new List<IPolyline>();

            IPointCollection roadPointCollection = roadPolyline as IPointCollection;
            IPoint startPoint = roadPointCollection.get_Point(0);
            IPoint endPoint = roadPointCollection.get_Point(1);
            IPoint extendedStartPoint1 = new PointClass();
            IPoint extendedEndPoint1 = new PointClass();
            IPoint extendedStartPoint2 = new PointClass();
            IPoint extendedEndPoint2 = new PointClass();

            //先解决竖直和水平的两种情况.
            if (startPoint.X == endPoint.X)
            { 
                //竖直情况.
                extendedStartPoint1.X = startPoint.X - roadWidth;
                extendedStartPoint1.Y = startPoint.Y;
                extendedEndPoint1.X = endPoint.X - roadWidth;
                extendedEndPoint1.Y = endPoint.Y;

                extendedStartPoint2.X = startPoint.X + roadWidth;
                extendedStartPoint2.Y = startPoint.Y;
                extendedEndPoint2.X = endPoint.X + roadWidth;
                extendedEndPoint2.Y = endPoint.Y;

                if(startPoint.Y < endPoint.Y)
                {
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint1, extendedEndPoint1));
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint2, extendedEndPoint2));
                }
                else
                {
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint2, extendedEndPoint2));
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint1, extendedEndPoint1));
                }
                
                return extendedInnerRoadList;
            }
            else if (startPoint.Y == endPoint.Y)
            {
                //水平情况.
                extendedStartPoint1.X = startPoint.X;
                extendedStartPoint1.Y = startPoint.Y + roadWidth;
                extendedEndPoint1.X = endPoint.X;
                extendedEndPoint1.Y = endPoint.Y + roadWidth;

                extendedStartPoint2.X = startPoint.X;
                extendedStartPoint2.Y = startPoint.Y - roadWidth;
                extendedEndPoint2.X = endPoint.X;
                extendedEndPoint2.Y = endPoint.Y - roadWidth;
                
                if(startPoint.X < endPoint.X)
                {
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint1, extendedEndPoint1));
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint2, extendedEndPoint2));
                }
                else
                {
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint2, extendedEndPoint2));
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint1, extendedEndPoint1));
                }
                
                return extendedInnerRoadList;
            }
            else
            {
                //再解决两个斜线情况. 算角度. cos值, sin值.
                double alpha = Math.Atan((endPoint.Y - startPoint.Y) / (endPoint.X - startPoint.X));
                double sinA = Math.Sin(alpha);
                double cosA = Math.Cos(alpha);
                double dx = Math.Abs(sinA * roadWidth);
                double dy = Math.Abs(cosA * roadWidth);

                if (startPoint.X < endPoint.X && startPoint.Y < endPoint.Y)
                {
                    /*
                     *   _
                     *   /|
                     *  /
                     * /
                     *-------------- 
                     */
                    extendedStartPoint1.X = startPoint.X - dx;
                    extendedStartPoint1.Y = startPoint.Y + dy;
                    extendedEndPoint1.X = endPoint.X - dx;
                    extendedEndPoint1.Y = endPoint.Y + dy;

                    extendedStartPoint2.X = startPoint.X + dx;
                    extendedStartPoint2.Y = startPoint.Y - dy;
                    extendedEndPoint2.X = endPoint.X + dx;
                    extendedEndPoint2.Y = endPoint.Y - dy;

                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint1, extendedEndPoint1));
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint2, extendedEndPoint2));
                    return extendedInnerRoadList;
                }
                else if(startPoint.X < endPoint.X && startPoint.Y > endPoint.Y)
                {
                    /*
                     *------------
                     *\
                     * \
                     *  \|
                     *  -
                     */
                    extendedStartPoint1.X = startPoint.X + dx;
                    extendedStartPoint1.Y = startPoint.Y + dy;
                    extendedEndPoint1.X = endPoint.X + dx;
                    extendedEndPoint1.Y = endPoint.Y + dy;

                    extendedStartPoint2.X = startPoint.X - dx;
                    extendedStartPoint2.Y = startPoint.Y - dy;
                    extendedEndPoint2.X = endPoint.X - dx;
                    extendedEndPoint2.Y = endPoint.Y - dy;

                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint1, extendedEndPoint1));
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint2, extendedEndPoint2));
                    return extendedInnerRoadList;
                }
                else if (startPoint.X > endPoint.X && startPoint.Y < endPoint.Y)
                {
                    /*
                     * _
                     *|\
                     *  \
                     *   \
                     *    ----------   
                     */
                    extendedStartPoint1.X = startPoint.X - dx;
                    extendedStartPoint1.Y = startPoint.Y - dy;
                    extendedEndPoint1.X = endPoint.X - dx;
                    extendedEndPoint1.Y = endPoint.Y - dy;

                    extendedStartPoint2.X = startPoint.X + dx;
                    extendedStartPoint2.Y = startPoint.Y + dy;
                    extendedEndPoint2.X = endPoint.X + dx;
                    extendedEndPoint2.Y = endPoint.Y + dy;

                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint1, extendedEndPoint1));
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint2, extendedEndPoint2));
                    return extendedInnerRoadList;
                }
                else
                {
                    /*
                     *    ----------
                     *   /
                     *  /
                     *|/
                     * -
                     */
                    extendedStartPoint1.X = startPoint.X + dx;
                    extendedStartPoint1.Y = startPoint.Y - dy;
                    extendedEndPoint1.X = endPoint.X + dx;
                    extendedEndPoint1.Y = endPoint.Y - dy;

                    extendedStartPoint2.X = startPoint.X - dx;
                    extendedStartPoint2.Y = startPoint.Y + dy;
                    extendedEndPoint2.X = endPoint.X - dx;
                    extendedEndPoint2.Y = endPoint.Y + dy;

                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint1, extendedEndPoint1));
                    extendedInnerRoadList.Add(GisTool.makePolyline(extendedStartPoint2, extendedEndPoint2));
                    return extendedInnerRoadList;
                }
            }
        }
    }
}
