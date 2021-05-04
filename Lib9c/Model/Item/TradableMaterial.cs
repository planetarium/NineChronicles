using System;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using Bencodex.Types;
using Libplanet;
using Nekoyume.Model.State;
using Nekoyume.TableData;

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

        public static Guid DeriveTradableId(HashDigest<SHA256> hashDigest) =>
            new Guid(HashDigest<MD5>.DeriveFrom(hashDigest.ToByteArray()).ToByteArray());

        public TradableMaterial(MaterialItemSheet.Row data) : base(data)
        {
            TradableId = DeriveTradableId(ItemId);
        }

        public TradableMaterial(Dictionary serialized) : base(serialized)
        {
            RequiredBlockIndex = serialized.ContainsKey("required_block_index")
                ? serialized["required_block_index"].ToLong()
                : default;

            TradableId = DeriveTradableId(ItemId);
        }

        protected TradableMaterial(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        protected bool Equals(TradableMaterial other)
        {
            return base.Equals(other) && TradableId.Equals(other.TradableId);
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
                return (base.GetHashCode() * 397) ^ TradableId.GetHashCode();
            }
        }

        public override IValue Serialize()
        {
            var result = ((Dictionary) base.Serialize())
                .SetItem("required_block_index", RequiredBlockIndex.Serialize());

            return result;
        }

        public override string ToString()
        {
            return base.ToString() +
                   $", {nameof(TradableId)}: {TradableId}" +
                   $", {nameof(RequiredBlockIndex)}: {RequiredBlockIndex}";
        }
    }
}
