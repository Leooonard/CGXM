using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Collections;
using ESRI.ArcGIS.Geodatabase;
using ESRI.ArcGIS.Carto;
using ESRI.ArcGIS.Geometry;

namespace Intersect
{
    class UsefulComment
    {
        private Area makeArea(IGeometry baseGeom, ArrayList roadList)
        {
            //Area area = new Area(baseGeom, new ArrayList());
            for (int i = 0; i < roadList.Count; i++)
            {
                IRelationalOperator reOp = baseGeom as IRelationalOperator;
                ITopologicalOperator tpOp = baseGeom as ITopologicalOperator;
                IPolyline road = roadList[i] as IPolyline;
                IPointCollection roadPtCol = road as IPointCollection;
                IPoint startPt = roadPtCol.get_Point(0);
                IPoint endPt = roadPtCol.get_Point(roadPtCol.PointCount - 1);
                if (reOp.Crosses(road))
                {
                    //有相交关系.
                    IGeometry geomBoundary = tpOp.Boundary;

                    //将线与边界线转为点集.
                    IPointCollection boundaryPtCol = geomBoundary as IPointCollection;
                    IPointCollection linePtCol = road as IPointCollection;
                    tpOp = boundaryPtCol as ITopologicalOperator;
                    IGeometry intersectedGeom = tpOp.Intersect(linePtCol as IGeometry, esriGeometryDimension.esriGeometry0Dimension);
                    IPointCollection intersectedPtCol = intersectedGeom as IPointCollection;

                    //查看交集结果点的个数.
                    int intersectedPtCount = intersectedPtCol.PointCount;
                    //2个点说明是切割关系, 1个点说明是插入关系, 0个点说明没有关系.
                    if (intersectedPtCount == 2)
                    {
                        //2个点. 说明是切割关系, 直接将图形内的切割线放入数组.
                        startPt = intersectedPtCol.get_Point(0);
                        endPt = intersectedPtCol.get_Point(1);
                        //area.addSplitPt(startPt, endPt);
                    }
                    else if (intersectedPtCount == 1)
                    {
                        //1个点. 说明是插入关系, 需要找到插入在图形内部的那个点.
                        IPoint pt = linePtCol.get_Point(0);
                        IPoint intersectedPt = intersectedPtCol.get_Point(0);
                        //要考虑端点正好在边界线上的极端情况.
                        if ((intersectedPt.X == pt.X && intersectedPt.Y == pt.Y))
                        {
                            //遇到了极端情况1. 需要保证另一个端点在图形内部.
                            pt = linePtCol.get_Point(linePtCol.PointCount - 1);
                            if (reOp.Contains(pt))
                            {
                                //area.addSplitPt(intersectedPtCol.get_Point(0) as IPoint, pt);
                            }
                        }
                        else if ((intersectedPt.X == linePtCol.get_Point(linePtCol.PointCount - 1).X && intersectedPt.Y == linePtCol.get_Point(linePtCol.PointCount - 1).Y))
                        {
                            //极端情况2, 需要保证pt在图形内部.
                            if (reOp.Contains(pt))
                            {
                                //找到了内部的点, 将其放入area对象的数组中.
                                //area.addSplitPt(intersectedPtCol.get_Point(0) as IPoint, pt);
                            }
                        }
                        else
                        {
                            if (reOp.Contains(pt))
                            {
                                //找到了内部的点, 将其放入area对象的数组中.
                                //area.addSplitPt(intersectedPtCol.get_Point(0) as IPoint, pt);
                            }
                            else
                            {
                                //另一个端点是内部的点, 将其放入area对象的数组中.
                                //area.addSplitPt(intersectedPtCol.get_Point(0) as IPoint, linePtCol.get_Point(linePtCol.PointCount - 1));
                            }
                        }
                    }
                }
            }

            //return area;
            return null;
        }

