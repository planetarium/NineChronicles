using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountAndPricePopup : ItemCountPopup<ItemCountAndPricePopup>
    {
        public readonly ReactiveProperty<decimal> price = new ReactiveProperty<decimal>(10);
        public readonly ReactiveProperty<bool> priceInteractable = new ReactiveProperty<bool>(true);
        
        public override void Dispose()
        {
            price.Dispose();
            priceInteractable.Dispose();
            
            base.Dispose();
        }
    }
}
