using System;
using System.Text;
using Newtonsoft.Json;

namespace ZookeeperClient.serialize
{
    public class SerializeHelper : ISerializeHelper
    {
        public byte[] Serialize(object obj)
        {
            var json = JsonConvert.SerializeObject(obj);
            return Encoding.UTF8.GetBytes(json);
        }

        public T Deserialize<T>(byte[] data)
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(data));
            }
            catch (Exception e)
            {
                throw new InvalidCastException("反序列化错误：" + data.ToString(), e);
            }
        }
    }
}
