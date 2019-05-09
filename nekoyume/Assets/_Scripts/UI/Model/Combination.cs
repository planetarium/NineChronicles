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

        public readonly ReactiveProperty<InventoryAndItemInfo> inventoryAndItemInfo
            = new ReactiveProperty<InventoryAndItemInfo>();

        public readonly ReactiveProperty<SelectItemCountPopup> itemCountPopup =
            new ReactiveProperty<SelectItemCountPopup>();

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

            inventoryAndItemInfo.Value = new InventoryAndItemInfo(items);
            inventoryAndItemInfo.Value.inventory.Value.dimmedFunc.Value = DimmedFunc;
            inventoryAndItemInfo.Value.itemInfo.Value.buttonText.Value = "조합 리스트에 올리기";
            inventoryAndItemInfo.Value.itemInfo.Value.buttonEnabledFunc.Value = ButtonEnabledFunc;

            itemCountPopup.Value = new SelectItemCountPopup();

            inventoryAndItemInfo.Value.itemInfo.Value.onClick.Subscribe(OnClickItemInfo);
            itemCountPopup.Value.onClickSubmit.Subscribe(OnSelectItemCountPopupOnClickSubmit);
            stagedItems.ObserveAdd().Subscribe(OnStagedItemsAdd);
            stagedItems.ObserveRemove().Subscribe(OnStagedItemsRemove);
            resultPopup.Subscribe(OnResultPopup);
        }

        public void Dispose()
        {
            inventoryAndItemInfo.Dispose();
            itemCountPopup.Dispose();
            stagedItems.DisposeAll();
            readyForCombination.Dispose();
            resultPopup.Dispose();

            onClickCombination.Dispose();
        }

        private bool DimmedFunc(InventoryItem inventoryItem)
        {
            return DimmedTypes.Contains(inventoryItem.item.Value.Item.Data.cls);
        }

        private bool ButtonEnabledFunc(InventoryItem inventoryItem)
        {
            if (ReferenceEquals(inventoryItem, null) ||
                inventoryItem.dimmed.Value)
            {
                return false;
            }

            var id = inventoryItem.item.Value.Item.Data.id; 
            foreach (var stagedItem in stagedItems)
            {
                if (id == stagedItem.item.Value.Item.Data.id)
                {
                    return false;
                } 
            }

            return true;
        }

        private void OnClickItemInfo(ItemInfo data)
        {
            if (ReferenceEquals(data, null) ||
                ReferenceEquals(data.item.Value, null))
            {
                return;
            }

            itemCountPopup.Value.item.Value = data.item.Value;
            itemCountPopup.Value.count.Value = 1;
            itemCountPopup.Value.minCount.Value = 1;
            itemCountPopup.Value.maxCount.Value = data.item.Value.count.Value;
        }

        private void OnSelectItemCountPopupOnClickSubmit(SelectItemCountPopup data)
        {
            if (ReferenceEquals(data, null) ||
                ReferenceEquals(data.item.Value, null))
            {
                itemCountPopup.Value.item.Value = null;
                return;
            }

            if (data.count.Value <= 0)
            {
                itemCountPopup.Value.item.Value = null;
                return;
            }

            foreach (var stagedItem in stagedItems)
            {
                if (stagedItem.item.Value.Item.Data.id != data.item.Value.item.Value.Item.Data.id)
                {
                    continue;
                }

                stagedItem.count.Value = data.count.Value;
                itemCountPopup.Value.item.Value = null;
                return;
            }

            if (stagedItems.Count >= _stagedItemsLimit)
            {
                itemCountPopup.Value.item.Value = null;
                return;
            }

            var item = new CountEditableItem(data.item.Value.item.Value, data.count.Value, "수정");
            stagedItems.Add(item);

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
                itemCountPopup.Value.count.Value = obj.count.Value;
                itemCountPopup.Value.minCount.Value = 1;
                itemCountPopup.Value.maxCount.Value = obj.item.Value.Count;
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

            SetStaged(data.item.Value.Item.Data.id, true);
            UpdateReadyForCombination();
        }

        private void OnStagedItemsRemove(CollectionRemoveEvent<CountEditableItem> e)
        {
            var data = e.Value;
            data.Dispose();

            SetStaged(data.item.Value.Item.Data.id, false);
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
            inventoryAndItemInfo.Value.RemoveFromInventory(data.materialItems);

            // 결과 아이템이 있다면, 인벤토리에 추가.
            if (!ReferenceEquals(resultPopup.Value.item.Value, null))
            {
                inventoryAndItemInfo.Value.AddToInventory(data);
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
            foreach (var item in inventoryAndItemInfo.Value.inventory.Value.items)
            {
                if (item.item.Value.Item.Data.id != id)
                {
                    continue;
                }

                item.covered.Value = isStaged;
                item.dimmed.Value = isStaged;
            }
            
            if (!ReferenceEquals(inventoryAndItemInfo.Value.itemInfo.Value.item.Value, null) &&
                inventoryAndItemInfo.Value.itemInfo.Value.item.Value.item.Value.Item.Data.id == id)
            {
                inventoryAndItemInfo.Value.itemInfo.Value.buttonEnabled.Value = !isStaged;
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
