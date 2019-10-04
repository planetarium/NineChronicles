using System;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using Libplanet;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.Model;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class Shop : IDisposable
    {
        public enum StateType
        {
            Show,
            Buy,
            Sell
        }

        public readonly ReactiveProperty<StateType> State = new ReactiveProperty<StateType>();
        public readonly ReactiveProperty<ShopItems> ShopItems = new ReactiveProperty<ShopItems>(new ShopItems());

        public readonly ReactiveProperty<ItemCountAndPricePopup> ItemCountAndPricePopup =
            new ReactiveProperty<ItemCountAndPricePopup>(new ItemCountAndPricePopup());

        public readonly Subject<Shop> OnClickSwitchBuy = new Subject<Shop>();
        public readonly Subject<Shop> OnClickSwitchSell = new Subject<Shop>();
        public readonly Subject<Shop> OnClickClose = new Subject<Shop>();

        public Shop()
        {
            State.Subscribe(SubscribeState);

            OnClickSwitchBuy.Subscribe(_ => State.Value = StateType.Buy);
            OnClickSwitchSell.Subscribe(_ => State.Value = StateType.Sell);
        }

        public void Dispose()
        {
            State.Dispose();
            ShopItems.DisposeAll();
            ItemCountAndPricePopup.DisposeAll();

            OnClickSwitchBuy.Dispose();
            OnClickSwitchSell.Dispose();
            OnClickClose.Dispose();
        }

        public void ResetItems(Dictionary<Address, List<Game.Item.ShopItem>> items)
        {
            if (items is null)
            {
                return;
            }

            ShopItems.Value.ResetItems(items);
        }

        #region Subscribe

        private void SubscribeState(StateType value)
        {
            ShopItems.Value.DeselectItemView();
        }

        #endregion

        public void ShowItemPopup(CountableItem inventoryItem)
        {
            switch (inventoryItem)
            {
                case null:
                    return;
                case ShopItem shopItem:
                {
                    if (State.Value == StateType.Buy)
                    {
                        // 구매하겠습니까?
                        ItemCountAndPricePopup.Value.titleText.Value = LocalizationManager.Localize("UI_BUY");
                    }
                    else
                    {
                        // 판매 취소하겠습니까?
                        ItemCountAndPricePopup.Value.titleText.Value = LocalizationManager.Localize("UI_RETRIEVE");
                    }

                    ItemCountAndPricePopup.Value.countEnabled.Value = false;
                    ItemCountAndPricePopup.Value.price.Value = shopItem.Price.Value;
                    ItemCountAndPricePopup.Value.priceInteractable.Value = false;
                    ItemCountAndPricePopup.Value.item.Value = new CountEditableItem(
                        inventoryItem.ItemBase.Value,
                        shopItem.Count.Value,
                        shopItem.Count.Value,
                        shopItem.Count.Value);

                    return;
                }
            }

            // 판매하겠습니까?
            ItemCountAndPricePopup.Value.titleText.Value = LocalizationManager.Localize("UI_SELL");
            ItemCountAndPricePopup.Value.countEnabled.Value = true;
            ItemCountAndPricePopup.Value.priceInteractable.Value = true;
            ItemCountAndPricePopup.Value.item.Value = new CountEditableItem(
                inventoryItem.ItemBase.Value,
                1,
                1,
                inventoryItem.Count.Value);
        }
    }
}
