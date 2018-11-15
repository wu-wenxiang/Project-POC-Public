using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections;
using System.Xml;
using UFileClient.Common;
using System.IO;
using log4net;
using ImageScan.TwainLib;

namespace ImageScan
{
    public class UserConfig
    {
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="capList"></param>
        public UserConfig(TwCap[] capList)
        {
            doc = Config.UFileCConfigDoc;
            this.capList = capList;
            configFilePath = Config.GetUFileClientConfigFile();
        }
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        private XmlDocument doc;                    // 配置文件对象
        private string configFilePath = null;       // 配置文件路径
        private Hashtable userSettings = null;      // 用户设置
        private TwCap[] capList;                    // 支持的能力列表
        private string defaultScannerName = null;   // 默认扫描仪名字 
        private Int32 maxSize = 0;                  // 图像容量的最大值

        static UserConfig()
        {
            //            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.GetConfigDir() + ConstCommon.logConfigFileName));
        }
        /// <summary>
        /// 默认扫描仪名称
        /// </summary>
        public string DefaultScannerName
        {
            get
            {
                if (defaultScannerName == null)
                {
                    defaultScannerName = GetDefaultScannerName();
                }
                return defaultScannerName;
            }
            set
            {
                defaultScannerName = value;
            }
        }

        /// <summary>
        /// 获取扫描扫描仪名称
        /// </summary>
        /// <returns></returns>
        private string GetDefaultScannerName()
        {
            try
            {
                return Config.GetValueByName(doc, "defaultScannerName");
            }
            catch (IOException e)
            {
                logger.Error("GetDefaultScannerName():IOexcception occured " + e.Message);
                return null;
            }
            catch (Exception e)
            {
                logger.Error("GetDefaultScannerName():Exception occured " + e.Message);
                return null;
            }
        }
        /// <summary>
        /// 保存指定扫描仪为默认扫描仪到配置文件
        /// </summary>
        /// <param name="name">扫描仪名称</param>
        /// <returns></returns>
        public bool SaveDefaultScannerName(string name)
        {
            defaultScannerName = name;
            try
            {
                XmlNode defaultScannerNameElement = doc.SelectSingleNode("/ufileclient_config/defaultScannerName");
                if (defaultScannerNameElement != null)
                {
                    defaultScannerNameElement.InnerText = defaultScannerName;
                }
                else
                {
                    XmlNode root = doc.SelectSingleNode("/ufileclient_config");
                    XmlNode tmp = doc.CreateElement("defaultScannerName");
                    tmp.InnerText = defaultScannerName;
                    root.AppendChild(tmp);
                }
                doc.Save(configFilePath);
                return true;
            }
            catch (Exception)
            {
                logger.Error("SaveDefaultScannerName Failed");
                return false;
            }
        }

        /// <summary>
        /// 配置文件的图像文件最大值
        /// </summary>
        public Int32 MaxSize
        {
            get
            {
                if (maxSize == 0)
                {
                    maxSize = GetMaxImageSize();
                }
                return maxSize;
            }
            set
            {
                maxSize = value;
            }
        }

        /// <summary>
        /// 读取配置文件中图像允许最大值
        /// </summary>
        /// <returns></returns>
        private Int32 GetMaxImageSize()
        {
            string maxImageSize;
            XmlNode maxImageSizeEle = doc.SelectSingleNode("/ufileclient_config/maxImageSize");
            if (maxImageSizeEle != null)
            {
                maxImageSize = maxImageSizeEle.InnerText;
                return 1024 * (Convert.ToInt32(maxImageSize));
            }
            else
            {
                XmlNode root = doc.SelectSingleNode("/ufileclient_config");
                XmlNode tmp = doc.CreateElement("maxImageSize");
                tmp.InnerText = "1024";
                root.AppendChild(tmp);
                doc.Save(Config.GetUFileClientConfigFile());
                return 1024 * 1024;
            }
        }

