using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Geometry;
using System.Collections;

namespace Intersect
{
    class Area
    {
        public IGeometry areaGeom;
        public IGeometry aroundGeom;
        public IGeometry totalGeom;
        public IGeometry splitLine;
        public ArrayList splitLineEndPtArray; //其中存放的全部都是线的两个端点对象. 其中的线都是分割了图形.

        public Area(IGeometry geom, IGeometry arGeom, ArrayList sArray)
        {
            areaGeom = geom;
            aroundGeom = arGeom;
            splitLineEndPtArray = sArray;
        }

        public Area(IGeometry areaGeo, IGeometry aroundGeo, IGeometry totalGeo, IGeometry splitLin)
        {
            areaGeom = areaGeo;
            aroundGeom = aroundGeo;
            totalGeom = totalGeo;
            splitLine = splitLin;
        }

        public void addSplitPt(IPoint startPt, IPoint endPt)
        {
            LineEndPt lineEnd = new LineEndPt();
            lineEnd.startPt = startPt;
            lineEnd.endPt = endPt;
            splitLineEndPtArray.Add(lineEnd);
        }
    }
}
