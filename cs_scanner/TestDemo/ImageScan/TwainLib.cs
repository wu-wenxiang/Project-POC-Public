using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using ImageScan.GdiPlusLib;
using UFileClient.Common;
using log4net;
using log4net.Config;
using ImageScan;

namespace ImageScan.TwainLib
{
    public enum TwainCommand
    {
        Not = -1,
        Null = 0,
        TransferReady = 1,
        CloseRequest = 2,
        CloseOk = 3,
        DeviceEvent = 4
    }

    public partial class TwainSession
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly ILog loadAndUnloadLogger = LogManager.GetLogger("LoadAndUnLoadLogger");
        private static readonly ILog messageLogger = LogManager.GetLogger("MessageLogger");

        private const short CountryUSA = 1;
        private const short LanguageUSA = 13;

        private IntPtr hwnd;                                            // 应用程序窗口句柄
        private TwIdentity appid;                                       // 应用程序标志结构
        private TwIdentity srcds;                                       // 数据源标示
        private TwEvent evtmsg;                                         // 事件结构
        private WINMSG winMsg;                                          // 消息结构
        private List<TwIdentity> srcList;                               // DSM管理的数据源列表
        private TwState state;                                          // 扫描仪所处状态
        private string imageSaveDir = null;                             // 扫描文件保存路径
        public String defaultScannerName;                               // 当前默认扫描仪名称 
        private TwCap[] capList;                                        // 扫描能力类型列表
        private Hashtable scannerCaps = null;                           // 扫描能力与对应数据映射表

        private bool isGroup4 = false;                                  // 是否采用GROUP4压缩算法                          
        private bool isBW = false;                                      // 是否是黑白文件


