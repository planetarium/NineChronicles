using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CombineConsumable : CombinationPanel<CombinationMaterialView>
    {
        public SimpleItemView resultItemView;
        public TextMeshProUGUI resultItemNameText;

        public Button countMinusButton;
        public Button countPlusButton;
        public TextMeshProUGUI countText;

        public Button recipeButton;

        public override bool IsSubmittable =>
            !(States.Instance.AgentState is null) &&
            States.Instance.AgentState.gold >= CostNCG &&
            !(States.Instance.CurrentAvatarState is null) &&
            States.Instance.CurrentAvatarState.actionPoint >= CostAP &&
            otherMaterials.Count(e => !e.IsLocked && !e.IsEmpty) >= 2;

        private readonly ReactiveProperty<int> _count = new ReactiveProperty<int>();

        protected override void Awake()
        {
            base.Awake();

            submitButton.SetText("UI_COMBINATION_ITEM");

            countMinusButton.OnClickAsObservable().Subscribe(SubscribeCountMinusClick).AddTo(gameObject);
            countPlusButton.OnClickAsObservable().Subscribe(SubscribeCountPlusClick).AddTo(gameObject);

            recipeButton.OnClickAsObservable().Subscribe(_ => AudioController.PlayClick()).AddTo(gameObject);

            _count.SubscribeTo(countText).AddTo(gameObject);
        }

        public void ResetCount()
        {
            _count.SetValueAndForceNotify(1);
        }

        public override bool Show(bool forced = false)
        {
            if (!base.Show(forced))
                return false;

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Unlock();
            }

            ResetCount();
            UpdateResultItem();
            UpdateCountButtons();
            return true;
        }

        public override bool DimFunc(InventoryItem inventoryItem)
        {
            if (!IsThereAnyUnlockedEmptyMaterialView)
                return true;

            var row = inventoryItem.ItemBase.Value.Data;
            return row.ItemType != ItemType.Material ||
                   row.ItemSubType != ItemSubType.FoodMaterial ||
                   inventoryItem.Count.Value < _count.Value ||
                   Contains(inventoryItem);
        }

        protected override int GetCostNCG()
        {
            return 0;
        }

        protected override int GetCostAP()
        {
            return otherMaterials.Any(e => !e.IsEmpty)
                ? GameConfig.CombineConsumableCostAP * _count.Value
                : 0;
        }
        
        protected override bool TryAddOtherMaterial(InventoryItem viewModel, int count, out CombinationMaterialView materialView)
        {
            if (viewModel is null ||
                viewModel.ItemBase.Value.Data.ItemType != ItemType.Material ||
                viewModel.ItemBase.Value.Data.ItemSubType != ItemSubType.FoodMaterial ||
                viewModel.Count.Value < _count.Value ||
                Contains(viewModel))
            {
                materialView = null;
                return false;
            }

            if (!base.TryAddOtherMaterial(viewModel, count, out materialView))
                return false;
            
            materialView.TryIncreaseCount(_count.Value - materialView.Model.Count.Value);

            UpdateResultItem();
            UpdateCountButtons();

            return true;
        }

        protected override bool TryRemoveOtherMaterial(CombinationMaterialView view,
            out CombinationMaterialView materialView)
        {
            if (!base.TryRemoveOtherMaterial(view, out materialView))
                return false;
            
            if (otherMaterials.Where(e => !e.IsLocked).All(e => e.IsEmpty))
            {
                ResetCount();
            }
            
            UpdateResultItem();
            UpdateCountButtons();

            return true;
        }

        public override void RemoveMaterialsAll()
        {
            base.RemoveMaterialsAll();

            _count.SetValueAndForceNotify(1);
            UpdateResultItem();
            UpdateCountButtons();
        }

        private void UpdateResultItem()
        {
            var ids = otherMaterials
                .Where(e => !e.IsLocked && !e.IsEmpty)
                .Select(e => e.Model.ItemBase.Value.Data.Id)
                .ToList();
            if (ids.Count >= 2)
            {
                resultItemView.gameObject.SetActive(true);
                if (TableSheets.FromTableSheetsState(TableSheetsState.Current).ConsumableItemRecipeSheet.TryGetValue(ids, out var recipeRow))
                {
                    Game.Game.instance.TableSheets.ConsumableItemSheet.TryGetValue(recipeRow.ResultConsumableItemId,
                        out var itemRow, true);
                    var itemBase = ItemFactory.Create(itemRow, Guid.NewGuid());
                    resultItemView.SetData(new Item(itemBase));
                    resultItemNameText.gameObject.SetActive(true);
                    resultItemNameText.text = itemRow.GetLocalizedName();
                    resultItemNameText.color = Color.white;
                }
                else
                {
                    resultItemView.SetToUnknown();
                    resultItemNameText.gameObject.SetActive(false);
                }
            }
            else
            {
                resultItemView.gameObject.SetActive(false);
                resultItemNameText.gameObject.SetActive(true);
                resultItemNameText.text = LocalizationManager.Localize("UI_ENHANCEMENT_REGISTER_THE_MATERIAL");
                resultItemNameText.color = ColorHelper.HexToColorRGB("81564C");
            }
        }

        private void UpdateCountButtons()
        {
            if (otherMaterials.Where(e => !e.IsLocked).All(e => e.IsEmpty))
            {
                countMinusButton.interactable = false;
                countPlusButton.interactable = false;
                return;
            }

            countMinusButton.interactable = !otherMaterials.Any(e => !e.IsEmpty && e.IsMinCount);
            countPlusButton.interactable = !otherMaterials.Any(e => !e.IsEmpty && e.IsMaxCount);
        }

        private void SubscribeCountMinusClick(Unit unit)
        {
            AudioController.PlayClick();
            _count.Value--;
            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.TryDecreaseCount();
            }
            UpdateCountButtons();
        }

        private void SubscribeCountPlusClick(Unit unit)
        {
            AudioController.PlayClick();
            _count.Value++;
            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.TryIncreaseCount();
            }
            UpdateCountButtons();
        }
    }
}
