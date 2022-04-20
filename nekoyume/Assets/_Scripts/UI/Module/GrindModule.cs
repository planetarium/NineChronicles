using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.BlockChain;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
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
                                 States.Instance.CurrentAvatarState.actionPoint > Grinding.CostAp &&
                                 _selectedItemsForGrind.All(item => !item.Equipped.Value);

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

            _selectedItemsForGrind.Clear();
            grindInventory.SetGrinding(ShowItemTooltip, OnUpdateInventory);
            grindButton.Interactable = false;
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

        private void OnUpdateInventory(Inventory inventory)
        {
            var selectedItemCount = _selectedItemsForGrind.Count;
            for (int i = 0; i < selectedItemCount; i++)
            {
                if (inventory.TryGetModel(_selectedItemsForGrind[i].ItemBase, out var inventoryItem))
                {
                    inventoryItem.GrindingCount.SetValueAndForceNotify(_selectedItemsForGrind[i].GrindingCount.Value);
                    _selectedItemsForGrind[i] = inventoryItem;
                    itemSlots[i].UpdateSlot(_selectedItemsForGrind[i]);
                }
            }

            grindButton.Interactable = CanGrind;
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

        /// <summary>
        /// Returns true if any of the selected equipment has enhanced equipment or has skills.
        /// </summary>
        /// <param name="equipments"></param>
        /// <returns></returns>
        private static bool CheckSelectedItemsAreStrong(List<Equipment> equipments)
        {
            return equipments.Exists(item =>
                item.level > 0 || item.Skills.Any() || item.BuffSkills.Any());
        }

        private void Action(List<Equipment> equipments)
        {
            if (!equipments.Any() || equipments.Count > LimitGrindingCount)
            {
                Debug.LogWarning($"Invalid selected items count. count : {equipments.Count}");
                return;
            }

            if (CheckSelectedItemsAreStrong(equipments))
            {
                var system = Widget.Find<IconAndButtonSystem>();
                // TODO: Add localizing key
                system.ShowWithTwoButton(L10nManager.Localize("UI_CONFIRM"),
                    "You chose strong equipment. It is ok?",
                    L10nManager.Localize("UI_OK"),
                    L10nManager.Localize("UI_CANCEL"),
                    false,
                    IconAndButtonSystem.SystemType.Information);
                system.ConfirmCallback = () => PushAction(equipments);
                system.CancelCallback = () => system.Close();
            }
            else
            {
                PushAction(equipments);
            }
        }

        private void PushAction(List<Equipment> equipments)
        {
            // TODO: add animation and etc.
            NotificationSystem.Push(MailType.Workshop,
                "Grind Start",
                NotificationCell.NotificationType.Information);
            ActionManager.Instance.Grinding(equipments).Subscribe();
            _selectedItemsForGrind.Clear();
        }
    }
}
