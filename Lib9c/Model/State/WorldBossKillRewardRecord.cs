using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Bencodex.Types;
using Libplanet.Assets;

namespace Nekoyume.Model.State
{
    public class WorldBossKillRewardRecord : IDictionary<int, List<FungibleAssetValue>>, IState
    {
        private Dictionary<int, List<FungibleAssetValue>> _dict = new Dictionary<int, List<FungibleAssetValue>>();

        public WorldBossKillRewardRecord(List serialized)
        {
            foreach (var iValue in serialized)
            {
                var list = (List) iValue;
                var key = list[0].ToInteger();
                var value = list[1].ToList(StateExtensions.ToFungibleAssetValue);
                _dict[key] = value;
            }
        }

        public WorldBossKillRewardRecord()
        {
        }

        public bool IsClaimable()
        {
            return _dict.Values.Any(i => !i.Any());
        }

        public IEnumerator<KeyValuePair<int, List<FungibleAssetValue>>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        public void Add(KeyValuePair<int, List<FungibleAssetValue>> item)
        {
            throw new System.NotImplementedException();
        }

        public void Clear()
        {
            _dict.Clear();
        }

        public bool Contains(KeyValuePair<int, List<FungibleAssetValue>> item)
        {
            return ((IDictionary<int, List<FungibleAssetValue>>)_dict).Contains(item);
        }

        public void CopyTo(KeyValuePair<int, List<FungibleAssetValue>>[] array, int arrayIndex)
        {
            ((IDictionary<int, List<FungibleAssetValue>>)_dict).CopyTo(array, arrayIndex);
        }

        public bool Remove(KeyValuePair<int, List<FungibleAssetValue>> item)
        {
            return ((IDictionary<int, List<FungibleAssetValue>>)_dict).Remove(item);
        }

        public int Count => _dict.Count;
        public bool IsReadOnly => false;
        public void Add(int key, List<FungibleAssetValue> value)
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

        public bool TryGetValue(int key, out List<FungibleAssetValue> value)
        {
            return _dict.TryGetValue(key, out value);
        }

        public List<FungibleAssetValue> this[int key]
        {
            get => _dict[key];
            set => _dict[key] = value;
        }

        public ICollection<int> Keys => _dict.Keys;
        public ICollection<List<FungibleAssetValue>> Values => _dict.Values;
        public IValue Serialize()
        {
            return _dict.OrderBy(kv => kv.Key)
                .Aggregate(List.Empty,
                    (current, kv) =>
                        current.Add(
                            List.Empty
                                .Add(kv.Key.Serialize())
                                .Add(kv.Value.Aggregate(
                                    List.Empty,
                                    (cur, fav) =>
                                        cur.Add(fav.Serialize())
                                    )
                                )
                        )
                );
        }
    }
}
