using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;

namespace Intersect
{
    public class VillageColorRandomer
    {
        private List<string> colorList;
        private List<string> randomColorList; //存放随机生成的颜色.
        private int usedColorCount;

        public VillageColorRandomer()
        {
            initColorList();
            randomColorList = new List<string>();
            usedColorCount = 0;
        }

        public string randomColor()
        {
            string colorString = "";
            if (usedColorCount < colorList.Count)
            {
                colorString = colorList[usedColorCount];
                usedColorCount++;
                return colorString;
            }
            while (true)
            {
                Random randomer = new Random();
                int red = randomer.Next(0, 256);
                int green = randomer.Next(0, 256);
                int blue = randomer.Next(0, 256);
                colorString = String.Format(@"#{0}{1}{2}", red.ToString("X2"), green.ToString("X2"), blue.ToString("X2"));
                if (randomColorList.Contains(colorString) || colorList.Contains(colorString))
                {
                    continue;
                }
                else
                {
                    randomColorList.Add(colorString);
                    return colorString;
                }
            }
        }

        public void reset()
        {
            initColorList();
            randomColorList = new List<string>();
            usedColorCount = 0;
        }

        public static int GetRedFromColorString(string colorString)
        {
            Color color = ColorTranslator.FromHtml(colorString);
            return color.R;
        }

        public static int GetGreenFromColorString(string colorString)
        {
            Color color = ColorTranslator.FromHtml(colorString);
            return color.G;
        }

        public static int GetBlueFromColorString(string colorString)
        {
            Color color = ColorTranslator.FromHtml(colorString);
            return color.B;
        }

        public static string GetReverseVillageColorString(string colorString)
        {
            int maxColorInt = 255;
            int red = VillageColorRandomer.GetRedFromColorString(colorString);
            int green = VillageColorRandomer.GetGreenFromColorString(colorString);
            int blue = VillageColorRandomer.GetBlueFromColorString(colorString);

            int reverseRed = maxColorInt - red;
            int reverseGreen = maxColorInt - green;
            int reverseBlue = maxColorInt - blue;

            Color color = Color.FromArgb(reverseRed, reverseGreen, reverseBlue);
            return ColorTranslator.ToHtml(color);
        }

        private void initColorList()
        {
            colorList = new List<string>() 
            { 
                "#FF00FF", //牡丹红.
                "#00FFFF", //青色.
                "#FFFF00", //黄色.
                "#000000", //黑色.
                "#70DB93", //海蓝.
                "#5C3317", //巧克力色.
                "#9F5F9F", //蓝紫色.
                "#B5A642", //黄铜色.
                "#D9D919", //亮金色.
                "#A67D3D", //棕色.
                "#8C7853", //青铜色.
                "#A67D3D", //2号青铜色.
                "#5F9F9F", //士官服蓝色.
                "#D98719", //冷铜色.
                "#B87333", //铜色.
                "#FF7F00", //珊瑚红.
                "#42426F", //紫蓝色.
                "#5C4033", //深棕.
                "#2F4F2F", //深绿.
                "#4A766E", //深铜绿色.
                "#4F4F2F", //深橄榄绿.
                "#9932CD", //深兰花色.
                "#871F78", //深紫色.
                "#6B238E", //深石板蓝.
                "#2F4F4F", //深铅灰色.
                "#97694F", //深棕褐色.
                "#7093DB", //深绿松石色.
                "#855E42", //暗木色.
                "#******", //淡灰色.
                "#856363", //土灰玫瑰红色.
                "#D19275", //长石色.
                "#8E2323", //火砖色.
                "#238E23", //森林绿.
                "#CD7F32", //金色.
                "#DBDB70", //鲜黄色.
                "#C0C0C0", //灰色.
                "#527F76", //铜绿色.
                "#93DB70", //青黄色.
                "#215E21", //猎人绿.
                "#4E2F2F", //印度红.
                "#9F9F5F", //土黄色.
                "#C0D9D9", //浅蓝色.
                "#A8A8A8", //浅灰色.
                "#8F8FBD", //浅钢蓝色.
                "#E9C2A6", //浅木色.
                "#32CD32", //石灰绿色.
                "#E47833", //桔黄色.
                "#8E236B", //褐红色.
                "#32CD99", //中海蓝色.
                "#3232CD", //中蓝色.
                "#6B8E23", //中森林绿.
                "#EAEAAE", //中鲜黄色.
                "#9370DB", //中兰花色.
                "#426F42", //中海绿色.
                "#7F00FF", //中石板蓝色.
                "#7FFF00", //中春绿色.
                "#70DBDB", //中绿松石色.
                "#DB7093", //中紫红色.
                "#A68064", //中木色.
                "#2F2F4F", //深藏青色.
                "#23238E", //海军蓝.
                "#4D4DFF", //霓虹篮.
                "#FF6EC7", //霓虹粉红.
                "#00009C", //新深藏青色.
                "#EBC79E", //新棕褐色.
                "#CFB53B", //暗金黄色.
                "#FF7F00", //橙色.
            }; 
        }
    }
}
