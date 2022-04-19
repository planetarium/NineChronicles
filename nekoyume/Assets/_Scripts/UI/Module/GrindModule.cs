using System.Collections.Generic;
using System.Linq;
using Libplanet.Assets;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.UI.Model;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using UniRx;
    public class GrindModule : MonoBehaviour
    {
        [SerializeField]
        private Inventory grindInventory;

        [SerializeField]
        private ConditionalCostButton grindButton;

        [SerializeField]
        private Text stakingLevelText;

        [SerializeField]
        private Text stakingBonusText;

        [SerializeField]
        private List<GrindingItemSlot> itemSlots;

        private bool _isInitialized;

        private readonly ReactiveCollection<InventoryItem> _selectedItemsForGrind =
            new ReactiveCollection<InventoryItem>();

        private const int LimitGrindingCount = 10;

        private bool CanGrind => _selectedItemsForGrind.Any() &&
                                 States.Instance.CurrentAvatarState.actionPoint > Grinding.CostAp;

        private void Initialize()
        {
            if (_isInitialized)
            {
                return;
            }

            grindButton.SetCost(ConditionalCostButton.CostType.ActionPoint, 5);
            grindButton.SetCondition(() => CanGrind);
            grindButton.OnSubmitSubject.Subscribe(_ =>
            {
                Action(_selectedItemsForGrind.Select(inventoryItem =>
                    (Equipment) inventoryItem.ItemBase).ToList());
            }).AddTo(gameObject);

            _selectedItemsForGrind.ObserveAdd().Subscribe(item =>
            {
                item.Value.GrindingCount.SetValueAndForceNotify(_selectedItemsForGrind.Count);
                itemSlots[item.Index].UpdateSlot(item.Value);
                item.Value.GrindObjectEnabled.OnNext(true);
            }).AddTo(gameObject);

            _selectedItemsForGrind.ObserveRemove().Subscribe(item =>
            {
                var listSize = _selectedItemsForGrind.Count;
                for (int i = item.Index; i < LimitGrindingCount; i++)
                {
                    if (i < listSize)
                    {
                        _selectedItemsForGrind[i].GrindingCount.SetValueAndForceNotify(i + 1);
                        itemSlots[i].UpdateSlot(_selectedItemsForGrind[i]);
                    }
                    else
                    {
                        itemSlots[i].UpdateSlot();
                    }
                }
                item.Value.GrindingCount.SetValueAndForceNotify(0);
            }).AddTo(gameObject);

            _selectedItemsForGrind.ObserveReset().Subscribe(_ =>
            {
                itemSlots.ForEach(slot => slot.UpdateSlot());
            }).AddTo(gameObject);

            _selectedItemsForGrind.ObserveCountChanged().Subscribe(count =>
            {
                grindButton.Interactable = CanGrind;
            }).AddTo(gameObject);

            ReactiveAvatarState.ActionPoint
                .Subscribe(_ => grindButton.Interactable = CanGrind)
                .AddTo(gameObject);

            itemSlots.ForEach(slot => slot.OnClick.Subscribe(_ =>
            {
                _selectedItemsForGrind.Remove(slot.AssignedItem);
            }));

            _isInitialized = true;
        }

        public void Show()
        {
            Initialize();

            grindInventory.SetGrinding(ShowItemTooltip);
            _selectedItemsForGrind.Clear();
            grindButton.Interactable = CanGrind;
        }

        private void ShowItemTooltip(InventoryItem model, RectTransform target)
        {
            var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
            var isRegister = !_selectedItemsForGrind.Contains(model);
            var isEquipment = model.ItemBase.ItemType == ItemType.Equipment;
            var interactable =
                isEquipment && _selectedItemsForGrind.Count < 10 && !model.Equipped.Value
                || !isRegister;
            var onSubmit = isEquipment
                ? new System.Action(() => RegisterToGrindingList(model, isRegister))
                : null;
            tooltip.Show(
                model,
                isRegister
                    ? L10nManager.Localize("UI_COMBINATION_REGISTER_MATERIAL")
                    : L10nManager.Localize("UI_COMBINATION_UNREGISTER_MATERIAL"),
                interactable,
                onSubmit,
                grindInventory.ClearSelectedItem,
                target: target);
        }

        private void RegisterToGrindingList(InventoryItem item, bool isRegister)
        {
            if (isRegister)
            {
                _selectedItemsForGrind.Add(item);
            }
            else
            {
                _selectedItemsForGrind.Remove(item);
            }
        }

        private void Action(List<Equipment> equipments)
        {
            if (!equipments.Any() || equipments.Count > LimitGrindingCount)
            {
                Debug.LogWarning($"Invalid selected items count. count : {equipments.Count}");
                return;
            }

            ActionManager.Instance.Grinding(equipments).Subscribe();
            _selectedItemsForGrind.Clear();
        }
    }
}
