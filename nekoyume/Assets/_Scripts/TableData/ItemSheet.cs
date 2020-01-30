using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Bencodex.Types;
using Nekoyume.Model.Elemental;
using Nekoyume.Model.Item;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ItemSheet : Sheet<int, ItemSheet.Row>
    {
        [Serializable]
        public abstract class RowBase : SheetRow<int>
        {
            public abstract IValue Serialize();
        }

        [Serializable]
        public class Row : RowBase
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public virtual ItemType ItemType { get; }
            public ItemSubType ItemSubType { get; protected set; }
            public int Grade { get; private set; }
            public ElementalType ElementalType { get; private set; }

            public Row()
            {
            }

            public Row(Bencodex.Types.Dictionary serialized)
            {
                Id = (Integer) serialized["item_id"];
                ItemSubType = (ItemSubType) Enum.Parse(typeof(ItemSubType), (Bencodex.Types.Text) serialized["item_sub_type"]);
                Grade = (Integer) serialized["grade"];
                ElementalType = (ElementalType) Enum.Parse(typeof(ElementalType), (Bencodex.Types.Text) serialized["elemental_type"]);
            }

            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                ItemSubType = (ItemSubType) Enum.Parse(typeof(ItemSubType), fields[1]);
                Grade = int.Parse(fields[2]);
                ElementalType = (ElementalType) Enum.Parse(typeof(ElementalType), fields[3]);
            }

            public override IValue Serialize() =>
                Bencodex.Types.Dictionary.Empty
                    .Add("item_id", Id)
                    .Add("item_sub_type", ItemSubType.ToString())
                    .Add("grade", Grade)
                    .Add("elemental_type", ElementalType.ToString());

            public static Row Deserialize(Bencodex.Types.Dictionary serialized)
            {
                return new Row(serialized);
            }
        }
        
        public ItemSheet() : base(nameof(ItemSheet))
        {
        }
    }
    
    public static class ItemSheetExtension
    {
        public static string GetLocalizedName(this ItemSheet.Row value)
        {
            return LocalizationManager.Localize($"ITEM_NAME_{value.Id}");
        }

        public static string GetLocalizedDescription(this ItemSheet.Row value)
        {
            return LocalizationManager.Localize($"ITEM_DESCRIPTION_{value.Id}");
        }
    }
}
