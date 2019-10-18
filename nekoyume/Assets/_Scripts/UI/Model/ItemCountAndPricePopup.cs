using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountAndPricePopup : ItemCountPopup<ItemCountAndPricePopup>
    {
        public readonly ReactiveProperty<decimal> Price = new ReactiveProperty<decimal>(10);
        public readonly ReactiveProperty<bool> PriceInteractable = new ReactiveProperty<bool>(true);
        
        public override void Dispose()
        {
            Price.Dispose();
            PriceInteractable.Dispose();
            
            base.Dispose();
        }
    }
}