        static TwainSession()
        {
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.GetConfigDir() + ConstCommon.logConfigFileName));
        }
        /// <summary>
        /// 当前DSM状态
        /// </summary>
        public TwState State
        {
            get
            {
                return state;
            }
            private set
            {
                TwState oldState = state;
                //状态值被修改时，记录修改状态值的函数
                if (oldState != value)
                {
                    StackTrace t = new StackTrace();
                    String s = t.GetFrame(1).GetMethod().ToString();
                    logger.Debug("function " + s + " twain state changed from " + oldState + " to " + value);
                }
                state = value;
            }
        }
        /// <summary>
        /// 构造函数
        /// </summary>
        public TwainSession(IntPtr hwndWindow, TwCap[] capList, UserConfig userConfig)
        {
            loadAndUnloadLogger.Debug("TwainSession constructor() loaded");
            // 初始化工作路径
            imageSaveDir = Config.GetImageDir();

            // 初始化应用程序标识
            appid = new TwIdentity();
            appid.Id = IntPtr.Zero;
            appid.Version.MajorNum = 1;
            appid.Version.MinorNum = 1;
            appid.Version.Language = LanguageUSA;
            appid.Version.Country = CountryUSA;
            appid.Version.Info = "UFileClient 1";
            appid.ProtocolMajor = TwProtocol.Major;
            appid.ProtocolMinor = TwProtocol.Minor;
            appid.SupportedGroups = (int)(TwDG.Image | TwDG.Control);
            appid.Manufacturer = "NETMaster";
            appid.ProductFamily = "Freeware";
            appid.ProductName = "UFile";

            hwnd = hwndWindow;

            ///初始化DS标示
            srcds = new TwIdentity();
            srcds.Id = IntPtr.Zero;
            State = TwState.LoadDSM;

            evtmsg.EventPtr = Marshal.AllocCoTaskMem(Marshal.SizeOf(winMsg));

            ///打开DMS
            if (!OpenDSM(hwndWindow))
            {
                MessageBox.Show("初始化DSM失败");
                return;
            }

            ///查看所有管理的DS
            srcList = new List<TwIdentity>();
            if (!GetAllDS())
            {
                MessageBox.Show("无法查看DSM所管理的数据源");
                return;
            }
            this.capList = capList;
            this.userConfig = userConfig;

            // 初始化用户设置到当前值
            Hashtable userSettings = userConfig.GetSettings();

            for (int i = 0; i < capList.Length; i++)
            {
                TwCap capId = capList[i];
                TwOneValue oneValue = userSettings[capId] as TwOneValue;
                SetCurrentValueOfCap(capId, oneValue);
            }

            loadAndUnloadLogger.Debug("TwainSession constructor() finished");
        }

        /// <summary>
        /// 析构函数
        /// </summary>
        ~TwainSession()
        {
            Marshal.FreeCoTaskMem(evtmsg.EventPtr);
        }

        /// <summary>
        /// 初始化DSM
        /// </summary>
        /// <param name="hwndp">初始化完成后指向应用程序句柄</param>
        private bool OpenDSM(IntPtr hwndp)
        {
            logger.Debug("begin to open DSM :state = " + state);
            if (state == TwState.LoadDSM)
            {
                if (!State2ToState3(hwndp))
                    State3ToState2();
            }
            logger.Debug("end to open DSM :state = " + state);
            return state == TwState.OpenDSM;
        }

        /// <summary>
        /// 查询DSM管理的所有数据源的名称列表
        /// </summary>
        /// <returns>数据源名称列表，如果查询失败则返回null</returns>
        public ArrayList GetDSNameList()
        {
            if (srcList != null)
            {
                ArrayList dsNamelist = new ArrayList();
                for (int i = 0; i < srcList.Count; i++)
                {
                    dsNamelist.Add(srcList[i].ProductName);
                }

                return dsNamelist;
            }
            else
                return null;
        }

        /// <summary>
        /// 查询DSM的所有数据源,必须在调用init函数之后执行
        /// </summary>
        /// <returns>查询DSM数据源是否成功</returns>
        private bool GetAllDS()
        {
            //当前有数据源未关闭 ，则先关闭数据源
            if (state == TwState.OpenDSM)
            {
                srcList.Clear();
                bool endOfList = false;
                TwIdentity srcDS;
                TwRC rc;

                //1：查询第一个DS
                srcDS = new TwIdentity();
                rc = DSMident(appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.GetFirst, srcDS);
                //解析查询结果
                if (rc == TwRC.Success)
                {
                    logger.Debug("find the first scanner name " + srcDS.ProductName);
                    srcList.Add(srcDS);
                }
                else if (rc == TwRC.EndOfList)
                    endOfList = true;
                else
                {
                    logger.Error("cannot get the first DS " + GetLastErrorCode());
                    srcList.Clear();
                    return false;
                }

                //2：循环查询下一个DS
                while (!endOfList)
                {
                    //查询下一个DS
                    srcDS = new TwIdentity();
                    rc = DSMident(appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.GetNext, srcDS);

                    //解析查询结果
                    if (rc == TwRC.Success)
                    {
                        logger.Debug("find the next scanner name " + srcDS.ProductName);
                        srcList.Add(srcDS);
                    }
                    else if (rc == TwRC.EndOfList)
                        endOfList = true;
                    else
                    {
                        logger.Error("cannot get the next DS " + GetLastErrorCode());
                        srcList.Clear();
                        return false;
                    }
                }
                return true;
            }
            else
                return false;
        }

        /// <summary>
        /// 将指定的扫描仪作为当前默认DS
        /// </summary>
        /// <param name="scannerName">扫描仪名称</param>
        /// <returns>选择成功返回true，否则返回false</returns>
        public bool SelectAsCurrentDS(string scannerName)
        {
            if (srcList == null)
                return false;


            //1：查找DSM所有的DS中与指定名称相符的DS
            int index = -1;
            for (int i = 0; i < srcList.Count; i++)
            {
                if (srcList[i].ProductName.Equals(scannerName))
                {
                    index = i;
                    break;
                }
            }

            //2：分析查找结果
            if (index == -1)
                return false;
            else
            {
                srcds = (TwIdentity)srcList[index];
                defaultScannerName = srcList[index].ProductName;
                return true;
            }
        }

        /// <summary>
        /// 连接当前选定的数据源
        /// </summary>
        /// <returns>连接是否成功，连接成功返回TREU，否则返回false</returns>
        public bool OpenDS()
        {
            try
            {
                //如果有正打开的数据源，则关闭数据源
                if (state > TwState.OpenDS)
                    DisableDS();
                if (state == TwState.OpenDSM)
                    State3ToState4();
                return (state == TwState.OpenDS);
            }
            catch (Exception e)
            {
                logger.Error("failed to open DS " + e.Message);
                return (state == TwState.OpenDS);
            }
        }


        /// <summary>
        /// 进行使能DS前的相关处理准备工作
        /// </summary>
        /// <param name="info">执行过程中的错误信息</param>
        /// <param name="flag">是否按照配置文件对扫描仪进行配置</param>
        /// <returns>执行结果是否成功</returns>
        public bool PrepareDS(out string info, bool flag)
        {
            info = null;

            //一：将扫描仪状态转换到状态4
            //1.1：如果会话状态大于状态4
            logger.Debug("Acquire(): begin to close DS");
            if (state > TwState.OpenDS)
                DisableDS();

            //1.2：如果扫描仪会话状态小于状态4
            if (state < TwState.OpenDS)
            {
                OpenDSM(hwnd);
                OpenDS();
            }

            //1.3检查当前状态是否处于OPENDS
            if (state != TwState.OpenDS)
            {
                info = "不能打开扫描仪" + defaultScannerName;
                return false;
            }


            //二：关闭扫描仪进度对话框
            SuppressProgressUI();

            //三：如果需要读取配置文件，则按照配置文件配置扫描仪
            if (flag || defaultScannerName.ToLower().Contains("panasonic"))
            {
                Hashtable results = SetCapsToScanner();

                if (results != null && results.Count != 0)
                {
                    String str = null;
                    ICollection caps = results.Keys;
                    bool setFlag = false;
                    foreach (TwCap capID in caps)
                    {
                        if (capID != TwCap.ICAP_AUTODISCARDBLANKPAGES && capID != TwCap.ICAP_AUTOMATICBORDERDETECTION
                                && capID != TwCap.ICAP_AUTOMATICDESKEW && capID != TwCap.ICAP_IMAGEFILEFORMAT &&
                            capID != TwCap.ICAP_COMPRESSION)
                        {
                            setFlag = true;
                            String capIDString = capID.ToString();
                            if (results[capID] != null)
                                str += "能力" + capIDString + "设置失败：设置数据 " + results[capID].ToString() + "\n";
                        }
                    }

                    if (setFlag)
                    {
                        info = "扫描仪参数配置失败:" + str;
                        return false;
                    }
                }
            }

            //四：检查送纸器是否有纸张
            bool isSupport = false;
            bool res = IsFeederLoaded(out isSupport);
            if (isSupport && !res)
            {
                if (defaultScannerName.ToLower().Contains("av8350"))
                {
                    return true;
                }
                else
                {
                    info = "扫描仪送纸器没有纸张，请放入纸张后再试";
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 设置用户设置到扫描仪，并记录设置结果
        /// </summary>
        /// <returns></returns>
        private Hashtable SetCapsToScanner()
        {
            Hashtable hashTable = new Hashtable();
            Hashtable userSettings = userConfig.GetSettings();
            CapInfo capInfo = null;
            for (int i = 0; i < capList.Length; i++)
            {
                TwCap capId = capList[i];
                if (capId == TwCap.ICAP_AUTOMATICDESKEW)
                {
                    if (capInfo.CurrentIntStr == "0")
                    {
                        continue;
                    }
                }
                if (capId == TwCap.CAP_DUPLEXENABLED)
                {
                    SetFeederEnabled();
                }
                TwOneValue oneValue = userSettings[capId] as TwOneValue;
                if (oneValue != null)
                {
                    //读取配置文件的能力设置，对扫描仪进行设置
                    SetCurrentValueOfCap(capId, oneValue);
                    capInfo = GetScannerCap(capId);
                    if (!capInfo.SetCap())
                    {
                        hashTable[capInfo.CapId] = capInfo.CurrentIntStr;
                    }
                }
            }
            return hashTable;
        }
        /// <summary>
        /// 打开扫描仪自带参数配置界面
        /// </summary>
        /// <param name="info">错误信息描述</param>
        /// <returns>执行是否成功</returns>
        public bool EnableDSUIOnly(out string info)
        {
            info = null;
            bool flag = false;
            if (state == TwState.OpenDS)
            {
                if (!State4ToState5WithOnlyUI())
                    info = "无法打开扫描仪自带的参数配置界面";
                else
                    flag = true;
            }
            return flag;
        }

        /// <summary>
        /// 使能DS
        /// </summary>
        /// <param name="info">执行过程中的错误信息记录</param>
        /// <param name="showUI">是否打开扫描仪自带参数配置界面</param>
        /// <returns>执行结果是否成功</returns>
        public bool EnableDS(out string info, bool showUI)
        {
            info = null;
            bool flag = false;
            if (state == TwState.OpenDS)
            {
                if (!State4ToState5WithShowUI(showUI))
                {
                    info = "扫描仪无法进入准备状态";
                }
                else
                    flag = true;
            }
            return flag;
        }

        /// <summary>
        /// 设置能力值
        /// </summary>
        /// <param name="capId"></param>
        /// <param name="curValue"></param>
        /// <returns></returns>
        public bool SetCapability(TwCap capId, String curValue)
        {
            bool setCapSucc = false;
            TwCapability cap = new TwCapability(capId);
            TwType datatype = new TwType();
            string str = GetCapabilityCurrentValue(capId, ref datatype);
            if (str == null)
                logger.Debug("failed to get current value of capability " + capId);
            else
            {
                CapInfo capInfo = new CapInfo();
                capInfo.CapId = capId;
                capInfo.capType = datatype;
                capInfo.CurrentIntStr = curValue;
                setCapSucc = SetCapability(capInfo);
            }
            return setCapSucc;
        }

        /// <summary>
        /// 检查送纸器中是否有纸张
        /// </summary>
        /// <param name="isSupported">扫描仪是否支持送纸器状态查询</param>
        /// <returns>是否有纸张</returns>
        public bool IsFeederLoaded(out bool isSupported)
        {
            bool loadedFlag = false;
            isSupported = false;

            //如果选择纸张来源是自动，则无需进行送纸器是否有纸检测
            if (NeedCheckPaperFeeder())
            {
                //1：查询扫描仪是否支持送纸器的查询
                if (SupportedCapability(TwCap.CAP_FEEDERENABLED))
                {
                    //2：获取送纸器查询值
                    TwType twType = TwType.Null;
                    string dataType = GetCapabilityCurrentValue(TwCap.CAP_FEEDERENABLED, ref twType);

                    if (dataType == null)
                        logger.Info("cannot get the cap_feederenabled");
                    //2.1：当前是ADF进纸器
                    if (dataType.Equals("1"))
                    {
                        isSupported = true;
                        // 查询送纸器是否有纸                
                        String dataStr = GetCapabilityCurrentValue(TwCap.CAP_FEEDERLOADED, ref twType);
                        if (dataStr == null)
                            loadedFlag = true;
                        else
                            loadedFlag = dataStr.Equals("1") || dataStr.Equals("1.0");
                    }
                    //2.2：当前是其他进纸器
                    else
                        isSupported = false;
                }
            }
            else
                loadedFlag = true;
            return loadedFlag;
        }

        /// <summary>
        /// 是否需要进行送纸器是否有纸张的检查
        /// </summary>
        /// <returns></returns>
        private bool NeedCheckPaperFeeder()
        {
            bool flag = false;
            string name = defaultScannerName.ToLower();

            if (name.Contains("canon"))
            {
                TwType dataType = TwType.Null;
                string curVal = GetCapabilityCurrentValue(TwCap.CCAP_CEI_DOCUMENT_PLACE, ref dataType);
                //0：自动，2：平板
                if (curVal == "0" || curVal == "2")
                    flag = false;
                //1：送纸器
                else
                    flag = true;
            }
            else if (name.Contains("fujitsu") && name.Contains("6230"))
            {
                flag = false;
            }
            return flag;
        }

        /// <summary>
        /// 关闭扫描仪扫描进度对话框
        /// </summary>
        /// <returns>如果成功则返回true,否则返回false</returns>
        private bool SuppressProgressUI()
        {
            if (state == TwState.OpenDS)
            {
                //是否支持关闭扫描进度提示框
                if (!SupportedCapability(TwCap.CAP_INDICATORS))
                    return false;
                else
                {
                    ///能力数据结构对象
                    TwCapability UICap = new TwCapability();
                    UICap.capID = TwCap.CAP_INDICATORS;
                    UICap.contentType = TwOn.One;

                    ///单值数据对象
                    TwOneValueIntegerOrBool oneValue = new TwOneValueIntegerOrBool();
                    oneValue.ItemType = GetCapabilitySupportedDataType(TwCap.CAP_INDICATORS);
                    oneValue.Item = 0;

                    ///关闭扫描过程中的进度提示
                    if (!SetCapabilityOneValue(TwCap.CAP_INDICATORS, oneValue))
                    {
                        logger.Debug("suppressUI():failed to suppress UI" + GetLastErrorCode());
                        return false;
                    }
                    else
                        return true;
                }
            }
            else
                return false;
        }

        /// <summary>
        /// 查询扫描仪当前文件格式
        /// </summary>
        /// <returns>扫描仪传输方式</returns>
        public TwFileFormat GetCurrentFileFormat()
        {
            long val = long.MinValue;
            try
            {
                //查询传输方式
                if (state > TwState.OpenDSM)
                {
                    TwType datatype = new TwType();
                    string mode = GetCapabilityCurrentValue(TwCap.ICAP_IMAGEFILEFORMAT, ref datatype);
                    if (mode != null)
                        val = long.Parse(mode);
                }
                TwFileFormat fileFormat;
                if (val != long.MinValue)
                    fileFormat = (TwFileFormat)val;
                else
                    fileFormat = TwFileFormat.TWFF_NULL;

                return fileFormat;
            }
            #region 异常处理
            catch (ArgumentException e)
            {
                logger.Error("GetCurrentFileFormat():ArgumentException occured :exception " + e.Message);
                return TwFileFormat.TWFF_NULL;
            }
            catch (FormatException e)
            {
                logger.Error("GetCurrentFileFormat():FormatException occured :exception " + e.Message);
                return TwFileFormat.TWFF_NULL;
            }
            catch (OverflowException e)
            {
                logger.Error("GetCurrentFileFormat():OverflowException occured :exception " + e.Message);
                return TwFileFormat.TWFF_NULL;
            }
            catch (Exception e)
            {
                logger.Error("GetCurrentFileFormat():Exception occured :exception " + e.Message);
                return TwFileFormat.TWFF_NULL;
            }
            #endregion
        }

        /// <summary>
        /// 查询扫描仪当前传输方式
        /// </summary>
        /// <returns>扫描仪传输方式</returns>
        public TwMode GetCurrentTransferMode()
        {
            long val = long.MinValue;
            try
            {
                //查询传输方式
                if (state > TwState.OpenDSM)
                {
                    TwType datatype = new TwType();
                    string mode = GetCapabilityCurrentValue(TwCap.ICAP_XferMech, ref datatype);
                    if (mode != null)
                        val = long.Parse(mode);
                }

                //分析传输方式结果
                switch ((TwMode)val)
                {
                    case TwMode.TWSX_NATIVE:
                    case TwMode.TWSX_FILE:
                    case TwMode.TWSX_MEMORY:
                        return (TwMode)val;
                    default:
                        return TwMode.NULL;
                }
            }
            #region 异常处理
            catch (ArgumentException e)
            {
                logger.Error(" GetCurrentTransferMode():ArgumentException occured :exception " + e.Message);
                return TwMode.NULL;
            }
            catch (FormatException e)
            {
                logger.Error(" GetCurrentTransferMode():FormatException occured :exception " + e.Message);
                return TwMode.NULL;
            }
            catch (OverflowException e)
            {
                logger.Error(" GetCurrentTransferMode():OverflowException occured :exception " + e.Message);
                return TwMode.NULL;
            }
            catch (Exception e)
            {
                logger.Error(" GetCurrentTransferMode():Exception occured :exception " + e.Message);
                return TwMode.NULL;
            }
            #endregion
        }


        //// <summary>
        /// 在native模式下传输单个文件
        /// </summary>
        /// <param name="fileTypeEnum">文件类型</param>
        /// <param name="fileName">传输成功时设置为文件的全路径名，否则设置为null</param>
        /// <returns>返回查询扫描仪状态，为-2时文件传输过程出错，为-1时还有图像未传输，为0时图像已全部传输，>0时为未传输的图像个数</returns>
        public TwRC TransferSingleFileInNativeMode(TwFileFormat fileType, out String fileName, int resolution, out string msg)
        {
            fileName = null;
            msg = null;
            IntPtr hbitmap = IntPtr.Zero;
            TwRC rc;
            ///检查参数
            if (fileType == TwFileFormat.TWFF_NULL)
            {
                msg = "文件格式类型不能为空";
                logger.Error(msg);
                return TwRC.BadValue;
            }

            try
            {
                if (state == TwState.TransferReady)
                {
                    TwImageInfo iinf = new TwImageInfo();
                    State = TwState.Transfer;

                    //1:启动图片传输
                    rc = DSixferNative(appid, srcds, TwDG.Image, TwDAT.ImageNativeXfer, TwMSG.Get, ref hbitmap);

                    //如果传输失败，则hbitmap不是有效的内存指针
                    if (rc != TwRC.XferDone)
                    {
                        fileName = null;
                        TwCC twCC = GetLastErrorCode();
                        msg = "启动文件传输失败：错误码 = " + rc + ",错误原因 = " + twCC;
                        logger.Debug(msg);
                        return TwRC.Failure;
                    }
                    //2:结束图片传输
                    else
                    {
                        State = TwState.Transfer;
                        //分析图片传输结果
                        if (hbitmap != IntPtr.Zero && rc == TwRC.XferDone)
                        {

                            string nameSuffix = ConvertFileTypeToExt(fileType);
                            if (fileType == TwFileFormat.TWFF_TIFF)
                            {
                                fileName = Gdip.SaveSingleImageToFile(hbitmap, ref nameSuffix, true, resolution);
                            }
                            else
                            {
                                fileName = Gdip.SaveSingleImageToFile(hbitmap, ref nameSuffix, false, resolution);
                            }
                            TwCap capId = TwCap.ICAP_AUTOMATICDESKEW;

                            if (GetCurrentValueOfCap(capId) == "1" && !IsSupported(capId))
                            {
                                // 调用纠偏处理函数
                                string tempFileName = imageSaveDir + FileNameUtility.GetOnlyOneFileName(nameSuffix);
                                if (ImageProcessor.ReviseTwist(fileName, tempFileName))
                                {
                                    //删除处理前的影像文件
                                    File.Delete(fileName);
                                    fileName = tempFileName;
                                }

                            }
                            capId = TwCap.ICAP_AUTOMATICBORDERDETECTION;
                            if (GetCurrentValueOfCap(capId) == "1" && !IsSupported(capId))
                            {
                                // 调用去黑边处理函数
                                string tempFileName = imageSaveDir + FileNameUtility.GetOnlyOneFileName(nameSuffix);
                                if (ImageProcessor.RemoveBlackEdge(fileName, tempFileName, 50))
                                {
                                    //删除处理前的影像文件
                                    File.Delete(fileName);
                                    fileName = tempFileName;
                                }
                            }
                        }
                        return rc;
                    }
                }
                else
                {
                    msg = "扫描仪状态不正确：" + state;
                    logger.Debug(msg);
                    return TwRC.Failure;
                }
            }
            catch (Exception e)
            {
                fileName = null;
                msg = "传输文件发生异常：" + e.Message;
                logger.Debug(msg);
                return TwRC.Failure;
            }

        }



        /// <summary>
        /// 在file模式下传输单个文件
        /// </summary>
        /// <param name="fileTypeEnum">文件类型</param>
        /// <param name="fileName">传输成功时设置为文件的全路径名，否则设置为null</param>
        /// <returns>返回查询扫描仪状态，为-2时文件传输过程出错，为-1时还有图像未传输，为0时图像已全部传输，>0时为未传输的图像个数</returns>
        public TwRC TransferSingleFileInFileMode(TwFileFormat fileType, out String fileName, int resolution, out string msg)
        {
            fileName = null;
            msg = null;
            TwRC rc = TwRC.Failure;
            //检查参数
            if (fileType == TwFileFormat.TWFF_NULL)
            {
                msg = "文件格式类型不能为空";
                logger.Info(msg);
                return TwRC.BadValue;
            }

            try
            {
                if (state == TwState.TransferReady)
                {
                    //指定图片传输的全路径名和文件格式                  
                    string nameSuffix = ConvertFileTypeToExt(fileType);
                    if (string.IsNullOrEmpty(nameSuffix))
                        return TwRC.BadValue;

                    TwSetupFileXfer fileXfer = new TwSetupFileXfer();
                    string fullPath = imageSaveDir + FileNameUtility.GetOnlyOneFileName(nameSuffix);
                    fileXfer.FileName = fullPath;
                    fileXfer.Format = (short)fileType;
                    fileXfer.VRefNum = 0;

                    //设置文件传输路径
                    rc = DSFileixfer(appid, srcds, TwDG.Control, TwDAT.SetupFileXfer, TwMSG.Set, fileXfer);
                    logger.Debug("DSFileixfer = " + rc);
                    if (rc != TwRC.Success)
                    {
                        TwCC twCC = GetLastErrorCode();
                        msg = "文件传输模式设置路径失败：错误码 =" + rc + ",错误原因=" + twCC + ",文件名=" + Path.GetFileName(fullPath) + ",文件格式=" + fileType;
                        logger.Debug(msg);
                        if (twCC == TwCC.BadValue)
                            return TwRC.BadValue;
                        else
                            return TwRC.Failure;
                    }

                    //3:启动传输
                    IntPtr p = IntPtr.Zero;
                    rc = DSixferFile(appid, srcds, TwDG.Image, TwDAT.ImageFileXfer, TwMSG.Get, ref p);
                    //如果传输失败
                    if (rc != TwRC.XferDone)
                    {
                        fileName = null;
                        TwCC twCC = GetLastErrorCode();
                        msg = "启动文件传输失败：错误码 = " + rc + ",错误原因 = " + twCC;
                        logger.Debug(msg);
                        return TwRC.Failure;
                    }
                    else
                    {
                        State = TwState.Transfer;

                        //5：分析图片传输结果
                        if (rc == TwRC.XferDone)
                        {
                            logger.Debug("handle the picture");
                            fileName = fullPath;
                            TwCap capId = TwCap.ICAP_AUTOMATICDESKEW;
                            if (GetCurrentValueOfCap(capId) == "1" && !IsSupported(capId))
                            {
                                // 调用纠偏处理函数
                                string tempFileName = imageSaveDir + FileNameUtility.GetOnlyOneFileName(nameSuffix);
                                if (ImageProcessor.ReviseTwist(fileName, tempFileName))
                                {
                                    //删除处理前的影像文件
                                    File.Delete(fileName);
                                    fileName = tempFileName;
                                }
                            }
                            capId = TwCap.ICAP_AUTOMATICBORDERDETECTION;
                            if (GetCurrentValueOfCap(capId) == "1" && !IsSupported(capId))
                            {
                                // 调用去黑边处理函数
                                string tempFileName = imageSaveDir + FileNameUtility.GetOnlyOneFileName(nameSuffix);
                                if (ImageProcessor.RemoveBlackEdge(fileName, tempFileName, 50))
                                {
                                    //删除处理前的影像文件
                                    File.Delete(fileName);
                                    fileName = tempFileName;
                                }
                            }
                        }
                        return rc;
                    }
                }
                else
                {
                    logger.Debug("state  = :" + state);
                }
                return TwRC.Failure;
            }
            catch (Exception e)
            {
                fileName = null;
                msg = "传输文件发生异常：" + e.Message;
                logger.Error(msg);
                return TwRC.Failure;
            }
        }

        /// <summary>
        /// 进行图像处理
        /// </summary>
        /// <param name="callApiFlag">图像处理函数调用标志</param>
        /// <param name="fullPath">图像文件全路径名</param>
        /// <returns>图像是否是空白页</returns>
        private string RevisePictureFromFile(Hashtable imageProcFlag, string fullPath)
        {

            bool isBlank = false;
            string fileName = fullPath;
            #region 需要调用图像处理函数

            IntPtr pImage = IntPtr.Zero;
            try
            {
                //1:加载图像
                pImage = ImageProcessor.LoadImageFile(fullPath);

                #region 2:图像处理
                if (pImage != IntPtr.Zero)
                {
                    IntPtr pTmpImage = IntPtr.Zero;

                    if ((bool)imageProcFlag[TwCap.ICAP_AUTODISCARDBLANKPAGES])
                        isBlank = ImageProcessor.IsBlank(pImage);

                    //图像非空
                    if (!isBlank)
                    {
                        if ((bool)imageProcFlag[TwCap.ICAP_AUTOMATICDESKEW])
                        {
                            pTmpImage = ImageProcessor.ReviseTwist(pImage);
                            ImageProcessor.ReleaseImage(pImage);
                            pImage = pTmpImage;
                        }

                        if ((bool)imageProcFlag[TwCap.ICAP_AUTOMATICBORDERDETECTION])
                        {
                            pTmpImage = ImageProcessor.RemoveBlackEdge(pImage);
                            ImageProcessor.ReleaseImage(pImage);
                            pImage = pTmpImage;
                        }
                        //3:图像保存
                        if (pImage != IntPtr.Zero)
                        {
                            string nameSuffix = Path.GetExtension(fullPath);
                            fileName = imageSaveDir + FileNameUtility.GetOnlyOneFileName(nameSuffix.Substring(1));
                            if (ImageProcessor.SaveImage(pImage, fileName, isBW, isGroup4))
                            {
                                File.Delete(fullPath);
                            }
                            else
                            {
                                fileName = fullPath;
                            }
                        }
                    }
                    else
                    {
                        File.Delete(fullPath);
                        fileName = null;
                    }
                }
                else
                {
                    fileName = fullPath;
                }
                #endregion
                return fileName;
            }

            #region 异常
            catch (DllNotFoundException dllNotFound)
            {
                logger.Error("dll not found:" + dllNotFound.Message);
                return null;
            }
            catch (EntryPointNotFoundException entryNotFound)
            {
                logger.Error("entry not found:" + entryNotFound.Message);
                return null;
            }
            catch (MarshalDirectiveException marshalException)
            {
                logger.Error("marshalException:" + marshalException.Message);
                return null;
            }
            catch (SEHException exp)
            {
                logger.Error("SEHException:" + exp.Message);
                return null;
            }
            catch (Exception exp)
            {
                logger.Error("Exception:" + exp.Message);
                return null;
            }
            #endregion

            finally
            {
                ImageProcessor.ReleaseImage(pImage);
            }
            #endregion
        }

        /// <summary>
        /// 进行图像处理
        /// </summary>
        /// <param name="callApiFlag">图像处理函数调用标志</param>
        /// <param name="fullPath">图像内存指针</param>
        /// <returns>图像处理后的内存指针</returns>
        private string RevisePictureFromIntPtr(Hashtable imageProcFlag, IntPtr p, ref string nameSuffix)
        {
            IntPtr tmpP = IntPtr.Zero;

            IntPtr pImage = IntPtr.Zero;

            IntPtr ptr = GlobalLock(p);

            String fileFullName = null;


            #region 需要调用图像处理函数

            try
            {
                //1:加载图像

                pImage = ImageProcessor.LoadImagePtr(ptr);

                #region 2:图像处理
                if (pImage != IntPtr.Zero)
                {
                    bool isBlank = false;
                    IntPtr pTmpImage = IntPtr.Zero;

                    if ((bool)imageProcFlag[TwCap.ICAP_AUTODISCARDBLANKPAGES])
                        isBlank = ImageProcessor.IsBlank(pImage);

                    //图像非空
                    if (!isBlank)
                    {
                        if ((bool)imageProcFlag[TwCap.ICAP_AUTOMATICDESKEW])
                        {
                            pTmpImage = ImageProcessor.ReviseTwist(pImage);
                            ImageProcessor.ReleaseImage(pImage);
                            pImage = pTmpImage;
                        }

                        if ((bool)imageProcFlag[TwCap.ICAP_AUTOMATICBORDERDETECTION])
                        {
                            pTmpImage = ImageProcessor.RemoveBlackEdge(pImage);
                            ImageProcessor.ReleaseImage(pImage);
                            pImage = pTmpImage;
                        }
                        if (pImage != IntPtr.Zero)
                        {
                            fileFullName = imageSaveDir + FileNameUtility.GetOnlyOneFileName(nameSuffix);
                            if (!ImageProcessor.SaveImage(pImage, fileFullName, isBW, isGroup4))
                            {
                                fileFullName = null;
                            }
                        }
                    }
                    else
                    {
                        fileFullName = null;
                    }
                }
                return fileFullName;
                #endregion
            }

            #region 异常处理
            catch (DllNotFoundException dllNotFound)
            {
                logger.Error("dll not found:" + dllNotFound.Message);
                return null;
            }
            catch (EntryPointNotFoundException entryNotFound)
            {
                logger.Error("entry not found:" + entryNotFound.Message);
                return null;
            }
            catch (MarshalDirectiveException marshalException)
            {
                logger.Error("marshalException:" + marshalException.Message);
                return null;
            }
            catch (SEHException exp)
            {
                logger.Error("SEHException:" + exp.Message);
                return null;
            }
            catch (Exception exp)
            {
                logger.Error("Exception:" + exp.Message);
                return null;
            }
            #endregion

            finally
            {
                if (pImage != IntPtr.Zero)
                {
                    ImageProcessor.ReleaseImage(pImage);
                }

                GlobalFree(ptr);
            }

            #endregion
        }


        /// <summary>
        ///  对系统消息进行预处理, 将Twain驱动发出的消息进行处理         
        /// </summary>
        /// <param name="msg">待处理的系统消息</param>
        /// <returns>消息处理结果</returns>
        public TwainCommand PassMessageWPF(ref System.Windows.Interop.MSG msg)
        {
            if (srcds.Id == IntPtr.Zero)
                return TwainCommand.Not;

            int pos = GetMessagePos();

            winMsg.hwnd = msg.hwnd;
            winMsg.message = msg.message;
            winMsg.wParam = msg.wParam;
            winMsg.lParam = msg.lParam;
            winMsg.time = GetMessageTime();
            winMsg.x = (short)pos;
            winMsg.y = (short)(pos >> 16);

            Marshal.StructureToPtr(winMsg, evtmsg.EventPtr, false);
            evtmsg.Message = 0;
            TwRC rc = DSevent(appid, srcds, TwDG.Control, TwDAT.Event, TwMSG.ProcessEvent, ref evtmsg);
            if (rc == TwRC.NotDSEvent)
                return TwainCommand.Not;

            if (evtmsg.Message == (short)TwMSG.XFerReady)
            {
                messageLogger.Debug("PassMessageWpf():Transfer Ready");
                State = TwState.TransferReady;
                return TwainCommand.TransferReady;
            }
            if (evtmsg.Message == (short)TwMSG.CloseDSReq)
            {
                messageLogger.Debug("PassMessageWpf():close request");
                return TwainCommand.CloseRequest;
            }
            if (evtmsg.Message == (short)TwMSG.CloseDSOK)
            {
                messageLogger.Debug("PassMessageWpf():close DS OK");
                return TwainCommand.CloseOk;
            }
            if (evtmsg.Message == (short)TwMSG.DeviceEvent)
            {
                messageLogger.Debug("PassMessageWpf():Device Event");
                return TwainCommand.DeviceEvent;
            }
            else
            {
                return TwainCommand.Null;
            }
        }

        /// <summary>
        /// 关闭当前正在使用的扫描源
        /// </summary>
        public bool CloseDS()
        {
            logger.Debug("begin to close DS :state = " + state);
            if (srcds.Id != IntPtr.Zero)
            {
                if (state > TwState.OpenDS)
                    DisableDS();

                //由状态OpenDS转换为OpenDSM
                if (state == TwState.OpenDS)
                    State4ToState3();
                logger.Debug("end to close DS :state = " + state);
                return state == TwState.OpenDSM;
            }
            logger.Debug("end to close DS :state = " + state);
            return true;
        }


        /// <summary>
        /// 结束一次传输过程
        /// </summary>
        /// <param name="count">当为0时，状态转换为EnableSource，为-1或>0时为TransferReady</param>
        /// <returns>结束传输过程的执行结果</returns>
        public TwRC EndXfer(out int count)
        {
            count = 0;

            try
            {
                TwPendingXfers pxfr = new TwPendingXfers();
                pxfr.Count = 0;
                pxfr.EOJ = 0;
                //结束文件传输
                TwRC rc = DSpxfer(appid, srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.EndXfer, pxfr);

                //分析结果
                if (rc == TwRC.Success)
                {
                    if (pxfr.Count == 0)
                        State = TwState.EnableSource;
                    else
                        State = TwState.TransferReady;
                    count = pxfr.Count;
                }
                else
                {
                    count = 0;
                    logger.Debug("endTransfer():failed to end Tranfer  " + GetLastErrorCode());
                }
                return rc;

            }
            catch (Exception e)
            {
                count = 0;
                //ResetScanner();
                logger.Error("end  transfer failed " + e.Message);
                return TwRC.Failure;
            }
        }

        public void ResetScanner()
        {
            TwRC twRc = DSpxfer(appid, srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.Reset, null);
            if (twRc != TwRC.Success)
                logger.Debug("failed to reset Scanner : " + GetLastErrorCode());
        }

        /// <summary>
        /// 将会话从状态6 TransferReady转换到状态5EnableSource
        /// </summary>
        /// <returns></returns>
        private bool State6ToState5()
        {
            if (state == TwState.TransferReady)
            {
                TwPendingXfers pxfr = new TwPendingXfers();
                pxfr.Count = 0;
                TwRC twRC = DSpxfer(appid, srcds, TwDG.Control, TwDAT.PendingXfers, TwMSG.Reset, pxfr);
                if (twRC == TwRC.Success)
                    State = TwState.EnableSource;
                else
                    logger.Debug("State6ToState5(): failed to change state  from TransferReady to EnableSource" + GetLastErrorCode());
            }
            return state == TwState.EnableSource;
        }

        /// <summary>
        /// 将会话从状态5EnableSource转换到状态4OpenDS
        /// </summary>
        /// <returns></returns>
        private bool State5ToState4()
        {
            if (state == TwState.EnableSource)
            {
                TwUserInterface guif = new TwUserInterface();
                loadAndUnloadLogger.Debug("before Disable DS: " + DateTime.Now);
                TwRC twRC = DSuserif(appid, srcds, TwDG.Control, TwDAT.UserInterface, TwMSG.DisableDS, guif);
                loadAndUnloadLogger.Debug("end Disbale DS: " + DateTime.Now);
                if (twRC == TwRC.Success)
                    State = TwState.OpenDS;
                else
                    logger.Debug("State5ToState4(): failed to change state from EnableSource to OpenDS" + GetLastErrorCode());
            }
            return state == TwState.OpenDS;
        }

        /// <summary>
        /// 将会话从状态4OpenDS转换到状态3OpenDSM
        /// </summary>
        /// <returns></returns>
        private bool State4ToState3()
        {
            if (state == TwState.OpenDS)
            {
                loadAndUnloadLogger.Debug("before close DS: " + DateTime.Now);
                TwRC twRC = DSMident(appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.CloseDS, srcds);
                loadAndUnloadLogger.Debug("end close DS: " + DateTime.Now);
                if (twRC == TwRC.Success)
                    State = TwState.OpenDSM;
                else
                    logger.Debug("State4ToState3():failed to change state  from OpenDS to OpenDSM" + GetLastErrorCode());
            }
            return state == TwState.OpenDSM;
        }

        /// <summary>
        /// 将会话从状态3OpenDSM转换到状态2loadDSM
        /// </summary>
        /// <returns></returns>
        private bool State3ToState2()
        {
            if (state == TwState.OpenDSM)
            {
                loadAndUnloadLogger.Debug("before close DSM: " + DateTime.Now);
                TwRC rc = DSMparent(appid, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.CloseDSM, ref hwnd);
                loadAndUnloadLogger.Debug("end close DSM: " + DateTime.Now);
                if (rc == TwRC.Success)
                {
                    State = TwState.LoadDSM;
                    appid.Id = IntPtr.Zero;
                }
                else
                {
                    logger.Debug("State3ToState2():failed to close DSM " + GetLastErrorCode());
                }
            }
            return state == TwState.LoadDSM;
        }

        /// <summary>
        /// 将会话从状态2loadDSM转换到状态3OpenDSM
        /// </summary>
        /// <returns></returns>
        private bool State2ToState3(IntPtr hwndp)
        {
            if (state == TwState.LoadDSM)
            {
                //打开DSM
                loadAndUnloadLogger.Debug("before open DSM: " + DateTime.Now);
                TwRC rc = DSMparent(appid, IntPtr.Zero, TwDG.Control, TwDAT.Parent, TwMSG.OpenDSM, ref hwndp);
                loadAndUnloadLogger.Debug("end open openDSM:" + DateTime.Now);

                //打开成功则转换状态
                if (rc == TwRC.Success)
                    State = TwState.OpenDSM;
                //打开失败则关闭DSM
                else
                {
                    TwCC twcc = GetLastErrorCode();
                    logger.Debug(" State2ToState3(IntPtr hwndp) failed: cannot open DSM：" + twcc.ToString());
                }
            }
            return state == TwState.OpenDSM;
        }



        /// <summary>
        /// 将会话从状态3OpenDSM转换到状态4OpenDS
        /// </summary>
        /// <returns></returns>
        private bool State3ToState4()
        {
            if (state == TwState.OpenDSM)
            {
                loadAndUnloadLogger.Debug("before open DS: " + DateTime.Now);
                TwRC twRC = DSMident(appid, IntPtr.Zero, TwDG.Control, TwDAT.Identity, TwMSG.OpenDS, srcds);
                loadAndUnloadLogger.Debug("end open DS: " + DateTime.Now);
                if (twRC == TwRC.Success)
                    State = TwState.OpenDS;
                else
                    logger.Debug("State3ToState4():cannot open the DS: condition" + GetLastErrorCode());
            }
            return state == TwState.OpenDS;

        }

        /// <summary>
        /// 将会话状态从状态4 OpenDS转换到状态5 enableDS
        /// </summary>
        /// <returns>状态转换是否成功</returns>
        private bool State4ToState5WithOnlyUI()
        {
            //1:初始化使能DS的数据结构
            TwUserInterface guif = new TwUserInterface();
            guif.ShowUI = 1;
            guif.ModalUI = 0;
            guif.ParentHand = hwnd;

            //2:使能DS
            loadAndUnloadLogger.Debug("before EnableDSUIOnly: " + DateTime.Now);
            TwRC rc = DSuserif(appid, srcds, TwDG.Control, TwDAT.UserInterface, TwMSG.EnableDSUIOnly, guif);
            loadAndUnloadLogger.Debug("end EnableDSUIOnly: " + DateTime.Now);


            //3:检查结果
            if (rc != TwRC.Success)
                logger.Error(" cannot enable scanner,condition code = " + GetLastErrorCode());
            else
                State = TwState.EnableSource;

            return state == TwState.EnableSource;

        }

        /// <summary>
        /// 将会话状态从状态4 OpenDS转换到状态5 enableDS
        /// </summary>
        /// <returns>状态转换是否成功</returns>
        private bool State4ToState5WithShowUI(bool showUI)
        {
            //1:初始化使能DS的数据结构
            TwUserInterface guif = new TwUserInterface();
            if (showUI)
                guif.ShowUI = 1;
            else
                guif.ShowUI = 0;
            guif.ModalUI = 0;
            guif.ParentHand = hwnd;

            //2:使能DS
            loadAndUnloadLogger.Debug("before Enable DS: " + DateTime.Now);
            TwRC rc = DSuserif(appid, srcds, TwDG.Control, TwDAT.UserInterface, TwMSG.EnableDS, guif);
            loadAndUnloadLogger.Debug("end  Enable DS: " + DateTime.Now);

            //3:检查结果
            if (rc == TwRC.Success)
                State = TwState.EnableSource;
            else
                logger.Error("State4ToState5WithShowUI:failed " + GetLastErrorCode());

            return state == TwState.EnableSource;
        }

        /// <summary>
        /// 将DS从状态5转换到状态4.使DS变为非使能状态
        /// </summary>
        /// <returns>如果成功执行返回true，否则返回false</returns>
        public bool DisableDS()
        {
            if (state == TwState.Transfer)
            {
                int cnt;
                EndXfer(out cnt);
            }
            //由状态transferReady转换为EnableSouce
            if (state == TwState.TransferReady)
                State6ToState5();

            //由状态EnableSource转换为OpenDS                
            if (state == TwState.EnableSource)
                State5ToState4();

            return state == TwState.OpenDS;
        }

        /// <summary>
        /// 关闭数据源管理器
        /// </summary>
        public bool CloseDSM()
        {
            logger.Debug("begin to close DSM :state = " + state);
            if (state >= TwState.OpenDS)
                CloseDS();
            if (appid.Id != IntPtr.Zero)
                State3ToState2();
            logger.Debug("end to close DSM :state = " + state);
            return state == TwState.LoadDSM;
        }

        /// <summary>
        /// 根据文件类型返回后缀名
        /// </summary>
        /// <param name="fileType">文件类型</param>
        /// <returns>文件类型所对应的后缀名</returns>
        private static String ConvertFileTypeToExt(TwFileFormat fileType)
        {
            String Ext;
            switch (fileType)
            {
                case TwFileFormat.TWFF_TIFF:
                    Ext = "tif";
                    break;
                case TwFileFormat.TWFF_BMP:
                    Ext = "bmp";
                    break;
                case TwFileFormat.TWFF_JFIF:
                    Ext = "jpg";
                    break;
                case TwFileFormat.TWFF_PNG:
                    Ext = "png";
                    break;
                case TwFileFormat.TWFF_PICT:
                case TwFileFormat.TWFF_XBM:
                case TwFileFormat.TWFF_FPX:
                case TwFileFormat.TWFF_TIFFMULTI:
                case TwFileFormat.TWFF_SPIFF:
                case TwFileFormat.TWFF_EXIF:
                default:
                    Ext = null;
                    break;
            }
            return Ext;
        }


        #region 导出DLL相关函数
        //------ DSM entry point DAT_ variants:        
        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DSMparent([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr refptr);

        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DSMident([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwIdentity idds);

        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DSMstatus([In, Out] TwIdentity origin, IntPtr zeroptr, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwStatus dsmstat);


        // ------ DSM entry point DAT_ variants to DS:
        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DSuserif([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, TwUserInterface guif);

        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DSevent([In, Out] TwIdentity origin, [In, Out] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref TwEvent evt);

        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DSstatus([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwStatus dsmstat);

        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DScapGet([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, /*[In, Out] */ ref TwCapability capa);


        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DScapSet([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, IntPtr pCapability);

        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DSiinf([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwImageInfo imginf);

        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DSixferNative([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr hbitmap);

        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DSixferFile([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, ref IntPtr p);

        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DSFileixfer([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwSetupFileXfer fileXfer);

        [DllImport("twain_32.dll", EntryPoint = "#1")]
        private static extern TwRC DSpxfer([In, Out] TwIdentity origin, [In] TwIdentity dest, TwDG dg, TwDAT dat, TwMSG msg, [In, Out] TwPendingXfers pxfr);

        //memory  API
        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GlobalAlloc(int flags, int size);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GlobalLock(IntPtr handle);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern bool GlobalUnlock(IntPtr handle);

        [DllImport("kernel32.dll", ExactSpelling = true)]
        public static extern IntPtr GlobalFree(IntPtr handle);

        //message API
        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int GetMessagePos();

        [DllImport("user32.dll", ExactSpelling = true)]
        private static extern int GetMessageTime();

        /// <summary>
        /// Window Message
        /// </summary>
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        internal struct WINMSG
        {
            public IntPtr hwnd;
            public int message;
            public IntPtr wParam;
            public IntPtr lParam;
            public int time;
            public int x;
            public int y;
        }

        #endregion


    }
    /// <summary>
    /// 扫描能力单项记录结构
    /// </summary>
    public struct CapItem
    {
        public TwOn capDataType;         //能力描述的数据类型
        public TwType listDataType;      //数据列表中的数据类型          
        public ArrayList list;           //具体能力所能支持的数据列表
    }

    public struct OneValueItem
    {
        public TwType dataType;         //oneValue中数据项的数据类型
        public ValueType oneValue;      //oneValue的详细分类结构
    }
}
