using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ZooKeeper.Mgt.Website.Models;
using ZookeeperClient;
using ZooKeeper.Mgt.Website.Common;
using ZookeeperClient.model;
using org.apache.zookeeper;

namespace ZooKeeper.Mgt.Website.Controllers
{
    public class HomeController : Controller
    {

        readonly IZooKeeperClient _zookeeperClient;
        readonly IConfiguration _configuration;
        readonly IHostingEnvironment _env;

        public HomeController(IZooKeeperClient zookeeperClient, IConfiguration configuration, IHostingEnvironment env)
        {
            _zookeeperClient = zookeeperClient;
            _configuration = configuration;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<BizResult<List<TreeNode>>> GetNodes(string path, int? parentId)
        {
            if (string.IsNullOrEmpty(path)) path = "/";

            if (path != "/")
            {
                var nodes = await GetChildrenNodesAsync(path, parentId, false);
                return new BizResult<List<TreeNode>>(nodes);
            }
            else
            {
                var returnNodes = new List<TreeNode>();
                var rootNode = (await GetChildrenNodesAsync(path, parentId, true)).FirstOrDefault(w => w.name == _configuration["RootNodeName"]);

                if (rootNode == null) return new BizResult<List<TreeNode>>(null, -1, "未找到配置对应的根节点！");

                returnNodes.Add(rootNode);
                var children = await GetChildrenNodesAsync(rootNode.bakValue, rootNode.id, false);
                returnNodes.AddRange(children.OrderBy(o => o.isParent));
                return new BizResult<List<TreeNode>>(returnNodes);
            }
        }

        public async Task<BizResult<ZNode>> GetNode(string path)
        {
            var data = await _zookeeperClient.GetDataResultAsync(path);
            return new BizResult<ZNode>(new ZNode
            {
                CreateTime = ConvertDatetime(data.Stat.getCtime()),
                ModifyTime = ConvertDatetime(data.Stat.getMtime()),
                Path = path,
                Value = Encoding.UTF8.GetString(data.Data),
                Version = data.Stat.getVersion()
            });
        }

        private DateTime ConvertDatetime(long unixTimestamp)
        {
            var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new System.DateTime(1970, 1, 1)); // 当地时区
            return startTime.AddMilliseconds(unixTimestamp);
        }

        private async Task<List<TreeNode>> GetChildrenNodesAsync(string path, int? parentId, bool isOpen)
        {
            List<TreeNode> nodeList = new List<TreeNode>();
            var nodes = await _zookeeperClient.GetChildrenAsync(path);
            for (int i = 0; i < nodes.Count; i++)
            {
                int idx = i + 1;
                var nPath = path == "/" ? $"{path}{nodes[i]}" : $"{path}/{nodes[i]}";
                if (!await _zookeeperClient.ExistsAsync(nPath)) continue;
                var dataResult = await _zookeeperClient.GetDataResultAsync(nPath);

                nodeList.Add(new TreeNode
                {
                    id = parentId.HasValue ? int.Parse(parentId.ToString() + idx) : idx,
                    pId = parentId ?? 0,
                    name = nodes[i],
                    bakValue = nPath,
                    open = isOpen,
                    isParent = dataResult.Stat.getNumChildren() > 0
                });
            }

            return nodeList.OrderBy(o => o.name).ToList();
        }

        [HttpPost]
        public async Task<BizResult<bool>> UpdateNode(string path, string value)
        {
            var exist = await _zookeeperClient.ExistsAsync(path);
            if (!exist) return new BizResult<bool>(false, -1, $"Node path '{path}' was not found");

            await _zookeeperClient.SetDataAsync(path, value);
            return new BizResult<bool>(true);
        }


        public async Task<BizResult<bool>> DeleteNode(string path)
        {
            await _zookeeperClient.DeleteAsync(path);
            return new BizResult<bool>(true);
        }

    }
}
