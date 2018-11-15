//==========================================================================
//   Copyright(C) 2010,AGRICULTURAL BANK OF CHINA,Corp.All rights reserved 
//==========================================================================
//--------------------------------------------------------------------------
//程序名: ThumbnailImage
//功能: 图像缩略图展示控件
//作者: xuhang
//创建日期: 2014-8-2
//--------------------------------------------------------------------------
//修改历史:
//日期      修改人     修改
//2017-2-27  xuhang  
//--------------------------------------------------------------------------
using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using log4net;
using System.Windows.Input;
using System.Collections;
using System.Windows.Data;
using System.Linq;
using UFileClient.Common;
using System.Text.RegularExpressions;

using System.Text;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows.Documents;


namespace UFileClient.Display
{
    /// <summary>
    /// ThumbnailImage.xaml 的交互逻辑
    /// </summary>
    public partial class ThumbnailImage : UserControl
    {
        #region 公共属性
        //缩略图中图片显示像素高度
        public int ImageWidth
        {
            set;
            get;
        }
        //聚合类型
        public AggregativeType AggregativeType
        {
            set;
            get;
        }

        //缩略图中图片绑定资源集合
        private FileMetaDataCollection FileMetaDataCollection
        {
            get
            {
                return this.FindResource("myFileMetaDataCollection") as FileMetaDataCollection;
            }
        }
        // 可编辑标志
        public bool ContextMenuEditbale
        {
            set;
            get;
        }
        //是否可以使用F2
        public bool CanKeyF2Use
        {
            set;
            get;
        }
        #endregion

        #region 私有变量
        // 获取鼠标所在的位置
        private delegate Point GetPositionDelegate(IInputElement element);

        // 是否按下了Shift键
        private bool isShiftKeyDown = false;

        // 鼠标处于按下状态
        private bool isMousePressed = false;
        // 日志对象
        private readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        // 基准图像列表，如果为null，表示全新增
        private List<FileMetaDataItem> baseFileMetaDataList = null;

        // 基准图像状态


        //缩略图控件的父窗口
        private Window topWindow;
        //拖动开始的鼠标位置
        private Point initialMousePosition;
        //控件的展示方向（横向或者纵向）
        private bool hasVerticalOrientation;
        //拖拽项的容器
        private FrameworkElement targetItemContainer;
        //插入位置索引
        private int insertionIndex;
        //鼠标位置是否在拖拽项的上半部分
        private bool isInFirstHalf;
        //插入位置标识
        private InsertionAdorner insertionAdorner;


        static ThumbnailImage()
        {
            //            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.GetConfigDir() + ConstCommon.logConfigFileName));
        }

        #endregion

        #region 注册控件发生事件
        // 注册控件发生事件
        public static readonly RoutedEvent ImageThumbnailEvent =
            EventManager.RegisterRoutedEvent("ImageThumbnailEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ThumbnailImage));
        //事件包装器
        public event RoutedEventHandler ImageThumbnailEventHandler
        {
            add { AddHandler(ImageThumbnailEvent, value); }
            remove { RemoveHandler(ImageThumbnailEvent, value); }
        }
        /// <summary>
        /// 事件触发
        /// </summary>
        /// <param name="routedEvent">路由事件</param>
        private void RaiseImageThumbnailsEvent(ImageThumbnailEventType eventType)
        {
            RoutedEventArgs routedEventArgs = new RoutedEventArgs(ImageThumbnailEvent);
            routedEventArgs.Source = eventType;
            RaiseEvent(routedEventArgs);
        }
        #endregion

        #region 构造函数
        /// <summary>
        ///  构造函数
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public ThumbnailImage()
        {
            InitializeComponent();
            ImageWidth = 100;
            AggregativeType = AggregativeType.Container;
            CanKeyF2Use = true;
        }
        #endregion

        #region 缩略图列表追加删除图像事件
        /// <summary>
        ///     向缩略图列表中追加图像事件
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ButtonAdd_Click(object sender, RoutedEventArgs e)
        {
            //1:打开追加图像对话框追加追加图像
            List<string> selImgNames = InnerUtility.OpenFileSelectDialog("追加");

            logger.Debug("向缩略图列表中追加图像事件处理开始。");

            //2:未选择追加的图像文件
            if (selImgNames == null) { return; }

            //3:追加图像
            this.AddImages(selImgNames, FileMetaDataCollection.Count);

            logger.Debug("向缩略图列表中追加图像事件处理结束。");
        }

        /// <summary>
        ///     向指定位置插入图片事件
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ButtonInsert_Click(object sender, RoutedEventArgs e)
        {
            //1:打开追加图像对话框插入图像
            List<string> selImgNames = InnerUtility.OpenFileSelectDialog("追加");

            logger.Debug("向指定位置插入图片事件处理开始。");

            //2:未选择插入的图像文件
            if (selImgNames == null) { return; }

            //3:仅在选择一个元素时,允许插入
            if (PhotosListBox.SelectedItems == null || PhotosListBox.SelectedItems.Count != 1)
            {
                MessageBox.Show("未选择或选择了多个元素作为插入点，只能选择一个元素作为插入点！");
                return;
            }

            //4:插入图像
            this.AddImages(selImgNames, PhotosListBox.SelectedIndex);

            logger.Debug("向指定位置插入图片事件处理结束。");
        }


        /// <summary>
        ///     删除选中图片事件
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ButtonDelete_Click(object sender, RoutedEventArgs e)
        {
            //1:删除确认
            MessageBoxResult retValue = MessageBox.Show(
                "确实要从列表删除选中的图像吗？",
                "删除确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (retValue == MessageBoxResult.No) { return; }

            logger.Debug("删除选中图片事件处理开始。");

            //2:获取删除列表
            List<FileMetaDataItem> delItemList = GetSelectedItemList();

            //3:选中列表空判断
            if (delItemList == null || delItemList.Count == 0) { return; }

            //4:删除选中图像
            this.DeleteImages(delItemList);

            logger.Debug("删除选中图片事件处理结束。");
        }

        /// <summary>
        ///     删除全部图片事件
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ButtonDeleteAll_Click(object sender, RoutedEventArgs e)
        {
            //全部删除确认
            MessageBoxResult retValue = MessageBox.Show(
            "确实要从列表删除全部图像吗？",
            "删除确认",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

            if (retValue == MessageBoxResult.No)
            { return; }

            logger.Debug("删除全部图片事件处理开始。");

            //删除全部图片
            this.DeleteAllImages();

            logger.Debug("删除全部图片事件处理结束。");

        }

        /// <summary>
        ///     替换图片
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-10 qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        private void ButtonUpdate_Click(object sender, RoutedEventArgs e)
        {

            //1:仅在选择一个待替换图像时,允许替换
            if (PhotosListBox.SelectedItems == null || PhotosListBox.SelectedItems.Count != 1)
            {
                MessageBox.Show("未选择或选择了多个元素作为替换图片，只能对一个图片进行替换操作！");
                return;
            }

            //2:确认替换
            MessageBoxResult retValue = MessageBox.Show(
                "确实要替换列表选中的图像吗？",
                "替换确认",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (retValue == MessageBoxResult.No) { return; }

            //3:打开替换图像对话框选择替换图像
            List<string> selImgNames = InnerUtility.OpenFileSelectDialog("替换");

            //4:仅在选择一个替换图像时,允许替换
            if (selImgNames == null || selImgNames.Count != 1) { return; }

            logger.Debug("替换图片事件处理开始。");

            //5:替换图像
            this.UpdateImages(selImgNames[0], PhotosListBox.SelectedIndex);

            logger.Debug("替换图片事件处理结束。");
        }
        #endregion

        #region 图片描述信息事件
        /// <summary>
        ///图片描述信息得到焦点
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void PhotoDescription_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            try
            {
                this.Cursor = Cursors.Wait;
                int index = PhotosListBox.SelectedIndex;
                FileMetaDataCollection[index].SetMetaField(BosField.descriptionName, textBox.Text);
                textBox.Text = FileMetaDataCollection[index].SerialID + " " + textBox.Text;
                //触发图像更改选中事件,设置原图描述信息
                RaiseImageThumbnailsEvent(ImageThumbnailEventType.SelectionChange);
                textBox.IsEnabled = false;
            }
            catch (Exception exp)
            {
                logger.Error("图片描述信息得到焦点事件处理异常，异常信息: " + exp.Message);
            }
            finally
            {
                this.Cursor = Cursors.Arrow;
            }
        }

        /// <summary>
        ///图片描述信息键盘按下
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        private void PhotoDescription_KeyDown(object sender, KeyEventArgs e)
        {
            TextBox textBox = (TextBox)sender;

            switch (e.Key)
            {
                // 确认改名
                case Key.Enter:
                    this.PhotosListBox.Focus();
                    break;
                // 取消改名
                case Key.Escape:
                    {
                        int index = PhotosListBox.SelectedIndex;
                        textBox.Text = FileMetaDataCollection[index].SerialID + " " + textBox.Tag.ToString();
                        this.PhotosListBox.Focus();
                        break;
                    }
                default:
                    break;
            }
        }

        /// <summary>
        ///图片描述信息失去焦点
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        private void PhotoDescription_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox textBox = (TextBox)sender;
            int index = PhotosListBox.SelectedIndex;
            textBox.Tag = FileMetaDataCollection[index].GetMetaField(BosField.descriptionName);
            textBox.Text = FileMetaDataCollection[index].GetMetaField(BosField.descriptionName);
            textBox.SelectAll();
        }
        #endregion

        #region 滚动条事件
        /// <summary>
        ///     右键点击空白处时，取消选择状态
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ScrollView_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            PhotosListBox.SelectedItems.Clear();
        }

        /// <summary>
        ///     菜单使能
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ScrollView_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            e.Handled = !ContextMenuEditbale;
        }
        #endregion

