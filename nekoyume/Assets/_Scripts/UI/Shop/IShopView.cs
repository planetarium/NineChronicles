using System;
using System.Collections.Generic;
using MarketService.Response;
using Nekoyume.EnumType;
using Nekoyume.UI.Model;
using UniRx;

namespace Nekoyume.UI.Module
{
    public interface IShopView
    {
        public void Show(
            ReactiveProperty<Dictionary<ItemSubTypeFilter, List<ItemProductResponseModel>>> itemProducts,
            ReactiveProperty<List<FungibleAssetValueProductResponseModel>> fungibleAssetProducts,
            Action<ShopItem> clickItem);
    }
}
