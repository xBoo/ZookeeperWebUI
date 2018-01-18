using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Newtonsoft.Json.Linq;
using ZooKeeper.Mgt.Website.Common;
using ZookeeperClient;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace Config.Keede.Website.Controllers
{
    public class ConfigController : Controller
    {
        readonly IZooKeeperClient _zookeeperClient;
        readonly IConfiguration _configuration;
        readonly IHostingEnvironment _env;

        public ConfigController(IZooKeeperClient zookeeperClient, ILogger<ConfigController> logger, IConfiguration configuration, IHostingEnvironment env)
        {
            _zookeeperClient = zookeeperClient;
            _configuration = configuration;
            _env = env;
        }

        public IActionResult Index()
        {
            return View();
        }

        public async Task<BizResult<List<TreeNode>>> GetTreeNodes(string path, int? parentId)
        {
            if (string.IsNullOrEmpty(path)) path = "/";

            if (path != "/")
            {
                var nodes = await GetChildrenTreeNodesAsync(path, parentId, false);
                return new BizResult<List<TreeNode>>(nodes);
            }
            else
            {
                var returnNodes = new List<TreeNode>();
                var rootNode = (await GetChildrenTreeNodesAsync(path, parentId, true)).FirstOrDefault(w => w.name == _configuration["RootNodeName"]);

                if (rootNode == null) return new BizResult<List<TreeNode>>(null, -1, "未找到配置对应的根节点！");

                returnNodes.Add(rootNode);
                var children = await GetChildrenTreeNodesAsync(rootNode.bakValue, rootNode.id, false);
                returnNodes.AddRange(children.OrderBy(o => o.isParent));
                return new BizResult<List<TreeNode>>(returnNodes);
            }
        }

        private async Task<List<TreeNode>> GetChildrenTreeNodesAsync(string path, int? parentId, bool isOpen)
        {
            List<TreeNode> nodeList = new List<TreeNode>();
            var nodes = (await _zookeeperClient.GetChildrenAsync(path)).Where(w => w.StartsWith("$$")).ToList();
            for (int i = 0; i < nodes.Count; i++)
            {
                var idx = i + 1;
                nodeList.Add(new TreeNode
                {
                    id = parentId.HasValue ? int.Parse(parentId.ToString() + idx.ToString()) : idx,
                    pId = parentId ?? 0,
                    name = nodes[i].TrimStart(new char[] { '$', '$' }),
                    bakValue = path == "/" ? "/" + nodes[i] : path + "/" + nodes[i],
                    open = isOpen,
                    isParent = nodes[i].StartsWith("$$")
                });
            }

            return nodeList.OrderBy(o => o.name).ToList();
        }

        public async Task<BizResult<List<ConfigNode>>> GetChildNodes(string path)
        {
            var confNodes = new List<ConfigNode>();
            var strs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);

            var p = string.Empty;
            foreach (string str in strs)
            {
                p += "/" + str;
                var conf = new ConfigNode { Path = p };
                var nodeNames = await this._zookeeperClient.GetChildrenAsync(p);
                foreach (var name in nodeNames)
                {
                    if (name.StartsWith("$$")) continue;

                    var childPath = p == "/" ? p + "name" : p + "/" + name;
                    var znode = await this._zookeeperClient.GetDataAsync<ZNode>(childPath);
                    if (znode.IsEncryptDisplay) znode.Value = "***敏感字段***";
                    conf.Nodes.Add(znode);
                }

                conf.Nodes = conf.Nodes.OrderBy(o => o.Key).ToList();
                confNodes.Add(conf);
            }

            return new BizResult<List<ConfigNode>>(confNodes);
        }

        public async Task<BizResult<ZNode>> GetNode(string path)
        {
            if (!await this._zookeeperClient.ExistsAsync(path)) return new BizResult<ZNode>(null, -1, $"Path：'{path}'节点不存在!");
            var znode = await this._zookeeperClient.GetDataAsync<ZNode>(path);
            if (znode.IsEncryptDisplay) znode.Value = "***敏感字段***";
            return new BizResult<ZNode>(znode);
        }

        [HttpPost]
        public async Task<BizResult<bool>> CreateZNode(string path, bool isParent, ZNode node)
        {

            if (node.Key.IndexOf('/') > 0 || node.Key.Contains("$$")) return new BizResult<bool>(false, -1, "Key不能包含 '/' 或者 '$$' 符号！");

            node.Key = node.Key.ToLower();
            var paths = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (paths.Count() > 1)
            {
                var existParentPath = string.Empty;
                foreach (var p in paths.Take(paths.Count() - 1))
                {
                    existParentPath += "/" + p;
                    if (await _zookeeperClient.ExistsAsync($"{existParentPath}/{node.Key}")) return new BizResult<bool>(false, -1, $"路径 {existParentPath} 下已存在相同名称的节点！");
                }
            }

            var zpath = path + "/" + (isParent ? "$$" + node.Key : node.Key);
            if (await _zookeeperClient.ExistsAsync(zpath)) return new BizResult<bool>(false, -1, $"Path：'{zpath.Replace("$$", "")}'节点已存在!");

            await _zookeeperClient.CreatePersistentAsync(zpath, node);
            return new BizResult<bool>(true);
        }

        public async Task<BizResult<bool>> DeleteZNode(string path)
        {
            if (!await _zookeeperClient.ExistsAsync(path)) return new BizResult<bool>(false, -1, "删除的节点不存在！");

            await _zookeeperClient.DeleteAsync(path);
            return new BizResult<bool>(true);
        }

        public async Task<BizResult<bool>> DeleteRecursiveZNode(string path)
        {
            if (await _zookeeperClient.ExistsAsync(path))
            {
                await _zookeeperClient.DeleteRecursiveAsync(path);
                return new BizResult<bool>(true);
            }
            else
                return new BizResult<bool>(false, -1, "删除的节点不存在！");
        }

        [HttpPost]
        public async Task<BizResult<bool>> UpdateZNode(string path, ZNode node)
        {
            string[] strs = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (strs.LastOrDefault() != node.Key) return new BizResult<bool>(false, -1, "非法操作！");

            await _zookeeperClient.SetDataAsync(path, node);
            return new BizResult<bool>(true);
        }

        public async Task<FileStreamResult> Export(string path)
        {
            var biz = await GetChildNodes(path);

            var exportNodes = new List<object>();
            foreach (var configNode in biz.ReturnObj)
            {
                foreach (var node in configNode.Nodes)
                {
                    exportNodes.Add(new { Key = node.Key, Value = node.Value, Description = node.Description, ZPath = $"{configNode.Path}/{node.Key}" });
                }
            }

            string content = "/**********************************************************************/\r\n"
                             + "/*以上内容请勿擅自修改，否则可能导致配置文件无法读取\r\n"
                             + "/**********************************************************************/\r\n";
            content = exportNodes.Aggregate(content, (current, p) => current + JsonConvert.SerializeObject(p) + "\r\n");

            var bytes = Encoding.UTF8.GetBytes(content);
            var stream = new MemoryStream(bytes);

            string fileName = $"config_{DateTime.Now:yyyyMMddHHmmssfff}.txt";
            return File(stream, "text/plain", fileName);
        }

        public async Task<BizResult<bool>> Import(string path)
        {
            var file = Request.Form.Files[0];
            if (file == null || file.Length == 0) return new BizResult<bool>(false, -1, "文件上传失败或文件内容为空");
            if (!file.FileName.EndsWith(".txt")) return new BizResult<bool>(false, -1, "文件类型错误");

            string baseFile = Path.Combine(_env.WebRootPath, "upload");
            if (!Directory.Exists(baseFile)) Directory.CreateDirectory(baseFile);

            string fileName = ($"appsetting_{Thread.CurrentThread.ManagedThreadId}_{DateTime.Now:yyyyMMddHHmmssfff}.txt");
            var savePath = Path.Combine(baseFile, fileName);

            using (var stream = new FileStream(savePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            return await ImportNodesAsync(path, savePath);
        }

        private async Task<BizResult<bool>> ImportNodesAsync(string parentNodePath, string filePath)
        {
            var lines = await System.IO.File.ReadAllLinesAsync(filePath);
            List<JObject> nodes = (from line in lines where !string.IsNullOrWhiteSpace(line) where !line.StartsWith("/*") select JsonConvert.DeserializeObject<object>(line)).Cast<JObject>().ToList();

            foreach (var node in nodes)
            {
                var zpath = node["ZPath"].ToString();
                var k = zpath.Substring(zpath.LastIndexOf('/') + 1).ToLower();
                var p = zpath.Substring(0, zpath.LastIndexOf('/'));

                if (p != parentNodePath)
                    return new BizResult<bool>(false, -1, $"检查出节点：{node} ZPath不在父节点{parentNodePath}之下，请修改后重试！");

                if (node["Key"].ToString().ToLower() != k)
                    return new BizResult<bool>(false, -1, $"检查出节点：{node} 中Key与ZPath中配置Key不匹配，请修改后重试！");
            }

            var childNodes = await this._zookeeperClient.GetChildrenAsync(parentNodePath);
            foreach (var childNode in childNodes)
            {
                await _zookeeperClient.DeleteAsync($"{parentNodePath}/{childNode}");
            }

            foreach (var node in nodes)
            {
                await _zookeeperClient.CreatePersistentAsync(node["ZPath"].ToString(), new { Key = node["Key"].ToString().ToLower(), Value = node["Value"], Description = node["Description"] });
            }

            return new BizResult<bool>(true);
        }
    }
}