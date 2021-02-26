using System;
using System.Security.Cryptography;
using Libplanet;
using UnityEngine;

namespace Nekoyume.JsonConvertibles
{
    /// <summary>
    /// 유니티에서 제공하는 `JsonUtility`를 사용하기 위해서는 이 클래스를 상속하는 Non Generic 클래스를 선언해서 사용해야 한다.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public class JsonConvertibleHashDigest<T> : ISerializationCallbackReceiver where T : HashAlgorithm
    {
        [NonSerialized] public HashDigest<T> Value;

        [SerializeField] private string json;

        public JsonConvertibleHashDigest(HashDigest<T> hashDigest)
        {
            Value = hashDigest;
            json = null;
        }
        
        public void OnBeforeSerialize()
        {
            json = Value.ToString();
        }

        public void OnAfterDeserialize()
        {
            Value = HashDigest<T>.FromString(json);
        }

        protected bool Equals(JsonConvertibleHashDigest<T> other)
        {
            return Value.Equals(other.Value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsonConvertibleHashDigest<T>) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
