using Nekoyume.Model.EnumType;

namespace Nekoyume.UI.Model
{
    public class SynthesizeModel
    {
        public Grade Grade { get; }
        public int InventoryItemCount { get; set; }
        public int RequiredItemCount { get; }

        public SynthesizeModel(Grade grade, int inventoryItemCount, int requiredItemCount)
        {
            Grade = grade;
            InventoryItemCount = inventoryItemCount;
            RequiredItemCount = requiredItemCount;
        }
    }
}
