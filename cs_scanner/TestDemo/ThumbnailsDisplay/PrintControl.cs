using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace UFileClient.Display
{
    /// <summary>
    /// 打印类
    /// </summary>
    public class PrintControl
    {
        // 要打印的图像名列表
        private List<string> ImageNameList { set; get; }

        // 本类的单例对象
        private static PrintControl singleObj = null;

        // 打印文档对象
        private PrintDocument printDoc = null;

        // 打印预览对话框
        private PrintPreviewDialog previewDlg = null;

        // 打印页面设置对话框
        private PageSetupDialog pageSetupDlg = null;

        // 打印对话框
        private PrintDialog printSetupDlg = null;

        // 打印页编号
        private int _pageIndex = 0;

        private static Object m_lock = new Object();

        private Margins margin = new Margins(0, 0, 0, 0);

        // 构造函数
        private PrintControl()
        {
            printDoc = new PrintDocument();
            // 设置打印事件
            printDoc.PrintPage += new PrintPageEventHandler(this.PrintDoc_PrintPage);
        }

        // 获得本类的单例
        public static PrintControl GetSingleObject()
        {
            lock (m_lock)
            {
                if (singleObj == null)
                    singleObj = new PrintControl();
            }
            return singleObj;
        }

        // 预览一张图像
        public void Preview(string imageName)
        {
            if (imageName == null)
                return;

            List<string> imgNameList = new List<string>();
            imgNameList.Add(imageName);
            Preview(imgNameList);
        }

        // 预览一组图像
        public void Preview(List<string> imageNameList)
        {
            if (imageNameList == null || imageNameList.Count == 0)
                return;
            this.ImageNameList = imageNameList;

            //设置打印总页数
            printDoc.DocumentName = imageNameList.Count.ToString();

            try
            {
                //申明并实例化previewDlg
                if (previewDlg == null)
                {
                    previewDlg = new PrintPreviewDialog();
                    previewDlg.Document = printDoc;
                }
                else//根据its需求更改完下面一个后，如果关闭了预览窗口之后，再点击预览会出报错，是因为对象还没有释放，但是句柄没了，所以新加了else中的内容，wk，2018-8-21
                {
                    previewDlg.Close();
                    previewDlg = new PrintPreviewDialog();
                    previewDlg.Document = printDoc;
                }

                //设置窗口尺寸和位置
                //previewDlg.ClientSize = new Size(640, 480);
                //previewDlg.Location = new Point(0, 0);

                //最大化窗口
                ((System.Windows.Forms.Form)previewDlg).WindowState = FormWindowState.Maximized;
                
                //previewDlg.ShowDialog();//原来的
                //应its需求，希望每次弹出的预览窗口在最上层，wk，2018-8-21
                ((System.Windows.Forms.Form)previewDlg).TopMost = true;
                ((System.Windows.Forms.Form)previewDlg).Show(); 
                
            }
            catch (System.Drawing.Printing.InvalidPrinterException)
            {
                MessageBox.Show("未安装打印机！", "打印", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "打印", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 打印页面设置
        public void PageSetup()
        {
            try
            {
                //申明并实例化PageSetupDialog
                if (pageSetupDlg == null)
                {
                    pageSetupDlg = new PageSetupDialog();
                    pageSetupDlg.Document = printDoc;
                }

                //相关文档及文档页面默认设置
                pageSetupDlg.PageSettings = printDoc.DefaultPageSettings;
                pageSetupDlg.PageSettings.Margins = margin;
                //显示对话框
                DialogResult result = pageSetupDlg.ShowDialog();
                if (result == DialogResult.OK)
                    printDoc.DefaultPageSettings = pageSetupDlg.PageSettings;

            }
            catch (System.Drawing.Printing.InvalidPrinterException)
            {
                MessageBox.Show("未安装打印机！", "打印", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "打印", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        // 打印一张图像
        public void PrintSetup(string imageName)
        {
            if (imageName == null)
                return;

            List<string> imgNameList = new List<string>();
            imgNameList.Add(imageName);
            PrintSetup(imgNameList);
        }

        // 打印一组图像
        public void PrintSetup(List<string> imageNameList)
        {
            if (imageNameList == null || imageNameList.Count == 0)
                return;
            this.ImageNameList = imageNameList;

            //设置打印总页数
            printDoc.DocumentName = imageNameList.Count.ToString();

            try
            {
                //申明并实例化PrintDialog
                if (printSetupDlg == null)
                {
                    printSetupDlg = new PrintDialog();

                    //可以选定页
                    printSetupDlg.AllowSomePages = false;
                    printSetupDlg.AllowPrintToFile = false;

                    //指定打印文档
                    printSetupDlg.Document = printDoc;
                }

                printSetupDlg.PrinterSettings = printDoc.PrinterSettings;

                //显示对话框
                DialogResult result = printSetupDlg.ShowDialog();
                if (result == DialogResult.OK)
                {
                    //保存打印设置
                    printDoc.PrinterSettings = printSetupDlg.PrinterSettings;
                    //修改页面设置
                    printDoc.DefaultPageSettings.Margins = margin;
                    //打印
                    printDoc.Print();
                }

            }
            catch (System.Drawing.Printing.InvalidPrinterException)
            {
                MessageBox.Show("未安装打印机！", "打印", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "打印", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        //添加打印页
        private void PrintDoc_PrintPage(object sender, PrintPageEventArgs e)
        {
            Image oneImage = Image.FromFile(@ImageNameList[_pageIndex]);

            int x = e.MarginBounds.X;
            int y = e.MarginBounds.Y;
            int boundWidth = e.MarginBounds.Width;
            int boundHeight = e.MarginBounds.Height;

            // 实际打印的宽度和高度
            int width;
            int height;

            double widthRate = (double)boundWidth / oneImage.Width;
            double heightRate = (double)boundHeight / oneImage.Height;
            // 如果图像尺寸超过纸张边框，则按比例缩小图像
            if (widthRate < 1 || heightRate < 1)
            {
                double rate = widthRate < heightRate ? widthRate : heightRate;
                width = (int)(oneImage.Width * rate);
                height = (int)(oneImage.Height * rate);
            }
            else  // 图像尺寸小于纸张边框，则按图像实际尺寸打印
            {
                width = oneImage.Width;
                height = oneImage.Height;
            }

            Rectangle destRect = new Rectangle(x, y, width, height);
            e.Graphics.DrawImage(oneImage, destRect);

            if (_pageIndex < ImageNameList.Count - 1)
            {
                _pageIndex++;
                e.HasMorePages = true;
            }
            else
            {
                _pageIndex = 0;
                e.HasMorePages = false;
            }
        }
    }
}
