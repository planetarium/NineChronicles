using Lib9c.Model.Order;
using Nekoyume.Model.Item;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class ShopItem : IItemViewModel
    {
        public ItemBase ItemBase { get; }
        public OrderDigest OrderDigest { get; }
        public int Grade { get; }
        public bool LevelLimited { get; }

        public readonly ReactiveProperty<bool> Selected;
        public readonly ReactiveProperty<bool> Expired;

        public RectTransform View { get; set; }

        public ShopItem(ItemBase itemBase, OrderDigest orderDigest,
            int grade, bool limited)
        {
            ItemBase = itemBase;
            OrderDigest = orderDigest;
            Grade = grade;
            LevelLimited = limited;
            Selected = new ReactiveProperty<bool>(false);
            Expired = new ReactiveProperty<bool>(false);
        }
    }
}
