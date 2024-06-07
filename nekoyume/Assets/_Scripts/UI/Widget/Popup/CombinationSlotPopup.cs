using System;
using System.Collections.Generic;
using System.Linq;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Nekoyume.Game;
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
            public List<ItemOptionView> MainStatViews;
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
        private TextMeshProUGUI requiredTimeText;

        [SerializeField]
        private TextMeshProUGUI timeText;

        [SerializeField]
        private ConditionalCostButton rapidCombinationButton;

        [SerializeField]
        private Button bgButton;

        [SerializeField]
        private List<Information> _informations;

        private CraftType _craftType;
        private CombinationSlotState _slotState;
        private int _slotIndex;
        private readonly List<IDisposable> _disposablesOfOnEnable = new();

        protected override void Awake()
        {
            base.Awake();

            rapidCombinationButton.OnSubmitSubject
                .Subscribe(_ =>
                {
                    AudioController.PlayClick();
                    Game.instance.ActionManager.RapidCombination(_slotState, _slotIndex).Subscribe();
                    var avatarAddress = States.Instance.CurrentAvatarState.address;
                    Find<CombinationSlotsPopup>().SetCaching(
                        avatarAddress,
                        _slotIndex,
                        true,
                        slotType: CombinationSlot.SlotType.WaitingReceive);
                    Close();
                })
                .AddTo(gameObject);

            bgButton.onClick.AddListener(() =>
            {
                AudioController.PlayClick();
                Close();
            });
        }

        protected override void OnEnable()
        {
            base.OnEnable();

            Game.instance.Agent.BlockIndexSubject.ObserveOnMainThread()
                .Subscribe(SubscribeOnBlockIndex)
                .AddTo(_disposablesOfOnEnable);
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

            UpdateOption(type, state);
            UpdateItemInformation(state.Result.itemUsable);
            UpdateButtonInformation(state, currentBlockIndex);
            UpdateRequiredBlockInformation(state, currentBlockIndex);
        }

        private void UpdateOption(CraftType type, CombinationSlotState slotState)
        {
            foreach (var information in _informations)
            {
                information.Icon.SetActive(information.Type.Equals(type));
                information.OptionContainer.SetActive(information.Type.Equals(type));
            }

            switch (slotState.Result)
            {
                case CombinationConsumable5.ResultModel cc5:
                    SetCombinationOption(GetInformation(type), cc5);
                    break;
                case ItemEnhancement7.ResultModel ie7:
                    SetEnhancementOption(GetInformation(type), ie7);
                    break;
                case ItemEnhancement13.ResultModel ie:
                    SetEnhancementOption(GetInformation(type), ie);
                    break;
                default:
                    NcDebug.LogError(
                        $"[{nameof(CombinationSlotPopup)}] Not supported type. {slotState.Result.GetType().FullName}");
                    break;
            }
        }

        private static void SetCombinationOption(
            Information information,
            CombinationConsumable5.ResultModel resultModel)
        {
            if (!resultModel.itemUsable.TryGetOptionInfo(out var itemOptionInfo))
            {
                NcDebug.LogError("Failed to create ItemOptionInfo");
                return;
            }

            // Consumable case
            if (!resultModel.subRecipeId.HasValue)
            {
                for (var i = 0; i < information.MainStatViews.Count; i++)
                {
                    var mainStatView = information.MainStatViews[i];
                    if (i >= itemOptionInfo.StatOptions.Count)
                    {
                        mainStatView.Hide();
                        continue;
                    }

                    var (type, value, _) = itemOptionInfo.StatOptions[i];
                    mainStatView.UpdateView($"{type} {type.ValueToString(value)}", string.Empty);
                    mainStatView.Show();
                }

                return;
            }

            var statType = itemOptionInfo.MainStat.type;
            var statValueString = statType.ValueToString(itemOptionInfo.MainStat.totalValue);
            information.MainStatViews[0].UpdateView($"{statType} {statValueString}", string.Empty);

            for (var i = 0; i < information.StatOptions.Count; i++)
            {
                var optionView = information.StatOptions[i];
                if (i >= itemOptionInfo.StatOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var (type, value, count) = itemOptionInfo.StatOptions[i];
                optionView.UpdateView($"{type} +{type.ValueToString(value)}", string.Empty, count);
                optionView.Show();
            }

            for (var i = 0; i < information.SkillOptions.Count; i++)
            {
                var optionView = information.SkillOptions[i];
                if (i >= itemOptionInfo.SkillOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var (skillRow, power, chance, ratio, type) = itemOptionInfo.SkillOptions[i];
                var powerText = SkillExtensions.EffectToString(skillRow.Id, skillRow.SkillType, power, ratio, type);
                optionView.UpdateView($"{skillRow.GetLocalizedName()} {powerText} / {chance}%",
                    string.Empty);
                optionView.Show();
            }
        }

        private static void SetEnhancementOption(
            Information information,
            ItemEnhancement7.ResultModel resultModel)
        {
            if (resultModel.itemUsable is not Equipment equipment)
            {
                NcDebug.LogError("resultModel.itemUsable is not Equipment");
                return;
            }

            var sheet = Game.instance.TableSheets.EnhancementCostSheetV2;
            var grade = equipment.Grade;
            var level = equipment.level;
            var row = sheet.OrderedList.FirstOrDefault(x => x.Grade == grade && x.Level == level);
            if (row is null)
            {
                NcDebug.LogError($"Not found row: {nameof(EnhancementCostSheetV2)} Grade({grade}) Level({level})");
                return;
            }

            if (!resultModel.itemUsable.TryGetOptionInfo(out var itemOptionInfo))
            {
                NcDebug.LogError("Failed to create ItemOptionInfo");
                return;
            }

            var format = "{0} +({1:N0}% - {2:N0}%)";
            var mainStatView = information.MainStatViews[0];
            if (row.BaseStatGrowthMin == 0 && row.BaseStatGrowthMax == 0)
            {
                mainStatView.Hide();
            }
            else
            {
                mainStatView.UpdateView(
                    string.Format(
                        format,
                        itemOptionInfo.MainStat.type,
                        row.BaseStatGrowthMin.NormalizeFromTenThousandths() * 100,
                        row.BaseStatGrowthMax.NormalizeFromTenThousandths() * 100),
                    string.Empty);
                mainStatView.Show();
            }

            for (var i = 0; i < information.StatOptions.Count; i++)
            {
                var optionView = information.StatOptions[i];
                if (row.ExtraStatGrowthMin == 0 && row.ExtraStatGrowthMax == 0 ||
                    i >= itemOptionInfo.StatOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var (type, _, count) = itemOptionInfo.StatOptions[i];
                var text = string.Format(
                    format,
                    type,
                    row.ExtraStatGrowthMin.NormalizeFromTenThousandths() * 100,
                    row.ExtraStatGrowthMax.NormalizeFromTenThousandths() * 100);
                optionView.UpdateView(text, string.Empty, count);
                optionView.Show();
            }

            format = "{0} +({1:N0}% - {2:N0}%) / ({3:N0}% - {4:N0}%)";
            for (var i = 0; i < information.SkillOptions.Count; i++)
            {
                var optionView = information.SkillOptions[i];
                if (row.ExtraSkillDamageGrowthMin == 0 && row.ExtraSkillDamageGrowthMax == 0 &&
                    row.ExtraSkillChanceGrowthMin == 0 && row.ExtraSkillChanceGrowthMax == 0 ||
                    i >= itemOptionInfo.SkillOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var (skillRow, _, _, _, _) = itemOptionInfo.SkillOptions[i];
                var text = string.Format(
                    format,
                    skillRow.GetLocalizedName(),
                    row.ExtraSkillDamageGrowthMin.NormalizeFromTenThousandths() * 100,
                    row.ExtraSkillDamageGrowthMax.NormalizeFromTenThousandths() * 100,
                    row.ExtraSkillChanceGrowthMin.NormalizeFromTenThousandths() * 100,
                    row.ExtraSkillChanceGrowthMax.NormalizeFromTenThousandths() * 100);
                optionView.UpdateView(text, string.Empty);
                optionView.Show();
            }
        }

        private static void SetEnhancementOption(
            Information information,
            ItemEnhancement13.ResultModel resultModel)
        {
            if (resultModel.itemUsable is not Equipment equipment)
            {
                NcDebug.LogError("resultModel.itemUsable is not Equipment");
                return;
            }

            if (resultModel.preItemUsable is not Equipment preEquipment)
            {
                NcDebug.LogError("resultModel.preItemUsable is not Equipment");
                return;
            }

            var itemOptionInfoPre = new ItemOptionInfo(preEquipment);
            var itemOptionInfo = new ItemOptionInfo(equipment);

            var statType = itemOptionInfo.MainStat.type;
            var statValueString = statType.ValueToString(itemOptionInfo.MainStat.totalValue);
            var statRate = RateOfChange(
                itemOptionInfoPre.MainStat.totalValue,
                itemOptionInfo.MainStat.totalValue);
            var statRateString = statRate > 0 ? $" (+{statRate}%)" : string.Empty;

            var mainStatView = information.MainStatViews[0];
            mainStatView.UpdateView(
                $"{statType} {statValueString}{statRateString}",
                string.Empty);
            mainStatView.Show();

            for (var i = 0; i < information.StatOptions.Count; i++)
            {
                var optionView = information.StatOptions[i];
                if (i >= itemOptionInfo.StatOptions.Count &&
                    i >= itemOptionInfoPre.StatOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var (type, value, count) = itemOptionInfo.StatOptions[i];
                var (_, preValue, _) = itemOptionInfoPre.StatOptions[i];
                var rate = RateOfChange(preValue, value);
                var rateString = rate > 0 ? $" (+{rate}%)" : string.Empty;

                optionView.UpdateView(
                    $"{type} +{type.ValueToString(value)}{rateString}",
                    string.Empty, count);
                optionView.Show();
            }

            for (var i = 0; i < information.SkillOptions.Count; i++)
            {
                var optionView = information.SkillOptions[i];
                if (i >= itemOptionInfo.SkillOptions.Count &&
                    i >= itemOptionInfoPre.SkillOptions.Count)
                {
                    optionView.Hide();
                    continue;
                }

                var (skillRow, power, chance, ratio, type) = itemOptionInfo.SkillOptions[i];
                var (_, prePower, preChance, preRatio, _) = itemOptionInfoPre.SkillOptions[i];
                long powerRate = 0;

                if (prePower != power)
                {
                    powerRate = RateOfChange(prePower, power);
                }
                else if (preRatio != ratio)
                {
                    powerRate = RateOfChange(preRatio, ratio);
                }

                var chancePlus = chance - preChance;
                var powerRateString = powerRate > 0 ? $" (+{powerRate}%)" : string.Empty;
                var chanceRateString = chancePlus > 0 ? $" (+{chancePlus}%p)" : string.Empty;
                var powerText = SkillExtensions.EffectToString(skillRow.Id, skillRow.SkillType, power, ratio, type);
                optionView.UpdateView(
                    $"{skillRow.GetLocalizedName()} {powerText}{powerRateString} / {chance}%{chanceRateString}",
                    string.Empty);
                optionView.Show();
            }
        }

        private void UpdateRequiredBlockInformation(CombinationSlotState state, long currentBlockIndex)
        {
            progressBar.maxValue = Math.Max(state.RequiredBlockIndex, 1);
            var diff = Math.Max(state.UnlockBlockIndex - currentBlockIndex, 1);
            progressBar.value = diff;
            requiredBlockIndexText.text = $"{diff}";
            requiredTimeText.text = $"({diff.BlockRangeToTimeSpanString(true)})";
            timeText.text = L10nManager.Localize("UI_REMAINING_TIME", diff.BlockRangeToTimeSpanString(true));
        }

        private void UpdateButtonInformation(CombinationSlotState state, long currentBlockIndex)
        {
            var diff = state.UnlockBlockIndex - currentBlockIndex;
            int cost;
            if (state.PetId.HasValue &&
                States.Instance.PetStates.TryGetPetState(state.PetId.Value, out var petState))
            {
                cost = PetHelper.CalculateDiscountedHourglass(
                    diff,
                    States.Instance.GameConfigState.HourglassPerBlock,
                    petState,
                    TableSheets.Instance.PetOptionSheet);
            }
            else
            {
                cost = RapidCombination0.CalculateHourglassCount(States.Instance.GameConfigState, diff);
            }
            rapidCombinationButton.SetCost(CostType.Hourglass, cost);
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

                case ItemEnhancement7.ResultModel _:
                case ItemEnhancement9.ResultModel _:
                case ItemEnhancement10.ResultModel _:
                case ItemEnhancement13.ResultModel _:
                    return CraftType.Enhancement;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state.Result), state.Result, null);
            }
        }

        private Information GetInformation(CraftType type)
        {
            return _informations.FirstOrDefault(x => x.Type.Equals(type));
        }

        private static long RateOfChange(decimal previous, decimal current)
        {
            return (long)Math.Round((current - previous) / previous * 100);
        }
    }
}
