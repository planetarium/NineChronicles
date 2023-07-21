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

        private int _id;
        private int _grade;
        private ItemType _itemType;
        private ItemSubType _itemSubType;
        private ElementalType _elementalType;
        private Text? _serializedId;
        private Text? _serializedGrade;
        private Text? _serializedItemType;
        private Text? _serializedItemSubType;
        private Text? _serializedElementalType;

        public int Id
        {
            get
            {
                if (_serializedId is { })
                {
                    _id = _serializedId.ToInteger();
                    _serializedId = null;
                }

                return _id;
            }
        }

        public int Grade
        {
            get
            {
                if (_serializedGrade is { })
                {
                    _grade = _serializedGrade.ToInteger();
                    _serializedGrade = null;
                }

                return _grade;
            }
        }

        public ItemType ItemType
        {
            get
            {
                if (_serializedItemType is { })
                {
                    _itemType = _serializedItemType.ToEnum<ItemType>();
                    _serializedItemType = null;
                }

                return _itemType;
            }
        }

        public ItemSubType ItemSubType
        {
            get
            {
                if (_serializedItemSubType is { })
                {
                    _itemSubType = _serializedItemSubType.ToEnum<ItemSubType>();
                    _serializedItemSubType = null;
                }

                return _itemSubType;
            }
        }

        public ElementalType ElementalType
        {
            get
            {
                if (_serializedElementalType is { })
                {
                    _elementalType = _serializedElementalType.ToEnum<ElementalType>();
                    _serializedElementalType = null;
                }

                return _elementalType;
            }
        }

        protected ItemBase(ItemSheet.Row data)
        {
            _id = data.Id;
            _grade = data.Grade;
            _itemType = data.ItemType;
            _itemSubType = data.ItemSubType;
            _elementalType = data.ElementalType;
        }

        protected ItemBase(ItemBase other)
        {
            _id = other.Id;
            _grade = other.Grade;
            _itemType = other.ItemType;
            _itemSubType = other.ItemSubType;
            _elementalType = other.ElementalType;
        }

        protected ItemBase(Dictionary serialized)
        {
            if (serialized.TryGetValue((Text) "id", out var id))
            {
                _serializedId = (Text) id;
            }
            if (serialized.TryGetValue((Text) "grade", out var grade))
            {
                _serializedGrade = (Text) grade;
            }
            if (serialized.TryGetValue((Text) "item_type", out var type))
            {
                _serializedItemType = (Text) type;
            }
            if (serialized.TryGetValue((Text) "item_sub_type", out var subType))
            {
                _serializedItemSubType = (Text) subType;
            }
            if (serialized.TryGetValue((Text) "elemental_type", out var elementalType))
            {
                _serializedElementalType = (Text) elementalType;
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
                .Add("id", _serializedId ?? Id.Serialize())
                .Add("item_type", _serializedItemType ?? ItemType.Serialize())
                .Add("item_sub_type", _serializedItemSubType ?? ItemSubType.Serialize())
                .Add("grade", _serializedGrade ?? Grade.Serialize())
                .Add("elemental_type", _serializedElementalType ?? ElementalType.Serialize());

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
