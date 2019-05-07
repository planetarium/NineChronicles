using System;
using System.Collections.Generic;
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

        public readonly ReactiveProperty<InventoryAndSelectedItemInfo> inventoryAndSelectedItemInfo
            = new ReactiveProperty<InventoryAndSelectedItemInfo>();

        public readonly ReactiveProperty<SelectItemCountPopup> selectItemCountPopup =
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

            inventoryAndSelectedItemInfo.Value = new InventoryAndSelectedItemInfo(items, DimmedTypes);
            inventoryAndSelectedItemInfo.Value.selectedItemInfo.Value.buttonText.Value = "조합 리스트에 올리기";

            selectItemCountPopup.Value = new SelectItemCountPopup();

            inventoryAndSelectedItemInfo.Value.selectedItemInfo.Value.onClick.Subscribe(OnSelectedItemInfoOnClick);
            selectItemCountPopup.Value.onClickSubmit.Subscribe(OnSelectItemCountPopupOnClickSubmit);
            stagedItems.ObserveAdd().Subscribe(OnStagedItemsAdd);
            stagedItems.ObserveRemove().Subscribe(OnStagedItemsRemove);
            resultPopup.Subscribe(OnResultPopup);
        }

        public void Dispose()
        {
            inventoryAndSelectedItemInfo.Dispose();
            selectItemCountPopup.Dispose();
            stagedItems.DisposeAll();
            readyForCombination.Dispose();
            resultPopup.Dispose();

            onClickCombination.Dispose();
        }

        private void OnSelectedItemInfoOnClick(ItemInfo data)
        {
            if (ReferenceEquals(data, null) ||
                ReferenceEquals(data.item.Value, null))
            {
                return;
            }

            selectItemCountPopup.Value.item.Value = data.item.Value;
            selectItemCountPopup.Value.count.Value = 1;
            selectItemCountPopup.Value.minCount.Value = 1;
            selectItemCountPopup.Value.maxCount.Value = data.item.Value.count.Value;
        }

        private void OnSelectItemCountPopupOnClickSubmit(SelectItemCountPopup data)
        {
            if (ReferenceEquals(data, null) ||
                ReferenceEquals(data.item.Value, null))
            {
                selectItemCountPopup.Value.item.Value = null;
                return;
            }

            if (data.count.Value <= 0)
            {
                selectItemCountPopup.Value.item.Value = null;
                return;
            }

            foreach (var stagedItem in stagedItems)
            {
                if (stagedItem.item.Value.Item.Data.id != data.item.Value.item.Value.Item.Data.id)
                {
                    continue;
                }

                stagedItem.count.Value = data.count.Value;
                selectItemCountPopup.Value.item.Value = null;
                return;
            }

            if (stagedItems.Count >= _stagedItemsLimit)
            {
                selectItemCountPopup.Value.item.Value = null;
                return;
            }

            var item = new CountEditableItem(data.item.Value.item.Value, data.count.Value, "수정");
            stagedItems.Add(item);

            selectItemCountPopup.Value.item.Value = null;
        }

        private void OnStagedItemsAdd(CollectionAddEvent<CountEditableItem> e)
        {
            var data = e.Value;
            data.count.Subscribe(count => UpdateReadyForCombination());
            data.onEdit.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null) ||
                    !ReferenceEquals(selectItemCountPopup.Value.item.Value, null))
                {
                    return;
                }

                selectItemCountPopup.Value.item.Value = obj;
                selectItemCountPopup.Value.count.Value = obj.count.Value;
                selectItemCountPopup.Value.minCount.Value = 1;
                selectItemCountPopup.Value.maxCount.Value = obj.item.Value.Count;
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
//            data.Count.Dispose();
//            data.OnEdit.Dispose();
//            data.OnClose.Dispose();
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
            inventoryAndSelectedItemInfo.Value.RemoveFromInventory(data.materialItems);

            // 결과 아이템이 있다면, 인벤토리에 추가.
            if (!ReferenceEquals(resultPopup.Value.item.Value, null))
            {
                inventoryAndSelectedItemInfo.Value.AddToInventory(data);
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
            foreach (var item in inventoryAndSelectedItemInfo.Value.inventory.Value.items)
            {
                if (item.item.Value.Item.Data.id != id)
                {
                    continue;
                }

                item.covered.Value = isStaged;
                item.dimmed.Value = isStaged;
            }
            
            if (!ReferenceEquals(inventoryAndSelectedItemInfo.Value.selectedItemInfo.Value.item.Value, null) &&
                inventoryAndSelectedItemInfo.Value.selectedItemInfo.Value.item.Value.item.Value.Item.Data.id == id)
            {
                inventoryAndSelectedItemInfo.Value.selectedItemInfo.Value.buttonEnabled.Value = !isStaged;
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
