using System;
using System.Globalization;
using UnityEngine;

namespace Nekoyume.JsonConvertibles
{
    [Serializable]
    public class JsonConvertibleDecimal : ISerializationCallbackReceiver
    {
        [NonSerialized] public decimal Value;
        
        [SerializeField] private string json;

        public static JsonConvertibleDecimal operator + (JsonConvertibleDecimal left, JsonConvertibleDecimal right) =>
            new JsonConvertibleDecimal(left.Value + right.Value);
        
        public static JsonConvertibleDecimal operator + (JsonConvertibleDecimal left, decimal right) =>
            new JsonConvertibleDecimal(left.Value + right);
        
        public static JsonConvertibleDecimal operator - (JsonConvertibleDecimal left, JsonConvertibleDecimal right) =>
            new JsonConvertibleDecimal(left.Value - right.Value);
        
        public static JsonConvertibleDecimal operator - (JsonConvertibleDecimal left, decimal right) =>
            new JsonConvertibleDecimal(left.Value - right);
        
        public static JsonConvertibleDecimal operator * (JsonConvertibleDecimal left, JsonConvertibleDecimal right) =>
            new JsonConvertibleDecimal(left.Value * right.Value);
        
        public static JsonConvertibleDecimal operator * (JsonConvertibleDecimal left, decimal right) =>
            new JsonConvertibleDecimal(left.Value * right);
        
        public static JsonConvertibleDecimal operator / (JsonConvertibleDecimal left, JsonConvertibleDecimal right) =>
            new JsonConvertibleDecimal(left.Value / right.Value);
        
        public static JsonConvertibleDecimal operator / (JsonConvertibleDecimal left, decimal right) =>
            new JsonConvertibleDecimal(left.Value / right);

        public static bool operator ==(JsonConvertibleDecimal left, JsonConvertibleDecimal right)
        {
            if (left is null ||
                right is null)
                return false;

            return left.Value == right.Value;
        }
        
        public static bool operator == (JsonConvertibleDecimal left, decimal right)
        {
            if (left is null)
                return false;

            return left.Value == right;
        }
        
        public static bool operator != (JsonConvertibleDecimal left, JsonConvertibleDecimal right)
        {
            if (left is null ||
                right is null)
                return false;

            return left.Value != right.Value;
        }
        
        public static bool operator != (JsonConvertibleDecimal left, decimal right)
        {
            if (left is null)
                return false;

            return left.Value != right;
        }

        public JsonConvertibleDecimal(decimal value)
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
            Value = decimal.Parse(json);
            json = null;
        }

        protected bool Equals(JsonConvertibleDecimal other)
        {
            return Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((JsonConvertibleDecimal) obj);
        }

        public override int GetHashCode()
        {
            return Value.GetHashCode();
        }
    }
}
