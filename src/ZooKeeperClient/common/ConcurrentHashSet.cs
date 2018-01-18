using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZookeeperClient.common
{
    internal class ConcurrentHashSet<T>
    {
        readonly ConcurrentDictionary<T, byte> _dictionary;

        public ConcurrentHashSet()
        {
            _dictionary = new ConcurrentDictionary<T, byte>();
        }

        public IEnumerator<T> GetEnumerator() => _dictionary.Keys.GetEnumerator();

        public bool TryRemove(T item) => _dictionary.TryRemove(item, out byte tempbyte);

        public bool TryAdd(T item) => _dictionary.TryAdd(item, default(byte));

        public ICollection<T> Keys => _dictionary.Keys;

        public int Count => this._dictionary.Count;

        public bool IsEmpty => this._dictionary.IsEmpty;

        public bool Contains(T item) => _dictionary.ContainsKey(item);

        public void Clear() => _dictionary.Clear();
    }
}
