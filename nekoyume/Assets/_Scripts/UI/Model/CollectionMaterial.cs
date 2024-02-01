using Nekoyume.TableData;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CollectionMaterial
    {
        public CollectionSheet.CollectionMaterial Row { get; }
        public int Grade { get; }
        public bool HasItem { get; }
        public bool EnoughLevel { get; }
        public bool EnoughCount { get; }

        public ReactiveProperty<bool> Selected { get; }

        public CollectionMaterial(
            CollectionSheet.CollectionMaterial row,
            int grade,
            bool hasItem,
            bool enoughLevel,
            bool enoughCount)
        {
            Row = row;
            Grade = grade;
            HasItem = hasItem;
            EnoughLevel = enoughLevel;
            EnoughCount = enoughCount;
            Selected = new ReactiveProperty<bool>(false);
        }
    }
}
