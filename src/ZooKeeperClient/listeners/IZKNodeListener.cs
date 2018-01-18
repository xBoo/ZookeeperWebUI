using System;
using System.Threading.Tasks;

namespace ZookeeperClient.listeners
{
    public interface IZKNodeListener
    {
        /// <summary>
        /// 节点变化
        /// </summary>
        Func<string, object, Task> NodeChangedHandler { get; set; }

        /// <summary>
        /// 节点创建或者变化
        /// </summary>
        Func<string, object, Task> DataCreatedOrChangeHandler { set; get; }

        /// <summary>
        /// 节点创建
        /// </summary>

        Func<string, object, Task> NodeCreatedHandler { get; set; }

        /// <summary>
        /// 节点删除
        /// </summary>

        Func<string, Task> NodeDeletedHandler { get; set; }
    }
}