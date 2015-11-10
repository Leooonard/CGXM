using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ESRI.ArcGIS.Controls;
using ESRI.ArcGIS.Geometry;
using ESRI.ArcGIS.Display;
using ESRI.ArcGIS.Carto;

namespace Intersect
{
    class HouseShowcaseManager
    {
        private AxMapControl mapControl;
        private IRgbColor outerColor;
        private IRgbColor innerColor;
        private IRgbColor textColor;
        private int upperLeftX = 10000;
        private int upperLeftY = 10000;
        private double textLeftGap = -2;
        private double textRigthGap = 2;
        private double textFrontGap = -2;
        private double textBackGap = 1;
        private double extraTextRightGap = 20;
        private double extraTextBottomGap = -2;
        private double textAdjustGap = 0.5;

        public HouseShowcaseManager(AxMapControl mc)
        {
            mapControl = mc;

            outerColor = new RgbColor();
            outerColor.Red = 206;
            outerColor.Green = 206;
            outerColor.Blue = 206;
            innerColor = new RgbColor();
            innerColor.Red = 250;
            innerColor.Green = 247;
            innerColor.Blue = 201;
            textColor = new RgbColor();
            textColor.Red = 255;
            textColor.Green = 0;
            textColor.Blue = 0;

        }

        private void drawLineWithText(IPoint startPt, IPoint endPt, string text, bool horizontal)
        {
            IPolyline line = new PolylineClass();
            IPointCollection ptCol = line as IPointCollection;
            ptCol.AddPoint(startPt);
            ptCol.AddPoint(endPt);
            IPoint midPt = new PointClass();
            midPt.X = (startPt.X + endPt.X) / 2;
            midPt.Y = (startPt.Y + endPt.Y) / 2;
            if (horizontal)
            {
                midPt.Y = midPt.Y + textAdjustGap;
            }
            GisTool.DrawPolyline(line, mapControl);
            GisTool.drawText(text, midPt, textColor, mapControl);
        }

        private void ClearShowcase()
        {
            IGraphicsContainer graphicsContainer = mapControl.Map as IGraphicsContainer;
            graphicsContainer.DeleteAllElements();
        }

