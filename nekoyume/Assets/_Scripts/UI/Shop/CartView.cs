using System.Collections.Generic;
using System.Linq;
using Libplanet.Assets;
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
            hideCartButton.OnSubmitSubject.Subscribe(_ =>
            {
                _onClickHideCart?.Invoke();
            }).AddTo(gameObject);

            buyButton.Text = L10nManager.Localize("UI_BUY");
            buyButton.OnSubmitSubject.Subscribe(_ =>
            {
                _onClickBuy?.Invoke();
            }).AddTo(gameObject);

            buyButton.OnClickDisabledSubject.Subscribe(_ =>
            {
                OneLineSystem.Push(MailType.System,
                    L10nManager.Localize("UI_NOT_ENOUGH_NCG"),
                    NotificationCell.NotificationType.Information);
            }).AddTo(gameObject);
        }

        public void Set(System.Action onClickBuy,
            System.Action onClickHideCart)
        {
            _onClickBuy = onClickBuy;
            _onClickHideCart = onClickHideCart;
        }

        public void UpdateCart(List<ShopItem> selectedItems)
        {
            var sortedItems = selectedItems.Where(x => !x.Expired.Value).ToList();
            var price = new FungibleAssetValue(States.Instance.GoldBalanceState.Gold.Currency, 0 ,0);
            for (var i = 0; i < cartItems.Count; i++)
            {
                if (i < sortedItems.Count)
                {
                    price += sortedItems[i].OrderDigest.Price;
                    cartItems[i].gameObject.SetActive(true);
                    cartItems[i].Set(sortedItems[i], (item) =>
                    {
                        item.Selected.SetValueAndForceNotify(false);
                        selectedItems.Remove(item);
                        UpdateCart(selectedItems);
                    });
                }
                else
                {
                    cartItems[i].gameObject.SetActive(false);
                }
            }

            if (States.Instance.GoldBalanceState.Gold < price)
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
