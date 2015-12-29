//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using ESRI.ArcGIS.Geometry;
//using ESRI.ArcGIS.Controls;
//using ESRI.ArcGIS.Geodatabase;
//using ESRI.ArcGIS.Carto;
//using System.Windows;
//using System.Collections;

//namespace Intersect
//{
//    class HouseManager
//    {
//        public IPoint lowerLeftPt;
//        public IPoint lowerRightPt;
//        public IPoint upperLeftPt;
//        public IPoint upperRightPt;
//        public IPoint southDirctionPt;
//        public House house;
//        public CommonHouse commonHouse;
//        public double rotateAngle;
//        public double totalHouseWidth
//        {
//            get
//            {
//                return house.width;
//            }
//            set { }
//        }

//        public HouseManager(IPoint llPt, House h, CommonHouse ch)
//        {
//            //四个角的点是外面圈的四个顶点, 因为在进行计算时, 一直以外圈为准.
//            lowerLeftPt = new PointClass();
//            lowerLeftPt.X = llPt.X;
//            lowerLeftPt.Y = llPt.Y;
//            house = h;
//            commonHouse = ch;

//            lowerRightPt = new PointClass();
//            lowerRightPt.X = lowerLeftPt.X + house.width;
//            lowerRightPt.Y = lowerLeftPt.Y;

//            upperLeftPt = new PointClass();
//            upperLeftPt.X = lowerLeftPt.X;
//            upperLeftPt.Y = lowerLeftPt.Y + commonHouse.height + commonHouse.backGap;

//            upperRightPt = new PointClass();
//            upperRightPt.X = lowerRightPt.X;
//            upperRightPt.Y = upperLeftPt.Y;

//            southDirctionPt = new PointClass();
//            southDirctionPt.X = (lowerLeftPt.X + lowerRightPt.X) / 2;
//            southDirctionPt.Y = lowerLeftPt.Y;

//            rotateAngle = 0;
//        }

//        public void move(IPoint newPt)
//        {
//            double dx = newPt.X - lowerLeftPt.X;
//            double dy = newPt.Y - lowerLeftPt.Y;

//            lowerLeftPt = MovePoint(lowerLeftPt, dx, dy);
//            lowerRightPt = MovePoint(lowerRightPt, dx, dy);
//            upperLeftPt = MovePoint(upperLeftPt, dx, dy);
//            upperRightPt = MovePoint(upperRightPt, dx, dy);
//            southDirctionPt = MovePoint(southDirctionPt, dx, dy);
//        }

//        public void rotate(IPoint basePt, double angle)
//        {
//            /*
//                转向所用的两个点, 一个使用参数, 另一个使用mid的中心点.
//                要注意, 只有outer和inner需要旋转, inner需要旋转后再移动调整位置, 五个记录信息的点不用旋转, 只需要移动相应的位置即可. 
//            */
//            List<IPoint> ptList = new List<IPoint>() { upperLeftPt, upperRightPt, lowerLeftPt, lowerRightPt, southDirctionPt };
//            for (int i = 0; i < ptList.Count; i++)
//            {
//                if (basePt.X != ptList[i].X || basePt.Y != ptList[i].Y)
//                {
//                    ptList[i] = rotateCornerPoint(basePt, ptList[i], angle);
//                }
//            }
//            rotateAngle += angle;
//        }

//        private IPoint rotateCornerPoint(IPoint basePt, IPoint targetPt, double angle)
//        {
//            IPolyline line = new PolylineClass();
//            IPointCollection ptCol = line as IPointCollection;
//            ptCol.AddPoint(basePt);
//            ptCol.AddPoint(targetPt);

//            line = RotateGeom(line, angle, true) as IPolyline;

//            IPoint afterMovePt = (line as IPointCollection).get_Point(1);
//            targetPt = MovePoint(targetPt, afterMovePt.X - targetPt.X, afterMovePt.Y - targetPt.Y);

//            return targetPt;
//        }

