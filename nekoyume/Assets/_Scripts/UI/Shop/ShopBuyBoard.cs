using System;
using System.Collections.Generic;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class ShopBuyBoard : MonoBehaviour
    {
        [SerializeField] List<ShopBuyWishItemView> items = new List<ShopBuyWishItemView>();
        [SerializeField] private GameObject defaultView;
        [SerializeField] private GameObject wishListView;
        [SerializeField] private Button showWishListButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private Button buyButton;

        public readonly Subject<bool> OnChangeBuyType = new Subject<bool>();

        private void Awake()
        {
            showWishListButton.OnClickAsObservable().Subscribe(ShowWishList).AddTo(gameObject);
            cancelButton.OnClickAsObservable().Subscribe(OnClickCancel).AddTo(gameObject);
            buyButton.OnClickAsObservable().Subscribe(OnClickBuy).AddTo(gameObject);
        }

        private void OnEnable()
        {
            OnClickCancel(Unit.Default);
        }

        private void ShowWishList(Unit unit)
        {
            Clear();
            defaultView.SetActive(false);
            wishListView.SetActive(true);
            OnChangeBuyType.OnNext(true);
        }

        private void OnClickCancel(Unit unit)
        {
            defaultView.SetActive(true);
            wishListView.SetActive(false);
            OnChangeBuyType.OnNext(false);
        }

        private void OnClickBuy(Unit unit)
        {

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

        private void OnDestroy()
        {
            OnChangeBuyType.Dispose();
        }
    }
}
