namespace ZooKeeper.Mgt.Website.Common
{
    public class TreeNode
    {
        public int id { get; set; }

        public int pId { get; set; }

        public string name { get; set; }

        public string bakValue { get; set; }

        public bool isParent { get; set; }

        public bool open { get; set; }
    }
}