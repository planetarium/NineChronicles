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

        public Shop(List<Game.Item.Inventory.InventoryItem> items, Game.Shop shop)
        {
            inventoryAndItemInfo.Value = new InventoryAndItemInfo(items);
            shopItems.Value = new ShopItems(shop);
            itemCountAndPricePopup.Value = new ItemCountAndPricePopup();
            itemCountAndPricePopup.Value.titleText.Value = "판매 설정";
            itemCountAndPricePopup.Value.submitText.Value = "판매";

            state.Subscribe(OnState);
            inventoryAndItemInfo.Value.itemInfo.Value.item.Subscribe(OnItemInfoItem);
            inventoryAndItemInfo.Value.itemInfo.Value.onClick.Subscribe(OnClickItemInfo);

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
            onClickClose.Dispose();
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
            return inventoryItem.item.Value.Data.cls == DimmedString;
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

            itemCountAndPricePopup.Value.item.Value = new CountEditableItem(
                itemInfo.item.Value.item.Value,
                itemInfo.item.Value.count.Value,
                0,
                itemInfo.item.Value.count.Value,
                "수정");
            itemCountAndPricePopup.Value.price.Value = 1;
        }
    }
}
