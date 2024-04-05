using System;
using System.Collections;
using System.Collections.Generic;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.Model.Mail;
using Nekoyume.State;
using Nekoyume.UI.Module;
using UnityEngine;
using System.Numerics;
using Nekoyume.Action;
using Nekoyume.Extensions;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine.UI;
using UnityEngine.UI.Extensions;

namespace Nekoyume.UI
{
    using Nekoyume.EnumType;
    using Nekoyume.UI.Module.Common;
    using System.Linq;
    using UniRx;

    public class Enhancement : Widget
    {
        [SerializeField]
        private EnhancementInventory enhancementInventory;

        [SerializeField]
        private ConditionalCostButton upgradeButton;

        [SerializeField]
        private ConditionalButton removeAllButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private UpgradeEquipmentSlot baseSlot;

        [SerializeField]
        private TextMeshProUGUI itemNameText;

        [SerializeField]
        private TextMeshProUGUI currentLevelText;

        [SerializeField]
        private TextMeshProUGUI nextLevelText;

        [SerializeField]
        private TextMeshProUGUI materialGuideText;

        [SerializeField]
        private EnhancementOptionView mainStatView;

        [SerializeField]
        private List<EnhancementOptionView> statViews;

        [SerializeField]
        private List<EnhancementSkillOptionView> skillViews;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private GameObject noneContainer;

        [SerializeField]
        private GameObject itemInformationContainer;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private PositionTooltip statTooltip;

        [SerializeField]
        private Slider expSlider;

        [SerializeField]
        private float minimumSliderEffectExtraDuration = 1f;

        [SerializeField]
        private AnimationCurve sliderEffectCurve;

        [SerializeField]
        private TextMeshProUGUI sliderPercentText;

        [SerializeField]
        private TextMeshProUGUI levelStateText;

        [SerializeField]
        private EnhancementSelectedMaterialItemScroll enhancementSelectedMaterialItemScroll;

        [SerializeField]
        private TimeBlock requierdBlockTimeViewr;

        [SerializeField]
        private TextMeshProUGUI currentEquipmentCP;
        [SerializeField]
        private TextMeshProUGUI nextEquipmentCP;

        private static readonly int HashToRegisterBase =
            Animator.StringToHash("RegisterBase");

        private static readonly int HashToPostRegisterBase =
            Animator.StringToHash("PostRegisterBase");

        private static readonly int HashToPostRegisterMaterial =
            Animator.StringToHash("PostRegisterMaterial");

        private static readonly int HashToUnregisterMaterial =
            Animator.StringToHash("UnregisterMaterial");

        private static readonly int HashToClose =
            Animator.StringToHash("Close");


        private EnhancementCostSheetV3 _costSheet;
        private BigInteger _costNcg = 0;
        private string _errorMessage;
        private IOrderedEnumerable<KeyValuePair<int, EnhancementCostSheetV3.Row>> _decendingbyExpCostSheet;
        private float _sliderAnchorPoint = 0;
        private int _levelAnchorPoint = 0;
        private Coroutine _sliderEffectCor;
        private UnityEngine.UI.Extensions.Scroller _matarialsScroller;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
            CloseWidget = Close;
        }

        public override void Initialize()
        {
            base.Initialize();

            _matarialsScroller = enhancementSelectedMaterialItemScroll.GetComponent<UnityEngine.UI.Extensions.Scroller>();

            upgradeButton.OnSubmitSubject
                .Subscribe(_ => OnSubmit())
                .AddTo(gameObject);

            _costSheet = Game.Game.instance.TableSheets.EnhancementCostSheetV3;
            _decendingbyExpCostSheet = _costSheet.OrderByDescending(r => r.Value.Exp);

            baseSlot.AddRemoveButtonAction(() => enhancementInventory.DeselectItem(true));
            removeAllButton.OnSubmitSubject
                .Subscribe(_ => enhancementInventory.DeselectItem(false))
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Clear();
            enhancementInventory.Set(UpdateInformation, enhancementSelectedMaterialItemScroll);
            if (_matarialsScroller != null)
                _matarialsScroller.Position = 0;
            base.Show(ignoreShowAnimation);
            if (enhancementInventory.TryGetCellByIndex(0, out var firstCell))
            {
                Game.Game.instance.Stage.TutorialController.SetTutorialTarget(new TutorialTarget
                {
                    type = TutorialTargetType.CombinationInventoryFirstCell,
                    rectTransform = (RectTransform)firstCell.transform
                });
            }

            if (enhancementInventory.TryGetCellByIndex(1, out var secondCell))
            {
                Game.Game.instance.Stage.TutorialController.SetTutorialTarget(new TutorialTarget
                {
                    type = TutorialTargetType.CombinationInventorySecondCell,
                    rectTransform = (RectTransform)secondCell.transform
                });
            }
        }

