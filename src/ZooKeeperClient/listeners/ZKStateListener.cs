using System;
using System.Threading.Tasks;
using org.apache.zookeeper;

namespace ZookeeperClient.listeners
{
    public class ZKStateListener : IZKStateListener
    {
       
        public Func<Watcher.Event.KeeperState, Task> StateChangedHandler { get; set; }

       
        public Func<string, Task> SessionExpiredHandler { get; set; }

      
        public Func<Task> NewSessionHandler { get; set; }

      
        public Func<Exception, Task> SessionEstablishmentErrorHandler { get; set; }
    }
}
