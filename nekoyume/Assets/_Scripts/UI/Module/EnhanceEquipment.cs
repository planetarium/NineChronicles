using System;
using System.Linq;
using Assets.SimpleLocalization;
using Nekoyume.BlockChain;
using Nekoyume.EnumType;
using Nekoyume.Game.Item;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EnhanceEquipment : CombinationPanel<EnhancementMaterialView>
    {
        public Image arrowImage;
        public GameObject message;
        public TextMeshProUGUI messageText;

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

            baseMaterial.titleText.text = LocalizationManager.Localize("UI_ENHANCEMENT_EQUIPMENT_TO_ENHANCE");
            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.titleText.text = LocalizationManager.Localize("UI_ENHANCEMENT_EQUIPMENT_TO_CONSUME");
            }

            message.SetActive(false);
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

                var baseEquipment = (Equipment) baseMaterial.Model.ItemBase.Value;
                if (baseEquipment.Data.ItemSubType != row.ItemSubType || baseEquipment.Data.Grade != row.Grade)
                    return true;

                var material = (Equipment) inventoryItem.ItemBase.Value;
                if (baseEquipment.level != material.level)
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

        protected override bool TryAddBaseMaterial(InventoryItemView view, out EnhancementMaterialView materialView)
        {
            if (view.Model is null ||
                view.Model.ItemBase.Value.Data.ItemType != ItemType.Equipment)
            {
                materialView = null;
                return false;
            }

            if (!baseMaterial.IsEmpty)
            {
                materialView = null;
                return false;
            }

            if (!base.TryAddBaseMaterial(view, out materialView))
                return false;

            if (!(view.Model.ItemBase.Value is Equipment equipment))
                throw new InvalidCastException(nameof(view.Model.ItemBase.Value));

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Unlock();
            }

            message.SetActive(true);
            messageText.text = string.Format(
                LocalizationManager.Localize("UI_ENHANCEMENT_N_OPTION_RANDOMLY_SELECT"),
                equipment.GetOptionCount());

            return true;
        }

        protected override bool TryRemoveBaseMaterial(EnhancementMaterialView view,
            out EnhancementMaterialView materialView)
        {
            if (!base.TryRemoveBaseMaterial(view, out materialView))
                return false;

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Clear();
                otherMaterial.Lock();
            }

            message.SetActive(false);

            return true;
        }

        protected override bool TryAddOtherMaterial(InventoryItemView view, out EnhancementMaterialView materialView)
        {
            if (!base.TryAddOtherMaterial(view, out materialView))
                return false;

            var equipment = (Equipment) baseMaterial.Model.ItemBase.Value;
            var resultValue = equipment.TryGetBaseStat(out var statType, out var value)
                ? value + equipment.levelStats
                : equipment.levelStats;
            baseMaterial.UpdateStatView(
                $" -> <color=#00ff00><size=120%>{resultValue}</size></color>");
            materialView.statView.Hide();

            if (!(baseMaterial.Model.ItemBase.Value is Equipment baseEquipment))
                throw new InvalidCastException(nameof(view.Model.ItemBase.Value));

            var maxCount = baseEquipment.GetOptionCount();

            foreach (var otherMaterial in otherMaterials.Where(e => !e.IsEmpty && !e.IsLocked))
            {
                if (!(otherMaterial.Model.ItemBase.Value is Equipment otherEquipment))
                    throw new InvalidCastException(nameof(view.Model.ItemBase.Value));

                maxCount = Math.Max(maxCount, otherEquipment.GetOptionCount());
            }

            messageText.text = string.Format(
                LocalizationManager.Localize("UI_ENHANCEMENT_N_OPTION_RANDOMLY_SELECT"),
                maxCount);

            return true;
        }

        protected override bool TryRemoveOtherMaterial(EnhancementMaterialView view,
            out EnhancementMaterialView materialView)
        {
            if (!base.TryRemoveOtherMaterial(view, out materialView))
                return false;

            baseMaterial.UpdateStatView();

            return true;
        }
    }
}
