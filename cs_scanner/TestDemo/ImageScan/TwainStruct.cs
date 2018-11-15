using System;
using System.Runtime.InteropServices;
using log4net;
using System.Collections;
using UFileClient.Common;
using System.IO;

namespace ImageScan.TwainLib
{
    /// <summary>
    /// 协议结构体
    /// </summary>
    public static class TwProtocol
    {
        public const short Major = 1;
        public const short Minor = 9;
    }

    /// <summary>
    /// 控制信息
    /// </summary>
    [Flags]
    internal enum TwDG : short
    {
        Control = 0x0001,
        Image = 0x0002,
        Audio = 0x0004
    }

    /// <summary>
    /// DAT类型
    /// </summary>
    internal enum TwDAT : short
    {									// DAT_....
        Null = 0x0000,
        Capability = 0x0001,
        Event = 0x0002,
        Identity = 0x0003,
        Parent = 0x0004,
        PendingXfers = 0x0005,
        SetupMemXfer = 0x0006,
        SetupFileXfer = 0x0007,
        Status = 0x0008,
        UserInterface = 0x0009,
        XferGroup = 0x000a,
        TwunkIdentity = 0x000b,
        CustomDSData = 0x000c,
        DeviceEvent = 0x000d,
        FileSystem = 0x000e,
        PassThru = 0x000f,

        ImageInfo = 0x0101,
        ImageLayout = 0x0102,
        ImageMemXfer = 0x0103,
        ImageNativeXfer = 0x0104,
        ImageFileXfer = 0x0105,
        CieColor = 0x0106,
        GrayResponse = 0x0107,
        RGBResponse = 0x0108,
        JpegCompression = 0x0109,
        Palette8 = 0x010a,
        ExtImageInfo = 0x010b,

        SetupFileXfer2 = 0x0301
    }

    public enum TwCapStatus : short
    {
        Disabled = 0,
        Unmodified = 1,
        Modified = 2
    }

    /// <summary>
    /// 消息类型
    /// </summary>
    internal enum TwMSG : short
    {									// MSG_.....
        Null = 0x0000,
        Get = 0x0001,
        GetCurrent = 0x0002,
        GetDefault = 0x0003,
        GetFirst = 0x0004,
        GetNext = 0x0005,
        Set = 0x0006,
        Reset = 0x0007,
        QuerySupport = 0x0008,

        XFerReady = 0x0101,
        CloseDSReq = 0x0102,
        CloseDSOK = 0x0103,
        DeviceEvent = 0x0104,

        CheckStatus = 0x0201,

        OpenDSM = 0x0301,
        CloseDSM = 0x0302,

        OpenDS = 0x0401,
        CloseDS = 0x0402,
        UserSelect = 0x0403,

        DisableDS = 0x0501,
        EnableDS = 0x0502,
        EnableDSUIOnly = 0x0503,

        ProcessEvent = 0x0601,

        EndXfer = 0x0701,
        StopFeeder = 0x0702,

        ChangeDirectory = 0x0801,
        CreateDirectory = 0x0802,
        Delete = 0x0803,
        FormatMedia = 0x0804,
        GetClose = 0x0805,
        GetFirstFile = 0x0806,
        GetInfo = 0x0807,
        GetNextFile = 0x0808,
        Rename = 0x0809,
        Copy = 0x080A,
        AutoCaptureDir = 0x080B,

        PassThru = 0x0901
    }

    /// <summary>
    /// 返回值类型
    /// </summary>
    public enum TwRC : short
    {
        BadValue = -2,
        TransferError = -1,
        Success = 0x0000,
        Failure = 0x0001,
        CheckStatus = 0x0002,
        Cancel = 0x0003,
        DSEvent = 0x0004,
        NotDSEvent = 0x0005,
        XferDone = 0x0006,
        EndOfList = 0x0007,
        InfoNotSupported = 0x0008,
        DataNotAvailable = 0x0009
    }

