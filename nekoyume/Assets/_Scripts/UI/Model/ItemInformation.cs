using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemInformation
    {
        public readonly ReactiveProperty<CountableItem> item = new ReactiveProperty<CountableItem>();
        public readonly ReactiveProperty<bool> optionalEnabled = new ReactiveProperty<bool>();

        public ItemInformation(CountableItem countableItem = null)
        {
            item.Value = countableItem;
        }
    }
}
