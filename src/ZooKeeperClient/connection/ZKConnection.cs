using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using org.apache.utils;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using ZookeeperClient;

namespace ZookeeperClient.connection
{
    public class ZKConnection : IZKConnection
    {
        static readonly TimeSpan DEFAULT_SESSION_TIMEOUT = new TimeSpan(0, 0, 0, 3);
        private readonly string _address;
        private readonly TimeSpan _sessionTimeout;
        org.apache.zookeeper.ZooKeeper _zooKeeper;
        static readonly object _connectLock = new object();

        private readonly ILogConsumer _logWriter;

        public ZKConnection(string address, TimeSpan sessionTimeout, ILogConsumer logger)
        {
            this._address = address;
            this._sessionTimeout = sessionTimeout;
            this._logWriter = logger;
        }

        public ZKConnection(string address, TimeSpan sessionTimeout) : this(address, sessionTimeout, null) { }

        public ZKConnection(string address) : this(address, DEFAULT_SESSION_TIMEOUT) { }

        public void Connect(Watcher watcher)
        {
            lock (_connectLock)
            {
                if (_zooKeeper != null)
                {
                    this.LogInfo("ZooKeeper client has been started");
                }

                this._zooKeeper = new org.apache.zookeeper.ZooKeeper(_address, (int)_sessionTimeout.TotalMilliseconds, watcher);
                this.LogInfo($"ZooKeeper client has been created and connect to {this._address}");
            }
        }

        private void LogInfo(string message)
        {
            _logWriter.Log(TraceLevel.Info, nameof(ZKConnection), message, null);
        }

        public void ReConnect(Watcher watcher)
        {
            Close();
            Connect(watcher);
        }

        public void Close()
        {
            lock (_connectLock)
            {
                if (_zooKeeper == null) return;
                this.LogInfo($"Closing ZooKeeper connected to {this._address}");


                Task.Run(async () =>
                {
                    await _zooKeeper.closeAsync().ConfigureAwait(false);
                }).ConfigureAwait(false).GetAwaiter().GetResult();

                _zooKeeper = null;
            }
        }

        public async Task<string> CreateAsync(string path, byte[] data, CreateMode mode)
        {
            return await _zooKeeper.createAsync(path, data, ZooDefs.Ids.OPEN_ACL_UNSAFE, mode);
        }

        public async Task<string> CreateAsync(string path, byte[] data, List<ACL> acl, CreateMode mode)
        {
            return await _zooKeeper.createAsync(path, data, acl, mode);
        }

        public async Task DeleteAsync(string path)
        {
            await _zooKeeper.deleteAsync(path, -1);
        }

        public async Task DeleteAsync(string path, int version)
        {
            await _zooKeeper.deleteAsync(path, version);
        }

        public async Task<bool> ExistsAsync(string path, bool watch)
        {
            return await _zooKeeper.existsAsync(path, watch) != null;
        }

        public async Task<List<string>> GetChildrenAsync(string path, bool watch)
        {
            return (await _zooKeeper.getChildrenAsync(path, watch)).Children;
        }

        public async Task<DataResult> GetDataAsync(string path, bool watch)
        {
            return await _zooKeeper.getDataAsync(path, watch);
        }

        public async Task SetDataAsync(string path, byte[] data)
        {
            await SetDataAsync(path, data, -1);
        }

        public async Task SetDataAsync(string path, byte[] data, int version)
        {
            await _zooKeeper.setDataAsync(path, data, version);
        }

        public async Task<Stat> SetDataReturnStatAsync(string path, byte[] data, int expectedVersion)
        {
            return await _zooKeeper.setDataAsync(path, data, expectedVersion);
        }

        public org.apache.zookeeper.ZooKeeper.States GetZookeeperState()
        {
            return _zooKeeper.getState();
        }

        public async Task<long> GetCreateTimeAsync(string path)
        {
            Stat stat = await _zooKeeper.existsAsync(path, false);
            if (stat != null)
            {
                return stat.getCtime();
            }
            return -1;
        }

        public void AddAuthInfo(string scheme, byte[] auth)
        {
            _zooKeeper.addAuthInfo(scheme, auth);
        }

        public async Task SetACLAsync(string path, List<ACL> acl, int version)
        {
            await _zooKeeper.setACLAsync(path, acl, version);
        }

        public async Task<ACLResult> GetACLAsync(string path)
        {
            return await _zooKeeper.getACLAsync(path);
        }
    }
}