    /// <summary>
    /// 条件码类型
    /// </summary>
    internal enum TwCC : short
    {
        FailedToGetLastError = -1,// TWCC_....
        Success = 0x0000,
        Bummer = 0x0001,
        LowMemory = 0x0002,
        NoDS = 0x0003,
        MaxConnections = 0x0004,
        OperationError = 0x0005,
        BadCap = 0x0006,
        BadProtocol = 0x0009,
        BadValue = 0x000a,
        SeqError = 0x000b,
        BadDest = 0x000c,
        CapUnsupported = 0x000d,
        CapBadOperation = 0x000e,
        CapSeqError = 0x000f,
        Denied = 0x0010,
        FileExists = 0x0011,
        FileNotFound = 0x0012,
        NotEmpty = 0x0013,
        PaperJam = 0x0014,
        PaperDoubleFeed = 0x0015,
        FileWriteError = 0x0016,
        CheckDeviceOnline = 0x0017
    }

    /// <summary>
    ///数据结构类型
    /// </summary>
    public enum TwOn : short
    {									// TWON_....
        Array = 0x0003,
        Enum = 0x0004,
        One = 0x0005,
        Range = 0x0006,
        DontCare = -1
    }

    /// <summary>
    /// 能力类型
    /// </summary>
    public enum TwCap : ushort
    {

        CAP_XferCount = 0x0001,			// CAP_XFERCOUNT
        ICAP_COMPRESSION = 0x0100,			// ICAP_...
        ICAP_PixelType = 0x0101,
        ICAP_Units = 0x0102,
        ICAP_XferMech = 0x0103,

        CAP_AUTHOR = 0x1000,
        CAP_CAPTION = 0x1001,
        CAP_FEEDERENABLED = 0x1002,
        CAP_FEEDERLOADED = 0x1003,
        CAP_TIMEDATE = 0x1004,
        CAP_SUPPORTEDCAPS = 0x1005,
        CAP_EXTENDEDCAPS = 0x1006,
        CAP_AUTOFEED = 0x1007,
        CAP_CLEARPAGE = 0x1008,
        CAP_FEEDPAGE = 0x1009,
        CAP_REWINDPAGE = 0x100a,
        CAP_INDICATORS = 0x100b,
        CAP_SUPPORTEDCAPSEXT = 0x100c,
        CAP_PAPERDETECTABLE = 0x100d,
        CAP_UICONTROLLABLE = 0x100e,
        CAP_DEVICEONLINE = 0x100f,
        CAP_AUTOSCAN = 0x1010,
        CAP_THUMBNAILSENABLED = 0x1011,
        CAP_DUPLEX = 0x1012,
        CAP_DUPLEXENABLED = 0x1013,
        CAP_ENABLEDSUIONLY = 0x1014,
        CAP_CUSTOMDSDATA = 0x1015,
        CAP_ENDORSER = 0x1016,
        CAP_JOBCONTROL = 0x1017,
        CAP_ALARMS = 0x1018,
        CAP_ALARMVOLUME = 0x1019,
        CAP_AUTOMATICCAPTURE = 0x101a,
        CAP_TIMEBEFOREFIRSTCAPTURE = 0x101b,
        CAP_TIMEBETWEENCAPTURES = 0x101c,
        CAP_CLEARBUFFERS = 0x101d,
        CAP_MAXBATCHBUFFERS = 0x101e,
        CAP_DEVICETIMEDATE = 0x101f,
        CAP_POWERSUPPLY = 0x1020,
        CAP_CAMERAPREVIEWUI = 0x1021,
        CAP_DEVICEEVENT = 0x1022,
        CAP_SERIALNUMBER = 0x1024,
        CAP_PRINTER = 0x1026,
        CAP_PRINTERENABLED = 0x1027,
        CAP_PRINTERINDEX = 0x1028,
        CAP_PRINTERMODE = 0x1029,
        CAP_PRINTERSTRING = 0x102a,
        CAP_PRINTERSUFFIX = 0x102b,
        CAP_LANGUAGE = 0x102c,
        CAP_FEEDERALIGNMENT = 0x102d,
        CAP_FEEDERORDER = 0x102e,
        CAP_REACQUIREALLOWED = 0x1030,
        CAP_BATTERYMINUTES = 0x1032,
        CAP_BATTERYPERCENTAGE = 0x1033,