        public void Show(ItemSubType itemSubType, Guid itemId, bool ignoreShowAnimation = false)
        {
            Show(ignoreShowAnimation);
            StartCoroutine(CoSelect(itemSubType, itemId));
        }

        private IEnumerator CoSelect(ItemSubType itemSubType, Guid itemId)
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            enhancementInventory.Select(itemSubType, itemId);
        }

        private void Close()
        {
            animator.Play(HashToClose);
            Close(true);
            Find<CombinationMain>().Show();
        }

        private void OnSubmit()
        {
            var (baseItem, materialItems) = enhancementInventory.GetSelectedModels();

            //Equip Upgragd ToDO
            if (!IsInteractableButton(baseItem, materialItems))
            {
                NotificationSystem.Push(MailType.System, _errorMessage,
                    NotificationCell.NotificationType.Alert);
                return;
            }

            if (States.Instance.GoldBalanceState.Gold.MajorUnit < _costNcg)
            {
                _errorMessage = L10nManager.Localize("UI_NOT_ENOUGH_NCG");
                NotificationSystem.Push(MailType.System, _errorMessage,
                    NotificationCell.NotificationType.Alert);
                return;
            }

            EnhancementAction(baseItem, materialItems);
        }

        //Used for migrating and showing equipment before update
        private long GetItemExp(Equipment equipment)
        {
            return equipment.GetRealExp(Game.Game.instance.TableSheets.EquipmentItemSheet, _costSheet);
        }

        private void EnhancementAction(Equipment baseItem, List<Equipment> materialItems)
        {
            var slots = Find<CombinationSlotsPopup>();
            if (!slots.TryGetEmptyCombinationSlot(out var slotIndex))
            {
                return;
            }

            var sheet = Game.Game.instance.TableSheets.EnhancementCostSheetV3;

            var baseItemExp = GetItemExp(baseItem);
            var targetExp = baseItemExp + materialItems.Aggregate(0L, (total, m) => total + GetItemExp(m));
            int requiredBlockIndex = GetBlockIndex(baseItem, sheet, targetExp);

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            slots.SetCaching(avatarAddress, slotIndex, true, requiredBlockIndex,
                itemUsable: baseItem);

            NotificationSystem.Push(MailType.Workshop,
                L10nManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_START"),
                NotificationCell.NotificationType.Information);

            Game.Game.instance.ActionManager
                .ItemEnhancement(baseItem, materialItems, slotIndex, _costNcg).Subscribe();

            enhancementInventory.DeselectItem(true);

            StartCoroutine(CoCombineNPCAnimation(baseItem, requiredBlockIndex, Clear));
        }

        private int GetBlockIndex(Equipment baseItem, EnhancementCostSheetV3 sheet, long targetExp)
        {
            int requiredBlockIndex = 0;
            try
            {
                if (!ItemEnhancement.TryGetRow(baseItem, _costSheet, out var baseItemCostRow))
                {
                    baseItemCostRow = new EnhancementCostSheetV3.Row();
                }

                EnhancementCostSheetV3.Row targetRow;
                targetRow = _decendingbyExpCostSheet
                .First(row =>
                    row.Value.ItemSubType == baseItem.ItemSubType &&
                    row.Value.Grade == baseItem.Grade &&
                    row.Value.Exp <= targetExp
                ).Value;

                requiredBlockIndex = targetRow.RequiredBlockIndex - baseItemCostRow.RequiredBlockIndex;
            }
            catch
            {
                requiredBlockIndex = 0;
            }

            return requiredBlockIndex;
        }

        private void Clear()
        {
            ClearInformation();
            enhancementInventory.DeselectItem(true);
        }

        private bool IsInteractableButton(IItem item, List<Equipment> materials)
        {
            if (item is null || materials.Count == 0)
            {
                _errorMessage = L10nManager.Localize("UI_SELECT_MATERIAL_TO_UPGRADE");
                return false;
            }

            if (ReactiveAvatarState.ActionPoint < GameConfig.EnhanceEquipmentCostAP)
            {
                _errorMessage = L10nManager.Localize("NOTIFICATION_NOT_ENOUGH_ACTION_POWER");
                return false;
            }

            if (!Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out _))
            {
                _errorMessage = L10nManager.Localize("NOTIFICATION_NOT_ENOUGH_SLOTS");
                return false;
            }

            return true;
        }

        private IEnumerator CoCombineNPCAnimation(
            ItemBase itemBase,
            long blockIndex,
            System.Action action)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SpeechBubbleWithItem.SetItemMaterial(new Item(itemBase));
            loadingScreen.SetCloseAction(action);
            Push();
            yield return new WaitForSeconds(.5f);

            var format = L10nManager.Localize("UI_COST_BLOCK");
            var quote = string.Format(format, blockIndex);
            loadingScreen.AnimateNPC(CombinationLoadingScreen.SpeechBubbleItemType.Equipment, quote);
        }

        private void SliderGageEffect(Equipment equipment, long targetExp, int targetLevel)
        {
            var expTable = _costSheet.Values.
                            Where((r) =>
                            r.Grade == equipment.Grade &&
                            r.ItemSubType == equipment.ItemSubType).Select((r) => r.Exp).ToList();
            expTable.Insert(0, 0);

            float GetSliderGage(float exp, out long nextExp)
            {
                nextExp = 0;
                for (int i = 0; i < expTable.Count; i++)
                {
                    if(expTable[i] > exp)
                    {
                        nextExp = expTable[i];
                        _levelAnchorPoint = i - 1;
                        return Mathf.InverseLerp(expTable[i-1], expTable[i], exp);
                    }
                }
                nextExp = expTable[expTable.Count - 1];
                return 1;
            }

            IEnumerator CoroutineEffect(float duration)
            {
                float elapsedTime = 0;
                float startAnchorPoint = _sliderAnchorPoint;
                while (elapsedTime <= duration)
                {
                    elapsedTime += Time.deltaTime;

                    _sliderAnchorPoint = Mathf.Lerp(startAnchorPoint, targetExp, sliderEffectCurve.Evaluate(elapsedTime / duration));
                    expSlider.value = GetSliderGage(_sliderAnchorPoint, out var nextExp);
                    sliderPercentText.text = $"{(int)(expSlider.value * 100)}% {(long)_sliderAnchorPoint}/{nextExp}";
                    yield return new WaitForEndOfFrame();
                }
                _sliderAnchorPoint = targetExp;
                expSlider.value = GetSliderGage(_sliderAnchorPoint, out var lastExp);
                sliderPercentText.text = $"{(int)(expSlider.value * 100)}% {(long)_sliderAnchorPoint}/{lastExp}";
                yield return 0;
            }

            if (_sliderEffectCor != null)
                StopCoroutine(_sliderEffectCor);

            float extraDuration = 0;
            int levelDiff = Mathf.Abs(_levelAnchorPoint - targetLevel);
            if (levelDiff > 0)
            {
                extraDuration = Mathf.Lerp(minimumSliderEffectExtraDuration, 3f, (float)levelDiff / 20);
            }

            _sliderEffectCor = StartCoroutine(CoroutineEffect(sliderEffectCurve.keys.Last().time + extraDuration));
        }

        private void ClearInformation()
        {
            itemNameText.text = string.Empty;
            currentLevelText.text = string.Empty;
            nextLevelText.text = string.Empty;

            levelStateText.text = string.Empty;

            mainStatView.gameObject.SetActive(false);
            foreach (var stat in statViews)
            {
                stat.gameObject.SetActive(false);
            }

            foreach (var skill in skillViews)
            {
                skill.gameObject.SetActive(false);
            }
        }

        private int UpgradeStat(int baseStat, int upgradeStat)
        {
            var result = baseStat * upgradeStat.NormalizeFromTenThousandths();
            if (result > 0)
            {
                result = Math.Max(1.0m, result);
            }

            return (int)result;
        }

        private long UpgradeStat(long baseStat, int upgradeStat)
        {
            var result = baseStat * upgradeStat.NormalizeFromTenThousandths();
            if (result > 0)
            {
                result = Math.Max(1.0m, result);
            }

            return (long)result;
        }

        private void UpdateInformation(EnhancementInventoryItem baseModel,
            List<EnhancementInventoryItem> materialModels)
        {
            _costNcg = 0;
            if (baseModel is null)
            {
                baseSlot.RemoveMaterial();
                noneContainer.SetActive(true);
                itemInformationContainer.SetActive(false);
                animator.Play(HashToRegisterBase);
                enhancementSelectedMaterialItemScroll.UpdateData(materialModels, _matarialsScroller?.Position != 0);
                closeButton.interactable = true;
                ClearInformation();

                if (_sliderEffectCor != null)
                    StopCoroutine(_sliderEffectCor);

                expSlider.value = 0;
                _sliderAnchorPoint = 0;
                _levelAnchorPoint = 0;
                sliderPercentText.text = "0% 0/0";
                removeAllButton.Interactable = false;
            }
            else
            {
                if (!baseSlot.IsExist)
                {
                    animator.Play(HashToPostRegisterBase);
                }

                baseSlot.AddMaterial(baseModel.ItemBase);

                enhancementSelectedMaterialItemScroll.UpdateData(materialModels);
                if (materialModels.Count != 0)
                {
                    if(materialModels.Count > 5 || _matarialsScroller?.Position != 0)
                        enhancementSelectedMaterialItemScroll.JumpTo(materialModels[materialModels.Count - 1], 0);

                    animator.Play(HashToPostRegisterMaterial);
                    noneContainer.SetActive(false);
                }
                else
                {
                    if(_matarialsScroller?.Position != 0)
                        enhancementSelectedMaterialItemScroll.RawJumpTo(0, 0);

                    noneContainer.SetActive(true);
                }

                var equipment = baseModel.ItemBase as Equipment;
                if (!ItemEnhancement.TryGetRow(equipment, _costSheet, out var baseItemCostRow))
                {
                    baseItemCostRow = new EnhancementCostSheetV3.Row();
                }

                var baseModelExp = GetItemExp(equipment);
                var targetExp = baseModelExp + materialModels.Aggregate(0L, (total, m) => total + GetItemExp((m.ItemBase as Equipment)));

                EnhancementCostSheetV3.Row targetRow;
                try
                {
                    targetRow = _decendingbyExpCostSheet
                    .First(row =>
                        row.Value.ItemSubType == equipment.ItemSubType &&
                        row.Value.Grade == equipment.Grade &&
                        row.Value.Exp <= targetExp
                    ).Value;
                }
                catch
                {
                    targetRow = baseItemCostRow;
                }

                itemInformationContainer.SetActive(true);

                ClearInformation();

                removeAllButton.Interactable = materialModels.Count >= 2;

                _costNcg = targetRow.Cost - baseItemCostRow.Cost;
                upgradeButton.SetCost(CostType.NCG, (long)_costNcg);

                var slots = Find<CombinationSlotsPopup>();
                upgradeButton.Interactable = slots.TryGetEmptyCombinationSlot(out var _);

                itemNameText.text = equipment.GetLocalizedNonColoredName();
                currentLevelText.text = $"+{equipment.level}";
                nextLevelText.text = $"+{targetRow.Level}";

                var targetRangeRows = _costSheet.Values.
                    Where((r) =>
                    r.Grade == equipment.Grade &&
                    r.ItemSubType == equipment.ItemSubType &&
                    equipment.level <= r.Level &&
                    r.Level <= targetRow.Level + 1
                    ).ToList();

                if (equipment.level == 0)
                {
                    targetRangeRows.Insert(0, new EnhancementCostSheetV3.Row());
                }

                if (targetRangeRows.Count < 2)
                {
                    NcDebug.LogError("[Enhancement] Faild Get TargetRangeRows");
                }
                else
                {
                    SliderGageEffect(equipment, targetExp, targetRow.Level);
                }

                levelStateText.text = $"Lv. {targetRow.Level}/{ItemEnhancement.GetEquipmentMaxLevel(equipment, _costSheet)}";

                //check Current CP
                currentEquipmentCP.text = Nekoyume.Battle.CPHelper.GetCP(equipment).ToString();

                var itemOptionInfo = new ItemOptionInfo(equipment);
                var baseStatMin = itemOptionInfo.MainStat.baseValue;
                var baseStatMax = itemOptionInfo.MainStat.baseValue;
                var statOptionsMin = itemOptionInfo.StatOptions.Select((v) => v.value).ToList();
                var statOptionsMax = itemOptionInfo.StatOptions.Select((v) => v.value).ToList();
                var skillChancesMin = itemOptionInfo.SkillOptions.Select((v) => v.chance).ToList();
                var skillChancesMax = itemOptionInfo.SkillOptions.Select((v) => v.chance).ToList();
                var skillPowersMin = itemOptionInfo.SkillOptions.Select((v) => v.power).ToList();
                var skillPowersMax = itemOptionInfo.SkillOptions.Select((v) => v.power).ToList();
                var skillStatPowerRatioMin = itemOptionInfo.SkillOptions.Select((v) => v.statPowerRatio).ToList();
                var skillStatPowerRatioMax = itemOptionInfo.SkillOptions.Select((v) => v.statPowerRatio).ToList();

                if (equipment.level != targetRow.Level)
                {
                    for (int i = 1; i < targetRangeRows.Count - 1; i++)
                    {
                        baseStatMin += UpgradeStat(baseStatMin, targetRangeRows[i].BaseStatGrowthMin);
                        baseStatMax += UpgradeStat(baseStatMax, targetRangeRows[i].BaseStatGrowthMax);

                        for (int statIndex = 0; statIndex < itemOptionInfo.StatOptions.Count; statIndex++)
                        {
                            statOptionsMin[statIndex] += UpgradeStat(statOptionsMin[statIndex], targetRangeRows[i].ExtraStatGrowthMin);
                            statOptionsMax[statIndex] += UpgradeStat(statOptionsMax[statIndex], targetRangeRows[i].ExtraStatGrowthMax);
                        }

                        for (int skillIndex = 0; skillIndex < itemOptionInfo.SkillOptions.Count; skillIndex++)
                        {
                            skillChancesMin[skillIndex] += UpgradeStat(skillChancesMin[skillIndex], targetRangeRows[i].ExtraSkillChanceGrowthMin);
                            skillChancesMax[skillIndex] += UpgradeStat(skillChancesMax[skillIndex], targetRangeRows[i].ExtraSkillChanceGrowthMax);

                            skillPowersMin[skillIndex] += UpgradeStat(skillPowersMin[skillIndex], targetRangeRows[i].ExtraSkillDamageGrowthMin);
                            skillPowersMax[skillIndex] += UpgradeStat(skillPowersMax[skillIndex], targetRangeRows[i].ExtraSkillDamageGrowthMax);

                            skillStatPowerRatioMin[skillIndex] += UpgradeStat(skillStatPowerRatioMin[skillIndex], targetRangeRows[i].ExtraSkillDamageGrowthMin);
                            skillStatPowerRatioMax[skillIndex] += UpgradeStat(skillStatPowerRatioMax[skillIndex], targetRangeRows[i].ExtraSkillDamageGrowthMax);
                        }
                    }
                }

                decimal nextCp = 0m;
                nextCp += Nekoyume.Battle.CPHelper.GetStatCP(itemOptionInfo.MainStat.type, baseStatMax);
                for (int statIndex = 0; statIndex < itemOptionInfo.StatOptions.Count; statIndex++)
                {
                    nextCp += Nekoyume.Battle.CPHelper.GetStatCP(itemOptionInfo.StatOptions[statIndex].type, statOptionsMax[statIndex]);
                }
                nextCp = nextCp * Nekoyume.Battle.CPHelper.GetSkillsMultiplier(itemOptionInfo.SkillOptions.Count);
                nextEquipmentCP.text = Nekoyume.Battle.CPHelper.DecimalToInt(nextCp).ToString();

                long requiredBlockIndex = GetBlockIndex(equipment, _costSheet, targetExp);
                requierdBlockTimeViewr.SetTimeBlock($"{requiredBlockIndex:#,0}", requiredBlockIndex.BlockRangeToTimeSpanString());

                //StatView
                mainStatView.gameObject.SetActive(true);
                var statType = itemOptionInfo.MainStat.type;
                mainStatView.Set(statType.ToString(),
                    statType.ValueToString(itemOptionInfo.MainStat.baseValue),
                    $"{statType.ValueToShortString(baseStatMin)} ~ {statType.ValueToShortString(baseStatMax)}");
                mainStatView.SetDescriptionButton(() =>
                {
                    statTooltip.transform.position = mainStatView.DescriptionPosition;
                    statTooltip.Set("", $"{statType.ValueToString(baseStatMin)} ~ {statType.ValueToString(baseStatMax)}<sprite name=icon_Arrow>");
                    statTooltip.gameObject.SetActive(true);
                });

                for (int statIndex = 0; statIndex < itemOptionInfo.StatOptions.Count; statIndex++)
                {
                    var optionStatType = itemOptionInfo.StatOptions[statIndex].type;
                    statViews[statIndex].gameObject.SetActive(true);
                    statViews[statIndex].Set(optionStatType.ToString(),
                            optionStatType.ValueToString(itemOptionInfo.StatOptions[statIndex].value),
                            $"{optionStatType.ValueToShortString(statOptionsMin[statIndex])} ~ {optionStatType.ValueToShortString(statOptionsMax[statIndex])}",
                            itemOptionInfo.StatOptions[statIndex].count);

                    var tooltipContext = $"{optionStatType.ValueToString(statOptionsMin[statIndex])} ~ {optionStatType.ValueToString(statOptionsMax[statIndex])}<sprite name=icon_Arrow>";
                    var statView = statViews[statIndex];
                    statViews[statIndex].SetDescriptionButton(() =>
                    {
                        statTooltip.transform.position = statView.DescriptionPosition;;
                        statTooltip.Set("", tooltipContext);
                        statTooltip.gameObject.SetActive(true);
                    });
                }

                for (int skillIndex = 0; skillIndex < itemOptionInfo.SkillOptions.Count; skillIndex++)
                {
                    skillViews[skillIndex].gameObject.SetActive(true);
                    var skillRow = itemOptionInfo.SkillOptions[skillIndex].skillRow;
                    var chanceText = $"<color=#FBF0B8>({itemOptionInfo.SkillOptions[skillIndex].chance}% > <color=#E3C32C>{skillChancesMin[skillIndex]}~{skillChancesMax[skillIndex]}%</color><sprite name=icon_Arrow>)</color>";
                    string valueText = string.Empty;

                    var currentEffect = SkillExtensions.EffectToString(
                                skillRow.Id,
                                skillRow.SkillType,
                                itemOptionInfo.SkillOptions[skillIndex].power,
                                itemOptionInfo.SkillOptions[skillIndex].statPowerRatio,
                                itemOptionInfo.SkillOptions[skillIndex].refStatType);

                    var targetEffectMin = SkillExtensions.EffectToString(
                                                    skillRow.Id,
                                                    skillRow.SkillType,
                                                    skillPowersMin[skillIndex],
                                                    skillStatPowerRatioMin[skillIndex],
                                                    itemOptionInfo.SkillOptions[skillIndex].refStatType);

                    var targetEffectMax = SkillExtensions.EffectToString(
                                                    skillRow.Id,
                                                    skillRow.SkillType,
                                                    skillPowersMax[skillIndex],
                                                    skillStatPowerRatioMax[skillIndex],
                                                    itemOptionInfo.SkillOptions[skillIndex].refStatType);
                    valueText = $"<color=#FBF0B8>({currentEffect} > <color=#E3C32C>{targetEffectMin.Replace("%", "")}~{targetEffectMax}</color><sprite name=icon_Arrow>)</color>";

                    skillViews[skillIndex].Set(skillRow.GetLocalizedName(), skillRow.SkillType, skillRow.Id, skillRow.Cooldown, chanceText, valueText);
                }
            }
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickCombinationInventoryFirstCell()
        {
            var item = enhancementInventory.GetEnabledItem(0);
            if (item.ItemBase is not Equipment equipment)
            {
                return;
            }

            enhancementInventory.Select(equipment.ItemSubType, equipment.ItemId);
        }


        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickCombinationInventorySecondCell()
        {
            var item = enhancementInventory.GetEnabledItem(1);
            if (item.ItemBase is not Equipment equipment)
            {
                return;
            }

            enhancementInventory.Select(equipment.ItemSubType, equipment.ItemId);
        }


        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickCombinationDeleteButton()
        {
            enhancementInventory.DeselectItem(true);
        }
    }
}
