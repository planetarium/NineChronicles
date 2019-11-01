using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.UI.Model;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class CombineConsumable : CombinationPanel<CombineConsumable>
    {
        public Button recipeButton;
        public Recipe recipe;

        public override bool IsSubmittable =>
            !(States.Instance.AgentState.Value is null) &&
            States.Instance.AgentState.Value.gold >= CostNCG &&
            !(States.Instance.CurrentAvatarState.Value is null) &&
            States.Instance.CurrentAvatarState.Value.actionPoint >= CostAP &&
            otherMaterials.Count(e => !e.IsEmpty) >= 2;

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
            if (row.ItemType != ItemType.Material ||
                row.ItemSubType != ItemSubType.FoodMaterial)
                return true;

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

        protected override bool TryAddOtherMaterial(InventoryItemView view)
        {
            if (view.Model is null ||
                view.Model.ItemBase.Value.Data.ItemType != ItemType.Material ||
                view.Model.ItemBase.Value.Data.ItemSubType != ItemSubType.FoodMaterial)
                return false;

            return base.TryAddOtherMaterial(view);
        }
    }
}