        private ArrayList getE0TypeFeature(IFeatureClass feaCls)
        {
            ArrayList E0TypeList = new ArrayList();
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "TYPE_TEXT='E0'";
            IFeatureLayer feaLy = new FeatureLayer();
            feaLy.FeatureClass = feaCls;
            IFeatureSelection feaSel = feaLy as IFeatureSelection;
            feaSel.SelectFeatures(filter, esriSelectionResultEnum.esriSelectionResultNew, false);
            ISelectionSet selSet = feaSel.SelectionSet;
            ICursor cursor;
            selSet.Search(null, true, out cursor);
            IFeatureCursor feaCursor = cursor as IFeatureCursor;
            IFeature fea = feaCursor.NextFeature();
            while (fea != null)
            {
                if (fea != null)
                {
                    E0TypeList.Add(fea.ShapeCopy);
                }
                fea = feaCursor.NextFeature();
            }
            
            return E0TypeList;
        }

        private ArrayList getBiggerSlopeGeometry(IFeatureClass feaCls)
        {
            ArrayList biggerSlopGeomArray = new ArrayList();
            IQueryFilter filter = new QueryFilterClass();
            filter.WhereClause = "GRIDCODE>=10";
            IFeatureLayer feaLy = new FeatureLayer();
            feaLy.FeatureClass = feaCls;
            IFeatureSelection feaSel = feaLy as IFeatureSelection;
            feaSel.SelectFeatures(filter, esriSelectionResultEnum.esriSelectionResultNew, false);
            ISelectionSet selSet = feaSel.SelectionSet;
            ICursor cursor;
            selSet.Search(null, true, out cursor);
            IFeatureCursor feaCursor = cursor as IFeatureCursor;
            IFeature fea = feaCursor.NextFeature();
            while (fea != null)
            {
                if (fea != null)
                {
                    biggerSlopGeomArray.Add(fea.ShapeCopy);
                }
                fea = feaCursor.NextFeature();
            }

            return biggerSlopGeomArray;
        }

        private string getMainType(ArrayList list)
        {
            string mainType = "";
            ArrayList sizeList = new ArrayList();
            for (int i = 0; i < list.Count; i++)
            {
                sizeList.Add(((IntersectedAreaInfo)list[i]).areaSize);
            }
            int index = getMaxIndex(sizeList);
            mainType = ((IntersectedAreaInfo)list[index]).areaType;
            return mainType;
        }

        private int getMaxIndex(ArrayList list)
        {
            //接受数字数组, 返回最大值的下标.
            double max = 0;
            if (list.Count == 0)
            {
                return 0;
            }
            max = (double)list[0];
            for (int i = 0; i < list.Count; i++)
            {
                if ((double)list[i] > max)
                {
                    max = (double)list[i];
                }
            }

            if (list.IndexOf(max) >= 0)
            {
                return list.IndexOf(max);
            }
            return 0;
        }
    }
}


