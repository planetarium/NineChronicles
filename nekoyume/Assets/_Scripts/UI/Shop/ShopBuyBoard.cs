using System.Collections.Generic;
using Nekoyume.UI.Module;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ShopBuyBoard : MonoBehaviour
    {
        [SerializeField] List<ShopBuyWishItemView> items = new List<ShopBuyWishItemView>();
        [SerializeField] private GameObject eachView;
        [SerializeField] private GameObject bothView;
        [SerializeField] private Button showBothViewButton;
        [SerializeField] private Button showEachViewButton;
        [SerializeField] private Button buyButton;

        private void OnEnable()
        {
            Clear();
        }

        private void Clear()
        {
            foreach (var item in items)
            {
                item.gameObject.SetActive(false);
            }
        }

        public void UpdateWishList(Model.ShopItems shopItems)
        {
            Clear();

            var wishItems = shopItems.wishItems;
            for (int i = 0; i < wishItems.Count; i++)
            {
                var shopItem = wishItems[i];
                items[i].gameObject.SetActive(true);
                items[i].SetData(shopItem, () =>
                {
                    shopItems.RemoveItemInWishList(shopItem);
                    UpdateWishList(shopItems);
                });
            }
        }
    }
}
