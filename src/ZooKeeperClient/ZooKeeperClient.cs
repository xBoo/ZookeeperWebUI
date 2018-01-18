using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using log4net;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using ZookeeperClient.common;
using ZookeeperClient.connection;
using ZookeeperClient.listeners;
using ZookeeperClient.model;
using ZookeeperClient.serialize;

namespace ZookeeperClient
{
    public class ZooKeeperClient : Watcher, IDisposable, IZooKeeperClient
    {
        private static readonly ILog _logHandler = LogManager.GetLogger(typeof(ZooKeeperClient));

        readonly ISerializeHelper _serialize;
        readonly IZKConnection _zkConnection;

        Watcher.Event.KeeperState _currentState;
        bool _closed;
        static readonly object _connectLock = new object();
        readonly TimeSpan _retryTimeout;
        readonly TimeSpan _connectionTimeout;

        private readonly AutoResetEvent _stateChangedCondition = new AutoResetEvent(false);
        private readonly AutoResetEvent _nodeEventCondition = new AutoResetEvent(false);
        private readonly AutoResetEvent _dataChangedCondition = new AutoResetEvent(false);

        readonly ConcurrentHashSet<IZKStateListener> _stateListeners;
        readonly ConcurrentDictionary<string, ConcurrentHashSet<IZKNodeListener>> _nodeListeners;
        readonly ConcurrentDictionary<string, ConcurrentHashSet<IZKChildNodeListener>> _childListeners;

        readonly ConcurrentDictionary<string, Node> _ephemeralNodels;

        public ZooKeeperClient(IZKConnection zkConnection, TimeSpan connectionTimeout, TimeSpan retryTimeout)
        {
            _serialize = new SerializeHelper();
            _zkConnection = zkConnection;
            _stateListeners = new ConcurrentHashSet<IZKStateListener>();
            _nodeListeners = new ConcurrentDictionary<string, ConcurrentHashSet<IZKNodeListener>>();
            _childListeners = new ConcurrentDictionary<string, ConcurrentHashSet<IZKChildNodeListener>>();
            _ephemeralNodels = new ConcurrentDictionary<string, Node>();
            _retryTimeout = retryTimeout;
            _connectionTimeout = connectionTimeout;

            this.Connect(connectionTimeout, this);
        }

        public ZooKeeperClient(IZKConnection zkConnection, TimeSpan connectionTimeout) :
            this(zkConnection, connectionTimeout, new TimeSpan(0, 0, 0, 10))
        {
        }

        public ZooKeeperClient(IZKConnection zkConnection) :
            this(zkConnection, new TimeSpan(0, 0, 0, 10), new TimeSpan(0, 0, 0, 10))
        {
        }

        public ZooKeeperClient(string address, TimeSpan connectionTimeout, TimeSpan retryTimeout)
            : this(new ZKConnection(address), connectionTimeout, retryTimeout)
        {
        }

        public ZooKeeperClient(string address, TimeSpan connectionTimeout)
            : this(address, connectionTimeout, new TimeSpan(0, 0, 0, 10))
        {
        }

        public ZooKeeperClient(string address)
            : this(address, new TimeSpan(0, 0, 0, 10))
        {
        }

        #region Subscribe & Unsubscribe

        /// <summary>
        ///订阅子节点改变事件 
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="listener">监听对象</param>
        /// <returns>子节点列表</returns>
        public List<string> SubscribeChildNodeChanges(string path, IZKChildNodeListener listener)
        {
            _childListeners.TryGetValue(path, out ConcurrentHashSet<IZKChildNodeListener> listeners);
            if (listeners == null)
            {
                listeners = new ConcurrentHashSet<IZKChildNodeListener>();
                _childListeners.TryAdd(path, listeners);
            }
            listeners.TryAdd(listener);
            return Task.Run(async () => await WatchForChildsAsync(path)).ConfigureAwait(false).GetAwaiter().GetResult();
        }

