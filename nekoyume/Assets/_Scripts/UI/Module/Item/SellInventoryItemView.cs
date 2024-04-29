using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class SellInventoryItemView : InventoryItemView
    {
        protected override void UpdateFungibleAsset(InventoryItem model, InventoryScroll.ContextModel context)
        {
            base.UpdateFungibleAsset(model, context);
            model.Tradable.Subscribe(b => baseItemView.TradableObject.SetActive(!b))
                .AddTo(Disposables);
        }
    }
}
