using System.Runtime.InteropServices;
using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Xml;
using log4net;
using ImageScan;


namespace ImageScan.TwainLib
{
    public partial class TwainSession
    {
        private UserConfig userConfig;
        /// <summary>
        /// 根据指定的数据类型，将oneValue对象的itemType用字符串表示
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="valueType">oneValue对象</param>
        /// <returns></returns>
        private string GetStringFromOneValueObject(TwType dataType, ValueType valueType)
        {
            if (valueType == null)
                return null;

            if (dataType == TwType.Fix32)
            {
                TwOneValueFix32 oneValueFix32 = (TwOneValueFix32)valueType;
                return oneValueFix32.Item.ToString();
            }
            else
            {
                TwOneValueIntegerOrBool oneValue = (TwOneValueIntegerOrBool)valueType;
                switch (oneValue.ItemType)
                {
                    case TwType.Bool:
                        return ((UInt16)(oneValue.Item)).ToString();
                    case TwType.Int8:
                        return ((SByte)(oneValue.Item)).ToString();
                    case TwType.Int16:
                        return ((Int16)(oneValue.Item)).ToString();
                    case TwType.Int32:
                        return ((Int32)(oneValue.Item)).ToString();
                    case TwType.UInt8:
                        return ((Byte)(oneValue.Item)).ToString();
                    case TwType.UInt16:
                        return ((UInt16)(oneValue.Item)).ToString();
                    case TwType.UInt32:
                        return ((UInt32)(oneValue.Item)).ToString();
                    default:
                        {
                            logger.Debug("cannot convert data type " + oneValue.ItemType + " to string");
                            return null;
                        }
                }
            }
        }

        /// <summary>
        /// 根据指定的数据类型，生成具体的oneValue对象
        /// </summary>
        /// <param name="dataType">数据类型</param>
        /// <param name="strValue">数据类型的字符串表示</param>
        /// <returns>具体的oneValue对象</returns>
        private ValueType GetOneValueObjectFromString(TwType dataType, string strValue)
        {
            try
            {
                if (String.IsNullOrEmpty(strValue))
                {
                    logger.Debug("etOneValueObjectFromString : the parameter string is null");
                    return null;
                }

                //对于fix32类型，从string转换到float，再转换到Fix32，生成TwOneValueFix32对象
                if (dataType == TwType.Fix32)
                {
                    TwOneValueFix32 oneValueFix32 = new TwOneValueFix32();
                    TwFix32 fix32 = new TwFix32();
                    if (TwFix32.Parse(strValue, ref fix32))
                    {
                        oneValueFix32.Item = fix32;
                        oneValueFix32.ItemType = TwType.Fix32;
                        return oneValueFix32;
                    }
                    else
                    {
                        return null;
                    }
                }
                //对于fix32类型，生成TwOneValue对象
                else
                {
                    UInt32 val;
                    switch (dataType)
                    {
                        case TwType.Bool:
                            val = (UInt32)UInt16.Parse(strValue);
                            break;
                        case TwType.Int8:
                            val = (UInt32)SByte.Parse(strValue);
                            break;
                        case TwType.UInt8:
                            val = (UInt32)Byte.Parse(strValue);
                            break;
                        case TwType.Int16:
                            val = (UInt32)Int16.Parse(strValue);
                            break;
                        case TwType.UInt16:
                            val = (UInt32)UInt16.Parse(strValue);
                            break;
                        case TwType.Int32:
                            val = (UInt32)Int32.Parse(strValue);
                            break;
                        case TwType.UInt32:
                            val = (UInt32)UInt32.Parse(strValue);
                            break;
                        default:
                            {
                                logger.Debug("getOneValueObjectFromString() cannot convert to data object dataType = " + dataType);
                                return null;
                            }
                    }

                    TwOneValueIntegerOrBool oneValue = new TwOneValueIntegerOrBool();
                    oneValue.ItemType = dataType;
                    oneValue.Item = val;
                    return oneValue;
                }
            }
            #region  异常处理
            catch (ArgumentException e)
            {
                logger.Error("getOneValueObjectFromString：ArgumentException occured:" + e.Message);
                return null;
            }
            catch (FormatException e)
            {
                logger.Error("getOneValueObjectFromString：FormatException occured:" + e.Message);
                return null;
            }
            catch (OverflowException e)
            {
                logger.Error("getOneValueObjectFromString：OverflowException occured:" + e.Message);
                return null;
            }
            #endregion
        }

        // 从字符串获得Fix32对象
        private ValueType GetFix32ObjectFromFloatString(TwType dataType, string strValue)
        {
            float f;
            try
            {
                f = (float)Convert.ToSingle(strValue);
            }
            catch (FormatException)
            {
                f = (float)0.00;
            }

            TwOneValueFix32 oneValueFix32 = new TwOneValueFix32();

            TwFix32 fix32 = TwFix32.FloatToFix32(f);
            oneValueFix32.Item = fix32;
            oneValueFix32.ItemType = TwType.Fix32;

            return oneValueFix32;
        }

