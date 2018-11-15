using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows.Forms;

namespace UFileClient.Common
{
    public static class AppConfig
    {
        static AppConfig() 
        {
 
        }
        public static string _appNo = null;
        public static string AppNo
        {
            get
            {
                return _appNo;
            }
            set
            {
                _appNo = value;

            }
        }
        public static string _baseDirPre = null;

        public static string BaseDirPre
        {
            get
            {
                return _baseDirPre;
            }
            set
            {
                _baseDirPre = value;
 //               Config.GetUFileClientConfigFile();
 //               Config.GetBranchInfoFile();
            }
        }
        public static string _xmlDir = null;
        public static string XmlDir
        {
            get
            {
                return _xmlDir;
            }
            set
            {
                _xmlDir = value;
                //               Config.GetUFileClientConfigFile();
                //               Config.GetBranchInfoFile();
            }
        }

    }
}
