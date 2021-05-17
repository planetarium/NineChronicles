using Libplanet.Assets;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountableAndPricePopup : ItemCountPopup<ItemCountableAndPricePopup>
    {
        public readonly ReactiveProperty<FungibleAssetValue> Price;
        public readonly ReactiveProperty<FungibleAssetValue> TotalPrice;
        public readonly ReactiveProperty<int> Count = new ReactiveProperty<int>(1);

        public readonly Subject<int> OnChangeCount = new Subject<int>();
        public readonly Subject<int> OnChangePrice = new Subject<int>();

        public ItemCountableAndPricePopup()
        {
            var currency = States.Instance.GoldBalanceState.Gold.Currency;
            Price = new ReactiveProperty<FungibleAssetValue>(new FungibleAssetValue(currency, 10, 0));
            TotalPrice = new ReactiveProperty<FungibleAssetValue>(new FungibleAssetValue(currency, 10, 0));
        }

        public override void Dispose()
        {
            Price.Dispose();
            TotalPrice.Dispose();
            Count.Dispose();
            OnChangeCount.Dispose();
            OnChangePrice.Dispose();
            base.Dispose();
        }
    }
}
