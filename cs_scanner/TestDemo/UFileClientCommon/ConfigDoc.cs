using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using System.Reflection;
using System.IO;

namespace UFileClient.Common
{
    // sys-config.xml类
    static class SysConfigDocument
    {
        private static XmlDocument _obj = new XmlDocument();
        public static XmlDocument Obj
        {
            get
            {
                //_obj = new XmlDocument();
                Assembly dll = Assembly.GetExecutingAssembly();
                string[] xmls = dll.GetManifestResourceNames();
                int index = -1;
                for (int i = 0; i < xmls.Length; i++)
                {
                    //如果资源列表名匹配，则读取资源到相应目录
                    if (xmls[i].Contains(ConstCommon.SysConfigFileName))
                    {
                        index = i;
                        break;
                    }
                }
                Stream xmlStream = dll.GetManifestResourceStream(xmls[index]);
                _obj.Load(xmlStream);
                return _obj;
            }
        }

    }

    // ufileclient-config.xml类
    public static class UfileClientConfigDocument
    {
        private static XmlDocument _obj = new XmlDocument();
        public static XmlDocument Obj
        {
            get
            {
               _obj.Load(Config.GetUFileClientConfigFile());
                return  _obj;
            }           
        }
    }

}
