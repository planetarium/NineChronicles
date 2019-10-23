using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.UI.Model;

namespace Nekoyume.UI.Module
{
    public class EnhanceEquipment : CombinationPanel<EnhanceEquipment>
    {
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

            submitButtonText.text = LocalizationManager.Localize("UI_COMBINATION_ENHANCEMENT");
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
            var row = inventoryItem.ItemBase.Value.Data;
            if (row.ItemType != ItemType.Equipment)
                return true;

            if (!baseMaterial.IsEmpty)
            {
                if (baseMaterial.Model.ItemBase.Value.Data.ItemSubType != row.ItemSubType ||
                    baseMaterial.Model.ItemBase.Value.Data.Grade != row.Grade)
                    return true;
            }

            return base.DimFunc(inventoryItem);
        }

        protected override int GetCostNCG()
        {
            return 0;
        }

        protected override int GetCostAP()
        {
            return GameConfig.EnhanceEquipmentCostAP;
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
                materialView.Set(null);
                materialView.Lock();
            }

            return true;
        }
    }
}
