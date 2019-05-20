using System;
using System.Collections.Generic;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class QuestPreparation : IDisposable
    {
        private static readonly string TypeString = ItemBase.ItemType.Material.ToString();
        
        public readonly ReactiveProperty<Inventory> inventory = new ReactiveProperty<Inventory>();
        public readonly ReactiveProperty<ItemInfo> itemInfo = new ReactiveProperty<ItemInfo>();

        public QuestPreparation(List<Game.Item.Inventory.InventoryItem> items)
        {
            inventory.Value = new Inventory(items);
            inventory.Value.dimmedFunc.Value = DimmedFunc;
            inventory.Value.glowedFunc.Value = GlowedFunc;
            itemInfo.Value = new ItemInfo();
            itemInfo.Value.buttonText.Value = "장착하기";
            itemInfo.Value.buttonEnabledFunc.Value = null;
            
            inventory.Value.selectedItem.Subscribe(OnInventorySelectedItem);
        }
        
        public void Dispose()
        {
            inventory.DisposeAll();
            itemInfo.DisposeAll();
        }

        private bool DimmedFunc(InventoryItem inventoryItem)
        {
            return inventoryItem.item.Value.Data.cls == TypeString;
        }

        private bool GlowedFunc(InventoryItem inventoryItem, ItemBase.ItemType type)
        {
            return inventoryItem.item.Value.Data.cls.ToEnumItemType() == type;
        }
        
        private void OnInventorySelectedItem(InventoryItem data)
        {
            itemInfo.Value.item.Value = data;
        }
    }
}