        /// <summary>
        /// 移除订阅子节点改变事件
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="childListener">监听对象</param>
        public void UnSubscribeChildChanges(string path, IZKChildNodeListener childListener)
        {
            _childListeners.TryGetValue(path, out ConcurrentHashSet<IZKChildNodeListener> listeners);
            listeners?.TryRemove(childListener);
        }

        /// <summary>
        /// 添加节点事件监听
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="listener">监听对象</param>
        public void SubscribeNodeChanges(string path, IZKNodeListener listener)
        {
            this._nodeListeners.TryGetValue(path, out ConcurrentHashSet<IZKNodeListener> listeners);
            if (listeners == null)
            {
                listeners = new ConcurrentHashSet<IZKNodeListener>();
                this._nodeListeners.TryAdd(path, listeners);
            }
            listeners.TryAdd(listener);
            Task.Run(async () =>
            {
                await WatchForDataAsync(path);
            }).ConfigureAwait(false).GetAwaiter().GetResult();

            _logHandler.Info($"Subscribed node changes for {path}");
        }


        /// <summary>
        /// 删除节点事件监听
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="dataListener">监听对象</param>
        public void UnSubscribeNodeChanges(string path, IZKNodeListener dataListener)
        {
            this._nodeListeners.TryGetValue(path, out ConcurrentHashSet<IZKNodeListener> listeners);
            listeners?.TryRemove(dataListener);

            if (listeners != null && !listeners.IsEmpty) return;
            _nodeListeners.TryRemove(path, out ConcurrentHashSet<IZKNodeListener> _listeners);
        }

        /// <summary>
        /// 订阅节点状态变化事件
        /// </summary>
        /// <param name="listener"></param>
        public void SubscribeStateChanges(IZKStateListener listener)
        {
            _stateListeners.TryAdd(listener);
        }


        /// <summary>
        /// 移除订阅节点状态改变事件
        /// </summary>
        /// <param name="stateListener"></param>
        public void UnSubscribeStateChanges(IZKStateListener stateListener)
        {
            _stateListeners.TryRemove(stateListener);
        }

        /// <summary>
        /// 移除所有订阅
        /// </summary>
        public void UnSubscribeAll()
        {
            _childListeners.Clear();
            _nodeListeners.Clear();
            _stateListeners.Clear();
        }
        #endregion

        #region connect
        private void Connect(TimeSpan connectionTimeout, Watcher watcher)
        {
            try
            {
                this._zkConnection.Connect(watcher);
                bool isWatingSuccess = WaitUntilConnected(connectionTimeout);

                if (!isWatingSuccess)
                    _logHandler.Error("Connect timeout", new TimeoutException($"Unable to connect to zookeeper server within timeout: {connectionTimeout}"));
            }
            catch (Exception e)
            {
                _logHandler.Error("Connection error", e);
            }
        }

        private void ReConnect(bool isRecreate)
        {
            lock (_connectLock)
            {
                try
                {
                    this._zkConnection.Close();
                    this._zkConnection.Connect(this);
                    Task.Run(async () =>
                    {
                        await RecreateTemporaryNode(isRecreate).ConfigureAwait(false);
                    }).ConfigureAwait(false).GetAwaiter().GetResult();
                }
                catch (Exception e)
                {
                    _logHandler.Error($"Reconnect error:{e}");
                    throw;
                }
            }
        }

        private async Task RecreateTemporaryNode(bool isRecreate)
        {
            if (isRecreate)
            {
                foreach (var node in this._ephemeralNodels.Values)
                {
                    if (node.ACLs == null)
                        await this.CreateAsync(node.Path, node.Data, node.CreateMode);
                    else
                        await this.CreateAsync(node.Path, node.Data, node.ACLs, node.CreateMode);
                }
            }
            else
                this._ephemeralNodels.Clear();
        }
        #endregion

        #region create
        /// <summary>
        /// 创建节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="data">节点值</param>
        /// <param name="mode">节点创建类型</param>
        public async Task<string> CreateAsync(string path, object data, CreateMode mode)
        {
            return await CreateAsync(path, data, ZooDefs.Ids.OPEN_ACL_UNSAFE, mode);
        }

