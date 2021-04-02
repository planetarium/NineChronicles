using System;
using System.Runtime.Serialization;
using Bencodex;
using Bencodex.Types;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using static Lib9c.SerializeKeys;

namespace Nekoyume.Model.Item
{
    [Serializable]
    public abstract class ItemBase : IState
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
            bool useLegacy = serialized.ContainsKey(LegacyItemTypeKey);
            Text gradeKey = useLegacy ? LegacyGradeKey : GradeKey;
            Text itemTypeKey = useLegacy ? LegacyItemTypeKey : ItemTypeKey;
            Text itemSubTypeKey = useLegacy ? LegacyItemSubTypeKey : ItemSubTypeKey;
            Text elementalTypeKey = useLegacy ? LegacyElementalTypeKey : ElementalTypeKey;
            if (serialized.TryGetValue((Text) IdKey, out var id))
            {
                Id = id.ToInteger();
            }
            if (serialized.TryGetValue(gradeKey, out var grade))
            {
                Grade = grade.ToInteger();
            }
            if (serialized.TryGetValue(itemTypeKey, out var type))
            {
                ItemType = type.ToEnum<ItemType>();
            }
            if (serialized.TryGetValue(itemSubTypeKey, out var subType))
            {
                ItemSubType = subType.ToEnum<ItemSubType>();
            }
            if (serialized.TryGetValue(elementalTypeKey, out var elementalType))
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
                .Add(IdKey, Id.Serialize())
                .Add(ItemTypeKey, ItemType.Serialize())
                .Add(ItemSubTypeKey, ItemSubType.Serialize())
                .Add(GradeKey, Grade.Serialize())
                .Add(ElementalTypeKey, ElementalType.Serialize());

        public virtual IValue SerializeLegacy() =>
            Dictionary.Empty
                .Add(IdKey, Id.Serialize())
                .Add(LegacyItemTypeKey, ItemType.Serialize())
                .Add(LegacyItemSubTypeKey, ItemSubType.Serialize())
                .Add(LegacyGradeKey, Grade.Serialize())
                .Add(LegacyElementalTypeKey, ElementalType.Serialize());

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
