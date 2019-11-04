using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.UI.Model;
using TMPro;

namespace Nekoyume.UI.Module
{
    public class EnhanceEquipment : CombinationPanel<EnhanceEquipment>
    {
        public TextMeshProUGUI baseMaterialTitleText;
        public TextMeshProUGUI baseMaterialItemNameText;
        public StatView baseMaterialStatView;
        public TextMeshProUGUI[] otherMaterialTitleTexts;
        public TextMeshProUGUI[] otherMaterialItemNameTexts;
        public StatView[] otherMaterialStatViews;
        
        public override bool IsSubmittable =>
            !(States.Instance.AgentState.Value is null) &&
            States.Instance.AgentState.Value.gold >= CostNCG &&
            !(States.Instance.CurrentAvatarState.Value is null) &&
            States.Instance.CurrentAvatarState.Value.actionPoint >= CostAP &&
            !(baseMaterial is null) &&
            !baseMaterial.IsEmpty &&
            otherMaterials.Count(e => !e.IsEmpty) > 0;

        protected override void Awake()
        {
            base.Awake();

            if (baseMaterial is null)
                throw new SerializeFieldNullException();

            submitButton.submitText.text = LocalizationManager.Localize("UI_COMBINATION_ENHANCEMENT");
        }

        public override void Show()
        {
            base.Show();

            baseMaterial.Unlock();

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Lock();
            }
        }

        public override bool DimFunc(InventoryItem inventoryItem)
        {
            if (!IsThereAnyUnlockedEmptyMaterialView)
                return true;
            
            var row = inventoryItem.ItemBase.Value.Data;
            if (row.ItemType != ItemType.Equipment)
                return true;

            if (!baseMaterial.IsEmpty)
            {
                if (Contains(inventoryItem))
                    return true;
                
                if (baseMaterial.Model.ItemBase.Value.Data.ItemSubType != row.ItemSubType ||
                    baseMaterial.Model.ItemBase.Value.Data.Grade != row.Grade)
                    return true;
            }

            return false;
        }

        protected override int GetCostNCG()
        {
            return 0;
        }

        protected override int GetCostAP()
        {
            return baseMaterial.IsEmpty ? 0 : GameConfig.EnhanceEquipmentCostAP;
        }

        protected override bool TryAddBaseMaterial(InventoryItemView view)
        {
            if (view.Model is null ||
                view.Model.ItemBase.Value.Data.ItemType != ItemType.Equipment)
                return false;

            if (!baseMaterial.IsEmpty)
                return false;

            if (base.TryAddBaseMaterial(view))
            {
                foreach (var otherMaterial in otherMaterials)
                {
                    otherMaterial.Unlock();
                }

                return true;
            }

            return false;
        }
        
        protected override bool TryRemoveBaseMaterial(CombinationMaterialView view)
        {
            if (!base.TryRemoveBaseMaterial(view))
                return false;

            foreach (var materialView in otherMaterials)
            {
                materialView.Clear();
                materialView.Lock();
            }

            return true;
        }
    }
}
