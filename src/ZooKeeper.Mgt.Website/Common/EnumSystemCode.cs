using System.ComponentModel;

namespace ZooKeeper.Mgt.Website.Common
{
    /// <summary>
    /// 业务CODE
    /// </summary>
    public enum EnumSystemCode
    {
        /// <summary>
        /// 成功
        /// </summary>
        [Description("成功")]
        Success = 1000,

        /// <summary>
        /// 内部错误
        /// </summary>
        [Description("内部错误")]
        Failed,

        /// <summary>
        /// 程序出错
        /// </summary>
        [Description("程序出错")]
        Exception,

        /// <summary>
        /// 服务器通信错误
        /// </summary>
        [Description("服务器通信错误")]
        CommunicationError,

        /// <summary>
        /// 服务器通信超时
        /// </summary>
        [Description("服务器通信超时")]
        Timeout,
    }
}