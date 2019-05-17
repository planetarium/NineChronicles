using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Manager;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class Combination : IDisposable
    {
        private static readonly string[] DimmedTypes =
        {
            ItemBase.ItemType.Weapon.ToString(),
            ItemBase.ItemType.RangedWeapon.ToString(),
            ItemBase.ItemType.Armor.ToString(),
            ItemBase.ItemType.Belt.ToString(),
            ItemBase.ItemType.Necklace.ToString(),
            ItemBase.ItemType.Ring.ToString(),
            ItemBase.ItemType.Helm.ToString(),
            ItemBase.ItemType.Set.ToString(),
            ItemBase.ItemType.Food.ToString(),
            ItemBase.ItemType.Shoes.ToString(),
        };

        public readonly ReactiveProperty<Inventory> inventory
            = new ReactiveProperty<Inventory>();
        public readonly ReactiveProperty<ItemInfo> itemInfo
            = new ReactiveProperty<ItemInfo>();

        public readonly ReactiveProperty<SimpleItemCountPopup> itemCountPopup =
            new ReactiveProperty<SimpleItemCountPopup>();

        public readonly ReactiveCollection<CountEditableItem> stagedItems =
            new ReactiveCollection<CountEditableItem>();

        public readonly ReactiveProperty<bool> readyForCombination = new ReactiveProperty<bool>();

        public readonly ReactiveProperty<CombinationResultPopup> resultPopup =
            new ReactiveProperty<CombinationResultPopup>();

        public readonly Subject<Combination> onClickCombination = new Subject<Combination>();

        private readonly int _stagedItemsLimit;

        public bool IsStagedItemsFulled => stagedItems.Count >= _stagedItemsLimit;

        public Combination(List<Game.Item.Inventory.InventoryItem> items, int stagedItemsLimit)
        {
            _stagedItemsLimit = stagedItemsLimit;

            inventory.Value = new Inventory(items);
            inventory.Value.dimmedFunc.Value = DimmedFunc;
            itemInfo.Value = new ItemInfo();
            itemInfo.Value.buttonText.Value = "재료 선택";
            itemInfo.Value.buttonEnabledFunc.Value = ButtonEnabledFunc;
            itemCountPopup.Value = new SimpleItemCountPopup();
            itemCountPopup.Value.titleText.Value = "재료 수량 선택";

            inventory.Value.selectedItem.Subscribe(OnInventorySelectedItem);
            itemInfo.Value.onClick.Subscribe(OnClickItemInfo);
            itemCountPopup.Value.onClickSubmit.Subscribe(OnClickSubmitItemCountPopup);
            stagedItems.ObserveAdd().Subscribe(OnStagedItemsAdd);
            stagedItems.ObserveRemove().Subscribe(OnStagedItemsRemove);
            resultPopup.Subscribe(OnResultPopup);
        }

        public void Dispose()
        {
            inventory.DisposeAll();
            itemInfo.DisposeAll();
            itemCountPopup.DisposeAll();
            stagedItems.DisposeAll();
            readyForCombination.Dispose();
            resultPopup.DisposeAll();

            onClickCombination.Dispose();
        }

        private bool DimmedFunc(InventoryItem inventoryItem)
        {
            return DimmedTypes.Contains(inventoryItem.item.Value.Data.cls);
        }

        private bool ButtonEnabledFunc(InventoryItem inventoryItem)
        {
            if (ReferenceEquals(inventoryItem, null) ||
                inventoryItem.dimmed.Value)
            {
                return false;
            }

            var id = inventoryItem.item.Value.Data.id; 
            foreach (var stagedItem in stagedItems)
            {
                if (id == stagedItem.item.Value.Data.id)
                {
                    return false;
                } 
            }

            return true;
        }
        
        private void OnInventorySelectedItem(InventoryItem data)
        {
            itemInfo.Value.item.Value = data;
        }

        private void OnClickItemInfo(ItemInfo data)
        {
            if (ReferenceEquals(data, null) ||
                ReferenceEquals(data.item.Value, null))
            {
                return;
            }

            itemCountPopup.Value.item.Value = new CountEditableItem(
                data.item.Value.item.Value,
                data.item.Value.count.Value,
                0,
                data.item.Value.count.Value,
                "수정");
        }

        private void OnClickSubmitItemCountPopup(SimpleItemCountPopup data)
        {
            if (ReferenceEquals(data, null) ||
                ReferenceEquals(data.item.Value, null))
            {
                itemCountPopup.Value.item.Value = null;
                return;
            }

            foreach (var stagedItem in stagedItems)
            {
                if (stagedItem.item.Value.Data.id != data.item.Value.item.Value.Data.id)
                {
                    continue;
                }

                if (data.item.Value.count.Value == 0)
                {
                    stagedItems.Remove(stagedItem);
                }
                else
                {
                    stagedItem.count.Value = data.item.Value.count.Value;                    
                }
                
                itemCountPopup.Value.item.Value = null;
                return;
            }

            if (stagedItems.Count >= _stagedItemsLimit)
            {
                itemCountPopup.Value.item.Value = null;
                return;
            }

            stagedItems.Add(data.item.Value);

            itemCountPopup.Value.item.Value = null;
        }

        private void OnStagedItemsAdd(CollectionAddEvent<CountEditableItem> e)
        {
            var data = e.Value;
            data.count.Subscribe(count => UpdateReadyForCombination());
            data.onEdit.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null) ||
                    !ReferenceEquals(itemCountPopup.Value.item.Value, null))
                {
                    return;
                }

                itemCountPopup.Value.item.Value = obj;
                AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ClickCombinationEditMaterialItem);
            });
            data.onClose.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }

                stagedItems.Remove(obj);
                AnalyticsManager.instance.OnEvent(AnalyticsManager.EventName.ClickCombinationRemoveMaterialItem);
            });

            SetStaged(data.item.Value.Data.id, true);
            UpdateReadyForCombination();
        }

        private void OnStagedItemsRemove(CollectionRemoveEvent<CountEditableItem> e)
        {
            var data = e.Value;
            data.Dispose();

            SetStaged(data.item.Value.Data.id, false);
            UpdateReadyForCombination();
        }

        private void OnResultPopup(CombinationResultPopup data)
        {
            if (ReferenceEquals(data, null))
            {
                return;
            }

            resultPopup.Value.onClickSubmit.Subscribe(OnResultPopupOnClickSubmit);
        }

        private void OnResultPopupOnClickSubmit(CombinationResultPopup data)
        {
            // 재료 아이템들을 인벤토리에서 제거.
            inventory.Value.RemoveFromInventory(data.materialItems);

            // 결과 아이템이 있다면, 인벤토리에 추가.
            if (!ReferenceEquals(resultPopup.Value.item.Value, null))
            {
                inventory.Value.AddToInventory(data);
            }

            while (stagedItems.Count > 0)
            {
                stagedItems.RemoveAt(0);
            }

            resultPopup.Value.Dispose();
            resultPopup.Value = null;
        }
        
        private void SetStaged(int id, bool isStaged)
        {
            foreach (var item in inventory.Value.items)
            {
                if (item.item.Value.Data.id != id)
                {
                    continue;
                }

                item.covered.Value = isStaged;
                item.dimmed.Value = isStaged;
            }
            
            if (!ReferenceEquals(itemInfo.Value.item.Value, null) &&
                itemInfo.Value.item.Value.item.Value.Data.id == id)
            {
                itemInfo.Value.buttonEnabled.Value = !isStaged;
            }
        }

        private void UpdateReadyForCombination()
        {
            using (var e = stagedItems.GetEnumerator())
            {
                var count = 0;

                while (e.MoveNext())
                {
                    if (ReferenceEquals(e.Current, null))
                    {
                        continue;
                    }

                    count += e.Current.count.Value;
                }

                readyForCombination.Value = count >= 2;
            }
        }
    }
}
