using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Nekoyume.Action;
using Nekoyume.EnumType;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.Model.Stat;
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
            public List<ItemOptionWithCountView> StatOptions;
            public List<ItemOptionView> SkillOptions;
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
        private TextMeshProUGUI timeText;

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
        private readonly List<IDisposable> _disposablesOfOnEnable = new List<IDisposable>();

        protected override void Awake()
        {
            base.Awake();

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
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Game.Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeOnBlockIndex)
                .AddTo(_disposablesOfOnEnable);

            CloseWidget = null;
        }

        protected override void OnDisable()
        {
            _disposablesOfOnEnable.DisposeAllAndClear();
            base.OnDisable();
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
            if (state?.Result.itemUsable is null)
            {
                return;
            }

            UpdateOption(type, state.Result);
            UpdateItemInformation(state.Result.itemUsable);
            UpdateButtonInformation(state, currentBlockIndex);
            UpdateRequiredBlockInformation(state, currentBlockIndex);
        }

        private void UpdateOption(CraftType type, AttachmentActionResult attachmentActionResult)
        {
            foreach (var information in _informations)
            {
                information.Icon.SetActive(information.Type.Equals(type));
                information.OptionContainer.SetActive(information.Type.Equals(type));
            }

            switch (attachmentActionResult)
            {
                case CombinationConsumable5.ResultModel cc5:
                    SetCombinationOption(GetInformation(type), cc5);
                    break;
                case ItemEnhancement7.ResultModel _:
                case ItemEnhancement.ResultModel _:
                    SetEnhancementOption(GetInformation(type), attachmentActionResult);
                    break;
                default:
                    Debug.LogError(
                        $"[{nameof(CombinationSlotPopup)}] Not supported type. {attachmentActionResult.GetType().FullName}");
                    break;
            }
        }

        private static void SetCombinationOption(Information information, CombinationConsumable5.ResultModel resultModel)
        {
            if (!resultModel.subRecipeId.HasValue)
            {
                foreach (var optionView in information.StatOptions)
                {
                    optionView.Hide();
                }

                foreach (var optionView in information.SkillOptions)
                {
                    optionView.Hide();
                }
                
                return;
            }
            
            var subRecipeRow = Game.Game.instance.TableSheets.EquipmentItemSubRecipeSheetV2.OrderedList
                .FirstOrDefault(e => e.Id == resultModel.subRecipeId);
            if (subRecipeRow is null)
            {
                Debug.LogError($"subRecipeRow is null. {resultModel.subRecipeId}");
                return;
            }

            if (!resultModel.itemUsable.TryGetOptionInfo(out var itemOptionInfo))
            {
                Debug.LogError("Failed to create ItemOptionInfo");
                return;
            }

            var optionRows = subRecipeRow.Options
                .Select(optionInfo => Game.Game.instance.TableSheets.EquipmentItemOptionSheet.OrderedList
                    .FirstOrDefault(optionRow => optionRow.Id == optionInfo.Id))
                .ToList();
            if (optionRows.Count != subRecipeRow.Options.Count)
            {
                Debug.LogError(
                    $"Failed to create optionRows with subRecipeRow.Options. Sub recipe id: {resultModel.subRecipeId}");
                return;
            }

            var statOptionRows = optionRows.Where(e => e.StatType != StatType.NONE).ToList();
            var format = L10nManager.Localize("UI_COMBINATION_POPUP_COMBINATION_RESULT_STATS");
            for (var i = 0; i < information.StatOptions.Count; i++)
            {
                var optionView = information.StatOptions[i];
                if (i >= statOptionRows.Count ||
                    i >= itemOptionInfo.StatOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var optionRow = statOptionRows[i];
                if (optionRow is null)
                {
                    optionView.Hide();
                    continue;
                }

                var (_, _, count) = itemOptionInfo.StatOptions[i];
                var text = string.Format(format, optionRow.StatType, optionRow.StatMin, optionRow.StatMax);
                optionView.UpdateView(text, string.Empty, count);
                optionView.Show();
            }

            format = L10nManager.Localize("UI_COMBINATION_POPUP_COMBINATION_RESULT_SKILLS");
            for (var i = 0; i < information.SkillOptions.Count; i++)
            {
                var optionView = information.SkillOptions[i];
                if (i >= itemOptionInfo.SkillOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var optionRow = statOptionRows[i];
                if (optionRow is null)
                {
                    optionView.Hide();
                    continue;
                }

                var (skillName, _, _) = itemOptionInfo.SkillOptions[i];
                var text = string.Format(format, skillName);
                optionView.UpdateView(text, string.Empty);
                optionView.Show();
            }
        }

        private static void SetEnhancementOption(Information information, AttachmentActionResult resultModel)
        {
            if (!(resultModel.itemUsable is Equipment equipment))
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
            
            if (!resultModel.itemUsable.TryGetOptionInfo(out var itemOptionInfo))
            {
                Debug.LogError("Failed to create ItemOptionInfo");
                return;
            }

            var format = L10nManager.Localize("UI_COMBINATION_POPUP_ENHANCEMENT_RESULT_STATS");
            for (var i = 0; i < information.StatOptions.Count; i++)
            {
                var optionView = information.StatOptions[i];
                if (i >= itemOptionInfo.StatOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var (type, _, count) = itemOptionInfo.StatOptions[i];
                var text = string.Format(
                    format,
                    type,
                    row.ExtraStatGrowthMin / GameConfig.TenThousand,
                    row.ExtraStatGrowthMax / GameConfig.TenThousand);
                optionView.UpdateView(text, string.Empty, count);
                optionView.Show();
            }

            format = L10nManager.Localize("UI_COMBINATION_POPUP_ENHANCEMENT_RESULT_SKILLS");
            for (var i = 0; i < information.SkillOptions.Count; i++)
            {
                var optionView = information.SkillOptions[i];
                if (i >= itemOptionInfo.SkillOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var (skillName, _, _) = itemOptionInfo.SkillOptions[i];
                var text = string.Format(
                    format,
                    skillName,
                    row.ExtraSkillDamageGrowthMin / GameConfig.TenThousand,
                    row.ExtraSkillDamageGrowthMax / GameConfig.TenThousand,
                    row.ExtraSkillChanceGrowthMin / GameConfig.TenThousand,
                    row.ExtraSkillChanceGrowthMax / GameConfig.TenThousand);
                optionView.UpdateView(text, string.Empty);
                optionView.Show();
            }
        }

        private void UpdateRequiredBlockInformation(CombinationSlotState state, long currentBlockIndex)
        {
            progressBar.maxValue = Math.Max(state.RequiredBlockIndex, 1);
            var diff = Math.Max(state.UnlockBlockIndex - currentBlockIndex, 1);
            progressBar.value = diff;
            requiredBlockIndexText.text = $"{diff}.";
            timeText.text = string.Format(L10nManager.Localize("UI_REMAINING_TIME"), Util.GetBlockToTime((int)diff));
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
