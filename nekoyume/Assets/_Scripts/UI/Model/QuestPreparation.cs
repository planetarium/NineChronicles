using System;
using Assets.SimpleLocalization;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class QuestPreparation : IDisposable
    {
        public readonly ReactiveProperty<Inventory> inventory = new ReactiveProperty<Inventory>();
        public readonly ReactiveProperty<ItemInfo> itemInfo = new ReactiveProperty<ItemInfo>();

        public QuestPreparation(Game.Item.Inventory inventory)
        {
            this.inventory.Value = new Inventory(inventory);
            this.inventory.Value.dimmedFunc.Value = DimmedFunc;
            itemInfo.Value = new ItemInfo();
            itemInfo.Value.buttonText.Value = LocalizationManager.Localize("UI_EQUIP");
            itemInfo.Value.buttonEnabledFunc.Value = null;
        }
        
        public void Dispose()
        {
            inventory.DisposeAll();
            itemInfo.DisposeAll();
        }

        private bool DimmedFunc(InventoryItem inventoryItem)
        {
            return inventoryItem.item.Value.Data.ItemType == ItemType.Material;
        }
    }
}
