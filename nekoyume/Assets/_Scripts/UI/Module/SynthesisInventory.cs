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

        private SynthesizeModel? _selectedModel;
        private Action<InventoryItem>? _onClickItem;

        private readonly List<IDisposable> _disposables = new();
        private readonly List<InventoryItem> _items = new();

        private List<InventoryItem>? _cachedInventoryItems;

        public ReactiveCollection<InventoryItem> SelectedItems { get; } = new();

        private void Awake()
        {
            foreach (Transform sampleChild in cellContainer)
            {
                Destroy(sampleChild);
            }

            SelectedItems.ObserveAdd().Subscribe(item =>
            {
                item.Value.SelectCountEnabled.SetValueAndForceNotify(true);
            }).AddTo(gameObject);

            SelectedItems.ObserveRemove()
                .Subscribe(item => item.Value.SelectCountEnabled.SetValueAndForceNotify(false))
                .AddTo(gameObject);
        }

        private void OnEnable()
        {
            ReactiveAvatarState.Inventory
                .Subscribe(UpdateInventory)
                .AddTo(_disposables);
        }

        private void OnDisable()
        {
            _disposables.DisposeAllAndClear();
        }

        public void Show(SynthesizeModel requiredItem)
        {
            ClearSelectedItems();
            _selectedModel = requiredItem;
            _cachedInventoryItems = GetModels(requiredItem);
            scroll.UpdateData(_cachedInventoryItems, true);
        }

        public void SetInventory(Action<InventoryItem> onClickItem)
        {
            _onClickItem = onClickItem;
            scroll.OnClick.Subscribe(SelectItem).AddTo(gameObject);
        }

        private void SetRequiredItem(SynthesizeModel requiredModel)
        {
            _cachedInventoryItems = GetModels(requiredModel);
            var model = _cachedInventoryItems.FirstOrDefault();
            if (model == null)
            {
                NcDebug.LogError("model is null.");
                return;
            }

            scroll.UpdateData(_cachedInventoryItems, true);
            SelectItem(model);
        }

        public bool SelectAutoSelectItems(SynthesizeModel model)
        {
            _cachedInventoryItems ??= GetModels(model);

            var itemCount = _cachedInventoryItems.Count();
            var selectedCount = SelectedItems.Count;
            var remainder = selectedCount % model.RequiredItemCount;
            if (itemCount - (selectedCount - remainder) < model.RequiredItemCount)
            {
                // 이미 선택된 아이템을 제외하고 필요 수량만큼 선택할 여분이 없으면 아이템 추가 선택 안함
                return false;
            }

            var selectCount = model.RequiredItemCount;
            // 이미 선택된 아이템이 있으면 그 아이템 포함하여 지정된 갯수가 되도록 셋팅
            var i = selectedCount % model.RequiredItemCount;

            var synthesisCount = selectedCount / selectCount;
            if (synthesisCount >= Synthesis.MaxSynthesisCount)
            {
                Synthesis.NotificationMaxSynthesisCount(model.RequiredItemCount);
                return false;
            }

            foreach (var cachedItem in _cachedInventoryItems)
            {
                if (cachedItem.SelectCountEnabled.Value)
                {
                    continue;
                }

                SelectItem(cachedItem);
                i++;

                if (i >= selectCount)
                {
                    break;
                }
            }

            return i >= selectCount;
        }

        public bool SelectAutoSelectAllItems(SynthesizeModel model)
        {
            ClearSelectedItems();
            _cachedInventoryItems ??= GetModels(model);

            var itemCount = _cachedInventoryItems.Count();
            if (itemCount < model.RequiredItemCount)
            {
                return false;
            }

            var selectCount = itemCount - (itemCount % model.RequiredItemCount);
            selectCount = Math.Min(selectCount, Synthesis.MaxSynthesisCount * model.RequiredItemCount);
            for (var i = 0; i < selectCount; ++i)
            {
                SelectItem(_cachedInventoryItems[i]);
            }

            return true;
        }

        private List<InventoryItem> GetModels(SynthesizeModel requiredItem)
        {
            // get from _items by required item's condition
            var items = _items.Where(item =>
                (Grade)item.ItemBase.Grade == requiredItem.Grade &&
                item.ItemBase.ItemSubType == requiredItem.ItemSubType &&
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

        public void ClearSelectedItems()
        {
            foreach (var selectedItem in SelectedItems)
            {
                selectedItem.SelectCountEnabled.SetValueAndForceNotify(false);
            }

            SelectedItems.Clear();
        }

        private void SelectItem(InventoryItem item)
        {
            _onClickItem?.Invoke(item);

            if (SelectedItems.Contains(item))
            {
                SelectedItems.Remove(item);
                return;
            }

            if (SelectedItems.Count >= Synthesis.MaxSynthesisCount * _selectedModel!.RequiredItemCount)
            {
                Synthesis.NotificationMaxSynthesisCount(_selectedModel.RequiredItemCount);
                return;
            }

            SelectedItems.Add(item);
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
            if (inventory == null || _selectedModel == null)
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

            SetRequiredItem(_selectedModel);
        }

        private void AddItem(ItemBase itemBase, int count = 1)
        {
            if (_selectedModel == null)
            {
                return;
            }

            if (itemBase is ITradableItem tradableItem)
            {
                var blockIndex = Game.Game.instance.Agent?.BlockIndex ?? -1;
                if (tradableItem.RequiredBlockIndex > blockIndex)
                {
                    return;
                }
            }

            if (_selectedModel.ItemSubType != itemBase.ItemSubType)
            {
                return;
            }

            if (_selectedModel.Grade != (Grade)itemBase.Grade)
            {
                return;
            }

            var inventoryItem = new InventoryItem(
                itemBase,
                count,
                !Util.IsUsableItem(itemBase),
                false);
            _items.Add(inventoryItem);
        }

#endregion
    }
}