//        private IGeometry RotateGeom(IGeometry geom, double angle, bool isLine = false)
//        {
//            IPoint centerPoint;
//            IElement elem;
//            if (isLine)
//            {
//                IPointCollection ptCol = geom as IPointCollection;
//                centerPoint = ptCol.get_Point(0);
//                elem = new LineElement();
//            }
//            else
//            {
//                centerPoint = new PointClass
//                {
//                    X = (geom.Envelope.LowerLeft.X + geom.Envelope.LowerRight.X) / 2,
//                    Y = (geom.Envelope.LowerLeft.Y + geom.Envelope.UpperRight.Y) / 2
//                };
//                elem = new PolygonElement();
//            }

//            elem.Geometry = geom;
//            ITransform2D trans = elem as ITransform2D;

//            if (angle > Math.PI / 2)
//            {
//                angle += Math.PI;
//            }
//            if (angle < -1 * Math.PI / 2)
//            {
//                angle -= Math.PI;
//            }

//            trans.Rotate(centerPoint, angle);
//            return elem.Geometry;
//        }

//        public bool isIntersect(IPolyline line)
//        {
//            //需要做四次相交, 分别是和四个边线.
//            //左边边线.
//            IPolyline boundLine = new PolylineClass();
//            IPointCollection ptCol = boundLine as IPointCollection;
//            ptCol.AddPoint(upperLeftPt);
//            ptCol.AddPoint(lowerLeftPt);
//            ITopologicalOperator tpOp = boundLine as ITopologicalOperator;
//            IGeometry geom = tpOp.Intersect(line, esriGeometryDimension.esriGeometry0Dimension);
//            if (!geom.IsEmpty)
//            {
//                return true;
//            }

//            //右边边线.
//            boundLine = new PolylineClass();
//            ptCol = boundLine as IPointCollection;
//            ptCol.AddPoint(upperRightPt);
//            ptCol.AddPoint(lowerRightPt);
//            tpOp = boundLine as ITopologicalOperator;
//            geom = tpOp.Intersect(line, esriGeometryDimension.esriGeometry0Dimension);
//            if (!geom.IsEmpty)
//            {
//                return true;
//            }

//            //上边边线.
//            boundLine = new PolylineClass();
//            ptCol = boundLine as IPointCollection;
//            ptCol.AddPoint(upperLeftPt);
//            ptCol.AddPoint(upperRightPt);
//            tpOp = boundLine as ITopologicalOperator;
//            geom = tpOp.Intersect(line, esriGeometryDimension.esriGeometry0Dimension);
//            if (!geom.IsEmpty)
//            {
//                return true;
//            }

//            //下边边线.
//            boundLine = new PolylineClass();
//            ptCol = boundLine as IPointCollection;
//            ptCol.AddPoint(lowerLeftPt);
//            ptCol.AddPoint(lowerRightPt);
//            tpOp = boundLine as ITopologicalOperator;
//            geom = tpOp.Intersect(line, esriGeometryDimension.esriGeometry0Dimension);
//            if (!geom.IsEmpty)
//            {
//                return true;
//            }

//            return false;
//        }

//        public ArrayList makeHousePolygon()
//        {
//            IGeometry outerHousePolygon = MakeHouseOuterPolygon();
//            List<IGeometry> innerHousePolygonList = MakeHouseInnerPolygonList();
//            ArrayList houseList = new ArrayList { outerHousePolygon as IPolygon, innerHousePolygonList };
//            return houseList;
//        }

//        private IGeometry MakeHouseOuterPolygon()
//        {
//            return MakePolygonFromPointsList(new List<IPoint>() { upperLeftPt, upperRightPt, lowerRightPt, lowerLeftPt });
//        }