        ICAP_AUTOBRIGHT = 0x1100,
        ICAP_BRIGHTNESS = 0x1101,
        ICAP_CONTRAST = 0x1103,
        ICAP_CUSTHALFTONE = 0x1104,
        ICAP_EXPOSURETIME = 0x1105,
        ICAP_FILTER = 0x1106,
        ICAP_FLASHUSED = 0x1107,
        ICAP_GAMMA = 0x1108,
        ICAP_HALFTONES = 0x1109,
        ICAP_HIGHLIGHT = 0x110a,
        ICAP_IMAGEFILEFORMAT = 0x110c,
        ICAP_LAMPSTATE = 0x110d,
        ICAP_LIGHTSOURCE = 0x110e,
        ICAP_ORIENTATION = 0x1110,
        ICAP_PHYSICALWIDTH = 0x1111,
        ICAP_PHYSICALHEIGHT = 0x1112,
        ICAP_SHADOW = 0x1113,
        ICAP_FRAMES = 0x1114,
        ICAP_XNATIVERESOLUTION = 0x1116,
        ICAP_YNATIVERESOLUTION = 0x1117,
        ICAP_XRESOLUTION = 0x1118,
        ICAP_YRESOLUTION = 0x1119,
        ICAP_MAXFRAMES = 0x111a,
        ICAP_TILES = 0x111b,
        ICAP_BITORDER = 0x111c,
        ICAP_CCITTKFACTOR = 0x111d,
        ICAP_LIGHTPATH = 0x111e,
        ICAP_PIXELFLAVOR = 0x111f,
        ICAP_PLANARCHUNKY = 0x1120,
        ICAP_ROTATION = 0x1121,
        ICAP_SUPPORTEDSIZES = 0x1122,
        ICAP_THRESHOLD = 0x1123,
        ICAP_XSCALING = 0x1124,
        ICAP_YSCALING = 0x1125,
        ICAP_BITORDERCODES = 0x1126,
        ICAP_PIXELFLAVORCODES = 0x1127,
        ICAP_JPEGPIXELTYPE = 0x1128,
        ICAP_TIMEFILL = 0x112a,
        ICAP_BITDEPTH = 0x112b,
        ICAP_BITDEPTHREDUCTION = 0x112c,
        ICAP_UNDEFINEDIMAGESIZE = 0x112d,
        ICAP_IMAGEDATASET = 0x112e,
        ICAP_EXTIMAGEINFO = 0x112f,
        ICAP_MINIMUMHEIGHT = 0x1130,
        ICAP_MINIMUMWIDTH = 0x1131,
        ICAP_AUTODISCARDBLANKPAGES = 0x1134,
        ICAP_FLIPROTATION = 0x1136,
        ICAP_BARCODEDETECTIONENABLED = 0x1137,
        ICAP_SUPPORTEDBARCODETYPES = 0x1138,
        ICAP_BARCODEMAXSEARCHPRIORITIES = 0x1139,
        ICAP_BARCODESEARCHPRIORITIES = 0x113a,
        ICAP_BARCODESEARCHMODE = 0x113b,
        ICAP_BARCODEMAXRETRIES = 0x113c,
        ICAP_BARCODETIMEOUT = 0x113d,
        ICAP_ZOOMFACTOR = 0x113e,
        ICAP_PATCHCODEDETECTIONENABLED = 0x113f,
        ICAP_SUPPORTEDPATCHCODETYPES = 0x1140,
        ICAP_PATCHCODEMAXSEARCHPRIORITIES = 0x1141,
        ICAP_PATCHCODESEARCHPRIORITIES = 0x1142,
        ICAP_PATCHCODESEARCHMODE = 0x1143,
        ICAP_PATCHCODEMAXRETRIES = 0x1144,
        ICAP_PATCHCODETIMEOUT = 0x1145,
        ICAP_FLASHUSED2 = 0x1146,
        ICAP_IMAGEFILTER = 0x1147,
        ICAP_NOISEFILTER = 0x1148,
        ICAP_OVERSCAN = 0x1149,
        ICAP_AUTOMATICBORDERDETECTION = 0x1150,
        ICAP_AUTOMATICDESKEW = 0x1151,
        ICAP_AUTOMATICROTATE = 0x1152,
        ICAP_JPEGQUALITY = 0x1153,

