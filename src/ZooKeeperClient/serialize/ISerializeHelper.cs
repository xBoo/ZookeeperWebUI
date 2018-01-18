namespace ZookeeperClient.serialize
{
    /// <summary>
    /// default serialize use json serialize
    /// </summary>
    internal interface ISerializeHelper
    {
        /// <summary>
        /// first obj serialize to json, and then use default utf8 encoding to convert to byte[]
        /// serialize object  need add attribute [DataContract] for class and [DataMember] for property
        /// </summary>
        byte[] Serialize(object obj);

        /// <summary>
        /// first using default utf8 encoding to convert data to json string, and then deserialize to object
        /// </summary>
        T Deserialize<T>(byte[] data);
    }
}