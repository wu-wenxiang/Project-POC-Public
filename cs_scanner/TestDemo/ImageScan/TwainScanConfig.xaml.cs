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
using System.Collections;
using ImageScan.TwainLib;
using UFileClient.Common;
using UFileClient.Twain;
using log4net;
using ImageScan;
using System.IO;

namespace UFileClient.Twain
{

    /// <summary>
    /// ScanConfig.xaml 的交互逻辑
    /// </summary>
    public partial class ScanConfig : Window
    {
        public ScanConfig()
        {
            InitializeComponent();
        }
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private TwainSession twSession;                 // 扫描管理对象        
        private Hashtable capToControl;                 // 能力ID与控件映射关系
        private Control[] controlList;                  // 界面组件列表                 
        private TwCap[] capList;                        // 配置能力ID列表   
        private bool initFinishedFlag = false;          // 控件初始化完成标志

        static ScanConfig()
        {
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.GetConfigDir() + ConstCommon.logConfigFileName));
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="twainSession">扫描管理对象</param>
        /// <param name="capList">能力列表</param>
        /// <returns></returns>
        public bool Init(TwainSession twainSession, TwCap[] capList)
        {
            twSession = twainSession;
            this.capList = capList;
            // 连接数据源
            if (!twainSession.OpenDS())
                return false;

            capToControl = new Hashtable();
            controlList = new Control[]{
                    ICAP_IXferMech,
                    CAP_DUPLEXENABLED,
                    ICAP_PixelType,

                    ICAP_XRESOLUTION,
                    ICAP_PIXELFLAVOR,
                    ICAP_THRESHOLD,
                    ICAP_IMAGEFILEFORMAT,
                    ICAP_COMPRESSION,                    

                    ICAP_BRIGHTNESS,
                    ICAP_CONTRAST,
                    ICAP_AUTOMATICBORDERDETECTION,
                    ICAP_AUTOMATICDESKEW,
                    ICAP_AUTODISCARDBLANKPAGES,

                    ECAP_PAPERSOURCE
                    };
            // 设置控件和能力的对应关系
            for (int i = 0; i < controlList.Length; i++)
            {
                capToControl.Add(capList[i], controlList[i]);
            }

            // 获取扫描仪能力并初始化
            InitConfigUi();
            return true;
        }

        /// <summary>
        /// 初始化配置界面
        /// </summary>
        /// <param name="beginPos"></param>
        private void InitConfigUi()
        {
            UpdateConfigUi(0, capList.Length);
        }

        /// <summary>
        /// 更新配置界面,不更新空白页阈值
        /// </summary>
        /// <param name="beginPos"></param>
        private void UpdateConfigUi(int beginPos, int endPos)
        {
            initFinishedFlag = false;
            //1:设置通用能力
            for (int i = beginPos; i < endPos; i++)
            {
                //  设置当前值
                TwCap capId = capList[i];
                twSession.ReadScannerCap(capId);
                CapInfo capInfo = twSession.GetScannerCap(capId);
                Control control = capToControl[capId] as Control;
                // 根据能力值设置控件能力范围
                InitControl(control, capInfo);
                // 设置控件当前值
                SetCurrentValue(control, capInfo);
                if (capId == TwCap.ECAP_PAPERSOURCE)
                    ECAP_PAPERSOURCE.IsEnabled = capInfo.hasPlaten;
            }

            initFinishedFlag = true;
        }


        /// <summary>
        /// 初始化控件
        /// </summary>
        /// <param name="control"></param>
        /// <param name="capInfo"></param>
        private void InitControl(Control control, CapInfo capInfo)
        {
            List<string> capValueList = capInfo.GetDataList();

            if (capValueList == null &&
                (capInfo.CapId != TwCap.ICAP_AUTODISCARDBLANKPAGES &&
                 capInfo.CapId != TwCap.ICAP_AUTOMATICBORDERDETECTION &&
                 capInfo.CapId != TwCap.ICAP_AUTOMATICDESKEW))
            {
                control.IsEnabled = false;
                return;
            }
            else
            {
                control.IsEnabled = true;
            }
            // 初始化下拉列表
            if (control is ComboBox)
            {
                ComboBox comboBox = control as ComboBox;
                comboBox.Items.Clear();

                {
                    foreach (string capValue in capValueList)
                    {
                        AddComboBoxItem(capValue, comboBox, capInfo.CapId);
                    }
                }
            }
        }