//<GroupBox Name="RestrainConfigGroupBox" Height="170">
//            <GroupBox.HeaderTemplate>
//                <DataTemplate>
//                    <TextBlock Style="{StaticResource GroupBoxHeader}">
//                        排除
//                    </TextBlock>
//                </DataTemplate>
//            </GroupBox.HeaderTemplate>
//            <ListBox Name="RestrainConfigListBox" Height="170" BorderThickness="0">
//                <ListBox.ItemContainerStyle>
//                    <Style TargetType="ListBoxItem">
//                        <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
//                        <Setter Property="Margin" Value="5"></Setter>
//                        <Style.Triggers>
//                            <Trigger Property="IsMouseOver" Value="true">
//                                <Setter Property="Background" Value="#f5f5f5"></Setter>
//                            </Trigger>
//                            <Trigger Property="IsSelected" Value="true">
//                                <Setter Property="Background" Value="#337ab7"></Setter>
//                                <Setter Property="Foreground" Value="White"></Setter>
//                            </Trigger>
//                        </Style.Triggers>
//                    </Style>
//                </ListBox.ItemContainerStyle>
//                <ListBox.ItemTemplate>
//                    <DataTemplate>
//                        <Grid PreviewMouseDown="ConfigGrid_MouseDown">
//                            <Grid.ColumnDefinitions>
//                                <ColumnDefinition Width="0.6*"></ColumnDefinition>
//                                <ColumnDefinition Width="0.4*"></ColumnDefinition>
//                            </Grid.ColumnDefinitions>
//                            <TextBlock Grid.Column="0" Style="{StaticResource SettingTitle}" Text="{Binding Path=name, Mode=TwoWay}"></TextBlock>
//                            <TextBox Grid.Column="1" Name="ConfigValueTextBox" Style="{StaticResource ErrorTip}">
//                                <TextBox.Text>
//                                    <Binding BindingGroupName="ConfigBindingGroup" Path="value" Mode="TwoWay" UpdateSourceTrigger="LostFocus">
//                                        <Binding.ValidationRules>
//                                            <cls:NotNegativeDoubleValidationRule></cls:NotNegativeDoubleValidationRule>
//                                        </Binding.ValidationRules>
//                                    </Binding>
//                                </TextBox.Text>
//                            </TextBox>
//                            <TextBlock Name="ConditionIDTextBlock" Text="{Binding Path=conditionID, Mode=TwoWay}" Visibility="Collapsed"></TextBlock>
//                        </Grid>
//                    </DataTemplate>
//                </ListBox.ItemTemplate>
//            </ListBox>
//        </GroupBox>
//        <GroupBox Name="StandardConfigGroupBox" Height="170" Margin="0,10,0,0">
//            <GroupBox.HeaderTemplate>
//                <DataTemplate>
//                    <TextBlock Style="{StaticResource GroupBoxHeader}">
//                        权重
//                    </TextBlock>
//                </DataTemplate>
//            </GroupBox.HeaderTemplate>
//            <ListBox Name="StandardConfigListBox" Height="170" BorderThickness="0">
//                <ListBox.ItemContainerStyle>
//                    <Style TargetType="ListBoxItem">
//                        <Setter Property="HorizontalContentAlignment" Value="Stretch"/>
//                        <Setter Property="Margin" Value="5"></Setter>
//                        <Style.Triggers>
//                            <Trigger Property="IsMouseOver" Value="true">
//                                <Setter Property="Background" Value="#f5f5f5"></Setter>
//                            </Trigger>
//                            <Trigger Property="IsSelected" Value="true">
//                                <Setter Property="Background" Value="#337ab7"></Setter>
//                                <Setter Property="Foreground" Value="White"></Setter>
//                            </Trigger>
//                        </Style.Triggers>
//                    </Style>
//                </ListBox.ItemContainerStyle>
//                <ListBox.ItemTemplate>
//                    <DataTemplate>
//                        <Grid PreviewMouseDown="ConfigGrid_MouseDown">
//                            <Grid.ColumnDefinitions>
//                                <ColumnDefinition Width="0.4*"></ColumnDefinition>
//                                <ColumnDefinition Width="0.3*"></ColumnDefinition>
//                                <ColumnDefinition Width="0.3*"></ColumnDefinition>
//                            </Grid.ColumnDefinitions>
//                            <TextBlock Grid.Column="0" Style="{StaticResource SettingTitle}" Text="{Binding Path=name, Mode=TwoWay}"></TextBlock>
//                            <TextBox Grid.Column="1" Name="ConfigValueTextBox" Style="{StaticResource ErrorTip}">
//                                <TextBox.Text>
//                                    <Binding BindingGroupName="ConfigBindingGroup" Path="value" Mode="TwoWay" UpdateSourceTrigger="LostFocus">
//                                        <Binding.ValidationRules>
//                                            <cls:NotNegativeDoubleValidationRule></cls:NotNegativeDoubleValidationRule>
//                                        </Binding.ValidationRules>
//                                    </Binding>
//                                </TextBox.Text>
//                            </TextBox>
//                            <TextBlock Grid.Column="2" Style="{StaticResource SettingTitle}" Name="ConfigRealStandardTextBlock" Text="{Binding Path=realStandard, Mode=TwoWay}"></TextBlock>
//                            <TextBlock Name="ConditionIDTextBlock" Text="{Binding Path=conditionID, Mode=TwoWay}" Visibility="Collapsed"></TextBlock>
//                        </Grid>
//                    </DataTemplate>
//                </ListBox.ItemTemplate>
//            </ListBox>
//        </GroupBox>