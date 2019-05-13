using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountAndPricePopup : ItemCountPopup<ItemCountAndPricePopup>
    {
        public readonly ReactiveProperty<int> price = new ReactiveProperty<int>();
        
        public override void Dispose()
        {
            price.Dispose();
            
            base.Dispose();
        }
    }
}
