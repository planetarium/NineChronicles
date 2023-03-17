using System;
using Libplanet.Assets;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class ItemCountableAndPricePopup : ItemCountPopup<ItemCountableAndPricePopup>
    {
        public readonly ReactiveProperty<Guid> ProductId = new();
        public readonly ReactiveProperty<FungibleAssetValue> Price = new();
        public readonly ReactiveProperty<FungibleAssetValue> PrePrice = new();
        public readonly ReactiveProperty<FungibleAssetValue> UnitPrice = new();
        public readonly ReactiveProperty<int> Count = new(1);
        public readonly ReactiveProperty<bool> IsSell = new();
        public readonly ReactiveProperty<bool> ChargeAp = new(false);

        public readonly Subject<int> OnChangeCount = new();
        public readonly Subject<decimal> OnChangePrice = new();
        public readonly Subject<ItemCountableAndPricePopup> OnClickReregister = new();

        public override void Dispose()
        {
            ProductId.Dispose();
            Price.Dispose();
            PrePrice.Dispose();
            UnitPrice.Dispose();
            Count.Dispose();
            IsSell.Dispose();
            OnChangeCount.Dispose();
            OnChangePrice.Dispose();
            OnClickReregister.Dispose();
            base.Dispose();
        }
    }
}