        /// <summary>
        /// 创建节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="data">节点值</param>
        /// <param name="acl">access control list</param>
        /// <param name="mode">节点创建类型</param>
        public async Task<string> CreateAsync(string path, object data, List<ACL> acl, CreateMode mode)
        {
            if (path == null)
            {
                throw new ArgumentNullException("Missing value for path");
            }
            if (acl == null || acl.Count == 0)
            {
                throw new ArgumentNullException("Missing value for ACL");
            }

            byte[] bytes = data == null ? null : this._serialize.Serialize(data);

            return await RetryUntilConnectedAsync(async () => await this._zkConnection.CreateAsync(path, bytes, acl, mode));
        }


        /// <summary>
        /// 递归创建节点,如果节点的上层节点不存在，则自动创建
        /// </summary>
        public async Task CreateRecursiveAsync(string path, object data, CreateMode createMode)
        {
            await CreateRecursiveAsync(path, data, ZooDefs.Ids.OPEN_ACL_UNSAFE, createMode);
        }

        /// <summary>
        /// 递归创建节点,如果节点的上层节点不存在，则自动创建
        /// </summary>
        public async Task CreateRecursiveAsync(string path, object data, List<ACL> acl, CreateMode createMode)
        {
            try
            {
                await CreateAsync(path, data, acl, createMode);
            }
            catch (KeeperException.NodeExistsException e)
            {
                _logHandler.Error($"The node '{path}' already exist", e);
            }
            catch (KeeperException.NoNodeException ne)
            {
                string parentDir = path.Substring(0, path.LastIndexOf('/'));
                await CreateRecursiveAsync(parentDir, null, acl, CreateMode.PERSISTENT);
                await CreateAsync(path, data, acl, createMode);
                _logHandler.Error($"The node '{path}' not exist", ne);
            }
        }

        /// <summary>
        /// 创建永久节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="data">节点值</param>
        /// <returns></returns>
        public async Task CreatePersistentAsync(string path, object data)
        {
            await CreateAsync(path, data, CreateMode.PERSISTENT);
        }

        /// <summary>
        /// 创建永久节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="data">节点值</param>
        /// <param name="acl">ACL</param>
        /// <returns></returns>
        public async Task CreatePersistentAsync(string path, object data, List<ACL> acl)
        {
            await CreateAsync(path, data, acl, CreateMode.PERSISTENT);
        }

        /// <summary>
        /// 创建永久节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="isCreateParents">是否自动创建父节点</param>
        /// <returns></returns>
        public async Task CreatePersistentAsync(string path, bool isCreateParents = false)
        {
            await CreatePersistentAsync(path, isCreateParents, ZooDefs.Ids.OPEN_ACL_UNSAFE);
        }

        /// <summary>
        /// 创建永久节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="isCreateParents">是否自动创建父节点</param>
        /// <param name="acl">access control list</param>
        public async Task CreatePersistentAsync(string path, bool isCreateParents, List<ACL> acl)
        {
            try
            {
                await CreateAsync(path, null, acl, CreateMode.PERSISTENT);
            }
            catch (KeeperException.NodeExistsException e)
            {
                if (!isCreateParents)
                {
                    throw e;
                }
            }
            catch (KeeperException.NoNodeException e)
            {
                if (!isCreateParents)
                {
                    throw e;
                }
                string parentDir = path.Substring(0, path.LastIndexOf('/'));
                await CreatePersistentAsync(parentDir, isCreateParents, acl);
                await CreatePersistentAsync(path, isCreateParents, acl);
            }
        }

        /// <summary>
        /// 创建临时节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="acl">access control list</param>
        public async Task CreateEphemeralNodeAsync(string path, List<ACL> acl)
        {
            await CreateAsync(path, null, acl, CreateMode.EPHEMERAL);
            this._ephemeralNodels.TryAdd(path, new Node(path, null, acl, CreateMode.EPHEMERAL));
        }

