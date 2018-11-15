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
using System.Xml;
using System.IO;
using FMPTestClient;
using UFileClient.Common;
using FMPTestClient.Scan;
using FMPTestClient.FileUploadDisplay;

namespace FMPTestClient
{
    /// <summary>
    /// 文件操作的主窗口对应的交互逻辑
    /// 该类实现文件操作的主窗口展示功能。
    /// </summary>
    public partial class MainWindow : Window
    {
        //构造函数
        public MainWindow()
        {
            //初始化界面
            InitializeComponent();
            Util.AppInit("testappid", "");
        }

     
        /// <summary>
        ///【退出】按钮按下触发的动作。
        /// </summary>
        private void ButtonQuit_Click(object sender, RoutedEventArgs e)
        {
            //关闭当前窗口
            this.Close();
        }

        private void buttonUploadDisplay_Click(object sender, RoutedEventArgs e)
        {
            ImageDisplayContainerWnd imageDisplayWnd = new ImageDisplayContainerWnd();
            imageDisplayWnd.Owner = this;
            imageDisplayWnd.ShowDialog();
        }

        private void ButtonScan_Click(object sender, RoutedEventArgs e)
        {
            ScanWindow scanWnd = new ScanWindow();
            scanWnd.Owner = this;
            scanWnd.ShowDialog();
            List<string> scanResult = scanWnd.scanResult;
            if (scanResult != null)
            {
                List<FileMetaData> fileMetaDataList = Util.GetFileMetaDataListFromImageNameList(scanResult);
                ImageDisplayContainerWnd displayWnd = new ImageDisplayContainerWnd(fileMetaDataList, false);
                displayWnd.ShowDialog();
            }

        }

    }

}
