﻿using System.Collections.Generic;
using System.Threading.Tasks;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using States = org.apache.zookeeper.ZooKeeper.States;

namespace ZookeeperClient.connection
{
    public interface IZKConnection
    {
        void Connect(Watcher watcher);

        void ReConnect(Watcher watcher);

        void Close();

        Task<string> CreateAsync(string path, byte[] data, CreateMode mode);

        Task<string> CreateAsync(string path, byte[] data, List<ACL> acl, CreateMode mode);

        Task DeleteAsync(string path);

        Task DeleteAsync(string path, int version);

        Task<bool> ExistsAsync(string path, bool watch);

        Task<List<string>> GetChildrenAsync(string path, bool watch);

        Task<DataResult> GetDataAsync(string path, bool watch);

        Task SetDataAsync(string path, byte[] data, int expectedVersion);

        Task<Stat> SetDataReturnStatAsync(string path, byte[] data, int expectedVersion);

        States GetZookeeperState();

        Task<long> GetCreateTimeAsync(string path);

        void AddAuthInfo(string scheme, byte[] auth);

        Task SetACLAsync(string path, List<ACL> acl, int version);

        Task<ACLResult> GetACLAsync(string path);
    }
}