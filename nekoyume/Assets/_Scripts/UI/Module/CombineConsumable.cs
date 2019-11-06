using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.Helper;
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
        public Recipe recipe;

        public override bool IsSubmittable =>
            !(States.Instance.AgentState.Value is null) &&
            States.Instance.AgentState.Value.gold >= CostNCG &&
            !(States.Instance.CurrentAvatarState.Value is null) &&
            States.Instance.CurrentAvatarState.Value.actionPoint >= CostAP &&
            otherMaterials.Count(e => !e.IsLocked && !e.IsEmpty) >= 2;

        private readonly ReactiveProperty<int> _count = new ReactiveProperty<int>();

        protected override void Awake()
        {
            base.Awake();

            submitButton.submitText.text = LocalizationManager.Localize("UI_COMBINATION_ITEM");

            countMinusButton.OnClickAsObservable().Subscribe(SubscribeCountMinusClick).AddTo(gameObject);
            countPlusButton.OnClickAsObservable().Subscribe(SubscribeCountPlusClick).AddTo(gameObject);

            recipeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    recipe.Show();
                }).AddTo(gameObject);

            _count.Subscribe(SubscribeCount).AddTo(gameObject);
        }

        public override void Show()
        {
            base.Show();

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Unlock();
            }

            UpdateResultItem();

            _count.SetValueAndForceNotify(1);
        }

        public override void Hide()
        {
            recipe.Hide();

            base.Hide();
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
            return otherMaterials.Any(e => !e.IsEmpty) ? GameConfig.CombineConsumableCostAP : 0;
        }
        
        protected override bool TryAddOtherMaterial(InventoryItemView view, out CombinationMaterialView materialView)
        {
            if (view.Model is null ||
                view.Model.ItemBase.Value.Data.ItemType != ItemType.Material ||
                view.Model.ItemBase.Value.Data.ItemSubType != ItemSubType.FoodMaterial ||
                view.Model.Count.Value < _count.Value ||
                Contains(view.Model))
            {
                materialView = null;
                return false;
            }

            if (!base.TryAddOtherMaterial(view, out materialView))
                return false;
            
            materialView.effectImage.enabled = true;

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
            
            materialView.effectImage.enabled = false;

            UpdateResultItem();

            if (otherMaterials.Where(e => !e.IsLocked).All(e => e.IsEmpty))
            {
                _count.SetValueAndForceNotify(1);
            }
            else
            {
                UpdateCountButtons();
            }

            return true;
        }

        public override void RemoveMaterialsAll()
        {
            base.RemoveMaterialsAll();

            UpdateResultItem();
            _count.SetValueAndForceNotify(1);
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
                if (Game.Game.instance.TableSheets.ConsumableItemRecipeSheet.TryGetValue(ids, out var recipeRow))
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
            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.TryDecreaseCount();
            }

            _count.Value--;
        }

        private void SubscribeCountPlusClick(Unit unit)
        {
            AudioController.PlayClick();
            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.TryIncreaseCount();
            }

            _count.Value++;
        }

        private void SubscribeCount(int count)
        {
            countText.text = count.ToString();
            UpdateCountButtons();
        }
    }
}
