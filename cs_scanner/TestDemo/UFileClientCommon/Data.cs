using System.Collections.Generic;

namespace UFileClient.Common
{

    #region 公用类数据结构

    /// <summary>
    /// 该类是一个文件的元数据（向下兼容而保留）
    /// </summary>
    public class FileMetaData
    {
        public string FileName { get; set; }
        public int SerialID { get; set; }
        public FileMetaDataStatus Status { get; set; }
        public string FileType { get; set; }
        public string FileDesp {get; set;}
        public string OriginalName { get; set; }
        public string TemplateId { get; set; }
        public string PageId { get; set; }
        public Dictionary<string, string> metaFields = new Dictionary<string, string>();

        public Dictionary<string, string> MetaFields
        {
            get { return metaFields; }
            set { metaFields = value; }
        }

        /// <summary>
        /// 设置文件元数据的扩展字段
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <param name="fieldValue">字段值</param>
        /// <returns></returns>
        public bool SetMetaField(string fieldName, string fieldValue)
        {
            metaFields[fieldName] = fieldValue;
            return true;
        }

        /// <summary>
        /// 查询文件元数据的扩展字段
        /// </summary>
        /// <param name="fieldName">字段名</param>
        /// <returns></returns>
        public string GetMetaField(string fieldName)
        {
            string str = null;
            if (metaFields.ContainsKey(fieldName))
                str = metaFields[fieldName];

            return str;
        }

        public FileMetaData()
        { }

        public FileMetaData(string imageName)
        {
            this.FileName = imageName;
        }

    }

    /// <summary>
    /// 该枚举体的元素是文件元数据中的状态的可选值
    /// </summary>
    public enum FileMetaDataStatus
    {
        K,
        A,
        D,
        CU,
        R,
    }

    /// <summary>
    /// 该类的对象用于存放命令报文的报头
    /// </summary>
    public class CmdXmlHeader
    {
        public string appNo = null;
        public string userNo = null;
        public string token = null;
    }

    /// <summary>
    /// 该枚举体的元素是上传/下载的文件种类
    /// </summary>
    public enum DomainType
    {
      
    }

    /// <summary>
    /// 该类定义与FSS中上传文件中页文件数据结构同结构，用于文件上传时数据转换
    /// </summary>
    public class UploadPageInfo
    {
        public string PageName { get; set; }
        public int PageSeq{get;set;}
        public string PageType { set; get; }
        public string PageDesp { get; set; }
        public byte[] File { get; set; }
    }

    ///<summary>
    ///该类定义下载文件的数据结构
    ///</summary>
    public class DownloadFileRe 
    {
        public string FileId { get; set; }
        public string SucessFlag { get; set; }
        public int PageNum { get; set; }
        public List<DownloadPageRe> DownloadPageReList { get; set; }
    }
    ///<summary>
    ///该类定义下载文件中的单笔文件数据结构，用于转化为FileMetaData
    ///</summary>
    public class DownloadPageRe
    {
        public string PageName { get; set; }
        public string PageId { get; set; }
        public int PageSeq { get; set; }
        public string PageType { set; get; }
        public string PageDesp { get; set; }
        public byte[] File { get; set; }
    }
    ///<summary>
    ///该类定义更新页文件数据结构，
    ///</summary>
    public class UpdatePageInfo
    {
        public string PageName { get; set; }
        public string PageId { get; set; }
        public string OprId { get; set; }
        public int PageSeq { get; set; }
        public string PageType { set; get; }
        public string PageDesp { get; set; }
        public byte[] File { get; set; }
    }

    #endregion

}
