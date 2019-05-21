using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountAndPricePopup : ItemCountPopup<ItemCountAndPricePopup>
    {
        public readonly ReactiveProperty<int> price = new ReactiveProperty<int>(1);
        public readonly ReactiveProperty<bool> priceEditable = new ReactiveProperty<bool>(true);
        
        public override void Dispose()
        {
            price.Dispose();
            priceEditable.Dispose();
            
            base.Dispose();
        }
    }
}
