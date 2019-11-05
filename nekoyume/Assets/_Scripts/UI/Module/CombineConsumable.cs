using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Game.Factory;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CombineConsumable : CombinationPanel<CombinationMaterialView>
    {
        public SimpleItemView resultItemView;
        public TextMeshProUGUI resultItemNameText;

        public Button recipeButton;
        public Recipe recipe;

        public override bool IsSubmittable =>
            !(States.Instance.AgentState.Value is null) &&
            States.Instance.AgentState.Value.gold >= CostNCG &&
            !(States.Instance.CurrentAvatarState.Value is null) &&
            States.Instance.CurrentAvatarState.Value.actionPoint >= CostAP &&
            otherMaterials.Count(e => !e.IsEmpty && !e.IsLocked) >= 2;

        protected override void Awake()
        {
            base.Awake();

            submitButton.submitText.text = LocalizationManager.Localize("UI_COMBINATION_ITEM");

            recipeButton.OnClickAsObservable()
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    recipe.Show();
                }).AddTo(gameObject);
        }

        public override void Show()
        {
            base.Show();

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Unlock();
            }

            UpdateResultItem();
        }

        public override void Hide()
        {
            recipe.Hide();

            base.Hide();
        }

        public override bool DimFunc(InventoryItem inventoryItem)
        {
            var row = inventoryItem.ItemBase.Value.Data;
            if (row.ItemType != ItemType.Material ||
                row.ItemSubType != ItemSubType.FoodMaterial)
                return true;

            if (!IsThereAnyUnlockedEmptyMaterialView)
                return !Contains(inventoryItem);

            return false;
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
                view.Model.ItemBase.Value.Data.ItemSubType != ItemSubType.FoodMaterial)
            {
                materialView = null;
                return false;
            }

            if (!base.TryAddOtherMaterial(view, out materialView))
                return false;

            UpdateResultItem();

            return true;
        }

        protected override bool TryRemoveOtherMaterial(CombinationMaterialView view,
            out CombinationMaterialView materialView)
        {
            if (!base.TryRemoveOtherMaterial(view, out materialView))
                return false;

            UpdateResultItem();

            return true;
        }

        private void UpdateResultItem()
        {
            var ids = otherMaterials
                .Where(e => !e.IsEmpty && !e.IsLocked)
                .Select(e => e.Model.ItemBase.Value.Data.Id)
                .ToList();
            if (ids.Count >= 2)
            {
                var resultItemId =
                    Game.Game.instance.TableSheets.ConsumableItemRecipeSheet.TryGetValue(ids, out var recipeRow)
                        ? recipeRow.ResultConsumableItemId
                        : GameConfig.CombinationDefaultFoodId;

                Game.Game.instance.TableSheets.ConsumableItemSheet.TryGetValue(resultItemId, out var itemRow, true);
                var itemBase = ItemFactory.Create(itemRow, Guid.NewGuid());
                resultItemView.gameObject.SetActive(true);
                resultItemView.SetData(new Item(itemBase));
                resultItemNameText.text = itemRow.GetLocalizedName();
//                resultItemNameText.color =
            }
            else
            {
                resultItemView.gameObject.SetActive(false);
                resultItemNameText.text = LocalizationManager.Localize("UI_ENHANCEMENT_REGISTER_THE_MATERIAL");
//                resultItemNameText.color = 
            }
        }
    }
}
