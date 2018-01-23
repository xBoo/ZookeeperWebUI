using System;

namespace ZooKeeper.Mgt.Website.Common
{
    public class ZNode
    {
        public string Path { get; set; }
        public int Version { get; set; }
        public DateTime CreateTime { get; set; }
        public DateTime ModifyTime { get; set; }
        public string ACL { get; set; }
        public string Value { get; set; }
    }
}