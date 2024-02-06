using Nekoyume.Model.Item;
using Nekoyume.TableData;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CollectionMaterial
    {
        public CollectionSheet.CollectionMaterial Row { get; }
        public int Grade { get; }
        public bool HasItem { get; } // todo : rx
        public ItemType ItemType { get; }
        public bool EnoughCount { get; } // todo : rx
        public bool Active { get; } // todo : rx

        // enough condition for active
        public bool Enough => HasItem && EnoughCount && !Active;

        public ReactiveProperty<bool> Selected { get; }

        public ReactiveProperty<bool> Focused { get; }

        public CollectionMaterial(
            CollectionSheet.CollectionMaterial row,
            int grade,
            ItemType itemType,
            bool hasItem,
            bool enoughCount)
        {
            Row = row;
            Grade = grade;
            ItemType = itemType;
            HasItem = hasItem;
            EnoughCount = enoughCount;
            Active = false;
            Selected = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
        }

        // when collection is active - set default (no need to check level and enough count)
        public CollectionMaterial(
            CollectionSheet.CollectionMaterial row,
            int grade,
            ItemType itemType) : this(row, grade, itemType, true, true)
        {
            Active = true;
        }
    }
}
