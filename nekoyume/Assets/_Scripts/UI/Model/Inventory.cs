using System;
using System.Collections.Generic;
using System.Linq;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Inventory : IDisposable
    {
        public readonly ReactiveCollection<InventoryItem> items = new ReactiveCollection<InventoryItem>();
        public readonly ReactiveProperty<InventoryItem> selectedItem = new ReactiveProperty<InventoryItem>(null);
        public readonly ReactiveProperty<string[]> dimmedTypes = new ReactiveProperty<string[]>();

        public Inventory(List<Game.Item.Inventory.InventoryItem> items, params string[] dimmedTypes)
        {
            this.dimmedTypes.Value = dimmedTypes;
            
            items.ForEach(item =>
            {
                var inventoryItem = new InventoryItem(item);
                InitInventoryItem(inventoryItem);
                this.items.Add(inventoryItem);
            });

            this.items.ObserveAdd().Subscribe(added =>
            {
                InitInventoryItem(added.Value);
            });
            this.items.ObserveRemove().Subscribe(removed => removed.Value.Dispose());
            
            this.dimmedTypes.Subscribe(value =>
            {
                foreach (var item in this.items)
                {
                    item.dimmed.Value = this.dimmedTypes.Value.Contains(item.item.Value.Item.Data.cls);
                }
            });
        }

        public void Dispose()
        {
            items.DisposeAll();
            selectedItem.DisposeAll();
        }

        private void InitInventoryItem(InventoryItem item)
        {
            item.dimmed.Value = dimmedTypes.Value.Contains(item.item.Value.Item.Data.cls);
            item.onClick.Subscribe(SubscribeOnClick);
        }

        private void SubscribeOnClick(InventoryItem inventoryItem)
        {
            if (!ReferenceEquals(selectedItem.Value, null))
            {
                selectedItem.Value.selected.Value = false;
                
                if (selectedItem.Value.item.Value.Item.Data.id == inventoryItem.item.Value.Item.Data.id)
                {
                    selectedItem.Value = null;
                    return;
                }
            }

            selectedItem.Value = inventoryItem;
            selectedItem.Value.selected.Value = true;
        }
    }
}
