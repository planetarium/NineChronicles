using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;
using Material = Nekoyume.Model.Item.Material;

namespace Nekoyume.UI.Module
{
    using UniRx;
    public class CollectionInventory : MonoBehaviour
    {
        [SerializeField] private InventoryScroll scroll;

        private CollectionMaterial _requiredItem;
        private Action<InventoryItem> _onClickItem;
        private readonly List<InventoryItem> _items = new List<InventoryItem>();
        private readonly List<IDisposable> _disposablesOnSet = new();

        public InventoryItem SelectedItem { get; private set; }

        public void SetInventory(
            Action<InventoryItem> onClickItem,
            Action<CollectionInventory, Nekoyume.Model.Item.Inventory> onUpdateInventory = null)
        {
            _onClickItem = onClickItem;

            ReactiveAvatarState.Inventory
                .Subscribe(e => { UpdateInventory(e, onUpdateInventory); })
                .AddTo(_disposablesOnSet);

            scroll.OnClick.Subscribe(OnClickItem).AddTo(_disposablesOnSet);
        }

        public void SetRequiredItem(CollectionMaterial requiredItem)
        {
            ClearSelectedItem();
            _requiredItem = requiredItem;

            var models = GetModels(_requiredItem);
            scroll.UpdateData(models, true);
        }

        private void UpdateInventory(
            Nekoyume.Model.Item.Inventory inventory,
            Action<CollectionInventory, Nekoyume.Model.Item.Inventory> onUpdateInventory = null)
        {
            _items.Clear();
            if (inventory == null)
            {
                return;
            }

            foreach (var item in inventory.Items)
            {
                AddItem(item.item, item.count);
            }

            var models = GetModels(_requiredItem);
            scroll.UpdateData(models, true);
            onUpdateInventory?.Invoke(this, inventory);

        }

        private void AddItem(ItemBase itemBase, int count = 1)
        {
            if (itemBase is ITradableItem tradableItem)
            {
                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                if (tradableItem.RequiredBlockIndex > blockIndex)
                {
                    return;
                }
            }

            InventoryItem inventoryItem;
            switch (itemBase.ItemType)
            {
                case ItemType.Consumable:
                case ItemType.Costume:
                case ItemType.Equipment:
                    inventoryItem = new InventoryItem(
                        itemBase,
                        count,
                        !Util.IsUsableItem(itemBase),
                        false);
                    _items.Add(inventoryItem);
                    break;
                case ItemType.Material:
                    var material = (Material)itemBase;
                    if (TryGetMaterial(material, out inventoryItem))
                    {
                        inventoryItem.Count.Value += count;
                    }
                    else
                    {
                        inventoryItem = new InventoryItem(
                            itemBase,
                            count,
                            false,
                            false);
                        _items.Add(inventoryItem);
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private bool TryGetMaterial(Material material, out InventoryItem model)
        {
            model = _items.FirstOrDefault(item =>
                item.ItemBase is Material m && m.ItemId.Equals(material.ItemId));

            return model != null;
        }

        private void OnClickItem(InventoryItem item)
        {
            if (SelectedItem == null)
            {
                SelectedItem = item;
                SelectedItem.Selected.SetValueAndForceNotify(true);
                _onClickItem?.Invoke(SelectedItem);
            }
            else
            {
                if (SelectedItem.Equals(item))
                {
                    SelectedItem.Selected.SetValueAndForceNotify(false);
                    SelectedItem = null;
                }
                else
                {
                    SelectedItem.Selected.SetValueAndForceNotify(false);
                    SelectedItem = item;
                    SelectedItem.Selected.SetValueAndForceNotify(true);
                    _onClickItem?.Invoke(SelectedItem);
                }
            }
        }

        private void ClearSelectedItem()
        {
            SelectedItem?.Selected.SetValueAndForceNotify(false);
            SelectedItem = null;
        }

        private List<InventoryItem> GetModels(CollectionMaterial requiredItem)
        {
            var items = _items.Where(item => item.ItemBase.Id == requiredItem.Row.ItemId);
            items = items.First().ItemBase.ItemType == ItemType.Equipment
                ? items.Where(item => ((Equipment)item.ItemBase).level == requiredItem.Row.Level)
                : items.Where(item => item.Count.Value >= requiredItem.Row.Count);
            // items = items.Where(item => ((Equipment)item.ItemBase).GetOptionCount() == requiredItem.OptionCount);
            // items = items.Where(item => ((Equipment)item.ItemBase).Skills.Any() == requiredItem.SkillContains);

            var usableItems = new List<InventoryItem>();
            var unusableItems = new List<InventoryItem>();
            foreach (var item in items)
            {
                if (Util.IsUsableItem(item.ItemBase))
                {
                    usableItems.Add(item);
                }
                else
                {
                    unusableItems.Add(item);
                }
            }

            usableItems.AddRange(unusableItems);
            return usableItems;
        }
    }
}
