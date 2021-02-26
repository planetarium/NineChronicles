using Nekoyume.Model.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CountEditableItem : CountableItem
    {
        public readonly ReactiveProperty<int> MinCount = new ReactiveProperty<int>(0);
        public readonly ReactiveProperty<int> MaxCount = new ReactiveProperty<int>(1);

        public CountEditableItem(ItemBase item, int count, int minCount, int maxCount) : base(item, count)
        {
            MinCount.Value = minCount;
            MaxCount.Value = maxCount;
            
            MinCount.Subscribe(min =>
            {
                if (Count.Value < min)
                {
                    Count.Value = min;
                }
            });

            MaxCount.Subscribe(max =>
            {
                if (Count.Value > max)
                {
                    Count.Value = max;
                }
            });
        }
        
        public override void Dispose()
        {
            MinCount.Dispose();
            MaxCount.Dispose();
            base.Dispose();
        }
    }
}