        // 设置不定义图像大小
        private bool SetUndefinedImageSize()
        {
            //bool flag = SetCapability(TwCap.ICAP_UNDEFINEDIMAGESIZE, "1");
            //return flag;
            bool setCapSucc = false;
            TwOneValueIntegerOrBool oneValue = new TwOneValueIntegerOrBool();
            oneValue.ItemType = TwType.Bool;
            oneValue.Item = 1;

            setCapSucc = SetCapabilityOneValue(TwCap.ICAP_UNDEFINEDIMAGESIZE, oneValue);
            return setCapSucc;
        }
        // 设置ICAP_BITDEPTHREDUCTION
        public bool SetBitDepthReduction()
        {
            //bool flag = SetCapability(TwCap.ICAP_BITDEPTHREDUCTION,"0");
            //return flag;
            bool setCapSucc = false;
            //1:查询ICAP_BITDEPTHREDUCTION能力的数据类型
            TwType dataType = GetCapabilitySupportedDataType(TwCap.ICAP_BITDEPTHREDUCTION);

            if (dataType == TwType.Null)
                logger.Error("cannot get the data type of TwCap.ICAP_BITDEPTHREDUCTION");

            //2:设置ICAP_BITDEPTHREDUCTION能力为0,即TWBR_THRESHOLD
            else if (dataType == TwType.Fix32)
            {
                TwFix32 fix32 = new TwFix32();
                fix32.Whole = 0;
                fix32.Frac = 0;

                TwOneValueFix32 oneValueFix32 = new TwOneValueFix32();
                oneValueFix32.Item = fix32;
                oneValueFix32.ItemType = TwType.Fix32;

                setCapSucc = SetCapabilityOneValueFix32(TwCap.ICAP_BITDEPTHREDUCTION, oneValueFix32);
            }
            else
            {
                TwOneValueIntegerOrBool oneValue = new TwOneValueIntegerOrBool();
                oneValue.ItemType = dataType;
                oneValue.Item = 0;

                setCapSucc = SetCapabilityOneValue(TwCap.ICAP_BITDEPTHREDUCTION, oneValue);
            }
            return setCapSucc;
        }
        // 设置使用进纸器
        public bool SetFeederEnabled()
        {
            //bool flag = SetCapability(TwCap.CAP_FEEDERENABLED, "1");
            //return flag;
            bool setCapSucc = false;
            TwType tmp = TwType.Null;
            //1:查询支持送纸器支持能力的数据类型
            TwType dataType = GetCapabilitySupportedDataType(TwCap.CAP_FEEDERENABLED);
            string str = GetCapabilityCurrentValue(TwCap.CAP_FEEDERENABLED, ref tmp);

            if (dataType == TwType.Null)
                logger.Error("cannot get the data type of TwCap.CAP_FEEDERENABLED");

            //2:设置送纸器支持能力为true
            else if (dataType == TwType.Fix32)
            {
                TwFix32 fix32 = new TwFix32();
                fix32.Whole = 1;
                fix32.Frac = 0;

                TwOneValueFix32 oneValueFix32 = new TwOneValueFix32();
                oneValueFix32.Item = fix32;
                oneValueFix32.ItemType = TwType.Fix32;

                setCapSucc = SetCapabilityOneValueFix32(TwCap.CAP_FEEDERENABLED, oneValueFix32);
            }
            else
            {
                TwOneValueIntegerOrBool oneValue = new TwOneValueIntegerOrBool();
                oneValue.ItemType = dataType;
                oneValue.Item = 1;

                setCapSucc = SetCapabilityOneValue(TwCap.CAP_FEEDERENABLED, oneValue);
            }
            return setCapSucc;
        }

        /// <summary>
        /// 设置分辨率的计量单位
        /// </summary>
        /// <param name="unit">分辨率计量单位</param>
        /// <returns>设置成功返回true,否则返回false</returns>
        public bool SetUint(TwUint unit)
        {
            //bool flag = SetCapability(TwCap.ICAP_Units, ((int)unit).ToString());
            //return flag;
            //1:获取计量单位的支持数据类型
            TwType dataType = GetCapabilitySupportedDataType(TwCap.ICAP_Units);
            if (dataType == TwType.Null)
            {
                logger.Error("failed to get the Uint supported data type");
                return false;
            }

            //2：根据数据类型进行设置
            bool resultFlag;
            if (dataType == TwType.Fix32)
            {
                TwFix32 fix32 = new TwFix32();
                fix32.Whole = (short)unit;
                fix32.Frac = 0;

                TwOneValueFix32 oneValuefix32 = new TwOneValueFix32();
                oneValuefix32.ItemType = dataType;
                oneValuefix32.Item = fix32;

                resultFlag = SetCapabilityOneValueFix32(TwCap.ICAP_Units, oneValuefix32);
            }
            else
            {
                TwOneValueIntegerOrBool oneValue = new TwOneValueIntegerOrBool();
                oneValue.ItemType = dataType;
                oneValue.Item = (UInt32)TwUint.TWUN_INCHES;

                resultFlag = SetCapabilityOneValue(TwCap.ICAP_Units, oneValue);
            }
            return resultFlag;
        }

        /// <summary>
        /// 将CapID->oneValueItem的映射配置到扫描仪
        /// </summary>
        /// <param name="map">映射</param>
        /// <returns>配置错误原因记录，配置正确时为null</returns>
        private Hashtable SaveCapsToScanner(Hashtable map)
        {
            if (state != TwState.OpenDS)
            {
                logger.Debug("cannot save config to scanner when state = " + state);
                return null;
            }
            if (map == null)
            {
                logger.Debug("cannot save config to scanner when hash is null");
                return null;
            }
            Hashtable hash = new Hashtable();

            #region  待删除
            //foreach (TwCap capID in capList)
            //{
            //    try
            //    {
            //        //1.1：依次读取用户自定义的配置
            //        if (capID == TwCap.CAP_FEEDERENABLED)
            //        {
            //            supportFeederEnabled = SetFeederEnabled();
            //            FeederEnabledSetted = true;
            //        }
            //        else if (map[capID] != null)
            //        {
            //            string dataValue = map[capID] as string;

            //            if (capID == TwCap.ICAP_PixelType)
            //            {
            //                isBW = (map[capID] as string == "0");// 黑白
            //            }
            //            TwType dataType = (TwType)dataTypeOfCapDataList[capID];

            //            #region 处理空白页判断、去黑边和纠偏
            //            if (capID == TwCap.ICAP_AUTODISCARDBLANKPAGES ||
            //                capID == TwCap.ICAP_AUTOMATICBORDERDETECTION ||
            //                capID == TwCap.ICAP_AUTOMATICDESKEW)
            //            {
            //                if (capID == TwCap.ICAP_AUTOMATICBORDERDETECTION)
            //                {
            //                    SetUndefinedImageSize(); 
            //                }

            //                CapItem capItem = GetCapabilitySupportedDataList(capID);
            //                if (capItem.capDataType == TwOn.DontCare)
            //                {
            //                    if (capID == TwCap.ICAP_AUTODISCARDBLANKPAGES)
            //                    {
            //                        imageProcFlag[capID] = false;
            //                    }
            //                    else if (dataValue == "1")
            //                    {
            //                        imageProcFlag[capID] = true;
            //                    }
            //                    else
            //                    {
            //                        imageProcFlag[capID] = false;
            //                    }
            //                    continue;
            //                }
            //                else
            //                {
            //                    imageProcFlag[capID] = false;

            //                    if (defaultScannerName.ToLower().Contains("av8350"))
            //                    {
            //                        if (capID == TwCap.ICAP_AUTODISCARDBLANKPAGES)
            //                        {
            //                            continue;
            //                        }
            //                    }                                    

            //                    // 去黑边默认设置为1
            //                    if (capID == TwCap.ICAP_AUTOMATICBORDERDETECTION)
            //                    {
            //                        dataValue = "1";
            //                    }
            //                }
            //            }
            //            #endregion
            //            if (capID == TwCap.ICAP_COMPRESSION)
            //            {
            //                TwCompression twCompression = (TwCompression)Enum.Parse(typeof(TwCompression), dataValue);
            //                if ((twCompression == TwCompression.TWCP_GROUP4))//file模式下 黑白像素 tiff格式
            //                {
            //                    isGroup4 = true;
            //                    dataValue = ((short)TwCompression.TWCP_NONE).ToString();
            //                }
            //            }
            //            if (capID == TwCap.ICAP_IMAGEFILEFORMAT)
            //            {
            //                //twSession.isGroup4 = false;
            //                // 传输机制为native，跳过
            //                if (map[TwCap.ICAP_IXferMech] as string == "0")
            //                {
            //                    isGroup4 = (dataValue == "0");
            //                    continue;
            //                }
            //            }
            //            if (capID == TwCap.ICAP_XRESOLUTION)
            //            {
            //                setUint(TwUint.TWUN_INCHES);
            //            }
            //            //设置扫描仪能力
            //            bool resultFlag;
            //            if (dataType == TwType.Fix32)
            //            {
            //                resultFlag = SetCapabilityOneValueFix32(capID, (TwOneValueFix32)getFix32ObjectFromFloatString(dataType, dataValue));
            //            }
            //            else
            //            {
            //                resultFlag = SetCapabilityOneValue(capID, (TwOneValueIntegerOrBool)getOneValueObjectFromString(dataType, dataValue));
            //            }
            //            //分析能力设置结果  不记录删除空白页、边缘检测和纠偏是否配置成功
            //            if (!resultFlag &&
            //                capID != TwCap.ICAP_AUTOMATICDESKEW &&
            //                capID != TwCap.ICAP_AUTOMATICBORDERDETECTION &&
            //                capID != TwCap.ICAP_AUTODISCARDBLANKPAGES)
            //            {
            //                TwType dataTypeTemp = new TwType();
            //                string currentStr = GetCapabilityCurrentValue(capID, ref dataTypeTemp);
            //                hash[capID] = "dataType = " + dataTypeTemp + "  currentValue = " + currentStr;
            //                continue;
            //            }
            //        }
            //    }
            //    catch (ArgumentException e)
            //    {
            //        logger.Error("argument exception when set pointer to structure TwOneValue" + e.Message);
            //    }
            //    catch (Exception e)
            //    {
            //        logger.Error(e.Message);
            //    }
            //}
            #endregion
            return hash;
        }