        public void ShowHouse(House house, CommonHouse commonHouse)
        {
            ClearShowcase();
            IPoint upperLeftPt = new PointClass()
                , upperRightPt = new PointClass()
                , lowerLeftPt = new PointClass()
                , lowerRightPt = new PointClass()
                , lowerMidPt = new PointClass()
                , midLeftPt = new PointClass()
                , midRightPt = new PointClass();
            Ring ring = new RingClass();
            IPolygon innerPolygon = null, outerPolygon = null;
            List<IPolygon> unitPolygonList = new List<IPolygon>();
            IEnvelope extent;

            if (house.width * commonHouse.height > 0)
            {
                upperLeftPt = new PointClass();
                upperLeftPt.X = upperLeftX;
                upperLeftPt.Y = upperLeftY;
                upperRightPt = new PointClass();
                upperRightPt.X = upperLeftPt.X + house.width;
                upperRightPt.Y = upperLeftPt.Y;
                lowerLeftPt = new PointClass();
                lowerLeftPt.X = upperLeftPt.X;
                lowerLeftPt.Y = upperLeftPt.Y - commonHouse.height;
                lowerRightPt = new PointClass();
                lowerRightPt.X = lowerLeftPt.X + house.width;
                lowerRightPt.Y = lowerLeftPt.Y;
                ring = new RingClass();
                ring.AddPoint(upperLeftPt);
                ring.AddPoint(upperRightPt);
                ring.AddPoint(lowerRightPt);
                ring.AddPoint(lowerLeftPt);
                innerPolygon = GisTool.MakePolygonFromRing(ring);

                //每单元
                double unitWidth = house.width / house.unit;
                double unitUpperLeftX = upperLeftX;
                double unitUpperLeftY = upperLeftY;
                for (int i = 0; i < house.unit; i++)
                {
                    IPoint unitUpperLeftPt = new PointClass();
                    unitUpperLeftPt.X = unitUpperLeftX;
                    unitUpperLeftPt.Y = unitUpperLeftY;
                    IPoint unitUpperRightPt = new PointClass();
                    unitUpperRightPt.X = unitUpperLeftPt.X + unitWidth;
                    unitUpperRightPt.Y = unitUpperLeftPt.Y;
                    IPoint unitLowerLeftPt = new PointClass();
                    unitLowerLeftPt.X = unitUpperLeftPt.X;
                    unitLowerLeftPt.Y = unitUpperLeftPt.Y - commonHouse.height;
                    IPoint unitLowerRightPt = new PointClass();
                    unitLowerRightPt.X = unitLowerLeftPt.X + unitWidth;
                    unitLowerRightPt.Y = unitLowerLeftPt.Y;
                    Ring unitRing = new RingClass();
                    unitRing.AddPoint(unitUpperLeftPt);
                    unitRing.AddPoint(unitUpperRightPt);
                    unitRing.AddPoint(unitLowerRightPt);
                    unitRing.AddPoint(unitLowerLeftPt);
                    IPolygon unitPolygon = GisTool.MakePolygonFromRing(unitRing);
                    unitPolygonList.Add(unitPolygon);
                    unitUpperLeftX += unitWidth;
                }
            }

            if (house.leftGap > 0 && house.rightGap > 0 && commonHouse.frontGap > 0 && commonHouse.backGap > 0 && house.width > 0 && commonHouse.height > 0)
            {
                upperLeftPt = new PointClass();
                upperLeftPt.X = upperLeftX - house.leftGap;
                upperLeftPt.Y = upperLeftY + commonHouse.backGap;
                upperRightPt = new PointClass();
                upperRightPt.X = upperLeftPt.X + house.leftGap + house.width + house.rightGap;
                upperRightPt.Y = upperLeftPt.Y;
                lowerLeftPt = new PointClass();
                lowerLeftPt.X = upperLeftPt.X;
                lowerLeftPt.Y = upperLeftPt.Y - commonHouse.backGap - commonHouse.height - commonHouse.frontGap;
                lowerRightPt = new PointClass();
                lowerRightPt.X = lowerLeftPt.X + house.leftGap + house.width + house.rightGap;
                lowerRightPt.Y = lowerLeftPt.Y;
                ring = new RingClass();
                ring.AddPoint(upperLeftPt);
                ring.AddPoint(upperRightPt);
                ring.AddPoint(lowerRightPt);
                ring.AddPoint(lowerLeftPt);
                outerPolygon = GisTool.MakePolygonFromRing(ring);
            }

            if (outerPolygon != null)
            {
                GisTool.drawPolygon(outerPolygon, mapControl, outerColor);
            }
            else
            { 
                //是否要绘制一个文本?
            }
            if (innerPolygon != null)
            {
                GisTool.drawPolygon(innerPolygon, mapControl, innerColor);
            }
            else
            { 
                //是否要绘制一个文本?
            }
            for (int i = 0; i < unitPolygonList.Count; i++)
            {
                GisTool.drawPolygon(unitPolygonList[i], mapControl, innerColor);
            }

            //绘制文字.
            upperLeftPt = new PointClass();
            upperLeftPt.X = upperLeftX;
            upperLeftPt.Y = upperLeftY;
            upperRightPt = new PointClass();
            upperRightPt.X = upperLeftPt.X + house.width;
            upperRightPt.Y = upperLeftPt.Y;
            lowerLeftPt = new PointClass();
            lowerLeftPt.X = upperLeftPt.X;
            lowerLeftPt.Y = upperLeftPt.Y - commonHouse.height;
            lowerRightPt = new PointClass();
            lowerRightPt.X = lowerLeftPt.X + house.width;
            lowerRightPt.Y = lowerLeftPt.Y;

            if (house.leftGap > 0 && house.rightGap > 0 && commonHouse.frontGap > 0 && commonHouse.backGap > 0 && house.width > 0 && commonHouse.height > 0)
            {
                midLeftPt = new PointClass();
                midLeftPt.X = lowerLeftPt.X;
                midLeftPt.Y = (lowerLeftPt.Y + upperLeftPt.Y) / 2;
                IPoint otherEndPt = new PointClass();
                otherEndPt.X = midLeftPt.X - house.leftGap;
                otherEndPt.Y = midLeftPt.Y;
                drawLineWithText(otherEndPt, midLeftPt, "左间距:" + String.Format("{0:F}", house.leftGap) + "(米)", true);
                midRightPt = new PointClass(); //画右间距.
                midRightPt.X = lowerRightPt.X;
                midRightPt.Y = (lowerLeftPt.Y + upperLeftPt.Y) / 2;
                otherEndPt = new PointClass();
                otherEndPt.X = midRightPt.X + house.rightGap;
                otherEndPt.Y = midRightPt.Y;
                drawLineWithText(midRightPt, otherEndPt, "右间距:" + String.Format("{0:F}", house.rightGap) + "(米)", true);
                IPoint upperMidPt = new PointClass(); //画后深.
                upperMidPt.X = (lowerLeftPt.X + lowerRightPt.X) / 2;
                upperMidPt.Y = upperLeftPt.Y;
                otherEndPt = new PointClass();
                otherEndPt.X = upperMidPt.X;
                otherEndPt.Y = upperMidPt.Y + commonHouse.backGap;
                drawLineWithText(otherEndPt, upperMidPt, "后间距:" + String.Format("{0:F}", commonHouse.backGap) + "(米)", false);
                lowerMidPt = new PointClass(); //画前深.
                lowerMidPt.X = (lowerLeftPt.X + lowerRightPt.X) / 2;
                lowerMidPt.Y = lowerLeftPt.Y;
                otherEndPt = new PointClass();
                otherEndPt.X = lowerMidPt.X;
                otherEndPt.Y = lowerMidPt.Y - commonHouse.frontGap;
                drawLineWithText(lowerMidPt, otherEndPt, "前深: " + String.Format("{0:F}", commonHouse.frontGap) + "(米)", false);
            }

            upperRightPt = new PointClass();
            if (house.width > 0)
            {
                upperRightPt.X = upperLeftX + house.width + house.rightGap;
                upperRightPt.Y = upperLeftY + commonHouse.backGap;
            }
            else
            {
                upperRightPt.X = upperLeftX;
                upperRightPt.Y = upperLeftY;
            }
            double extraTextY = 0;
            IPoint extraTextPt = new PointClass();
            extraTextPt.X = upperRightPt.X + extraTextRightGap;
            extraTextPt.Y = upperRightPt.Y;
            extraTextY = extraTextPt.Y;
            if (house.width > 0)
                GisTool.drawText("面宽: " + String.Format("{0:F}", house.width) + "(米)", extraTextPt, textColor, mapControl);
            else
                GisTool.drawText("面宽: 未知", extraTextPt, textColor, mapControl);
            extraTextPt = new PointClass();
            extraTextPt.X = upperRightPt.X + extraTextRightGap;
            extraTextPt.Y = extraTextY + extraTextBottomGap;
            extraTextY = extraTextPt.Y;
            if (commonHouse.height > 0)
                GisTool.drawText("进深:" + String.Format("{0:F}", commonHouse.height) + "(米)", extraTextPt, textColor, mapControl);
            else
                GisTool.drawText("进深: 未知", extraTextPt, textColor, mapControl);
            extraTextPt = new PointClass(); //画房型名.
            extraTextPt.X = upperRightPt.X + extraTextRightGap;
            extraTextPt.Y = extraTextY + extraTextBottomGap;
            extraTextY = extraTextPt.Y;
            if (house.width > 0 && house.unit > 0)
                GisTool.drawText("每单元面宽: " + String.Format("{0:F}", house.width / house.unit) + "(米)", extraTextPt, textColor, mapControl);
            else
                GisTool.drawText("每单元面宽: 未知", extraTextPt, textColor, mapControl);
            extraTextPt = new PointClass(); //画房型名.
            extraTextPt.X = upperRightPt.X + extraTextRightGap;
            extraTextPt.Y = extraTextY + extraTextBottomGap;
            extraTextY = extraTextPt.Y;
            if (commonHouse.height > 0 && house.unit > 0)
                GisTool.drawText("每单元进深: " + String.Format("{0:F}", commonHouse.height / house.unit) + "(米)", extraTextPt, textColor, mapControl);
            else
                GisTool.drawText("每单元进深: 未知", extraTextPt, textColor, mapControl);
            extraTextPt = new PointClass(); //画房型名.
            extraTextPt.X = upperRightPt.X + extraTextRightGap;
            extraTextPt.Y = extraTextY + extraTextBottomGap;
            extraTextY = extraTextPt.Y;
            GisTool.drawText("房型名: " + house.name, extraTextPt, textColor, mapControl);
            extraTextPt = new PointClass(); //画层数.
            extraTextPt.X = upperRightPt.X + extraTextRightGap;
            extraTextPt.Y = extraTextY + extraTextBottomGap;
            extraTextY = extraTextPt.Y;
            if (commonHouse.floor > 0)
                GisTool.drawText("层数: " + commonHouse.floor.ToString() + "(层)", extraTextPt, textColor, mapControl);
            else
                GisTool.drawText("层数: 未知", extraTextPt, textColor, mapControl);
            extraTextPt = new PointClass();
            extraTextPt.X = upperRightPt.X + extraTextRightGap;
            extraTextPt.Y = extraTextY + extraTextBottomGap;
            extraTextY = extraTextPt.Y;
            if (commonHouse.floorHeight > 0)
                GisTool.drawText("层高: " + String.Format("{0:F}", commonHouse.floorHeight) + "(米)", extraTextPt, textColor, mapControl);
            else
                GisTool.drawText("层高: 未知", extraTextPt, textColor, mapControl);
            extraTextPt = new PointClass();
            extraTextPt.X = upperRightPt.X + extraTextRightGap;
            extraTextPt.Y = extraTextY + extraTextBottomGap;
            extraTextY = extraTextPt.Y;
            if (house.unit > 0)
                GisTool.drawText("单元数: " + house.unit.ToString(), extraTextPt, textColor, mapControl);
            else
                GisTool.drawText("单元数: 未知", extraTextPt, textColor, mapControl);
            extraTextPt = new PointClass();
            extraTextPt.X = upperRightPt.X + extraTextRightGap;
            extraTextPt.Y = extraTextY + extraTextBottomGap;
            extraTextY = extraTextPt.Y;
            if (house.houseHold > 0)
                GisTool.drawText("每单元户数: " + house.houseHold.ToString() + "(户)", extraTextPt, textColor, mapControl);
            else
                GisTool.drawText("每单元户数: 未知", extraTextPt, textColor, mapControl);
            extraTextPt = new PointClass();
            extraTextPt.X = upperRightPt.X + extraTextRightGap;
            extraTextPt.Y = extraTextY + extraTextBottomGap;
            extraTextY = extraTextPt.Y;
            if (house.width > 0 && commonHouse.height > 0)
                GisTool.drawText("层面积: " + String.Format("{0:F}", house.width * commonHouse.height) + "(平方米)", extraTextPt, textColor, mapControl);
            else
                GisTool.drawText("层面积: 未知", extraTextPt, textColor, mapControl);
            extraTextPt = new PointClass();
            extraTextPt.X = upperRightPt.X + extraTextRightGap;
            extraTextPt.Y = extraTextY + extraTextBottomGap;
            extraTextY = extraTextPt.Y;
            if (house.width > 0 && commonHouse.height > 0 && commonHouse.floor > 0)
                GisTool.drawText("总面积: " + String.Format("{0:F}", house.width * commonHouse.height * commonHouse.floor) + "(平方米)", extraTextPt, textColor, mapControl);
            else
                GisTool.drawText("总面积: 未知", extraTextPt, textColor, mapControl);

            //移动地图视角.
            if (outerPolygon == null)
            {
                extent = upperRightPt.Envelope;
            }
            else
            {
                extent = outerPolygon.Envelope;
            }
            extent.Expand(2, 2, true);
            mapControl.Extent = extent;
        }
    }
}
