using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.IO;
using ImageScan.TwainLib;
using log4net;
using System.Xml;
using UFileClient.Common;
using ImageScan.GdiPlusLib;
using ImageScan;
using System.Windows.Input;

namespace UFileClient.Twain
{
    /// <summary>
    /// 纸张来源
    /// </summary>
    public enum PaperSource
    {
        /// <summary>
        /// 自动选择送纸器或者平板
        /// </summary>
        Auto,
        /// <summary>
        /// 送纸器
        /// </summary>
        Feeder,
        /// <summary>
        /// 平板
        /// </summary>
        Platen
    }
    /// <summary>
    /// TwainScan.xaml 的交互逻辑
    /// </summary>
    public partial class TwainScan : UserControl
    {
        //获取日志记录器
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ILog messageLogger = LogManager.GetLogger("MessageLogger");
        private static readonly ILog loadAndUnloadLogger = LogManager.GetLogger("LoadAndUnLoadLogger");
        static TwainScan()
        {
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.getConfigDir() + ConstCommon.logConfigFileName));
        }
        private IntPtr hwndWindow;                     // WPF窗口句柄
        private bool msgfilter;                        // 捕捉消息设置标志
        private TwainSession twSession;                // 扫描仪对象
        private List<string> scanResultList = null;    // 扫描得到的文件名列表
        private bool configFlag = false;               // 扫描仪已经能力配置标志
        private UserConfig userConfig;                 // 用户设置管理
        private TwCap[] capList;                       // 配置能力ID列表
        private bool initFlag;                         // 初始化是否完成

        private delegate void RaiseEventDelegate(RoutedEventArgs args); //向应用发送消息函数委托
        private delegate TwRC transferFileDelegate(TwFileFormat fileType, out string fileName, int resolution, out string msg);//文件传输委托

        /// <summary>
        /// 单双面设置标志
        /// </summary>
        public DuplexPageEnable DuplexPageFlag
        {
            set;
            get;
        }

        public bool ShowProgress
        {
            set;
            get;
        }
        public PaperSource PaperSourceFlag
        {
            get;
            set;
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public TwainScan()
        {
            InitializeComponent();

            //  初始化默认参数
            Setting.IsEnabled = false;
            Scan.IsEnabled = false;
            msgfilter = false;
            initFlag = false;
            DuplexPageFlag = DuplexPageEnable.Default;
            PaperSourceFlag = PaperSource.Auto;
            scanResultList = new List<string>();
            ShowProgress = true;

            //添加样式接口，供GTS进行界面样式设置
            if (Application.Current != null)
            {
                if (Application.Current.Resources["BusinessViewBackgroundBrush"] != null)
                {
                    this.SetResourceReference(BackgroundProperty, "BusinessViewBackgroundBrush");

                }
            }
        }

        /// <summary>
        /// 控件加载函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            capList = new TwCap[] {
                    TwCap.ICAP_XferMech,               //传输方式
                    TwCap.CAP_DUPLEXENABLED,            //单双面                      
                    TwCap.ICAP_PixelType,               //位素类型 黑白 ，灰度，彩色

                    TwCap.ICAP_XRESOLUTION,             //X分辨率 
                    TwCap.ICAP_PIXELFLAVOR,             //反色
                    TwCap.ICAP_THRESHOLD,               //黑白阀值
                    TwCap.ICAP_IMAGEFILEFORMAT,         //文件格式
                    TwCap.ICAP_COMPRESSION,             //获取压缩方式                    

                    TwCap.ICAP_BRIGHTNESS,              //亮度
                    TwCap.ICAP_CONTRAST,                //对比度
                    TwCap.ICAP_AUTOMATICBORDERDETECTION,//自动去黑边                
                    TwCap.ICAP_AUTOMATICDESKEW,         //自动纠偏
                    TwCap.ICAP_AUTODISCARDBLANKPAGES,   //自动删除空白页功能

                    TwCap.ECAP_PAPERSOURCE,             //送纸器类型
                    };
            InitDataSrcList();
        }

        /// <summary>
        /// 初始化数据源列表
        /// </summary>
        public void InitDataSrcList()
        {
            
            loadAndUnloadLogger.Debug("初始化数据源列表开始:" + DateTime.Now);
            HwndSource source = (HwndSource)HwndSource.FromVisual(this);
            hwndWindow = source.Handle;
            
            userConfig = new UserConfig(capList);
            String scannerName = userConfig.DefaultScannerName;
            twSession = new TwainSession(hwndWindow, capList, userConfig);
           
            // 获取扫描仪数据源列表 
            DataSrc.Items.Clear();
            ArrayList scannerList = twSession.GetDSNameList();
            if (scannerList == null)
            {
                MessageBox.Show("没有找到扫描仪数据源,请确认是否安装了扫描仪驱动！");
                return;
            }

            // 将扫描仪数据源添加到下拉列表中
            for (int i = 0; i < scannerList.Count; i++)
            {
                ComboBoxItem item = new ComboBoxItem();
                item.Content = scannerList[i];
                DataSrc.Items.Add(item);
            }

            try
            {
                // 如果默认扫描仪在DSM当前列表中存在则选中
                if (scannerName != null && twSession.SelectAsCurrentDS(scannerName))
                {
                    DataSrc.Text = scannerName;
                }
                //否则选定一个默认值
                else if (DataSrc.HasItems)
                {
                    DataSrc.SelectedIndex = 0;
                    twSession.SelectAsCurrentDS(DataSrc.Text);
                    userConfig.DefaultScannerName = DataSrc.Text;
                }
                initFlag = true;
            }
            catch (Exception e)
            {
                DataSrc.SelectedIndex = 0;

                String msg = "配置文件读取失败,信息:" + e.ToString();
                logger.Error(msg);
                MessageBox.Show(msg);
            }
            finally
            {
                //如果有数据源，则允许用户进行参数设置和扫描
                if (DataSrc.HasItems)
                {
                    Setting.IsEnabled = true;
                    Scan.IsEnabled = true;
                }
                loadAndUnloadLogger.Debug("初始化数据源列表结束: " + DateTime.Now);
            }
        }

        /// <summary>
        /// 系统消息处理函数
        /// </summary>
        /// <param name="msg"></param>
        /// <param name="handled"></param>
        private void MessageFilter(ref System.Windows.Interop.MSG msg, ref bool handled)
        {

            //1:让TWAIN进行消息处理
            TwainCommand cmd = twSession.PassMessageWPF(ref msg);
            messageLogger.Debug("TWAIN 信息 = " + cmd + ":" + DateTime.Now);

            if (cmd == TwainCommand.Not || cmd == TwainCommand.Null)   // 非数据源消息
            {
                handled = false;           // 消息未处理，由窗口过程处理
                return;
            }

            //2：进行消息处理
            switch (cmd)
            {
                case TwainCommand.CloseRequest:
                    {
                        configFlag = false;
                        EndingScan();
                        twSession.DisableDS();
                        break;
                    }
                case TwainCommand.CloseOk:
                    {
                        configFlag = true;
                        EndingScan();
                        twSession.DisableDS();
                        break;
                    }
                case TwainCommand.DeviceEvent:
                    {
                        break;
                    }
                //扫描仪传输就绪函数
                case TwainCommand.TransferReady:
                    {
                        loadAndUnloadLogger.Debug("TWAIN 信息 = TransferReady " + DateTime.Now);
                        ScanImageViewer scanViewer = null;
                        try
                        {
                            if (ShowProgress)
                            {
                                //方式一：启动文件传输，弹出进度提示对话框  
                                //获得文件格式
                                CapInfo capInfo;
                                capInfo = twSession.GetScannerCap(TwCap.ICAP_IMAGEFILEFORMAT);
                                string intStr = capInfo.CurrentIntStr;
                                string enumStr = twSession.ConvertIntStringToEnumString(TwCap.ICAP_IMAGEFILEFORMAT, intStr);
                                TwFileFormat fileFormat = (TwFileFormat)Enum.Parse(typeof(TwFileFormat), enumStr);

                                //获取分辨率
                                capInfo = twSession.GetScannerCap(TwCap.ICAP_XRESOLUTION);
                                Int32 resolution = Int32.Parse(capInfo.CurrentIntStr);

                                // 获得空白页检测阈值
                                capInfo = twSession.GetScannerCap(TwCap.ICAP_AUTODISCARDBLANKPAGES);
                                intStr = capInfo.CurrentIntStr;
                                int blankImageSizeThreshold = 0;
                                if (!Int32.TryParse(intStr, out blankImageSizeThreshold))
                                {
                                    blankImageSizeThreshold = 0;
                                }
                                scanViewer = new ScanImageViewer(twSession, userConfig.MaxSize, blankImageSizeThreshold, resolution, fileFormat);      //生成显示控件对象
                                scanViewer.StartTransfer();                                                //开始图像传输   
                                scanViewer.ShowDialog();                                                   //展示显示控件
                                scanResultList = scanViewer.ScanFileNames;                                 //获取文件传输过程中的列表
                            }
                            else
                            {
                                //方式二：设置鼠标为等待状态，在相同线程中启动文件传输
                                Mouse.OverrideCursor = Cursors.Wait;
                                TransferFiles();
                            }
                            //发出扫描完成事件
                            logger.Debug("扫描完成 :" + DateTime.Now);
                            RoutedEventArgs scanArgs = new RoutedEventArgs(ScanCompletedEvent);
                            RaiseEvent(scanArgs);
                            break;
                        }
                        catch (Exception e)
                        {
                            logger.Error("在MessageFilter中发生异常，异常信息; " + e.Message);
                        }
                        finally
                        {
                            if (scanViewer != null)
                            {
                                logger.Debug("关闭扫描仪。");
                                scanViewer.Close();
                            }
                            //结束扫描
                            logger.Debug("结束扫描，使DS变为非使能状态。");
                            EndingScan();
                            twSession.DisableDS();
                            if (!ShowProgress)
                            {
                                //将鼠标复位
                                Mouse.OverrideCursor = null;
                            }
                        }
                        break;
                    }
                default:
                    break;
            }
            handled = true;
        }

        /// <summary>
        /// 循环传输每张图像
        /// </summary>
        private void TransferFiles()
        {
            //1：查询相关数据
            string fileSingleName;      // 每次传输返回的文件名
            string msg = null;
            //获得文件格式
            CapInfo capInfo;
            capInfo = twSession.GetScannerCap(TwCap.ICAP_IMAGEFILEFORMAT);
            string intStr = capInfo.CurrentIntStr;
            string enumStr = twSession.ConvertIntStringToEnumString(TwCap.ICAP_IMAGEFILEFORMAT, intStr);
            TwFileFormat fileFormat = (TwFileFormat)Enum.Parse(typeof(TwFileFormat), enumStr);

            //获取分辨率
            capInfo = twSession.GetScannerCap(TwCap.ICAP_XRESOLUTION);
            Int32 resolution = Int32.Parse(capInfo.CurrentIntStr);

            // 获得空白页检测阈值
            capInfo = twSession.GetScannerCap(TwCap.ICAP_AUTODISCARDBLANKPAGES);
            intStr = capInfo.CurrentIntStr;
            int blankImageSizeThreshold = 0;
            if (!Int32.TryParse(intStr, out blankImageSizeThreshold))
            {
                blankImageSizeThreshold = 0;
            }
            //获取传输模式
            TwMode mode = twSession.GetCurrentTransferMode();

            transferFileDelegate transferFileAction = null;                                //委托，记录不同传输模式下的单个文件传输函数   
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
            int cnt = 0;
            scanResultList = new List<string>();

            //2：依次传输每个文件
            TwRC transferResult;                                            //每次传输的返回结果
            logger.Debug("开始传输文件: " + DateTime.Now);
            if (transferFileAction != null)
            {
                int count = 0;
                try
                {
                    do
                    {
                        logger.Debug("开始传输单个文件: count = " + count + ":" + DateTime.Now);
                        //传输单个文件                           
                        transferResult = transferFileAction(fileFormat, out fileSingleName, resolution, out msg);
                        twSession.EndXfer(out count);
                        logger.Debug("结束一次传输 : count = " + count + ",传输结果: " + transferResult + ":" + DateTime.Now);
                        //2.1:传输成功则检查文件大小
                        if (transferResult == TwRC.XferDone)
                        {
                            if (fileSingleName != null)
                            {
                                //A:判断读取的文件大小,零字节则不添加到列表中
                                FileInfo fileInfo = new FileInfo(fileSingleName);
                                if (fileInfo.Length > blankImageSizeThreshold && fileInfo.Length < userConfig.MaxSize)
                                {
                                    cnt++;
                                    scanResultList.Add(fileSingleName);
                                    logger.Debug("追加文件到列表，文件名: " + fileSingleName);
                                    //RaiseGetOnePageEvent(fileSingleName);           
                                }
                                //B:否则删除文件
                                else
                                {
                                    fileInfo.Delete();
                                    if (fileInfo.Length <= blankImageSizeThreshold)
                                        logger.Info("删除扫描结果文件，把他当作空白页。");
                                    if (fileInfo.Length >= userConfig.MaxSize)
                                        logger.Info("文件太大，删除扫描结果文件。");
                                }
                            }
                            else
                                logger.Debug("文件名为null。");
                        }
                        //2.2:传输失败则跳出循环
                        else
                        {
                            if (count != 0) //如果扫描仪还有图像未传输，通知扫描仪清空
                                twSession.ResetScanner();
                            break;
                        }
                    } while (count != 0 && count != -2);

                    logger.Debug("结束循环 :" + DateTime.Now);

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
                        MessageBox.Show("扫描仪当前设置不支持文件格式" + fileFormat + "，请重新设置");
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
                    logger.Error("安全性错误引发的异常，异常信息:" + e.Message);
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

            //3：处理传输结果

        }
        /// <summary>
        /// 获取扫描结果，即扫描后的图像文件名列表
        /// <returns>
        /// 返回值：扫描后的图像文件名列表
        /// </returns>
        /// </summary>
        public List<string> GetScanResult()
        {
            logger.Debug("获取结果列表 :" + DateTime.Now);
            return scanResultList;
        }

        /// <summary>
        /// 窗口指针
        /// </summary>
        public IntPtr HwndOuter
        {
            get { return hwndWindow; }
            set { hwndWindow = value; }
        }

        /// <summary>
        /// 扫描按钮处理函数
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Scan_Click(object sender, RoutedEventArgs e)
        {
            StartScan();
        }

        /// <summary>
        /// 获取用户所选择的扫描仪名称
        /// </summary>
        /// <returns>扫描仪名称，没有选择时则返回null</returns>
        private string GetSelectedScannerName()
        {
            if (DataSrc.SelectedItem != null)
                return ((ComboBoxItem)DataSrc.SelectedItem).Content.ToString();
            else
                return null;
        }

        /// <summary>
        /// 结束扫描,移除消息处理函数
        /// </summary>
        private void EndingScan()
        {
            if (msgfilter)
            {
                loadAndUnloadLogger.Debug("结束扫描,移除消息处理函数开始: " + DateTime.Now);
                msgfilter = false;
                ComponentDispatcher.ThreadFilterMessage
                  -= new ThreadMessageEventHandler(MessageFilter);
                loadAndUnloadLogger.Debug("结束扫描,移除消息处理函数结束: " + DateTime.Now);
                messageLogger.Debug("移除消息处理函数。");
            }
        }

        /// <summary>
        /// 扫描仪设置
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Setting_Click(object sender, RoutedEventArgs e)
        {
            if (!initFlag)
            {
                InitDataSrcList();
                configFlag = false;
            }

            bool failedFlag = false;//函数执行错误标志
            try
            {
                //查询用户选择的扫描仪名称
                string selectedName = GetSelectedScannerName();
                if (selectedName == null)
                {
                    MessageBox.Show("在进行参数配置前请先选择扫描仪");
                    failedFlag = true;
                    return;
                }
                else
                {
                    //将扫描仪选择当前DSM管理的扫描仪
                    if (!twSession.SelectAsCurrentDS(selectedName))
                    {
                        MessageBox.Show("设置扫描仪失败，扫描仪可能不存在");
                        failedFlag = true;
                        return;
                    }

                    //调用扫描仪自带界面
                    if (UISelected.IsChecked == true)
                    {
                        //弹出扫描仪自带的参数设置界面
                        string info = null;
                        twSession.PrepareDS(out info, false);

                        twSession.EnableDSUIOnly(out info);
                        //检查控件是否加载了消息处理函数
                        if (!msgfilter)
                        {
                            msgfilter = true;
                            ComponentDispatcher.ThreadFilterMessage
                              += new ThreadMessageEventHandler(MessageFilter);
                            messageLogger.Debug("加载消息处理函数。");
                        }
                    }

                    //调用参数设置统一界面
                    else if (UISelected.IsChecked == false)
                    {
                        //生成并初始化参数配置界面
                        ScanConfig scanConfig = new ScanConfig();
                        if (!scanConfig.Init(twSession, capList))
                        {
                            scanConfig.Close();
                            MessageBox.Show("初始化扫描仪失败，请确定扫描仪与计算机连接正常");
                            failedFlag = true;
                            return;
                        }
                        scanConfig.ShowDialog();
                        configFlag = (bool)scanConfig.DialogResult;
                    }
                    else
                    {
                        logger.Debug("UISelected.IsChecked = ?");
                    }
                }
            }
            catch (Exception exp)
            {
                logger.Error("在与扫描仪交互过程中发生异常，异常信息: " + exp.Message);
                MessageBox.Show("在与扫描仪交互过程中发生异常，详细信息请查看日志");
                failedFlag = true;
            }
            finally
            {
                if (failedFlag)
                {
                    if (twSession.State >= TwState.OpenDS)
                        twSession.DisableDS();
                    EndingScan();
                }
            }
        }


        //事件包装器
        public event RoutedEventHandler ScanCompleted
        {
            add { AddHandler(ScanCompletedEvent, value); }
            remove { RemoveHandler(ScanCompletedEvent, value); }
        }

        //事件包装器
        public event RoutedEventHandler ScanCanceled
        {
            add { AddHandler(ScanCanceledEvent, value); }
            remove { RemoveHandler(ScanCanceledEvent, value); }
        }
        //事件包装器
        public event RoutedEventHandler ScanOnePageCompleted
        {
            add { AddHandler(ScanOnePageCompletedEvent, value); }
            remove { RemoveHandler(ScanOnePageCompletedEvent, value); }
        }

        // 注册扫描完成事件
        public static readonly RoutedEvent ScanCompletedEvent =
            EventManager.RegisterRoutedEvent("ScanCompletedEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TwainScan));

        // 注册退出扫描事件
        public static readonly RoutedEvent ScanCanceledEvent =
            EventManager.RegisterRoutedEvent("ScanCanceledEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TwainScan));
        // 注册退出扫描事件
        public static readonly RoutedEvent ScanOnePageCompletedEvent =
            EventManager.RegisterRoutedEvent("ScanOnePageCompletedEvent", RoutingStrategy.Bubble, typeof(RoutedEventHandler), typeof(TwainScan));


        public void RaiseGetOnePageEvent(string fileName)
        {
            RoutedEventArgs eventargs = new RoutedEventArgs(ScanOnePageCompletedEvent);
            eventargs.Source = fileName;

            this.Dispatcher.BeginInvoke(new RaiseEventDelegate(RaiseEvent), eventargs);
        }
        /// <summary>
        /// 取消按钮响应事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            //发送扫描取消事件
            RoutedEventArgs ScanCanceledEventArgs = new RoutedEventArgs(ScanCanceledEvent);
            RaiseEvent(ScanCanceledEventArgs);
        }
        /// <summary>
        /// 资源释放
        /// //2011-9-6 dengqiong 增加释放动态资源引用
        /// </summary>
        public void DisposeTwain()
        {
            loadAndUnloadLogger.Debug("资源释放处理开始: " + DateTime.Now);
            string name = GetSelectedScannerName();
            if (name != null)
            {
                userConfig.SaveDefaultScannerName(name);
            }
            if (twSession.State >= TwState.OpenDS)
                twSession.DisableDS();
            twSession.CloseDSM();
            EndingScan();
            loadAndUnloadLogger.Debug("资源释放处理结束: " + DateTime.Now);
            initFlag = false;
            hwndWindow = IntPtr.Zero;

            this.ClearValue(BackgroundProperty);
        }

        /// <summary>
        /// 切换扫描仪选择时，关闭之前选择的扫描仪。
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataSrc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (twSession.State >= TwState.OpenDS)
                twSession.CloseDS();
        }
        /// <summary>
        /// 开始扫描
        /// </summary>
        public void StartScan()
        {
            logger.Debug("开始扫描 :" + DateTime.Now);
            if (!initFlag)
            {
                InitDataSrcList();
                configFlag = false;
            }

            bool initSuccessFlag = true;
            try
            {
                //检查控件是否加载了消息处理函数
                if (!msgfilter)
                {
                    msgfilter = true;
                    ComponentDispatcher.ThreadFilterMessage
                      += new ThreadMessageEventHandler(MessageFilter);
                    messageLogger.Debug("加载消息处理函数。");
                }

                //读取用户在下列列表中所选择的扫描仪名称
                string scannerName = GetSelectedScannerName();
                if (scannerName == null)
                {
                    MessageBox.Show("在进行扫描前请先选定扫描仪");
                    initSuccessFlag = false;
                    return;
                }

                //检查用户选择的扫描仪是否在DSM的管理数据源之中
                if (!twSession.SelectAsCurrentDS(scannerName))
                {
                    MessageBox.Show("TWAIN没有成功管理用户选择的扫描仪");
                    initSuccessFlag = false;
                    return;
                }

                //扫描仪使能前的准备工作
                string info = null;
                bool flag = UISelected.IsChecked == true ? false : true;

                if (!twSession.PrepareDS(out info, !configFlag))
                {
                    string str = "扫描仪启动失败：";
                    if (info != null)
                        str += info;
                    initSuccessFlag = false;
                    MessageBox.Show(str);
                    return;
                }
                configFlag = true;

                //根据应用程序设置配置扫描仪
                AppConfiguration();

                //使能扫描仪,关闭扫描仪自带的参数配置界面
                info = null;
                if (!twSession.EnableDS(out info, false)) //这里设置扫描仪进入使能状态，开始进行扫描，只补充了注释，wk，2018-9-3
                {
                    string str = "扫描仪进入使能状态失败";
                    if (info != null)
                        str = info;
                    initSuccessFlag = false;
                    MessageBox.Show(str);
                    return;
                }
            }
            catch (Exception exp)
            {
                MessageBox.Show("扫描仪启动扫描过程失败，详细信息请查看日志");
                logger.Error("扫描仪启动扫描过程失败，异常信息: " + exp.Message);
            }
            //资源清理
            finally
            {
                //如果初始化失败，则关闭DS，删除消息处理函数
                if (!initSuccessFlag)
                {
                    twSession.DisableDS();
                    EndingScan();
                }
            }

            logger.Debug("扫描结束:" + DateTime.Now);
        }

        /// <summary>
        /// 根据应用程序配置扫描仪能力
        /// </summary>
        public void AppConfiguration()
        {
            string duplexPageString = null;
            switch (DuplexPageFlag)
            {
                case DuplexPageEnable.Double:
                    duplexPageString = "1";
                    break;
                case DuplexPageEnable.Single:
                    duplexPageString = "0";
                    break;
                default:
                    duplexPageString = null;
                    break;
            }
            if (duplexPageString != null)
                twSession.SetCapability(TwCap.CAP_DUPLEXENABLED, duplexPageString);

            string scannerName = GetSelectedScannerName().ToLower();
            if (scannerName.Contains("canon"))
                SetCanonPaperSource();
            else if (scannerName.Contains("fujitsu") && scannerName.Contains("6230"))
                SetFujitsuPaperSource();

        }
        /// <summary>
        /// 设置佳能型号的纸张来源
        /// </summary>
        private bool SetCanonPaperSource()
        {
            string sourceVal = "0";//默认为自动

            //1：检查是否带有平板
            bool autoFlag = false;
            CapItem capItem = twSession.GetCapabilitySupportedDataList(TwCap.CCAP_CEI_DOCUMENT_PLACE);
            if (capItem.list.Count == 3)
                autoFlag = true;

            //2:如果没有平板，需要修改应用设置
            if (!autoFlag)
                PaperSourceFlag = PaperSource.Feeder;

            //3:根据应用设置进行能力设置
            switch (PaperSourceFlag)
            {
                case PaperSource.Auto:
                    sourceVal = "0";
                    break;
                case PaperSource.Feeder:
                    sourceVal = "2";
                    break;
                case PaperSource.Platen:
                    sourceVal = "1";
                    break;
                default:
                    break;
            }

            bool flag = twSession.SetCapability(TwCap.CCAP_CEI_DOCUMENT_PLACE, sourceVal);
            return flag;
        }

        /// <summary>
        /// 设置富士通型号的纸张来源
        /// </summary>
        /// <returns></returns>
        private bool SetFujitsuPaperSource()
        {
            bool flag = false;
            bool loadedFlag = false; //adf送纸器是否有纸张
            bool AdfOpenFlag = false;//adf送纸器打开标志

            //1：获取送纸器查询值
            TwType twType = TwType.Null;
            string dataType = twSession.GetCapabilityCurrentValue(TwCap.CAP_FEEDERENABLED, ref twType);

            if (dataType == null)
                logger.Info("cannot get the cap_feederenabled");
            //1.1：当前是ADF进纸器
            if (dataType.Equals("1"))
            {
                // 查询送纸器是否有纸                
                String dataStr = twSession.GetCapabilityCurrentValue(TwCap.CAP_FEEDERLOADED, ref twType);
                if (dataStr != null && (dataStr.Equals("1") || dataStr.Equals("1.0")))
                    loadedFlag = true;
                else
                    loadedFlag = false;
                AdfOpenFlag = true;
            }
            //1.2:当前是平板进纸器
            else
                AdfOpenFlag = false;


            //3:根据应用设置进行能力设置
            // CAP_FEEDERENABLED属性：参数为true为ADF进纸，参数为false为平板进纸
            string sourceVal = "1";
            switch (PaperSourceFlag)
            {
                case PaperSource.Auto:
                    {
                        //支持ADF送纸器并且送纸器有纸，则使用ADF送纸器
                        if (AdfOpenFlag && loadedFlag)
                            sourceVal = "1";
                        else
                            sourceVal = "0";
                        break;
                    }
                case PaperSource.Feeder:
                    sourceVal = "1";
                    break;
                case PaperSource.Platen:
                    sourceVal = "0";
                    break;
                default:
                    break;
            }

            flag = twSession.SetCapability(TwCap.CAP_FEEDERENABLED, sourceVal);
            return flag;
        }
        /// <summary>
        /// 设置按钮是否显示
        /// </summary>
        /// <param name="buttonType">指定按钮</param>
        /// <param name="visible">是否显示</param>
        public void SetButtonVisible(ButtonType buttonType, bool visible)
        {
            Button button = ConvertButtonTypeToButton(buttonType);
            if (button == null)
            {
                return;
            }

            if (visible)
            {
                button.Visibility = Visibility.Visible;
            }
            else
            {
                button.Visibility = Visibility.Collapsed;
            }
        }

        public enum ButtonType
        {
            SettingButton,
            ScanButton,
            CancelButton
        }
        /// <summary>
        /// 按钮名称转换成按钮控件
        /// </summary>
        /// <param name="buttonType">按钮名称</param>
        /// <returns>按钮控件</returns>
        private Button ConvertButtonTypeToButton(ButtonType buttonType)
        {
            switch (buttonType)
            {
                case ButtonType.SettingButton:
                    return Setting;
                case ButtonType.ScanButton:
                    return Scan;
                case ButtonType.CancelButton:
                    return Cancel;
                default:
                    return null;
            }
        }
    }

    public enum DuplexPageEnable
    {
        Default,                        // 按照参数配置默认值
        Double,                         // 双面扫描
        Single                          // 单面扫描
    }
}