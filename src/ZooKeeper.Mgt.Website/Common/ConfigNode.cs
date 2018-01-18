using System.Collections.Generic;

namespace ZooKeeper.Mgt.Website.Common
{
    public class ConfigNode
    {
        public ConfigNode()
        {
            Nodes = new List<ZNode>();
        }

        public string Path { get; set; }

        public string ViewPath => Path.Replace("/$$", "/");

        public List<ZNode> Nodes { get; set; }
    }
}