        /// <summary>
        /// 设置能力的当前值
        /// </summary>
        /// <param name="control"></param>
        /// <param name="capInfo"></param>
        private void SetCurrentValue(Control control, CapInfo capInfo)
        {
            if (!control.IsEnabled)
            {
                return;
            }

            bool twOk;
            string enumStr = null;
            if (control is ComboBox)
            {
                ComboBox comboBox = control as ComboBox;
                //对于压缩算法和文件格式
                if (capInfo.CapId == TwCap.ICAP_COMPRESSION || capInfo.CapId == TwCap.ICAP_IMAGEFILEFORMAT)
                {
                    CapInfo xferCapInfo = twSession.GetScannerCap(TwCap.ICAP_XferMech);
                    if (xferCapInfo.CurrentIntStr == "0") // native模式
                    {
                        // 只设置到控件
                        bool OK = SetCurrentValueOfComboBox(comboBox, capInfo);
                        if (!OK)
                        {
                            comboBox.SelectedIndex = 0;
                            enumStr = (comboBox.SelectedItem as ComboBoxItem).Content.ToString();
                            capInfo.CurrentIntStr = twSession.ConvertEnumStringToIntString(capInfo.CapId, enumStr);
                        }
                        return;
                    }
                }

                // 设置用户保存的设置
                twOk = SetOneValue(comboBox, capInfo);
                if (twOk)
                {
                    return;
                }
                // 获取默认值
                capInfo.CurrentIntStr = capInfo.DefaultIntStr;
                // 设置默认值
                twOk = SetOneValue(comboBox, capInfo);
                if (twOk)
                {
                    return;
                }
                // 设置默认值失败,选择一个值进行设置
                comboBox.SelectedIndex = comboBox.Items.Count / 2;
                string enumString = (comboBox.SelectedItem as ComboBoxItem).Content.ToString();
                string intString = twSession.ConvertEnumStringToIntString(capInfo.CapId, enumString);
                capInfo.CurrentIntStr = intString;
                twOk = SetOneValue(comboBox, capInfo);
            }
            else if (control is CheckBox)
            {
                CheckBox checkBox = control as CheckBox;
                SetOneValue(checkBox, capInfo);
            }
        }

