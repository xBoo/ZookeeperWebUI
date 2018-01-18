using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZookeeperClient.listeners
{
    public interface IZKChildNodeListener
    {
        /// <summary>
        /// 子节点数量变化
        /// parent path
        /// children
        /// </summary>
        Func<string, List<string>, Task> ChildCountChangedHandler { get; set; }


        /// <summary>
        /// 子节点内容变化
        /// parent path
        /// children
        /// </summary>
        Func<string,List<string>,Task> ChildChangedHandler { get; set; }
    }
}