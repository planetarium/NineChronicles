using System.Collections.Generic;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
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
        [SerializeField] private Button transactionHistoryButton;

        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI buyText;
        [SerializeField] private TextMeshProUGUI transactionHistoryText;

        public readonly Subject<bool> OnChangeBuyType = new Subject<bool>();

        private List<ShopItem> _wishList = new List<ShopItem>();
        private double _price;

        private void Awake()
        {
            showWishListButton.OnClickAsObservable().Subscribe(ShowWishList).AddTo(gameObject);
            cancelButton.OnClickAsObservable().Subscribe(OnCloseBuyWishList).AddTo(gameObject);
            buyButton.OnClickAsObservable().Subscribe(OnClickBuy).AddTo(gameObject);
            transactionHistoryButton.OnClickAsObservable().Subscribe(OnClickTransactionHistory).AddTo(gameObject);
            buyText.text = L10nManager.Localize("UI_BUY_MULTIPLE");
            transactionHistoryText.text = L10nManager.Localize("UI_TRANSACTION_HISTORY");
        }

        private void OnEnable()
        {
            ShowDefaultView();
        }

        private void ShowWishList(Unit unit)
        {
            Clear();
            defaultView.SetActive(false);
            wishListView.SetActive(true);
            OnChangeBuyType.OnNext(true);
        }

        private void ShowDefaultView()
        {
            priceText.text = "0";
            defaultView.SetActive(true);
            wishListView.SetActive(false);
            OnChangeBuyType.OnNext(false);
        }

        private void OnCloseBuyWishList(Unit unit)
        {
            if (_wishList.Count > 0)
            {
                Widget.Find<TwoButtonPopup>().Show(L10nManager.Localize("UI_CLOSE_BUY_WISH_LIST"),
                                                   L10nManager.Localize("UI_YES"),
                                                   L10nManager.Localize("UI_NO"),
                                                   ShowDefaultView);
            }
            else
            {
                ShowDefaultView();
            }
        }

        private void OnClickBuy(Unit unit)
        {
            if (_price <= 0)
            {
                return;
            }

            var currentGold = double.Parse(States.Instance.GoldBalanceState.Gold.GetQuantityString());
            if (currentGold < _price)
            {
                OneLinePopup.Push(MailType.System, L10nManager.Localize("UI_NOT_ENOUGH_NCG"));
                return;
            }

            var content = string.Format(L10nManager.Localize("UI_BUY_MULTIPLE_FORMAT"), _wishList.Count, _price);
            content = content.Replace("\\n", "\n");
            Widget.Find<TwoButtonPopup>().Show(content,
                                               L10nManager.Localize("UI_BUY"),
                                               L10nManager.Localize("UI_CANCEL"),
                                               (() => {}));
        }

        private void OnClickTransactionHistory(Unit unit)
        {
            OneLinePopup.Push(MailType.System, L10nManager.Localize("UI_ALERT_NOT_IMPLEMENTED_CONTENT"));
        }

        private void Clear()
        {
            foreach (var item in items)
            {
                item?.gameObject.SetActive(false);
            }
        }

        public void UpdateWishList(Model.ShopItems shopItems)
        {
            Clear();
            _wishList = shopItems.wishItems;
            _price = 0.0f;
            for (int i = 0; i < _wishList.Count; i++)
            {
                var shopItem = _wishList[i];
                _price += double.Parse(shopItem.Price.Value.GetQuantityString());
                items[i].gameObject.SetActive(true);
                items[i].SetData(shopItem, () =>
                {
                    shopItems.RemoveItemInWishList(shopItem);
                    UpdateWishList(shopItems);
                });
            }

            priceText.text = _price.ToString();
        }

        private void OnDestroy()
        {
            OnChangeBuyType.Dispose();
        }
    }
}
