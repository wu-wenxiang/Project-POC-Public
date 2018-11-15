using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UFileClient.Common
{
    /// <summary>
    /// 公用常量类
    /// </summary>
    public static class ConstCommon
    {
        // UFileClient配置文件名
        public const string SysConfigFileName = "sys-config.xml";

        // 用户配置文件名
        public const string UFileClientConfigFileName = "ufileclient-config.xml";

        // 相对基础路径
        public const string BasePath = "\\UFile\\Client\\";

        // 配置文件的路径名
        public const string CofigPathName = "configPath";
        // 下载文件保存目录在配置文件中的关键字
        public const string DownloadDirectory = "downloadFilePath";

        // 图像保存目录在配置文件中的关键字
        public const string ImageDirectory = "imagePath";

        // 扫描配置文件保存目录在配置文件中的关键字
        public const string ScanConfigDirectory = "configPath";

        // Log文件保存目录在配置文件中的关键字
        public const string LogDirectory = "logPath";

        // GTP文件存放的路径名
        public const string GtpPathName = "gtpPath";


        // 日志记录器配置文件名
        public const string logConfigFileName = "loggerConfig.xml";
  

        // 默认扫描仪名称在配置文件中的关键字
        public const string defaultScannerName = "defaultScannerName";

        // 扫描仪配置在配置文件中的Xpath
        public const string scannerConfigXpath = "//ufileclient_config/scannerList/scanner";


        //ADS服务地址配置信息
        public const string ADSSerList = "//ufileclient_config/ADSSerList/ADSSer";

        public const string BranchId = "BranchId";
        public const string AdsAddress = "AdsAddress";
        public const string F5Address = "F5Address";

        //ADS服务Url
        public const string ADSUrl = "ADSUrl";
        
        //ADS简易服务Url
        public const string ADSSimpleUrl = "ADSSimpleUrl";

        //FSS服务Url
        public const string FSSUrl = "FSSUrl";

        //FSS简易服务Url
        public const string FSSSimpleUrl = "FSSSimpleUrl";
        
        // 分行信息配置文件名
        public const string branchInfoFileName = "branch-info.xml";  
        //默认
        public const string defaultSysName = "default";

        //配置文件错误码
        public const string FAILED = "3000";
        //配置文件错误描述
        public const string errConfig = "Config file Error or Wrong branchId";
    }
}
