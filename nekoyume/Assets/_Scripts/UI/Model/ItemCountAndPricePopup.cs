using UniRx;

namespace Nekoyume.UI.Model
{
    public class SelectItemCountAndPricePopup : SelectItemCountPopup
    {
        public readonly ReactiveProperty<int> price = new ReactiveProperty<int>();
        
        public override void Dispose()
        {
            price.Dispose();
            
            base.Dispose();
        }
    }
}
