using System;
using UnityEngine;

namespace Nekoyume.JsonConvertibles
{
    [Serializable]
    public class JsonConvertibleGuid : ISerializationCallbackReceiver
    {
        [NonSerialized]
        private Guid _value;

        [SerializeField]
        private string json;

        public Guid Value => _value;

        public JsonConvertibleGuid(Guid value)
        {
            _value = value;
            json = null;
        }

        public void OnBeforeSerialize()
        {
            json = _value.ToString();
        }

        public void OnAfterDeserialize()
        {
            _value = Guid.Parse(json);
            json = null;
        }

        protected bool Equals(JsonConvertibleGuid other)
        {
            return _value.Equals(other._value);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsonConvertibleGuid) obj);
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }
    }
}
