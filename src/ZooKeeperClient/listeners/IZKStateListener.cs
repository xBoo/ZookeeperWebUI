using System;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace ZookeeperClient.listeners
{
    public interface IZKStateListener
    {
        /// <summary>
        /// 状态更改
        /// </summary>
        Func<Watcher.Event.KeeperState, Task> StateChangedHandler { set; get; }

        /// <summary>
        /// 会话过期
        /// </summary>
        Func<string, Task> SessionExpiredHandler { set; get; }

        /// <summary>
        /// 新会话创建
        /// </summary>
        Func<Task> NewSessionHandler { set; get; }

        /// <summary>
        /// 会话错误处理
        /// </summary>
        Func<Exception, Task> SessionEstablishmentErrorHandler { set; get; }
    }
}