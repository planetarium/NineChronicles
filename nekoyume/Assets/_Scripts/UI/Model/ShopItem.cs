using Lib9c.Model.Order;
using MarketService.Response;
using Nekoyume.Model.Item;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class ShopItem
    {
        public ItemBase ItemBase { get; }
        public ItemProductResponseModel Product { get; }
        public int Grade { get; }
        public bool LevelLimited { get; }

        public readonly ReactiveProperty<bool> Selected;
        public readonly ReactiveProperty<bool> Expired;
        public readonly ReactiveProperty<bool> Loading;

        public ShopItem(ItemBase itemBase, ItemProductResponseModel product,
            int grade, bool limited)
        {
            ItemBase = itemBase;
            Product = product;
            Grade = grade;
            LevelLimited = limited;
            Selected = new ReactiveProperty<bool>(false);
            Expired = new ReactiveProperty<bool>(false);
            Loading = new ReactiveProperty<bool>(false);
        }
    }
}
