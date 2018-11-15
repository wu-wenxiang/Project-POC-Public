using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml;
using System.ComponentModel;
using System.Reflection;
using log4net;
using System.Collections;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Text.RegularExpressions;
namespace UFileClient.Common
{
    /// <summary>
    /// 删除所有的临时文件操作类
    /// </summary>
    public static class DeleteTempFiles
    {
//        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static DeleteTempFiles()
        {
//            log4net.Config.XmlConfigurator.Configure(new FileInfo(Config.GetLoggerConfigFilePath()));
        }
        // 构造函数

        /// <summary>
        /// 删除所有的临时文件，该方法是异步的
        /// </summary>
        public static void DeleteAll()
        {
            BackgroundWorker _worker = new BackgroundWorker();
            _worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                DeleteImageFiles();
            };
            _worker.RunWorkerAsync();
        }


        // 删除图像文件所在文件夹
        public static void DeleteImageFiles()
        {
            string imgFolder = Config.GetImageDir();
            DeleteFolder(imgFolder);
        }

        // 删除指定目录
        public static void DeleteFolder(string targetFolder)
        {
            DirectoryInfo dirInfo = new DirectoryInfo(targetFolder);
            if (dirInfo.Exists)
            {
                DeleteFiles(targetFolder, true);
                dirInfo.Delete(true);
            }
        }

