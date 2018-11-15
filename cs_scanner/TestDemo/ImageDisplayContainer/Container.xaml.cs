//==========================================================================
//   Copyright(C) 2010,AGRICULTURAL BANK OF CHINA,Corp.All rights reserved 
//==========================================================================
//--------------------------------------------------------------------------
//程序名: ThumbnailImage
//功能: 图像展示控件
//作者: qizhenguo
//创建日期: 2010-8-4
//--------------------------------------------------------------------------
//修改历史:
//日期      修改人     修改
//2010-8-4  qizhenguo  创建代码
//--------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using UFileClient.Common;
using log4net;
using System.IO;

namespace UFileClient.Display
{
    /// <summary>
    /// ExplorerImage.xaml 的交互逻辑
    /// </summary>
    public partial class ExplorerImage : UserControl
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
        static ExplorerImage()
        {
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.GetConfigDir() + ConstCommon.logConfigFileName));
        }
        #endregion

        #region 共有属性
        //是否隐藏缩略图控件
        private bool isThumbHidden = false;
        public bool IsThumbHidden
        {
            get
            {
                return isThumbHidden;
            }
            set
            {
                isThumbHidden = value;
            }
        }

        //图像显示类型
        private ImageDisplayType displayType = ImageDisplayType.Adaptable;
        public ImageDisplayType ImageDisplayType
        {
            set
            {
                displayType = value;
                images.ImageDisplayType = value;
            }

            get
            {
                return displayType;
            }
        }

        // 缩略图和原图控件聚合方式
        public AggregativeType AggregativeType
        {
            set
            {
                //container方式组合
                if (value == AggregativeType.Container)
                {
                    images.Thumbnail = thumbs;
                }
                else if (value == AggregativeType.PopUP)
                {
                    thumbnailColumn.Width = new GridLength(containerCtrl.Width - 20);
                    splitterColumn.Width = new GridLength(0);
                    imageColumn.Width = new GridLength(0);
                    thumbs.SetOrientation(Orientation.Horizontal);
                }
                images.AggregativeType = value;
                thumbs.AggregativeType = value;
            }
            get
            {
                return AggregativeType;
            }
        }
        #endregion

        #region 构造函数
        /// <summary>
        ///  构造函数
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-4  qizhenguo  创建代码
        //2010-8-5  zhanggang  添加单图展示控件响应事件
        //2011-9-1  dengqiong  初始化时设置单图控件所有按钮不可用
        //--------------------------------------------------------------------------        
        public ExplorerImage()
        {
            InitializeComponent();

            //挂载缩略图控件事件
            thumbs.ImageThumbnailEventHandler += new RoutedEventHandler(Thumbs_ImageThumbnailEventHandler);

            AggregativeType = AggregativeType.Container;
            images.ImageDisplayType = displayType;
            images.AggregativeType = AggregativeType.Container;
            thumbs.AggregativeType = AggregativeType.Container;

            images.SetAllButtonEnable(false);
        }
        #endregion

        #region 缩略图控件路由事件处理
        /// <summary>
        ///     缩略图控件路由事件处理
        ///     <param name="sender">调用对象</param>
        ///     <param name="e">参数信息</param>
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //2010-8-5  zhanggang  新增打开窗口的代码
        //--------------------------------------------------------------------------
        private void Thumbs_ImageThumbnailEventHandler(object sender, RoutedEventArgs e)
        {
            logger.Debug("缩略图控件路由事件处理开始。");

            //获取操作类型
            ImageThumbnailEventType type = (ImageThumbnailEventType)e.OriginalSource;

            switch (type)
            {
                //缩略图控件的删除图像
                case ImageThumbnailEventType.DeleteImage:
                    ImageThumbnailDeleteImage();
                    break;

                //缩略图控件的更改图像选择
                case ImageThumbnailEventType.SelectionChange:
                    ImageThumbnailSelectionChange();
                    break;
                //当以弹出方式组合时的打开原图展示窗口
                case ImageThumbnailEventType.OpenSingleImageWindow:
                    OpenSingleImageWindow();
                    break;
                default:
                    break;
            }

            logger.Debug("缩略图控件路由事件处理结束。");

        }

        /// <summary>
        ///     缩略图控件的更改图像选择
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-4  qizhenguo  创建代码
        //--------------------------------------------------------------------------
        private void ImageThumbnailSelectionChange()
        {
            logger.Debug("缩略图控件的更改图像选择处理事件开始。");

            List<FileMetaDataItem> listFileMetaDataItem = thumbs.GetSelectedItemObject(ItemNameType.SelectedItem);

            //选中一张缩略图
            if ((listFileMetaDataItem != null) && (listFileMetaDataItem.Count == 1))
            {
                SetSingleImage(listFileMetaDataItem[0].FileName, thumbs.GetSelectedIndex(), listFileMetaDataItem[0].GetMetaField(BosField.descriptionName));
            }

            logger.Debug("缩略图控件的更改图像选择处理事件结束。");

        }

        /// <summary>
        ///     缩略图控件的删除图像
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-2  qizhenguo  创建代码
        //2011-9-1  dengqiong  设置按钮不可用时传入的参数应为false
        //--------------------------------------------------------------------------
        private void ImageThumbnailDeleteImage()
        {
            logger.Debug("缩略图控件的删除图像处理事件开始。");

            //获取缩略图列表中图片个数
            int photosListBoxCount = thumbs.GetPhotosListBoxCount();
            //若图片个数为0，清空单个图片控件中的图片,并设置按钮不可用
            if (photosListBoxCount == 0)
            {
                images.Dispose();
                images.SetAllButtonEnable(false);
            }

            logger.Debug("缩略图控件的删除图像处理事件结束。");

        }
        /// <summary>
        ///    打开原图展示窗口
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-6  zhanggang  创建代码
        //--------------------------------------------------------------------------
        private void OpenSingleImageWindow()
        {
            logger.Debug("打开原图展示窗口处理事件开始。");

            List<FileMetaDataItem> listFileMetaDataItem = thumbs.GetSelectedItemObject(ItemNameType.SelectedItem);

            //选中一张缩略图
            if ((listFileMetaDataItem != null) && (listFileMetaDataItem.Count == 1))
            {
                SingleImageWindow singleImageWin = new SingleImageWindow();
                singleImageWin.SingeImage.SetDisplayedImage(listFileMetaDataItem[0].FileName, thumbs.GetSelectedIndex(), listFileMetaDataItem[0].GetMetaField(BosField.descriptionName));
                singleImageWin.Container = this;
                singleImageWin.ShowDialog();
            }
            else
            {
                MessageBox.Show("只能双击选择一个图像进行预览");
            };

            logger.Debug("打开原图展示窗口处理事件结束。");

        }
        #endregion

        #region 设置原图展示图像
        /// <summary>
        /// 设置原图展示的图像
        /// </summary>
        /// <param name="fileName">图片文件路径</param>
        /// <param name="index">图片文件在缩略图中的索引</param>
        /// <param name="description">图片文件描述信息</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-6  zhanggang  创建代码
        //2010-8-12 qizhenguo  修改按钮设置
        //--------------------------------------------------------------------------
        private void SetSingleImage(string fileName, int index, string description)
        {
            logger.Debug("设置原图展示图像处理开始。");

            images.SetDisplayedImage(fileName, index, description);
            thumbs.SetSelectedIndex(index);
            int cnt = thumbs.GetPhotosListBoxCount();

            images.SetAllButtonEnable(true);

            if (index == 0)
            {
                images.SetButtonEnable(ButtonType.PreviousPageButton, false);
            }
            else if (index == (cnt - 1))
            {
                images.SetButtonEnable(ButtonType.NextPageButton, false);
            }

            logger.Debug("设置原图展示图像处理结束。");
        }
        #endregion

        #region 设置可编辑标志
        /// <summary>
        /// 设置可编辑标志
        /// </summary>
        /// <param name="isEnable">是否可编辑</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void SetEditable(bool isEnable)
        {
            logger.Debug("设置可编辑标志处理开始。");

            thumbs.ContextMenuEditbale = isEnable;

            logger.Debug("设置可编辑标志处理结束。");
        }
        #endregion

        #region 接口方法
        /// <summary>
        /// 设置选中图像的模板ID
        /// </summary>
        /// <param name="templateID">模板ID</param>
        /// <returns>设置是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool SetTemplateIDtoSelectedImages(String templateID)
        {
            return thumbs.SetTemplateID(templateID);
        }

        /// <summary>
        /// 设置所有图像的描述名称
        /// </summary>
        /// <param name="descriptionNameList">描述名称列表</param>
        /// <returns>设置是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool SetDescriptionNametoAll(List<String> descriptionNameList)
        {
            if (descriptionNameList == null || descriptionNameList.Count == 0)
            {
                return false;
            }
            return thumbs.SetDescriptionNametoAll(descriptionNameList);
        }

        /// <summary>
        /// 设置选定图像的描述名称
        /// </summary>
        /// <param name="descriptionName">描述名称</param>
        /// <returns>设置是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool SetDescriptionNametoSelectedImages(String descriptionName)
        {
            return thumbs.SetDescriptionNametoSelectedImages(descriptionName);
        }

        /// <summary>
        /// 根据描述名称显示图像
        /// </summary>
        /// <param name="descriptionName">描述名称</param>
        /// <returns>操作是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool ShowImageByDescriptionName(String descriptionName)
        {
            if (String.IsNullOrEmpty(descriptionName))
            {
                return false;
            }
            int index = thumbs.GetIndexByDescriptionName(descriptionName);
            if (index != -1)
            {
                images.SetDisplayedImage(thumbs.GetSelectedImage(index).FileName, index, descriptionName);
                return true;
            }
            return false;
        }

        /// <summary>
        /// 根据描述名称过滤图像
        /// </summary>
        /// <param name="descriptionName">描述名称</param>
        /// <returns>操作是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool FilterImagesByDescritionName(String descriptionName)
        {
            if (String.IsNullOrEmpty(descriptionName))
            { return false; }
            List<string> imageNameList = thumbs.GetImageNamesByDescriptionName(descriptionName);

            if (imageNameList == null || imageNameList.Count == 0)
            { return false; }

            thumbs.DeleteAllImages();
            thumbs.AddImages(imageNameList, 0);

            return true;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void Dispose()
        {
        }

        /// <summary>
        /// 追加图像，带元数据信息
        /// </summary>
        /// <param name="imageList">带元数据信息的图像列表</param>
        /// <param name="isBased">基准图像标志</param>
        /// <returns>操作是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool AddImageList(List<FileMetaData> imageList, bool isBased)
        {
            if (imageList == null || imageList.Count == 0)
            { return false; }

            int PhotosListBoxCount = thumbs.GetPhotosListBoxCount();
            thumbs.AddImages(imageList, PhotosListBoxCount, isBased);

            return true;
        }

        /// <summary>
        /// 追加图像，不带元数据信息
        /// </summary>
        /// <param name="imageNameList">图像列表</param>
        /// <returns>操作是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool AddImageList(List<string> imageNameList)
        {
            return AddImageList(imageNameList, false);
        }

        /// <summary>
        /// 追加图像
        /// </summary>
        /// <param name="imageNameList">图像列表</param>
        /// <param name="isBased">是否基准图像</param>
        /// <returns>操作是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool AddImageList(List<string> imageNameList, bool isBased)
        {
            if (imageNameList == null || imageNameList.Count == 0)
            { return false; }

            List<FileMetaData> fileMetaDataList = new List<FileMetaData>();

            foreach (string imageName in imageNameList)
            {
                FileMetaData fileMetaData = new FileMetaData();
                fileMetaData.FileName = imageName;
                fileMetaDataList.Add(fileMetaData);
            }

            int PhotosListBoxCount = thumbs.GetPhotosListBoxCount();
            thumbs.AddImages(fileMetaDataList, PhotosListBoxCount, isBased);

            return true;
        }

        /// <summary>
        /// 插入图像
        /// </summary>
        /// <param name="imageNameList">图像列表</param>
        /// <returns>操作是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool InsertImageList(List<string> imageNameList)
        {
            if (imageNameList == null || imageNameList.Count == 0)
            { return false; }

            thumbs.InsertImages(imageNameList);
            return true;
        }

        /// <summary>
        /// 清空图像
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void Clear()
        {
            thumbs.DeleteAllImages();
            images.Dispose();
        }
        /// <summary>
        /// 重置内部数据结构
        /// </summary>
        public void Reset()
        {
            thumbs.Reset();
        }
        /// <summary>
        /// 删除图像
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void DeleteImageList()
        {
            //1:获取删除列表
            List<FileMetaDataItem> delItemList = thumbs.GetSelectedItemList();

            //2:选中列表空判断
            if (delItemList == null || delItemList.Count == 0) { return; }

            //3:删除选中图像
            thumbs.DeleteImages(delItemList);
        }

        /// <summary>
        /// 全选
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void SelectAll()
        {
            thumbs.SelectAll();
        }

        /// <summary>
        /// 反选
        /// </summary>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public void DeSelect()
        {
            thumbs.DeSelect();
        }

        /// <summary>
        /// 查询当前选中的图像索引
        /// </summary>
        /// <returns>当前选中的图像索引</returns> 
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public int GetSelectedIndex()
        {
            return thumbs.GetSelectedIndex();
        }

        /// <summary>
        /// 获取全部图像描述列表
        /// </summary>
        /// <returns></returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public List<FileMetaData> GetAllImages()
        {
            return thumbs.GetAllImages();
        }
        /// <summary>
        /// 判断容器控件中是否有图像已发生变化，包括追加、删除、替换和旋转
        /// </summary>
        /// <returns></returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期       修改人     修改
        //2011-12-21 dengqiong  创建代码
        //--------------------------------------------------------------------------
        public Boolean IsChanged()
        {
            return thumbs.IsChanged();
        }
        /// <summary>
        /// 获取全部图像的ID列表
        /// </summary>
        /// <returns>全部图像的ID列表</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public List<string> GetAllImageIdList()
        {
            return thumbs.GetAllImageIdList();
        }

        /// <summary>
        /// 获取全部图像路径列表
        /// </summary>
        /// <returns>全部图像路径列表</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public List<string> GetAllImageNameList()
        {
            return thumbs.GetSelectedItemFileName(ItemNameType.AllItem);
        }

        /// <summary>
        /// 获取当前选择图像路径列表
        /// </summary>
        /// <returns>当前选择图像路径列表</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public List<string> GetSelectedImageNameList()
        {
            return thumbs.GetSelectedImageNameList();
        }

        /// <summary>
        /// 替换图片
        /// </summary>
        /// <param name="imageNameList">替换图片列表</param>
        /// <returns>是否成功</returns>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人     修改
        //2010-8-12 qizhenguo  创建代码
        //--------------------------------------------------------------------------
        public bool ReplaceSingleImage(List<string> imageNameList)
        {
            int index = GetSelectedIndex();
            int maxIndex = thumbs.GetPhotosListBoxCount();
            if (index < 0)
            {
                MessageBox.Show("请先选中要替换的图像");
                return false;
            }
            if (thumbs.GetSelectedItemCount() != 1)
            {
                MessageBox.Show("每次替换只能选中一个图像");
                return false;
            }

            //如果是最后一张，则先增加，后删除
            if (index == (maxIndex - 1))
            {
                SetSelectIndex(thumbs.GetPhotosListBoxCount() - 1);
                InsertImageList(imageNameList);
                SetSelectIndex(thumbs.GetPhotosListBoxCount() - 1);
                DeleteImageList();
                SetSelectIndex(thumbs.GetPhotosListBoxCount() - 1);
            }
            //否则先删除，后增加
            else
            {
                DeleteImageList();
                SetSelectIndex(index);
                InsertImageList(imageNameList);
                SetSelectIndex(index);
            }

            return true;
        }

        /// <summary>
        /// 设置按钮是否显示
        /// </summary>
        /// <param name="buttonType">指定按钮</param>
        /// <param name="visible">是否显示</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人        修改
        //2010-8-12 liangshengji  创建代码
        //--------------------------------------------------------------------------
        public void SetButtonVisible(ButtonType buttonType, bool visible)
        {
            images.SetButtonVisible(buttonType, visible);

        }

        /// <summary>
        /// 设置缩略图的选中状态，此时会产生事件，由容器捕获，并大图显示
        /// </summary>
        /// <param name="index">缩略图索引</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人        修改
        //2010-8-12 liangshengji  创建代码
        //--------------------------------------------------------------------------
        public void SetSelectIndex(int index)
        {
            thumbs.SetSelectedIndex(index);
        }

        /// <summary>
        /// 设置缩略图是否隐藏
        /// </summary>
        /// <param name="flag">缩略图是否隐藏</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人        修改
        //2010-8-12 liangshengji  创建代码
        //--------------------------------------------------------------------------
        public void SetThumbnailHidden(bool flag)
        {
            //支持缩略图隐藏并设置隐藏标志
            if (isThumbHidden && flag)
            {
                GridLengthConverter myGridLengthConverter = new GridLengthConverter();
                GridLength gl1 = (GridLength)myGridLengthConverter.ConvertFromString("0");
                thumbnailColumn.Width = gl1;
                this.UpdateLayout();
            }
            //不支持隐藏或未设置隐藏标志
            else
            {
                GridLengthConverter myGridLengthConverter = new GridLengthConverter();
                GridLength gl1 = (GridLength)myGridLengthConverter.ConvertFromString("180");
                thumbnailColumn.Width = gl1;
                this.UpdateLayout();
            }
        }
        //设置工具条的可见性
        /// <summary>
        /// 设置工具条的可见性
        /// </summary>
        /// <param name="visible">工具条是否可见</param>
        //--------------------------------------------------------------------------
        //修改历史:
        //日期      修改人        修改
        //2010-8-12 liangshengji  创建代码
        //--------------------------------------------------------------------------
        public void SetToolBarVisible(bool visible)
        {
            images.SetToolBarVisible(visible);
        }
        public void SetMenuItem(bool IsEnable) 
        {
            thumbs.setMenuItem(IsEnable);
        }
        #endregion

    }
}