        /// <summary>
        /// 创建临时节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="data">节点值</param>
        public async Task CreateEphemeralNodeAsync(string path, object data = null)
        {
            await CreateAsync(path, data, CreateMode.EPHEMERAL);
            this._ephemeralNodels.TryAdd(path, new Node(path, data, CreateMode.EPHEMERAL));
        }

        /// <summary>
        /// 创建带序号的临时节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="data">节点值</param>
        /// <returns></returns>
        public async Task<string> CreateEphemeralSequentialAsync(string path, object data)
        {
            string retPath = await CreateAsync(path, data, CreateMode.EPHEMERAL_SEQUENTIAL);
            this._ephemeralNodels.TryAdd(path, new Node(path, data, CreateMode.EPHEMERAL_SEQUENTIAL));
            return retPath;
        }

        /// <summary>
        /// 创建临时节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="data">节点值</param>
        /// <param name="acl">access control list</param>
        /// <returns></returns>
        public async Task<string> CreateEphemeralSequentialAsync(string path, object data, List<ACL> acl)
        {
            string retPath = await CreateAsync(path, data, acl, CreateMode.EPHEMERAL_SEQUENTIAL);
            this._ephemeralNodels.TryAdd(path, new Node(path, data, acl, CreateMode.EPHEMERAL_SEQUENTIAL));
            return retPath;
        }

        #endregion

        #region exists

        /// <summary>
        /// 检查节点是否存在
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="isWatch">是否监听</param>
        /// <returns></returns>
        public async Task<bool> ExistsAsync(string path, bool isWatch)
        {
            return await RetryUntilConnectedAsync(async () => await this._zkConnection.ExistsAsync(path, isWatch));
        }

        /// <summary>
        /// 检查节点是否存在
        /// </summary>
        /// <param name="path">节点path</param>
        /// <returns></returns>
        public async Task<bool> ExistsAsync(string path)
        {
            return await ExistsAsync(path, HasListeners(path));
        }

        private bool HasListeners(string path)
        {
            this._nodeListeners.TryGetValue(path, out ConcurrentHashSet<IZKNodeListener> nodeListeners);
            if (nodeListeners != null && nodeListeners.Count > 0)
                return true;

            _childListeners.TryGetValue(path, out ConcurrentHashSet<IZKChildNodeListener> childListeners);
            return childListeners != null && childListeners.Count > 0;
        }
        #endregion

        #region delete

        /// <summary>
        /// 递归删除节点
        /// </summary>
        /// <param name="path">主节点path</param>
        /// <returns>删除状态</returns>
        public async Task<bool> DeleteRecursiveAsync(string path)
        {
            List<string> children;
            try
            {
                children = await GetChildrenAsync(path, false);
            }
            catch (KeeperException.NoNodeException)
            {
                return true;
            }

            foreach (string subPath in children)
            {
                var result = await DeleteRecursiveAsync(path + "/" + subPath);
                if (!result)
                {
                    return false;
                }
            }
            return await DeleteAsync(path);
        }

        /// <summary>
        /// 删除节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <returns>删除状态</returns>
        public async Task<bool> DeleteAsync(string path)
        {
            return await DeleteAsync(path, -1);
        }


        /// <summary>
        /// 指定版本删除节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="version">节点版本</param>
        /// <returns>删除状态</returns>
        public async Task<bool> DeleteAsync(string path, int version)
        {
            try
            {
                return await RetryUntilConnected(async () =>
                {
                    await this._zkConnection.DeleteAsync(path, version);
                    return true;
                });
            }
            catch (KeeperException.NoNodeException)
            {
                return false;
            }
        }
        #endregion

        #region get

        /// <summary>
        /// 获取节点值
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <param name="path">节点path</param>
        /// <returns>泛型T</returns>
        public async Task<T> GetDataAsync<T>(string path)
        {
            return (await GetZKDataAsync<T>(path, isReturnNullIfPathNotExists: false)).Data;
        }

