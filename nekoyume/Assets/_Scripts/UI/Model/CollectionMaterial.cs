using Nekoyume.TableData;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CollectionMaterial
    {
        public CollectionSheet.CollectionMaterial Row { get; }
        public int Grade { get; }
        public bool HasItem { get; }
        public bool CheckLevel { get; }
        public bool EnoughCount { get; }

        // enough condition for active
        public bool Enough => HasItem && EnoughCount;

        public ReactiveProperty<bool> Selected { get; }

        public ReactiveProperty<bool> Focused { get; }

        public CollectionMaterial(
            CollectionSheet.CollectionMaterial row,
            int grade,
            bool hasItem,
            bool checkLevel,
            bool enoughCount)
        {
            Row = row;
            Grade = grade;
            HasItem = hasItem;
            CheckLevel = checkLevel;
            EnoughCount = enoughCount;
            Selected = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
        }

        // when collection is active - set default (no need to check level and enough count)
        public CollectionMaterial(
            CollectionSheet.CollectionMaterial row,
            int grade) : this(row, grade, true, true, true)
        {
            Row = row;
            Grade = grade;
            Selected = new ReactiveProperty<bool>(false);
            Focused = new ReactiveProperty<bool>(false);
        }
    }
}