        #region 图片事件
        /// <summary>
        /// 拖拽过程中若鼠标靠近上、下边界并且滚动条可以滚动时，滚动滚动条
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ListBoxItem_DragOver(object sender, DragEventArgs e)
        {
            Point newPoint = e.GetPosition(this);

            if (newPoint.Y < 30)
                ScrollView.ScrollToVerticalOffset(ScrollView.VerticalOffset - 110);

            if (ScrollView.ActualHeight - newPoint.Y < 30)
                ScrollView.ScrollToVerticalOffset(ScrollView.VerticalOffset + 110);

        }

        /// <summary>
        /// 拖拽动作结束时，将拖拽项在原来的位置删除，在新的位置插入，并更新相应的索引号
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ListBoxItem_Drop(object sender, DragEventArgs e)
        {
            if (isMousePressed)
            {
                //获得鼠标位置所在的项的索引值
                int index = GetCurrentIndex(e.GetPosition);
                if (index < 0)
                { return; }

                //已选中的待移动图片
                List<FileMetaDataItem> selectedItems = e.Data.GetData(typeof(List<FileMetaDataItem>)) as List<FileMetaDataItem>;
                //要移动到的图片位置
                FileMetaDataItem dropTargetItem = PhotosListBox.Items[index] as FileMetaDataItem;
                // 如果目标项属于拖拽项之一，则不操作
                if (selectedItems.Contains(dropTargetItem))
                    return;
                //拖拽项中首尾项的索引
                int removedLastIndex = PhotosListBox.Items.IndexOf(selectedItems[selectedItems.Count - 1]);
                int removedFirstIndex = PhotosListBox.Items.IndexOf(selectedItems[0]);
                //如果两边的项同时拖向中间的某个位置，则不操作
                if ((removedFirstIndex <= this.insertionIndex) && (this.insertionIndex <= removedLastIndex))
                { return; }
                //如果拖拽项的尾项索引小于插入位置索引，则插入位置索引减去拖拽项数为新的插入位置索引
                else if (removedLastIndex < this.insertionIndex)
                { this.insertionIndex = this.insertionIndex - selectedItems.Count; }

                //移除待移动图片
                foreach (FileMetaDataItem item in selectedItems)
                {
                    FileMetaDataCollection.Remove(item);
                }

                //将待移动图片插入指定位置
                foreach (FileMetaDataItem item in selectedItems)
                {
                    FileMetaDataCollection.Insert(this.insertionIndex++, item);
                }

                //文件序号
                int serialID = 0;
                //更新序号
                foreach (FileMetaDataItem item in PhotosListBox.Items)
                {
                    serialID++;
                    int indexOrder = PhotosListBox.Items.IndexOf(item);
                    FileMetaDataCollection[indexOrder].LabelContent = (indexOrder + 1).ToString() + " " + FileMetaDataCollection[indexOrder].GetMetaField(BosField.descriptionName);
                    FileMetaDataCollection[indexOrder].SerialID = serialID;
                }

                isMousePressed = false;
                //移除插入标识
                RemoveInsertionAdorner();

            }
        }

