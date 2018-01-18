using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ZooKeeper.Mgt.Website.Models;
using ZookeeperClient;
using ZooKeeper.Mgt.Website.Common;

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
            if (string.IsNullOrEmpty(path))
            {
                path = "/";
                var nodes = await GetChildrenNodesAsync(path, parentId, false);
                return new BizResult<List<TreeNode>>(nodes);
            }
            else
            {
                return new BizResult<List<TreeNode>>(null);
            }
        }

        private async Task<List<TreeNode>> GetChildrenNodesAsync(string path, int? parentId, bool isOpen)
        {
            List<TreeNode> nodeList = new List<TreeNode>();
            var nodes = await _zookeeperClient.GetChildrenAsync(path);
            for (int i = 1; i < nodes.Count + 1; i++)
            {

                var nPath = $"{path}/{nodes[i]}";
                var n = await _zookeeperClient.GetZKDataAsync<string>(nPath);

                nodeList.Add(new TreeNode
                {
                    id = parentId.HasValue ? int.Parse(parentId.ToString() + i) : i,
                    pId = parentId ?? 0,
                    name = nodes[i],
                    bakValue = nPath,
                    open = isOpen,
                    isParent = n.Stat.getNumChildren() > 0
                });
            }

            return nodeList.OrderBy(o => o.name).ToList();
        }
    }
}