//        private List<IGeometry> MakeHouseInnerPolygonList()
//        {
//            List<IGeometry> geomList = new List<IGeometry>();
//            IPoint ulPt, urPt, llPt, lrPt, ctPt; //分别为左上角点, 右上角点, 左下角点, 右下角点, 正中心点.
//            ctPt = getCenterPt();
//            ulPt = new PointClass();
//            ulPt.X = ctPt.X - house.width / 2;
//            ulPt.Y = ctPt.Y + commonHouse.height / 2;
//            ulPt = RotateInnerPoint(ulPt);
//            llPt = new PointClass();
//            llPt.X = ctPt.X - house.width / 2;
//            llPt.Y = ctPt.Y - commonHouse.height / 2;
//            llPt = RotateInnerPoint(llPt);
//            double dx = house.width / house.unit * Math.Cos(rotateAngle);
//            double dy = house.width / house.unit * Math.Sin(rotateAngle);
//            urPt = new PointClass();
//            urPt.X = ulPt.X + dx;
//            urPt.Y = ulPt.Y + dy;
//            lrPt = new PointClass();
//            lrPt.X = llPt.X + dx;
//            lrPt.Y = llPt.Y + dy;
//            for (int i = 0; i < house.unit; i++)
//            {
//                urPt = new PointClass();
//                urPt.X = ulPt.X + dx;
//                urPt.Y = ulPt.Y + dy;
//                lrPt = new PointClass();
//                lrPt.X = llPt.X + dx;
//                lrPt.Y = llPt.Y + dy;
//                geomList.Add(MakePolygonFromPointsList(new List<IPoint>() { ulPt, urPt, lrPt, llPt }));
//                ulPt = new PointClass();
//                ulPt.X = urPt.X;
//                ulPt.Y = urPt.Y;
//                llPt = new PointClass();
//                llPt.X = lrPt.X;
//                llPt.Y = lrPt.Y;
//            }

//            return geomList;
//        }

//        private IPoint RotateInnerPoint(IPoint oldPt)
//        {
//            IPolyline line = new PolylineClass();
//            IPoint centerPt = getCenterPt();
//            IPoint newPt = new PointClass();
//            IPointCollection ptCol = line as IPointCollection;
//            ptCol.AddPoint(centerPt);
//            ptCol.AddPoint(oldPt);

//            line = RotateGeom(line, rotateAngle, true) as IPolyline;
//            newPt = (line as IPointCollection).get_Point(1);

//            return newPt;
//        }

//        private IGeometry MakePolygonFromPointsList(List<IPoint> ptsList)
//        {
//            Ring ring = new RingClass();
//            object missing = Type.Missing;
//            for (int i = 0; i < ptsList.Count; i++)
//            {
//                ring.AddPoint(ptsList[i]);
//            }
//            IGeometryCollection pointPolygon = new PolygonClass();
//            pointPolygon.AddGeometry(ring as IGeometry, ref missing, ref missing);
//            IPolygon polygon = pointPolygon as IPolygon;
//            polygon.SimplifyPreserveFromTo();
//            return polygon;
//        }

//        private IPoint MovePoint(IPoint pt, double dx, double dy)
//        {
//            pt.X += dx;
//            pt.Y += dy;

//            return pt;
//        }

//        private static double GetAngle(IPolyline pPolyline)
//        {
//            //IPolycurve pPolycurve;  
//            ILine pTangentLine = new ESRI.ArcGIS.Geometry.Line();
//            pPolyline.QueryTangent(esriSegmentExtension.esriNoExtension, 0.5, true, pPolyline.Length, pTangentLine);
//            Double radian = pTangentLine.Angle;
//            //Double angle = radian * 180 / Math.PI;  
//            //// 如果要设置正角度执行以下方法  
//            //while (angle < 0)  
//            //{  
//            //    angle = angle + 360;  
//            //}  
//            //// 返回角度  
//            //return angle;  

//            // 返回弧度  
//            return radian;
//        }

//        private IPoint getCenterPt()
//        {
//            IPolygon polygon = MakeHouseOuterPolygon() as IPolygon;
//            IEnvelope envelope = polygon.Envelope;
//            IPoint pt = new PointClass();
//            pt.X = (envelope.UpperLeft.X + envelope.UpperRight.X) / 2;
//            pt.Y = (envelope.UpperLeft.Y + envelope.LowerLeft.Y) / 2;
//            return pt;
//        }

//        private IGeometry MoveGeom(IGeometry geom, double dx, double dy)
//        {
//            IElement elem = new PolygonElement();
//            elem.Geometry = geom;
//            ITransform2D trans = elem as ITransform2D;
//            trans.Move(dx, dy);
//            return elem.Geometry;
//        }
//    }
//}