        /// <summary>
        /// 获取节点值
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <param name="path">节点path</param>
        /// <param name="isReturnNullIfPathNotExists">当节点不存在是否返回空值</param>
        /// <returns>泛型T</returns>
        public async Task<T> GetDataAsync<T>(string path, bool isReturnNullIfPathNotExists)
        {
            return (await GetZKDataAsync<T>(path, isReturnNullIfPathNotExists: isReturnNullIfPathNotExists)).Data;
        }

        /// <summary>
        /// 获取节点值以及节点状态
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <param name="path">节点path</param>
        /// <returns>泛型T</returns>
        public async Task<ZKData<T>> GetZKDataAsync<T>(string path)
        {
            return await GetZKDataAsync<T>(path, isReturnNullIfPathNotExists: false);
        }


        /// <summary>
        /// 获取节点值以及节点状态
        /// </summary>
        /// <typeparam name="T">泛型T</typeparam>
        /// <param name="path">节点path</param>
        /// <param name="isReturnNullIfPathNotExists">当节点不存在是否返回空值</param>
        /// <returns>泛型T</returns>
        public async Task<ZKData<T>> GetZKDataAsync<T>(string path, bool isReturnNullIfPathNotExists)
        {
            ZKData<T> zkData = null;
            try
            {
                var dataResult = await RetryUntilConnectedAsync(async () => (await this._zkConnection.GetDataAsync(path, HasListeners(path))));
                zkData = new ZKData<T>
                {
                    Data = this._serialize.Deserialize<T>(dataResult.Data),
                    Stat = dataResult.Stat
                };
            }
            catch (KeeperException.NoNodeException e)
            {
                if (!isReturnNullIfPathNotExists)
                {
                    throw e;
                }
            }
            return zkData;
        }

        public async Task<DataResult> GetDataResultAsync(string path)
        {
            return await GetDataResultAsync(path, isReturnNullIfPathNotExists: false);
        }

        public async Task<DataResult> GetDataResultAsync(string path, bool isReturnNullIfPathNotExists)
        {
            return await RetryUntilConnectedAsync(async () => (await this._zkConnection.GetDataAsync(path, HasListeners(path))));
        }

        /// <summary>
        /// 获取所有子节点
        /// </summary>
        /// <param name="path">节点Path</param>
        /// <returns>节点列表</returns>
        public async Task<List<string>> GetChildrenAsync(string path)
        {
            return await GetChildrenAsync(path, HasListeners(path));
        }

        /// <summary>
        /// 获取所有子节点
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="isWatch">是否继续监听</param>
        /// <returns></returns>
        protected async Task<List<string>> GetChildrenAsync(string path, bool isWatch)
        {
            return await RetryUntilConnectedAsync(async () => await this._zkConnection.GetChildrenAsync(path, isWatch));
        }
        #endregion

        #region set

        /// <summary>
        /// 设置节点值
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="data">值对象</param>
        /// <returns>Task</returns>
        public async Task SetDataAsync(string path, object data)
        {
            await SetDataAsync(path, data, -1);
        }

        /// <summary>
        /// 设置节点值
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="data">值对象</param>
        /// <param name="version">特定版本</param>
        /// <returns>Task</returns>
        public async Task SetDataAsync(string path, object data, int version)
        {
            await SetDataReturnStatAsync(path, data, version);
        }

        /// <summary>
        /// 设置节点值并返回节点状态
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="data">值对象</param>
        /// <param name="expectedVersion">期望版本</param>
        /// <returns>Task</returns>
        public async Task<Stat> SetDataReturnStatAsync(string path, object data, int expectedVersion)
        {
            byte[] bytes = this._serialize.Serialize(data);
            return await this.RetryUntilConnectedAsync(async () =>
            {
                var stat = await this._zkConnection.SetDataReturnStatAsync(path, bytes, expectedVersion);
                return stat;
            });
        }
        #endregion

        #region acl

