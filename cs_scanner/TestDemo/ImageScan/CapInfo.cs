using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ImageScan.TwainLib;
using log4net;
using System.Runtime.InteropServices;
using System.Collections;
using System.Xml;
using UFileClient.Common;
using System.IO;

namespace ImageScan
{
    public class CapInfo
    {
        public CapInfo()
        {
        }
        public CapInfo(TwCap capId, TwainSession twSession)
        {
            this.capId = capId;
            this.twSession = twSession;
        }
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static CapInfo()
        {
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.GetConfigDir() + ConstCommon.logConfigFileName));
        }
        private TwCap capId;               //能力名称
        public TwCap CapId
        {
            set { capId = value; }
            get { return capId; }
        }
        public TwType capType = TwType.Null;         //数据列表中的数据类型
        public TwType CapType
        {
            get { return capType; }
            set { capType = value; }
        }
        private string currentIntStr = null;         //当前值,字符串形式
        public string CurrentIntStr
        {
            get
            {
                if (currentIntStr == null)
                {
                    currentIntStr = GetCurrent();
                }
                return currentIntStr;
            }
            set { currentIntStr = value; }
        }
        private string defaultIntStr = null;         //默认值,字符串形式
        public string DefaultIntStr
        {
            get
            {
                if (defaultIntStr == null)
                {
                    defaultIntStr = GetDefalut();
                }
                return defaultIntStr;
            }
            set
            {
                defaultIntStr = value;
            }
        }
        private ArrayList valueList;                 //数据列表
        public TwainSession twSession;
        public bool hasPlaten;                      //是否有平板


        public void GetPaperSource()
        {
            if (twSession.defaultScannerName.ToLower().Contains("kodak"))
            {
                capId = TwCap.CAP_PAPERSOURCE;


                CapItem capItem = twSession.GetCapabilitySupportedDataList(TwCap.CAP_PAPERSOURCE);
                CapType = capItem.listDataType;
                CurrentIntStr = twSession.GetCapabilityCurrentValue(TwCap.CAP_PAPERSOURCE, ref capType);
                DefaultIntStr = twSession.GetCapabilityDefaultValue(TwCap.CAP_PAPERSOURCE, ref capType);

                if (capItem.list != null && capItem.list.Count == 3)
                {
                    hasPlaten = true;
                    if (valueList == null)
                        valueList = new ArrayList();
                    valueList.Clear();
                    valueList.Add(PaperSouceString.ADF);
                    valueList.Add(PaperSouceString.Platen);
                }
                //如果是自动模式，则设置为送纸器
                if (currentIntStr.Equals("0"))
                {
                    bool flag = SetCap("1");
                    if (flag)
                        currentIntStr = "1";
                }
                capId = TwCap.ECAP_PAPERSOURCE;
            }
        }
        public bool GetCap()
        {
            bool flag = true;
            if (capId == TwCap.ICAP_XRESOLUTION || capId == TwCap.ICAP_YRESOLUTION)
                flag = SetInch();
            //if (capId == TwCap.ICAP_THRESHOLD)
            //{
            //    flag = SetBitDepthReduction();
            //}
            if (flag)
            {
                if (capId == TwCap.ECAP_PAPERSOURCE)
                {
                    GetPaperSource();
                }
                else
                {
                    TwCapability cap = new TwCapability(capId);

                    if (twSession.GetCapability(ref cap))
                    {
                        IntPtr intPtr = cap.contentValuePtr;
                        switch (cap.contentType)
                        {
                            case TwOn.One:
                                GetCapFromOneValue(intPtr);
                                break;
                            case TwOn.Array:
                                GetCapFromArray(intPtr);
                                break;
                            case TwOn.Enum:
                                GetCapFromEnumeration(intPtr);
                                break;
                            case TwOn.Range:
                                GetCapFromRange(intPtr);
                                break;
                            default:
                                {
                                    logger.Debug("the contentType in TwCapability is wrong");
                                    flag = false;
                                }
                                break;
                        }
                    }
                    if (cap.contentValuePtr != null)
                        TwainSession.GlobalFree(cap.contentValuePtr);
                }
            }
            return flag;
        }
        public bool SetCap(string val)
        {
            this.CurrentIntStr = val;
            if (this.CapId == TwCap.ICAP_XRESOLUTION)
            {
                SetInch();
            }
            //if (this.capId == TwCap.ICAP_THRESHOLD)
            //{
            //    SetBitDepthReduction();
            //}
            // 如果是单双面，先设置进纸器使能
            if (this.CapId == TwCap.CAP_DUPLEXENABLED)
            {
                twSession.SetFeederEnabled();
            }
            return twSession.SetCapability(this);
        }

        public bool SetCap()
        {
            if (this.capId == TwCap.ICAP_XRESOLUTION)
            {
                SetInch();
            }
            if (capId == TwCap.ECAP_PAPERSOURCE)
                return SetPaperSource();
            else
                return twSession.SetCapability(this);
        }
        public bool SetPaperSource()
        {
            bool flag = false;
            if (twSession.defaultScannerName.ToLower().Contains("kodak"))
            {
                capId = TwCap.CAP_PAPERSOURCE;
                TwType type = capType;
                CapInfo tmpCap = new CapInfo();
                tmpCap.capId = capId;
                tmpCap.capType = capType;
                tmpCap.currentIntStr = currentIntStr;
                flag = twSession.SetCapability(tmpCap);
                capId = TwCap.ECAP_PAPERSOURCE;
            }
            return flag;
        }

        public List<string> GetDataList()
        {
            if (capId == TwCap.ICAP_IMAGEFILEFORMAT)
            {
                CapInfo xferCapInfo = twSession.GetScannerCap(TwCap.ICAP_XferMech);
                if (xferCapInfo.currentIntStr == "0") // native模式下的文件格式
                {
                    List<string> tmpCapValueList = new List<string>();
                    tmpCapValueList.Add("0"); // TWFF_TIFF
                    tmpCapValueList.Add("2"); // TWFF_BMP
                    tmpCapValueList.Add("4"); // TWFF_JFIF
                    tmpCapValueList.Add("7"); // TWFF_PNG
                    return tmpCapValueList;
                }
            }
            if (capId == TwCap.ICAP_COMPRESSION)
            {
                CapInfo xferCapInfo = twSession.GetScannerCap(TwCap.ICAP_XferMech);
                if (xferCapInfo.currentIntStr == "0") // native模式下的压缩格式
                {
                    List<string> tmpCapValueList = new List<string>();
                    tmpCapValueList.Add("0"); // TWCP_NONE
                    CapInfo pixelCapInfo = twSession.GetScannerCap(TwCap.ICAP_PixelType);
                    if (pixelCapInfo.CurrentIntStr == "0") //BW
                    {
                        tmpCapValueList.Add("5"); // TWCP_GROUP4
                    }
                    return tmpCapValueList;
                }
            }

            if (valueList == null)
            {
                return null;
            }
            List<string> capValueList = new List<string>();
            foreach (object item in valueList)
            {
                string capValueStr = item.ToString();
                capValueList.Add(capValueStr);
            }
            //if (capId == TwCap.ICAP_IMAGEFILEFORMAT) // File
            //{
            //    //
            //}
            if (capId == TwCap.ICAP_COMPRESSION) // File
            {
                CapInfo formatCapInfoF = twSession.GetScannerCap(TwCap.ICAP_IMAGEFILEFORMAT);
                if (formatCapInfoF.CurrentIntStr == "4") // 去掉JPG以外的压缩方式
                {
                    // 去掉JPG以外的文件格式
                    List<string> delList = new List<string>();
                    foreach (string value in capValueList)
                    {
                        if (value != "6")
                        {
                            delList.Add(value);
                        }
                    }
                    foreach (string value in delList)
                    {
                        capValueList.Remove(value);
                    }
                }
            }
            return capValueList;
        }
        private string GetCurrent()
        {
            return twSession.GetCapabilityCurrentValue(this.capId, ref capType);
        }
        private string GetDefalut()
        {
            return twSession.GetCapabilityDefaultValue(this.capId, ref capType);
        }

        private void GetItemAsFix32(IntPtr intPtr, TwFix32 fix32, int offset)
        {
            return;
        }
        private void GetItemAsUInt32(IntPtr intPtr, ref UInt32 val, TwType type, int offset)
        {
            return;
        }
        private void GetItemAsString(IntPtr intPtr, ref string val, TwType type, int offset)
        {
            return;
        }
        private bool GetCapFromOneValue(IntPtr intPtr)
        {
            bool bret = true;
            IntPtr tempPtr = TwainSession.GlobalLock(intPtr);
            try
            {
                TwOneValueIntegerOrBool one = (TwOneValueIntegerOrBool)Marshal.PtrToStructure(tempPtr, typeof(TwOneValueIntegerOrBool));
                IntPtr p = new IntPtr(tempPtr.ToInt32() + Marshal.SizeOf(one) - Marshal.SizeOf(one.Item));
                capType = one.ItemType;
                valueList = twSession.GetDataListFromPointer(one.ItemType, p, 1);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                TwainSession.GlobalUnlock(tempPtr);
            }
            return bret;
        }
        private bool GetCapFromEnumeration(IntPtr intPtr)
        {
            bool bret = true;
            IntPtr tempPtr = TwainSession.GlobalLock(intPtr);
            try
            {
                TwEnumeration enu = (TwEnumeration)Marshal.PtrToStructure(tempPtr, typeof(TwEnumeration));
                IntPtr p = new IntPtr(tempPtr.ToInt32() + Marshal.SizeOf(enu) - Marshal.SizeOf(enu.ItemList));
                capType = enu.ItemType;
                valueList = twSession.GetDataListFromPointer(enu.ItemType, p, enu.NumItems);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                TwainSession.GlobalUnlock(tempPtr);
            }
            return bret;
        }
        private bool GetCapFromArray(IntPtr intPtr)
        {
            bool bret = true;
            IntPtr tempPtr = TwainSession.GlobalLock(intPtr);
            try
            {
                TwArray array = (TwArray)Marshal.PtrToStructure(tempPtr, typeof(TwArray));
                IntPtr p = new IntPtr(tempPtr.ToInt32() + Marshal.SizeOf(array) - Marshal.SizeOf(array.ItemList));
                capType = array.ItemType;
                valueList = twSession.GetDataListFromPointer(array.ItemType, p, array.NumItems);
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                TwainSession.GlobalUnlock(tempPtr);
            }
            return bret;
        }
        private bool GetCapFromRange(IntPtr intPtr)
        {
            bool bret = true;
            IntPtr tempPtr = TwainSession.GlobalLock(intPtr);
            try
            {
                TwRangeIntegerOrBool range = (TwRangeIntegerOrBool)Marshal.PtrToStructure(tempPtr, typeof(TwRangeIntegerOrBool));
                ArrayList list = new ArrayList();

                //fix32类型
                if (range.ItemType == TwType.Fix32)
                {
                    TwRangeFix32 rangeFix32 = (TwRangeFix32)Marshal.PtrToStructure(tempPtr, typeof(TwRangeFix32));
                    // 特殊处理
                    if (rangeFix32.CurrentValue.Frac == 0 && rangeFix32.StepSize.Frac == 0)
                    {
                        rangeFix32.MinValue.Frac = 0;
                    }
                    for (long val = (long)rangeFix32.MinValue.ToFloat(); val <= (long)rangeFix32.MaxValue.ToFloat(); val += (long)rangeFix32.StepSize.ToFloat())
                    {
                       list.Add(val);
                    }

                }
                //UINT8 UINT16 UINT32 INT8 INT16 INT32类型
                else
                    list = twSession.GetRangeDataList(range.ItemType, range.MinValue, range.MaxValue, range.StepSize); //对数据列表进行赋值

                capType = range.ItemType;
                valueList = list;
            }
            catch (Exception)
            {
                return false;
            }
            finally
            {
                TwainSession.GlobalUnlock(tempPtr);
            }
            return bret;
        }
        private bool SetInch()
        {
            return twSession.SetUint(TwUint.TWUN_INCHES);
        }
        private bool SetBitDepthReduction()
        {
            return twSession.SetBitDepthReduction();
        }
    }

    public static class  PaperSouceString
    {
        public static string ADF = "ADF进纸器";
        public static string Platen = "平板进纸器";
    }

}
