#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Battle;
using Nekoyume.Helper;
using Nekoyume.Model.EnumType;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    using UniRx;

    public class SynthesisInventory : MonoBehaviour
    {
        [SerializeField] private InventoryScroll scroll = null!;
        [SerializeField] private Transform cellContainer = null!;

        private SynthesisMaterial _requiredItem;
        private Action<InventoryItem>? _onClickItem;
        private CollectionMaterial[]? _requiredItems;

        private readonly List<InventoryItem> _items = new();

        public InventoryItem? SelectedItem { get; private set; }

        private void Awake()
        {
            foreach (Transform sampleChild in cellContainer)
            {
                Destroy(sampleChild);
            }
        }

        public void SetInventory(Action<InventoryItem> onClickItem)
        {
            _onClickItem = onClickItem;

            ReactiveAvatarState.Inventory
                .Subscribe(UpdateInventory)
                .AddTo(gameObject);

            scroll.OnClick.Subscribe(SelectItem).AddTo(gameObject);
        }

        public void SetRequiredItem(SynthesisMaterial requiredItem)
        {
            _requiredItem = requiredItem;

            var models = GetModels(_requiredItem);
            var model = models.FirstOrDefault();
            if (model == null)
            {
                NcDebug.LogError("model is null.");
                return;
            }

            scroll.UpdateData(models, true);
            SelectItem(model);
        }

        private List<InventoryItem> GetModels(SynthesisMaterial requiredItem)
        {
            // get from _items by required item's condition
            var items = _items.Where(item =>
                (Grade)item.ItemBase.Grade == requiredItem.Grade &&
                item.ItemBase.ItemType == requiredItem.ItemType &&
                item.ItemBase is INonFungibleItem).ToList();
            if (!items.Any())
            {
                return items;
            }

            switch (items.First().ItemBase.ItemType)
            {
                case ItemType.Equipment:
                    items = items
                        .OrderBy(item => CPHelper.GetCP(item.ItemBase as Equipment)).ToList();
                    UpdateEquipmentEquipped(items);
                    break;
                case ItemType.Costume:
                    UpdateCostumeEquipped(items);
                    break;
            }

            return items;
        }

        private void SelectItem(InventoryItem item)
        {
            SelectedItem?.CollectionSelected.SetValueAndForceNotify(false);
            SelectedItem = item;
            SelectedItem.CollectionSelected.SetValueAndForceNotify(true);
            _onClickItem?.Invoke(SelectedItem);
        }

        private static void UpdateEquipmentEquipped(List<InventoryItem> equipments)
        {
            var equippedEquipments = new List<Guid>();
            for (var i = 1; i < (int)BattleType.End; i++)
            {
                equippedEquipments.AddRange(States.Instance.CurrentItemSlotStates[(BattleType)i].Equipments);
            }

            foreach (var equipment in equipments)
            {
                var equipped =
                    equippedEquipments.Exists(x => x == ((Equipment)equipment.ItemBase).ItemId);
                equipment.Equipped.SetValueAndForceNotify(equipped);
            }
        }

        private static void UpdateCostumeEquipped(List<InventoryItem> costumes)
        {
            var equippedCostumes = new List<Guid>();
            for (var i = 1; i < (int)BattleType.End; i++)
            {
                equippedCostumes.AddRange(States.Instance.CurrentItemSlotStates[(BattleType)i].Costumes);
            }

            foreach (var costume in costumes)
            {
                var equipped =
                    equippedCostumes.Exists(x => x == ((Costume)costume.ItemBase).ItemId);
                costume.Equipped.SetValueAndForceNotify(equipped);
            }
        }

#region Update Inventory

        private void UpdateInventory(Nekoyume.Model.Item.Inventory? inventory)
        {
            _items.Clear();
            if (inventory == null)
            {
                return;
            }

            foreach (var item in inventory.Items)
            {
                if (item.Locked)
                {
                    continue;
                }

                AddItem(item.item, item.count);
            }

            SetRequiredItem(_requiredItem);
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

            switch (itemBase.ItemType)
            {
                case ItemType.Costume:
                case ItemType.Equipment:
                    var inventoryItem = new InventoryItem(
                        itemBase,
                        count,
                        !Util.IsUsableItem(itemBase),
                        false);
                    _items.Add(inventoryItem);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

#endregion
    }
}
