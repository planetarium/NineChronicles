using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CountEditableItem : CountableItem
    {
        public readonly ReactiveProperty<int> minCount = new ReactiveProperty<int>(0);
        public readonly ReactiveProperty<int> maxCount = new ReactiveProperty<int>(1);

        public readonly Subject<CountEditableItem> onMinus = new Subject<CountEditableItem>();
        public readonly Subject<CountEditableItem> onPlus = new Subject<CountEditableItem>();
        public readonly Subject<CountEditableItem> onDelete = new Subject<CountEditableItem>();
        
        public CountEditableItem(ItemBase item, int count, int minCount, int maxCount) : base(item, count)
        {
            this.minCount.Value = minCount;
            this.maxCount.Value = maxCount;
            
            this.minCount.Subscribe(min =>
            {
                if (this.Count.Value < min)
                {
                    this.Count.Value = min;
                }
            });

            this.maxCount.Subscribe(max =>
            {
                if (this.Count.Value > max)
                {
                    this.Count.Value = max;
                }
            });
        }
        
        public override void Dispose()
        {
            base.Dispose();

            minCount.Dispose();
            maxCount.Dispose();

            onDelete.Dispose();
        }
    }
}
