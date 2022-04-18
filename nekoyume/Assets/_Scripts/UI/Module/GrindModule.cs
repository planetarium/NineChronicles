using System.Collections.Generic;
using System.Linq;
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

        private readonly List<InventoryItem> _selectedItemsForGrinding = new List<InventoryItem>();

        public void Start()
        {
            grindButton.SetCost(ConditionalCostButton.CostType.ActionPoint, 5);
            grindButton.SetCondition(() => _selectedItemsForGrinding.Any());
            grindButton.OnSubmitSubject.Subscribe(_ =>
            {
                _selectedItemsForGrinding.Select(inventoryItem =>
                    ((Equipment) inventoryItem.ItemBase).ItemId);
            }).AddTo(gameObject);
        }

        public void Initialize()
        {
            grindInventory.SetGrinding(ShowItemTooltip);
        }

        private void ShowItemTooltip(InventoryItem model, RectTransform target)
        {
            var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
            var isRegister = !_selectedItemsForGrinding.Contains(model);
            tooltip.Show(
                model,
                isRegister
                    ? L10nManager.Localize("UI_COMBINATION_REGISTER_MATERIAL")
                    : L10nManager.Localize("UI_COMBINATION_UNREGISTER_MATERIAL"),
                model.ItemBase.ItemType == ItemType.Equipment && _selectedItemsForGrinding.Count < 10,
                () => RegisterToGrindingList(model, isRegister),
                grindInventory.ClearSelectedItem,
                target: target);
        }

        private void RegisterToGrindingList(InventoryItem item, bool isRegister)
        {
            if (isRegister)
            {
                _selectedItemsForGrinding.Add(item);
                item.GrindingCount.SetValueAndForceNotify(_selectedItemsForGrinding.Count);
                item.GrindObjectEnabled.OnNext(true);
            }
            else
            {
                var deleteIndex = _selectedItemsForGrinding.IndexOf(item);
                if (deleteIndex != -1)
                {
                    _selectedItemsForGrinding.RemoveAt(deleteIndex);
                    var listSize = _selectedItemsForGrinding.Count;
                    for (int i = deleteIndex; i < listSize; i++)
                    {
                        _selectedItemsForGrinding[i].GrindingCount.SetValueAndForceNotify(i + 1);
                    }

                    item.GrindObjectEnabled.OnNext(false);
                }
            }
        }

        private void Action()
        {
            if (!_selectedItemsForGrinding.Any())
            {
                return;
            }

        }
    }
}
