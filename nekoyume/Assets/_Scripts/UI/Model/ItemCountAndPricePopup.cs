using System;
using Libplanet.Assets;
using Nekoyume.State;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountAndPricePopup : ItemCountPopup<ItemCountAndPricePopup>
    {
        public readonly ReactiveProperty<FungibleAssetValue> Price;
        public readonly ReactiveProperty<bool> PriceInteractable = new(true);
        public readonly ReactiveProperty<Guid> ProductId = new ();
        public readonly ReactiveProperty<bool> ChargeAp = new (false);

        public ItemCountAndPricePopup()
        {
            var currency = States.Instance.GoldBalanceState.Gold.Currency;
            Price = new ReactiveProperty<FungibleAssetValue>(new FungibleAssetValue(currency, 10, 0));
        }

        public override void Dispose()
        {
            Price.Dispose();
            PriceInteractable.Dispose();
            ProductId.Dispose();
            base.Dispose();
        }
    }
}
