using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using log4net;
using System.IO;
using System.Reflection;
using System.Xml.Schema;

namespace UFileClient.Common
{
    public static class Config
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static Config() //有静态构造器，BeforeFieldInit未被标记
        {
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(GetLoggerConfigFilePath()));
            //GetBranchInfoFile();
        }

        // sys-config.xml对象
        public static XmlDocument SysConfigDoc
        {
            get
            {
                return SysConfigDocument.Obj;
            }
        }

        // ufileClient-config.xml对象
        public static XmlDocument UFileCConfigDoc
        {
            get
            {
                return UfileClientConfigDocument.Obj;
            }
        }     
        private static string imageDir = null;
        private static string logDir = null;
        private static string downDir = null;
        private static string templateDir = null;
        private static string configDir = null;
        private static int isZiped = -1;

        //private static long sendTimeOut = -1;  //单位：秒，hbase服务异常情况下，解决等待时间较长问题，用于UFileService中wcf的配置，wk,2018-9-7

        private static string gtpDir = null;
        private static Dictionary<string, string> ADSSerList = new Dictionary<string, string>();
        private static Dictionary<string, string> F5SerList = new Dictionary<string, string>();

        private static string ADSUrl = null;
        private static string ADSSimpleUrl = null;

        private static string FSSUrl = null;
        private static string FSSSimpleUrl = null;


        /*
         * 获取配置文件中的图像保存路径
         * 如果配置文件中没有配置该路径，则认为图像保存路径为"当前工作目录\images\"
         */
        public static string GetImageDir()
        {
            if (imageDir == null)
            {
                ReadParamFromConfigFile();
            }
            if (!Directory.Exists(imageDir))
                Directory.CreateDirectory(imageDir);

            return imageDir;
        }

        // 获取配置文件中的Log路径
        public static string GetLogDir()
        {
            if (logDir == null)
            {
                ReadParamFromConfigFile();
            }
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);

            return logDir;
        }

        // 获取配置文件中的Tempalte路径
        public static string GetTempalteDir()
        {
            if (templateDir == null)
            {
                ReadParamFromConfigFile();
            }
            if (!Directory.Exists(templateDir))
                Directory.CreateDirectory(templateDir);

            return templateDir;
        }

        // 获取配置文件中的下载文件保存路径注释7
        public static string GetDownloadDir()
        {
            if (downDir == null)
            {
                ReadParamFromConfigFile();
            }

            if (!Directory.Exists(downDir))
                Directory.CreateDirectory(downDir);

            return downDir;
        }

        // 获取配置文件存放路径
        public static string GetConfigDir()
        {
            if (configDir == null)
            {
                ReadParamFromConfigFile();
            }

            if (!Directory.Exists(configDir))
                Directory.CreateDirectory(configDir);

            return configDir;
        }

        // 获取GTP文件路径 注释4
        public static string GetGtpDir()
        {
            if (gtpDir == null)
            {
                ReadParamFromConfigFile();
            }
            if (!Directory.Exists(gtpDir))
                Directory.CreateDirectory(gtpDir);

            return gtpDir;
        }

        /// <summary>
        /// 获取用户文件是否压缩标记
        /// </summary>
        /// <returns></returns>
        public static int GetZipedFlag()
        {
            if (isZiped == -1)
            {
                ReadParamFromConfigFile();
            }
            return isZiped;
        }

        /// <summary>
        /// 获取发送超时时间，wk，2018-9-7
        /// </summary>
        /// <returns></returns>
        //public static long GetSendTimeOut()
        //{
        //    if (sendTimeOut == -1)
        //    {
        //        ReadParamFromConfigFile();
        //    }
        //    return sendTimeOut;
        //}

        /// <summary>
        /// 获取分行接入地址
        /// </summary>
        /// <param name="branchId">机构编号</param>
        /// <param name="f5Mode">是否F5地址</param>
        /// <returns>服务地址，没有返回null</returns>
        public static string GetSerByBranchId(string branchId, bool f5Mode)
        {
            if (f5Mode)
            {
                return GetF5SerByBranchId(branchId);
            }
            else
            {
                return GetADDSerByBranchId(branchId);                     
            }
        }
        /// <summary>
        /// 获取当前分支机构码
        /// </summary>
        /// <returns>返回当前分支机构码</returns>
        public static string GetLocalBranchId() 
        {
            if (ADSSerList.Count == 0)
            {
                ReadParamFromConfigFile();
            }
            if (ADSSerList.Count > 2)
            {
                return null;
            }
            foreach (string branchId in ADSSerList.Keys)
            {
                if (!branchId.Equals("2"))
                {
                    return branchId;
                }
                    
            }
            return null;
        }
        /// <summary>
        /// 根据branchId获取ADS服务地址
        /// </summary>
        /// <param name="branchId">机构编号</param>
        /// <returns>ADS服务地址，没有返回null</returns>
        private static string GetADDSerByBranchId(string branchId)
        
        {
            if (ADSSerList.Count == 0)
            {
                
                ReadParamFromConfigFile();
            }

            return ADSSerList[branchId];
        }

        /// <summary>
        /// 根据branchId获取F5服务地址
        /// </summary>
        /// <param name="branchId">机构编号</param>
        /// <returns>F5服务地址，没有返回null</returns>
        private static string GetF5SerByBranchId(string branchId)
        {
            if (ADSSerList.Count == 0)
            {
                ReadParamFromConfigFile();
            }

            return F5SerList[branchId];
        }

        /// <summary>
        /// 获取ADS服务地址
        /// </summary>
        /// <returns></returns>
        public static string GetADSUrl(string adsser)
        {
            if (ADSUrl == null)
            {
                ReadParamFromConfigFile();
            }
            string fullurl = string.Format(ADSUrl, adsser);
            return fullurl;
        }

        /// <summary>
        /// 获取ADS简易服务地址
        /// </summary>
        /// <returns></returns>
        public static string GetADSSimpleUrl(string adsser)
        {
            if (ADSSimpleUrl == null)
            {
                ReadParamFromConfigFile();
            }
            string fullurl = String.Format(ADSSimpleUrl, adsser);
            return fullurl;
        }

        /// <summary>
        /// 获取FSS服务地址
        /// </summary>
        /// <returns></returns>
        public static string GetFSSUrl(string adsser)
        {
            if (FSSUrl == null)
            {
                ReadParamFromConfigFile();
            }
            string fullurl = string.Format(FSSUrl, adsser);
            return fullurl;
        }

        /// <summary>
        /// 获取FSS简易服务地址
        /// </summary>
        /// <returns></returns>
        public static string GetFSSSimpleUrl(string adsser)
        {
            if (ADSSimpleUrl == null)
            {
                ReadParamFromConfigFile();
            }
            string fullurl = String.Format(FSSSimpleUrl, adsser);
            return fullurl;
        }

        /// <summary>
        /// 从配置文件初始化所有设置
        /// </summary>
        /// <returns></returns>
        private static bool ReadParamFromConfigFile()
        
        {
            
            string basePath = GetBaseDir();
            try
            {
                if (basePath == null)
                {
                    return false;
                }
                // imageDir
                string imgFolder = GetValueByName(SysConfigDoc, ConstCommon.ImageDirectory);
                imageDir = basePath + imgFolder + "\\";

                // logDir
                //string logFolder = GetValueByName(SysConfigDoc, ConstCommon.LogDirectory);
                //logDir = basePath + logFolder + "\\";


                // configDir
                string configFolder = GetValueByName(SysConfigDoc, ConstCommon.CofigPathName);
                configDir = basePath + configFolder + "\\";

                // downDir
                string downFolder = GetValueByName(SysConfigDoc, ConstCommon.DownloadDirectory);
                downDir = basePath + downFolder + "\\";

                // gtpDir
                string gtpFolder = GetValueByName(SysConfigDoc, ConstCommon.GtpPathName);
                gtpDir = basePath + gtpFolder + "\\";

                //设置是否压缩标识
                XmlElement ziped = UFileCConfigDoc.SelectSingleNode("/ufileclient_config/IsZiped") as XmlElement;
                if (ziped == null)
                {
                    //添加IsZiped配置信息到iac-config.xml
                    XmlDocument xmldoc = UFileCConfigDoc;
                    XmlNode root = xmldoc.SelectSingleNode("/ufileclient_config");
                    XmlElement tmp = xmldoc.CreateElement("IsZiped");
                    tmp.InnerText = "1";
                    root.AppendChild(tmp);
                    xmldoc.Save(GetUFileClientConfigFile());
                    isZiped = 1;
                }
                else
                {
                    string s_ziped = ziped.InnerText;
                    if (s_ziped == null || s_ziped.Length == 0)
                    {
                        isZiped = 1;
                    }
                    else
                    {
                        isZiped = Convert.ToInt32(s_ziped);
                    }
                }

                //设置发送超时时间，下面都是新加的，仿照上面压缩的参数写的，一直到初始化ADS服务列表，wk，2018-9-7
                //XmlElement sendTimeOutNode = UFileCConfigDoc.SelectSingleNode("/ufileclient_config/SendTimeOut") as XmlElement;
                //if (sendTimeOutNode == null)
                //{
                //    //添加SendTimeOut配置信息到ufileclient-config.xml
                //    XmlDocument xmldoc = UFileCConfigDoc;
                //    XmlNode root = xmldoc.SelectSingleNode("/ufileclient_config");
                //    XmlElement tmp = xmldoc.CreateElement("SendTimeOut");
                //    tmp.InnerText = "180";//如果没有这个节点的话，默认设置为3分钟
                //    root.AppendChild(tmp);
                //    xmldoc.Save(GetUFileClientConfigFile());
                //    sendTimeOut = 180;
                //}
                //else
                //{
                //    string sendTime = sendTimeOutNode.InnerText;
                //    if (sendTime == null || sendTime.Length == 0)
                //    {
                //        sendTimeOut = 180;//xml中这个标签如果没有值的话也配置成180
                //    }
                //    else
                //    {
                //        sendTimeOut = Convert.ToInt64(sendTime);
                //    }
                //}


                //初始化ADS服务列表
                //XmlNodeList adsserlistnode = UFileCConfigDoc.SelectNodes(ConstCommon.ADSSerList);
                //foreach (XmlNode sernode in adsserlistnode)
                //{
                //    XmlElement branchIdNode = sernode.SelectSingleNode(ConstCommon.BranchId) as XmlElement;
                //    XmlElement adsAdressNode = sernode.SelectSingleNode(ConstCommon.AdsAddress) as XmlElement;
                //    XmlElement f5AdressNode = sernode.SelectSingleNode(ConstCommon.F5Address) as XmlElement;

                //    ADSSerList.Add(branchIdNode.InnerText, adsAdressNode.InnerText);
                //    F5SerList.Add(branchIdNode.InnerText, f5AdressNode.InnerText);
                //}

                ////初始化ADS服务地址
                //ADSUrl = GetValueByName(UFileCConfigDoc, ConstCommon.ADSUrl);

                ////初始化ADS简易服务地址
                //ADSSimpleUrl = GetValueByName(UFileCConfigDoc, ConstCommon.ADSSimpleUrl);

                ////初始化FSS服务地址
                //FSSUrl = GetValueByName(UFileCConfigDoc, ConstCommon.FSSUrl);

                ////初始化ADS简易服务地址
                //FSSSimpleUrl = GetValueByName(UFileCConfigDoc, ConstCommon.FSSSimpleUrl);

                return true;
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                throw e;
            }
        }
        // 获取配置文件中的基础路径
        public static string GetBaseDir()
        {
            string basePath = null;
            if (AppConfig.BaseDirPre == null || AppConfig.BaseDirPre == "" || !Directory.Exists(AppConfig.BaseDirPre))
            {
                string dllDir = System.Reflection.Assembly.GetExecutingAssembly().Location;
                basePath = Path.GetDirectoryName(dllDir);
                int pos = basePath.LastIndexOf("\\");
                basePath = basePath.Substring(0, pos);
                basePath += ConstCommon.BasePath + AppConfig.AppNo + "\\";
            }
            else 
            {
                basePath = AppConfig.BaseDirPre;
                //检查给定的根路径是否有双斜杠,有则删除
                if (basePath.EndsWith("\\"))
                {
                    basePath = basePath.Substring(0, (basePath.Length - 1));
                }
                basePath += ConstCommon.BasePath + AppConfig.AppNo + "\\";
            }

            return basePath;
        }

        /*
         * 获取log4net配置文件中的保存路径
         */
        public static string GetLoggerConfigFilePath()
        {
            string configfilePath = GetConfigDir() + ConstCommon.logConfigFileName;
            if (!File.Exists(configfilePath))
                InitDLL(configfilePath);
            return configfilePath;
        }

        // 获取UFileClient配置文件路径（含文件名）
        public static string GetUFileClientConfigFile()
        {
            try
            {
                //将日志记录器的配置文件拷贝到指定目录下
                //string logConfigFilePath = GetConfigDir() + ConstCommon.logConfigFileName;
                //if (!File.Exists(logConfigFilePath))
                //    initDLL(logConfigFilePath);
                string configFilePath = "";
                if (Directory.Exists(AppConfig.XmlDir))
                {
                    configFilePath = AppConfig.XmlDir + "\\" + ConstCommon.UFileClientConfigFileName;
                }
                else
                { 
                    configFilePath = GetConfigDir() + ConstCommon.UFileClientConfigFileName;
                    if (!File.Exists(configFilePath))
                    {
                        InitDLL(configFilePath);
                    }
                }

                return configFilePath;
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                throw e;
            }
        }

        // 获取分行信息配置文件路径（含文件名）
        public static string GetBranchInfoFile()
        {
            string branchInfoFilePath = GetConfigDir() + ConstCommon.branchInfoFileName;
            if (!File.Exists(branchInfoFilePath))
            {
                InitDLL(branchInfoFilePath);
            }
            return branchInfoFilePath;
        }


        /// <summary>
        /// 获取GTP目录
        /// </summary>
        /// <returns></returns>
        public static string GetGtpFolder()
        {
            XmlElement xmlElement = UFileCConfigDoc.SelectSingleNode("/ufileclient_config/GTPFolder") as XmlElement;
            if (xmlElement == null)
            {
                return null;
            }
            string gtpFolder = xmlElement.InnerText;
            return gtpFolder;
        }
        /// <summary>
        /// 机构代码
        /// </summary>
        //private static string _branchCode = null;
        //public static string branchCode
        //{
        //    get
        //    {
        //        if (_branchCode == null)
        //        {
        //            //从配置文件读取机构代码

        //            XmlElement tier2_BranchDefault = UFileCConfigDoc.SelectSingleNode("/iac_config/branchInfo/tier2_Branch") as XmlElement;
        //            if (tier2_BranchDefault == null)
        //            {
        //                return null;
        //            }
        //            string tier2_BranchCode = tier2_BranchDefault.GetAttribute("code");
        //            if (tier2_BranchCode == null)
        //            {
        //                _branchCode = null;
        //            }
        //            else if (tier2_BranchCode.Equals("None"))
        //            {
        //                XmlElement tier1_BranchDefault = UFileCConfigDoc.SelectSingleNode("/iac_config/branchInfo/tier1_Branch") as XmlElement;
        //                _branchCode = tier1_BranchDefault.GetAttribute("code");
        //            }
        //            else
        //            {
        //                _branchCode = tier2_BranchCode;
        //            }
        //        }
        //        return _branchCode;
        //    }
        //    set { _branchCode = value; }
        //}
        //private static string _branchName = null;
        //public static string branchName
        //{
        //    get
        //    {
        //        if (_branchName == null)
        //        {
        //            //从配置文件读取机构名称

        //            XmlElement tier2_BranchDefault = UFileCConfigDoc.SelectSingleNode("/iac_config/branchInfo/tier2_Branch") as XmlElement;
        //            if (tier2_BranchDefault == null)
        //            {
        //                return null;
        //            }
        //            string tier2_BranchName = tier2_BranchDefault.GetAttribute("name");
        //            if (tier2_BranchName == null)
        //            {
        //                _branchName = null;
        //            }
        //            else if (tier2_BranchName.Equals("None"))
        //            {
        //                XmlElement tier1_BranchDefault = UFileCConfigDoc.SelectSingleNode("/iac_config/branchInfo/tier1_Branch") as XmlElement;
        //                _branchName = tier1_BranchDefault.GetAttribute("name");
        //            }
        //            else
        //            {
        //                _branchName = tier2_BranchName;
        //            }
        //        }
        //        return _branchName;
        //    }
        //    set { _branchName = value; }
        //}
        public static XmlElement ReadElement(XmlDocument doc,String name)
        {
            XmlNodeList xmlNodeList = doc.GetElementsByTagName(name);
            if (xmlNodeList != null)
            {
                return xmlNodeList[0] as XmlElement;
            }
            return null;
        }
        public static string GetValueByName(XmlDocument doc, String name)
        {
            if (ReadElement(doc, name) != null)
                return ReadElement(doc, name).InnerText;
            else
                return null;
        }

        /// <summary>
        /// 将DLL的资源复制到指定的文件目录下
        /// </summary>
        /// <param name="fullPath">资源的文件路径</param>
        /// <returns>发生异常时返回异常错误信息，否则返回null</returns>
        public static string InitDLL(string fullPath)
        {
            FileStream fs = null;
            try
            {
                string path = System.AppDomain.CurrentDomain.BaseDirectory;
                string name = Path.GetFileName(fullPath);
                string dirPath = Path.GetDirectoryName(fullPath);


                //1：如果文件存在则返回
                if (File.Exists(fullPath))
                    return null;

                //2：目录不存在则创建目录
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                //3：获取dll的资源列表
                Assembly dll = Assembly.GetExecutingAssembly();
                string[] xmls = dll.GetManifestResourceNames();

                for (int i = 0; i < xmls.Length; i++)
                {
                    //如果资源列表名匹配，则读取资源到相应目录
                    if (xmls[i].Contains(name))
                    {
                        Stream xmlStream = dll.GetManifestResourceStream(xmls[i]);
                        byte[] buffer = new byte[xmlStream.Length];
                        xmlStream.Read(buffer, 0, buffer.Length);
                        fs = new FileStream(fullPath, FileMode.Create);
                        fs.Write(buffer, 0, buffer.Length);
                        fs.Flush();
                        fs.Close();



                        //if (name == "loggerConfig.xml")
                        //{
                        //    StreamReader reader = new StreamReader(fullPath);
                        //    string tempxml = reader.ReadToEnd();
                        //    reader.Close();
                        //    reader.Dispose();
                        //    string newdir = "..\\UserData\\IASP\\IAC\\"  + "\\log";
                        //    string olddir = ".\\IASP\\IAC\\log";
                        //    string cfgxml = tempxml.Replace(olddir, newdir);

                        //    StreamWriter writer = new StreamWriter(fullPath);
                        //    writer.Write(cfgxml);
                        //    writer.Close();
                        //    writer.Dispose();
                        //}

                    }
                }
                return null;
            }
            catch (Exception e)
            {
                logger.Error(e.Message);
                return e.Message;
            }
            finally 
            {
                fs.Close();
            }
        }

        /// <summary>
        /// 获取普通文件上传文件上限值，超过该值的文件将采用断点续传
        /// </summary>
        /// <returns></returns>
        public static long GetNormalFileMaxSize()
        {
            //TODO
            return 10 * 1024 * 1024;
        }
        /// <summary>
        /// 断点续传的时候，定义每次传输的文件字节大小
        /// </summary>
        /// <returns></returns>
        public static long GetBpsFileSize()
        {
            //TODO
            //public static long BPS_LENGTH = 1024 * 1024 * 1;
            return 1024 * 1024 * 1;
        }
    }
}
