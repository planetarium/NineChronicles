using System;
using System.Collections.Generic;
using Lib9c.Model.Order;
using Nekoyume.Model.Market;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public interface IShopView
    {
        public void Show(ReactiveProperty<List<ItemProductModel>> digests,
            Action<ShopItem> clickItem);
    }
}
