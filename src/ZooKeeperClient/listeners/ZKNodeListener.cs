using System;
using System.Threading.Tasks;

namespace ZookeeperClient.listeners
{
    public class ZKNodeListener : IZKNodeListener
    {
        public Func<string, object, Task> NodeChangedHandler { get; set; }
        public Func<string, object, Task> DataCreatedOrChangeHandler { get; set; }
        public Func<string, object, Task> NodeCreatedHandler { get; set; }
        public Func<string, Task> NodeDeletedHandler { get; set; }
    }
}
