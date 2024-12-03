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
    }
}
