using System;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;

namespace Nekoyume.UI.Model
{
    public class SynthesizeModel
    {
        public Grade Grade { get; }
        public ItemSubType ItemSubType { get; }
        public int InventoryItemCount { get; set; }
        public int RequiredItemCount { get; }

        public SynthesizeModel(Grade grade, ItemSubType itemSubType, int inventoryItemCount, int requiredItemCount)
        {
            Grade = grade;
            ItemSubType = itemSubType;
            InventoryItemCount = inventoryItemCount;
            RequiredItemCount = requiredItemCount;
        }

        public static bool operator ==(SynthesizeModel obj1, SynthesizeModel obj2)
        {
            if (obj1 is null)
            {
                return obj2 is null;
            }

            if (obj2 is null)
            {
                return false;
            }

            return obj1.Grade == obj2.Grade && obj1.ItemSubType == obj2.ItemSubType;
        }

        public static bool operator!= (SynthesizeModel obj1, SynthesizeModel obj2)
        {
            return !(obj1 == obj2);
        }
    }
}
