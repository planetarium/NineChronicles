namespace Lib9c.Tests
{
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Collections.Immutable;
    using Libplanet.Store.Trie;

    public class MemoryKeyValueStore : IKeyValueStore
    {
        public MemoryKeyValueStore()
            : this(new ConcurrentDictionary<KeyBytes, byte[]>())
        {
        }

        public MemoryKeyValueStore(ConcurrentDictionary<KeyBytes, byte[]> dictionary)
        {
            Dictionary = new ConcurrentDictionary<KeyBytes, byte[]>(dictionary);
        }

        private ConcurrentDictionary<KeyBytes, byte[]> Dictionary { get; }

        public byte[] Get(KeyBytes key)
        {
            return Dictionary[key];
        }

        public void Set(KeyBytes key, byte[] value)
        {
            Dictionary[key] = value;
        }

        public void Set(IDictionary<KeyBytes, byte[]> values)
        {
            foreach (KeyValuePair<KeyBytes, byte[]> kv in values)
            {
                Dictionary[kv.Key] = kv.Value;
            }
        }

        public void Delete(KeyBytes key)
        {
            Dictionary.Remove(key, out _);
        }

        public bool Exists(KeyBytes key)
        {
            return Dictionary.ContainsKey(key);
        }

        public void Dispose()
        {
        }

        public byte[] Get(in KeyBytes key) => Dictionary[key];

        public IReadOnlyDictionary<KeyBytes, byte[]> Get(IEnumerable<KeyBytes> keys)
        {
            var dictBuilder = ImmutableDictionary.CreateBuilder<KeyBytes, byte[]>();
            foreach (KeyBytes key in keys)
            {
                if (Dictionary.TryGetValue(key, out var value) && value is { } v)
                {
                    dictBuilder[key] = v;
                }
            }

            return dictBuilder.ToImmutable();
        }

        public void Set(in KeyBytes key, byte[] value) =>
            Dictionary[key] = value;

        public void Delete(in KeyBytes key) =>
            Dictionary.TryRemove(key, out _);

        public void Delete(IEnumerable<KeyBytes> keys)
        {
            foreach (KeyBytes key in keys)
            {
                Dictionary.TryRemove(key, out _);
            }
        }

        public bool Exists(in KeyBytes key) =>
            Dictionary.ContainsKey(key);

        IEnumerable<KeyBytes> IKeyValueStore.ListKeys() =>
            (IEnumerable<KeyBytes>)Dictionary.Keys;

        public IEnumerable<KeyBytes> ListKeys() => Dictionary.Keys;

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
