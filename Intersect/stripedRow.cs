using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using System.Windows;

namespace Intersect
{
    class stripedRow
    {
        public IGeometry row;
        public IGeometry stripedrow;
        public double rowHeight;
        public double rowWidth;
        public stripedRow(IGeometry geom, double height)
        {
            row = geom;
            rowHeight = height;
        }

        public void formatRow()
        {
            //把行切成规整的矩形.
            ITopologicalOperator tpOp = row as ITopologicalOperator;
            tpOp.Simplify();
            IPointCollection ptCol = tpOp.Boundary as IPointCollection;
            List<IPoint> upperPtList = new List<IPoint>()
                , lowerPtList = new List<IPoint>();
            IPoint ulPt = new PointClass()
                , urPt = new PointClass()
                , llPt = new PointClass()
                , lrPt = new PointClass()
                , tempPt = new PointClass();
            int index = -1;
            if (ptCol.PointCount == 6)
            {
                double yMin = row.Envelope.YMin;
                double yMax = row.Envelope.YMax;
                for (int i = 0; i < ptCol.PointCount; i++)
                {
                    if (ptCol.get_Point(i).Y > yMin && ptCol.get_Point(i).Y < yMax)
                    {
                        index = i;
                        break;
                    }
                }
            }
            for (int i = 0; i < ptCol.PointCount- 1; i++)
            {
                tempPt = ptCol.get_Point(i);
                for (int j = 0; j < ptCol.PointCount- 1; j++)
                {
                    if (i == j|| i == index)
                    {
                        continue;
                    }
                    if (ptCol.get_Point(j).Y < tempPt.Y)
                    {
                        upperPtList.Add(tempPt);
                        break;
                    }
                    if (ptCol.get_Point(j).Y > tempPt.Y)
                    {
                        lowerPtList.Add(tempPt);
                        break;
                    }
                }
            }
            if (upperPtList[0].X < upperPtList[1].X)
            {
                ulPt = upperPtList[0];
                urPt = upperPtList[1];
            }
            else
            {
                ulPt = upperPtList[1];
                urPt = upperPtList[0];
            }
            if (lowerPtList[0].X < lowerPtList[1].X)
            {
                llPt = lowerPtList[0];
                lrPt = lowerPtList[1];
            }
            else
            {
                llPt = lowerPtList[1];
                lrPt = lowerPtList[0];
            }
            if (llPt.X < ulPt.X)
            { 
                llPt.X= ulPt.X;
            }
            else
            {
                ulPt.X= llPt.X;
            }
            if (lrPt.X < urPt.X)
            {
                urPt.X = lrPt.X;
            }
            else
            {
                lrPt.X = urPt.X;
            }
            stripedrow = MakePolygonFromPointsList(new List<IPoint>() { ulPt, urPt, lrPt, llPt });
            rowWidth = Math.Abs(lrPt.X - llPt.X);
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
    }
}
