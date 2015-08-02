using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Carto;
using System.Windows;
using ESRI.ArcGIS.Display;

namespace Intersect
{
    class houseGenerator
    {
        //通过宽度, 深度, 层数, 层高生成IGeometry.
        public houseGenerator(double w, double h, int f, double fh)
        {
            houseWidth = w;
            houseHeight = h;
            houseFloor = f;
            houseFloorHeight = fh;
        }

        private IPoint leftTopPt;
        private IPoint rightTopPt;
        private IPoint leftBottomPt;
        private IPoint rightBottomPt;

        private double houseWidth;
        private double houseHeight;
        private int houseFloor;
        private double houseFloorHeight;

        private int leftTopX = 10000;
        private int leftTopY = -10000;
        private double cornerSize = 0.1;

        private IPolygon GeneratePolygonFromRing(Ring ring)
        {
            IGeometryCollection pointPolygon = new PolygonClass();
            object missing = Type.Missing;  
            pointPolygon.AddGeometry(ring as IGeometry, ref missing, ref missing);
            IPolygon polyGonGeo = pointPolygon as IPolygon;
            polyGonGeo.SimplifyPreserveFromTo();

            return polyGonGeo;
        }

        public List<IGeometry> getHouseGeomList()
        {
            leftTopPt = new PointClass();
            leftTopPt.PutCoords(leftTopX, leftTopY);
            rightTopPt = new PointClass();
            rightTopPt.PutCoords(leftTopX+ houseWidth, leftTopY);
            leftBottomPt = new PointClass();
            leftBottomPt.PutCoords(leftTopX, leftTopY - houseHeight);
            rightBottomPt = new PointClass();
            rightBottomPt.PutCoords(leftTopX + houseWidth, leftTopY - houseHeight);

            //靠宽度, 深度定下四个点. 画出房子那一圈.
            Ring ring = new RingClass();
            ring.AddPoint(leftTopPt);
            ring.AddPoint(rightTopPt);
            ring.AddPoint(rightBottomPt);
            ring.AddPoint(leftBottomPt);
            IPolygon housePoly = GeneratePolygonFromRing(ring); //房子图形.

            //开始画外圈, 外圈左右前多2M, 后需要进行计算. 值为楼层数* 层高* 1.2
            double outerDist = houseFloor * houseFloorHeight * 1.2;
            leftTopPt.PutCoords(leftTopX - 2, leftTopY + outerDist);
            rightTopPt.PutCoords(leftTopX + houseWidth + 2, leftTopY - outerDist);
            leftBottomPt.PutCoords(leftTopX - 2, leftTopY - houseHeight - 2);
            rightBottomPt.PutCoords(leftTopX + houseWidth + 2, leftTopY - houseHeight - 2);

            ring = new RingClass();
            ring.AddPoint(leftTopPt);
            ring.AddPoint(rightTopPt);
            ring.AddPoint(rightBottomPt);
            ring.AddPoint(leftBottomPt);
            IPolygon outerPoly = GeneratePolygonFromRing(ring);

            //然后依次左上, 右上, 左下, 右下, 朝南, 5个圈.
            double x = outerPoly.Envelope.UpperLeft.X;
            double y = outerPoly.Envelope.UpperLeft.Y;
            leftTopPt.PutCoords(x - cornerSize, y + cornerSize);
            rightTopPt.PutCoords(x + cornerSize, y + cornerSize);
            leftBottomPt.PutCoords(x - cornerSize, y - cornerSize);
            rightBottomPt.PutCoords(x + cornerSize, y - cornerSize);
            ring = new RingClass();
            ring.AddPoint(leftTopPt);
            ring.AddPoint(rightTopPt);
            ring.AddPoint(rightBottomPt);
            ring.AddPoint(leftBottomPt);
            IPolygon leftTopPoly = GeneratePolygonFromRing(ring);

            x = outerPoly.Envelope.UpperRight.X;
            y = outerPoly.Envelope.UpperRight.Y;
            leftTopPt.PutCoords(x - cornerSize, y + cornerSize);
            rightTopPt.PutCoords(x + cornerSize, y + cornerSize);
            leftBottomPt.PutCoords(x - cornerSize, y - cornerSize);
            rightBottomPt.PutCoords(x + cornerSize, y - cornerSize);
            ring = new RingClass();
            ring.AddPoint(leftTopPt);
            ring.AddPoint(rightTopPt);
            ring.AddPoint(rightBottomPt);
            ring.AddPoint(leftBottomPt);
            IPolygon rightTopPoly = GeneratePolygonFromRing(ring);

            x = outerPoly.Envelope.LowerLeft.X;
            y = outerPoly.Envelope.LowerLeft.Y;
            leftTopPt.PutCoords(x - cornerSize, y + cornerSize);
            rightTopPt.PutCoords(x + cornerSize, y + cornerSize);
            leftBottomPt.PutCoords(x - cornerSize, y - cornerSize);
            rightBottomPt.PutCoords(x + cornerSize, y - cornerSize);
            ring = new RingClass();
            ring.AddPoint(leftTopPt);
            ring.AddPoint(rightTopPt);
            ring.AddPoint(rightBottomPt);
            ring.AddPoint(leftBottomPt);
            IPolygon leftBottomPoly = GeneratePolygonFromRing(ring);

            x = outerPoly.Envelope.LowerRight.X;
            x = outerPoly.Envelope.LowerRight.Y;
            leftTopPt.PutCoords(x - cornerSize, y + cornerSize);
            rightTopPt.PutCoords(x + cornerSize, y + cornerSize);
            leftBottomPt.PutCoords(x - cornerSize, y - cornerSize);
            rightBottomPt.PutCoords(x + cornerSize, y - cornerSize);
            ring = new RingClass();
            ring.AddPoint(leftTopPt);
            ring.AddPoint(rightTopPt);
            ring.AddPoint(rightBottomPt);
            ring.AddPoint(leftBottomPt);
            IPolygon rightBottomPoly = GeneratePolygonFromRing(ring);

            x = (outerPoly.Envelope.LowerLeft.X + outerPoly.Envelope.LowerRight.X) / 2;
            y = outerPoly.Envelope.LowerLeft.Y;
            leftTopPt.PutCoords(x - cornerSize, y + cornerSize);
            rightTopPt.PutCoords(x + cornerSize, y + cornerSize);
            leftBottomPt.PutCoords(x - cornerSize, y - cornerSize);
            rightBottomPt.PutCoords(x + cornerSize, y - cornerSize);
            ring = new RingClass();
            ring.AddPoint(leftTopPt);
            ring.AddPoint(rightTopPt);
            ring.AddPoint(rightBottomPt);
            ring.AddPoint(leftBottomPt);
            IPolygon southPoly = GeneratePolygonFromRing(ring);

            //最后按照, 内, 外, 左上, 右上, 左下, 右下, 朝南放入list.
            List<IGeometry> houseGeomList = new List<IGeometry>();
            houseGeomList.Add(housePoly);
            houseGeomList.Add(outerPoly);
            houseGeomList.Add(leftTopPoly);
            houseGeomList.Add(rightTopPoly);
            houseGeomList.Add(leftBottomPoly);
            houseGeomList.Add(rightBottomPoly);
            houseGeomList.Add(southPoly);

            return houseGeomList;
        }

        
    }
}
