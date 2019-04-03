using System;
using System.Collections.Generic;
using Nekoyume.Game.Item;
using UniRx;

namespace Nekoyume.UI.Model
{
    [Serializable]
    public class Combination : IDisposable
    {
        private static readonly string MaterialString = ItemBase.ItemType.Material.ToString();

        public readonly ReactiveProperty<Inventory> Inventory = new ReactiveProperty<Inventory>();
        public readonly ReactiveProperty<ItemInfo> SelectedItemInfo = new ReactiveProperty<ItemInfo>();

        public readonly ReactiveProperty<SelectItemCountPopup<Inventory.Item>> Popup =
            new ReactiveProperty<SelectItemCountPopup<Inventory.Item>>();

        public readonly ReactiveCollection<CountEditableItem<Inventory.Item>> StagedItems =
            new ReactiveCollection<CountEditableItem<Inventory.Item>>();

        public readonly IReadOnlyReactiveProperty<bool> ReadyForCombination = null;
        
        public int stagedItemsLimit;

        public bool IsStagedItemsFulled => StagedItems.Count >= stagedItemsLimit;

        public Combination(List<Game.Item.Inventory.InventoryItem> items, int stagedItemsLimit)
        {
            this.stagedItemsLimit = stagedItemsLimit;

            ReadyForCombination = StagedItems
                .ObserveCountChanged()
                .Select(count => count >= 2)
                .ToReactiveProperty();

            Inventory.Value = new Inventory(items, MaterialString);
            SelectedItemInfo.Value = new ItemInfo();
            SelectedItemInfo.Value.ButtonText.Value = "조합 리스트에 올리기";
            Popup.Value = new SelectItemCountPopup<Inventory.Item>();

            Inventory.Value.SelectedItem.Subscribe(OnInventorySelectedItem);
            SelectedItemInfo.Value.OnClick.Subscribe(OnSelectedItemInfoOnClick);
            Popup.Value.OnClickSubmit.Subscribe(OnPopupOnClickSubmit);
            StagedItems.ObserveAdd().Subscribe(OnStagedItemsAdd);
            StagedItems.ObserveRemove().Subscribe(OnStagedItemsRemove);
        }

        public void Dispose()
        {
            Inventory.Dispose();
            Inventory.Value.Dispose();
            SelectedItemInfo.Dispose();
            SelectedItemInfo.Value.Dispose();
            StagedItems.DisposeAll();
        }

        private void OnInventorySelectedItem(Model.Inventory.Item data)
        {
            if (ReferenceEquals(data, null))
            {
                return;
            }

            SelectedItemInfo.Value.Item.Value = data;
        }

        private void OnSelectedItemInfoOnClick(ItemInfo data)
        {
            if (ReferenceEquals(data, null) ||
                ReferenceEquals(data.Item.Value, null))
            {
                return;
            }

            Popup.Value.Item.Value = data.Item.Value;
            Popup.Value.Count.Value = 1;
            Popup.Value.MinCount.Value = 1;
            Popup.Value.MaxCount.Value = data.Item.Value.Count;
        }

        private void OnPopupOnClickSubmit(SelectItemCountPopup<Model.Inventory.Item> data)
        {
            if (ReferenceEquals(data, null) ||
                ReferenceEquals(data.Item.Value, null))
            {
                Popup.Value.Item.Value = null;
                return;
            }

            if (data.Count.Value <= 0)
            {
                Popup.Value.Item.Value = null;
                return;
            }
            
            foreach (var stagedItem in StagedItems)
            {
                if (stagedItem.Item.Value.Item.Data.Id == data.Item.Value.Item.Data.Id)
                {
                    stagedItem.Count.Value = data.Count.Value;
                    Popup.Value.Item.Value = null;
                    return;
                }
            }

            if (StagedItems.Count >= stagedItemsLimit)
            {
                Popup.Value.Item.Value = null;
                return;
            }

            var item = new CountEditableItem<Inventory.Item>(data.Item.Value, data.Count.Value, "수정");
            StagedItems.Add(item);

            SelectedItemInfo.Value.ButtonEnabled.Value = false;

            data.Item.Value.Covered.Value = true;
            data.Item.Value.Dimmed.Value = true;

            Popup.Value.Item.Value = null;
        }

        private void OnStagedItemsAdd(CollectionAddEvent<CountEditableItem<Model.Inventory.Item>> e)
        {
            var data = e.Value;
            data.OnEdit.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null) ||
                    !ReferenceEquals(Popup.Value.Item.Value, null))
                {
                    return;
                }
                
                Popup.Value.Item.Value = obj.Item.Value;
                Popup.Value.Count.Value = obj.Count.Value;
                Popup.Value.MinCount.Value = 1;
                Popup.Value.MaxCount.Value = obj.Item.Value.Count;
            });
            data.OnClose.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }

                StagedItems.Remove(obj);
            });
        }

        private void OnStagedItemsRemove(CollectionRemoveEvent<CountEditableItem<Model.Inventory.Item>> e)
        {
            var data = e.Value;

            if (SelectedItemInfo.Value.Item.Value.Item.Data.Id == data.Item.Value.Item.Data.Id)
            {
                SelectedItemInfo.Value.ButtonEnabled.Value = true;
            }

            data.Item.Value.Covered.Value = false;
            data.Item.Value.Dimmed.Value = false;
            
            data.OnEdit.Dispose();
            data.OnClose.Dispose();
        }
    }
}
