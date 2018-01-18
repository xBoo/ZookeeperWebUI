using System.Collections.Generic;
using org.apache.zookeeper;
using org.apache.zookeeper.data;

namespace ZookeeperClient.model
{
    public class Node
    {
        public Node(string path, object data, List<ACL> acLs, CreateMode createMode)
        {
            Path = path;
            Data = data;
            CreateMode = createMode;
            ACLs = acLs;
        }

        public Node(string path, object data, CreateMode createMode) : this(path, data, null, createMode)
        { }



        public string Path { get; private set; }

        public object Data { get; private set; }

        public CreateMode CreateMode { get; private set; }

        public List<ACL> ACLs { get; private set; }
    }
}
