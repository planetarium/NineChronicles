using System.Collections.Generic;
using Lib9c.Model.Order;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public interface IShopItemView
    {
        public void Show(ReactiveProperty<List<OrderDigest>> digests,
            System.Action<ShopItemViewModel, RectTransform> clickItem);
    }
}
