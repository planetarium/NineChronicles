using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class TradableMaterial : Material, ITradableFungibleItem
    {
        public Guid TradableId { get; }

        public long RequiredBlockIndex
        {
            get => _requiredBlockIndex;
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(
                        $"{nameof(RequiredBlockIndex)} must be greater than 0, but {value}");
                }
                _requiredBlockIndex = value;
            }
        }

        private long _requiredBlockIndex;

        public static Guid DeriveTradableId(HashDigest<SHA256> fungibleId) =>
            new Guid(HashDigest<MD5>.DeriveFrom(fungibleId.ToByteArray()).ToByteArray());

        public TradableMaterial(MaterialItemSheet.Row data) : base(data)
        {
            TradableId = DeriveTradableId(ItemId);
        }

        public TradableMaterial(Dictionary serialized) : base(serialized)
        {
            RequiredBlockIndex = serialized.ContainsKey(RequiredBlockIndexKey)
                ? serialized[RequiredBlockIndexKey].ToLong()
                : default;

            TradableId = DeriveTradableId(ItemId);
        }

        protected TradableMaterial(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        protected bool Equals(TradableMaterial other)
        {
            return base.Equals(other) && RequiredBlockIndex == other.RequiredBlockIndex && TradableId.Equals(other.TradableId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TradableMaterial) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ RequiredBlockIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ TradableId.GetHashCode();
                return hashCode;
            }
        }

        public override IValue Serialize() => ((Dictionary) base.Serialize())
            .SetItem(RequiredBlockIndexKey, RequiredBlockIndex.Serialize());

        public override string ToString()
        {
            return base.ToString() +
                   $", {nameof(TradableId)}: {TradableId}" +
                   $", {nameof(RequiredBlockIndex)}: {RequiredBlockIndex}";
        }

        public object Clone()
        {
            return MemberwiseClone();
        }
    }
}
