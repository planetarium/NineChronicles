using System;
using System.Collections.Generic;
using Nekoyume.Game.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    public class Inventory : IDisposable
    {
        public readonly ReactiveCollection<InventoryItem> items = new ReactiveCollection<InventoryItem>();
        public readonly ReactiveProperty<InventoryItemView> selectedItemView = new ReactiveProperty<InventoryItemView>(null);
        public readonly ReactiveProperty<Func<InventoryItem, bool>> dimmedFunc = new ReactiveProperty<Func<InventoryItem, bool>>();
        public readonly ReactiveProperty<Func<InventoryItem, ItemBase.ItemType, bool>> glowedFunc = new ReactiveProperty<Func<InventoryItem, ItemBase.ItemType, bool>>();

        public readonly Subject<InventoryItemView> onDoubleClickItemView = new Subject<InventoryItemView>();
        
        public Inventory(Game.Item.Inventory inventory)
        {
            dimmedFunc.Value = DimmedFunc;
            glowedFunc.Value = GlowedFunc;

            foreach (var item in inventory.Items)
            {
                var inventoryItem = new InventoryItem(item.item, item.count);
                InitInventoryItem(inventoryItem);
                items.Add(inventoryItem);
            }
            
            items.ObserveAdd().Subscribe(added =>
            {
                InitInventoryItem(added.Value);
            });
            items.ObserveRemove().Subscribe(removed => removed.Value.Dispose());
            
            dimmedFunc.Subscribe(func =>
            {
                if (dimmedFunc.Value == null)
                {
                    dimmedFunc.Value = DimmedFunc;
                }
                
                foreach (var item in items)
                {
                    item.dimmed.Value = dimmedFunc.Value(item);
                }
            });
        }

        public void Dispose()
        {
            items.DisposeAll();
            selectedItemView.Dispose();
            dimmedFunc.Dispose();
            glowedFunc.Dispose();
            
            onDoubleClickItemView.Dispose();
        }

        public InventoryItem AddFungibleItem(ItemBase itemBase, int count)
        {
            if (TryGetFungibleItem(itemBase, out var inventoryItem))
            {
                inventoryItem.count.Value += count;
                return inventoryItem;
            }

            inventoryItem = new InventoryItem(itemBase, count);
            items.Add(inventoryItem);
            return inventoryItem;
        }
        
        // Todo. NonFungibleItem 개발 후 `ItemUsable itemBase` 인자를 `NonFungibleItem nonFungibleItem`로 수정.
        public InventoryItem AddNonFungibleItem(ItemUsable itemBase)
        {
            var inventoryItem = new InventoryItem(itemBase, 1);
            items.Add(inventoryItem);
            return inventoryItem;
        }

        public void RemoveFungibleItems(IEnumerable<CountEditableItem> collection)
        {
            foreach (var countEditableItem in collection)
            {
                if (ReferenceEquals(countEditableItem, null))
                {
                    continue;
                }

                RemoveFungibleItem(countEditableItem.item.Value.Data.id, countEditableItem.count.Value);
            }
        }

        public bool RemoveFungibleItem(int id, int count = 1)
        {
            if (!TryGetFungibleItem(id, out var outFungibleItem) ||
                outFungibleItem.count.Value < count)
            {
                return false;
            }

            outFungibleItem.count.Value -= count;
            if (outFungibleItem.count.Value == 0)
            {
                items.Remove(outFungibleItem);
            }

            return true;
        }
        
        // Todo. NonFungibleItem 개발 후 `ItemUsable itemUsable` 인자를 `NonFungibleItem nonFungibleItem`로 수정.
        public bool RemoveNonFungibleItem(ItemUsable itemUsable)
        {
            return TryGetNonFungibleItem(itemUsable, out var outFungibleItem) && items.Remove(outFungibleItem);
        }

        public void DeselectAll()
        {
            if (ReferenceEquals(selectedItemView.Value, null))
            {
                return;
            }

            selectedItemView.Value.Model.selected.Value = false;
            selectedItemView.Value = null;
        }
        
        public void SubscribeOnClick(InventoryItemView view)
        {
            if (!ReferenceEquals(selectedItemView.Value, null) &&
                !ReferenceEquals(selectedItemView.Value.Model, null))
            {
                selectedItemView.Value.Model.selected.Value = false;
            }

            selectedItemView.SetValueAndForceNotify(view);
            selectedItemView.Value.Model.selected.Value = true;

            foreach (var item in items)
            {
                item.glowed.Value = false;
            }
        }
        
        private bool TryGetFungibleItem(ItemBase itemBase, out InventoryItem outInventoryItem)
        {
            return TryGetFungibleItem(itemBase.Data.id, out outInventoryItem);
        }
        
        private bool TryGetFungibleItem(int id, out InventoryItem outFungibleItem)
        {
            foreach (var fungibleItem in items)
            {
                if (fungibleItem.item.Value.Data.id != id)
                {
                    continue;
                }
                
                outFungibleItem = fungibleItem;
                return true;
            }

            outFungibleItem = null;
            return false;
        }
        
        // Todo. NonFungibleItem 개발 후 `ItemUsable itemUsable` 인자를 `NonFungibleItem nonFungibleItem`로 수정.
        private bool TryGetNonFungibleItem(ItemUsable itemUsable, out InventoryItem outNonFungibleItem)
        {
            foreach (var fungibleItem in items)
            {
                if (fungibleItem.item.Value.Data.id != itemUsable.Data.id)
                {
                    continue;
                }
                
                outNonFungibleItem = fungibleItem;
                return true;
            }

            outNonFungibleItem = null;
            return false;
        }

        private void InitInventoryItem(InventoryItem item)
        {
            item.dimmed.Value = dimmedFunc.Value(item);
            item.onClick.Subscribe(SubscribeOnClick);
            item.onDoubleClick.Subscribe(onDoubleClickItemView);
        }
        
        private bool DimmedFunc(InventoryItem inventoryItem)
        {
            return false;
        }

        private bool GlowedFunc(InventoryItem inventoryItem, Game.Item.ItemBase.ItemType type)
        {
            return false;
        }
    }
}