        /// <summary>
        /// 设置ACL
        /// </summary>
        /// <param name="path">节点path</param>
        /// <param name="acls">ALC Collction</param>
        /// <returns></returns>
        public async Task SetACLAsync(string path, List<ACL> acls)
        {
            if (path == null)
            {
                throw new ArgumentNullException($"Missing value for path");
            }

            if (acls == null || acls.Count == 0)
            {
                throw new ArgumentNullException($"Missing value for ACLs");
            }

            if (!(await ExistsAsync(path)))
            {
                throw new Exception($"trying to set acls on non existing node {path}");
            }

            await RetryUntilConnected(async () =>
            {
                var stat = (await this._zkConnection.GetDataAsync(path, false)).Stat;
                await this._zkConnection.SetACLAsync(path, acls, stat.getAversion());
            });
        }

        /// <summary>
        /// /获取ACL
        /// </summary>
        /// <param name="path">节点path</param>
        /// <returns>ACLResult</returns>
        public async Task<ACLResult> GetACLAsync(string path)
        {
            if (path == null)
            {
                throw new ArgumentNullException("Missing value for path");
            }

            if (!(await ExistsAsync(path)))
            {
                throw new Exception("trying to get acls on non existing node " + path);
            }
            return await this.RetryUntilConnectedAsync(async () => await this._zkConnection.GetACLAsync(path));
        }

        /// <summary>
        /// 添加认证信息，用于访问被ACL保护的节点
        /// </summary>
        /// <param name="scheme">scheme</param>
        /// <param name="auth">auth</param>
        public void AddAuthInfo(string scheme, byte[] auth)
        {
            RetryUntilConnected(() =>
            {
                this._zkConnection.AddAuthInfo(scheme, auth);
                return true;
            });
        }
        #endregion

        #region watching

        private async Task WatchForDataAsync(string path)
        {
            await RetryUntilConnected(async () =>
            {
                await this._zkConnection.ExistsAsync(path, true);
                return true;
            });
        }

        private async Task<List<string>> WatchForChildsAsync(string path)
        {
            return await RetryUntilConnected(async () =>
            {
                await ExistsAsync(path, true);
                try
                {
                    return await GetChildrenAsync(path, true);
                }
                catch (KeeperException.NoNodeException e)
                {
                    _logHandler.Error($"The node '{path}' is not exist", e);
                }
                return null;
            });
        }


        private async Task<T> RetryUntilConnectedAsync<T>(Func<Task<T>> fun)
        {
            var timeNow = DateTime.Now;
            while (true)
            {
                if (_closed)
                    throw new Exception("ZooKeeper Client already closed!");

                try
                {
                    return await fun();
                }
                catch (KeeperException.ConnectionLossException ce)
                {
                    await Task.Yield();
                    this.WaitForRetry();
                    _logHandler.Error($"Async retry until connected error:{ce}");
                }
                catch (KeeperException.SessionExpiredException se)
                {
                    await Task.Yield();
                    this.WaitForRetry();
                    _logHandler.Error($"Async retry until connected error:{se}");
                }

                if (_retryTimeout.TotalMilliseconds > 0 && (DateTime.Now - timeNow) >= _retryTimeout)
                {
                    throw new TimeoutException($"Operation cannot be retried because of retry timeout ({_retryTimeout.TotalMilliseconds} milliseconds)");
                }
            }
        }

        private T RetryUntilConnected<T>(Func<T> fun)
        {
            var timeNow = DateTime.Now;
            while (true)
            {
                if (_closed)
                    throw new Exception("ZooKeeper Client already closed!");

                try
                {
                    return fun();
                }
                catch (KeeperException.ConnectionLossException ce)
                {
                    Task.Yield();
                    this.WaitForRetry();
                    _logHandler.Error($"Retry until connected error:{ce}");
                }
                catch (KeeperException.SessionExpiredException se)
                {
                    Task.Yield();
                    this.WaitForRetry();
                    _logHandler.Error($"Retry until connected error:{se}");
                }

                if (_retryTimeout.TotalMilliseconds > 0 && (DateTime.Now - timeNow) >= _retryTimeout)
                {
                    throw new TimeoutException($"Operation cannot be retried because of retry timeout ({_retryTimeout.TotalMilliseconds} milliseconds)");
                }
            }
        }

