   M S B A M L   `   `   `  ����  	Intersect"    Intersect.NetSizeUserControl   5      U QWPFToolkit, Version=3.5.40128.1, Culture=neutral, PublicKeyToken=31bf3856ad364e35\ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35^ ZWindowsFormsIntegration, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35H 9http://schemas.microsoft.com/winfx/2006/xaml/presentation      5      R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5      Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5      @d2http://schemas.microsoft.com/expression/blend/2008     5        � 	Resources  5         GridCellTextBlock �� 
TargetType �� Property �� Value  GridCellTextBox �� CornerRadius �� TextAlignment �� Triggers P� Property P� Value	 �� Self
 �� RelativeSource �� Path  NotErrorTip��%�  &	        &	 �     &	 {    �� 5   
   " ��6*   .�5	      ��  � ��$ 12(�6)   66   �� 5
       �@��Padding$ 5q�6(   64   �� 5       �@��TextAlignment$ Center=�6.   6?   5      �� 5   
   " ��6(   .�5      ��  � ��$ 12(�6)   66   �� 5       � ��$ 5q�6(   64   �� 5       �@=�ErrorTemplate 5      �� 5      .R�5      ]� .+�5      �� $ 20��6%   $��20��67   $��20��6C   
���  ��$
��Left=�6_   ._�5   "   �� $��20��6,   $��20��68   
��� ���$��0,-4,0,0q�6n   $��20(�5   ,   $
��Bold"�6:   $ Center=�6L   ��5   C   Z� !5   f   5      �� 5      $��Right=�68   5      5      5       5      P� 5       �@=�HasError . . �5      ��  �@�ToolTip 5       �� #
 Z	 $) #(Validation.Errors)[0].ErrorContent��� 5"       � ��
 �  ��6B   5#      5%      �� 5&   
   " �6$    5'      P� 5(       �@=�HasError . . �5)      ��  �@=�ErrorTemplate 5*      �� 5+      .R�5,   "   �� 
���  ��$��0q�6;   ._�5-   &   �� 5.   #   5/      51      52      54      55      .��56      � -   5        $��NetSizeGrid��56   0   #��[ 6   $��60��6C    � BindingGroup 57   
   &   System.Windows.Data.BindingGroup  58         Name$ NetSizeBindingGroup��6   68    � RowDefinitions 5:   
   �� 5;      $��5* �6   6*   �� 5<      $��5* �6   6*   .+�5>   
   � $��0��6   $��
0, 0, 0, 0q�6    � ColumnDefinitions 5?      �� 5@      $��4* �6#   60   �� 5A      $��6* �6#   60   .+�5C      �� $��0��6   #��[  6(   网格宽度(米)6\   �� 5D      -   5        $��NetSizeWidthTextBox��5D      $��1��61   #��[ 6A   6l   5E      � 5F   
   $��1��6    5G      �� 5H      $��4* �6#   60   �� 5I      $��6* �6#   60   .+�5K      �� $��0��6   #��[  6(   网格高度(米)6\   �� 5L      -   5        $��NetSizeHeightTextBox��5L      $��1��62   #��[ 6B   6m   5M      5N      5O      