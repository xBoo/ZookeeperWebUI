namespace ZooKeeper.Mgt.Website.Common
{
    public class ZNode
    {
        public string Key { get; set; }
        public string Value { get; set; }
        public bool IsEncryptDisplay { get; set; } = false;
        public string Description { get; set; }
    }
}