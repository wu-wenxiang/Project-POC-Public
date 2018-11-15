using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using System.IO;
using System.ComponentModel;
using ImageScan.TwainLib;
using ImageScan.GdiPlusLib;
using log4net;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using System.Threading;
using UFileClient.Common;


namespace UFileClient.Twain
{
    /// <summary>
    /// 扫描图像展示类
    /// </summary>
    public partial class ScanImageViewer : Window
    {
        BackgroundWorker worker = null;
        private TwainSession twSession;                                         // 扫描仪对象    
        private List<string> fileNames = new List<string>();                    // 生成的文件名列表
        public delegate TwRC transferFile(TwFileFormat fileType, out string fileName, int resolution, out string msg);
        private transferFile transferFileAction;                                //委托，记录不同传输模式下的单个文件传输函数         
        private TwRC transferResult;                                            //每次传输的返回结果
        private int cnt;                                                        //记录已传输文件数
        private TwFileFormat fileType;                                          //文件格式
        private Int32 maxImageSize;
        private string msg;
        private int resolution;
        private Int32 blankSizeLimit;

        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static ScanImageViewer()
        {
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.GetConfigDir() + ConstCommon.logConfigFileName));
        }


        public List<string> ScanFileNames
        {
            get { return fileNames; }
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="twSource">twSession</param>
        /// <param name="format">文件格式字符串表示</param>
        /// <param name="action">文件传输函数委托</param>
        public ScanImageViewer(TwainSession twainSession, Int32 maxImageSize, int blanklimit, int resolution, TwFileFormat fileformat)
        {
            InitializeComponent();
            twSession = twainSession;
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.WorkerSupportsCancellation = true;
            cnt = 0;
            this.buttonControl.DataContext = "传输未开始";
            this.maxImageSize = maxImageSize;
            this.blankSizeLimit = blanklimit;
            this.resolution = resolution;
            fileType = fileformat;
            Init();
        }
        //事件包装器
        public event RoutedEventHandler ScanPageCompleted
        {
            add { AddHandler(ScanPageCompletedEvent, value); }
            remove { RemoveHandler(ScanPageCompletedEvent, value); }
        }

        // 注册退出扫描事件
        public static readonly RoutedEvent ScanPageCompletedEvent =
            EventManager.RegisterRoutedEvent("ScanPageCompletedEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(ScanImageViewer));

        /// <summary>
        /// 初始化扫描进程显示界面
        /// 添加文件保存后台线程的三个回调方法
        /// </summary>
        private void Init()
        {
            worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                String fileSingleName;      // 每次传输返回的文件名

                //获取传输模式
                TwMode mode = twSession.GetCurrentTransferMode();
                switch (mode)
                {
                    case TwMode.TWSX_FILE:
                        transferFileAction = twSession.TransferSingleFileInFileMode;
                        break;
                    case TwMode.TWSX_NATIVE:
                        transferFileAction = twSession.TransferSingleFileInNativeMode;
                        break;
                    default:
                        break;
                }

                // 1：初始化GDI+
                Gdip.InitGDIPlus();

                //2：依次传输每个文件
                logger.Debug("开始传输文件:" + DateTime.Now);
                if (transferFileAction != null)
                {
                    int count = 0;
                    try
                    {
                        do
                        {
                            logger.Debug("开始传输单个文件: count = " + count);
                            //传输单个文件                           
                            transferResult = transferFileAction(fileType, out fileSingleName, resolution, out msg);
                            logger.Debug("传输单个文件结束: result = " + transferResult);
                            logger.Debug("开始结束一次传输过程。");
                            twSession.EndXfer(out count);
                            logger.Debug("结束一次传输过程结束: count = " + count);
                            //2.1:传输成功则检查文件大小
                            if (transferResult == TwRC.XferDone)
                            {
                                logger.Debug("得到一个传输文件: " + DateTime.Now);
                                if (fileSingleName != null)
                                {
                                    //A:判断读取的文件大小,零字节则不添加到列表中
                                    FileInfo fileInfo = new FileInfo(fileSingleName);
                                    if (fileInfo.Length > blankSizeLimit && fileInfo.Length < maxImageSize)
                                    {
                                        cnt++;
                                        fileNames.Add(fileSingleName);
                                        worker.ReportProgress(fileNames.Count);
                                        logger.Debug("添加文件到列表，文件名: " + fileSingleName);
                                        //iacTwain.RaiseGetOnePageEvent(fileSingleName);           
                                    }
                                    //B:否则删除文件
                                    else
                                    {
                                        fileInfo.Delete();
                                        logger.Info("删除太大或太小的扫描结果文件。");
                                    }
                                }
                                else
                                    logger.Debug("文件名为null。");
                            }
                            //2.2:传输失败则跳出循环
                            else
                            {
                                logger.Debug("传输失败，跳出循环。");
                                //if (count != 0) //如果扫描仪还有图像未传输，通知扫描仪清空
                                //    twSession.ResetScanner();
                                break;
                            }
                            logger.Debug("结束传输单个文件: count = " + count);
                        } while (count != 0 && count != -2);
                        logger.Debug("结束循环 :" + DateTime.Now);
                    }
                    #region 异常处理
                    catch (ArgumentNullException e)
                    {
                        logger.Error("参数为空异常，异常信息: " + e.Message);
                    }
                    catch (ArgumentException e)
                    {
                        logger.Error("无效参数异常，异常信息: " + e.Message);
                    }
                    catch (System.Security.SecurityException e)
                    {
                        logger.Error("安全性错误引发的异常，异常信息: " + e.Message);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        logger.Error("IO错误或指定类型引发的安全性错误引发的异常，异常信息: " + e.Message);
                    }
                    catch (PathTooLongException e)
                    {
                        logger.Error("路径太长引发的异常，异常信息: " + e.Message);
                    }
                    catch (NotSupportedException e)
                    {
                        logger.Error("调用的方法不受支持或视图读取，查找或写入不支持调用功能的流引发的异常，异常信息: " + e.Message);
                    }
                    catch (IOException e)
                    {
                        logger.Error("IO异常，异常信息: " + e.Message);
                    }
                    catch (Exception e)
                    {
                        logger.Error("操作发生异常，异常信息: " + e.Message);
                    }
                    finally
                    {
                        Gdip.ClearGDIPlus();
                    }
                    #endregion
                }
            };

            worker.ProgressChanged += delegate(object s, ProgressChangedEventArgs args)
            {
                buttonControl.Content = "已经扫描传输" + cnt + "页";
            };

            worker.RunWorkerCompleted += delegate(object s, RunWorkerCompletedEventArgs args)
            {

                if (transferResult == TwRC.Failure)
                    MessageBox.Show("扫描失败:" + msg);
                else
                {
                    if (twSession.State >= TwState.OpenDS)
                        twSession.DisableDS();
                }

                if (transferResult == TwRC.TransferError)
                    MessageBox.Show("传输失败:" + msg);

                if (transferResult == TwRC.BadValue)
                    MessageBox.Show("扫描仪当前设置不支持文件格式" + fileType + "，请重新设置");

                Close();
            };
        }
        private void CloseWnd()
        {
            this.Dispatcher.Invoke(DispatcherPriority.Normal, (ThreadStart)delegate
            {
                Close();
            });
        }
        /// <summary>
        /// 启动文件保存进程
        /// </summary>
        public void StartTransfer()
        {
            worker.RunWorkerAsync();
            buttonControl.Content = "扫描仪扫描中，请稍候...";
            buttonControl.IsEnabled = false;
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            if (twSession.State >= TwState.OpenDS)
                twSession.DisableDS();
        }
    }
}