        #endregion

        #region close
        /// <summary>
        /// 关闭ZooKeeperClient
        /// </summary>
        public void Close()
        {
            if (_closed) return;

            _logHandler.Info("Closing zookeeper Client...");

            lock (_connectLock)
            {
                if (_closed) return;

                _closed = true;
                this._zkConnection.Close();
            }
        }
        #endregion

        #region wait for handle
        private void WaitForRetry()
        {
            if (this._retryTimeout.TotalMilliseconds > 0)
                this.WaitUntilConnected(this._retryTimeout);
            else
                this.WaitUntilConnected();
        }

        private bool WaitUntilConnected(TimeSpan timeOut)
        {
            return WaitForKeeperState(Watcher.Event.KeeperState.SyncConnected, timeOut);
        }

        private void WaitUntilConnected()
        {
            WaitUntilConnected(this._connectionTimeout);
        }

        private bool WaitForKeeperState(Watcher.Event.KeeperState keeperState, TimeSpan timeOut)
        {
            _logHandler.Info($"Waiting for keeper state {Convert.ToString(keeperState)}");


            bool stillWaiting = true;
            while (_currentState != keeperState)
            {
                if (!stillWaiting)
                {
                    return false;
                }
                stillWaiting = _stateChangedCondition.WaitOne(timeOut);
            }
            _logHandler.Info($"State is {_currentState}");
            return true;
        }
        #endregion

        #region process
        public override async Task process(WatchedEvent @event)
        {
            _logHandler.Info($"Received event: {@event}");
            if (_closed)
            {
                _logHandler.Info("zookeeper already closed,watch event was been ignore!");
                _logHandler.Info($"path:{@event.getPath()},state:{@event.getState()}");
                return;
            }

            bool stateChanged = @event.getPath() == null;
            bool nodeChanged = @event.getPath() != null;
            bool dataChanged = @event.get_Type() == Event.EventType.NodeDataChanged ||
                               @event.get_Type() == Event.EventType.NodeDeleted ||
                               @event.get_Type() == Event.EventType.NodeCreated ||
                               @event.get_Type() == Event.EventType.NodeChildrenChanged;


            try
            {
                if (stateChanged)
                {
                    await Task.Run(() => ProcessStateChanged(@event));
                }

                if (nodeChanged)
                {
                }

                if (dataChanged)
                {
                    await Task.Run(() => ProcessDataOrChildChanged(@event));
                }
            }
            finally
            {
                if (stateChanged)
                {
                    _stateChangedCondition.Set();

                    // If the session expired we have to signal all conditions, because watches might have been removed and
                    // there is no guarantee that those
                    // conditions will be signaled at all after an Expired event
                    // TODO PVo write a test for this
                    if (@event.getState() == Event.KeeperState.Expired)
                    {
                        _nodeEventCondition.Set();
                        _dataChangedCondition.Set();

                        await Task.Run(() => ProcessAllEvents(@event.get_Type()));
                    }
                }
                if (nodeChanged)
                {
                    _nodeEventCondition.Set();
                }
                if (dataChanged)
                {
                    _dataChangedCondition.Set();
                }
            }
        }

        private void ProcessAllEvents(Event.EventType eventType)
        {
            foreach (var path in _childListeners.Keys)
            {
                if (_childListeners.TryGetValue(path, out ConcurrentHashSet<IZKChildNodeListener> childListenes))
                {
                    this.HandleChildChangedEvents(path, childListenes, eventType);
                }
            }
            foreach (var path in this._nodeListeners.Keys)
            {
                if (_nodeListeners.TryGetValue(path, out ConcurrentHashSet<IZKNodeListener> dataListeners))
                {
                    this.HandleNodeChanged(path, dataListeners, eventType);
                }
            }
        }

