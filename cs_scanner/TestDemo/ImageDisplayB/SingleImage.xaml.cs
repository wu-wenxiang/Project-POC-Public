//==========================================================================
//   Copyright(C) 2010,AGRICULTURAL BANK OF CHINA,Corp.All rights reserved 
//==========================================================================
//--------------------------------------------------------------------------
//程序名: ThumbnailImage
//功能: 单个图像展示控件
//作者: qizhenguo
//创建日期: 2010-8-3
//--------------------------------------------------------------------------
//修改历史:
//日期      修改人     修改
//2010-8-2  qizhenguo  创建代码
//--------------------------------------------------------------------------
using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using System.IO;
using System.Windows.Controls;
using System.Collections.Generic;
using UFileClient.Display;
using log4net;
using UFileClient.Common;

namespace UFileClient.Display
{
    /// <summary>
    /// IAC.Display.xaml 的交互逻辑
    /// </summary>
    public partial class SingleImage : System.Windows.Controls.UserControl
    {
        #region 日志记录器
        //获取日志记录器
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        /// <summary>
        ///     静态构造函数，指定日志文件路径
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-20 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        static SingleImage()
        {
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.GetConfigDir() + ConstCommon.logConfigFileName));
        }
        #endregion

        #region  内部数据
        // 当前展示的图像的文件名
        private string imageName;

        // 当前图像在列表中的位置
        private int currentPos;

        // 当前图像的描述信息
        private string currentDescription;

        // 是否正在拖动图片的标志
        private bool draging = false;

        // 拖动图片的起始点
        private Point beginPoint;

        // 当前缩放系数
        private double curCoef;

        //旋转角度
        private int rotateDegree;

        //原图按钮按下标志
        private bool orginalViewButtonPressed;

        //多批次显示列表
        public List<string> FilePathList
        {
            set;
            get;
        }

        //组合方式
        public AggregativeType AggregativeType
        {
            set;
            get;
        }

        //缩略图引用
        public ThumbnailImage Thumbnail
        {
            set;
            get;
        }

        //影像展示类型：自适应、非自适应、适宽
        public ImageDisplayType ImageDisplayType
        {
            set;
            get;
        }

        // 当前展示的图像
        public string SelectedImageName
        {
            get
            {
                return imageName;
            }
            set
            {
                logger.Debug("设置当前展示的图像处理开始。");

                imageName = value;

                using (FileStream mse = new FileStream(value, FileMode.Open, FileAccess.Read))
                {

                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = mse;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();
                    ViewedPhoto.Source = image;
                    ViewedCaption.Content = string.Format("第 {0} 页 {1}", currentPos + 1, currentDescription);

                    if (orginalViewButtonPressed)
                    {
                        //原图按钮按下时，根据原图比例展示图像
                        OriginalView();
                    }
                    else
                    {
                        //原图按钮未按下时，根据展示属性设置展示图像
                        SingleImageSizeChanged();
                    }

                    //滚动条可见性初始化
                    SetScrollViewerVisible(ViewScroll, ScrollBarVisibility.Auto);

                    logger.Debug("设置当前展示的图像处理结束。");

                }
            }
        }

        #endregion

        #region 构造函数
        /// <summary>
        ///     构造函数
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------        
        public SingleImage()
        {
            InitializeComponent();
            AggregativeType = AggregativeType.Container;
            ImageDisplayType = ImageDisplayType.Adaptable;
            SetAllButtonEnable(true);
            //放大倍数初始化
            curCoef = 1.0;
            //旋转角度初始化
            rotateDegree = 0;
            orginalViewButtonPressed = false;
        }
        #endregion

        #region 更新图像大小
        /// <summary>
        ///     
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  zhanggang  创建代码
        //--------------------------------------------------------------------------
        private void Zoom_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("更新图像大小处理开始。");

            orginalViewButtonPressed = false;

            //1：判断命令来源
            Button button = e.Source as Button;
            string cmd = button.Content.ToString();

            //2:根据按钮进行放大系数修改
            switch (cmd)
            {
                case "放大":
                    curCoef *= 1.5;
                    SingleImageSizeChanged();
                    break;
                case "缩小":
                    curCoef *= (double)2.0 / 3;
                    SingleImageSizeChanged();
                    break;
                case "原图":
                    curCoef = 1.0;
                    orginalViewButtonPressed = true;
                    OriginalView();
                    break;
                default:
                    break;

            }

            ViewScroll.UpdateLayout();
            SetScrollViewerVisible(ViewScroll, ScrollBarVisibility.Auto);

            logger.Debug("更新图像大小处理结束。");

        }

        /// <summary>
        ///     按原图比例展示图像
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-27 zhanggang  创建代码
        //--------------------------------------------------------------------------
        public void OriginalView()
        {
            BitmapSource img = (BitmapSource)(ViewedPhoto.Source);

            ViewedPhoto.Width = curCoef * img.Width;
            ViewedPhoto.Height = curCoef * img.Height;
            ViewScroll.UpdateLayout();
        }
        #endregion

        #region 旋转图片
        /// <summary>
        ///     顺时针旋转图片
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  zhanggang  创建代码
        //--------------------------------------------------------------------------
        private void Rotate_Click(object sender, RoutedEventArgs e)
        {
            logger.Debug("旋转图片处理开始。");

            //1：判断命令来源
            Button button = e.Source as Button;
            string cmd = button.Content.ToString();

            //2:根据按钮进行放大系数修改
            switch (cmd)
            {
                case "旋转(顺)":
                    rotateDegree += 90;
                    break;
                case "旋转(逆)":
                    rotateDegree -= 90;
                    break;
                default:
                    break;
            }

            //3:旋转图像
            using (FileStream mse = new FileStream(SelectedImageName, FileMode.Open, FileAccess.Read))
            {
                BitmapImage bitImage = new BitmapImage();
                bitImage.BeginInit();
                bitImage.StreamSource = mse;
                bitImage.CacheOption = BitmapCacheOption.OnLoad;
                bitImage.Rotation = InnerUtility.GetRotation(rotateDegree);
                bitImage.EndInit();
                bitImage.Freeze();

                ViewedPhoto.Source = bitImage;
                ViewedPhoto.Width = bitImage.Width * curCoef;
                ViewedPhoto.Height = bitImage.Height * curCoef;

                ViewScroll.UpdateLayout();
            }

            logger.Debug("旋转图片处理结束。");
        }
        #endregion

        #region 打印图片
        /// <summary>
        ///     打印图片
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void Print_Click(object sender, RoutedEventArgs e)
        {
            PrintControl printCtl = PrintControl.GetSingleObject();
            printCtl.PrintSetup(SelectedImageName);
        }
        #endregion

        #region 预览图片
        /// <summary>
        ///     预览图片
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void PrintPreView_Click(object sender, RoutedEventArgs e)
        {
            PrintControl printCtl = PrintControl.GetSingleObject();
            printCtl.Preview(SelectedImageName);
        }

        /// <summary>
        ///     预览全部
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------        
        private void PrintPreViewAll_Click(object sender, RoutedEventArgs e)
        {
            List<string> list = null;
            //预览多批次图像
            if (AggregativeType == AggregativeType.MultiBatch)
            {
                list = FilePathList;
            }
            //预览缩略图图像
            else
            {
                list = Thumbnail.GetSelectedItemFileName(ItemNameType.AllItem);
            }

            if ((list == null) || (list.Count == 0))
            {
                return;
            }
            else
            {
                PrintControl printCtl = PrintControl.GetSingleObject();
                printCtl.Preview(list);
            }

        }
        #endregion

        #region 打印设置
        /// <summary>
        ///     打印设置
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void PrintSetup_Click(object sender, RoutedEventArgs e)
        {
            PrintControl printCtl = PrintControl.GetSingleObject();
            printCtl.PageSetup();
        }
        #endregion

        #region 鼠标在图片上按下左键时的动作。根据滚动条是否出现判断是否允许拖动
        /// <summary>
        ///     鼠标在图片上按下左键时的动作。根据滚动条是否出现判断是否允许拖动
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ViewedPhoto_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 获取滚动条是否出现的状态
            Visibility isHoriVisible = ViewScroll.ComputedHorizontalScrollBarVisibility;
            Visibility isVertVisible = ViewScroll.ComputedVerticalScrollBarVisibility;

            // 如果滚动条出现，则允许拖动
            if (Visibility.Visible.Equals(isHoriVisible) ||
                Visibility.Visible.Equals(isVertVisible))
            {
                ViewedPhoto.Cursor = Cursors.Hand;
                draging = true;
                beginPoint = e.GetPosition(this);
            }
        }
        #endregion

        #region 鼠标在图片上放开左键时的动作。根据滚动条是否出现判断是否停止拖动状态
        /// <summary>
        ///     鼠标在图片上放开左键时的动作。根据滚动条是否出现判断是否停止拖动状态
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ViewedPhoto_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            // 获取滚动条是否出现的状态
            Visibility isHoriVisible = ViewScroll.ComputedHorizontalScrollBarVisibility;
            Visibility isVertVisible = ViewScroll.ComputedVerticalScrollBarVisibility;

            // 如果滚动条出现，则停止拖动状态
            if (Visibility.Visible.Equals(isHoriVisible) ||
                Visibility.Visible.Equals(isVertVisible))
            {
                ViewedPhoto.Cursor = Cursors.Arrow;
                draging = false;
            }
        }
        #endregion

        #region 鼠标在图片上移动的拖动操作
        /// <summary>
        ///     鼠标在图片上移动的拖动操作
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ViewedPhoto_MouseMove(object sender, MouseEventArgs e)
        {
            // 如果正在拖动图片，则通过变换滚动条位置，移动图片
            if (draging)
            {
                Point newPoint = e.GetPosition(this);
                double newHoriOffset = ViewScroll.HorizontalOffset - newPoint.X + beginPoint.X;
                double newVertOffset = ViewScroll.VerticalOffset - newPoint.Y + beginPoint.Y;

                ViewScroll.ScrollToHorizontalOffset(newHoriOffset);
                ViewScroll.ScrollToVerticalOffset(newVertOffset);

                beginPoint = newPoint;
            }
        }
        #endregion

        #region 保存旋转
        /// <summary>
        ///     保存旋转
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void SaveRotation_Click(object sender, RoutedEventArgs e)
        {
            //1:原图图片存在判断
            if (ViewedPhoto.Source == null)
            {
                return;
            }
            //2:图片文件存在判断
            if (SelectedImageName == null)
            {
                return;
            }
            //3:旋转角度判断
            if (rotateDegree % 360 == 0)
            {
                return;
            }

            logger.Debug("保存旋转处理开始。");

            //4:保存图片
            using (FileStream mse = new FileStream(SelectedImageName, FileMode.Create))
            {
                BitmapEncoder encoder = GetEncoder(SelectedImageName);
                encoder.Frames.Add(BitmapFrame.Create(ViewedPhoto.Source as BitmapSource));
                encoder.Save(mse);
                mse.Close();
            }

            //5:触发原图控件保存旋转事件
            Thumbnail.SaveRotation(currentPos, rotateDegree);
            Thumbnail.RotateImage(currentPos, rotateDegree);

            //旋转角度初始化
            rotateDegree = 0;

            logger.Debug("保存旋转处理结束。");

        }

        /// <summary>
        ///     获取影像的编码格式
        ///     <param name="filePath">文件路径</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private BitmapEncoder GetEncoder(string filePath)
        {
            BitmapEncoder encoder = new JpegBitmapEncoder();
            string ext = Path.GetExtension(filePath);
            switch (ext)
            {
                case "jpg":
                case "jpeg":
                    encoder = new JpegBitmapEncoder();
                    break;
                case "bmp":
                    encoder = new BmpBitmapEncoder();
                    break;
                case "tiff":
                case "tif":
                    encoder = new TiffBitmapEncoder();
                    break;
                case "png":
                    encoder = new PngBitmapEncoder();
                    break;
                case "gif":
                    encoder = new GifBitmapEncoder();
                    break;
                default:
                    break;
            }
            return encoder;
        }
        #endregion

        #region 另存图片
        /// <summary>
        ///     另存图片
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------     
        private void SaveImage_Click(object sender, RoutedEventArgs e)
        {
            //1:原图图片存在判断
            if (ViewedPhoto.Source == null)
            { return; }
            //2:图片文件存在判断
            if (SelectedImageName == null)
            { return; }

            logger.Debug("另存图片处理开始。");

            //3:图片格式过滤
            string filter = "JPEG文件,(*.jpg)|*.jpg|PNG文件,(*.png)|*.png|BMP文件,(*.bmp)|*.bmp|TIFF文件,(*.tif)|*.tif|所有文件,(*.*)|*.*";

            string fileName = SaveFileDialog("保存图片", filter, SelectedImageName);
            //4:另存图片
            if (fileName != null && SelectedImageName != fileName)
            {
                File.Copy(SelectedImageName, fileName);
            }

            logger.Debug("另存图片处理结束。");
        }

        /// <summary>
        ///     保存图片对话框
        ///     <param name="title">对话框标题</param>
        ///     <param name="filter">过滤图片格式</param>
        ///     <param name="fileName">图片名称（包括路径）</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  qizhenguo  创建代码
        //--------------------------------------------------------------------------   
        private string SaveFileDialog(string title, string filter, string fileName)
        {
            logger.Debug("保存图片对话框处理开始。");

            Microsoft.Win32.SaveFileDialog dialogSaveFile = new Microsoft.Win32.SaveFileDialog();

            dialogSaveFile.Filter = filter;
            string ext = System.IO.Path.GetExtension(fileName);
            dialogSaveFile.DefaultExt = ext;
            int index = 1;
            switch (ext)
            {
                case ".jpg":
                case ".jpeg":
                    index = 1;
                    break;
                case ".png":
                    index = 2;
                    break;
                case ".bmp":
                    index = 3;
                    break;
                case ".tif":
                case ".tiff":
                    index = 4;
                    break;
                default:
                    index = 5;
                    break;
            }
            dialogSaveFile.FilterIndex = index;
            dialogSaveFile.FileName = System.IO.Path.GetFileNameWithoutExtension(fileName);
            dialogSaveFile.CheckPathExists = true;
            dialogSaveFile.Title = title;

            bool isOpened = (bool)dialogSaveFile.ShowDialog();

            logger.Debug("保存图片对话框处理结束。");

            if (!isOpened)
                return null;
            else
                return dialogSaveFile.FileName;
        }
        #endregion

        #region 翻页功能
        /// <summary>
        ///     翻页功能
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  zhanggang  创建代码
        //-------------------------------------------------------------------------- 
        private void TurnOverPage_Click(object sender, RoutedEventArgs e)
        {
            //1：判断命令来源
            Button button = e.Source as Button;
            string cmd = button.Content.ToString();

            //2:根据按钮进行翻页修改
            SingleImageEventType type = SingleImageEventType.FirstPage;
            switch (cmd)
            {
                case "上页":
                    type = SingleImageEventType.PrevoiusPage;
                    break;
                case "下页":
                    type = SingleImageEventType.NextPage;
                    break;
                case "首页":
                    type = SingleImageEventType.FirstPage;
                    break;
                case "尾页":
                    type = SingleImageEventType.LastPage;
                    break;
                default:
                    break;
            }


            //3:根据按钮判断待显示图像索引
            int index = currentPos;
            int cnt = -1;
            //显示多批次图像
            if (AggregativeType == AggregativeType.MultiBatch)
            {
                cnt = FilePathList.Count;
            }
            //显示缩略图图像
            else
            {
                cnt = Thumbnail.GetPhotosListBoxCount();
            }
            switch (type)
            {
                //获取缩略图控件上一页
                case SingleImageEventType.PrevoiusPage:
                    index = (index - 1) < 0 ? 0 : (index - 1);
                    break;
                //获取缩略图控件下一页
                case SingleImageEventType.NextPage:
                    index = (index + 1) < cnt ? (index + 1) : (cnt - 1);
                    break;
                //获取缩略图控件首页
                case SingleImageEventType.FirstPage:
                    index = 0;
                    break;
                //获取缩略图控件尾页
                case SingleImageEventType.LastPage:
                    index = cnt - 1;
                    break;
                default:
                    return;
            }

            //4:显示图像
            //显示多批次图像
            if (AggregativeType == AggregativeType.MultiBatch)
            {
                SetDisplayedImage(FilePathList[index], index, string.Empty);
            }
            //显示缩略图控件
            else
            {
                SetDisplayedImage(Thumbnail.GetSelectedImage(index).FileName, index, Thumbnail.GetSelectedImage(index).GetMetaField(BosField.descriptionName));
                Thumbnail.SetSelectedIndex(index);
            }


            //5：设置按钮的使能性
            SetButtonEnable(ButtonType.FirstPageButton, true);
            SetButtonEnable(ButtonType.NextPageButton, true);
            SetButtonEnable(ButtonType.PreviousPageButton, true);
            SetButtonEnable(ButtonType.LastPageButton, true);

            if (index == 0)
                SetButtonEnable(ButtonType.PreviousPageButton, false);
            else if (index == (cnt - 1))
                SetButtonEnable(ButtonType.NextPageButton, false);
        }
        #endregion

        #region 清除展示图片
        /// <summary>
        ///     清除展示图片
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        public void Dispose()
        {
            logger.Debug("清除展示图片处理开始。");

            ViewedPhoto.Source = null;
            ViewedCaption.Content = string.Empty;
            //设置滚动条可见性
            SetScrollViewerVisible(ViewScroll, ScrollBarVisibility.Hidden);

            logger.Debug("清除展示图片处理结束。");
        }
        #endregion

        #region 设置展示图像
        /// <summary>
        ///     设置展示图像
        ///     <param name="fileName">图像路径</param>
        ///     <param name="index">图像所在缩略图的索引</param>
        ///     <param name="description">图像描述信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  zhanggang  创建代码
        //-------------------------------------------------------------------------- 
        public void SetDisplayedImage(string fileName, int index, string description)
        {
            logger.Debug("设置展示图像处理开始。");

            currentPos = index;
            currentDescription = description;
            SelectedImageName = fileName;

            logger.Debug("设置展示图像处理结束。");

        }
        #endregion

        #region 设置控件属性
        /// <summary>
        /// 设置工具条按钮可见性
        /// </summary>
        /// <param name="type">按钮名称</param>
        /// <param name="isVisible">是否可见</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  zhanggang  创建代码
        //-------------------------------------------------------------------------- 
        public void SetButtonVisible(ButtonType type, bool isVisible)
        {
            logger.Debug("设置工具条按钮可见性处理开始。");

            Button btn = FindName(type.ToString()) as Button;
            if (btn != null)
            {
                if (isVisible)
                {
                    btn.Visibility = Visibility.Visible;
                }
                else
                {
                    btn.Visibility = Visibility.Collapsed;
                }
            }

            logger.Debug("设置工具条按钮可见性处理结束。");
        }

        /// <summary>
        /// 设置按钮使能性
        ///  <param name="type">按钮名称</param>
        /// <param name="isEnable">是否使能</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  zhanggang  创建代码
        //-------------------------------------------------------------------------- 
        public void SetButtonEnable(ButtonType type, bool isEnable)
        {
            logger.Debug("设置按钮使能性处理开始。");

            Button btn = FindName(type.ToString()) as Button;
            btn.IsEnabled = isEnable;

            logger.Debug("设置按钮使能性处理结束。");
        }

        /// <summary>
        /// 设置滚动条可见性
        ///  <param name="scrollViewer">滚动条名称</param>
        /// <param name="scrollBarVisibilityType">设置类型</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        private void SetScrollViewerVisible(ScrollViewer scrollViewer, ScrollBarVisibility scrollBarVisibilityType)
        {
            logger.Debug("设置滚动条可见性处理开始。");

            scrollViewer.VerticalScrollBarVisibility = scrollBarVisibilityType;
            scrollViewer.HorizontalScrollBarVisibility = scrollBarVisibilityType;

            logger.Debug("设置滚动条可见性处理结束。");
        }

        /// <summary>
        /// 设置全部按钮使能性
        /// <param name="isEnable">是否使能</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        public void SetAllButtonEnable(bool isEnable)
        {
            logger.Debug("设置全部按钮使能性处理开始。");

            SetButtonEnable(ButtonType.ZoomInButton, isEnable);
            SetButtonEnable(ButtonType.ZoomOutButton, isEnable);
            SetButtonEnable(ButtonType.OriginalViewButton, isEnable);
            SetButtonEnable(ButtonType.RotateRightButton, isEnable);
            SetButtonEnable(ButtonType.RotateLeftButton, isEnable);
            SetButtonEnable(ButtonType.SaveRotationButton, isEnable);
            SetButtonEnable(ButtonType.PrintButton, isEnable);
            SetButtonEnable(ButtonType.PrintPreViewButton, isEnable);
            SetButtonEnable(ButtonType.PrintPreViewAllButton, isEnable);
            SetButtonEnable(ButtonType.PrintSetupButton, isEnable);
            SetButtonEnable(ButtonType.SaveImageButton, isEnable);
            SetButtonEnable(ButtonType.PreviousPageButton, isEnable);
            SetButtonEnable(ButtonType.NextPageButton, isEnable);
            SetButtonEnable(ButtonType.FirstPageButton, isEnable);
            SetButtonEnable(ButtonType.LastPageButton, isEnable);
            SetButtonEnable(ButtonType.PrintPreViewButton, isEnable);
            SetButtonEnable(ButtonType.SaveAsImageButton, isEnable);

            logger.Debug("设置全部按钮使能性处理结束。");
        }

        /// <summary>
        /// 设置工具条的可见性
        /// <param name="visible">工具条是否可见</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        public void SetToolBarVisible(bool visible)
        {
            logger.Debug("设置工具条的可见性处理开始。");

            if (visible)
            {
                toolbarBorder.Visibility = Visibility.Visible;
            }
            else
            {
                toolbarBorder.Visibility = Visibility.Collapsed;
            }

            logger.Debug("设置工具条的可见性处理结束。");

        }
        #endregion

        #region 重新计算图像大小
        /// <summary>
        /// 重新计算图像大小
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        ///--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人      修改
        //2010-8-26  zhanggang  创建代码
        //--------------------------------------------------------------------------
        private void ViewScroll_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            SingleImageSizeChanged();
        }

        /// <summary>
        /// 根据展示属性设置进行图像展示大小的修改
        /// </summary>
        ///--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人      修改
        //2010-8-26  zhanggang  创建代码
        //--------------------------------------------------------------------------
        private void SingleImageSizeChanged()
        {
            if (ImageDisplayType == ImageDisplayType.Adaptable)
            {
                ViewedPhoto.Width = ViewScroll.ActualWidth * 0.93 * curCoef;
                ViewedPhoto.Height = ViewScroll.ActualHeight * 0.99 * curCoef;
            }
            else if (ImageDisplayType == ImageDisplayType.WidthAdaptable)
            {
                ViewedPhoto.Width = ViewScroll.ActualWidth * 0.93 * curCoef;

                if (ViewedPhoto.Source != null)
                {
                    BitmapSource img = (BitmapSource)(ViewedPhoto.Source);
                    ViewedPhoto.Height = (ViewedPhoto.Width * img.Height) / img.Width;
                }

            }
        }
        #endregion

        private void SaveAsImage_Click(object sender, RoutedEventArgs e)
        {
            List<string> list = null;
            //多批次图像
            if (AggregativeType == AggregativeType.MultiBatch)
            {
                list = FilePathList;
            }
            //缩略图图像
            else
            {
                list = Thumbnail.GetSelectedItemFileName(ItemNameType.AllItem);
            }

            if ((list == null) || (list.Count == 0))
            {
                return;
            }
            System.Windows.Forms.FolderBrowserDialog folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog();
            folderBrowserDialog.ShowDialog();
            string selctPath = folderBrowserDialog.SelectedPath;
            for (int i = 0; i < list.Count;i++)
            {
                //string fileName = System.IO.Path.GetFileNameWithoutExtension(list[i]);//原来的
                //fileName = System.IO.Path.GetFileName(list[i]);//原来的

                //应its需求，在点击批量保存按钮时将文件名按照顺序号输出，wk，2018-8-23
                string seq = (i+1).ToString("000");
                string fileExtension = System.IO.Path.GetExtension(list[i]);
                string fullName = selctPath + "\\" + seq + fileExtension;

                //string fullName = selctPath + "\\" + fileName;//原来的
                File.Copy(list[i],fullName);
            }
        }
    }

}