        /// <summary>
        /// 设置一个值到控件和扫描仪
        /// </summary>
        /// <param name="control"></param>
        /// <param name="capInfo"></param>
        /// <returns></returns>
        private bool SetOneValue(Control control, CapInfo capInfo)
        {
            if (control is ComboBox)
            {
                ComboBox comboBox = control as ComboBox;
                bool OK = SetCurrentValueOfComboBox(comboBox, capInfo);

                if (OK) // 列表中有该值
                {
                    // 将该值设置到扫描仪
                    bool twOk = twSession.SetCapability(capInfo);
                    if (twOk)
                    {
                        return true;
                    }
                }
                return false;
            }
            else
            {
                CheckBox checkBox = control as CheckBox;
                if (capInfo.CurrentIntStr == null)
                {
                    capInfo.CurrentIntStr = "0";
                }
                if (capInfo.CapId == TwCap.ICAP_AUTODISCARDBLANKPAGES)
                {
                    {
                        Int32 intVal = Int32.Parse(capInfo.CurrentIntStr);
                        BlankImageThreshold.Text = (Math.Round((double)intVal / 1024)).ToString();
                    }
                    checkBox.IsChecked = !(capInfo.CurrentIntStr == "0");
                }
                else
                {
                    checkBox.IsChecked = (capInfo.CurrentIntStr == "1");
                }
                return capInfo.SetCap();
            }

        }
        /// <summary>
        /// 设置下拉列表的当前值
        /// </summary>
        /// <param name="comboBox"></param>
        /// <param name="capInfo"></param>
        /// <returns></returns>
        private bool SetCurrentValueOfComboBox(ComboBox comboBox, CapInfo capInfo)
        {
            if (capInfo.CurrentIntStr == null)
            {
                return false;
            }
            string enumStr = twSession.ConvertIntStringToEnumString(capInfo.CapId, capInfo.CurrentIntStr);
            string currentStr = null;
            int i = comboBox.Items.Count - 1;
            for (; i >= 0; i--)
            {
                string tempStr = (comboBox.Items[i] as ComboBoxItem).Content.ToString();
                if (tempStr == enumStr)
                {
                    comboBox.SelectedIndex = i;
                    currentStr = tempStr;
                    break;
                }
            }
            if (i < 0)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 添加一个数据项到下拉列表
        /// </summary>
        /// <param name="strItem"></param>
        /// <param name="ctrl"></param>
        /// <param name="capID"></param>
        private void AddComboBoxItem(String strItem, Control ctrl, TwCap capID)
        {
            ComboBoxItem newItem = new ComboBoxItem();
            string capStr = twSession.ConvertIntStringToEnumString(capID, strItem);
            if (capStr != null)
            {
                newItem.Content = capStr;
                ((ComboBox)(ctrl)).Items.Add(newItem);
            }
        }

        /// <summary>
        /// 点击cancel按钮响应事件
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        /// <summary>
        /// 保存用户参数设置到配置文件中
        /// </summary>
        /// <param name="sender">事件对象</param>
        /// <param name="e">事件名称</param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Hashtable capEnableList = new Hashtable();
                foreach (TwCap id in capList)
                {
                    Control ctrl = capToControl[id] as Control;
                    capEnableList.Add(id, ctrl.IsEnabled);
                }
                twSession.SaveSettings(capEnableList);
            }
            #region  异常处理
            catch (ArgumentException ae)
            {
                logger.Error("保存用户参数设置到配置文件中时发生参数异常，异常信息: " + ae.Message);
            }
            catch (FormatException fe)
            {
                logger.Error("保存用户参数设置到配置文件中时发生转换格式异常，异常信息: " + fe.Message);
            }
            catch (OverflowException oe)
            {
                logger.Error("保存用户参数设置到配置文件中时发生溢出异常，异常信息: " + oe.Message);
            }
            catch (Exception ex)
            {
                logger.Error("保存用户参数设置到配置文件中时发生异常，异常信息; " + ex.Message);
            }
            #endregion

