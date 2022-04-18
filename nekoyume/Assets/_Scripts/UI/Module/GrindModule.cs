using System.Collections.Generic;
using System.Linq;
using Libplanet.Assets;
using Nekoyume.BlockChain;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
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

        private readonly ReactiveCollection<InventoryItem> _selectedItemsForGrind =
            new ReactiveCollection<InventoryItem>();

        public void Start()
        {
            grindButton.SetCost(ConditionalCostButton.CostType.ActionPoint, 5);
            grindButton.SetCondition(() => _selectedItemsForGrind.Any());
            grindButton.OnSubmitSubject.Subscribe(_ =>
            {
                _selectedItemsForGrind.Select(inventoryItem =>
                    ((Equipment) inventoryItem.ItemBase).ItemId);
            }).AddTo(gameObject);

            _selectedItemsForGrind.ObserveAdd().Subscribe(item =>
            {
                item.Value.GrindingCount.SetValueAndForceNotify(_selectedItemsForGrind.Count);
                item.Value.GrindObjectEnabled.OnNext(true);
            }).AddTo(gameObject);

            _selectedItemsForGrind.ObserveRemove().Subscribe(item =>
            {
                var listSize = _selectedItemsForGrind.Count;
                for (int i = item.Index; i < listSize; i++)
                {
                    _selectedItemsForGrind[i].GrindingCount.SetValueAndForceNotify(i + 1);
                }
                item.Value.GrindObjectEnabled.OnNext(false);
            }).AddTo(gameObject);
        }

        public void Initialize()
        {
            grindInventory.SetGrinding(ShowItemTooltip);
            _selectedItemsForGrind.Clear();
        }

        private void ShowItemTooltip(InventoryItem model, RectTransform target)
        {
            var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
            var isRegister = !_selectedItemsForGrind.Contains(model);
            var isEquipment = model.ItemBase.ItemType == ItemType.Equipment;
            var interactable = isEquipment && _selectedItemsForGrind.Count < 10 || !isRegister;
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
            if (!_selectedItemsForGrind.Any())
            {
                return;
            }

            ActionManager.Instance.Grinding(equipments).Subscribe();
        }
    }
}
