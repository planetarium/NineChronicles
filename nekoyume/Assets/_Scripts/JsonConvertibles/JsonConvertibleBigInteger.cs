using System;
using System.Globalization;
using System.Numerics;
using UnityEngine;

namespace Nekoyume.JsonConvertibles
{
    [Serializable]
    public class JsonConvertibleBigInteger : ISerializationCallbackReceiver
    {
        [NonSerialized]
        public BigInteger Value;

        [SerializeField]
        private string json;

        public static JsonConvertibleBigInteger operator +(JsonConvertibleBigInteger left, JsonConvertibleBigInteger right)
        {
            return new JsonConvertibleBigInteger(left.Value + right.Value);
        }

        public static JsonConvertibleBigInteger operator +(JsonConvertibleBigInteger left, BigInteger right)
        {
            return new JsonConvertibleBigInteger(left.Value + right);
        }

        public static JsonConvertibleBigInteger operator -(JsonConvertibleBigInteger left, JsonConvertibleBigInteger right)
        {
            return new JsonConvertibleBigInteger(left.Value - right.Value);
        }

        public static JsonConvertibleBigInteger operator -(JsonConvertibleBigInteger left, BigInteger right)
        {
            return new JsonConvertibleBigInteger(left.Value - right);
        }

        public static JsonConvertibleBigInteger operator *(JsonConvertibleBigInteger left, JsonConvertibleBigInteger right)
        {
            return new JsonConvertibleBigInteger(left.Value * right.Value);
        }

        public static JsonConvertibleBigInteger operator *(JsonConvertibleBigInteger left, BigInteger right)
        {
            return new JsonConvertibleBigInteger(left.Value * right);
        }

        public static JsonConvertibleBigInteger operator /(JsonConvertibleBigInteger left,
            JsonConvertibleBigInteger right)
        {
            return new JsonConvertibleBigInteger(left.Value / right.Value);
        }

        public static JsonConvertibleBigInteger operator /(JsonConvertibleBigInteger left, BigInteger right)
        {
            return new JsonConvertibleBigInteger(left.Value / right);
        }

        public static bool operator ==(JsonConvertibleBigInteger left, JsonConvertibleBigInteger right)
        {
            if (left is null ||
                right is null)
                return false;

            return left.Value == right.Value;
        }

        public static bool operator ==(JsonConvertibleBigInteger left, BigInteger right)
        {
            if (left is null)
                return false;

            return left.Value == right;
        }

        public static bool operator !=(JsonConvertibleBigInteger left, JsonConvertibleBigInteger right)
        {
            if (left is null ||
                right is null)
                return false;

            return left.Value != right.Value;
        }

        public static bool operator !=(JsonConvertibleBigInteger left, BigInteger right)
        {
            if (left is null)
                return false;

            return left.Value != right;
        }

        public JsonConvertibleBigInteger(BigInteger value)
        {
            Value = value;
            json = null;
        }

        public void OnBeforeSerialize()
        {
            json = Value.ToString(CultureInfo.InvariantCulture);
        }

        public void OnAfterDeserialize()
        {
            Value = BigInteger.Parse(json);
            json = null;
        }

        protected bool Equals(JsonConvertibleBigInteger other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((JsonConvertibleBigInteger) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
