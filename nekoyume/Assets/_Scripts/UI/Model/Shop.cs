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
        public readonly ReactiveProperty<UI.Shop.StateType> State = new ReactiveProperty<UI.Shop.StateType>();

        public readonly ReactiveProperty<ItemCountAndPricePopup> ItemCountAndPricePopup =
            new ReactiveProperty<ItemCountAndPricePopup>(new ItemCountAndPricePopup());

        public void Dispose()
        {
            State.Dispose();
            ItemCountAndPricePopup.DisposeAll();
        }

        public void ShowItemPopup(CountableItem viewModel)
        {
            switch (viewModel)
            {
                case null:
                    return;
                case InventoryItem _:
                {
                    if (State.Value == UI.Shop.StateType.Sell)
                    {
                        // 판매하겠습니까?
                        ItemCountAndPricePopup.Value.TitleText.Value = LocalizationManager.Localize("UI_SELL");
                        ItemCountAndPricePopup.Value.InfoText.Value = LocalizationManager.Localize("UI_SELL_INFO");
                        ItemCountAndPricePopup.Value.CountEnabled.Value = true;
                        ItemCountAndPricePopup.Value.PriceInteractable.Value = true;
                        ItemCountAndPricePopup.Value.Item.Value = new CountEditableItem(
                            viewModel.ItemBase.Value,
                            1,
                            1,
                            viewModel.Count.Value);   
                    }
                    
                    return;
                }
                case ShopItem shopItem:
                {
                    if (State.Value == UI.Shop.StateType.Buy)
                    {
                        // 구매하겠습니까?
                        ItemCountAndPricePopup.Value.TitleText.Value = LocalizationManager.Localize("UI_BUY");
                        ItemCountAndPricePopup.Value.InfoText.Value = LocalizationManager.Localize("UI_BUY_INFO");
                    }
                    else
                    {
                        // 판매 취소하겠습니까?
                        ItemCountAndPricePopup.Value.TitleText.Value = LocalizationManager.Localize("UI_RETRIEVE");
                        ItemCountAndPricePopup.Value.InfoText.Value = LocalizationManager.Localize("UI_RETRIEVE_INFO");
                    }

                    ItemCountAndPricePopup.Value.CountEnabled.Value = false;
                    ItemCountAndPricePopup.Value.Price.Value = shopItem.Price.Value;
                    ItemCountAndPricePopup.Value.PriceInteractable.Value = false;
                    ItemCountAndPricePopup.Value.Item.Value = new CountEditableItem(
                        viewModel.ItemBase.Value,
                        shopItem.Count.Value,
                        shopItem.Count.Value,
                        shopItem.Count.Value);

                    return;
                }
            }
        }
    }
}
