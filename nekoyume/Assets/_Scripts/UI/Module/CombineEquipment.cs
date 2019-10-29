using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.UI.Model;

namespace Nekoyume.UI.Module
{
    public class CombineEquipment : CombinationPanel<CombineEquipment>
    {
        public override bool IsSubmittable =>
            !(States.Instance.AgentState.Value is null) &&
            States.Instance.AgentState.Value.gold >= CostNCG &&
            !(States.Instance.CurrentAvatarState.Value is null) &&
            States.Instance.CurrentAvatarState.Value.actionPoint >= CostAP &&
            !(baseMaterial is null) &&
            !baseMaterial.IsEmpty &&
            otherMaterials.Any(e => !e.IsEmpty);


        protected override void Awake()
        {
            base.Awake();

            if (baseMaterial is null)
                throw new SerializeFieldNullException();

            submitButtonText.text = LocalizationManager.Localize("UI_COMBINATION_ITEM");
        }

        public override void Show()
        {
            base.Show();

            baseMaterial.Unlock();

            foreach (var material in otherMaterials)
            {
                material.Lock();
            }
        }

        public override bool DimFunc(InventoryItem inventoryItem)
        {
            var row = inventoryItem.ItemBase.Value.Data;
            if (row.ItemType != ItemType.Material)
                return true;

            if (baseMaterial.IsEmpty)
            {
                if (row.ItemSubType != ItemSubType.EquipmentMaterial)
                    return true;
            }
            else if (row.ItemSubType != ItemSubType.EquipmentMaterial &&
                     row.ItemSubType != ItemSubType.MonsterPart)
                return true;

            return base.DimFunc(inventoryItem);
        }

        protected override int GetCostNCG()
        {
            if (baseMaterial is null ||
                baseMaterial.Model is null)
                return 0;

            Game.Game.instance.TableSheets.ItemConfigForGradeSheet.TryGetValue(
                baseMaterial.Model.ItemBase.Value.Data.Grade,
                out var configRow, true);

            var otherMaterialsCount = otherMaterials.Count(e => !e.IsEmpty);
            var ncgCount = Math.Max(0, otherMaterialsCount - configRow.MonsterPartsCountForCombination);
            return ncgCount * GameConfig.CombineEquipmentCostNCG;
        }

        protected override int GetCostAP()
        {
            return GameConfig.CombineEquipmentCostAP;
        }

        protected override bool TryAddBaseMaterial(InventoryItemView view)
        {
            if (view.Model is null ||
                view.Model.ItemBase.Value.Data.ItemType != ItemType.Material ||
                view.Model.ItemBase.Value.Data.ItemSubType != ItemSubType.EquipmentMaterial)
                return false;

            if (base.TryAddBaseMaterial(view))
            {
                Game.Game.instance.TableSheets.ItemConfigForGradeSheet.TryGetValue(view.Model.ItemBase.Value.Data.Grade,
                    out var configRow, true);

                for (var i = 0; i < otherMaterials.Count; i++)
                {
                    var material = otherMaterials[i];
                    if (i < configRow.MonsterPartsCountForCombination)
                    {
                        material.Unlock();
                    }
                    else if (i < configRow.MonsterPartsCountForCombination +
                             configRow.MonsterPartsCountForCombinationWithNCG)
                    {
                        material.Unlock();
                    }
                    else
                    {
                        material.Lock();
                    }
                }

                return true;
            }

            return false;
        }

        protected override bool TryRemoveBaseMaterial(CombinationMaterialView view)
        {
            if (!base.TryRemoveBaseMaterial(view))
                return false;

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Set(null);
                otherMaterial.Lock();
            }

            return true;
        }
    }
}
