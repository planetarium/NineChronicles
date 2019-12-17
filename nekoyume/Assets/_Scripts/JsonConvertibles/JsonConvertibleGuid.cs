using System;
using UnityEngine;

namespace Nekoyume.JsonConvertibles
{
    [Serializable]
    public class JsonConvertibleGuid : ISerializationCallbackReceiver
    {
        [NonSerialized]
        public Guid Value;

        [SerializeField]
        private string json;

        public JsonConvertibleGuid() : this(new Guid())
        {
        }

        public JsonConvertibleGuid(Guid value)
        {
            Value = value;
            json = null;
        }
        
        public void OnBeforeSerialize()
        {
            json = Value.ToString();
        }

        public void OnAfterDeserialize()
        {
            Value = Guid.Parse(json);
            json = null;
        }

        protected bool Equals(JsonConvertibleGuid other)
        {
            return Value.Equals(other.Value);
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
            return Value.GetHashCode();
        }
    }
}
