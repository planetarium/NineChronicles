using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Libplanet.Types.Assets;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;
using TMPro;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class CartView : MonoBehaviour
    {
        [SerializeField]
        private List<ShopCartItemView> cartItems = new();

        [SerializeField]
        private ConditionalButton hideCartButton;

        [SerializeField]
        private ConditionalButton buyButton;

        [SerializeField]
        private TextMeshProUGUI priceText;

        private System.Action _onClickBuy;
        private System.Action _onClickHideCart;

        private void Awake()
        {
            hideCartButton.Text = L10nManager.Localize("UI_CANCEL");
            hideCartButton.OnSubmitSubject.Subscribe(_ => { _onClickHideCart?.Invoke(); }).AddTo(gameObject);

            buyButton.Text = L10nManager.Localize("UI_BUY");
            buyButton.OnSubmitSubject.Subscribe(_ => { _onClickBuy?.Invoke(); }).AddTo(gameObject);

            buyButton.OnClickDisabledSubject.Subscribe(_ =>
            {
                var sumPrice = ShopBuy.GetSumPrice(cartItems
                    .Where(i => i.ShopItem is not null)
                    .Select(i => i.ShopItem).ToList());
                Widget.Find<PaymentPopup>().ShowLackPaymentNCG(sumPrice.ToString());
            }).AddTo(gameObject);
        }

        public void Set(System.Action onClickBuy,
            System.Action onClickHideCart)
        {
            _onClickBuy = onClickBuy;
            _onClickHideCart = onClickHideCart;
        }

        public void UpdateCart(List<ShopItem> selectedItems, System.Action onClick)
        {
            if (States.Instance.GoldBalanceState is null)
            {
                return;
            }

            var sortedItems = selectedItems.Where(x => !x.Expired.Value).ToList();
            var price = new FungibleAssetValue(States.Instance.GoldBalanceState.Gold.Currency, 0, 0);
            for (var i = 0; i < cartItems.Count; i++)
            {
                if (i < sortedItems.Count)
                {
                    var p = sortedItems[i].ItemBase is not null
                        ? (BigInteger)sortedItems[i].Product.Price
                        : (BigInteger)sortedItems[i].FungibleAssetProduct.Price;
                    price += p * States.Instance.GoldBalanceState.Gold.Currency;
                    cartItems[i].gameObject.SetActive(true);
                    cartItems[i].Set(sortedItems[i], (item) =>
                    {
                        onClick?.Invoke();
                        item.Selected.SetValueAndForceNotify(false);
                        selectedItems.Remove(item);
                        UpdateCart(selectedItems, onClick);
                    });
                }
                else
                {
                    cartItems[i].Set(null, null);
                    cartItems[i].gameObject.SetActive(false);
                }
            }

            if (States.Instance?.GoldBalanceState.Gold < price)
            {
                buyButton.Interactable = false;
                priceText.color = Palette.GetColor(ColorType.ButtonDisabled);
            }
            else
            {
                buyButton.Interactable = true;
                priceText.color = Palette.GetColor(0);
            }

            priceText.text = price.GetQuantityString();
        }
    }
}
