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

        public InventoryItem SelectedItem { get; private set; }

        public void SetInventory(Action<InventoryItem> onClickItem)
        {
            _onClickItem = onClickItem;

            ReactiveAvatarState.Inventory.Subscribe(UpdateInventory).AddTo(gameObject);

            scroll.OnClick.Subscribe(OnClickItem).AddTo(gameObject);
        }

        public void SetRequiredItem(CollectionMaterial requiredItem)
        {
            _requiredItem = requiredItem;
            // Todo : Click First Item
            OnClickItem(null);

            scroll.UpdateData(GetModels(_requiredItem), true);
        }

        private void UpdateInventory(Nekoyume.Model.Item.Inventory inventory)
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

            scroll.UpdateData(GetModels(_requiredItem), true);
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
            if (item == null)
            {
                return;
            }

            SelectedItem?.CollectionSelected.SetValueAndForceNotify(false);
            SelectedItem = item;
            SelectedItem.CollectionSelected.SetValueAndForceNotify(true);
            _onClickItem?.Invoke(SelectedItem);
        }

        private List<InventoryItem> GetModels(CollectionMaterial requiredItem)
        {
            // get from _items by required item's condition
            var row = requiredItem.Row;
            var items = _items.Where(item => item.ItemBase.Id == row.ItemId);
            items = items.First().ItemBase.ItemType == ItemType.Equipment
                ? items.Where(item => ((Equipment)item.ItemBase).level == row.Level)
                : items.Where(item => item.Count.Value >= row.Count);
            // items = items.Where(item => ((Equipment)item.ItemBase).GetOptionCount() == row.OptionCount);
            // items = items.Where(item => ((Equipment)item.ItemBase).Skills.Any() == row.SkillContains);

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
