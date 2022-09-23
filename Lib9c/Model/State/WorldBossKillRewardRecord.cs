using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Assets;

namespace Nekoyume.Model.State
{
    public class WorldBossKillRewardRecord : IDictionary<int, bool>, IState
    {
        private Dictionary<int, bool> _dict = new Dictionary<int, bool>();

        public WorldBossKillRewardRecord(List serialized)
        {
            foreach (var iValue in serialized)
            {
                var list = (List) iValue;
                var key = list[0].ToInteger();
                var value = list[1].ToBoolean();
                _dict[key] = value;
            }
        }

        public WorldBossKillRewardRecord()
        {
        }

        public bool IsClaimable(int level)
        {
#pragma warning disable LAA1002
            return  _dict
#pragma warning restore LAA1002
                .Where(kv => kv.Key < level)
                .Any(kv => !kv.Value);
        }

        public IEnumerator<KeyValuePair<int, bool>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public void Add(KeyValuePair<int, bool> item)
        {
            ((IDictionary<int, bool>)_dict).Add(item);
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<int, bool> item)
        {
            return ((IDictionary<int, bool>)_dict).Contains(item);
        }

        public void CopyTo(KeyValuePair<int, bool>[] array, int arrayIndex)
        {
            ((IDictionary<int, bool>)_dict).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<int, bool> item)
        {
            return ((IDictionary<int, bool>)_dict).Remove(item);
        }

        public int Count => _dict.Count;
        public bool IsReadOnly => false;
        public void Add(int key, bool value)
        {
            _dict.Add(key, value);
        }

        public bool ContainsKey(int key)
        {
            return _dict.ContainsKey(key);
        }

        public bool Remove(int key)
        {
            return _dict.Remove(key);
        }

        public bool TryGetValue(int key, out bool value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public bool this[int key]
        {
            get => _dict[key];
            set => _dict[key] = value;
        }

        public ICollection<int> Keys => _dict.Keys;
        public ICollection<bool> Values => _dict.Values;
        public IValue Serialize()
        {
            return _dict.OrderBy(kv => kv.Key)
                .Aggregate(List.Empty,
                    (current, kv) =>
                        current.Add(
                            List.Empty
                                .Add(kv.Key.Serialize())
                                .Add(kv.Value.Serialize())
                        )
                );
        }
    }
}
