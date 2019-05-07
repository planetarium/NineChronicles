using System;
using System.Collections.Generic;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class QuestPreparation : IDisposable
    {
        public readonly ReactiveProperty<InventoryAndSelectedItemInfo> inventoryAndSelectedItemInfo = new ReactiveProperty<InventoryAndSelectedItemInfo>();

        public QuestPreparation(List<Game.Item.Inventory.InventoryItem> items)
        {
            inventoryAndSelectedItemInfo.Value = new InventoryAndSelectedItemInfo(items, ItemBase.ItemType.Material.ToString());
            inventoryAndSelectedItemInfo.Value.selectedItemInfo.Value.buttonText.Value = "장착하기";
        }
        
        public void Dispose()
        {
            inventoryAndSelectedItemInfo.DisposeAll();
        }
    }
}
