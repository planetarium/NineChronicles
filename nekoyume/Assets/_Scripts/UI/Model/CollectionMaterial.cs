using Nekoyume.TableData;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CollectionMaterial
    {
        public ItemSheet.Row Row { get; }
        public bool HasItem { get; }

        public int Level { get; }
        public bool EnoughLevel { get; }

        public int Count { get; }
        public bool EnoughCount { get; }

        public ReactiveProperty<bool> Selected { get; }

        public CollectionMaterial(
            ItemSheet.Row row,
            bool hasItem,
            int level,
            bool enoughLevel,
            int count,
            bool enoughCount)
        {
            Row = row;
            HasItem = hasItem;
            Level = level;
            EnoughLevel = enoughLevel;
            Count = count;
            EnoughCount = enoughCount;
            Selected = new ReactiveProperty<bool>(false);
        }
    }
}
