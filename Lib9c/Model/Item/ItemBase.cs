using System;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.State;
using Nekoyume.TableData;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public abstract class ItemBase : IItem
    {
        protected static readonly Codec Codec = new Codec();

        public int Id { get; }
        public int Grade { get; }
        public ItemType ItemType { get; }
        public ItemSubType ItemSubType { get; }
        public ElementalType ElementalType { get; }
        protected ItemBase(ItemSheet.Row data)
        {
            Id = data.Id;
            Grade = data.Grade;
            ItemType = data.ItemType;
            ItemSubType = data.ItemSubType;
            ElementalType = data.ElementalType;
        }

        protected ItemBase(Dictionary serialized)
        {
            if (serialized.TryGetValue((Text) "id", out var id))
            {
                Id = id.ToInteger();
            }
            if (serialized.TryGetValue((Text) "grade", out var grade))
            {
                Grade = grade.ToInteger();
            }
            if (serialized.TryGetValue((Text) "item_type", out var type))
            {
                ItemType = type.ToEnum<ItemType>();
            }
            if (serialized.TryGetValue((Text) "item_sub_type", out var subType))
            {
                ItemSubType = subType.ToEnum<ItemSubType>();
            }
            if (serialized.TryGetValue((Text) "elemental_type", out var elementalType))
            {
                ElementalType = elementalType.ToEnum<ElementalType>();
            }
        }

        protected ItemBase(SerializationInfo info, StreamingContext _)
            : this((Dictionary) Codec.Decode((byte[]) info.GetValue("serialized", typeof(byte[]))))
        {
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("serialized", Codec.Encode(Serialize()));
        }

        protected bool Equals(ItemBase other)
        {
            return Id == other.Id;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((ItemBase) obj);
        }

        public override int GetHashCode()
        {
            return Id;
        }

        public virtual IValue Serialize() =>
            Dictionary.Empty
                .Add("id", Id.Serialize())
                .Add("item_type", ItemType.Serialize())
                .Add("item_sub_type", ItemSubType.Serialize())
                .Add("grade", Grade.Serialize())
                .Add("elemental_type", ElementalType.Serialize());

        public override string ToString()
        {
            return
                $"{nameof(Id)}: {Id}" +
                $", {nameof(Grade)}: {Grade}" +
                $", {nameof(ItemType)}: {ItemType}" +
                $", {nameof(ItemSubType)}: {ItemSubType}" +
                $", {nameof(ElementalType)}: {ElementalType}";
        }
    }
}
