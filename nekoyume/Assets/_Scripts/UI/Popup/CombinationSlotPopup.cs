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
using Nekoyume.TableData;
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
        public enum CraftType
        {
            CombineEquipment,
            CombineConsumable,
            Enhancement,
        }

        [Serializable]
        public class Information
        {
            public CraftType Type;
            public GameObject Icon;
            public GameObject OptionContainer;
            public TextMeshProUGUI ItemLevel;
            public List<SlotPopupOptionView> Options;
        }

        [SerializeField]
        private SimpleItemView itemView;

        [SerializeField]
        private Slider progressBar;

        [SerializeField]
        private TextMeshProUGUI itemNameText;

        [SerializeField]
        private TextMeshProUGUI requiredBlockIndexText;

        [SerializeField]
        private TextMeshProUGUI hourglassCountText;

        [SerializeField]
        private Button rapidCombinationButton;

        [SerializeField]
        private Button bgButton;

        [SerializeField]
        private List<Information> _informations;

        private CraftType _craftType;
        private CombinationSlotState _slotState;
        private int _slotIndex;

        protected override void Awake()
        {
            base.Awake();

            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeOnBlockIndex).AddTo(gameObject);

            rapidCombinationButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                RapidCombination(_slotState, _slotIndex, Game.Game.instance.Agent.BlockIndex);
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
            UpdateInformation(_craftType, _slotState, currentBlockIndex);
        }

        public void Show(CombinationSlotState state, int slotIndex, long currentBlockIndex)
        {
            _slotState = state;
            _slotIndex = slotIndex;
            _craftType = GetCombinationType(state);
            UpdateInformation(_craftType, state, currentBlockIndex);
            base.Show();
        }

        private void UpdateInformation(CraftType type, CombinationSlotState state, long currentBlockIndex)
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

        private void UpdateOption(CraftType type, ItemUsable itemUsable)
        {
            foreach (var information in _informations)
            {
                information.Icon.SetActive(information.Type.Equals(type));
                information.OptionContainer.SetActive(information.Type.Equals(type));
            }

            switch (type)
            {
                case CraftType.CombineEquipment:
                case CraftType.CombineConsumable:
                    SetCombinationOption(GetInformation(type), itemUsable);
                    break;
                case CraftType.Enhancement:
                    SetEnhancementOption(GetInformation(type), itemUsable);
                    break;
            }
        }

        private static void SetCombinationOption(Information information, ItemUsable itemUsable)
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

        private static void SetEnhancementOption(Information information, ItemUsable itemUsable)
        {
            if (!(itemUsable is Equipment equipment))
            {
                return;
            }

            information.ItemLevel.text = $"+{equipment.level}";
            var sheet = Game.Game.instance.TableSheets.EnhancementCostSheetV2;
            var grade = equipment.Grade;
            var level = equipment.level;
            var row = sheet.OrderedList.FirstOrDefault(x => x.Grade == grade && x.Level == level);
            if (row is null)
            {
                Debug.LogError($"Not found row: {nameof(EnhancementCostSheetV2)} Grade({grade}) Level({level})");
                return;
            }

            foreach (var option in information.Options)
            {
                option.gameObject.SetActive(false);
            }

            var stats = itemUsable.StatsMap.GetStats().ToList();
            for (var i = 0; i < stats.Count; i++)
            {
                information.Options[i].gameObject.SetActive(true);
                information.Options[i].Set($"{stats[i].StatType} " +
                                           $"+({row.ExtraStatGrowthMin / GameConfig.TenThousand}% " +
                                           $"~ {row.ExtraStatGrowthMax / GameConfig.TenThousand}%)");
            }

            var skills = itemUsable.Skills;
            for (var i = 0; i < skills.Count; i++)
            {
                information.Options[i + stats.Count].gameObject.SetActive(true);
                information.Options[i + stats.Count].Set(
                    $"{skills[i].SkillRow.GetLocalizedName()} " +
                    $"{L10nManager.Localize("UI_SKILL_POWER")} : " +
                    $"+({row.ExtraSkillDamageGrowthMin / GameConfig.TenThousand}% " +
                    $"~ {row.ExtraSkillDamageGrowthMax / GameConfig.TenThousand}%) / " +
                    $"{L10nManager.Localize("UI_SKILL_CHANCE")} : " +
                    $"+({row.ExtraSkillChanceGrowthMin / GameConfig.TenThousand}% " +
                    $"~ {row.ExtraSkillChanceGrowthMax / GameConfig.TenThousand}%)");
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

        private static CraftType GetCombinationType(CombinationSlotState state)
        {
            switch (state.Result)
            {
                case CombinationConsumable5.ResultModel craft:
                    return craft.itemUsable is Equipment
                        ? CraftType.CombineEquipment
                        : CraftType.CombineConsumable;

                case ItemEnhancement.ResultModel _:
                    return CraftType.Enhancement;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state.Result), state.Result, null);
            }
        }

        private Information GetInformation(CraftType type)
        {
            return _informations.FirstOrDefault(x => x.Type.Equals(type));
        }

        private static void RapidCombination(CombinationSlotState state, int slotIndex, long currentBlockIndex)
        {
            var row = Game.Game.instance.TableSheets.MaterialItemSheet.Values
                .First(r => r.ItemSubType == ItemSubType.Hourglass);
            var avatarAddress = States.Instance.CurrentAvatarState.address;
            var diff = state.UnlockBlockIndex - currentBlockIndex;
            var cost = RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);
            LocalLayerModifier.RemoveItem(avatarAddress, row.ItemId, cost);

            switch (state.Result)
            {
                case CombinationConsumable5.ResultModel craft:
                    LocalLayerModifier.AddNewResultAttachmentMail(avatarAddress, craft.id, currentBlockIndex);
                    break;

                case ItemEnhancement.ResultModel enhancement:
                    LocalLayerModifier.AddNewResultAttachmentMail(avatarAddress, enhancement.id, currentBlockIndex);
                    break;
            }

            var format = L10nManager.Localize("NOTIFICATION_COMBINATION_COMPLETE");
            Notification.Push(MailType.Workshop, string.Format(CultureInfo.InvariantCulture, format,
                state.Result.itemUsable.GetLocalizedName()));
            Notification.CancelReserve(state.Result.itemUsable.ItemId);

            Game.Game.instance.ActionManager.RapidCombination(avatarAddress, slotIndex);
            States.Instance.RemoveSlotState(slotIndex);
            Find<CombinationSlots>().SetCaching(slotIndex, false);
        }
    }
}
