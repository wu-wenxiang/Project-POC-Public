using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Media;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Collections;

namespace UFileClient.Display
{

    /// <summary>
    /// 工具类，提供若干工具方法
    /// </summary>
    public static class InnerUtility
    {

        #region 打开图像文件选择对话框
        /// <summary>
        ///  打开图像文件选择对话框
        ///     <param name="title">对话框标题</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public static List<string> OpenFileSelectDialog(string title)
        {
            // 类型过滤字符串，是否可以多选及对话框标题设置
            string imageFilter = "图片文件(*.jpg,*.jpeg,*.gif,*.bmp,*.tif,*.png)|*.jpg;*.jpeg;*.gif;*.bmp;*.tif;*.png";
            Microsoft.Win32.OpenFileDialog dialogOpenFile = new Microsoft.Win32.OpenFileDialog();
            dialogOpenFile.Multiselect = true;
            dialogOpenFile.Filter = imageFilter;
            dialogOpenFile.Title = title;

            //打开对话框
            bool isOpened = (bool)dialogOpenFile.ShowDialog();

            if (!isOpened)
            {
                return null;
            }

            //获取选择文件名称列表并返回
            string[] strNames = dialogOpenFile.FileNames;
            List<string> selImgNames = strNames.ToList();

            return selImgNames;
        }
        #endregion

        #region 计算旋转角度
        /// <summary>
        ///  计算旋转角度
        ///     <param name="degree">角度</param>
        /// <returns>旋转角度</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public static Rotation GetRotation(int degree)
        {
            Rotation rotation = Rotation.Rotate0;

            int reminder = (degree % 360 + 360) % 360;

            if (reminder == 0)
                rotation = Rotation.Rotate0;
            else if (reminder == 90)
                rotation = Rotation.Rotate90;
            else if (reminder == 180)
                rotation = Rotation.Rotate180;
            else if (reminder == 270)
                rotation = Rotation.Rotate270;

            return rotation;
        }
        #endregion

        #region 判断容器展示方向
        /// <summary>
        ///  判断容器展示方向
        ///     <param name="itemContainer">容器</param>
        /// <returns>是否为纵向</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public static bool HasVerticalOrientation(FrameworkElement itemContainer)
        {
            bool hasVerticalOrientation = true;
            if (itemContainer != null)
            {
                Panel panel = VisualTreeHelper.GetParent(itemContainer) as Panel;
                StackPanel stackPanel;
                WrapPanel wrapPanel;

                if ((stackPanel = panel as StackPanel) != null)
                {
                    hasVerticalOrientation = (stackPanel.Orientation == Orientation.Vertical);
                }
                else if ((wrapPanel = panel as WrapPanel) != null)
                {
                    hasVerticalOrientation = (wrapPanel.Orientation == Orientation.Vertical);
                }
            }
            return hasVerticalOrientation;
        }
        #endregion

        #region 判断监测点是否在容器的上半或者左半位置
        /// <summary>
        ///  判断监测点是否在容器的上半或者左半位置
        ///     <param name="container">容器</param>
        ///     <param name="clickedPoint">检测点</param>
        ///     <param name="hasVerticalOrientation">容器展示方向</param>
        /// <returns>监测点是否在容器的左半或者上半位置</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public static bool IsInFirstHalf(FrameworkElement container, Point clickedPoint, bool hasVerticalOrientation)
        {
            if (hasVerticalOrientation)
            {
                return clickedPoint.Y < container.ActualHeight / 2;
            }
            return clickedPoint.X < container.ActualWidth / 2;
        }
        #endregion
    }

    /// <summary>
    /// 原图展示控件事件枚举类型
    /// </summary>
    public enum SingleImageEventType
    {
        PrevoiusPage,
        NextPage,
        FirstPage,
        LastPage,
        PreviewAllPages,
        SaveRotation
    }

    /// <summary>
    /// 原图展示控件事件参数
    /// </summary>
    public class SingleImageEventArgs
    {
        public SingleImageEventType eventType;
        public int currentPage;
        public int RotateDegree;
        public SingleImageEventArgs(SingleImageEventType type, int curIndex, int rotateDegree)
        {
            eventType = type;
            currentPage = curIndex;
            RotateDegree = rotateDegree;
        }
    }

    public enum ButtonType
    {
        PrintButton,
        PrintPreViewButton,
        PrintPreViewAllButton,
        PrintSetupButton,
        SaveImageButton,
        ZoomInButton,
        ZoomOutButton,
        RotateRightButton,
        RotateLeftButton,
        OriginalViewButton,
        SaveRotationButton,
        PreviousPageButton,
        NextPageButton,
        FirstPageButton,
        LastPageButton,
        SaveAsImageButton
    }
    //影像展示类型
    public enum ImageDisplayType
    {
        Adaptable,             // 自适应显示
        NonAdaptable,          // 非自适应显示
        WidthAdaptable         // 适宽显示
    }
    #region 数组
    /// <summary>
    /// 缩略图控件事件类型数组
    /// </summary>
    public enum ImageThumbnailEventType
    {
        SelectionChange,
        DeleteImage,
        OpenSingleImageWindow
    }

    /// <summary>
    /// 获取图像名字的类型
    /// </summary>
    public enum ItemNameType
    {
        AllItem,
        SelectedItem
    }

    /// <summary>
    /// 控件组合方式
    /// </summary>
    public enum AggregativeType
    {
        Null,
        Container,
        PopUP,
        MultiBatch
    }
    #endregion
}