        //删除指定目录的所有文件和子目录，包括只读文件和子目录
        private static void DeleteFiles(string targetFolder, bool isDelSubDir)
        {
            foreach (string fileName in Directory.GetFiles(targetFolder))
            {
                File.SetAttributes(fileName, FileAttributes.Normal);
                File.Delete(fileName);
            }
            if (isDelSubDir)
            {
                DirectoryInfo dir = new DirectoryInfo(targetFolder);
                foreach (DirectoryInfo subFoder in dir.GetDirectories())
                {
                    DeleteFiles(subFoder.FullName, true);
                    subFoder.Delete();
                }
            }
        }
        // 删除文件列表中指定的文件
        public static void DeleteFilesInList(IList fileList)
        {
            if (fileList == null || fileList.Count == 0)
            {
                return;
            }
            BackgroundWorker _worker = new BackgroundWorker();
            _worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                foreach (string file in fileList)
                {
                    try
                    {
                        if (File.Exists(file))
                        {
                            File.Delete(file);
                        }
                    }
                    catch (Exception e)
                    {
                        MessageBox.Show(e.Message);
                        throw;
                    }
                }
                fileList.Clear();
            };
            _worker.RunWorkerAsync();
        }
        // 删除列表中指定的文件和目录
        public static void DeleteFilesInList(List<string> fileList, List<string> dirList)
        {
            if (fileList == null || fileList.Count == 0)
            {
                return;
            }
            BackgroundWorker _worker = new BackgroundWorker();
            _worker.DoWork += delegate(object s, DoWorkEventArgs args)
            {
                if (fileList != null)
                {
                    foreach (string file in fileList)
                    {
                        try
                        {
                            if (File.Exists(file))
                            {
                                File.Delete(file);
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                            throw;
                        }
                    }
                    fileList.Clear();
                }
                if (dirList != null)
                {
                    foreach (string dir in dirList)
                    {
                        try
                        {
                            if (Directory.Exists(dir))
                            {
                                foreach (string fileName in Directory.GetFiles(dir))
                                {
                                    File.SetAttributes(fileName, FileAttributes.Normal);
                                    File.Delete(fileName);
                                }
                                Directory.Delete(dir);
                            }
                        }
                        catch (Exception e)
                        {
                            MessageBox.Show(e.Message);
                            throw;
                        }
                    }
                    dirList.Clear();
                }
            };
            _worker.RunWorkerAsync();
        }


        /// <summary>
        /// 删除指定文件
        /// </summary>
        /// <param name="filePath">待删除文件路径</param>
        /// <returns>删除是否成功</returns>
        public static bool DeleteFile(string filePath)
        {
            bool flag = true;
            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
                flag = false;
            }
            return flag;
        }

        /// <summary>
        /// 删除指定文件夹
        /// </summary>
        /// <param name="dirPath">文件夹路径</param>
        /// <param name="recursive">是否删除指定目录的子目录、目录或文件夹</param>
        /// <returns></returns>
        public static bool DeleteDirectory(string dirPath, bool recursive)
        {
            bool flag = true;
            try
            {
                if (Directory.Exists(dirPath))
                    Directory.Delete(dirPath, recursive);
            }
            catch (Exception exp)
            {
                MessageBox.Show(exp.Message);
                flag = false;
            }
            return flag;
        }
    }

    /// <summary>
    /// 工具类
    /// </summary>
    public static class Util
    {
        private static readonly ILog logger = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
        static BindingFlags bindingFlag = BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic;
        static Util()
        {

        }
        /// <summary>
        /// 设置对象属性值
        /// </summary>
        /// <param name="obj">对象名</param>
        /// <param name="fieldName">域名</param>
        /// <param name="val">域的值</param>
        public static void SetMemeber(object obj, string fieldName, string val)
        {
            Type type = obj.GetType();
            FieldInfo field = type.GetField(fieldName, bindingFlag);
            if (field == null)
            {
                logger.Error("Type " + type.ToString() + "has no property: "
                    + fieldName + " .");
                //此处需抛出异常，表明对象类型中不存在该属性
            }
            else
            {
                field.SetValue(obj, val);
            }
        }



        public static bool CheckDPI(string filePath, out float x, out  float y)
        {
            FileInfo finfo = new FileInfo(filePath);
            if (finfo.Length == 0)
            {
                x = 0;
                y = 0;
                return false;
            }

            //1：通过ImageProcessor.dll获取DPI
            IntPtr xP = Marshal.AllocCoTaskMem(4);
            IntPtr yP = Marshal.AllocCoTaskMem(4);
            GetDPI(filePath, xP, yP);
            x = Marshal.ReadInt32(xP);
            y = Marshal.ReadInt32(yP);
            Marshal.FreeCoTaskMem(xP);
            Marshal.FreeCoTaskMem(yP);

            //2:通过C#获取DPI
            System.Drawing.Image img = System.Drawing.Image.FromFile(filePath);
            System.Drawing.Graphics gdi = System.Drawing.Graphics.FromImage(img);
            bool ret = (x == img.HorizontalResolution) && (y == img.VerticalResolution);
            img.Dispose();
            gdi.Dispose();
            return ret;
        }
        /// <summary>
        /// 初始化接口
        /// </summary>
        /// <param name="appNo">必填项</param>
        /// <param name="baseDirPre">非必填项，不填写时，配置文件，文件下载目录，文件展示临时目录会自动在dll上一级目录</param>
        public static void AppInit(string appNo, string baseDirPre)
        {
            if (appNo == null)
            {
                MessageBox.Show("应用系统编号为空，请输入应用系统编号");
                return;
            }
            AppConfig.BaseDirPre = baseDirPre;
            AppConfig.AppNo = appNo;
            Config.GetUFileClientConfigFile();      
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="appNo"></param>
        /// <param name="baseDirPre"></param>
        /// <param name="xmlDir"></param>
        /// <returns></returns>
        public static bool AppInitBoeing(string appNo, string baseDirPre, string xmlDir)
        {
            if (appNo == null || appNo.Equals(""))
            {
                MessageBox.Show("应用系统编号为空，请输入应用系统编号");
                return false;
            }
            if (xmlDir == null || xmlDir.Equals("") || !Directory.Exists(xmlDir))
            {
                MessageBox.Show("配置文件存放路径为空，请指定");
                return false;
            }
            if (baseDirPre == null || baseDirPre.Equals("") || !Directory.Exists(baseDirPre))
            {
                MessageBox.Show("临时文件存放路径为空，请指定");
                return false;
            }
            AppConfig.BaseDirPre = baseDirPre;
            AppConfig.AppNo = appNo;
            AppConfig.XmlDir = xmlDir;
            Config.GetUFileClientConfigFile();
            return true;
        }

        [DllImport("ImageProcessor.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Auto)]
        internal static extern bool GetDPI(string srcImgName, IntPtr x, IntPtr y);
       
        /// <summary>
        /// 从文件名列表或得文件元数据列表
        /// </summary>
        /// <param name="imageNameList">文件名列表</param>
        /// <returns>文件元数据列表</returns> 
        public static List<FileMetaData> GetFileMetaDataListFromImageNameList(List<string> imageNameList)
        {
            if (imageNameList == null || imageNameList.Count == 0)
            {
                return null;
            }
            List<FileMetaData> fileMetaDataList = new List<FileMetaData>();
            foreach (string imageName in imageNameList)
            {
                FileMetaData fileMetaData = new FileMetaData();
                fileMetaData.FileName = imageName;
                fileMetaDataList.Add(fileMetaData);
            }
            return fileMetaDataList;
        }
        
      
        /// <summary>
        ///  255216 jpg 208207 doc xls ppt wps 8075 docx pptx xlsx zip
        ///  5150 txt 8297 rar 7790 exe 3780 pdf
        ///  13780 png 6677 bmp 7173 gif
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static int IsZipType(string filePath)
        {
            string fileclass = "";
            FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            BinaryReader bReader = new BinaryReader(fs);
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    fileclass += bReader.ReadByte().ToString();
                }
            }
            catch (Exception)
            {
                return -1;
            }
            if (fileclass.Equals("8075"))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="files"></param>
        /// <returns></returns>
        public static int IsZipTypeByte(byte[] files)
        {
            string fileclass = "";
            try
            {
                for (int i = 0; i < 2; i++)
                {
                    fileclass += files[i].ToString();
                }
            }
            catch (Exception)
            {
                return -1;
            }
            if (fileclass.Equals("8075"))
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static bool CheckFilePath(string filePath)
        {
            if (filePath.Equals(null) || filePath.Equals(""))
            {
                return false;
            }
            Regex regex = new Regex(@"^([a-zA-Z]:\\)?[^\/\:\*\?\""\<\>\|\,]*$");
            Match x = regex.Match(filePath);
            if (!x.Success)
            {
                return false;
            }
            return true;
        }
        public static bool CheckFileName(string fileName)
        {
            if (fileName.Equals(null) || fileName.Equals(""))
            {
                return false;
            }
            if (fileName.Contains("."))
            {
                return false;
            }
            Regex regex = new Regex(@"^[^\/\:\*\?\""\<\>\|\,]*$");
            Match x = regex.Match(fileName);
            if (!x.Success)
            {
                return false;
            }
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static bool IsImage(String path)
        {
            try
            {
                System.Drawing.Image image = System.Drawing.Image.FromFile(path);
                return true;
            }
            catch (Exception e)
            {
                return false;
            }
        }

    }
}
