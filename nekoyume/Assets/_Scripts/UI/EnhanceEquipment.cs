using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using Nekoyume.Action;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class EnhanceEquipment : EnhancementPanel<EnhancementMaterialView>
    {
        public Image arrowImage;
        public GameObject message;
        public TextMeshProUGUI messageText;

        [SerializeField]
        private Module.Inventory inventory = null;

        public override bool IsSubmittable =>
            !(States.Instance.AgentState is null) &&
            States.Instance.GoldBalanceState.gold >= CostNCG &&
            !(States.Instance.CurrentAvatarState is null) &&
            States.Instance.CurrentAvatarState.actionPoint >= CostAP &&
            !(baseMaterial is null) &&
            !baseMaterial.IsEmpty &&
            baseMaterial.Model.ItemBase.Value is Equipment equipment &&
            equipment.level < 10 &&
            otherMaterials.Count(e => !e.IsEmpty) > 0 &&
            Find<Combination>().selectedIndex >= 0;

        protected override void Awake()
        {
            base.Awake();

            if (baseMaterial is null)
                throw new SerializeFieldNullException();

            baseMaterial.titleText.text =
                L10nManager.Localize("UI_ENHANCEMENT_EQUIPMENT_TO_ENHANCE");
            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.titleText.text =
                    L10nManager.Localize("UI_ENHANCEMENT_EQUIPMENT_TO_CONSUME");
            }

            message.SetActive(false);
            submitButton.SetSubmitText(L10nManager.Localize("UI_COMBINATION_ENHANCEMENT"));
        }

        public override void Initialize()
        {
            base.Initialize();

            RemoveMaterialsAll();
            OnMaterialChange.Subscribe(SubscribeOnMaterialChange)
                .AddTo(gameObject);
            submitButton.OnSubmitClick.Subscribe(_ =>
            {
                var itemBase = baseMaterial.Model.ItemBase.Value;
                StartCoroutine(Find<Combination>().CoCombineNPCAnimation(itemBase, SubscribeOnClickSubmit));
                ActionEnhanceEquipment();
            }).AddTo(gameObject);

            inventory.SharedModel.SelectedItemView.Subscribe(ShowTooltip).AddTo(gameObject);
            inventory.OnDoubleClickItemView.Subscribe(StageMaterial).AddTo(gameObject);
            inventory.SharedModel.DeselectItemView();
            inventory.SharedModel.State.Value = ItemType.Equipment;
            inventory.SharedModel.DimmedFunc.Value = DimFunc;
            inventory.SharedModel.EffectEnabledFunc.Value = Contains;
        }

        public override bool Show(bool forced = false)
        {
            if (!base.Show(forced))
                return false;

            baseMaterial.Unlock(false);

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Lock();
            }

            return true;
        }

        public override void Close(bool ignoreCloseAnimation = false)
        {
            base.Close(ignoreCloseAnimation);

            RemoveMaterialsAll();
        }


        public override bool DimFunc(InventoryItem inventoryItem)
        {
            if (!IsThereAnyUnlockedEmptyMaterialView)
                return true;

            var item = inventoryItem.ItemBase.Value;
            if (item.ItemType != ItemType.Equipment)
                return true;

            if (!baseMaterial.IsEmpty)
            {
                if (Contains(inventoryItem))
                    return true;

                var baseEquipment = (Equipment) baseMaterial.Model.ItemBase.Value;
                if (baseEquipment.ItemSubType != item.ItemSubType ||
                    baseEquipment.Grade != item.Grade)
                    return true;

                var material = (Equipment) inventoryItem.ItemBase.Value;
                if (baseEquipment.level != material.level)
                    return true;
            }

            return false;
        }

        protected override BigInteger GetCostNCG()
        {
            if (baseMaterial.IsEmpty ||
                !(baseMaterial.Model.ItemBase.Value is Equipment equipment) ||
                equipment.level >= 10)
                return 0;

            var row = Game.Game.instance.TableSheets
                .EnhancementCostSheet.Values
                .FirstOrDefault(x => x.Grade == equipment.Grade && x.Level == equipment.level + 1);

            return row is null ? 0 : row.Cost;
        }

        protected override int GetCostAP()
        {
            return baseMaterial.IsEmpty ? 0 : ItemEnhancement.GetRequiredAp();
        }

        protected override bool TryAddBaseMaterial(InventoryItem viewModel, int count,
            out EnhancementMaterialView materialView)
        {
            if (viewModel is null ||
                viewModel.ItemBase.Value.ItemType != ItemType.Equipment)
            {
                materialView = null;
                return false;
            }

            if (!baseMaterial.IsEmpty)
            {
                materialView = null;
                return false;
            }

            if (!base.TryAddBaseMaterial(viewModel, count, out materialView))
                return false;

            if (!(viewModel.ItemBase.Value is Equipment equipment))
                throw new InvalidCastException(nameof(viewModel.ItemBase.Value));

            foreach (var otherMaterial in otherMaterials)
            {
                otherMaterial.Unlock(false);
            }

            UpdateMessageText();

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

            UpdateMessageText();

            return true;
        }

        protected override bool TryAddOtherMaterial(InventoryItem viewModel, int count,
            out EnhancementMaterialView materialView)
        {
            if (!base.TryAddOtherMaterial(viewModel, count, out materialView))
                return false;

            var equipment = (Equipment) baseMaterial.Model.ItemBase.Value;
            var statValue = equipment.StatsMap.GetStat(equipment.UniqueStatType, true);
            var resultValue = statValue + (int) equipment.GetIncrementAmountOfEnhancement();
            baseMaterial.UpdateStatView(resultValue.ToString(CultureInfo.InvariantCulture));
            UpdateMessageText();

            return true;
        }

        protected override bool TryRemoveOtherMaterial(EnhancementMaterialView view,
            out EnhancementMaterialView materialView)
        {
            if (!base.TryRemoveOtherMaterial(view, out materialView))
                return false;

            baseMaterial.UpdateStatView();
            UpdateMessageText();

            return true;
        }

        private void UpdateMessageText()
        {
            if (baseMaterial.IsEmpty)
            {
                message.SetActive(false);
                return;
            }

            if (!(baseMaterial.Model.ItemBase.Value is Equipment baseEquipment))
                throw new InvalidCastException(nameof(baseMaterial.Model.ItemBase.Value));

            var count = baseEquipment.GetOptionCount();
            foreach (var otherMaterial in otherMaterials.Where(e => !e.IsLocked && !e.IsEmpty))
            {
                if (!(otherMaterial.Model.ItemBase.Value is Equipment otherEquipment))
                    throw new InvalidCastException(nameof(otherMaterial.Model.ItemBase.Value));

                count = Math.Max(count, otherEquipment.GetOptionCount());
            }

            if (count == 0)
                return;

            message.SetActive(true);
            messageText.text = string.Format(
                L10nManager.Localize("UI_ENHANCEMENT_GUIDE"),
                count);
        }

        private void SubscribeOnMaterialChange(EnhancementPanel<EnhancementMaterialView> viewModel)
        {
            inventory.SharedModel.UpdateDimAll();
            inventory.SharedModel.UpdateEffectAll();
        }

        private void ActionEnhanceEquipment()
        {
            var baseEquipmentGuid =
                ((Equipment) baseMaterial.Model.ItemBase.Value).ItemId;
            var otherEquipmentGuidList = otherMaterials
                .Select(e => ((Equipment) e.Model.ItemBase.Value).ItemId)
                .ToList();

            UpdateCurrentAvatarState(baseEquipmentGuid, otherEquipmentGuidList);
            CreateItemEnhancementAction(baseEquipmentGuid, otherEquipmentGuidList, Widget.Find<Combination>().selectedIndex);
            RemoveMaterialsAll();
        }

        private void UpdateCurrentAvatarState(Guid baseItemGuid, IEnumerable<Guid> otherItemGuidList)
        {
            var agentAddress = States.Instance.AgentState.address;
            var avatarAddress = States.Instance.CurrentAvatarState.address;

            LocalStateModifier.ModifyAgentGold(agentAddress, -CostNCG);
            LocalStateModifier.ModifyAvatarActionPoint(avatarAddress, -CostAP);
            LocalStateModifier.RemoveItem(avatarAddress, baseItemGuid);
            foreach (var itemGuid in otherItemGuidList)
            {
                LocalStateModifier.RemoveItem(avatarAddress, itemGuid);
            }
        }

        private void CreateItemEnhancementAction(
            Guid baseItemGuid,
            List<Guid> otherItemGuidList,
            int slotIndex)
        {
            LocalStateModifier.ModifyCombinationSlotItemEnhancement(this, otherItemGuidList, slotIndex);
            var msg = L10nManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_START");
            Notification.Push(MailType.Workshop, msg);
            Game.Game.instance.ActionManager
                .ItemEnhancement(baseItemGuid, otherItemGuidList, slotIndex)
                .Subscribe(
                    _ => { },
                    _ => Widget.Find<ActionFailPopup>().Show("Timeout occurred during ItemEnhancement"));
        }


        private void ShowTooltip(InventoryItemView view)
        {
            var tooltip = Find<ItemInformationTooltip>();
            if (view is null ||
                view.RectTransform == tooltip.Target)
            {
                tooltip.Close();
                return;
            }

            tooltip.Show(
                view.RectTransform,
                view.Model,
                value => !view.Model?.Dimmed.Value ?? false,
                L10nManager.Localize("UI_COMBINATION_REGISTER_MATERIAL"),
                _ => StageMaterial(view),
                _ => inventory.SharedModel.DeselectItemView());
        }


        private void StageMaterial(InventoryItemView itemView)
        {
            Find<Combination>().ShowSpeech("SPEECH_COMBINE_STAGE_MATERIAL_");
            TryAddMaterial(itemView);
        }
    }
}
