using Libplanet.Assets;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountableAndPricePopup : ItemCountPopup<ItemCountableAndPricePopup>
    {
        public readonly ReactiveProperty<FungibleAssetValue> Price;
        public readonly ReactiveProperty<int> Count = new ReactiveProperty<int>(1);

        public ItemCountableAndPricePopup()
        {
            var currency = States.Instance.GoldBalanceState.Gold.Currency;
            Price = new ReactiveProperty<FungibleAssetValue>(new FungibleAssetValue(currency, 10, 0));
        }

        public override void Dispose()
        {
            Price.Dispose();
            Count.Dispose();

            base.Dispose();
        }
    }
}
