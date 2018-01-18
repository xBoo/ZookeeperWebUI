using org.apache.zookeeper.data;

namespace ZookeeperClient.model
{
    public class ZKData<T>
    {
        public T Data { set; get; }

        public Stat Stat { set; get; }
    }
}
