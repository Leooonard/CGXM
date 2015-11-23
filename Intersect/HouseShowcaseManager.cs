//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using ESRI.ArcGIS.Controls;
//using ESRI.ArcGIS.Geometry;
//using ESRI.ArcGIS.Display;
//using ESRI.ArcGIS.Carto;

//namespace Intersect
//{
//    class HouseShowcaseManager
//    {
//        private AxMapControl mapControl;
//        private IRgbColor outerColor;
//        private IRgbColor innerColor;
//        private IRgbColor textColor;

//        private int upperLeftX = 10000;
//        private int upperLeftY = 10000;
//        private double textLeftGap = -2;
//        private double textRigthGap = 2;
//        private double textFrontGap = -3;
//        private double textBackGap = 1;
//        private double extraTextRightGap = 20;
//        private double extraTextBottomGap = -2;

//        public HouseShowcaseManager(AxMapControl mc)
//        {
//            mapControl = mc;

//            outerColor = new RgbColor();
//            outerColor.Red = 206;
//            outerColor.Green = 206;
//            outerColor.Blue = 206;

//            innerColor = new RgbColor();
//            innerColor.Red = 250;
//            innerColor.Green = 247;
//            innerColor.Blue = 201;

//            textColor = new RgbColor();
//            textColor.Red = 255;
//            textColor.Green = 0;
//            textColor.Blue = 0;
//        }

//        private void ClearShowcase()
//        {
//            IGraphicsContainer graphicsContainer = mapControl.Map as IGraphicsContainer;
//            graphicsContainer.DeleteAllElements();
//        }

//        private bool checkHouseValid(House house, CommonHouse commonHouse)
//        {
//            if (house.width <= 0 || house.unit <= 0)
//            {
//                return false;
//            }
//            if (commonHouse.backGap <= 0 || commonHouse.floor <= 0 || commonHouse.floorHeight <= 0 || commonHouse.frontGap <= 0 ||
//                commonHouse.height <= 0 || commonHouse <= 0)
//            {
//                return false;
//            }
//            return true;
//        }

//        public void ShowHouse(House house, CommonHouse commonHouse)
//        {
//            if (!checkHouseValid(house, commonHouse))
//            {
//                Tool.W("数据错误，无法显示。"); //暂时先用这个办法提示。
//                return;
//            }

//            ClearShowcase();

//            IPolygon innerPolygon = null, outerPolygon = null;
//            List<IPolygon> unitPolygonList = new List<IPolygon>();
//            IEnvelope extent;

//            innerPolygon = GisTool.MakePolygon(upperLeftX, upperLeftY, house.width * house.unit, commonHouse.height);

//            //每单元
//            double unitWidth = house.width;
//            double unitUpperLeftX = upperLeftX;
//            double unitUpperLeftY = upperLeftY;
//            for (int i = 0; i < house.unit; i++)
//            {
//                unitPolygonList.Add(GisTool.MakePolygon(unitUpperLeftX, unitUpperLeftY, house.width, commonHouse.height));
//                unitUpperLeftX += unitWidth;
//            }

//            outerPolygon = GisTool.MakePolygon(upperLeftX - commonHouse.horizontalGap, upperLeftY + commonHouse.frontGap,
//                house.width * house.unit + commonHouse.horizontalGap * 2,
//                commonHouse.height + commonHouse.frontGap + commonHouse.backGap);

//            GisTool.drawPolygon(outerPolygon, mapControl, outerColor);
//            GisTool.drawPolygon(innerPolygon, mapControl, innerColor);
//            for (int i = 0; i < unitPolygonList.Count; i++)
//            {
//                GisTool.drawPolygon(unitPolygonList[i], mapControl, innerColor);
//            }

//            //绘制文字.
//            //upperLeftPt = new PointClass();
//            //upperLeftPt.X = upperLeftX;
//            //upperLeftPt.Y = upperLeftY;
//            //upperRightPt = new PointClass();
//            //upperRightPt.X = upperLeftPt.X + house.width;
//            //upperRightPt.Y = upperLeftPt.Y;
//            //lowerLeftPt = new PointClass();
//            //lowerLeftPt.X = upperLeftPt.X;
//            //lowerLeftPt.Y = upperLeftPt.Y - commonHouse.height;
//            //lowerRightPt = new PointClass();
//            //lowerRightPt.X = lowerLeftPt.X + house.width;
//            //lowerRightPt.Y = lowerLeftPt.Y;

//            //midLeftPt = new PointClass();
//            //midLeftPt.X = lowerLeftPt.X;
//            //midLeftPt.Y = (lowerLeftPt.Y + upperLeftPt.Y) / 2;
//            //IPoint otherEndPt = new PointClass();
//            //otherEndPt.X = midLeftPt.X - commonHouse.horizontalGap;
//            //otherEndPt.Y = midLeftPt.Y;
//            //GisTool.DrawLineWithText(otherEndPt, midLeftPt, "间距:" + String.Format("{0:F}", commonHouse.horizontalGap) + "(米)", mapControl);
            
