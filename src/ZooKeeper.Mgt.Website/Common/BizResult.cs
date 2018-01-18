using System.Runtime.Serialization;

namespace ZooKeeper.Mgt.Website.Common
{
    /// <summary>
    /// 业务返回类
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [DataContract]
    public class BizResult<T>
    {
        public BizResult(T returnObj, int businessCode)
        {
            this.ReturnObj = returnObj;
            this.BusinessCode = businessCode;
        }

        public BizResult(T returnObj, int businessCode, Pager pager)
        {
            this.ReturnObj = returnObj;
            this.BusinessCode = businessCode;
            this.Pager = pager;
        }

        public BizResult(T returnObj, int businessCode, string businessMessage)
        {
            this.ReturnObj = returnObj;
            this.BusinessCode = businessCode;
            this.BusinessMessage = businessMessage;
        }

        public BizResult(T returnObj, int businessCode, string businessMessage, Pager pager)
        {
            this.ReturnObj = returnObj;
            this.BusinessCode = businessCode;
            this.BusinessMessage = businessMessage;
            this.Pager = pager;
        }

        public BizResult(T returnObj)
        {
            this.ReturnObj = returnObj;
        }

        public BizResult(T returnObj, Pager pager)
        {
            this.ReturnObj = returnObj;
            this.Pager = pager;
        }

        public BizResult(EnumSystemCode sysCode)
        {
            this.SysCode = sysCode;
            this.SysMessage = this.SysCode.ToDescription();
        }

        public BizResult(EnumSystemCode sysCode, string sysMessage)
        {
            this.SysCode = sysCode;
            this.SysMessage = sysMessage;
        }

        public BizResult()
        {
        }

        /// <summary>
        /// 系统消息
        /// </summary>
        [DataMember]
        public string SysMessage { get; set; } = EnumSystemCode.Success.ToDescription();

        /// <summary>
        /// 业务代码
        /// </summary>
        [DataMember]
        public EnumSystemCode SysCode { get; set; } = EnumSystemCode.Success;

        /// <summary>
        /// 系统调用是否成功，如失败则参考 syscode 以及 sysmessage 查看失败信息
        /// </summary>
        public bool IsSuccess => this.SysCode == EnumSystemCode.Success;

        /// <summary>
        /// 业务消息
        /// </summary>
        [DataMember]
        public string BusinessMessage { get; set; }

        /// <summary>
        /// 业务代码
        /// </summary>
        [DataMember]
        public int BusinessCode { get; set; }

        /// <summary>
        /// 返回结果
        /// </summary>
        [DataMember]
        public T ReturnObj { get; set; }

        [DataMember]
        public Pager Pager { get; set; }

        public override string ToString()
        {
            return $"SysCode:{SysCode},SysMessage:{SysMessage},BusinessCode:{BusinessCode},BusinessMessage{BusinessMessage},ReturnObj:{ReturnObj}";
        }
    }
}