        /// <summary>
        /// 查询指定的能力所能支持的数据范围
        /// </summary>
        /// <param name="capID">能力ID</param>
        /// <returns>能力支持数据范围的CapItem扫描对象</returns>
        public CapItem GetCapabilitySupportedDataList(TwCap capID)
        {
            // 查询能力
            TwCapability twCap = new TwCapability(capID);
            GetCapability(ref twCap);

            CapItem capItem = new CapItem();
            capItem.capDataType = TwOn.DontCare;
            capItem.listDataType = TwType.Null;
            capItem.list = null;

            try
            {
                #region 根据返回结果对能力进行分析
                if (twCap.contentType != TwOn.DontCare && twCap.contentValuePtr != IntPtr.Zero)
                {
                    IntPtr tempPtr = TwainSession.GlobalLock(twCap.contentValuePtr);

                    switch ((TwOn)twCap.contentType)
                    {
                        case TwOn.Array:
                            {
                                TwArray array = (TwArray)Marshal.PtrToStructure(tempPtr, typeof(TwArray));
                                IntPtr p = new IntPtr(tempPtr.ToInt32() + Marshal.SizeOf(array) - Marshal.SizeOf(array.ItemList));
                                capItem.listDataType = array.ItemType;                                                                                      //对capitem的数据类型进行赋值
                                capItem.capDataType = TwOn.Array;
                                capItem.list = GetDataListFromPointer(array.ItemType, p, array.NumItems);                                      //记录数据列表中的数据类型                                                                                   
                                break;
                            }
                        case TwOn.Enum:
                            {
                                TwEnumeration enu = (TwEnumeration)Marshal.PtrToStructure(tempPtr, typeof(TwEnumeration));
                                IntPtr p = new IntPtr(tempPtr.ToInt32() + Marshal.SizeOf(enu) - Marshal.SizeOf(enu.ItemList));
                                capItem.listDataType = enu.ItemType;                                                                                        //对capitem的数据列表进行赋值
                                capItem.capDataType = TwOn.Enum;
                                capItem.list = GetDataListFromPointer(enu.ItemType, p, enu.NumItems);                                            //记录数据列表中的数据类型                                                                                  
                                break;
                            }
                        case TwOn.One:
                            {
                                TwOneValueIntegerOrBool one = (TwOneValueIntegerOrBool)Marshal.PtrToStructure(tempPtr, typeof(TwOneValueIntegerOrBool));
                                IntPtr p = new IntPtr(tempPtr.ToInt32() + Marshal.SizeOf(one) - Marshal.SizeOf(one.Item));
                                capItem.listDataType = one.ItemType;                                                                                        //对capitem的数据列表进行赋值
                                capItem.capDataType = TwOn.One;
                                capItem.list = GetDataListFromPointer(one.ItemType, p, 1);                                                                  //记录数据列表中的数据类型                                                                                  
                                break;
                            }
                        case TwOn.Range:
                            {
                                TwRangeIntegerOrBool range = (TwRangeIntegerOrBool)Marshal.PtrToStructure(tempPtr, typeof(TwRangeIntegerOrBool));
                                ArrayList list = new ArrayList();

                                //fix32类型
                                if (range.ItemType == TwType.Fix32)
                                {
                                    TwRangeFix32 rangeFix32 = (TwRangeFix32)Marshal.PtrToStructure(tempPtr, typeof(TwRangeFix32));
                                    for (long val = (long)rangeFix32.MinValue.ToFloat(); val <= rangeFix32.MaxValue.ToFloat(); val += (long)rangeFix32.StepSize.ToFloat())
                                    {
                                        list.Add(val);
                                    }

                                }

                                //UINT8 UINT16 UINT32 INT8 INT16 INT32类型
                                else
                                    list = GetRangeDataList(range.ItemType, range.MinValue, range.MaxValue, range.StepSize);                            //对数据列表进行赋值

                                capItem.listDataType = range.ItemType;                                                                                      //对capitem的数据列表进行赋值
                                capItem.capDataType = TwOn.Range;
                                capItem.list = list;
                                break;
                            }
                        default:
                            break;
                    }
                }
                #endregion
                return capItem;
            }
            #region  异常处理
            catch (ArgumentNullException e)
            {
                logger.Error("getCap():ArgumentNullException occured " + e.Message);
                return capItem;
            }
            catch (ArgumentException e)
            {
                logger.Error("getCap():ArgumentException  occured " + e.Message);
                return capItem;
            }
            catch (Exception e)
            {
                logger.Error("getCap():Exception  occured " + e.Message);
                return capItem;
            }
            #endregion
            finally
            {
                if (twCap.contentValuePtr != IntPtr.Zero)
                {
                    TwainSession.GlobalUnlock(twCap.contentValuePtr);
                    TwainSession.GlobalFree(twCap.contentValuePtr);
                }
            }
        }

