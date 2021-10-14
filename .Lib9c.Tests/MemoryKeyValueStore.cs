namespace Lib9c.Tests
{
    using System.Collections.Generic;
    using Libplanet.Store.Trie;

    public class MemoryKeyValueStore : IKeyValueStore
    {
        public MemoryKeyValueStore()
            : this(new Dictionary<byte[], byte[]>())
        {
        }

        public MemoryKeyValueStore(Dictionary<byte[], byte[]> dictionary)
        {
            Dictionary = new Dictionary<byte[], byte[]>(dictionary, new BytesEqualityComparer());
        }

        private Dictionary<byte[], byte[]> Dictionary { get; }

        public byte[] Get(byte[] key)
        {
            return Dictionary[key];
        }

        public void Set(byte[] key, byte[] value)
        {
            Dictionary[key] = value;
        }

        public void Delete(byte[] key)
        {
            Dictionary.Remove(key);
        }

        public bool Exists(byte[] key)
        {
            return Dictionary.ContainsKey(key);
        }

        public void Dispose()
        {
        }

        public IEnumerable<byte[]> ListKeys() => Dictionary.Keys;

        private class BytesEqualityComparer : IEqualityComparer<byte[]>
        {
            public bool Equals(byte[] x, byte[] y)
            {
                if (!(x is byte[] xb && y is byte[] yb && xb.LongLength == yb.LongLength))
                {
                    return false;
                }

                for (int i = 0; i < xb.LongLength; i++)
                {
                    if (xb[i] != yb[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            public int GetHashCode(byte[] obj)
            {
                int hash = 17;
                unchecked
                {
                    foreach (byte b in obj)
                    {
                        hash *= 31 + b;
                    }
                }

                return hash;
            }
        }
    }
}
