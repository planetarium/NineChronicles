using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using Bencodex.Types;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public class Costume : ItemBase, INonFungibleItem, IEquippableItem
    {
        public const string RequiredBlockIndexKey = "rbi";
        // FIXME: Do not use anymore please!
        public bool equipped = false;
        public string SpineResourcePath { get; }

        public Guid ItemId { get; }

        public long RequiredBlockIndex
        {
            get => _requiredBlockIndex;
            private set
            {
                if (value <= RequiredBlockIndex)
                {
                    throw new ArgumentOutOfRangeException(
                        $"{nameof(RequiredBlockIndex)} must be greater than {RequiredBlockIndex}, but {value}");
                }
                _requiredBlockIndex = value;
            }
        }

        public bool Equipped => equipped;

        private long _requiredBlockIndex;

        public Costume(CostumeItemSheet.Row data, Guid itemId) : base(data)
        {
            SpineResourcePath = data.SpineResourcePath;
            ItemId = itemId;
        }

        public Costume(Dictionary serialized) : base(serialized)
        {
            if (serialized.TryGetValue((Text) "equipped", out var toEquipped))
            {
                equipped = toEquipped.ToBoolean();
            }
            if (serialized.TryGetValue((Text) "spine_resource_path", out var spineResourcePath))
            {
                SpineResourcePath = (Text) spineResourcePath;
            }

            ItemId = serialized["item_id"].ToGuid();

            if (serialized.ContainsKey(RequiredBlockIndexKey))
            {
                RequiredBlockIndex = serialized[RequiredBlockIndexKey].ToLong();
            }
        }
        
        protected Costume(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public override IValue Serialize()
        {
#pragma warning disable LAA1002
            var innerDictionary = new Dictionary<IKey, IValue>
            {
                [(Text) "equipped"] = equipped.Serialize(),
                [(Text) "spine_resource_path"] = SpineResourcePath.Serialize(),
                [(Text) "item_id"] = ItemId.Serialize()
            };
            if (RequiredBlockIndex > 0)
            {
                innerDictionary.Add((Text) RequiredBlockIndexKey, RequiredBlockIndex.Serialize());
            }
            return new Dictionary(innerDictionary.Union((Dictionary) base.Serialize()));
        }
#pragma warning restore LAA1002

        protected bool Equals(Costume other)
        {
            return base.Equals(other) && equipped == other.equipped && ItemId.Equals(other.ItemId);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((Costume) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ equipped.GetHashCode();
                hashCode = (hashCode * 397) ^ ItemId.GetHashCode();
                return hashCode;
            }
        }

        public void Equip()
        {
            equipped = true;
        }

        public void Unequip()
        {
            equipped = false;
        }

        public void Lock(long requiredBlockIndex)
        {
            Unequip();
            RequiredBlockIndex = requiredBlockIndex;
        }
    }
}
