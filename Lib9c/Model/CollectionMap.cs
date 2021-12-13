using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.State;

namespace Nekoyume.Model
{
    [Serializable]
    public class CollectionMap : IState, IDictionary<int, int>, ISerializable
    {
        private Dictionary<int, int> _dictionary;
        private Dictionary _serialized;

        public CollectionMap()
        {
            _dictionary = new Dictionary<int, int>();
        }

        public CollectionMap(Bencodex.Types.Dictionary serialized)
        {
            _serialized = serialized;
        }

        private CollectionMap(SerializationInfo info, StreamingContext context)
        {
            _dictionary = (Dictionary<int, int>)info.GetValue(
                nameof(Dictionary),
                typeof(Dictionary<int, int>)
            );
        }

        private Dictionary<int, int> Dictionary
        {
            get
            {
                if (_serialized is Dictionary d)
                {
                    _dictionary = d.ToDictionary(
                        kv => kv.Key.ToInteger(),
                        kv => kv.Value.ToInteger()
                    );
                    _serialized = null;
                    return _dictionary;
                }

                return _dictionary;
            }

            set
            {
                _dictionary = value;
                _serialized = null;
            }
        }

        [SuppressMessage(
            "Libplanet.Analyzers.ActionAnalyzer",
            "LAA1002",
            Justification = "It's serialzed into a dictionary in the end.")]
        public IValue Serialize() => _serialized ?? new Dictionary(
            Dictionary.Select(kv =>
                new KeyValuePair<IKey, IValue>(
                    (Text) kv.Key.ToString(CultureInfo.InvariantCulture),
                    (Text) kv.Value.ToString(CultureInfo.InvariantCulture)
                )
            )
        );

        public void Add(KeyValuePair<int, int> item)
        {
            if (Dictionary.ContainsKey(item.Key))
            {
                Dictionary[item.Key] += item.Value;
            }
            else
            {
                Dictionary[item.Key] = item.Value;
            }
        }

        public void Clear()
        {
            Dictionary.Clear();
        }

        public bool Contains(KeyValuePair<int, int> item)
        {
#pragma warning disable LAA1002
            return Dictionary.Contains(item);
#pragma warning restore LAA1002
        }

        public void CopyTo(KeyValuePair<int, int>[] array, int arrayIndex)
        {
            throw new System.NotImplementedException();
        }

        public bool Remove(KeyValuePair<int, int> item)
        {
            return Dictionary.Remove(item.Key);
        }

        public int Count => Dictionary.Count;
        public bool IsReadOnly => false;

        public IEnumerator<KeyValuePair<int, int>> GetEnumerator()
        {
            return Dictionary.OrderBy(kv => kv.Key).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(int key, int value)
        {
            Add(new KeyValuePair<int, int>(key, value));
        }

        public bool ContainsKey(int key)
        {
            return Dictionary.ContainsKey(key);
        }

        public bool Remove(int key)
        {
            return Dictionary.Remove(key);
        }

        public bool TryGetValue(int key, out int value)
        {
            return Dictionary.TryGetValue(key, out value);
        }

        public int this[int key]
        {
            get => Dictionary[key];
            set => Dictionary[key] = value;
        }

        public ICollection<int> Keys => Dictionary.Keys;
        public ICollection<int> Values => Dictionary.Values;

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Dictionary), Dictionary);
        }
    }
}
