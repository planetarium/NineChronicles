using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Nekoyume.EnumType;

namespace Nekoyume.TableData
{
    [Serializable]
    public class ItemSheet : Sheet<int, ItemSheet.Row>
    {
        [Serializable]
        public class Row : SheetRow<int>
        {
            public override int Key => Id;
            public int Id { get; private set; }
            public virtual ItemType ItemType { get; }
            public ItemSubType ItemSubType { get; protected set; }
            public int Grade { get; private set; }
            public ElementalType ElementalType { get; private set; }
            
            public override void Set(IReadOnlyList<string> fields)
            {
                Id = int.Parse(fields[0]);
                ItemSubType = (ItemSubType) Enum.Parse(typeof(ItemSubType), fields[1]);
                Grade = int.Parse(fields[2]);
                ElementalType = (ElementalType) Enum.Parse(typeof(ElementalType), fields[3]);
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
