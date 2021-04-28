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
    public class Material : ItemBase, ISerializable, IFungibleItem, ITradableItem
    {
        public HashDigest<SHA256> ItemId { get; }

        public HashDigest<SHA256> FungibleId => ItemId;
        
        public Guid TradeId { get; }
        
        public bool IsTradable { get; }

        public Material(MaterialItemSheet.Row data, bool isTradable = default) : base(data)
        {
            ItemId = data.ItemId;
            TradeId = new Guid(HashDigest<MD5>.DeriveFrom(ItemId.ToByteArray()).ToByteArray());
            IsTradable = isTradable;
        }

        public Material(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "item_id", out var itemId))
            {
                ItemId = itemId.ToItemId();
                TradeId = new Guid(HashDigest<MD5>.DeriveFrom(ItemId.ToByteArray()).ToByteArray());
            }

            IsTradable = serialized.ContainsKey("is_tradable") ? serialized["is_tradable"].ToBoolean() : default;
        }

        protected Material(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        protected bool Equals(Material other)
        {
            return base.Equals(other) &&
                   ItemId.Equals(other.ItemId) &&
                   IsTradable.Equals(other.IsTradable);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Material) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ ItemId.GetHashCode();
                hashCode = (hashCode * 397) ^ IsTradable.GetHashCode();
                return hashCode;
            }
        }

        public override IValue Serialize()
        {
            var result = ((Dictionary) base.Serialize())
                .SetItem("item_id", ItemId.Serialize());
            
            if (IsTradable)
            {
                result = result.SetItem("is_tradable", IsTradable.Serialize());
            }

            return result;
        }

        public override string ToString()
        {
            return base.ToString() +
                   $", {nameof(ItemId)}: {ItemId}" +
                   $", {nameof(TradeId)}: {TradeId}" +
                   $", {nameof(IsTradable)}: {IsTradable}";
        }
    }
}