        /// <summary>
        /// 查询指定的能力是否被支持
        /// </summary>
        /// <param name="capID">能力ID</param>
        /// <returns>如果支持返回true，否则返回false</returns>
        private bool SupportedCapability(TwCap capID)
        {
            TwCapability cap = new TwCapability();
            cap.capID = capID;
            cap.contentType = TwOn.DontCare;
            cap.contentValuePtr = IntPtr.Zero;

            bool bSupport = false;


            if (state >= TwState.OpenDS)
            {
                TwRC rc = DScapGet(appid, srcds, TwDG.Control, TwDAT.Capability, TwMSG.Get, ref  cap);
                if (rc == TwRC.Success)
                {
                    bSupport = true;
                }
                else
                {
                    logger.Debug("supportedCapability : failed " + capID + "condition code = " + GetLastErrorCode());
                    bSupport = false;
                }
            }
            else
            {
                logger.Debug("cannot get" + capID + "during sate = " + state);
                bSupport = false;
            }

            //释放内存
            if (cap.contentValuePtr != IntPtr.Zero)
                TwainSession.GlobalFree(cap.contentValuePtr);

            return bSupport;

        }

        /// <summary>
        /// 查询能力
        /// </summary>
        /// <param name="twcap">能力类型</param>
        /// <returns>如果不支持该能力，返回null，支持则返回能力对象</returns>
        public bool GetCapability(ref TwCapability cap)
        {
            try
            {
                if (state >= TwState.OpenDS)
                {
                    TwRC rc;
                    rc = DScapGet(appid, srcds, TwDG.Control, TwDAT.Capability, TwMSG.Get, ref cap);
                    if (rc == TwRC.Success)
                        return true;
                    else
                    {
                        logger.Debug("GetCapability(): failed to get cap " + cap.capID.ToString() + ",condition Code =" + GetLastErrorCode());
                        return false;
                    }
                }
                return false;
            }
            catch (Exception e)
            {
                logger.Error("GetCapability(): failed to get cap :exception occured " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// 查询指定的能力的当前数据值
        /// </summary>
        /// <param name="capID">能力ID</param>
        /// <param name="dataType">表示当前能力的数据类型</param>
        /// <returns>当前的字符串表示,查询失败返回null</returns>
        public string GetCapabilityCurrentValue(TwCap capID, ref TwType dataType)
        {
            dataType = TwType.Null;

            if (state > TwState.OpenDSM)
            {
                TwCapability cap = new TwCapability();
                cap.capID = capID;
                cap.contentType = TwOn.DontCare;
                cap.contentValuePtr = IntPtr.Zero;

                //1:获取当前值
                TwRC rc = DScapGet(appid, srcds, TwDG.Control, TwDAT.Capability, TwMSG.GetCurrent, ref cap);
                if (rc != TwRC.Success)
                {
                    logger.Debug("cannot get the current value " + capID + ",condition code = " + GetLastErrorCode());
                    return null;
                }

                if (cap.contentType != TwOn.One)
                {
                    logger.Error("cannot get the current value " + capID + ",get the wrong cap.contentType" + cap.contentType);
                    return null;
                }

                //2：分析当前数据值
                try
                {
                    //2.1内存加锁
                    IntPtr tmpPtr = TwainSession.GlobalLock(cap.contentValuePtr);

                    //2.2生成单值对象
                    TwOneValueIntegerOrBool one = (TwOneValueIntegerOrBool)Marshal.PtrToStructure(tmpPtr, typeof(TwOneValueIntegerOrBool));
                    IntPtr p = new IntPtr(tmpPtr.ToInt32() + Marshal.SizeOf(one) - Marshal.SizeOf(one.Item));

                    //2.3分析数据
                    ArrayList list = GetDataListFromPointer(one.ItemType, p, 1);

                    //2.4 查询分析结果
                    if (list.Count == 1)
                    {
                        dataType = one.ItemType;
                        return list[0].ToString();
                    }
                    else
                    {
                        logger.Debug("getCapabilityCurrentValue:list size is not 1");
                        dataType = TwType.Null;
                        return null;
                    }
                }
                #region 异常处理
                catch (ArgumentNullException e)
                {
                    logger.Error("getCapabilityCurrentValue:ArgumentNullException occured " + e.Message);
                    dataType = TwType.Null;
                    return null;
                }
                catch (ArgumentException e)
                {
                    logger.Error("getCapabilityCurrentValue():ArgumentException  occured " + e.Message);
                    dataType = TwType.Null;
                    return null;
                }
                catch (Exception e)
                {
                    logger.Error("getCapabilityCurrentValue():Exception  occured " + e.Message);
                    dataType = TwType.Null;
                    return null;
                }
                #endregion
                finally
                {
                    //解锁，释放内存
                    if (cap.contentValuePtr != IntPtr.Zero)
                    {
                        TwainSession.GlobalUnlock(cap.contentValuePtr);
                        TwainSession.GlobalFree(cap.contentValuePtr);
                    }
                }
            }
            else
            {
                logger.Error("cannot get the current value " + capID + ",State = " + state);
                return null;
            }
        }

        /// <summary>
        /// 查询指定的能力的默认数据值
        /// </summary>
        /// <param name="capID">能力ID</param>
        /// <param name="dataType">表示当前能力的数据类型</param>
        /// <returns>当前的字符串表示,查询失败返回null</returns>
        public string GetCapabilityDefaultValue(TwCap capID, ref TwType dataType)
        {
            dataType = TwType.Null;

            if (state > TwState.OpenDSM)
            {
                TwCapability cap = new TwCapability();
                cap.capID = capID;
                cap.contentType = TwOn.DontCare;
                cap.contentValuePtr = IntPtr.Zero;

                //1:获取当前值
                TwRC rc = DScapGet(appid, srcds, TwDG.Control, TwDAT.Capability, TwMSG.GetDefault, ref cap);
                if (rc != TwRC.Success)
                {
                    logger.Error("cannot get the current value " + capID + ",condition code = " + GetLastErrorCode());
                    return null;
                }

                if (cap.contentType != TwOn.One)
                {
                    logger.Error("cannot get the current value " + capID + ",get the wrong cap.contentType" + cap.contentType);
                    return null;
                }

                //2：分析当前数据值
                try
                {
                    //2.1内存加锁
                    IntPtr tmpPtr = TwainSession.GlobalLock(cap.contentValuePtr);

                    //2.2生成单值对象
                    TwOneValueIntegerOrBool one = (TwOneValueIntegerOrBool)Marshal.PtrToStructure(tmpPtr, typeof(TwOneValueIntegerOrBool));
                    IntPtr p = new IntPtr(tmpPtr.ToInt32() + Marshal.SizeOf(one) - Marshal.SizeOf(one.Item));

                    //2.3分析数据
                    ArrayList list = GetDataListFromPointer(one.ItemType, p, 1);

                    //2.4 查询分析结果
                    if (list.Count == 1)
                    {
                        dataType = one.ItemType;
                        return list[0].ToString();
                    }
                    else
                    {
                        logger.Debug("getCapabilityCurrentValue:list size is not 1");
                        dataType = TwType.Null;
                        return null;
                    }
                }
                #region 异常处理
                catch (ArgumentNullException e)
                {
                    logger.Error("getCapabilityCurrentValue:ArgumentNullException occured " + e.Message);
                    dataType = TwType.Null;
                    return null;
                }
                catch (ArgumentException e)
                {
                    logger.Error("getCapabilityCurrentValue():ArgumentException  occured " + e.Message);
                    dataType = TwType.Null;
                    return null;
                }
                catch (Exception e)
                {
                    logger.Error("getCapabilityCurrentValue():Exception  occured " + e.Message);
                    dataType = TwType.Null;
                    return null;
                }
                #endregion
                finally
                {
                    //解锁，释放内存
                    if (cap.contentValuePtr != IntPtr.Zero)
                    {
                        TwainSession.GlobalUnlock(cap.contentValuePtr);
                        TwainSession.GlobalFree(cap.contentValuePtr);
                    }
                }
            }
            else
            {
                logger.Error("cannot get the current value " + capID + ",State = " + state);
                return null;
            }
        }


        /// <summary>
        /// 查询指定的能力所能接受的数据类型
        /// </summary>
        /// <param name="capId">能力ID</param>
        /// <returns>支持的数据能力，如果不支持该能力则返回TwType.NULL</returns>
        private TwType GetCapabilitySupportedDataType(TwCap capID)
        {
            //1：查询能力
            TwCapability twCap = new TwCapability(capID);
            GetCapability(ref twCap);


            if (twCap.contentType == TwOn.DontCare || twCap.contentValuePtr == IntPtr.Zero)
            {
                logger.Debug("cannot get the capability");
                return TwType.Null;
            }
            else
            {
                #region 根据返回结果对能力进行分析

                TwType dataType = TwType.Null;
                try
                {
                    // 1：内存加锁
                    IntPtr tmpPtr = TwainSession.GlobalLock(twCap.contentValuePtr);

                    //2：分析对象的数据类型
                    switch ((TwOn)twCap.contentType)
                    {
                        case TwOn.Array:
                            {
                                TwArray array = (TwArray)Marshal.PtrToStructure(tmpPtr, typeof(TwArray));
                                dataType = array.ItemType;
                                break;
                            }
                        case TwOn.Enum:
                            {
                                TwEnumeration enu = (TwEnumeration)Marshal.PtrToStructure(tmpPtr, typeof(TwEnumeration));
                                dataType = enu.ItemType;
                                break;
                            }
                        case TwOn.One:
                            {
                                TwOneValueIntegerOrBool one = (TwOneValueIntegerOrBool)Marshal.PtrToStructure(tmpPtr, typeof(TwOneValueIntegerOrBool));
                                dataType = one.ItemType;
                                break;
                            }
                        case TwOn.Range:
                            {
                                TwRangeIntegerOrBool range = (TwRangeIntegerOrBool)Marshal.PtrToStructure(tmpPtr, typeof(TwRangeIntegerOrBool));
                                dataType = range.ItemType;
                                break;
                            }
                        default:
                            {
                                break;
                            }
                    }
                    return dataType;
                }
                #endregion

                #region  异常处理
                catch (ArgumentNullException e)
                {
                    logger.Error("GetCapabilitySupportedDataType():ArgumentNullException occured " + e.Message);
                    return TwType.Null;
                }
                catch (ArgumentException e)
                {
                    logger.Error("GetCapabilitySupportedDataType():ArgumentException  occured " + e.Message);
                    return TwType.Null;
                }
                catch (Exception e)
                {
                    logger.Error("GetCapabilitySupportedDataType():Exception  occured " + e.Message);
                    return TwType.Null;
                }
                #endregion

                //释放内存
                finally
                {
                    if (twCap.contentValuePtr != IntPtr.Zero)
                    {
                        TwainSession.GlobalUnlock(twCap.contentValuePtr);
                        TwainSession.GlobalFree(twCap.contentValuePtr);
                    }
                }
            }
        }

        /// <summary>
        /// 根据指定的内存生成range的数据列表
        /// </summary>
        /// <param name="itemType"></param>
        /// <param name="ptr"></param>
        /// <returns></returns>
        public ArrayList GetRangeDataList(TwType itemType, UInt32 minValue, UInt32 maxValue, UInt32 stepSize)
        {
            ArrayList list = new ArrayList();
            Int64 min = Int64.MinValue;
            Int64 max = Int64.MaxValue;
            Int64 step = 0;

            //根据不同的数据类型计算出最大值，最小值和步长
            switch (itemType)
            {
                case TwType.Int8:
                    min = ((SByte)(minValue));
                    max = ((SByte)(maxValue));
                    step = ((SByte)(stepSize));
                    break;
                case TwType.UInt8:
                    min = (Byte)(minValue);
                    max = (Byte)(maxValue);
                    step = (Byte)(stepSize);
                    break;
                case TwType.Int16:
                    min = (Int16)(minValue);
                    max = (Int16)(maxValue);
                    step = (Int16)(stepSize);
                    break;
                case TwType.UInt16:
                    min = (UInt16)(minValue);
                    max = (UInt16)(maxValue);
                    step = (UInt16)(stepSize);
                    break;
                case TwType.Int32:
                    min = (Int32)(minValue);
                    max = (Int32)(maxValue);
                    step = (Int32)(stepSize); ;
                    break;
                case TwType.UInt32:
                    min = (UInt32)(minValue);
                    max = (UInt32)(maxValue);
                    step = (UInt32)(stepSize);
                    break;
                default:
                    break;
            }

            //根据获取的最大值，最小值和步长进行数据列表的计算
            if (min != Int64.MinValue && max != Int64.MaxValue && step > 0)
            {
                for (Int64 val = min; val <= max; val += step)
                {
                    list.Add(val);
                }
                return list;
            }
            else
            {
                logger.Error("getRangeDataList () failed :min = " + min + "max = " + max + "step = " + step);
                return list;
            }


        }

        /// <summary>
        /// 按照指定的数据类型从内存中读取
        /// </summary>
        /// <param name="itemType">数据类型</param>
        /// <param name="p">内存起始地址</param>
        /// <param name="numItems">内存中包含的数据个数</param>
        /// <returns>数据类型的列表</returns>
        public ArrayList GetDataListFromPointer(TwType itemType, IntPtr ptr, uint numItems)
        {
            int size = -1;                   //数据类型所占的字节数
            IntPtr p = ptr;
            ArrayList list = new ArrayList();//数据元素列表


            if (ptr == IntPtr.Zero || numItems <= 0)
            {
                logger.Error("getDataFromPointer() : paramter is wrong  ");
                return list;
            }

            try
            {
                //2：依次解析字节，将其添加到list中
                for (int i = 0; i < numItems; i++)
                {
                    if (itemType == TwType.Bool)
                    {
                        UInt16 val = (UInt16)Marshal.PtrToStructure(p, typeof(UInt16));
                        size = Marshal.SizeOf(val);
                        list.Add(val);
                    }
                    else if (itemType == TwType.Fix32)
                    {
                        TwFix32 val = (TwFix32)Marshal.PtrToStructure(p, typeof(TwFix32));
                        size = Marshal.SizeOf(val);
                        list.Add(val);
                    }
                    else if (itemType == TwType.Int8)
                    {
                        SByte val = (SByte)Marshal.PtrToStructure(p, typeof(SByte));
                        size = Marshal.SizeOf(val);
                        list.Add(val);
                    }
                    else if (itemType == TwType.Int16)
                    {
                        Int16 val = (Int16)Marshal.PtrToStructure(p, typeof(Int16));
                        size = Marshal.SizeOf(val);
                        list.Add(val);
                    }
                    else if (itemType == TwType.Int32)
                    {
                        Int32 val = (Int32)Marshal.PtrToStructure(p, typeof(Int32));
                        size = Marshal.SizeOf(val);
                        list.Add(val);
                    }
                    else if (itemType == TwType.UInt8)
                    {
                        Byte val = (Byte)Marshal.PtrToStructure(p, typeof(Byte));
                        size = Marshal.SizeOf(val);
                        list.Add(val);
                    }
                    else if (itemType == TwType.UInt16)
                    {
                        UInt16 val = (UInt16)Marshal.PtrToStructure(p, typeof(UInt16));
                        size = Marshal.SizeOf(val);
                        list.Add(val);
                    }
                    else if (itemType == TwType.UInt32)
                    {
                        UInt32 val = (UInt32)Marshal.PtrToStructure(p, typeof(UInt32));
                        size = Marshal.SizeOf(val);
                        list.Add(val);
                    }
                    else if (itemType == TwType.Str32)
                    {
                        TwStr32 twStr32 = (TwStr32)Marshal.PtrToStructure(p, typeof(TwStr32));
                        size = Marshal.SizeOf(twStr32);
                        list.Add(twStr32.str);
                    }
                    else if (itemType == TwType.Str64)
                    {
                        TwStr64 twStr64 = (TwStr64)Marshal.PtrToStructure(p, typeof(TwStr64));
                        size = Marshal.SizeOf(twStr64);
                        list.Add(twStr64.str);
                    }
                    else if (itemType == TwType.Str128)
                    {
                        TwStr128 twStr128 = (TwStr128)Marshal.PtrToStructure(p, typeof(TwStr128));
                        size = Marshal.SizeOf(twStr128);
                        list.Add(twStr128.str);
                    }
                    else if (itemType == TwType.Str255)
                    {
                        TwStr256 twStr256 = (TwStr256)Marshal.PtrToStructure(p, typeof(TwStr256));
                        size = Marshal.SizeOf(twStr256);
                        list.Add(twStr256.str);
                    }

                    //如果数据类型不存在，跳出循环
                    if (size == -1)
                        break;
                    //如果数据类型存在，则指针指向下一个数据内存
                    else
                    {
                        p = new IntPtr(p.ToInt32() + size);
                    }

                }
                //返回数据结果
                return list;
            }
            #region  异常处理
            catch (ArgumentNullException e)
            {
                logger.Error("getDataListFromPointer();ArgumentNULLException occured " + e.Message);
                list.Clear();
                return list;
            }
            catch (ArgumentException e)
            {
                logger.Error("getDataListFromPointer():ArgumentException occured " + e.Message);
                list.Clear();
                return list;
            }
            catch (AccessViolationException e)
            {
                logger.Error("getDataListFromPointer():AccessViolationException occured " + e.Message);
                list.Clear();
                return list;
            }
            catch (Exception e)
            {
                logger.Error("getDataListFromPointer():Exception occured " + e.Message);
                list.Clear();
                return list;
            }
            #endregion
        }

        /// <summary>
        /// 设置单值的能力
        /// </summary>
        /// <param name="cap">能力ID</param>
        /// <param name="oneValueFix32">单值数据类对象</param>
        /// <returns>设置是否成功</returns>
        private bool SetCapabilityOneValueFix32(TwCap capID, TwOneValueFix32 oneValueFix32)
        {

            IntPtr pCapability = IntPtr.Zero;
            IntPtr twCapOneValuePtr = IntPtr.Zero;

            try
            {

                //1：生成单值能力结构类型
                TwCapability twCapOneValue = new TwCapability();
                twCapOneValue.capID = capID;
                twCapOneValue.contentType = TwOn.One;

                //2：对能力结构进行赋值
                int twCapOneValueSize = Marshal.SizeOf(oneValueFix32);
                twCapOneValuePtr = Marshal.AllocCoTaskMem(twCapOneValueSize);
                Marshal.StructureToPtr(oneValueFix32, twCapOneValuePtr, true);
                twCapOneValue.contentValuePtr = twCapOneValuePtr;

                //生成指向能力结构的指针
                int capSize = Marshal.SizeOf(twCapOneValue);
                pCapability = Marshal.AllocCoTaskMem(capSize);
                Marshal.StructureToPtr(twCapOneValue, pCapability, true);

                //进行能力设置
                TwRC rc = DScapSet(appid, srcds, TwDG.Control, TwDAT.Capability, TwMSG.Set, pCapability);
                //分析能力设置结果
                if (rc != TwRC.Success)
                {
                    logger.Debug("set cap " + capID + " failed : condition " + GetLastErrorCode());
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (ArgumentException e)
            {
                logger.Error("argument exception when set pointer to structure TwOneValue" + e.Message);
                return false;
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                return false;
            }
            finally
            {
                if (twCapOneValuePtr != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(twCapOneValuePtr);
                if (pCapability != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pCapability);
            }
        }

        /// <summary>
        /// 设置单值的能力
        /// </summary>
        /// <param name="cap">能力ID</param>
        /// <param name="oneValue">单值数据类对象</param>
        /// <returns>设置是否成功</returns>
        private bool SetCapabilityOneValue(TwCap capID, TwOneValueIntegerOrBool oneValue)
        {

            IntPtr pCapability = IntPtr.Zero;
            IntPtr twCapOneValuePtr = IntPtr.Zero;

            try
            {

                //1：生成单值能力结构类型
                TwCapability twCapOneValue = new TwCapability();
                twCapOneValue.capID = capID;
                twCapOneValue.contentType = TwOn.One;

                //2：对能力结构进行赋值
                int twCapOneValueSize = Marshal.SizeOf(oneValue);
                twCapOneValuePtr = Marshal.AllocCoTaskMem(twCapOneValueSize);
                Marshal.StructureToPtr(oneValue, twCapOneValuePtr, true);
                twCapOneValue.contentValuePtr = twCapOneValuePtr;

                //生成指向能力结构的指针
                int capSize = Marshal.SizeOf(twCapOneValue);
                pCapability = Marshal.AllocCoTaskMem(capSize);
                Marshal.StructureToPtr(twCapOneValue, pCapability, true);

                //进行能力设置
                TwRC rc = DScapSet(appid, srcds, TwDG.Control, TwDAT.Capability, TwMSG.Set, pCapability);
                //分析能力设置结果
                if (rc != TwRC.Success)
                {
                    logger.Debug("set cap " + capID + " failed : condition " + GetLastErrorCode());
                    return false;
                }
                else
                {
                    return true;
                }
            }
            catch (ArgumentException e)
            {
                logger.Error("argument exception when set pointer to structure TwOneValue" + e.Message);
                return false;
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                return false;
            }
            finally
            {
                if (twCapOneValuePtr != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(twCapOneValuePtr);
                if (pCapability != IntPtr.Zero)
                    Marshal.FreeCoTaskMem(pCapability);
            }

        }

        // 设置能力到扫描仪
        public bool SetCapability(CapInfo capInfo)
        {
            bool resultFlag = false;
            try
            {
                if (capInfo.CapId == TwCap.ECAP_PAPERSOURCE)
                    capInfo.CapId = TwCap.CAP_PAPERSOURCE;
                string dataValue = capInfo.CurrentIntStr;
                TwType dataType = capInfo.CapType;
                if (dataValue == null || dataType == TwType.Null)
                {
                    return true;
                }
                if (dataType == TwType.Fix32)
                {
                    if (capInfo.CapId == TwCap.ICAP_AUTOMATICBORDERDETECTION)
                    {
                        SetUndefinedImageSize();
                    }
                    resultFlag = SetCapabilityOneValueFix32(capInfo.CapId, (TwOneValueFix32)GetFix32ObjectFromFloatString(dataType, dataValue));
                    if (capInfo.CapId == TwCap.ICAP_XRESOLUTION)
                    {
                        resultFlag = SetCapabilityOneValueFix32(TwCap.ICAP_YRESOLUTION, (TwOneValueFix32)GetFix32ObjectFromFloatString(dataType, dataValue));
                    }
                }
                else
                {
                    if (capInfo.CapId == TwCap.ICAP_AUTOMATICBORDERDETECTION)
                    {
                        SetUndefinedImageSize();
                    }
                    resultFlag = SetCapabilityOneValue(capInfo.CapId, (TwOneValueIntegerOrBool)GetOneValueObjectFromString(dataType, dataValue));
                    if (capInfo.CapId == TwCap.ICAP_XRESOLUTION)
                    {
                        resultFlag = SetCapabilityOneValueFix32(TwCap.ICAP_YRESOLUTION, (TwOneValueFix32)GetFix32ObjectFromFloatString(dataType, dataValue));
                    }
                }
                logger.Debug("设置能力:" + capInfo.CapId + ", 值类型:" + dataType.ToString() + ",值:" + dataValue + ",结果:" + resultFlag.ToString());
            }
            catch (Exception e)
            {
                logger.Debug("failed to set cap " + capInfo.CapId + ":" + e.Message);
            }
            finally
            {
                if (capInfo.CapId == TwCap.CAP_PAPERSOURCE)
                    capInfo.CapId = TwCap.ECAP_PAPERSOURCE;
            }
            return resultFlag;
        }

        /// <summary>
        /// 查询错误代码
        /// </summary>
        /// <returns>最后一次调用DSM_ENTRY函数的错误码</returns>
        private TwCC GetLastErrorCode()
        {
            TwStatus status = new TwStatus();
            TwCC twCC;
            TwRC rc = DSstatus(appid, srcds, TwDG.Control, TwDAT.Status, TwMSG.Get, status);
            if (rc == TwRC.Failure)
                twCC = TwCC.FailedToGetLastError;
            else
                twCC = (TwCC)status.ConditionCode;
            return twCC;
        }

        // 刷新能力值
        private bool UpdateScannerCaps(int beginPos)
        {
            for (int i = beginPos; i < capList.Length; i++)
            {
                TwCap capId = capList[i];
                CapInfo capInfo = scannerCaps[capId] as CapInfo;
                capInfo.GetCap();
            }
            return true;
        }

        // 从扫描仪读取能力
        public bool ReadScannerCap(TwCap capId)
        {
            if (scannerCaps == null)
            {
                scannerCaps = new Hashtable();
            }
            CapInfo capInfo = GetScannerCap(capId);
            capInfo.GetCap();
            scannerCaps[capId] = capInfo;

            return true;
        }

        // 获得扫描仪能力映射表
        public CapInfo GetScannerCap(TwCap capId)
        {
            if (scannerCaps == null)
            {
                scannerCaps = new Hashtable();
            }
            if ((scannerCaps[capId] as CapInfo) == null)
            {
                CapInfo capInfo = new CapInfo(capId, this);
                scannerCaps[capId] = capInfo;
            }
            return scannerCaps[capId] as CapInfo;
        }

        // 设置能力的当前值
        public bool SetCurrentValueOfCap(TwCap capId, TwOneValue oneValue)
        {
            CapInfo capInfo = GetScannerCap(capId);

            if (oneValue == null)
            {
                capInfo.CurrentIntStr = null;
            }
            else
            {
                capInfo.CurrentIntStr = oneValue.ItemStr;
                capInfo.CapType = oneValue.ItemType;
            }
            return true;
        }

        // 保存设置到配置文件
        public void SaveSettings(Hashtable capEnableList)
        {
            foreach (TwCap capId in capList)
            {
                object obj = capEnableList[capId];
                if (obj.ToString().Equals(Boolean.TrueString))
                {
                    CapInfo capInfo = GetScannerCap(capId);
                    TwOneValue oneValue = new TwOneValue(capInfo.CapType, capInfo.CurrentIntStr);
                    userConfig.SetSettings(capId, oneValue);
                }
                else
                    userConfig.SetSettings(capId, null);
            }

            userConfig.SaveSettings();
        }

        /// <summary>
        /// 根据数据能力ID，将数字型字符串转换为twain枚举型的字符串，比如字符串0 在cap_fileformat格式下表示TWSX_NATIVE字符串
        /// </summary>
        /// <param name="capID"></param>
        /// <returns></returns>
        public string ConvertIntStringToEnumString(TwCap capID, string intStrValue)
        {
            string enumStrValue = intStrValue;
            int iVal;

            try
            {
                if (!string.IsNullOrEmpty(intStrValue))
                {

                    switch (capID)
                    {
                        case TwCap.ICAP_IMAGEFILEFORMAT:
                            iVal = int.Parse(intStrValue);
                            enumStrValue = ((TwFileFormat)iVal).ToString();
                            break;
                        case TwCap.ICAP_COMPRESSION:
                            iVal = int.Parse(intStrValue);
                            enumStrValue = ((TwCompression)iVal).ToString();
                            break;
                        case TwCap.ICAP_XferMech:
                            if (intStrValue == "2")  // memory模式暂不支持
                            {
                                enumStrValue = null;
                            }
                            else
                            {
                                iVal = int.Parse(intStrValue);
                                enumStrValue = ((TwMode)iVal).ToString();
                            }
                            break;
                        case TwCap.ICAP_PixelType:
                            iVal = int.Parse(intStrValue);
                            enumStrValue = ((TwPixelType)iVal).ToString();
                            break;
                        case TwCap.ECAP_PAPERSOURCE:
                            if (intStrValue.Equals("1"))
                                enumStrValue = PaperSouceString.ADF;
                            else if (intStrValue.Equals("2"))
                                enumStrValue = PaperSouceString.Platen;
                            break;
                        default:
                            break;
                    }
                }
                return enumStrValue;
            }
            #region 异常处理
            catch (ArgumentException e)
            {
                logger.Error("ConvertIntStringToEnumString ArgumentException occured:capId =" + capID + "inStrValue=" + intStrValue + "exception =" + e.Message);
                return enumStrValue;
            }
            catch (FormatException e)
            {
                logger.Error("ConvertIntStringToEnumString FormatException occured:capId =" + capID + "inStrValue=" + intStrValue + "exception =" + e.Message);
                return enumStrValue;
            }
            catch (OverflowException e)
            {
                logger.Error("ConvertIntStringToEnumString OverflowExceptionoccured:capId =" + capID + "inStrValue=" + intStrValue + "exception =" + e.Message);
                return enumStrValue;
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                return enumStrValue;
            }
            #endregion
        }

        /// <summary>
        /// 根据数据能力ID，将twain枚举型的字符串转换为数字型字符串，比在cap_fileformat格式下TWSX_NATIVE字符串对应的值为0
        /// </summary>
        /// <param name="capID"></param>
        /// <param name="enumStrValue"></param>
        /// <returns></returns>
        public string ConvertEnumStringToIntString(TwCap capID, string enumStrValue)
        {
            string intStrValue = enumStrValue;
            try
            {
                if (!string.IsNullOrEmpty(enumStrValue))
                {
                    switch (capID)
                    {
                        case TwCap.ICAP_IMAGEFILEFORMAT:
                            TwFileFormat fileFormat = (TwFileFormat)Enum.Parse(typeof(TwFileFormat), enumStrValue);
                            intStrValue = ((int)fileFormat).ToString();
                            break;
                        case TwCap.ICAP_COMPRESSION:
                            TwCompression compression = (TwCompression)Enum.Parse(typeof(TwCompression), enumStrValue);
                            intStrValue = ((int)compression).ToString();
                            break;
                        case TwCap.ICAP_XferMech:
                            TwMode mode = (TwMode)Enum.Parse(typeof(TwMode), enumStrValue);
                            intStrValue = ((int)mode).ToString();
                            break;
                        case TwCap.ICAP_PixelType:
                            TwPixelType pixelType = (TwPixelType)Enum.Parse(typeof(TwPixelType), enumStrValue);
                            intStrValue = ((int)pixelType).ToString();
                            break;
                        case TwCap.ECAP_PAPERSOURCE:
                            if (enumStrValue.Equals(PaperSouceString.ADF))
                                intStrValue = "1";
                            else if (enumStrValue.Equals(PaperSouceString.Platen))
                                intStrValue = "2";
                            break;
                        default:
                            break;
                    }
                }
                return intStrValue;
            }
            #region  异常处理
            catch (ArgumentException e)
            {
                logger.Error("ConvertIntStringToEnumString ArgumentException occured:capId =" + capID + "inStrValue=" + intStrValue + "exception =" + e.Message);
                return intStrValue;
            }
            catch (FormatException e)
            {
                logger.Error("ConvertIntStringToEnumString FormatException occured:capId =" + capID + "inStrValue=" + intStrValue + "exception =" + e.Message);
                return intStrValue;
            }
            catch (OverflowException e)
            {
                logger.Error("ConvertIntStringToEnumString OverflowExceptionoccured:capId =" + capID + "inStrValue=" + intStrValue + "exception =" + e.Message);
                return intStrValue;
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                return intStrValue;
            }
            #endregion
        }

        // 获得某个能力的当前值
        public string GetCurrentValueOfCap(TwCap capId)
        {
            CapInfo capInfo = GetScannerCap(capId);
            return capInfo.CurrentIntStr;
        }

        public bool IsSupported(TwCap capId)
        {
            CapInfo capInfo = GetScannerCap(capId);
            return capInfo.CapType != TwType.Null;
        }
    }
}



