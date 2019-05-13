using System;
using System.Collections.Generic;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class QuestPreparation : IDisposable
    {
        private static readonly string TypeString = ItemBase.ItemType.Material.ToString();
        
        public readonly ReactiveProperty<InventoryAndItemInfo> inventoryAndItemInfo = new ReactiveProperty<InventoryAndItemInfo>();

        public QuestPreparation(List<Game.Item.Inventory.InventoryItem> items)
        {
            inventoryAndItemInfo.Value = new InventoryAndItemInfo(items);
            inventoryAndItemInfo.Value.inventory.Value.dimmedFunc.Value = DimmedFunc;
            inventoryAndItemInfo.Value.inventory.Value.glowedFunc.Value = GlowedFunc;
            inventoryAndItemInfo.Value.itemInfo.Value.buttonText.Value = "장착하기";
            inventoryAndItemInfo.Value.itemInfo.Value.buttonEnabledFunc.Value = null;
        }
        
        public void Dispose()
        {
            inventoryAndItemInfo.DisposeAll();
        }

        private bool DimmedFunc(InventoryItem inventoryItem)
        {
            return inventoryItem.item.Value.Item.Data.cls == TypeString;
        }

        private bool GlowedFunc(InventoryItem inventoryItem, ItemBase.ItemType type)
        {
            return inventoryItem.item.Value.Item.Data.cls.ToEnumItemType() == type;
        }
    }
}