//            //IPoint upperMidPt = new PointClass(); //画后深.
//            //upperMidPt.X = (lowerLeftPt.X + lowerRightPt.X) / 2;
//            //upperMidPt.Y = upperLeftPt.Y;
//            //otherEndPt = new PointClass();
//            //otherEndPt.X = upperMidPt.X;
//            //otherEndPt.Y = upperMidPt.Y + commonHouse.backGap;
//            //GisTool.DrawLineWithText(otherEndPt, upperMidPt, "后深:" + String.Format("{0:F}", commonHouse.backGap) + "(米)", mapControl);

//            //lowerMidPt = new PointClass(); //画前深.
//            //lowerMidPt.X = (lowerLeftPt.X + lowerRightPt.X) / 2;
//            //lowerMidPt.Y = lowerLeftPt.Y;
//            //otherEndPt = new PointClass();
//            //otherEndPt.X = lowerMidPt.X;
//            //otherEndPt.Y = lowerMidPt.Y - commonHouse.frontGap;
//            //GisTool.DrawLineWithText(lowerMidPt, otherEndPt, "前深: " + String.Format("{0:F}", commonHouse.frontGap) + "(米)", mapControl);

//            //upperRightPt = new PointClass();
//            //upperRightPt.X = upperLeftX + house.width + commonHouse.horizontalGap;
//            //upperRightPt.Y = upperLeftY + commonHouse.backGap;

//            //double extraTextY = 0;
//            //IPoint extraTextPt = new PointClass();
//            //extraTextPt.X = upperRightPt.X + extraTextRightGap;
//            //extraTextPt.Y = upperRightPt.Y;
//            //extraTextY = extraTextPt.Y;
//            //GisTool.drawText("面宽: " + String.Format("{0:F}", house.width) + "(米)", extraTextPt, textColor, mapControl);

//            //extraTextPt = new PointClass();
//            //extraTextPt.X = upperRightPt.X + extraTextRightGap;
//            //extraTextPt.Y = extraTextY + extraTextBottomGap;
//            //extraTextY = extraTextPt.Y;
//            //GisTool.drawText("进深:" + String.Format("{0:F}", commonHouse.height) + "(米)", extraTextPt, textColor, mapControl);
            
//            //extraTextPt = new PointClass(); //画房型名.
//            //extraTextPt.X = upperRightPt.X + extraTextRightGap;
//            //extraTextPt.Y = extraTextY + extraTextBottomGap;
//            //extraTextY = extraTextPt.Y;
//            //if (house.width > 0 && house.unit > 0)
//            //    GisTool.drawText("每单元面宽: " + String.Format("{0:F}", house.width / house.unit) + "(米)", extraTextPt, textColor, mapControl);
//            //else
//            //    GisTool.drawText("每单元面宽: 未知", extraTextPt, textColor, mapControl);
            
//            //extraTextPt = new PointClass(); //画层数.
//            //extraTextPt.X = upperRightPt.X + extraTextRightGap;
//            //extraTextPt.Y = extraTextY + extraTextBottomGap;
//            //extraTextY = extraTextPt.Y;
//            //if (commonHouse.floor > 0)
//            //    GisTool.drawText("层数: " + commonHouse.floor.ToString() + "(层)", extraTextPt, textColor, mapControl);
//            //else
//            //    GisTool.drawText("层数: 未知", extraTextPt, textColor, mapControl);
//            //extraTextPt = new PointClass();
//            //extraTextPt.X = upperRightPt.X + extraTextRightGap;
//            //extraTextPt.Y = extraTextY + extraTextBottomGap;
//            //extraTextY = extraTextPt.Y;
//            //if (commonHouse.floorHeight > 0)
//            //    GisTool.drawText("层高: " + String.Format("{0:F}", commonHouse.floorHeight) + "(米)", extraTextPt, textColor, mapControl);
//            //else
//            //    GisTool.drawText("层高: 未知", extraTextPt, textColor, mapControl);
//            //extraTextPt = new PointClass();
//            //extraTextPt.X = upperRightPt.X + extraTextRightGap;
//            //extraTextPt.Y = extraTextY + extraTextBottomGap;
//            //extraTextY = extraTextPt.Y;
//            //if (house.unit > 0)
//            //    GisTool.drawText("单元数: " + house.unit.ToString(), extraTextPt, textColor, mapControl);
//            //else
//            //    GisTool.drawText("单元数: 未知", extraTextPt, textColor, mapControl);
//            IPoint textPt = new PointClass();
//            textPt.X = upperLeftX + house.width * house.unit + commonHouse.horizontalGap + extraTextRightGap;
//            textPt.Y = upperLeftY - commonHouse.height * 2 + extraTextBottomGap;
//            GisTool.drawText("层面积: " + String.Format("{0:F}", house.width * house.unit * commonHouse.height) + "(平方米)", textPt, textColor, mapControl);

//            textPt = new PointClass();
//            textPt.X = upperLeftX + house.width * house.unit + commonHouse.horizontalGap + extraTextRightGap;
//            textPt.Y = upperLeftY - commonHouse.height * 2 + extraTextBottomGap + textFrontGap;
//            GisTool.drawText("总面积: " + String.Format("{0:F}", house.width * house.unit * commonHouse.height * commonHouse.floor) + "(平方米)", textPt, textColor, mapControl);


//            //移动地图视角.
//            extent = outerPolygon.Envelope;

//            extent.Expand(2, 2, true);
//            mapControl.Extent = extent;
//            mapControl.ActiveView.Refresh();
//        }
//    }
//}
