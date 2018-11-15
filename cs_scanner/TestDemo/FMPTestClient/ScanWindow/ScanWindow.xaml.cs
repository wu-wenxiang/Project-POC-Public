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
using UFileClient.Twain;

namespace FMPTestClient.Scan
{
    /// <summary>
    /// 扫描窗口类
    /// </summary>
    public partial class ScanWindow : Window
    {
        // 扫描结果，即扫描得到图像文件的全路径列表
        public List<string> scanResult = null;

        // 构造方法
        public ScanWindow()
        {
            InitializeComponent();
        }

        // 扫描完成事件的响应方法
        private void Window_ScanCompleted(object sender, RoutedEventArgs e)
        {
            // 获取扫描结果列表
            scanResult = scanCtl.GetScanResult();

            // 关闭窗口并返回
            this.DialogResult = true;
        }

        // 退出扫描控件事件的响应方法
        private void scanCtl_ScanCanceled(object sender, RoutedEventArgs e)
        {
            // 关闭窗口并返回
            this.DialogResult = false;
        }

        // 关闭窗口事件的响应方法
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            // 释放扫描资源
            scanCtl.DisposeTwain();
        }
    }
}