        private void ProcessDataOrChildChanged(WatchedEvent @event)
        {
            string path = @event.getPath();

            if (@event.get_Type() == Event.EventType.NodeChildrenChanged
                || @event.get_Type() == Event.EventType.NodeCreated
                || @event.get_Type() == Event.EventType.NodeDeleted)
            {
                _childListeners.TryGetValue(path, out ConcurrentHashSet<IZKChildNodeListener> childListeners);
                if (childListeners != null && !childListeners.IsEmpty)
                {
                    HandleChildChangedEvents(path, childListeners, @event.get_Type());
                }
            }

            if (@event.get_Type() == Event.EventType.NodeDataChanged
                || @event.get_Type() == Event.EventType.NodeDeleted
                || @event.get_Type() == Event.EventType.NodeCreated)
            {
                this._nodeListeners.TryGetValue(path, out ConcurrentHashSet<IZKNodeListener> listeners);
                if (listeners != null && !listeners.IsEmpty)
                {
                    HandleNodeChanged(@event.getPath(), listeners, @event.get_Type());
                }
            }
        }

        private void HandleNodeChanged(string path, ConcurrentHashSet<IZKNodeListener> listeners, Event.EventType eventType)
        {
            foreach (var listener in listeners)
            {
                Task.Run(async () =>
                {
                    await ExistsAsync(path, true);
                    try
                    {
                        var data = await GetDataAsync<object>(path, true);

                        if (eventType == Event.EventType.NodeCreated)
                        {
                            if (listener.NodeCreatedHandler != null)
                                await listener.NodeCreatedHandler(path, data);
                        }
                        else
                        {
                            if (listener.NodeChangedHandler != null)
                                await listener.NodeChangedHandler(path, data);
                        }

                        if (listener.DataCreatedOrChangeHandler != null)
                            await listener.DataCreatedOrChangeHandler(path, data);
                    }
                    catch (Exception)
                    {
                        if (listener.NodeDeletedHandler != null)
                            await listener.NodeDeletedHandler(path);
                    }
                }).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        private void HandleChildChangedEvents(string path, ConcurrentHashSet<IZKChildNodeListener> childListeners, Event.EventType eventType)
        {
            try
            {
                foreach (var listener in childListeners)
                {

                    Task.Run(async () =>
                    {
                        try
                        {
                            await ExistsAsync(path);
                            var children = await GetChildrenAsync(path);

                            if (listener.ChildChangedHandler != null)
                                await listener.ChildChangedHandler(path, children);

                            //子节点个数变化
                            if (eventType == Event.EventType.NodeChildrenChanged
                                || eventType == Event.EventType.NodeCreated
                                || eventType == Event.EventType.NodeDeleted)
                            {
                                if (listener.ChildCountChangedHandler != null)
                                    await listener.ChildCountChangedHandler(path, children);
                            }
                        }
                        catch (KeeperException.NoNodeException e)
                        {
                            if (listener.ChildChangedHandler != null)
                                await listener.ChildChangedHandler(path, null);

                            _logHandler.Error("Handle child changed event error", e);
                        }
                    }).ConfigureAwait(false).GetAwaiter().GetResult();
                }
            }
            catch (Exception e)
            {
                _logHandler.Error("Handle child changed event error", e);
            }
        }

        private void ProcessStateChanged(WatchedEvent @event)
        {
            this._currentState = @event.getState();

            this.HandleStateChanged(this._currentState);
            if (@event.getState() != Event.KeeperState.Expired) return;
            try
            {
                this.ReConnect(true);
            }
            catch (Exception e)
            {
                _logHandler.Error("Process state changed event error", e);
                this.HandleEstablishmentError(e);
            }
        }

        private void HandleEstablishmentError(Exception exception)
        {
            foreach (var listener in this._stateListeners.Keys)
            {
                listener.SessionEstablishmentErrorHandler?.Invoke(exception);
            }
        }

        private void HandleStateChanged(Event.KeeperState state)
        {
            foreach (var listener in this._stateListeners.Keys)
            {
                Task.Run(async () => { await listener.StateChangedHandler(state); });
            }
        }
        #endregion

        #region Dispose
        public void Dispose()
        {
            this.Close();
        }
        #endregion
    }
}