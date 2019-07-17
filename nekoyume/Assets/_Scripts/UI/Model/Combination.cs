using System;
using System.Collections.Generic;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.Manager;
using Nekoyume.Game.Item;
using Nekoyume.UI.Module;
using UniRx;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class Combination : IDisposable
    {
        private static readonly string[] DimmedTypes =
        {
            nameof(ItemBase.ItemType.Weapon),
            nameof(ItemBase.ItemType.RangedWeapon),
            nameof(ItemBase.ItemType.Armor),
            nameof(ItemBase.ItemType.Belt),
            nameof(ItemBase.ItemType.Necklace),
            nameof(ItemBase.ItemType.Ring),
            nameof(ItemBase.ItemType.Helm),
            nameof(ItemBase.ItemType.Set),
            nameof(ItemBase.ItemType.Food),
            nameof(ItemBase.ItemType.Shoes),
        };

        public readonly ReactiveProperty<Inventory> inventory
            = new ReactiveProperty<Inventory>();

        public readonly ReactiveProperty<ItemInfo> itemInfo
            = new ReactiveProperty<ItemInfo>();

        public readonly ReactiveProperty<SimpleItemCountPopup> itemCountPopup =
            new ReactiveProperty<SimpleItemCountPopup>();

        public readonly ReactiveCollection<CombinationMaterial> materials =
            new ReactiveCollection<CombinationMaterial>();

        public readonly ReactiveProperty<int> openedMaterialCount = new ReactiveProperty<int>();

        public readonly ReactiveProperty<bool> readyForCombination = new ReactiveProperty<bool>();

        public readonly ReactiveProperty<CombinationResultPopup> resultPopup =
            new ReactiveProperty<CombinationResultPopup>();

        public readonly Subject<Combination> onClickCombination = new Subject<Combination>();
        public readonly Subject<CombinationResultPopup> onShowResultVFX = new Subject<CombinationResultPopup>();

        public bool IsMaterialsFulled => materials.Count >= openedMaterialCount.Value;

        public Combination(Game.Item.Inventory inventory, int materialCount)
        {
            this.inventory.Value = new Inventory(inventory);
            this.inventory.Value.dimmedFunc.Value = DimmedFunc;
            itemInfo.Value = new ItemInfo();
            itemInfo.Value.buttonText.Value = LocalizationManager.Localize("UI_COMBINATION_SELECT_MATERIAL");
            itemInfo.Value.buttonEnabledFunc.Value = ButtonEnabledFunc;
            itemCountPopup.Value = new SimpleItemCountPopup();
            itemCountPopup.Value.titleText.Value = LocalizationManager.Localize("UI_COMBINATION_SELECT_COUNT_OF_MATERIAL");;
            openedMaterialCount.Value = materialCount;

            this.inventory.Value.selectedItemView.Subscribe(OnInventorySelectedItem);
            itemCountPopup.Value.onClickSubmit.Subscribe(OnClickSubmitItemCountPopup);
            materials.ObserveAdd().Subscribe(OnMaterialsAdd);
            materials.ObserveRemove().Subscribe(OnMaterialsRemove);
            resultPopup.Subscribe(OnResultPopup);
        }

        public void Dispose()
        {
            inventory.DisposeAll();
            itemInfo.DisposeAll();
            itemCountPopup.DisposeAll();
            materials.DisposeAll();
            openedMaterialCount.Dispose();
            readyForCombination.Dispose();
            resultPopup.DisposeAll();

            onClickCombination.Dispose();
            onShowResultVFX.Dispose();
        }

        private bool DimmedFunc(InventoryItem inventoryItem)
        {
            return DimmedTypes.Contains(inventoryItem.item.Value.Data.cls);
        }

        private bool ButtonEnabledFunc(InventoryItem inventoryItem)
        {
            return false;
        }

        private void OnInventorySelectedItem(InventoryItemView view)
        {
            if (view is null)
            {
                itemInfo.Value.item.Value = null;
                
                return;
            }
            
            itemInfo.Value.item.Value = view.Model;
        }

        private void OnClickSubmitItemCountPopup(SimpleItemCountPopup data)
        {
            if (ReferenceEquals(data, null)
                || ReferenceEquals(data.item.Value, null))
            {
                itemCountPopup.Value.item.Value = null;
                return;
            }

            RegisterToStagedItems(data.item.Value);
            itemCountPopup.Value.item.Value = null;
        }

        public bool RegisterToStagedItems(CountableItem countEditableItem)
        {
            if (ReferenceEquals(countEditableItem, null))
            {
                return false;
            }

            foreach (var material in materials)
            {
                if (material.item.Value.Data.id != countEditableItem.item.Value.Data.id)
                {
                    continue;
                }

                if (countEditableItem.count.Value == 0)
                {
                    materials.Remove(material);
                }
                else
                {
                    material.count.Value = countEditableItem.count.Value;
                }

                return true;
            }

            if (materials.Count >= openedMaterialCount.Value)
            {
                return false;
            }

            materials.Add(new CombinationMaterial(
                countEditableItem.item.Value,
                1,
                0,
                countEditableItem.count.Value,
                false));
            return true;
        }

        private void OnMaterialsAdd(CollectionAddEvent<CombinationMaterial> e)
        {
            var data = e.Value;
            data.count.Subscribe(count => UpdateReadyForCombination());
            data.onEdit.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }

                itemCountPopup.Value.item.Value = obj;
                itemCountPopup.Value.item.Value.minCount.Value = 1;
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickCombinationEditMaterialItem);
            });
            data.onMinus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }

                if (obj.count.Value > 1)
                {
                    obj.count.Value--;
                }
            });
            data.onPlus.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }
                
                if (obj.count.Value < obj.maxCount.Value)
                {
                    obj.count.Value++;
                }
            });
            data.onDelete.Subscribe(obj =>
            {
                if (!(obj is CombinationMaterial material))
                {
                    return;
                }

                materials.Remove(material);
                AnalyticsManager.Instance.OnEvent(AnalyticsManager.EventName.ClickCombinationRemoveMaterialItem);
            });

            SetStaged(data.item.Value.Data.id, true);
            UpdateReadyForCombination();
        }

        private void OnMaterialsRemove(CollectionRemoveEvent<CombinationMaterial> e)
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
            // 재료 아이템들을 인벤토리에서 제거하기.
            inventory.Value.RemoveFungibleItems(data.materialItems);

            // 결과 아이템이 있다면, 인벤토리에 추가하고 해당 아이템을 선택하기.
            if (!ReferenceEquals(data.item.Value, null))
            {
                var addedItem = inventory.Value.AddNonFungibleItem((ItemUsable) data.item.Value);
            }

            while (materials.Count > 0)
            {
                materials.RemoveAt(0);
            }

            onShowResultVFX.OnNext(resultPopup.Value);

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
                itemInfo.Value.buttonEnabled.Value = itemInfo.Value.buttonEnabledFunc.Value(itemInfo.Value.item.Value);
            }
        }

        private void UpdateReadyForCombination()
        {
            using (var e = materials.GetEnumerator())
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
