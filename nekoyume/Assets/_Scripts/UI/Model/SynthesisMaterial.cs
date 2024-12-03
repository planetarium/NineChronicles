using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;

namespace Nekoyume.UI.Model
{
    public struct SynthesisMaterial
    {
        public Grade Grade { get; }
        public ItemType ItemType { get; }

        public SynthesisMaterial(Grade grade, ItemType itemType)
        {
            Grade = grade;
            ItemType = itemType;
        }
    }
}