        /// <summary>
        /// 拖拽过程中鼠标移动事件
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-6  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ListBoxItem_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && isMousePressed == true && ContextMenuEditbale)
            {
                List<FileMetaDataItem> dragItems = new List<FileMetaDataItem>();

                foreach (FileMetaDataItem item in PhotosListBox.SelectedItems)
                {
                    dragItems.Add(item);
                }

                isShiftKeyDown = (Keyboard.GetKeyStates(Key.LeftShift) & KeyStates.Down) > 0 ||
                                 (Keyboard.GetKeyStates(Key.RightShift) & KeyStates.Down) > 0;

                if (dragItems.Count != 0 && !isShiftKeyDown)
                {
                    // 先排序待拖拽项，保证顺序
                    dragItems.Sort(delegate(FileMetaDataItem item1, FileMetaDataItem item2)
                    {
                        return item1.SerialID - item2.SerialID;
                    });
                    DragDrop.DoDragDrop(PhotosListBox, dragItems, DragDropEffects.Move);
                    //移除插入位置标识
                    this.RemoveInsertionAdorner();
                }

            }

        }

        /// <summary>
        ///双击事件，控件组合方式选择
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ListBoxItem_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            //当控件组合方式为POPUP时
            if (AggregativeType == AggregativeType.PopUP)
            {
                RaiseImageThumbnailsEvent(ImageThumbnailEventType.OpenSingleImageWindow);
            }
        }

        /// <summary>
        /// 图像选择改变处理
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ListBoxItem_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //触发图像更改选中事件
            RaiseImageThumbnailsEvent(ImageThumbnailEventType.SelectionChange);
        }

        /// <summary>
        /// 鼠标右键按下时，根据是否选择了多个图像，设置菜单项的可用性
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ListBoxItem_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            //获取永久选中状态列表
            List<FileMetaDataItem> fixedFileMetaDataItem = GetFixedSelectedItem();

            bool status = false;

            //选择多个元素时,右键菜单的【插入】不可用
            if (fixedFileMetaDataItem.Count > 1 || PhotosListBox.SelectedItems.Count > 1)
            { status = false; }
            else if (fixedFileMetaDataItem.Count == 0)
            { status = true; }
            else
            {
                if (fixedFileMetaDataItem[0].Equals(PhotosListBox.SelectedItem))
                { status = true; }
                else
                { status = false; }
            }
            if (CMenuDeleteAll.IsEnabled == true)
            {
                CMenuInsert.IsEnabled = status;
            }
            else
            {
                CMenuInsert.IsEnabled = false;
            }

        }

        /// <summary>
        ///右键点击图片时，如果该图片已经被选择，则不再修改当前的选择状态
        ///     <param name="target">目标元素</param>
        ///     <param name="getPosition">鼠标在缩略图控件上的位置</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  qizhenguo  创建代码
        //--------------------------------------------------------------------------        
        private void ListBoxItem_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            //获得鼠标位置所在的项的索引值
            int index = GetCurrentIndex(e.GetPosition);

            FileMetaDataItem item = PhotosListBox.Items[index] as FileMetaDataItem;

            if (PhotosListBox.SelectedItems.Contains(item))
            // 将鼠标右键按下事件设置为已处理事件
            { e.Handled = true; }

        }

        /// <summary>
        ///将鼠标左键按下事件
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  qizhenguo  创建代码
        //--------------------------------------------------------------------------  
        private void ListBoxItem_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            //获得鼠标位置所在的项的索引值
            int index = GetCurrentIndex(e.GetPosition);
            PhotosListBox.ScrollIntoView(this.PhotosListBox.Items[index]);
            isMousePressed = true;

            this.initialMousePosition = e.GetPosition(this.topWindow);
            this.topWindow = Window.GetWindow(this.PhotosListBox);
        }

        /// <summary>
        ///拖拽时，鼠标移动到进入元素边界事件
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //--------------------------------------------------------------------------  
        private void ListBoxItem_PreviewDragEnter(object sender, DragEventArgs e)
        {
            DecideDropTarget(e);

            if (PhotosListBox.SelectedItems != null)
            {
                CreateInsertionAdorner();
            }

        }

        /// <summary>
        ///拖拽时，鼠标在元素上移动事件
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //--------------------------------------------------------------------------  
        private void ListBoxItem_PreviewDragOver(object sender, DragEventArgs e)
        {
            DecideDropTarget(e);

            if (PhotosListBox.SelectedItems != null)
            {
                UpdateInsertionAdornerPosition();
            }
        }

        /// <summary>
        ///拖拽时，鼠标移出元素边界事件
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        private void ListBoxItem_PreviewDragLeave(object sender, DragEventArgs e)
        {
            if (PhotosListBox.SelectedItems != null)
            {
                RemoveInsertionAdorner();
            }

            e.Handled = true;
        }

        /// <summary>
        ///鼠标左键松开事件
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        private void ListBoxItem_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            RemoveInsertionAdorner();
        }

        /// <summary>
        ///键盘空格键或F2按下事件
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        private void ListBoxItem_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {

                case Key.Left:
                case Key.Up:
                case Key.Right:
                case Key.Down:
                    {
                        isShiftKeyDown = (Keyboard.GetKeyStates(Key.LeftShift) & KeyStates.Down) > 0 ||
                                (Keyboard.GetKeyStates(Key.RightShift) & KeyStates.Down) > 0;
                        if (isShiftKeyDown)
                        {
                            e.Handled = true;
                        }
                    }
                    break;

                case Key.Space:
                    {
                        int count = PhotosListBox.SelectedItems.Count;
                        // 没有可操作项
                        if (count == 0)
                        { return; }

                        FileMetaDataItem item = PhotosListBox.Items.CurrentItem as FileMetaDataItem;
                        //获取永久选中状态
                        List<FileMetaDataItem> fixedSelectedItems = GetFixedSelectedItem();

                        if (fixedSelectedItems.Contains(item))
                        {
                            // 选中的项包含于“永久”选中集合
                            foreach (FileMetaDataItem selectedItem in PhotosListBox.SelectedItems)
                            {
                                int index = PhotosListBox.Items.IndexOf(selectedItem);
                                SetFixedSelectedItem(false, index, selectedItem);
                            }
                            PhotosListBox.UnselectAll();
                        }
                        else
                        {
                            // 选中的项不包含于“永久”选中集合
                            foreach (FileMetaDataItem selectedItem in PhotosListBox.SelectedItems)
                            {
                                int index = PhotosListBox.Items.IndexOf(selectedItem);
                                SetFixedSelectedItem(true, index, selectedItem);
                            }
                        }
                    }
                    e.Handled = true;
                    break;
                case Key.F2:
                    {
                        if (CanKeyF2Use)
                        {
                            TextBox myTextbox = this.GetCurrentTextBoxObj("PhotoDescription") as TextBox;
                            myTextbox.IsEnabled = true;
                            myTextbox.Focus();
                        }
                    }
                    break;
                default:
                    break;
            }
        }

        // 获得鼠标位置所在的项的索引值
        /// <summary>
        ///获得鼠标位置所在的项的索引值
        ///     <param name="getPosition">鼠标在缩略图控件上的位置</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private int GetCurrentIndex(GetPositionDelegate getPosition)
        {
            int index = -1;

            for (int i = 0; i < this.PhotosListBox.Items.Count; ++i)
            {
                ListBoxItem item = (ListBoxItem)(PhotosListBox.ItemContainerGenerator.ContainerFromItem(PhotosListBox.Items[i]));

                if (this.IsMouseOverTarget(item, getPosition))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }

        /// <summary>
        ///判断鼠标是否点击在某个可视元素上
        ///     <param name="target">目标元素</param>
        ///     <param name="getPosition">鼠标在缩略图控件上的位置</param>
        ///     <returns>鼠标是否点击在某个可视元素上</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-5  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private bool IsMouseOverTarget(Visual target, GetPositionDelegate getPosition)
        {
            Rect bounds = VisualTreeHelper.GetDescendantBounds(target);
            Point mousePos = getPosition((IInputElement)target);
            return bounds.Contains(mousePos);
        }
        #endregion

        #region 共有方法
        /// <summary>
        ///     在指定位置替换图片
        ///     <param name="selImgNames">需要替换的文件名列表（包括路径）</param>
        ///     <param name="postion">替换位置</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-10 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void UpdateImages(string selImgName, int postion)
        {
            logger.Debug("在指定位置替换图片处理开始。");

            //1:获取替换位置文件序号
            int serialID = FileMetaDataCollection[postion].SerialID;

            //2:替换图像
            string fileName = this.ConvertFile(selImgName);
            //            selImgName = baseFileMetaDataList[postion].OriginalName;
            FileMetaDataItem fileMetaData = new FileMetaDataItem(ImageWidth, fileName, serialID, false, selImgName);
            fileMetaData.OriginalName = selImgName;
            //            fileMetaData.FileType = baseFileMetaDataList[postion].FileType;
            //            fileMetaData.FileDesp = baseFileMetaDataList[postion].FileDesp;
            //            fileMetaData.PageId = baseFileMetaDataList[postion].PageId;
            //            fileMetaData.TemplateId = baseFileMetaDataList[postion].TemplateId;
            fileMetaData.Status = FileMetaDataStatus.R;
            FileMetaDataCollection.SetItem(postion, fileMetaData);
            FileMetaDataCollection[postion].SerialID = serialID;

            //3:选中缩略图列表中替换项
            SetDefaultSelectedImage(postion);

            logger.Debug("在指定位置替换图片处理结束。");

        }

        /// <summary>
        ///     默认选中缩略图列表中第一项
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-4  qizhenguo  创建代码
        //2010-8-5  zhanggang  重构函数
        //--------------------------------------------------------------------------        
        public void SetDefaultSelectedImage(int index)
        {
            //设置默认选中第一项
            if (PhotosListBox.Items.Count > 0 && PhotosListBox.Items.Count > index)
            {
                ListBoxItem firstItem = (ListBoxItem)(PhotosListBox.ItemContainerGenerator.ContainerFromItem(PhotosListBox.Items[index]));
                if (firstItem != null) { firstItem.Focus(); }
                PhotosListBox.SelectedIndex = index;
                PhotosListBox.ScrollIntoView(this.PhotosListBox.Items[index]);

            }
        }

        /// <summary>
        ///     通过控件本身向指定位置插入图片
        ///     <param name="selImgNames">需要插入的文件名列表（包括路径）</param>
        ///     <param name="postion">插入位置</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-10 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void AddImages(List<string> selImgNames, int postion)
        {
            logger.Debug("通过控件本身向指定位置插入图片处理开始。");

            //文件序号
            int serialID = 0;

            //1:清除永久选中状态
            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                int index = PhotosListBox.Items.IndexOf(item);
                SetFixedSelectedItem(false, index, item);
            }

            //2:插入图像文件
            foreach (string imageName in selImgNames)
            {
                if (!File.Exists(imageName))
                {
                    MessageBox.Show("图像不存在：" + imageName);
                    return;
                }
                if (!Util.IsImage(imageName))
                {
                    MessageBox.Show("图像无法正常读取，文件可能已损坏：" + imageName);
                    return;
                }
                //生成插入单个图片对象，并插入到图片对象集合指定位置
                string fileName = this.ConvertFile(imageName);
                FileMetaDataItem fileMetaData = new FileMetaDataItem(ImageWidth, fileName, 0, false, imageName);
                fileMetaData.OriginalName = imageName;
                fileMetaData.FileType = FileNameUtility.GetFileType(imageName);
                fileMetaData.Status = FileMetaDataStatus.A;


                FileMetaDataCollection.Insert(postion++, fileMetaData);
            }

            //3:更新序号
            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                serialID++;
                int index = PhotosListBox.Items.IndexOf(item);
                FileMetaDataCollection[index].LabelContent = (index + 1).ToString() + " " + FileMetaDataCollection[index].GetMetaField(BosField.descriptionName);
                FileMetaDataCollection[index].SerialID = serialID;
            }

            //4:默认选中缩略图列表中第一项
            SetDefaultSelectedImage(0);

            logger.Debug("通过控件本身向指定位置插入图片处理结束。");
        }

        /// <summary>
        ///     通过接口向指定位置追加图片
        ///     <param name="selImgNames">需要插入的文件名列表（包括路径）</param>
        ///     <param name="postion">插入位置</param>
        ///     <param name="isBased">是否作为基数据</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-10 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void AddImages(List<FileMetaData> selImgNames, int postion, bool isBased)
        {
            logger.Debug("通过接口向指定位置追加图片处理开始。");

            //文件序号
            int serialID = 0;

            //1:清除永久选中状态
            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                int index = PhotosListBox.Items.IndexOf(item);
                SetFixedSelectedItem(false, index, item);
            }
            //2:插入图像文件
            foreach (FileMetaData imageName in selImgNames)
            {
                if (!File.Exists(imageName.FileName))
                {
                    MessageBox.Show("图像不存在：" + imageName.FileName);
                    return;
                }
//                if (!Util.IsImage(imageName.FileName))
//                {
//                    MessageBox.Show("图像无法正常读取，文件可能已损坏：" + imageName.FileName);
//                    return;
//                }
                //生成插入单个图片对象，并插入到图片对象集合指定位置
                string OldName = null;
                if (imageName.OriginalName == null)
                {
                    OldName = imageName.FileName;
                }
                else
                {
                    OldName = imageName.OriginalName;
                }

                string fileName = this.ConvertFile(imageName.FileName);
                imageName.FileName = fileName;
                FileMetaDataItem fileMetaData = SetFileMetaDataItem(imageName, OldName);
                FileMetaDataCollection.Insert(postion++, fileMetaData);

                //如果作为基准图像,则添加入基准图像列表
                if (isBased)
                {
                    if (baseFileMetaDataList == null)
                        baseFileMetaDataList = new List<FileMetaDataItem>();
                    FileMetaDataItem baseFileMetaData = SetFileMetaDataItem(imageName, OldName);
                    baseFileMetaDataList.Add(baseFileMetaData);
                }
            }

            //3:更新序号
            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                serialID++;
                int index = PhotosListBox.Items.IndexOf(item);
                FileMetaDataCollection[index].LabelContent = (index + 1).ToString() + " " + FileMetaDataCollection[index].GetMetaField(BosField.descriptionName);
                FileMetaDataCollection[index].SerialID = serialID;
            }

            //4:默认选中缩略图列表中第一项
            SetDefaultSelectedImage(0);

            logger.Debug("通过接口向指定位置追加图片处理结束。");

        }
        /// <summary>
        ///     通过接口向指定位置插入图片
        ///     <param name="selImgNames">需要插入的文件名列表（包括路径）</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-10 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void InsertImages(List<string> selImgNames)
        {
            logger.Debug("通过接口向指定位置插入图片处理开始。");

            //1:仅在选择一个元素时,允许插入
            if (PhotosListBox.SelectedItems == null || PhotosListBox.SelectedItems.Count != 1)
            {
                MessageBox.Show("未选择或选择了多个元素作为插入点，只能选择一个元素作为插入点！");
                return;
            }
            //2:在指定位置插入图片
            AddImages(selImgNames, PhotosListBox.SelectedIndex);

            logger.Debug("通过接口向指定位置插入图片处理结束。");

        }

        /// <summary>
        ///     删除当前选择的图像列表
        ///     <param name="sender">待删除当前选择的图像列表</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-10 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void DeleteImages(List<FileMetaDataItem> listFileMetaDataItem)
        {
            logger.Debug("删除当前选择的图像列表处理开始。");

            //文件序号
            int serialID = 0;

            //1:更新缩略图
            List<FileMetaDataItem> temp = new List<FileMetaDataItem>();
            foreach (FileMetaDataItem item in listFileMetaDataItem)
            {
                item.BitmapImageSource = null;
                FileMetaDataCollection.Remove(item);
            }

            //2:更新序号
            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                serialID++;
                int index = PhotosListBox.Items.IndexOf(item);
                FileMetaDataCollection[index].LabelContent = (index + 1).ToString() + " " + FileMetaDataCollection[index].GetMetaField(BosField.descriptionName);
                FileMetaDataCollection[index].SerialID = serialID;
            }

            //3:触发删除图像事件
            RaiseImageThumbnailsEvent(ImageThumbnailEventType.DeleteImage);

            //4:默认选中缩略图列表中第一项
            SetDefaultSelectedImage(0);

            logger.Debug("删除当前选择的图像列表处理结束。");
        }

        /// <summary>
        ///     删除全部图片
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-10 qizhenguo  创建代码
        //--------------------------------------------------------------------------        
        public void DeleteAllImages()
        {
            logger.Debug("删除全部图片处理开始。");

            FileMetaDataCollection.Clear();
            //触发删除图像事件
            RaiseImageThumbnailsEvent(ImageThumbnailEventType.DeleteImage);

            logger.Debug("删除全部图片处理结束。");
        }

        /// <summary>
        /// 清除图像及重置内部数据结构
        /// </summary>
        public void Reset()
        {
            logger.Debug("清除状态开始。");

            FileMetaDataCollection.Clear();
            if (baseFileMetaDataList != null)
            {
                this.baseFileMetaDataList.Clear();
                this.baseFileMetaDataList = null;
            }

            //触发删除图像事件
            RaiseImageThumbnailsEvent(ImageThumbnailEventType.DeleteImage);

            logger.Debug("清除状态结束。");
        }

        /// <summary>
        ///     设置缩略图排列方向
        ///     <param name="ori">缩略图排列方向参数</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void SetOrientation(Orientation ori)
        {
            //横向排列
            if (ori == Orientation.Horizontal)
            {
                PhotosListBox.Style = (Style)this.FindResource("PhotoListBoxHorizontalStyle");
            }
            //纵向排列
            else
            {
                PhotosListBox.Style = (Style)this.FindResource("PhotoListBoxVerticalStyle");
            }
        }

        /// <summary>
        /// 获取选定的图像
        /// </summary>
        /// <returns>选定图像列表</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public List<string> GetSelectedItemFileName(ItemNameType type)
        {
            logger.Debug("获取选定的图像处理开始。");

            List<string> listFileMetaDataItemFileName = new List<string>();
            IList list = null;

            if (type == ItemNameType.AllItem)
                list = PhotosListBox.Items;
            else if (type == ItemNameType.SelectedItem)
                list = PhotosListBox.SelectedItems;

            foreach (FileMetaDataItem item in list)
            {
                listFileMetaDataItemFileName.Add(item.FileName);
            }

            logger.Debug("获取选定的图像处理结束。");

            return listFileMetaDataItemFileName;
        }

        /// <summary>
        /// 获取选定的图像集合对象列表
        /// </summary>
        /// <returns>选定图像集合对象列表</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-26 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public List<FileMetaDataItem> GetSelectedItemObject(ItemNameType type)
        {
            logger.Debug("获取选定的图像集合对象列表处理开始。");

            List<FileMetaDataItem> listFileMetaDataItem = new List<FileMetaDataItem>();
            IList list = null;

            if (type == ItemNameType.AllItem)
                list = PhotosListBox.Items;
            else if (type == ItemNameType.SelectedItem)
                list = PhotosListBox.SelectedItems;

            foreach (FileMetaDataItem item in list)
            {
                listFileMetaDataItem.Add(item);
            }

            logger.Debug("获取选定的图像集合对象列表处理结束。");

            return listFileMetaDataItem;
        }

        /// <summary>
        /// 查询当前选中的第一个图像
        /// </summary>
        /// <returns>当前选中的第一个图像</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public int GetSelectedIndex()
        {
            return PhotosListBox.SelectedIndex;
        }

        /// <summary>
        /// 获取选中图片个数
        /// </summary>
        /// <returns>选中图片个数</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------        
        public int GetSelectedItemCount()
        {
            return PhotosListBox.SelectedItems.Count;
        }

        /// <summary>
        /// 获取列表中图片个数
        /// </summary>
        /// <returns>列表中图片个数</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public int GetPhotosListBoxCount()
        {
            return PhotosListBox.Items.Count;
        }
        /// <summary>
        /// 获取元数据中指定索引的图像对象
        /// </summary>
        /// <param name="index">元数据中指定索引</param>
        /// <returns>元数据中指定索引的图像对象</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //2010-8-26 qizhenguo  修改函数返回值为图片对象
        //--------------------------------------------------------------------------
        public FileMetaDataItem GetSelectedImage(int index)
        {
            int cnt = FileMetaDataCollection.Count;
            if (index < 0 || index >= cnt)
                return null;
            else
                return FileMetaDataCollection[index];
        }

        /// <summary>
        /// 设置当前选定图像
        /// </summary>
        /// <param name="index">图像索引</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void SetSelectedIndex(int index)
        {
            int cnt = FileMetaDataCollection.Count;
            if (index >= 0 && index < cnt)
            {
                PhotosListBox.Focus();
                PhotosListBox.SelectedIndex = index;
                PhotosListBox.ScrollIntoView(PhotosListBox.SelectedItem);
            }
        }

        /// <summary>
        /// 保存旋转结果（此函数供容器控件调用）
        /// </summary>
        /// <param name="index">图片在缩略图中索引</param>
        /// <param name="degree">旋转角度</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void SaveRotation(int index, int degree)
        {
            logger.Debug("保存旋转结果处理开始。");
            int cnt = FileMetaDataCollection.Count;

            if (index >= 0 && index < cnt)
            {
                using (FileStream mse = new FileStream(FileMetaDataCollection[index].FileName, FileMode.Open, FileAccess.Read))
                {
                    BitmapImage bitImage = new BitmapImage();
                    bitImage.BeginInit();
                    bitImage.StreamSource = mse;
                    bitImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitImage.Rotation = InnerUtility.GetRotation(degree);
                    bitImage.EndInit();
                    bitImage.Freeze();
                    FileMetaDataCollection[index].BitmapImageSource = bitImage;
                }
                // 设置图像的状态为UR
                FileMetaDataCollection[index].Status = FileMetaDataStatus.R;
            }
            logger.Debug("保存旋转结果处理结束。");

        }

        /// <summary>
        /// 旋转图像
        /// </summary>
        /// <param name="index">图片在缩略图中索引</param>
        /// <param name="rotateDegree">旋转角度</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void RotateImage(int index, int rotateDegree)
        {
            logger.Debug("旋转图像处理开始。");

            int cnt = FileMetaDataCollection.Count;

            if (index >= 0 && index < cnt)
            {
                using (FileStream mse = new FileStream(FileMetaDataCollection[index].FileName, FileMode.Open, FileAccess.Read))
                {
                    BitmapImage bitImage = new BitmapImage();
                    bitImage.BeginInit();
                    bitImage.StreamSource = mse;
                    bitImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitImage.EndInit();
                    bitImage.Freeze();

                    FileMetaDataCollection[index].BitmapImageSource = bitImage;
                }

                // 设置图像的状态为UR
                FileMetaDataCollection[index].Status = FileMetaDataStatus.R;
            }

            logger.Debug("旋转图像处理结束。");
        }

        /// <summary>
        /// 设置图像的模板ID
        /// </summary>
        /// <param name="templateID">模板ID</param>
        /// <returns>是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool SetTemplateID(String templateID)
        {
            if (PhotosListBox.SelectedItems == null || PhotosListBox.SelectedItems.Count == 0)
            {
                return false;
            }

            foreach (FileMetaDataItem item in PhotosListBox.SelectedItems)
            {
                int index = PhotosListBox.Items.IndexOf(item);
                float x = 0;
                float y = 0;
                if (Util.CheckDPI(FileMetaDataCollection[index].FileName, out x, out y))
                {
                    FileMetaDataCollection[index].TemplateId = templateID;
                }
                else
                {
                    logger.Error(FileMetaDataCollection[index].FileName + "图像的文件头DPI与实际DPI不一致");
                    return false;
                }

            };
            return true;
        }

        /// <summary>
        /// 设置所有图像的描述名称
        /// </summary>
        /// <param name="descriptionNameList">描述名称列表</param>
        /// <returns>是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool SetDescriptionNametoAll(List<String> descriptionNameList)
        {
            if (FileMetaDataCollection.Count != descriptionNameList.Count)
            {
                return false;
            }

            for (int i = 0; i < FileMetaDataCollection.Count; i++)
            {
                FileMetaDataItem fileMetaData = FileMetaDataCollection[i];
                SetDescriptionName(fileMetaData, descriptionNameList[i]);
            }

            //触发图像更改选中事件,设置原图描述信息
            RaiseImageThumbnailsEvent(ImageThumbnailEventType.SelectionChange);

            return true;
        }

        /// <summary>
        /// 设置选定图像的描述名称
        /// </summary>
        /// <param name="descriptionName">描述名称</param>
        /// <returns>是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool SetDescriptionNametoSelectedImages(String descriptionName)
        {
            if (PhotosListBox.SelectedItems == null || PhotosListBox.SelectedItems.Count == 0)
            {
                return false;
            }
            foreach (FileMetaDataItem item in PhotosListBox.SelectedItems)
            {
                int index = PhotosListBox.Items.IndexOf(item);
                SetDescriptionName(FileMetaDataCollection[index], descriptionName);
            }

            //触发图像更改选中事件,设置原图描述信息
            RaiseImageThumbnailsEvent(ImageThumbnailEventType.SelectionChange);

            return true;
        }

        /// <summary>
        /// 按描述名称获取对应图像的索引
        /// </summary>
        /// <param name="descriptionName">描述名称</param>
        /// <returns>图像索引</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public int GetIndexByDescriptionName(String descriptionName)
        {
            for (int i = 0; i < FileMetaDataCollection.Count; i++)
            {
                if (FileMetaDataCollection[i].GetMetaField(BosField.descriptionName) == descriptionName)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// 按关键字获取对应的图像名称
        /// </summary>
        /// <param name="name">关键字</param>
        /// <returns>描述名称列表</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public List<string> GetImageNamesByDescriptionName(String name)
        {
            List<string> imageNameList = new List<string>();

            for (int i = 0; i < FileMetaDataCollection.Count; i++)
            {
                if (FileMetaDataCollection[i].GetMetaField(BosField.descriptionName).Contains(name))
                {
                    imageNameList.Add(FileMetaDataCollection[i].FileName);
                }
            }

            return imageNameList;
        }

        /// <summary>
        /// 从图像展示区删除所有图像，并清除基准图像
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void Dispose()
        {
            logger.Debug("从图像展示区删除所有图像开始。");
            foreach (FileMetaDataItem item in FileMetaDataCollection)
            {
                item.BitmapImageSource = null;
            }



            baseFileMetaDataList = null;
            BindingOperations.ClearBinding(PhotosListBox, ListBox.ItemsSourceProperty);
            BindingOperations.ClearAllBindings(this);

            ScrollView.MouseRightButtonDown -= new MouseButtonEventHandler(ScrollView_MouseRightButtonDown);
            ScrollView.ContextMenuOpening -= new ContextMenuEventHandler(ScrollView_ContextMenuOpening);

            PhotosListBox.MouseDoubleClick -= new MouseButtonEventHandler(ListBoxItem_MouseDoubleClick);
            PhotosListBox.SelectionChanged -= new SelectionChangedEventHandler(ListBoxItem_SelectionChanged);
            PhotosListBox.MouseRightButtonUp -= new MouseButtonEventHandler(ListBoxItem_MouseRightButtonUp);
            PhotosListBox.DragOver -= new DragEventHandler(ListBoxItem_DragOver);
            PhotosListBox.Drop -= new DragEventHandler(ListBoxItem_Drop);
            PhotosListBox.MouseMove -= new MouseEventHandler(ListBoxItem_MouseMove);
            PhotosListBox.PreviewMouseRightButtonDown -= new MouseButtonEventHandler(ListBoxItem_PreviewMouseRightButtonDown);
            PhotosListBox.PreviewMouseLeftButtonDown -= new MouseButtonEventHandler(ListBoxItem_PreviewMouseLeftButtonDown);
            PhotosListBox.PreviewKeyDown -= new KeyEventHandler(ListBoxItem_PreviewKeyDown);
            PhotosListBox.PreviewDragEnter -= new DragEventHandler(ListBoxItem_PreviewDragEnter);
            PhotosListBox.PreviewDragOver -= new DragEventHandler(ListBoxItem_PreviewDragOver);
            PhotosListBox.PreviewDragLeave -= new DragEventHandler(ListBoxItem_PreviewDragLeave);
            PhotosListBox.PreviewMouseLeftButtonUp -= new MouseButtonEventHandler(ListBoxItem_PreviewMouseLeftButtonUp);

            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                TextBox myTextbox = this.GetCurrentTextBoxObj("PhotoDescription") as TextBox;
                if (myTextbox != null)
                {
                    myTextbox.KeyDown -= new KeyEventHandler(PhotoDescription_KeyDown);
                    myTextbox.LostFocus -= new RoutedEventHandler(PhotoDescription_LostFocus);
                    myTextbox.GotFocus -= new RoutedEventHandler(PhotoDescription_GotFocus);
                }
            }
            FileMetaDataCollection.Clear();
            logger.Debug("从图像展示区删除所有图像结束。");
        }

        /// <summary>
        ///     获取当前选择的图像列表
        ///     <returns>当前选择的图像集合</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public List<FileMetaDataItem> GetSelectedItemList()
        {
            List<FileMetaDataItem> selImgList = new List<FileMetaDataItem>();

            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                if (item.FixedSelect == true)
                {
                    selImgList.Add(item);
                }
            }

            //选择为空判断
            if ((selImgList == null || selImgList.Count == 0) && (PhotosListBox.SelectedItems == null || PhotosListBox.SelectedItems.Count == 0))
            { return null; }

            foreach (FileMetaDataItem img in PhotosListBox.SelectedItems)
            {
                if (!selImgList.Contains(img))
                {
                    selImgList.Add(img);
                }
            }

            return selImgList;
        }

        /// <summary>
        ///     获取当前选择图像路径列表
        ///     <returns>当前选择图像路径列表</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public List<string> GetSelectedImageNameList()
        {
            List<string> selImgNameList = new List<string>();

            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                if (item.FixedSelect == true)
                {
                    selImgNameList.Add(item.FileName);

                }
            }


            //选择为空判断
            if ((selImgNameList == null || selImgNameList.Count == 0) && (PhotosListBox.SelectedItems == null || PhotosListBox.SelectedItems.Count == 0))
            { return null; }

            foreach (FileMetaDataItem img in PhotosListBox.SelectedItems)
            {
                if (!selImgNameList.Contains(img.FileName))
                {
                    selImgNameList.Add(img.FileName);
                }
            }

            return selImgNameList;
        }

        public List<FileMetaData> GetSelectedImageMetaList()
        {
            List<string> selImgNameList = new List<string>();
            List<FileMetaData> fMDList = new List<FileMetaData>();

            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                if (item.FixedSelect == true)
                {
                    FileMetaData f = new FileMetaData();
                    f.FileName = Path.GetFileNameWithoutExtension(item.OriginalName);
                    if (item.FileDesp != null)
                    {
                        f.FileDesp = item.FileDesp;
                    }
                    if (item.FileType != null)
                    {
                        f.FileType = item.FileType;
                    }
                    else
                    {
                        f.FileType = FileNameUtility.GetFileType(item.FileName);
                    }
                    if (item.TemplateId != null)
                    {
                        f.TemplateId = item.TemplateId;
                    }

                    fMDList.Add(f);
                    selImgNameList.Add(item.FileName);

                }
            }


            //选择为空判断
            if ((selImgNameList == null || selImgNameList.Count == 0) && (PhotosListBox.SelectedItems == null || PhotosListBox.SelectedItems.Count == 0))
            { return null; }

            foreach (FileMetaDataItem img in PhotosListBox.SelectedItems)
            {
                if (!selImgNameList.Contains(img.FileName))
                {
                    FileMetaData f = new FileMetaData();
                    f.FileName = Path.GetFileNameWithoutExtension(img.OriginalName);
                    if (img.FileDesp != null)
                    {
                        f.FileDesp = img.FileDesp;
                    }
                    if (img.FileDesp != null)
                    {
                        f.FileDesp = img.FileDesp;
                    }
                    if (img.FileType != null)
                    {
                        f.FileType = img.FileType;
                    }
                    else
                    {
                        f.FileType = FileNameUtility.GetFileType(img.FileName);
                    }
                    if (img.TemplateId != null)
                    {
                        f.TemplateId = img.TemplateId;
                    }
                    fMDList.Add(f);
                    selImgNameList.Add(img.FileName);
                }
            }

            return fMDList;
        }

        /// <summary>
        ///     获取全部图像ID列表
        ///     <returns>全部图像ID列表</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-3  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public List<string> GetAllImageIdList()
        {
            List<string> imageNameList = GetSelectedItemFileName(ItemNameType.AllItem);

            if (imageNameList == null || imageNameList.Count == 0)
            { return null; }

            List<string> imageIdList = new List<string>();
            foreach (string path in imageNameList)
            {
                string imageItem = Path.GetFileNameWithoutExtension(path);
                imageIdList.Add(imageItem);
            }

            return imageIdList;

        }

        /// <summary>
        ///     全选
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void SelectAll()
        {
            PhotosListBox.SelectAll();
        }

        /// <summary>
        ///     反选
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void DeSelect()
        {
            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                ListBoxItem myListBoxItem = (ListBoxItem)(PhotosListBox.ItemContainerGenerator.ContainerFromItem(item));
                myListBoxItem.IsSelected = !myListBoxItem.IsSelected;
            }
        }

        /// <summary>
        /// 获取当前图像展示区的所有图像的元数据列表
        /// <returns>当前图像展示区的所有图像的元数据列表</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public List<FileMetaData> GetAllImages()
        {
            if (baseFileMetaDataList == null)
            {
                return GetAllNewImages();
            }
            else
            {
                return GetAllUpdateImages();
            }
        }
        public FileMetaData GetSelectImageMeta()
        {
            List<FileMetaData> fileMetaDataList = GetSelectedImageMetaList();
            FileMetaData fileMetaData = new FileMetaData();
            for (int i = 0; i < fileMetaDataList.Count; i++)
            {
                fileMetaData.FileName = fileMetaDataList[i].FileName;
                fileMetaData.FileType = fileMetaDataList[i].FileType;
                fileMetaData.FileDesp = fileMetaDataList[i].FileDesp;
                fileMetaData.TemplateId = fileMetaDataList[i].TemplateId;
            }


            return fileMetaData;
        }
        public bool UpdateSelectImageMeta(FileMetaData fileMetaData)
        {
            bool isSucc = false;
            List<string> selImgNameList = new List<string>();
            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                if (item.FixedSelect == true)
                {
                    item.FileDesp = fileMetaData.FileDesp;
                    item.OriginalName = fileMetaData.FileName;
                    item.FileType = fileMetaData.FileType;
                    item.TemplateId = fileMetaData.TemplateId;
                }

            }


            //选择为空判断
            if ((selImgNameList == null || selImgNameList.Count == 0) && (PhotosListBox.SelectedItems == null || PhotosListBox.SelectedItems.Count == 0))
            { return isSucc; }

            foreach (FileMetaDataItem img in PhotosListBox.SelectedItems)
            {
                img._OriginalName = fileMetaData.FileName;
                img.FileType = fileMetaData.FileType;
                img.FileDesp = fileMetaData.FileDesp;
                img.TemplateId = fileMetaData.TemplateId;
            }
            return isSucc;
        }
        /// <summary>
        /// 判断缩略图控件中是否有图像已发生变化，包括追加、删除、替换和旋转
        /// </summary>
        /// <returns></returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期       修改人     修改
        //2011-12-21 dengqiong  创建代码
        //--------------------------------------------------------------------------
        public Boolean IsChanged()
        {
            List<FileMetaData> fileMetaDataList = this.GetAllImages();
            if (fileMetaDataList == null)
                return false;
            foreach (FileMetaData fileMetaData in fileMetaDataList)
            {
                if (fileMetaData.Status != FileMetaDataStatus.K)
                    return true;
            }
            return false;
        }
        #endregion

        #region 私有方法
        /// <summary>
        ///     新增情况下，获得当前图像展示区的所有图像列表
        /// <returns>当前图像展示区的所有图像列表</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-4  qizhenguo  创建代码
        //--------------------------------------------------------------------------  
        private List<FileMetaData> GetAllNewImages()
        {
            if (!PhotosListBox.HasItems)
            { return null; }

            List<FileMetaData> allNewImg = new List<FileMetaData>();

            for (int i = 0; i < FileMetaDataCollection.Count; i++)
            {
                FileMetaData meta = new FileMetaData();

                meta.FileName = FileMetaDataCollection[i].FileName;
                meta.SerialID = i + 1;
                meta.TemplateId = FileMetaDataCollection[i].TemplateId;
                meta.FileDesp = FileMetaDataCollection[i].FileDesp;
                meta.OriginalName = FileMetaDataCollection[i].OriginalName;
                meta.Status = FileMetaDataStatus.A;
                meta.FileType = FileNameUtility.GetFileType(meta.FileName);
                allNewImg.Add(meta);
            }
            return allNewImg;
        }

        /// <summary>
        ///     更新情况下，获得当前图像展示区的所有图像列表
        /// <returns>当前图像展示区的所有图像列表</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------  
        private List<FileMetaData> GetAllUpdateImages()
        {
            List<FileMetaData> allUpdateImg = new List<FileMetaData>();

            List<int> tempFlagToBase = new List<int>();

            // 初始化基础图像表标志
            // 0表示未处理，1表示已处理过
            for (int i = 0; i < baseFileMetaDataList.Count; i++)
            { tempFlagToBase.Add(0); }
            //            List<string> baseImageNameList = GetImageNameListFromFileMetaDataList(baseFileMetaDataList);
            List<string> basePageIdList = GetPageIdListFromFileMetaDataList(baseFileMetaDataList);
            for (int i = 0; i < FileMetaDataCollection.Count; i++)
            {
                FileMetaData meta = new FileMetaData();
                meta.FileName = FileMetaDataCollection[i].FileName;
                meta.OriginalName = FileMetaDataCollection[i].OriginalName;
                meta.Status = FileMetaDataStatus.K;
                //                meta.SerialID = i + 1;
                meta.SerialID = FileMetaDataCollection[i].SerialID;
                meta.TemplateId = FileMetaDataCollection[i].TemplateId;
                meta.PageId = FileMetaDataCollection[i].PageId;
                meta.FileDesp = FileMetaDataCollection[i].FileDesp;
                meta.FileType = FileMetaDataCollection[i].FileType;

                if (meta.PageId != null)
                {
                    int index = basePageIdList.IndexOf(Path.GetFileNameWithoutExtension(meta.PageId));

                    if (meta.SerialID != index + 1)
                    {
                        meta.Status = FileMetaDataStatus.CU;
                        meta.SerialID = i + 1;
                    }
                    //                    else
                    //                    {
                    //                        meta.serialID = 0;
                    //                    }
                    if (meta.OriginalName != baseFileMetaDataList[index]._OriginalName)
                    {
                        meta.Status = FileMetaDataStatus.CU;
                    }
                    if (meta.FileDesp != baseFileMetaDataList[index].FileDesp)
                    {
                        meta.Status = FileMetaDataStatus.CU;
                    }
                    if (meta.FileType != baseFileMetaDataList[index].FileType)
                    {
                        meta.Status = FileMetaDataStatus.CU;
                    }
                    if (baseFileMetaDataList[index].TemplateId != null && meta.TemplateId != baseFileMetaDataList[index].TemplateId)
                    {
                        meta.Status = FileMetaDataStatus.CU;
                    }
                    tempFlagToBase[index] = 1;

                    foreach (FileMetaDataItem tmpMata in FileMetaDataCollection)
                    {
                        if (tmpMata.FileName == meta.FileName && tmpMata.Status == FileMetaDataStatus.R)
                        {
                            meta.Status = FileMetaDataStatus.R;
                        }
                    }
                }
                else
                {
                    meta.Status = FileMetaDataStatus.A;
                }
                //                meta.FileType = FileNameUtility.GetFileType(meta.FileName);
                allUpdateImg.Add(meta);
            }
            for (int i = 0; i < tempFlagToBase.Count; i++)
            {
                if (tempFlagToBase[i] == 1)
                    continue;

                FileMetaData meta = new FileMetaData();
                //                meta.FileName = baseImageNameList[i];
                meta.FileName = baseFileMetaDataList[i].FileName;
                meta.OriginalName = baseFileMetaDataList[i].OriginalName;
                meta.Status = FileMetaDataStatus.D;
                meta.FileDesp = baseFileMetaDataList[i].FileDesp;
                meta.SerialID = baseFileMetaDataList[i].SerialID;
                meta.PageId = baseFileMetaDataList[i].PageId;
                meta.TemplateId = baseFileMetaDataList[i].TemplateId;
                meta.FileType = baseFileMetaDataList[i].FileType;
                allUpdateImg.Add(meta);
            }

            return allUpdateImg;

        }

        /// <summary>
        ///     从元数据获取文件名列表
        /// <returns>文件名列表</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------  
        private List<string> GetImageNameListFromFileMetaDataList(List<FileMetaDataItem> fileMetaDataList)
        {
            if (fileMetaDataList == null)
            {
                return null;
            }
            List<string> imageNameList = new List<string>();
            if (fileMetaDataList.Count == 0)
            {
                return imageNameList;
            }

            foreach (FileMetaDataItem fileMetaData in fileMetaDataList)
            {
                string imageName = Path.GetFileNameWithoutExtension(fileMetaData._OriginalName);
                imageNameList.Add(imageName);
            }

            return imageNameList;
        }
        /// <summary>
        ///     从元数据获取页文件名ID列表
        /// <returns>页文件ID列表</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2015-7-2 xuhang  创建代码
        //--------------------------------------------------------------------------  
        private List<string> GetPageIdListFromFileMetaDataList(List<FileMetaDataItem> fileMetaDataList)
        {
            if (fileMetaDataList == null)
            {
                return null;
            }
            List<string> pageIdList = new List<string>();
            if (fileMetaDataList.Count == 0)
            {
                return pageIdList;
            }

            foreach (FileMetaDataItem fileMetaData in fileMetaDataList)
            {
                string pageId = fileMetaData.PageId;
                pageIdList.Add(pageId);
            }

            return pageIdList;
        }

        /// <summary>
        ///     获取永久选中状态列表
        /// <returns>永久选中状态列表</returns>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-4  qizhenguo  创建代码
        //--------------------------------------------------------------------------          
        private List<FileMetaDataItem> GetFixedSelectedItem()
        {
            List<FileMetaDataItem> fixedFileMetaDataItem = new List<FileMetaDataItem>();

            foreach (FileMetaDataItem item in PhotosListBox.Items)
            {
                if (item.FixedSelect == true)
                { fixedFileMetaDataItem.Add(item); }
            }

            return fixedFileMetaDataItem;
        }

        /// <summary>
        ///     设置永久选中状态
        ///     <param name="setFlag">设置或取消标志</param>
        ///     <param name="index">当前索引</param>
        ///     <param name="setFlag">当前图片对象</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-6  qizhenguo  创建代码
        //--------------------------------------------------------------------------          
        private void SetFixedSelectedItem(bool setFlag, int index, FileMetaDataItem item)
        {
            //设置永久选中状态
            if (setFlag)
            {
                if (item.FixedSelect == false)
                {
                    FileMetaDataCollection[index].FixedSelect = true;
                    FileMetaDataCollection[index].LabelForeground = Brushes.Red;
                    FileMetaDataCollection[index].LabelBackground = Brushes.White;
                }
            }
            //取消永久选中状态
            else
            {
                if (item.FixedSelect == true)
                {
                    FileMetaDataCollection[index].FixedSelect = false;
                    FileMetaDataCollection[index].LabelForeground = Brushes.Black;
                    FileMetaDataCollection[index].LabelBackground = Brushes.White;
                }
            }
        }

        /// <summary>
        ///获取当前控件对象
        ///     <param name="textBoxName">对象控件名称</param>
        ///     <param name="TextBox">当前控件对象</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-6  qizhenguo  创建代码
        //--------------------------------------------------------------------------    
        private object GetCurrentTextBoxObj(string textBoxName)
        {
            ListBoxItem myListBoxItem = (ListBoxItem)(PhotosListBox.ItemContainerGenerator.ContainerFromItem(PhotosListBox.Items.CurrentItem));
            ContentPresenter myContentPresenter = FindVisualChild<ContentPresenter>(myListBoxItem);
            DataTemplate myDataTemplate = myContentPresenter.ContentTemplate;
            return myDataTemplate.FindName(textBoxName, myContentPresenter);
        }

        /// <summary>
        ///寻找控件子节点
        ///     <param name="myTextbox">依赖对象</param>
        ///     <param name="childItem">子节点</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-6  qizhenguo  创建代码
        //--------------------------------------------------------------------------    
        private ChildItem FindVisualChild<ChildItem>(DependencyObject obj) where ChildItem : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is ChildItem)
                    return (ChildItem)child;
                else
                {
                    ChildItem childOfChild = FindVisualChild<ChildItem>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        /// <summary>
        /// 设置单个图像的描述名称
        /// </summary>
        /// <param name="fileMetaData">图像的描述信息</param>
        /// <param name="descriptionName">描述名称</param>
        /// <returns>是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-6  qizhenguo  创建代码
        //--------------------------------------------------------------------------    
        private bool SetDescriptionName(FileMetaDataItem fileMetaDataItem, String descriptionName)
        {
            int index = FileMetaDataCollection.IndexOf(fileMetaDataItem);
            if (index == -1)
            {
                return false;
            }
            FileMetaDataCollection[index].SetMetaField(BosField.descriptionName, descriptionName);
            FileMetaDataCollection[index].LabelContent = FileMetaDataCollection[index].SerialID.ToString() + " " + descriptionName;
            return true;
        }

        /// <summary>
        /// 设置单个图像的绑定元数据信息
        /// </summary>
        /// <param name="fileMetaData">元数据</param>
        /// <returns>绑定元数据</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-13 qizhenguo  创建代码
        //--------------------------------------------------------------------------    
        private FileMetaDataItem SetFileMetaDataItem(FileMetaData fileMetaData, string oldName)
        {
            string fileName = fileMetaData.FileName;

            FileMetaDataItem newFileMetaData = new FileMetaDataItem(ImageWidth, fileName, 0, false, oldName);

            newFileMetaData.SerialID = fileMetaData.SerialID;
            newFileMetaData.FileName = fileName;
            newFileMetaData.FileType = fileMetaData.FileType;
            newFileMetaData.Status = fileMetaData.Status;
            newFileMetaData.PageId = fileMetaData.PageId;
            newFileMetaData.TemplateId = fileMetaData.TemplateId;
            newFileMetaData.FileDesp = fileMetaData.FileDesp;
            newFileMetaData._OriginalName = oldName;
            return newFileMetaData;
        }
        /// <summary>
        /// 将文件列表中任意路径下的文件转化为展示控件指定工作目录下的文件
        /// </summary>
        /// <param name="srcfilePath">元数据文件路径（包括文件名）</param>
        /// <returns>指定工作目录下的文件路径（包括文件名）</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-18 qizhenguo  创建代码
        //2010-8-19 zhanggang  修改可能正确复制文件，但返回空名字的bug
        //-------------------------------------------------------------------------- 
        public string ConvertFile(string srcfilePath)
        {
            //指定工作目录下的文件路径
            string dstfilePath = null;
            //基础工作路径
            string basPath = Config.GetImageDir();
            //UUID 验证规则
            Regex regex = new Regex(@"^(\{){0,1}[0-9a-fA-F]{8}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{4}\-[0-9a-fA-F]{12}(\}){0,1}$", RegexOptions.Compiled);

            string srcName = Path.GetFileNameWithoutExtension(srcfilePath);
            //1：如果是UUID形式的
            if (regex.IsMatch(srcName))
            {
                // 1.1如果是在IMAGE目录下则不作移动操作
                if ((Path.GetDirectoryName(srcfilePath) + Path.DirectorySeparatorChar).Equals(basPath))
                {
                    dstfilePath = srcfilePath;
                }
                //1.2否则,移动到image目录下
                else
                {
                    //1.2.1如果目录下已存在同名文件，则需要进行改名
                    if (File.Exists(basPath + Path.GetFileName(srcfilePath)))
                    {
                        dstfilePath = basPath + Guid.NewGuid() + Path.GetExtension(srcfilePath);
                    }
                    //1.2.2如果不存在，则直接复制过去
                    else
                    {
                        dstfilePath = basPath + Path.GetFileName(srcfilePath);
                    }
                    //1.3：移动文件
                    try
                    {
                        File.Copy(srcfilePath, dstfilePath);
//                        CopyFile(srcfilePath, dstfilePath);
                    }
                    catch (Exception e)
                    {
                        logger.Error("Exception occured when copy file from " + srcfilePath + " to " + dstfilePath + ":" + e.Message);
                    }
                }
            }
            //2不是UUID形式
            else
            {
                //2.1重命名
                dstfilePath = basPath + Guid.NewGuid() + Path.GetExtension(srcfilePath);
                //2.2再移动
                try
                {
                    File.Copy(srcfilePath, dstfilePath);
//                    CopyFile(srcfilePath, dstfilePath);
                }
                catch (Exception e)
                {
                    logger.Error("Exception occured when copy file from " + srcfilePath + " to " + dstfilePath + ":" + e.Message);
                }
            }
            return dstfilePath;
        }

        /// <summary>
        /// 拖拽时，根据鼠标位置判断元素的插入位置
        /// </summary>
        /// <param name="DragEventArgs"></param>
        /// <returns></returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //--------------------------------------------------------------------------  
        private void DecideDropTarget(DragEventArgs e)
        {
            int targetItemsControlCount = this.PhotosListBox.Items.Count;
            if (targetItemsControlCount > 0)
            {
                this.hasVerticalOrientation = InnerUtility.HasVerticalOrientation(this.PhotosListBox.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement);
                this.targetItemContainer = PhotosListBox.ContainerFromElement((DependencyObject)e.OriginalSource) as FrameworkElement;

                if (this.targetItemContainer != null)
                {
                    Point positionRelativeToItemContainer = e.GetPosition(this.targetItemContainer);
                    this.isInFirstHalf = InnerUtility.IsInFirstHalf(this.targetItemContainer, positionRelativeToItemContainer, this.hasVerticalOrientation);
                    this.insertionIndex = this.PhotosListBox.ItemContainerGenerator.IndexFromContainer(this.targetItemContainer);

                    if (!this.isInFirstHalf)
                    {
                        this.insertionIndex++;
                    }
                }
                else
                {
                    this.targetItemContainer = this.PhotosListBox.ItemContainerGenerator.ContainerFromIndex(targetItemsControlCount - 1) as FrameworkElement;
                    this.isInFirstHalf = false;
                    this.insertionIndex = targetItemsControlCount;
                }
            }
            else
            {
                this.targetItemContainer = null;
                this.insertionIndex = 0;
            }
        }

        /// <summary>
        /// 拖拽时，生成插入位置标识
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        private void CreateInsertionAdorner()
        {
            if (this.targetItemContainer != null)
            {
                var adornerLayer = AdornerLayer.GetAdornerLayer(this.targetItemContainer);
                this.insertionAdorner = new InsertionAdorner(this.hasVerticalOrientation, this.isInFirstHalf, this.targetItemContainer, adornerLayer);
            }
        }

        /// <summary>
        /// 拖拽时，更新插入位置标识
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        private void UpdateInsertionAdornerPosition()
        {
            if (this.insertionAdorner != null)
            {
                this.insertionAdorner.IsInFirstHalf = this.isInFirstHalf;
                this.insertionAdorner.InvalidateVisual();
            }
        }

        /// <summary>
        /// 拖拽时，移除插入位置标识
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2011-3-31 qizhenguo  创建代码
        //-------------------------------------------------------------------------- 
        private void RemoveInsertionAdorner()
        {
            if (this.insertionAdorner != null)
            {
                this.insertionAdorner.Detach();
                this.insertionAdorner = null;
            }
        }
        #endregion

        private void ButtonEditMeta_Click(object sender, RoutedEventArgs e)
        {
            FileMetaData fMD = new FileMetaData();
            fMD = GetSelectImageMeta();
            EditPageMetaWindow editPageMetaWnd = new EditPageMetaWindow(new MyInvoke(UpdateSelectImageMeta));
            editPageMetaWnd.textBoxName.Text = fMD.FileName;
            editPageMetaWnd.textBoxFileType.Text = fMD.FileType;
            editPageMetaWnd.textBoxDesp.Text = fMD.FileDesp;
            editPageMetaWnd.textBoxTemplateID.Text = fMD.TemplateId;
            editPageMetaWnd.ShowDialog();

        }
        public void setMenuItem(bool flag)
        {
            if (flag)
            {
                CMenuDeleteAll.IsEnabled = true;
                CMenuDelete.IsEnabled = true;
                CMenuUpdate.IsEnabled = true;
                CMenuInsert.IsEnabled = true;
            }
            else
            {
                CMenuDeleteAll.IsEnabled = false;
                CMenuDelete.IsEnabled = false;
                CMenuUpdate.IsEnabled = false;
                CMenuInsert.IsEnabled = false;
            }
        }
        public static bool CopyFile(string src, string des)
        {
            bool re = false;
            FileStream fsRead = null;
            FileStream fsWrite = null;
            try
            {
                fsRead = new FileStream(src, FileMode.Open, FileAccess.Read);
                fsWrite = new FileStream(des, FileMode.Create, FileAccess.Write);
                byte[] bteData = new byte[12 * 1024 * 1024];
                int r = fsRead.Read(bteData, 0, bteData.Length);
                while (r > 0)
                {
                    fsWrite.Write(bteData, 0, r);
                    double d = 100 * (fsWrite.Position / (double)fsRead.Length);
                    r = fsRead.Read(bteData, 0, bteData.Length);
                }
                re = true;
                return re;
            }
            catch (Exception e)
            {
                return re;
            }
            finally
            {
                fsRead.Flush();
                fsWrite.Flush();
                fsRead.Close();
                fsWrite.Close();
            }
        }


    }
    /// <summary>
    /// 该类是一个文件的元数据（向下兼容而保留）
    /// </summary>
    public class FileMetaDataItem : INotifyPropertyChanged
    {
        //定义事件
        public event PropertyChangedEventHandler PropertyChanged;

        #region 无参构造函数
        /// <summary>
        ///  构造函数
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public FileMetaDataItem()
        { }
        #endregion

        #region 带参构造函数
        /// <summary>
        ///  构造函数
        ///     <param name="imageName">图像名称（带路径）</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //2010-8-4  zhanggang  优化代码
        //--------------------------------------------------------------------------
        public FileMetaDataItem(int imageHeight, string fileName, int labelContent, bool fixedSelectedFlag, string tooltip)
        {
            this.ImageWidth = imageHeight;
            this.FileName = fileName;
            this.labelContent = labelContent.ToString();
            this.FixedSelect = fixedSelectedFlag;
            //this.serialID = labelContent;
            this.ImageToolTip = Path.GetFileName(tooltip);
        }
        #endregion

        #region 属性字段
        //文件名属性（带路径）
        private string fileName;
        public string FileName
        {
            get
            {
                return fileName;
            }

            set
            {
                fileName = value;
                using (FileStream mse = new FileStream(value, FileMode.Open, FileAccess.Read))
                {
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.StreamSource = mse;
                    image.DecodePixelWidth = 100;
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.EndInit();
                    image.Freeze();
                    BitmapImageSource = image;
                }

            }
        }
        //文件序号属性
        private int serialID;
        public int SerialID
        {
            get
            {
                return serialID;
            }
            set
            {
                serialID = value;
                //调用属性更新通知事件
                OnPropertyChanged("SerialID");
            }
        }
        //文件元数据状态属性
        public FileMetaDataStatus Status { get; set; }
        //文件类型属性
        public string FileType { get; set; }
        //模板ID属性
        public string TemplateId { get; set; }
        public string PageId { get; set; }
        public string FileDesp { get; set; }
        //元数据域
        private Dictionary<string, string> metaFields = new Dictionary<string, string>();
        //元数据域属性
        public Dictionary<string, string> MetaFields
        {
            get { return metaFields; }
            set { metaFields = value; }
        }

        //图片高度属性
        private int imageHeight = 100;
        public int ImageWidth
        {
            get
            {
                return imageHeight;
            }
            set
            {
                imageHeight = value;
            }
        }
        //标签内容属性
        private string labelContent;
        public string LabelContent
        {
            get
            {
                return labelContent;
            }
            set
            {
                labelContent = value;
                //调用属性更新通知事件
                OnPropertyChanged("LabelContent");
            }

        }
        //标签字体颜色属性
        private SolidColorBrush labelForeground = Brushes.Black;
        public SolidColorBrush LabelForeground
        {
            get
            {
                return labelForeground;
            }
            set
            {
                labelForeground = value;
                //调用属性更新通知事件
                OnPropertyChanged("LabelForeground");
            }

        }
        //标签背景颜色属性
        private SolidColorBrush labelBackground = Brushes.White;
        public SolidColorBrush LabelBackground
        {
            get
            {
                return labelBackground;
            }
            set
            {
                labelBackground = value;
                //调用属性更新通知事件
                OnPropertyChanged("LabelBackground");
            }

        }
        //永久选中状态属性
        public bool FixedSelect { get; set; }
        //图片toolTip
        public string ImageToolTip { get; set; }
        //图片数据对象属性
        private BitmapImage bitmapImageSource;
        public BitmapImage BitmapImageSource
        {
            get
            {
                return bitmapImageSource;
            }
            set
            {
                bitmapImageSource = value;
                //调用属性更新通知事件
                OnPropertyChanged("BitmapImageSource");
            }
        }

        public string _OriginalName;
        public string OriginalName
        {
            set
            {
                _OriginalName = value;
                LabelContent = SerialID.ToString() + " " + value;
            }
            get
            {
                return _OriginalName;
            }
        }
        #endregion

        #region 设置文件元数据的扩展字段
        /// <summary>
        /// 设置文件元数据的扩展字段
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="fieldValue">字段值</param>
        /// <returns></returns>
        public bool SetMetaField(string fieldName, string fieldValue)
        {
            metaFields[fieldName] = fieldValue;
            return true;
        }
        #endregion

        #region 查询文件元数据的扩展字段
        /// <summary>
        /// 查询文件元数据的扩展字段
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <returns></returns>
        public string GetMetaField(string fieldName)
        {
            string str = null;
            if (metaFields.ContainsKey(fieldName))
                str = metaFields[fieldName];

            return str;
        }
        #endregion

        #region 属性改变通知事件
        /// <summary>
        /// 属性改变通知事件
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <returns></returns>
        protected void OnPropertyChanged(string name)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(name));
            }
        }
        #endregion
    }
    /// <summary>
    /// 该类是一个文件的元数据集合
    /// </summary>
    public class FileMetaDataCollection : ObservableCollection<FileMetaDataItem>
    {
        #region 无参构造函数
        /// <summary>
        ///  构造函数
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public FileMetaDataCollection()
        { }
        #endregion

        #region 替换指定元素
        /// <summary>
        ///  替换指定元素
        ///     <param name="selImgNames">待替换元素的从零开始的索引</param>
        ///     <param name="postion">位于指定索引处的元素的新值</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-11 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public new void SetItem(int index, FileMetaDataItem item)
        {
            base.SetItem(index, item);
        }

        #region 从文件名列表或得文件元数据列表
        /// <summary>
        /// 从文件名列表或得文件元数据列表
        /// </summary>
        /// <param name="imageNameList">文件名列表</param>
        /// <returns>文件元数据列表</returns> 
        public static FileMetaDataCollection GetFileMetaDataListFromImageNameList(List<string> imageNameList)
        {
            //文件序号
            int serialID = 0;

            //文件名列表为空判断
            if (imageNameList == null || imageNameList.Count == 0)
            {
                return null;
            }

            FileMetaDataCollection fileMetaDataCollection = new FileMetaDataCollection();

            foreach (string imageName in imageNameList)
            {
                serialID++;

                FileMetaDataItem fileMetaData = new FileMetaDataItem();

                fileMetaData.FileName = imageName;
                fileMetaData.SerialID = serialID;

                fileMetaDataCollection.Add(fileMetaData);
            }

            return fileMetaDataCollection;
        }

        #endregion
        #endregion
    }
    public static class BosField
    {
        public const string descriptionName = "descriptionName";
    }

}