        ACAP_AUDIOFILEFORMAT = 0x1201,
        ACAP_XFERMECH = 0x1202,

        TWEI_BARCODEX = 0x1200,
        TWEI_BARCODEY = 0x1201,
        TWEI_BARCODETEXT = 0x1202,
        TWEI_BARCODETYPE = 0x1203,
        TWEI_DESHADETOP = 0x1204,
        TWEI_DESHADELEFT = 0x1205,
        TWEI_DESHADEHEIGHT = 0x1206,
        TWEI_DESHADEWIDTH = 0x1207,
        TWEI_DESHADESIZE = 0x1208,
        TWEI_SPECKLESREMOVED = 0x1209,
        TWEI_HORZLINEXCOORD = 0x120A,
        TWEI_HORZLINEYCOORD = 0x120B,
        TWEI_HORZLINELENGTH = 0x120C,
        TWEI_HORZLINETHICKNESS = 0x120D,
        TWEI_VERTLINEXCOORD = 0x120E,
        TWEI_VERTLINEYCOORD = 0x120F,
        TWEI_VERTLINELENGTH = 0x1210,
        TWEI_VERTLINETHICKNESS = 0x1211,
        TWEI_PATCHCODE = 0x1212,
        TWEI_ENDORSEDTEXT = 0x1213,
        TWEI_FORMCONFIDENCE = 0x1214,
        TWEI_FORMTEMPLATEMATCH = 0x1215,
        TWEI_FORMTEMPLATEPAGEMATCH = 0x1216,
        TWEI_FORMHORZDOCOFFSET = 0x1217,
        TWEI_FORMVERTDOCOFFSET = 0x1218,
        TWEI_BARCODECOUNT = 0x1219,
        TWEI_BARCODECONFIDENCE = 0x121A,
        TWEI_BARCODEROTATION = 0x121B,
        TWEI_BARCODETEXTLENGTH = 0x121C,
        TWEI_DESHADECOUNT = 0x121D,
        TWEI_DESHADEBLACKCOUNTOLD = 0x121E,
        TWEI_DESHADEBLACKCOUNTNEW = 0x121F,
        TWEI_DESHADEBLACKRLMIN = 0x1220,
        TWEI_DESHADEBLACKRLMAX = 0x1221,
        TWEI_DESHADEWHITECOUNTOLD = 0x1222,
        TWEI_DESHADEWHITECOUNTNEW = 0x1223,
        TWEI_DESHADEWHITERLMIN = 0x1224,
        TWEI_DESHADEWHITERLAVE = 0x1225,
        TWEI_DESHADEWHITERLMAX = 0x1226,
        TWEI_BLACKSPECKLESREMOVED = 0x1227,
        TWEI_WHITESPECKLESREMOVED = 0x1228,
        TWEI_HORZLINECOUNT = 0x1229,
        TWEI_VERTLINECOUNT = 0x122A,
        TWEI_DESKEWSTATUS = 0x122B,
        TWEI_SKEWORIGINALANGLE = 0x122C,
        TWEI_SKEWFINALANGLE = 0x122D,
        TWEI_SKEWCONFIDENCE = 0x122E,
        TWEI_SKEWWINDOWX1 = 0x122F,
        TWEI_SKEWWINDOWY1 = 0x1230,
        TWEI_SKEWWINDOWX2 = 0x1231,
        TWEI_SKEWWINDOWY2 = 0x1232,
        TWEI_SKEWWINDOWX3 = 0x1233,
        TWEI_SKEWWINDOWY3 = 0x1234,
        TWEI_SKEWWINDOWX4 = 0x1235,
        TWEI_SKEWWINDOWY4 = 0x1236,
        TWEI_BOOKNAME = 0x1238, /* added 1.9 */
        TWEI_CHAPTERNUMBER = 0x1239, /* added 1.9 */
        TWEI_DOCUMENTNUMBER = 0x123A, /* added 1.9 */
        TWEI_PAGENUMBER = 0x123B, /* added 1.9 */
        TWEI_CAMERA = 0x123C, /* added 1.9 */
        TWEI_FRAMENUMBER = 0x123D,  /* added 1.9 */
        TWEI_FRAME = 0x123E,  /* added 1.9 */
        TWEI_PIXELFLAVOR = 0x123F,  /* added 1.9 */

