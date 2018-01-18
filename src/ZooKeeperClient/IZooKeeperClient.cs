using System.Collections.Generic;
using System.Threading.Tasks;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using ZookeeperClient.listeners;
using ZookeeperClient.model;

namespace ZookeeperClient
{
    public interface IZooKeeperClient
    {
        void AddAuthInfo(string scheme, byte[] auth);
        void Close();
        Task<string> CreateAsync(string path, object data, CreateMode mode);
        Task<string> CreateAsync(string path, object data, List<ACL> acl, CreateMode mode);
        Task CreateEphemeralNodeAsync(string path, object data = null);
        Task CreateEphemeralNodeAsync(string path, List<ACL> acl);
        Task<string> CreateEphemeralSequentialAsync(string path, object data);
        Task<string> CreateEphemeralSequentialAsync(string path, object data, List<ACL> acl);
        Task CreatePersistentAsync(string path, bool isCreateParents = false);
        Task CreatePersistentAsync(string path, bool isCreateParents, List<ACL> acl);
        Task CreatePersistentAsync(string path, object data);
        Task CreatePersistentAsync(string path, object data, List<ACL> acl);
        Task CreateRecursiveAsync(string path, object data, CreateMode createMode);
        Task CreateRecursiveAsync(string path, object data, List<ACL> acl, CreateMode createMode);
        Task<bool> DeleteAsync(string path);
        Task<bool> DeleteAsync(string path, int version);
        Task<bool> DeleteRecursiveAsync(string path);
        void Dispose();
        Task<bool> ExistsAsync(string path);
        Task<bool> ExistsAsync(string path, bool isWatch);
        Task<ACLResult> GetACLAsync(string path);
        Task<List<string>> GetChildrenAsync(string path);
        Task<T> GetDataAsync<T>(string path);
        Task<T> GetDataAsync<T>(string path, bool isReturnNullIfPathNotExists);
        Task<ZKData<T>> GetZKDataAsync<T>(string path);
        Task<ZKData<T>> GetZKDataAsync<T>(string path, bool isReturnNullIfPathNotExists);
        Task process(WatchedEvent @event);
        Task SetACLAsync(string path, List<ACL> acls);
        Task SetDataAsync(string path, object data);
        Task SetDataAsync(string path, object data, int version);
        Task<Stat> SetDataReturnStatAsync(string path, object data, int expectedVersion);
        List<string> SubscribeChildNodeChanges(string path, IZKChildNodeListener listener);
        void SubscribeNodeChanges(string path, IZKNodeListener listener);
        void SubscribeStateChanges(IZKStateListener listener);
        void UnSubscribeAll();
        void UnSubscribeChildChanges(string path, IZKChildNodeListener childListener);
        void UnSubscribeNodeChanges(string path, IZKNodeListener dataListener);
        void UnSubscribeStateChanges(IZKStateListener stateListener);
    }
}