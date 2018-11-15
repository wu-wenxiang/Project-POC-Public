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
using System.Windows.Shapes;
using UFileClient.Display;
using UFileClient.Common;

namespace FMPTestClient.FileUploadDisplay
{
    /// <summary>
    /// ImageDisplayContainerWnd.xaml 的交互逻辑
    /// </summary>
    public partial class ImageDisplayContainerWnd : Window
    {
        private bool IsImage = false;
        public ImageDisplayContainerWnd()
        {
            InitializeComponent();
            image.SetEditable(true);
            image.SetToolBarVisible(true);
            image.SetButtonVisible(ButtonType.SaveRotationButton, true);
            image.ImageDisplayType = ImageDisplayType.WidthAdaptable;
        }
        public ImageDisplayContainerWnd(List<FileMetaData> fileMetaDataList, bool isUpdate)
        {
            InitializeComponent();
            image.AddImageList(fileMetaDataList, false);
            image.SetEditable(true);
            image.SetToolBarVisible(true);
            image.SetButtonVisible(ButtonType.SaveRotationButton, true);
            image.ImageDisplayType = ImageDisplayType.WidthAdaptable;
        }

        private void buttonAddImage_Click(object sender, RoutedEventArgs e)
        {
            List<string> selImgNames = InnerUtility.OpenFileSelectDialog("插入");
            if (selImgNames != null)
            {
                bool flag = image.AddImageList(selImgNames);
                if (!flag)
                {
                    MessageBox.Show("追加图像失败");
                }
                else
                {
                    ClearImage.IsEnabled = true;
                }
            }
            else
            {
                MessageBox.Show("未选择图像");
            }
        }

        private void buttonInsertImage_Click(object sender, RoutedEventArgs e)
        {
            //1:获取选中图片列表
            List<string> selectedImgList = image.GetSelectedImageNameList();
            //2:获取选中图片列表不为空判断
            if (selectedImgList == null)
            {
                MessageBox.Show("请选中一张图片");
                return;
            }
            //3:插入图片
            //3-1:只能选中一张图片作为插入位置
            if (selectedImgList.Count != 1)
            {
                MessageBox.Show("请仅选中一张图片");
            }
            //3-2:插入图片
            else
            {
                List<string> selImgNames = InnerUtility.OpenFileSelectDialog("插入");
                if (selImgNames != null)
                {
                    bool flag = image.InsertImageList(selImgNames);
                    if (!flag)
                    {
                        MessageBox.Show("追加图像失败");
                    }
                }
                else
                {
                    MessageBox.Show("未选择图像");
                }
            }
        }

        private void buttonClearImage_Click(object sender, RoutedEventArgs e)
        {
            
            List<FileMetaData> f = image.GetAllImages();
            if (f != null)
            {
                image.Clear();
            }
            else
            {
                MessageBox.Show("未选择上传图像");
            }

        }

    }
}