        CAP_PAPERSOURCE = 0x802C,
        CCAP_CEI_DOCUMENT_PLACE = 0x8062,
        ECAP_PAPERSOURCE = 0xFFFE
    }

    /// <summary>
    /// Twain状态描述
    /// </summary>
    public enum TwState : short
    {
        PreSession = 1,
        LoadDSM = 2,
        OpenDSM = 3,
        OpenDS = 4,
        EnableSource = 5,
        TransferReady = 6,
        Transfer = 7
    }

    /// <summary>
    /// 单位类型
    /// </summary>
    public enum TwUint : short
    {
        TWUN_INCHES = 0,
        TWUN_CENTIMETERS = 1,
        TWUN_PICAS = 2,
        TWUN_POINTS = 3,
        TWUN_TWIPS = 4,
        TWUN_PIXELS = 5
    }

    /// <summary>
    /// 传输模式类型
    /// </summary>
    public enum TwMode : short
    {
        NULL = -1,
        TWSX_NATIVE = 0,
        TWSX_FILE = 1,
        TWSX_MEMORY = 2
    }

    /// <summary>
    /// 文件格式类型
    /// </summary>
    public enum TwFileFormat : short
    {
        TWFF_NULL = -1,
        TWFF_TIFF = 0,
        TWFF_PICT = 1,
        TWFF_BMP = 2,
        TWFF_XBM = 3,
        TWFF_JFIF = 4,
        TWFF_FPX = 5,
        TWFF_TIFFMULTI = 6,
        TWFF_PNG = 7,
        TWFF_SPIFF = 8,
        TWFF_EXIF = 9
    }

    /// <summary>
    /// 文件压缩算法
    /// </summary>
    public enum TwCompression : short
    {
        TWCP_NULL = -1,
        TWCP_NONE = 0,
        TWCP_PACKBITS = 1,
        TWCP_GROUP31D = 2,
        TWCP_GROUP31DEOL = 3,
        TWCP_GROUP32D = 4,
        TWCP_GROUP4 = 5,
        TWCP_JPEG = 6,
        TWCP_LZW = 7,
        TWCP_JBIG = 8,
        /* Added 1.8 */
        TWCP_PNG = 9,
        TWCP_RLE4 = 10,
        TWCP_RLE8 = 11,
        TWCP_BITFIELDS = 12
    }

    /// <summary>
    /// 灰度等级类型
    /// </summary>
    public enum TwPixelType : short
    {
        TWPT_NULL = -1,
        TWPT_BW = 0,
        TWPT_GRAY,
        TWPT_RGB,
        TWPT_PALETTE,
        TWPT_CMY,
        TWPT_CMYK,
        TWPT_YUV,
        TWPT_YUVK,
        TWPT_CIEXYZ
    }


