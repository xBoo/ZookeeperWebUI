using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZookeeperClient.listeners
{
    public class ZKChildNodeListener: IZKChildNodeListener
    {
        public Func<string, List<string>, Task> ChildCountChangedHandler { get; set; }
        public Func<string, List<string>, Task> ChildChangedHandler { get; set; }
    }
}