        /// <summary>
        /// 读取配置文件
        /// </summary>
        /// <returns></returns>
        private bool LoadSettings()
        {
            try
            {
                //一：读取默认扫描仪名称
                string scannerName = defaultScannerName;
                //检查扫描仪名称是否存在
                if (scannerName == null)
                {
                    logger.Debug("没有选择扫描仪");
                    return false;
                }

                //二：根据扫描仪名称读取节点配置
                XmlNodeList scannerNodeList = doc.SelectNodes(ConstCommon.scannerConfigXpath);

                //1：获得具有指定名称的配置节点
                XmlNode scannerNode = GetScannerSettins(scannerName, scannerNodeList);
                //1.1：如果不存在则返回
                if (scannerNode == null)
                {
                    logger.Debug("指定的扫描仪没有配置");
                    return true;
                }
                //2：读取节点能力配置列表
                else
                {
                    XmlNodeList scannerChildList = scannerNode.ChildNodes;
                    if (scannerChildList[1] != null)
                    {

                        XmlNodeList capNodeList = scannerChildList[1].ChildNodes;

                        for (int j = 0; j < capNodeList.Count; j++)
                        {
                            XmlElement xmlElement = capNodeList[j] as XmlElement;

                            //2.1 获取节点数值
                            string capName = xmlElement.Name;
                            string capType = xmlElement.GetAttribute("Itemtype");
                            string capValue = xmlElement.GetAttribute("Item");

                            //2.2 生成oneValueItem对象
                            TwCap capID = ((TwCap)Enum.Parse(typeof(TwCap), capName));
                            TwType dataType = (TwType)Enum.Parse(typeof(TwType), capType);
                            TwOneValue oneValue = new TwOneValue(dataType, capValue);

                            //2.4 记录扫描能力
                            userSettings[capID] = oneValue;
                        }
                    }

                }
            }
            #region  异常处理
            catch (System.Xml.XPath.XPathException e)
            {
                logger.Error("配置文件读取失败：" + e.Message);
                return false;
            }
            catch (ArgumentException e)
            {
                logger.Error("配置文件读取失败：" + e.Message);
                return false;
            }
            #endregion
            return true;
        }
        /// <summary>
        ///  查找节点scannerList的子元素列表中是否具有扫描仪名称的节点
        /// </summary>
        /// <param name="list">节点列表对象</param>
        /// <returns>包含扫描仪名称的子节点</returns>
        private XmlNode GetScannerSettins(string name, XmlNodeList list)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            XmlElement nameEle = null;
            for (int i = 0; i < list.Count; i++)
            {
                //1:得到每个scanner节点 <scanner>
                XmlNodeList scannerlist = list[i].ChildNodes;

                //2:获取扫描仪名称节点<sannerName>
                nameEle = scannerlist[0] as XmlElement;
                //2.1如果扫描仪名称存在，则得到扫描能力节点
                if (nameEle.InnerText.Equals(name))
                    return list[i];
                else
                    continue;
            }
            return null;
        }
        /// <summary>
        /// 获取用户设置
        /// </summary>
        /// <returns></returns>
        public Hashtable GetSettings()
        {
            if (userSettings == null)
            {
                userSettings = new Hashtable();
                //foreach (TwCap cap in capList)
                //{
                //    userSettings[cap] = null;
                //}
                if (!LoadSettings())
                {
                    logger.Debug("加载扫描仪配置会出错");
                }
            }
            return userSettings;
        }
        /// <summary>
        /// 保存能力设置
        /// </summary>
        /// <param name="capId">能力标识</param>
        /// <param name="oneValue">能力值</param>
        public void SetSettings(TwCap capId, TwOneValue oneValue)
        {
            if (userSettings == null)
            {
                return;
            }
            userSettings[capId] = oneValue;
        }
        /// <summary>
        /// 保存设置到配置文件
        /// </summary>
        /// <returns>保存是否成功</returns>
        public bool SaveSettings()
        {
            string configPath = Config.GetUFileClientConfigFile();  //用户自定义能力配置文件路径
            //1：如果配置文件不存在返回
            if (!File.Exists(configPath))
            {
                logger.Error("配置文件不存在");
                return false;
            }

            //2：如果扫描仪节点未设置则返回
            if (defaultScannerName == null)
            {
                logger.Error("默认扫描仪没有设置");
                return false;
            }
            //二：保存配置函数
            try
            {
                //1 创建扫描能力记录节点
                XmlNode scannerEle = doc.CreateElement("scanner");

                //1.1创建扫描仪名称节点
                XmlElement scannerNameEle = doc.CreateElement("scannerName");
                scannerNameEle.InnerText = defaultScannerName;
                scannerEle.AppendChild(scannerNameEle);

                //1.2创建能力列表                   
                XmlElement scannerCapListEle = doc.CreateElement("scannerCap");
                ICollection caps = userSettings.Keys;
                foreach (TwCap capID in caps)
                {
                    //1.2.1获取能力
                    string capName = capID.ToString();
                    TwOneValue oneValue = userSettings[capID] as TwOneValue;
                    if (oneValue == null || oneValue.ItemStr == null)
                    {
                        continue;
                    }
                    //1.2.2生成节点
                    XmlElement capEle = doc.CreateElement(capName);
                    capEle.SetAttribute("Itemtype", oneValue.ItemType.ToString());
                    capEle.SetAttribute("Item", oneValue.ItemStr);

                    //1.2.3添加到尾部
                    scannerCapListEle.AppendChild(capEle);
                }
                scannerEle.AppendChild(scannerCapListEle);


                //2.1读取配置文件中的不同扫描仪的配置节点列表
                XmlNode scanners = doc.SelectSingleNode("//ufileclient_config/scannerList");

                if (scanners == null)
                {
                    XmlNode root = doc.SelectSingleNode("//ufileclient_config");
                    XmlElement tmpList = doc.CreateElement("scannerList");
                    root.AppendChild(tmpList);
                    scanners = doc.SelectSingleNode("//ufileclient_config/scannerList");
                }

                //2.2查找配置文件中的默认扫描仪节点列表
                XmlNodeList scannerList = doc.SelectNodes("//ufileclient_config/scannerList/scanner");
                XmlNode scannerNode = GetScannerSettins(defaultScannerName, scannerList);

                //2.3 如果节点存在则替换之
                if (scannerNode != null)
                {
                    scanners.ReplaceChild(scannerEle, scannerNode);
                }
                //2.3 节点不存在则创建新节点
                else
                {
                    scanners.AppendChild(scannerEle);
                }
            }
            #region handle the exception
            catch (Exception e)
            {
                logger.Error("配置文件操作失败：" + e.Message);
                return false;
            }
            finally
            {
                if (doc != null)
                {
                    try
                    {
                        doc.Save(configPath);
                    }
                    catch (Exception e)
                    {
                        logger.Error("配置文件保存失败：" + e.Message);
                    }
                };
            }
            #endregion
            return true;
        }

    }
}