    /// <summary>
    /// 标示类
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Ansi)]
    internal class TwIdentity
    {									// TW_IDENTITY
        public IntPtr Id;
        public TwVersion Version;
        public short ProtocolMajor;
        public short ProtocolMinor;
        public int SupportedGroups;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 34)]
        public string Manufacturer;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 34)]
        public string ProductFamily;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 34)]
        public string ProductName;
    }

    /// <summary>
    /// 版本类
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Ansi)]
    internal struct TwVersion
    {									// TW_VERSION
        public short MajorNum;
        public short MinorNum;
        public short Language;
        public short Country;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 34)]
        public string Info;
    }

    /// <summary>
    /// 用户界面类
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwUserInterface
    {									// TW_USERINTERFACE
        public short ShowUI;				// bool is strictly 32 bit, so use short
        public short ModalUI;
        public IntPtr ParentHand;
    }

    /// <summary>
    /// 状态类
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwStatus
    {									// TW_STATUS
        public short ConditionCode;		// TwCC
        public short Reserved;
    }

    /// <summary>
    /// 事件类
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal struct TwEvent
    {									// TW_EVENT
        public IntPtr EventPtr;
        public short Message;
    }

    /// <summary>
    /// 图像信息类
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwImageInfo
    {									// TW_IMAGEINFO
        public int XResolution;
        public int YResolution;
        public int ImageWidth;
        public int ImageLength;
        public short SamplesPerPixel;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        public short[] BitsPerSample;
        public short BitsPerPixel;
        public short Planar;
        public short PixelType;
        public short Compression;
    }

    /// <summary>
    /// 图像传输过程查询类
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    internal class TwPendingXfers
    {									// TW_PENDINGXFERS
        public short Count;
        public int EOJ;
    }

    /// <summary>
    /// 文件信息配置类
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2, CharSet = CharSet.Ansi)]
    public class TwSetupFileXfer
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string FileName;
        public short Format;
        public short VRefNum;
    }

    /// <summary>
    /// 数据类型fix32类
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwFix32
    {
        public short Whole;
        public ushort Frac;

        public float ToFloat()
        {
            float f;
            f = (float)((float)Whole + (float)Frac / 65536.0);
            return f;
        }


        /// <summary>
        /// 将形如XXXX.XXXX的字符串转换为fix32数据类型
        /// </summary>
        /// <param name="itemValue">解析的字符串</param>
        /// <param name="fix32">解析完成后初始化的对象</param>
        /// <returns>解析成功返回true,否则返回false</returns>
        public static bool Parse(string itemValue, ref TwFix32 fix32)
        {
            ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.GetConfigDir() + ConstCommon.logConfigFileName));
            try
            {
                // 1:按照.对字符串进行拆分
                char[] separator = { '.' };
                string[] results = itemValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);

                fix32.Whole = short.MinValue;
                fix32.Frac = ushort.MinValue;

                //2:如果字符串非空
                if (results.Length > 0)
                {
                    //2.3.1:给fix32整数部分进行赋值
                    fix32.Whole = short.Parse(results[0]);
                    //2.3.1:给fix32小数部分进行赋值
                    if (results.Length == 2)
                    {
                        fix32.Frac = (ushort)(double.Parse(results[1]));
                    }
                    else
                        fix32.Frac = 0;
                }
                return true;
            }
            #region 异常处理
            catch (ArgumentNullException e)
            {

                logger.Error("argument is null in TwFix32.Parse function " + e.Message);
                return false;
            }
            catch (FormatException e)
            {
                logger.Error("FormatException is wrong in TwFix32.Parse function  " + e.Message);
                return false;
            }
            catch (OverflowException e)
            {
                logger.Error("OverflowException occured in TwFix32.Parse function " + e.Message);
                return false;
            }
            catch (Exception e)
            {
                logger.Error("Exception occured in TwFix32.Parse function " + e.Message);
                return false;
            }
            #endregion
        }

        /// <summary>
        /// 重载ToString函数
        /// </summary>
        /// <returns>fix32的字符串表示</returns>
        public override string ToString()
        {
            if (Frac == 0)
            {
                return Whole.ToString();
            }
            return Whole.ToString() + "." + Frac.ToString();
        }
        /// <summary>
        /// 从float转换到TwFix32
        /// </summary>
        /// <param name="floatValue"></param>
        /// <returns></returns>
        public static TwFix32 FloatToFix32(float floatValue)
        {
            TwFix32 fix32 = new TwFix32();
            Int32 value = (Int32)(floatValue * 65536.0 + 0.5);
            fix32.Whole = (Int16)(value >> 16);
            fix32.Frac = (UInt16)(value & 0x0000ffffL);
            return fix32;
        }
    }

    /// <summary>
    /// 数据类型frame
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwFrame
    {
        public TwFix32 Left;
        public TwFix32 Top;
        public TwFix32 Right;
        public TwFix32 Bottom;

        /// <summary>
        /// 将形如XXXX.XXXX:XXXX.XXXX:XXXX.XXXX:XXXX.XXXX的字符串转换为frame数据类型
        /// </summary>
        /// <param name="itemValue">解析的字符串</param>
        /// <param name="fix32">解析完成后初始化的对象</param>
        /// <returns>解析成功返回true,否则返回false</returns>
        public static bool Parse(string itemValue, ref TwFrame twFrame)
        {
            char[] separator = { ':' };
            string[] results = itemValue.Split(separator, StringSplitOptions.RemoveEmptyEntries);

            //字符串数组不为4个
            if (results.Length != 4)
            {
                return false;
            }

            if (!(TwFix32.Parse(results[0], ref twFrame.Left)
                && TwFix32.Parse(results[1], ref twFrame.Top)
                && TwFix32.Parse(results[2], ref twFrame.Right)
                && TwFix32.Parse(results[3], ref twFrame.Bottom)))
            {
                return false;
            }
            else
                return true;

        }

        /// <summary>
        /// 重载ToString函数
        /// </summary>
        /// <returns>frame的字符串表示</returns>
        public override string ToString()
        {
            return Left.ToString() + ":" + Top.ToString() + ":" + Right.ToString() + ":" + Bottom.ToString();
        }
    }

    /// <summary>
    /// 数据类型
    /// </summary>
    public enum TwType : ushort
    {
        Null = 0xFFFF,
        Int8 = 0x0000,
        Int16 = 0x0001,
        Int32 = 0x0002,
        UInt8 = 0x0003,
        UInt16 = 0x0004,
        UInt32 = 0x0005,
        Bool = 0x0006,
        Fix32 = 0x0007,
        Frame = 0x0008,
        Str32 = 0x0009,
        Str64 = 0x000a,
        Str128 = 0x000b,
        Str255 = 0x000c
    }

    /// <summary>
    /// 数据类型为fix32d的结构体
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwOneValueFix32
    {
        public TwType ItemType;
        public TwFix32 Item;
    }
    /// <summary>
    /// 数据类型为整数的结构体
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwOneValueIntegerOrBool
    {
        public TwType ItemType;
        public UInt32 Item;
    }
    /// <summary>
    /// OneValue的数据记录
    /// </summary>
    public class TwOneValue
    {
        public TwType ItemType
        { get; set; }
        public string ItemStr
        { get; set; }
        public TwOneValue(TwType itemType, string itemStr)
        {
            this.ItemStr = itemStr;
            this.ItemType = itemType;
        }
    }

    /// <summary>
    /// 能力结构描述类
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwCapability
    {
        public TwCap capID;//能力类型
        public TwOn contentType;//扫描能力的数据类型
        public IntPtr contentValuePtr;//数据类型所指向的句柄      
        public TwCapability(TwCap capID)
        {
            this.capID = capID;
            this.contentType = TwOn.DontCare;
            this.contentValuePtr = IntPtr.Zero;
        }
    }



    /// <summary>
    /// 数组类型
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwArray
    {
        public TwType ItemType;
        public uint NumItems;
        public IntPtr ItemList;
    }

    /// <summary>
    /// 枚举类型
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwEnumeration
    {
        public TwType ItemType;
        public uint NumItems;
        public uint CurrentIndex;
        public uint DefaultIndex;
        public IntPtr ItemList;
    }

    /// <summary>
    /// 范围类型
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwRangeIntegerOrBool
    {
        public TwType ItemType;
        public uint MinValue;
        public uint MaxValue;
        public uint StepSize;
        public uint DefaultValue;
        public uint CurrentValue;
    }

    /// <summary>
    /// 范围类型
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwRangeFix32
    {
        public TwType ItemType;
        public TwFix32 MinValue;
        public TwFix32 MaxValue;
        public TwFix32 StepSize;
        public TwFix32 DefaultValue;
        public TwFix32 CurrentValue;

    }

    /// <summary>
    /// 长度为32的字符串
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwStr32
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 34)]
        public string str;
    }

    /// <summary>
    /// 长度为64的字符串
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwStr64
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 66)]
        public string str;
    }

    /// <summary>
    /// 长度为128的字符串
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwStr128
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 130)]
        public string str;
    }

    /// <summary>
    /// 长度为256的字符串
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 2)]
    public struct TwStr256
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string str;
    }

    //是否调用图像处理函数标志
    public struct PicProcessorApiCallFlag
    {
        public bool removeBlanckEdge;
        public bool reviseTwist;
        public bool isBlank;
    }
}
