using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using System.Windows;

namespace Intersect
{
    class roadStripedRow
    {
        public double rowHeight;
        public double rowWidth;
        public double rotateAngle;
        public IGeometry row;
        public IGeometry stripedRow;
        public IPoint lowerLeftPt;
        public IPoint lowerRightPt;
        public IPoint upperLeftPt;
        public IPoint upperRightPt;
        public roadStripedRow(IGeometry r, double height)
        {
            row = r;
            rowHeight = height;
        }

        private double GetAngle(IPolyline pPolyline)
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

        private IGeometry RotateLine(IGeometry geom, double newAngle, double oldAngle, bool isLine = true)
        {
            IPoint centerPoint;
            IElement elem;
            if (isLine)
            {
                IPointCollection ptCol = geom as IPointCollection;
                centerPoint = ptCol.get_Point(0);
                elem = new LineElement();
            }
            else
            {
                centerPoint = new PointClass
                {
                    X = (geom.Envelope.LowerLeft.X + geom.Envelope.LowerRight.X) / 2,
                    Y = (geom.Envelope.LowerLeft.Y + geom.Envelope.UpperRight.Y) / 2
                };
                elem = new PolygonElement();
            }

            elem.Geometry = geom;
            ITransform2D trans = elem as ITransform2D;

            double angle = newAngle - oldAngle;
            if ((newAngle - oldAngle) > Math.PI / 2)
            {
                angle += Math.PI;
            }
            if ((newAngle - oldAngle) < -1 * Math.PI / 2)
            {
                angle -= Math.PI;
            }

            trans.Rotate(centerPoint, angle);
            return elem.Geometry;
        }

        private IGeometry MakePolygonFromPointsList(List<IPoint> ptsList)
        {
            Ring ring = new RingClass();
            object missing = Type.Missing;
            for (int i = 0; i < ptsList.Count; i++)
            {
                ring.AddPoint(ptsList[i]);
            }
            IGeometryCollection pointPolygon = new PolygonClass();
            pointPolygon.AddGeometry(ring as IGeometry, ref missing, ref missing);
            IPolygon polygon = pointPolygon as IPolygon;
            polygon.SimplifyPreserveFromTo();
            return polygon;
        }

        public void formatRow()
        {
            IPolyline roadLine = new PolylineClass();
            IPolyline topLine = new PolylineClass();
            IPolyline horizontalLine = new PolylineClass();
            IPolyline leftBoundLine = new PolylineClass();
            IPolyline rightBoundLine = new PolylineClass();
            IPolygon formatedRoadArea;
            ITopologicalOperator tpOp = row as ITopologicalOperator;
            IPointCollection ptCol = tpOp.Boundary as IPointCollection;
            IPoint urPt, ulPt, lrPt, llPt, tempPt;
            tempPt = new PointClass();

            List<IPoint> leftPtList = new List<IPoint>()
                , rightPtList = new List<IPoint>();
            for (int i = 0; i < ptCol.PointCount - 1; i++)
            {
                tempPt = ptCol.get_Point(i);
                for (int j = 0; j < ptCol.PointCount - 1; j++)
                {
                    if (i == j)
                    {
                        continue;
                    }
                    if (ptCol.get_Point(j).X < tempPt.X)
                    {
                        rightPtList.Add(tempPt);
                        break;
                    }
                    if (ptCol.get_Point(j).X > tempPt.X)
                    {
                        leftPtList.Add(tempPt);
                        break;
                    }
                }
            }

            if (leftPtList[0].Y < leftPtList[1].Y)
            {
                llPt = leftPtList[0];
                ulPt = leftPtList[1];
            }
            else
            {
                ulPt = leftPtList[0];
                llPt = leftPtList[1];
            }
            if (rightPtList[0].Y < rightPtList[1].Y)
            {
                lrPt = rightPtList[0];
                urPt = rightPtList[1];
            }
            else
            {
                urPt = rightPtList[0];
                lrPt = rightPtList[1];
            }
            ptCol = horizontalLine as IPointCollection;
            ptCol.AddPoint(llPt);
            tempPt = new PointClass();
            tempPt.X = llPt.X + 10;
            tempPt.Y = llPt.Y;
            ptCol.AddPoint(tempPt);
            double oldAngle = GetAngle(horizontalLine);
            ptCol = roadLine as IPointCollection;
            ptCol.AddPoint(llPt);
            ptCol.AddPoint(lrPt);
            ptCol = topLine as IPointCollection;
            ptCol.AddPoint(ulPt);
            ptCol.AddPoint(urPt);
            double newAngle = GetAngle(roadLine);
            rotateAngle = newAngle - oldAngle;

            ptCol = leftBoundLine as IPointCollection;
            ptCol.AddPoint(ulPt);
            ptCol.AddPoint(llPt);
            leftBoundLine = RotateLine(leftBoundLine, newAngle, oldAngle) as IPolyline;

            ptCol = rightBoundLine as IPointCollection;
            ptCol.AddPoint(lrPt);
            ptCol.AddPoint(urPt);
            rightBoundLine = RotateLine(rightBoundLine, newAngle, oldAngle) as IPolyline;

            tpOp = roadLine as ITopologicalOperator;
            IGeometry geom = tpOp.Intersect(leftBoundLine, esriGeometryDimension.esriGeometry0Dimension);
            ptCol = geom as IPointCollection;
            llPt = ptCol.get_Point(0);
            tpOp = topLine as ITopologicalOperator;
            geom = tpOp.Intersect(rightBoundLine, esriGeometryDimension.esriGeometry0Dimension);
            ptCol = geom as IPointCollection;
            urPt = ptCol.get_Point(0);
            formatedRoadArea = MakePolygonFromPointsList(new List<IPoint>() { ulPt, urPt, lrPt, llPt }) as IPolygon;
            stripedRow = formatedRoadArea;

            rowWidth = Math.Pow((llPt.X - lrPt.X) * (llPt.X - lrPt.X) + (llPt.Y - lrPt.Y) * (llPt.Y - lrPt.Y), 0.5);
            lowerLeftPt = llPt;
            lowerRightPt = lrPt;
            upperLeftPt = ulPt;
            upperRightPt = urPt;
        }
    }
}
