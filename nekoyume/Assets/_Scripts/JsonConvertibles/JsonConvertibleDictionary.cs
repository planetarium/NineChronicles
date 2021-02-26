using System;
using System.Collections.Generic;
using UnityEngine;

namespace Nekoyume.JsonConvertibles
{
    /// <summary>
    /// 유니티에서 제공하는 `JsonUtility`를 사용하기 위해서는 이 클래스를 상속하는 Non Generic 클래스를 선언해서 사용해야 한다.
    /// </summary>
    /// <typeparam name="TKey"></typeparam>
    /// <typeparam name="TValue"></typeparam>
    [Serializable]
    public class JsonConvertibleDictionary<TKey, TValue> : ISerializationCallbackReceiver
    {
        [SerializeField]
        private List<TKey> keys;

        [SerializeField]
        private List<TValue> values;

        public int Count => Value.Count;

        [field: NonSerialized]
        public Dictionary<TKey, TValue> Value { get; }

        public JsonConvertibleDictionary(params (TKey key, TValue value)[] tuples)
        {
            Value = new Dictionary<TKey, TValue>(tuples.Length);
            foreach (var (key, value) in tuples)
            {
                Value.Add(key, value);
            }

            keys = null;
            values = null;
        }

        public JsonConvertibleDictionary(Dictionary<TKey, TValue> value)
        {
            Value = value;
            keys = null;
            values = null;
        }

        public void OnBeforeSerialize()
        {
            keys = new List<TKey>(Value.Keys);
            values = new List<TValue>(Value.Values);
        }

        public void OnAfterDeserialize()
        {
            var count = Math.Min(keys.Count, values.Count);
            for (var i = 0; i < count; i++)
            {
                Value.Add(keys[i], values[i]);
            }

            keys = null;
            values = null;
        }
    }
}
