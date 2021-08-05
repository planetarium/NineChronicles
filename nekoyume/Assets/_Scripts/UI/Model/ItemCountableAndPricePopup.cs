using Libplanet.Assets;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountableAndPricePopup : ItemCountPopup<ItemCountableAndPricePopup>
    {
        public readonly ReactiveProperty<FungibleAssetValue> Price = new ReactiveProperty<FungibleAssetValue>();
        public readonly ReactiveProperty<FungibleAssetValue> TotalPrice = new ReactiveProperty<FungibleAssetValue>();
        public readonly ReactiveProperty<FungibleAssetValue> PreTotalPrice = new ReactiveProperty<FungibleAssetValue>();
        public readonly ReactiveProperty<int> Count = new ReactiveProperty<int>(1);
        public readonly ReactiveProperty<bool> IsSell = new ReactiveProperty<bool>();

        public readonly Subject<int> OnChangeCount = new Subject<int>();
        public readonly Subject<decimal> OnChangePrice = new Subject<decimal>();
        public readonly Subject<ItemCountableAndPricePopup> OnClickReregister = new Subject<ItemCountableAndPricePopup>();

        public override void Dispose()
        {
            Price.Dispose();
            TotalPrice.Dispose();
            PreTotalPrice.Dispose();
            Count.Dispose();
            IsSell.Dispose();
            OnChangeCount.Dispose();
            OnChangePrice.Dispose();
            OnClickReregister.Dispose();
            base.Dispose();
        }
    }
}
