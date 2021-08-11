using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using UniRx;

    public class CombinationSlotPopup : PopupWidget
    {
        public enum CombinationType
        {
            None,
            CraftEquipment,
            CrateFood,
            Enhancement,
        }

        [Serializable]
        public class Information
        {
            public CombinationType Type;
            public GameObject Icon;
            public GameObject OptionContainer;
            public TextMeshProUGUI ItemLevel;
            public List<SlotPopupOptionView> Options;
        }

        [SerializeField] private SimpleItemView itemView;
        [SerializeField] private Slider progressBar;

        [SerializeField] private TextMeshProUGUI itemNameText;
        [SerializeField] private TextMeshProUGUI requiredBlockIndexText;
        [SerializeField] private TextMeshProUGUI hourglassCountText;

        [SerializeField] private  Button rapidCombinationButton;
        [SerializeField] private  Button bgButton;
        [SerializeField] private List<Information> _informations;

        private CombinationType _type = CombinationType.None;
        private CombinationSlotState _state;
        private int _slotIndex;

        protected override void Awake()
        {
            base.Awake();

            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeOnBlockIndex).AddTo(gameObject);

            rapidCombinationButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                RapidCombination(_state, _slotIndex, Game.Game.instance.Agent.BlockIndex);
                Close();
            });

            bgButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });

            CloseWidget = null;
        }

        private void SubscribeOnBlockIndex(long currentBlockIndex)
        {
            UpdateInformation(_type, _state, currentBlockIndex);
        }

        public void Show(CombinationSlotState state, int slotIndex, long currentBlockIndex)
        {
            _state = state;
            _slotIndex = slotIndex;
            _type = GetCombinationType(state);
            UpdateInformation(_type, state, currentBlockIndex);
            base.Show();
        }

        private void UpdateInformation(CombinationType type, CombinationSlotState state, long currentBlockIndex)
        {
            if (state == null)
            {
                return;
            }

            UpdateOption(type, state.Result.itemUsable);
            UpdateItemInformation(state.Result.itemUsable);
            UpdateButtonInformation(state, currentBlockIndex);
            UpdateRequiredBlockInformation(state, currentBlockIndex);
        }

        private void UpdateOption(CombinationType type, ItemUsable itemUsable)
        {
            foreach (var information in _informations)
            {
                information.Icon.SetActive(information.Type.Equals(type));
                information.OptionContainer.SetActive(information.Type.Equals(type));
            }

            switch (type)
            {
                case CombinationType.CraftEquipment:
                    SetCraftOption(GetInformation(type), itemUsable);
                    break;
                case CombinationType.Enhancement:
                    SetEnhancementOption(GetInformation(type), itemUsable);
                    break;
            }
        }

        private void SetCraftOption(Information information, ItemUsable itemUsable)
        {
            foreach (var option in information.Options)
            {
                option.gameObject.SetActive(false);
            }

            var stats = itemUsable.StatsMap.GetStats().ToList();
            for (var i = 0; i < stats.Count; i++)
            {
                information.Options[i].gameObject.SetActive(true);
                information.Options[i].Set(stats[i].StatType.ToString());
            }

            var skills = itemUsable.Skills;
            for (var i = 0; i < skills.Count; i++)
            {
                information.Options[i + stats.Count].gameObject.SetActive(true);
                information.Options[i + stats.Count].Set(skills[i].SkillRow.GetLocalizedName());
            }
        }

        private void SetEnhancementOption(Information information, ItemUsable itemUsable)
        {
            if (!(itemUsable is Equipment equipment))
            {
                return;
            }

            information.ItemLevel.text = $"+{equipment.level}";
            var sheet = Game.Game.instance.TableSheets.EnhancementCostSheetV2;
            var grade = equipment.Grade;
            var level = equipment.level;
            var row = sheet.OrderedList.FirstOrDefault(x => x.Grade == grade  && x.Level == level);

            foreach (var option in information.Options)
            {
                option.gameObject.SetActive(false);
            }

            var stats = itemUsable.StatsMap.GetStats().ToList();
            for (var i = 0; i < stats.Count; i++)
            {
                information.Options[i].gameObject.SetActive(true);
                information.Options[i].Set($"{stats[i].StatType} " +
                                           $"+({(int)(row.ExtraStatGrowthMin * 100)}% " +
                                           $"~ {(int)(row.ExtraStatGrowthMax * 100)}%)");
            }

            var skills = itemUsable.Skills;
            for (var i = 0; i < skills.Count; i++)
            {
                information.Options[i + stats.Count].gameObject.SetActive(true);
                information.Options[i + stats.Count].Set(
                    $"{skills[i].SkillRow.GetLocalizedName()} " +
                        $"{L10nManager.Localize("UI_SKILL_POWER")} : " +
                        $"+({(int)(row.ExtraSkillDamageGrowthMin * 100)}% " +
                        $"~ {(int)(row.ExtraSkillDamageGrowthMax * 100)}%) / " +
                        $"{L10nManager.Localize("UI_SKILL_CHANCE")} : " +
                        $"+({(int)(row.ExtraSkillChanceGrowthMin * 100)}% " +
                        $"~ {(int)(row.ExtraSkillChanceGrowthMax * 100)}%)");
            }
        }

        private void UpdateRequiredBlockInformation(CombinationSlotState state, long currentBlockIndex)
        {
            progressBar.maxValue = Math.Max(state.RequiredBlockIndex, 1);
            var diff = Math.Max(state.UnlockBlockIndex - currentBlockIndex, 1);
            progressBar.value = diff;
            requiredBlockIndexText.text = $"{diff}.";
        }

        private void UpdateButtonInformation(CombinationSlotState state, long currentBlockIndex)
        {
            var diff = state.UnlockBlockIndex - currentBlockIndex;
            var cost =
                RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);
            var inventory = States.Instance.CurrentAvatarState.inventory;
            var count = Util.GetHourglassCount(inventory, currentBlockIndex);
            hourglassCountText.text = cost.ToString();
            var isEnable = count >= cost;
            hourglassCountText.color = isEnable
                ? Palette.GetColor(ColorType.ButtonEnabled)
                : Palette.GetColor(ColorType.TextDenial);

            rapidCombinationButton.interactable = isEnable;
        }

        private void UpdateItemInformation(ItemUsable item)
        {
            itemView.SetData(new Item(item));
            itemNameText.text = TextHelper.GetItemNameInCombinationSlot(item);
        }

        private CombinationType GetCombinationType(CombinationSlotState state)
        {
            switch (state.Result)
            {
                case CombinationConsumable5.ResultModel craft:
                    return craft.itemUsable is Equipment
                        ? CombinationType.CraftEquipment
                        : CombinationType.CrateFood;

                case ItemEnhancement.ResultModel _:
                    return CombinationType.Enhancement;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state.Result), state.Result, null);
            }
        }

        private Information GetInformation(CombinationType type)
        {
            return _informations.FirstOrDefault(x => x.Type.Equals(type));
        }

        private void RapidCombination(CombinationSlotState state, int slotIndex, long currentBlockIndex)
        {
            var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                .First(r => r.ItemSubType == ItemSubType.Hourglass);
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var diff = state.UnlockBlockIndex - currentBlockIndex;
            var cost = RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);
            LocalLayerModifier.RemoveItem(avatarAddress, row.ItemId, cost);

            // Notify
            string formatKey;
            switch (state.Result)
            {
                case CombinationConsumable5.ResultModel combineResultModel:
                {
                    LocalLayerModifier.AddNewResultAttachmentMail(avatarAddress, combineResultModel.id,
                        currentBlockIndex);
                    if (combineResultModel.itemUsable is Equipment equipment)
                    {
                        formatKey = equipment.optionCountFromCombination == 4
                            ? "NOTIFICATION_COMBINATION_COMPLETE_GREATER"
                            : "NOTIFICATION_COMBINATION_COMPLETE";
                    }
                    else
                    {
                        formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                    }

                    break;
                }
                case ItemEnhancement.ResultModel enhancementResultModel:
                {
                    LocalLayerModifier.AddNewResultAttachmentMail(avatarAddress, enhancementResultModel.id,
                        currentBlockIndex);
                    switch (enhancementResultModel.enhancementResult)
                    {
                        case ItemEnhancement.EnhancementResult.GreatSuccess:
                            formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_GREATER";
                            break;
                        case ItemEnhancement.EnhancementResult.Success:
                            formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                            break;
                        case ItemEnhancement.EnhancementResult.Fail:
                            formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE_FAIL";
                            break;
                        default:
                            Debug.LogError(
                                $"Unexpected result.enhancementResult: {enhancementResultModel.enhancementResult}");
                            formatKey = "NOTIFICATION_ITEM_ENHANCEMENT_COMPLETE";
                            break;
                    }

                    break;
                }
                default:
                    Debug.LogError(
                        $"Unexpected state.Result: {state.Result}");
                    formatKey = "NOTIFICATION_COMBINATION_COMPLETE";
                    break;
            }

            var format = L10nManager.Localize(formatKey);
            Notification.CancelReserve(state.Result.itemUsable.TradableId);
            Notification.Push(MailType.Workshop, string.Format(format, state.Result.itemUsable.GetLocalizedName()));
            // ~Notify

            Game.Game.instance.ActionManager.RapidCombination(avatarAddress, slotIndex);
            States.Instance.RemoveSlotState(slotIndex);
            Find<CombinationSlots>().SetCaching(slotIndex, false);
        }
    }
}
