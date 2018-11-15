using System;
using System.Collections.Generic;
using System.IO;
using log4net;

namespace UFileClient.Common
{
    public static class FileNameUtility
    {
//        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static FileNameUtility()
        {
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.GetLoggerConfigFilePath()));
        }
        
        // 根据指定的文件路径，获取文件类型
        public static string GetFileType(string filePath)
        {
            return Path.GetExtension(filePath).Substring(1);
        }

        // 获取一个指定类型的唯一文件名
        public static string GetOnlyOneFileName(string fileType)
        {
            string guid = Guid.NewGuid().ToString();
            string type = null;

            if (fileType == null || fileType == "")
                type = "";
            else
                type = "." + fileType;

            return guid + type;
        }
        // 根据路径和文件获取全路径
        public static string GetFileFullPath(string dir,string fileName)
        {
            if(dir!=null)
            {
                if (dir.EndsWith("/") || dir.EndsWith("\\"))
                {
                     return dir + fileName;
                }
                else
                {
                    return dir + "/" + fileName;
                }
            }
            else
            {
                return null;
            }
        }
    }

}
