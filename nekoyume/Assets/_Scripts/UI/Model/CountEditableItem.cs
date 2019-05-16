using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class CountEditableItem : CountableItem
    {
        public readonly ReactiveProperty<int> minCount = new ReactiveProperty<int>(0);
        public readonly ReactiveProperty<int> maxCount = new ReactiveProperty<int>(1);
        public readonly ReactiveProperty<string> editButtonText = new ReactiveProperty<string>("");

        public readonly Subject<CountEditableItem> onClose = new Subject<CountEditableItem>();
        public readonly Subject<CountEditableItem> onEdit = new Subject<CountEditableItem>();
        
        public CountEditableItem(ItemBase item, int count, int minCount, int maxCount, string editButtonText) : base(item, count)
        {
            this.minCount.Value = minCount;
            this.maxCount.Value = maxCount;
            this.editButtonText.Value = editButtonText;
            
            this.minCount.Subscribe(min =>
            {
                if (this.count.Value < min)
                {
                    this.count.Value = min;
                }
            });

            this.maxCount.Subscribe(max =>
            {
                if (this.count.Value > max)
                {
                    this.count.Value = max;
                }
            });
        }
        
        public override void Dispose()
        {
            base.Dispose();

            minCount.Dispose();
            maxCount.Dispose();
            editButtonText.Dispose();

            onClose.Dispose();
            onEdit.Dispose();
        }
    }
}
