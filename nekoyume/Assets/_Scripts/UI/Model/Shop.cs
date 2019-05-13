using System;
using System.Collections.Generic;
using Nekoyume.Game.Item;
using UniRx;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class Shop : IDisposable
    {
        private static readonly string DimmedString = ItemBase.ItemType.Material.ToString();
        
        public enum State
        {
            Buy, Sell
        }
        
        public readonly ReactiveProperty<State> state = new ReactiveProperty<State>();
        public readonly ReactiveProperty<InventoryAndItemInfo> inventoryAndItemInfo = new ReactiveProperty<InventoryAndItemInfo>();
        public readonly ReactiveProperty<ShopItems> shopItems = new ReactiveProperty<ShopItems>();
        public readonly ReactiveProperty<ItemCountAndPricePopup> itemCountAndPricePopup = new ReactiveProperty<ItemCountAndPricePopup>();
        
        public readonly Subject<Shop> onClickSwitchBuy = new Subject<Shop>();
        public readonly Subject<Shop> onClickSwitchSell = new Subject<Shop>();
        public readonly Subject<Shop> onClickClose = new Subject<Shop>();

        public Shop(List<Game.Item.Inventory.InventoryItem> items)
        {
            inventoryAndItemInfo.Value = new InventoryAndItemInfo(items);
            shopItems.Value = new ShopItems();
            itemCountAndPricePopup.Value = new ItemCountAndPricePopup();

            state.Subscribe(OnState);
            inventoryAndItemInfo.Value.itemInfo.Value.item.Subscribe(OnItemInfoItem);
            inventoryAndItemInfo.Value.itemInfo.Value.onClick.Subscribe(OnClickItemInfo);
            shopItems.Value.onClickRefresh.Subscribe(OnClickShopItemsRefresh);

            onClickSwitchBuy.Subscribe(_ => state.Value = State.Buy);
            onClickSwitchSell.Subscribe(_ => state.Value = State.Sell);
        }
        
        public void Dispose()
        {
            state.Dispose();
            inventoryAndItemInfo.DisposeAll();
            shopItems.DisposeAll();
            itemCountAndPricePopup.DisposeAll();
            onClickSwitchBuy.Dispose();
            onClickSwitchSell.Dispose();
        }

        private void OnState(State value)
        {
            inventoryAndItemInfo.Value.inventory.Value.DeselectAll();
            
            switch (value)
            {
                case State.Buy:
                    inventoryAndItemInfo.Value.inventory.Value.dimmedFunc.Value = null;
                    inventoryAndItemInfo.Value.itemInfo.Value.buttonText.Value = "구매하기";
                    inventoryAndItemInfo.Value.itemInfo.Value.buttonEnabledFunc.Value = ButtonEnabledFuncForBuy;
                    break;
                case State.Sell:
                    inventoryAndItemInfo.Value.inventory.Value.dimmedFunc.Value = DimmedFuncForSell;
                    inventoryAndItemInfo.Value.itemInfo.Value.buttonText.Value = "판매하기";
                    inventoryAndItemInfo.Value.itemInfo.Value.buttonEnabledFunc.Value = null;
                    break;
            }
        }
        
        private static bool DimmedFuncForSell(InventoryItem inventoryItem)
        {
            return inventoryItem.item.Value.Item.Data.cls == DimmedString ||
                   inventoryItem.item.Value.Item.registeredToShop;
        }

        private static bool ButtonEnabledFuncForBuy(InventoryItem inventoryItem)
        {
            return false;
        }

        private void OnItemInfoItem(InventoryItem inventoryItem)
        {
            Debug.Log("OnItemInfoItem");
        }
        
        private void OnClickItemInfo(ItemInfo itemInfo)
        {
            if (ReferenceEquals(itemInfo, null) ||
                ReferenceEquals(itemInfo.item.Value, null))
            {
                return;
            }

            itemCountAndPricePopup.Value.item.Value = itemInfo.item.Value;
            itemCountAndPricePopup.Value.count.Value = 1;
            itemCountAndPricePopup.Value.minCount.Value = 1;
            itemCountAndPricePopup.Value.maxCount.Value = itemInfo.item.Value.count.Value;
            itemCountAndPricePopup.Value.price.Value = 0;
        }

        private void OnClickShopItemsRefresh(ShopItems value)
        {
            if (state.Value == State.Sell)
            {
                return;
            }
            
            Debug.Log("OnClickShopItemsRefresh");
        }
    }
}
