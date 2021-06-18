using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Model.Mail;
using Nekoyume.UI.Module;

namespace Nekoyume.UI.Model
{
    public class ShopBuyItems : ShopItems
    {
        private readonly List<ShopItem> wishItems = new List<ShopItem>();
        private const int WishListSize = 8;

        public List<ShopItem> GetWishItems => wishItems;

        public int WishItemCount => wishItems?.Count ?? 0;

        protected override void ResetSelectedState()
        {
            foreach (var keyValuePair in Items.Value)
            {
                foreach (var shopItem in keyValuePair.Value)
                {
                    var isSelected =
                        wishItems.Exists(x => x.OrderId.Value == shopItem.OrderId.Value);
                    shopItem.Selected.Value = isSelected;
                }
            }
        }

        protected override void OnClickItem(ShopItemView view)
        {
            if (isMultiplePurchase)
            {
                var wishItem = wishItems.FirstOrDefault(x =>
                    x.OrderId.Value == view.Model.OrderId.Value);
                if (wishItem is null)
                {
                    if (wishItems.Count < WishListSize)
                    {
                        wishItems.Add(view.Model);
                        SelectedItemView.SetValueAndForceNotify(view);
                        _selectedItemViewModel.SetValueAndForceNotify(view.Model);
                        _selectedItemViewModel.Value.Selected.SetValueAndForceNotify(true);
                    }
                    else
                    {
                        OneLinePopup.Push(MailType.System,
                            L10nManager.Localize("NOTIFICATION_BUY_WISHLIST_FULL"));
                    }
                }
                else
                {
                    _selectedItemViewModel.SetValueAndForceNotify(view.Model);
                    _selectedItemViewModel.Value.Selected.SetValueAndForceNotify(false);
                    SelectedItemView.SetValueAndForceNotify(view);
                    wishItems.Remove(wishItem);

                    _selectedItemViewModel.SetValueAndForceNotify(null);
                    SelectedItemView.SetValueAndForceNotify(null);
                }
            }
            else
            {
                if (view is null || view == SelectedItemView.Value)
                {
                    DeselectItemView();
                    return;
                }

                SelectItemView(view);
            }
        }

        public void RemoveItemInWishList(ShopItem shopItem)
        {
            var selected = wishItems.FirstOrDefault(x =>
                x.OrderId.Value == shopItem.OrderId.Value);

            if (selected is null)
            {
                return;
            }

            wishItems.Remove(shopItem);
            foreach (var keyValuePair in Items.Value)
            {
                var reuslt = keyValuePair.Value.FirstOrDefault(
                    x => x.OrderId.Value == selected.OrderId.Value);
                if (reuslt != null)
                {
                    _selectedItemViewModel.Value = reuslt;
                    _selectedItemViewModel.Value.Selected.Value = false;
                    SelectedItemView.Value = reuslt.View;

                    _selectedItemViewModel.Value = null;
                    SelectedItemView.Value = null;
                    return;
                }
            }
        }

        public void ClearWishList()
        {
            wishItems.Clear();
            ResetSelectedState();
        }

        public void SetMultiplePurchase(bool value)
        {
            ClearWishList();
            isMultiplePurchase = value;
        }
    }
}
