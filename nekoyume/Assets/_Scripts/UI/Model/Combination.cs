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

        public readonly ReactiveProperty<SelectItemCountPopup<Inventory.Item>> SelectItemCountPopup =
            new ReactiveProperty<SelectItemCountPopup<Inventory.Item>>();

        public readonly ReactiveCollection<CountEditableItem<Inventory.Item>> StagedItems =
            new ReactiveCollection<CountEditableItem<Inventory.Item>>();

        public readonly ReactiveProperty<bool> ReadyForCombination = new ReactiveProperty<bool>();
        
        public readonly ReactiveProperty<CombinationResultPopup<Inventory.Item>> ResultPopup =
            new ReactiveProperty<CombinationResultPopup<Inventory.Item>>(null);

        public readonly Subject<Combination> OnClickCombination = new Subject<Combination>();
        
        private readonly int _stagedItemsLimit;

        public bool IsStagedItemsFulled => StagedItems.Count >= _stagedItemsLimit;

        public Combination(List<Game.Item.Inventory.InventoryItem> items, int stagedItemsLimit)
        {
            _stagedItemsLimit = stagedItemsLimit;

            Inventory.Value = new Inventory(items, MaterialString);
            SelectedItemInfo.Value = new ItemInfo();
            SelectedItemInfo.Value.ButtonText.Value = "조합 리스트에 올리기";
            SelectItemCountPopup.Value = new SelectItemCountPopup<Inventory.Item>();

            Inventory.Value.SelectedItem.Subscribe(OnInventorySelectedItem);
            SelectedItemInfo.Value.OnClick.Subscribe(OnSelectedItemInfoOnClick);
            SelectItemCountPopup.Value.OnClickSubmit.Subscribe(OnSelectItemCountPopupOnClickSubmit);
            StagedItems.ObserveAdd().Subscribe(OnStagedItemsAdd);
            StagedItems.ObserveRemove().Subscribe(OnStagedItemsRemove);
            ResultPopup.Subscribe(OnResultPopup);
        }

        public void Dispose()
        {
            Inventory.DisposeAll();
            SelectedItemInfo.DisposeAll();
            SelectItemCountPopup.Dispose();
            StagedItems.DisposeAll();
            ReadyForCombination.Dispose();
            ResultPopup.Dispose();
            
            OnClickCombination.Dispose();
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

            SelectItemCountPopup.Value.Item.Value = data.Item.Value;
            SelectItemCountPopup.Value.Count.Value = 1;
            SelectItemCountPopup.Value.MinCount.Value = 1;
            SelectItemCountPopup.Value.MaxCount.Value = data.Item.Value.Count;
        }

        private void OnSelectItemCountPopupOnClickSubmit(SelectItemCountPopup<Model.Inventory.Item> data)
        {
            if (ReferenceEquals(data, null) ||
                ReferenceEquals(data.Item.Value, null))
            {
                SelectItemCountPopup.Value.Item.Value = null;
                return;
            }

            if (data.Count.Value <= 0)
            {
                SelectItemCountPopup.Value.Item.Value = null;
                return;
            }

            foreach (var stagedItem in StagedItems)
            {
                if (stagedItem.Item.Value.Item.Data.id != data.Item.Value.Item.Data.id)
                {
                    continue;
                }
                
                stagedItem.Count.Value = data.Count.Value;
                SelectItemCountPopup.Value.Item.Value = null;
                return;
            }

            if (StagedItems.Count >= _stagedItemsLimit)
            {
                SelectItemCountPopup.Value.Item.Value = null;
                return;
            }

            var item = new CountEditableItem<Inventory.Item>(data.Item.Value, data.Count.Value, "수정");
            StagedItems.Add(item);

            SelectItemCountPopup.Value.Item.Value = null;
        }

        private void OnStagedItemsAdd(CollectionAddEvent<CountEditableItem<Model.Inventory.Item>> e)
        {
            var data = e.Value;
            
            SetStaged(data.Item.Value, true);

            data.Count.Subscribe(count => UpdateReadyForCombination());
            
            data.OnEdit.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null) ||
                    !ReferenceEquals(SelectItemCountPopup.Value.Item.Value, null))
                {
                    return;
                }

                SelectItemCountPopup.Value.Item.Value = obj.Item.Value;
                SelectItemCountPopup.Value.Count.Value = obj.Count.Value;
                SelectItemCountPopup.Value.MinCount.Value = 1;
                SelectItemCountPopup.Value.MaxCount.Value = obj.Item.Value.Count;
            });
            data.OnClose.Subscribe(obj =>
            {
                if (ReferenceEquals(obj, null))
                {
                    return;
                }

                StagedItems.Remove(obj);
            });

            UpdateReadyForCombination();
        }

        private void OnStagedItemsRemove(CollectionRemoveEvent<CountEditableItem<Model.Inventory.Item>> e)
        {
            var data = e.Value;

            SetStaged(data.Item.Value, false);

            data.OnEdit.Dispose();
            data.OnClose.Dispose();

            UpdateReadyForCombination();
        }

        private void OnResultPopup(CombinationResultPopup<Inventory.Item> data)
        {
            if (ReferenceEquals(data, null))
            {
                return;
            }
            
            ResultPopup.Value.OnClickSubmit.Subscribe(OnResultPopupOnClickSubmit);
        }
        
        private void OnResultPopupOnClickSubmit(CombinationResultPopup<Model.Inventory.Item> data)
        {
            // 재료 아이템들을 인벤토리에서 제거.
            RemoveFromInventory(StagedItems);
            
            // 결과 아이템이 있다면, 인벤토리에 추가.
            if (!ReferenceEquals(ResultPopup.Value.ResultItem, null))
            {
                AddToInventory(ResultPopup.Value.ResultItem);
            }
            
            while (StagedItems.Count > 0)
            {
                StagedItems.RemoveAt(0);
            }
            
            ResultPopup.Value.Dispose();
            ResultPopup.Value = null;
        }

        private void SetStaged(Inventory.Item item, bool isStaged)
        {
            if (SelectedItemInfo.Value.Item.Value.Item.Data.id == item.Item.Data.id)
            {
                SelectedItemInfo.Value.ButtonEnabled.Value = !isStaged;
            }

            item.Covered.Value = isStaged;
            item.Dimmed.Value = isStaged;
        }
        
        private void RemoveFromInventory(ICollection<CountEditableItem<Model.Inventory.Item>> items)
        {
            var shouldRemoveItems = new List<CountEditableItem<Model.Inventory.Item>>();
            
            using (var e = items.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (ReferenceEquals(e.Current, null))
                    {
                        continue;
                    }

                    var stagedItem = e.Current;
                    
                    using (var e2 = Inventory.Value.Items.GetEnumerator())
                    {
                        while (e2.MoveNext())
                        {
                            if (ReferenceEquals(e2.Current, null) ||
                                e2.Current.Item.Data.id != stagedItem.Item.Value.Item.Data.id)
                            {
                                continue;
                            }

                            var inventoryItem = e2.Current;
                            inventoryItem.Count -= stagedItem.Count.Value;
                            inventoryItem.RaiseCountChanged();

                            if (inventoryItem.Count == 0)
                            {
                                Inventory.Value.Items.Remove(inventoryItem);
                            }

                            shouldRemoveItems.Add(stagedItem);
                            
                            break;
                        }
                    }
                }
            }
            
            shouldRemoveItems.ForEach(item => items.Remove(item));
        }
        
        private void AddToInventory(Model.Inventory.Item item)
        {
            using (var e = Inventory.Value.Items.GetEnumerator())
            {
                while (e.MoveNext())
                {
                    if (ReferenceEquals(e.Current, null) ||
                        e.Current.Item.Data.id != item.Item.Data.id)
                    {
                        continue;
                    }

                    e.Current.Count += item.Count;
                    e.Current.RaiseCountChanged();
                    return;
                }
            }
            
            Inventory.Value.Items.Add(item);
        }

        private void UpdateReadyForCombination()
        {
            using (var e = StagedItems.GetEnumerator())
            {
                var count = 0;

                while (e.MoveNext())
                {
                    if (ReferenceEquals(e.Current, null))
                    {
                        continue;
                    }

                    count += e.Current.Count.Value;
                }
                
                ReadyForCombination.Value = count >= 2;   
            }
        }
    }
}