            DialogResult = true;
        }
        #region  待删除
        /// <summary>
        /// 根据压缩算法当前值过滤不支持的文件格式
        /// </summary>
        /// <param name="fileList">原始文件列表</param>
        /// <returns>过滤后的文件列表</returns>
        private ArrayList FilterFileFormatInFileMode(ArrayList fileList)
        {
            try
            {
                if (ICAP_COMPRESSION.IsEnabled && ICAP_COMPRESSION.SelectedItem != null)
                {
                    // 获取压缩模式
                    TwCompression twCompression;
                    String strValue = ((ComboBoxItem)(ICAP_COMPRESSION.SelectedItem)).Content.ToString();
                    twCompression = (TwCompression)Enum.Parse(typeof(TwCompression), strValue);

                    ArrayList list = new ArrayList();
                    for (int i = 0; i < fileList.Count; i++)
                    {
                        TwFileFormat twFileFormat = (TwFileFormat)Enum.Parse(typeof(TwFileFormat), fileList[i].ToString());

                        //1:JPEG文件不支持无压缩
                        if (twCompression == TwCompression.TWCP_NONE && twFileFormat == TwFileFormat.TWFF_JFIF)
                            continue;
                        //2:TIF文件不支持JPEG系列压缩算法
                        if (twCompression == TwCompression.TWCP_JPEG && (twFileFormat == TwFileFormat.TWFF_TIFF || twFileFormat == TwFileFormat.TWFF_BMP))
                            continue;
                        //BMP文件不支持GROUP系列算法
                        if ((twFileFormat == TwFileFormat.TWFF_BMP || twFileFormat == TwFileFormat.TWFF_JFIF) &&
                              (twCompression == TwCompression.TWCP_GROUP31D || twCompression == TwCompression.TWCP_GROUP31DEOL
                              || twCompression == TwCompression.TWCP_GROUP32D || twCompression == TwCompression.TWCP_GROUP4))
                            continue;

                        list.Add(fileList[i].ToString());
                    }
                    return list;
                }
                else
                    return fileList;

            }
            catch (ArgumentException ae)
            {
                logger.Error("根据压缩算法当前值过滤不支持的文件格式时发生参数异常，异常信息: " + ae.Message);
                return fileList;
            }
            catch (FormatException fe)
            {
                logger.Error("根据压缩算法当前值过滤不支持的文件格式时发生转换格式异常，异常信息: " + fe.Message);
                return fileList;
            }
            catch (OverflowException oe)
            {
                logger.Error("根据压缩算法当前值过滤不支持的文件格式时发生溢出异常，异常信息: " + oe.Message);
                return fileList;
            }
            catch (Exception e)
            {
                logger.Error("根据压缩算法当前值过滤不支持的文件格式时发生异常，异常信息; " + e.Message);
                return fileList;
            }
        }

        /// <summary>
        /// 根据像素类型当前值过滤压缩算法
        /// </summary>
        /// <param name="cmpList">原始压缩算法</param>
        /// <returns>过滤后压缩算法</returns>
        private ArrayList FilterCompressionInFileMode(ArrayList cmpList)
        {
            try
            {
                //一：根据传输模式进行过滤
                if (ICAP_IXferMech.SelectedItem != null)
                {
                    //native模式
                    if (((ComboBoxItem)ICAP_IXferMech.SelectedItem).Content.ToString() == TwMode.TWSX_NATIVE.ToString())
                    {
                        ArrayList list = new ArrayList();
                        list.Add((int)TwCompression.TWCP_NONE);
                        return list;
                    }
                    else
                    {
                        //二：根据像素类型进行过滤
                        if (ICAP_PixelType.IsEnabled && ICAP_PixelType.SelectedItem != null)
                        {
                            TwPixelType twPixelType;
                            //获取像素类型
                            String strValue = ((ComboBoxItem)(ICAP_PixelType.SelectedItem)).Content.ToString();
                            twPixelType = (TwPixelType)Enum.Parse(typeof(TwPixelType), strValue);

                            //根据当前像素类型过滤压缩算法
                            ArrayList list = new ArrayList();
                            for (int i = 0; i < cmpList.Count; i++)
                            {
                                TwCompression twCompression = (TwCompression)Enum.Parse(typeof(TwCompression), cmpList[i].ToString());

                                //RGB不支持GROUP系列压缩
                                if ((twPixelType == TwPixelType.TWPT_RGB || twPixelType == TwPixelType.TWPT_GRAY) &&
                                      (twCompression == TwCompression.TWCP_GROUP31D || twCompression == TwCompression.TWCP_GROUP31DEOL
                                      || twCompression == TwCompression.TWCP_GROUP32D || twCompression == TwCompression.TWCP_GROUP4))
                                    continue;
                                //BW不支持JPEG压缩
                                if (twPixelType == TwPixelType.TWPT_BW && twCompression == TwCompression.TWCP_JPEG)
                                    continue;
                                list.Add(cmpList[i]);
                            }
                            return list;
                        }
                        else
                            return cmpList;
                    }
                }
                return cmpList;
            }
            catch (ArgumentException ae)
            {
                logger.Error("根据像素类型当前值过滤压缩算法时发生参数异常，异常信息: " + ae.Message);
                return cmpList;
            }
            catch (FormatException fe)
            {
                logger.Error("根据像素类型当前值过滤压缩算法时发生转换格式异常，异常信息: " + fe.Message);
                return cmpList;
            }
            catch (OverflowException oe)
            {
                logger.Error("根据像素类型当前值过滤压缩算法时发生溢出异常，异常信息: " + oe.Message);
                return cmpList;
            }
            catch (Exception e)
            {
                logger.Error("根据像素类型当前值过滤压缩算法时发生异常，异常信息; " + e.Message);
                return cmpList;
            }
        }

        /* 响应传输模式选择变化事件，
        * 当选择native模式时，显示控件可支持的文件格式
        * 当选择file模式时，提供扫描仪支持的文件格式
        */
        private void ICAP_IXferMech_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ;
        }

        private void ICAP_COMPRESSION_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ;
        }
        #endregion

        // 下拉列表变化的响应事件
        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!initFinishedFlag)
            {
                return;
            }
            TwCap capId;
            int index = 0;
            Control control = e.Source as Control;
            for (int i = 0; i < capList.Length; i++)
            {
                capId = capList[i];
                if (capToControl[capId] as Control == control)
                {
                    index = i;
                    break;
                }
            }

            capId = capList[index];
            CapInfo capInfo = twSession.GetScannerCap(capId);
            control = capToControl[capId] as Control;
            string enumString = (((ComboBox)control).SelectedItem as ComboBoxItem).Content.ToString();
            capInfo.CurrentIntStr = twSession.ConvertEnumStringToIntString(capId, enumString);
            SetOneValue(control, capInfo);
            UpdateConfigUi(index + 1, capList.Length - 1);

            if (capId == TwCap.ECAP_PAPERSOURCE)
            {
                ComboBoxItem item = ((ComboBox)ECAP_PAPERSOURCE).SelectedItem as ComboBoxItem;
                if (item.Content.ToString() == PaperSouceString.Platen)
                    CAP_DUPLEXENABLED.IsEnabled = false;
                if (item.Content.ToString() == PaperSouceString.ADF)
                    CAP_DUPLEXENABLED.IsEnabled = true;
            }
        }

        // 勾选框被勾选时的处理
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            if (!initFinishedFlag)
            {
                return;
            }
            CheckBox checkBox = e.Source as CheckBox;
            CapInfo capInfo = null;
            int index = 0;
            for (int i = 0; i < capList.Length; i++)
            {
                TwCap tempCapId = capList[i];
                if (capToControl[tempCapId] as CheckBox == checkBox)
                {
                    index = i;
                    break;
                }
            }
            TwCap capId = capList[index];
            capInfo = twSession.GetScannerCap(capId);
            capInfo.CurrentIntStr = "1";
            SetOneValue((Control)checkBox, capInfo);
            UpdateConfigUi(index + 1, capList.Length - 1);
        }

        // 勾选框被反勾选时的处理
        private void CheckBox_UnChecked(object sender, RoutedEventArgs e)
        {
            if (!initFinishedFlag)
            {
                return;
            }
            CheckBox checkBox = e.Source as CheckBox;
            CapInfo capInfo = null;
            int index = 0;
            for (int i = 0; i < capList.Length; i++)
            {
                TwCap tempCapId = capList[i];
                if (capToControl[tempCapId] as CheckBox == checkBox)
                {
                    index = i;
                    break;
                }
            }
            TwCap capId = capList[index];
            capInfo = twSession.GetScannerCap(capId);
            capInfo.CurrentIntStr = "0";
            SetOneValue((Control)checkBox, capInfo);
            UpdateConfigUi(index + 1, capList.Length - 1);
        }

        // 判断空白页大小编辑框输入的是否是数字, 及数字的范围
        private void BlankImageThreshold_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            int number = 0;
            Int32 maxSize = Int32.MaxValue / 1024;
            if (!Int32.TryParse(e.Text, out number)) //输入的是否是数字
                e.Handled = true;
            else
            {
                if (number > maxSize)
                {
                    e.Handled = true;
                }
            }
            Console.WriteLine("BlankImageThreshold_PreviewTextInput");
        }

        // 判断空白页大小编辑框输入数字的范围
        private void BlankImageThreshold_TextChanged(object sender, TextChangedEventArgs e)
        {
            //TextBox tb = sender as TextBox;
            //int number = 0;
            //Int32 maxSize = Int32.MaxValue / 1024;

            //if (!Int32.TryParse(tb.Text, out number))
            //    MessageBox.Show("请输入从0到" + maxSize + "之间的数字");
            //else
            //{
            //    if (number > maxSize)
            //    {
            //        MessageBox.Show("输入的数字大于" + maxSize);
            //        tb.Text = "0";
            //        e.Handled = true;
            //    }
            //}
            Console.WriteLine("BlankImageThreshold_TextChanged");
        }

        // 空白页勾选
        private void ICAP_AUTODISCARDBLANKPAGES_Checked(object sender, RoutedEventArgs e)
        {
            BlankImageThreshold.IsEnabled = true;
            UInt32 intValue = UInt32.Parse(BlankImageThreshold.Text) * 1024;
            CapInfo capInfo = twSession.GetScannerCap(TwCap.ICAP_AUTODISCARDBLANKPAGES);
            capInfo.CurrentIntStr = intValue.ToString();
            capInfo.SetCap();
        }

        // 空白页反勾选
        private void ICAP_AUTODISCARDBLANKPAGES_Unchecked(object sender, RoutedEventArgs e)
        {
            BlankImageThreshold.IsEnabled = false;
            CapInfo capInfo = twSession.GetScannerCap(TwCap.ICAP_AUTODISCARDBLANKPAGES);
            capInfo.CurrentIntStr = "0";
        }

        // 输入框失去焦点时保存值并设置
        private void BlankImageThreshold_LostFocus(object sender, RoutedEventArgs e)
        {
            // 获取空白页大小 
            if (BlankImageThreshold.IsEnabled)
            {
                UInt32 intValue = UInt32.Parse(BlankImageThreshold.Text) * 1024;
                CapInfo capInfo = twSession.GetScannerCap(TwCap.ICAP_AUTODISCARDBLANKPAGES);
                capInfo.CurrentIntStr = intValue.ToString();
                capInfo.SetCap();
            }
        }

        private void SetFileFormatInNative(Control control, CapInfo capInfo)
        {
            if (!control.IsEnabled)
            {
                return;
            }

            if (control is ComboBox)
            {
                ComboBox comboBox = control as ComboBox;

                if (capInfo.CurrentIntStr == null)
                {
                    if (capInfo.DefaultIntStr != null)
                    {
                        capInfo.CurrentIntStr = capInfo.DefaultIntStr;
                    }
                    else
                    {
                        comboBox.SelectedIndex = comboBox.Items.Count / 2;
                        string enumString = (comboBox.SelectedItem as ComboBoxItem).Content.ToString();
                        string intString = twSession.ConvertEnumStringToIntString(capInfo.CapId, enumString);
                        capInfo.CurrentIntStr = intString;

                    }

                }

                string intStr = capInfo.CurrentIntStr;
                string enumStr = twSession.ConvertIntStringToEnumString(capInfo.CapId, intStr);
                string currentStr = null;
                for (int i = 0; i < comboBox.Items.Count; i++)
                {
                    string tempStr = (comboBox.Items[i] as ComboBoxItem).Content.ToString();
                    if (tempStr == enumStr)
                    {
                        comboBox.SelectedIndex = i;
                        currentStr = tempStr;
                        break;
                    }
                }
                return;
            }
        }
    }
}




