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
using Nekoyume.Battle;
using Nekoyume.Extensions;
using Nekoyume.Helper;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Scroller;
using TMPro;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    using Module.Common;
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
        private EnhancementExpSlider enhancementExpSlider;

        [SerializeField]
        private TextMeshProUGUI levelStateText;

        [SerializeField]
        private EnhancementSelectedMaterialItemScroll enhancementSelectedMaterialItemScroll;

        [SerializeField]
        private TimeBlock requiredBlockTimeViewer;

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

        private BigInteger _costNcg = 0;
        private string _errorMessage;
        private UnityEngine.UI.Extensions.Scroller _materialsScroller;

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
            CloseWidget = Close;
        }

        public override void Initialize()
        {
            base.Initialize();

            _materialsScroller = enhancementSelectedMaterialItemScroll.GetComponent<UnityEngine.UI.Extensions.Scroller>();

            upgradeButton.OnSubmitSubject
                .Subscribe(_ => OnSubmit())
                .AddTo(gameObject);

            baseSlot.AddRemoveButtonAction(() => enhancementInventory.DeselectBaseItem());
            removeAllButton.OnSubmitSubject
                .Subscribe(_ => enhancementInventory.DeselectAllMaterialItems())
                .AddTo(gameObject);
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Clear();
            enhancementInventory.Set(UpdateInformation, enhancementSelectedMaterialItemScroll);
            if (_materialsScroller != null)
                _materialsScroller.Position = 0;
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
            var (baseItem, materialItems, hammers) = enhancementInventory.GetSelectedModels();

            // Equip Upgrade ToDO
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

            EnhancementAction(baseItem, materialItems, hammers);
        }

        //Used for migrating and showing equipment before update
        private long GetItemExp(Equipment equipment)
        {
            return equipment.GetRealExp(
                Game.Game.instance.TableSheets.EquipmentItemSheet,
                Game.Game.instance.TableSheets.EnhancementCostSheetV3);
        }

        private void EnhancementAction(
            Equipment baseItem,
            List<Equipment> materialItems,
            Dictionary<int, int> hammers)
        {
            var slots = Find<CombinationSlotsPopup>();
            if (!slots.TryGetEmptyCombinationSlot(out var slotIndex))
            {
                return;
            }

            var equipmentItemSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
            var enhancementCostSheet = Game.Game.instance.TableSheets.EnhancementCostSheetV3;
            var baseModelExp = baseItem.GetRealExp(equipmentItemSheet, enhancementCostSheet);
            var materialItemsExp = materialItems.Sum(equipment =>
                equipment.GetRealExp(equipmentItemSheet, enhancementCostSheet));
            var hammersExp = hammers.Sum(pair =>
                Equipment.GetHammerExp(pair.Key, enhancementCostSheet) * pair.Value);
            var targetExp = baseModelExp + materialItemsExp + hammersExp;

            int requiredBlockIndex;
            try
            {
                var baseItemCostRows = enhancementCostSheet.Values
                    .Where(row => row.ItemSubType == baseItem.ItemSubType &&
                                  row.Grade == baseItem.Grade).ToList();
                var currentRow =
                    baseItemCostRows.FirstOrDefault(row => row.Level == baseItem.level) ??
                    new EnhancementCostSheetV3.Row();
                var targetRow = baseItemCostRows
                    .OrderByDescending(r => r.Exp)
                    .First(row => row.Exp <= targetExp);

                requiredBlockIndex = targetRow.RequiredBlockIndex - currentRow.RequiredBlockIndex;
            }
            catch
            {
                requiredBlockIndex = 0;
            }

            var avatarAddress = States.Instance.CurrentAvatarState.address;
            slots.SetCaching(avatarAddress, slotIndex, true, requiredBlockIndex,
                itemUsable: baseItem);

            NotificationSystem.Push(
                MailType.Workshop,
                L10nManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_START"),
                NotificationCell.NotificationType.Information);

            Game.Game.instance.ActionManager
                .ItemEnhancement(baseItem, materialItems, slotIndex, hammers, _costNcg).Subscribe();

            enhancementInventory.DeselectBaseItem();

            StartCoroutine(CoCombineNPCAnimation(baseItem, requiredBlockIndex, Clear));
        }

        private void Clear()
        {
            ClearInformation();
            enhancementInventory.DeselectBaseItem();
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

        private static long UpgradeStat(long baseStat, int upgradeStat)
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
                enhancementSelectedMaterialItemScroll.UpdateData(materialModels, _materialsScroller?.Position != 0);
                closeButton.interactable = true;
                ClearInformation();

                enhancementExpSlider.SetEquipment(null, true);

                removeAllButton.Interactable = false;
            }
            else
            {
                // Update Base Slot
                if (!baseSlot.IsExist)
                {
                    animator.Play(HashToPostRegisterBase);
                }

                baseSlot.AddMaterial(baseModel.ItemBase);

                // Update Material Scroll
                enhancementSelectedMaterialItemScroll.UpdateData(materialModels);
                if (materialModels.Count != 0)
                {
                    if (materialModels.Count > 5 || _materialsScroller?.Position != 0)
                    {
                        enhancementSelectedMaterialItemScroll.JumpTo(materialModels[^1], 0);
                    }

                    animator.Play(HashToPostRegisterMaterial);
                    noneContainer.SetActive(false);
                }
                else
                {
                    if (_materialsScroller?.Position != 0)
                    {
                        enhancementSelectedMaterialItemScroll.RawJumpTo(0, 0);
                    }

                    noneContainer.SetActive(true);
                }

                var equipmentItemSheet = Game.Game.instance.TableSheets.EquipmentItemSheet;
                var enhancementCostSheet = Game.Game.instance.TableSheets.EnhancementCostSheetV3;
                var equipment = baseModel.ItemBase as Equipment;
                var baseItemCostRows = enhancementCostSheet.Values
                    .Where(row => row.ItemSubType == equipment.ItemSubType &&
                                  row.Grade == equipment.Grade).ToList();
                var baseItemCostRow =
                    baseItemCostRows.FirstOrDefault(row => row.Level == equipment.level) ??
                    new EnhancementCostSheetV3.Row();

                // Get Target Exp
                var baseModelExp = equipment.GetRealExp(
                    equipmentItemSheet,
                    enhancementCostSheet);
                var targetExp = baseModelExp + materialModels.Sum(inventoryItem =>
                {
                    if (ItemEnhancement.HammerIds.Contains(inventoryItem.ItemBase.Id))
                    {
                        var hammerExp = Equipment.GetHammerExp(
                            inventoryItem.ItemBase.Id,
                            enhancementCostSheet);
                        return hammerExp * inventoryItem.SelectedMaterialCount.Value;
                    }

                    return (inventoryItem.ItemBase as Equipment).GetRealExp(
                        equipmentItemSheet,
                        enhancementCostSheet);
                });

                // Get Target Level
                EnhancementCostSheetV3.Row targetRow;
                try
                {
                    targetRow = baseItemCostRows
                        .OrderByDescending(r => r.Exp)
                        .First(row => row.Exp <= targetExp);
                }
                catch
                {
                    targetRow = baseItemCostRow;
                }

                // Update UI
                removeAllButton.Interactable = materialModels.Count >= 2;

                _costNcg = targetRow.Cost - baseItemCostRow.Cost;
                upgradeButton.SetCost(CostType.NCG, (long)_costNcg);
                var slots = Find<CombinationSlotsPopup>();
                upgradeButton.Interactable = slots.TryGetEmptyCombinationSlot(out _);

                itemInformationContainer.SetActive(true);
                ClearInformation();

                itemNameText.text = equipment.GetLocalizedNonColoredName();
                currentLevelText.text = $"+{equipment.level}";
                nextLevelText.text = $"+{targetRow.Level}";

                levelStateText.text = $"Lv. {targetRow.Level}/{ItemEnhancement.GetEquipmentMaxLevel(equipment, enhancementCostSheet)}";

                currentEquipmentCP.text = CPHelper.GetCP(equipment).ToString();

                long requiredBlockIndex =
                    targetRow.RequiredBlockIndex - baseItemCostRow.RequiredBlockIndex;
                requiredBlockTimeViewer.SetTimeBlock(
                    $"{requiredBlockIndex:#,0}",
                    requiredBlockIndex.BlockRangeToTimeSpanString());

                // Get Target Range Rows
                var targetRangeRows = baseItemCostRows
                    .Where(row => row.Level >= equipment.level &&
                                  row.Level <= targetRow.Level + 1).ToList();
                if (equipment.level == 0)
                {
                    targetRangeRows.Insert(0, new EnhancementCostSheetV3.Row());
                }

                if (targetRangeRows.Count >= 2)
                {
                    enhancementExpSlider.SetEquipment(equipment);
                    enhancementExpSlider.SliderGageEffect(targetExp, targetRow.Level);
                }
                else
                {
                    NcDebug.LogError($"[Enhancement] Failed Get TargetRangeRows : {equipment.level} -> {targetRow.Level}");
                }

                // Get ItemOptionInfo
                var itemOptionInfo = new ItemOptionInfo(equipment);
                var baseStatMin = itemOptionInfo.MainStat.baseValue;
                var baseStatMax = itemOptionInfo.MainStat.baseValue;
                var statOptionsMin = itemOptionInfo.StatOptions.Select(v => v.value).ToList();
                var statOptionsMax = itemOptionInfo.StatOptions.Select(v => v.value).ToList();
                var skillChancesMin = itemOptionInfo.SkillOptions.Select(v => v.chance).ToList();
                var skillChancesMax = itemOptionInfo.SkillOptions.Select(v => v.chance).ToList();
                var skillPowersMin = itemOptionInfo.SkillOptions.Select(v => v.power).ToList();
                var skillPowersMax = itemOptionInfo.SkillOptions.Select(v => v.power).ToList();
                var skillStatPowerRatioMin = itemOptionInfo.SkillOptions.Select(v => v.statPowerRatio).ToList();
                var skillStatPowerRatioMax = itemOptionInfo.SkillOptions.Select(v => v.statPowerRatio).ToList();

                if (equipment.level != targetRow.Level)
                {
                    for (var i = 1; i < targetRangeRows.Count - 1; i++)
                    {
                        var targetRangeRow = targetRangeRows[i];
                        baseStatMin += UpgradeStat(baseStatMin, targetRangeRow.BaseStatGrowthMin);
                        baseStatMax += UpgradeStat(baseStatMax, targetRangeRow.BaseStatGrowthMax);

                        for (var statIndex = 0; statIndex < itemOptionInfo.StatOptions.Count; statIndex++)
                        {
                            statOptionsMin[statIndex] += UpgradeStat(statOptionsMin[statIndex], targetRangeRow.ExtraStatGrowthMin);
                            statOptionsMax[statIndex] += UpgradeStat(statOptionsMax[statIndex], targetRangeRow.ExtraStatGrowthMax);
                        }

                        for (var skillIndex = 0; skillIndex < itemOptionInfo.SkillOptions.Count; skillIndex++)
                        {
                            skillChancesMin[skillIndex] += (int)UpgradeStat(skillChancesMin[skillIndex], targetRangeRow.ExtraSkillChanceGrowthMin);
                            skillChancesMax[skillIndex] += (int)UpgradeStat(skillChancesMax[skillIndex], targetRangeRow.ExtraSkillChanceGrowthMax);

                            skillPowersMin[skillIndex] += UpgradeStat(skillPowersMin[skillIndex], targetRangeRow.ExtraSkillDamageGrowthMin);
                            skillPowersMax[skillIndex] += UpgradeStat(skillPowersMax[skillIndex], targetRangeRow.ExtraSkillDamageGrowthMax);

                            skillStatPowerRatioMin[skillIndex] += (int)UpgradeStat(skillStatPowerRatioMin[skillIndex], targetRangeRow.ExtraSkillDamageGrowthMin);
                            skillStatPowerRatioMax[skillIndex] += (int)UpgradeStat(skillStatPowerRatioMax[skillIndex], targetRangeRow.ExtraSkillDamageGrowthMax);
                        }
                    }
                }

                // Update StatView
                var (mainStatType, baseValue, _) = itemOptionInfo.MainStat;
                mainStatView.gameObject.SetActive(true);
                mainStatView.Set(
                    mainStatType.ToString(),
                    mainStatType.ValueToString(baseValue),
                    $"{mainStatType.ValueToShortString(baseStatMin)} ~ {mainStatType.ValueToShortString(baseStatMax)}");
                mainStatView.SetDescriptionButton(() =>
                {
                    statTooltip.transform.position = mainStatView.DescriptionPosition;
                    statTooltip.Set("", $"{mainStatType.ValueToString(baseStatMin)} ~ {mainStatType.ValueToString(baseStatMax)}<sprite name=icon_Arrow>");
                    statTooltip.gameObject.SetActive(true);
                });

                for (var statIndex = 0; statIndex < itemOptionInfo.StatOptions.Count; statIndex++)
                {
                    var (optionStatType, value, count) = itemOptionInfo.StatOptions[statIndex];
                    var statView = statViews[statIndex];
                    statView.gameObject.SetActive(true);
                    statView.Set(
                        optionStatType.ToString(),
                        optionStatType.ValueToString(value),
                        $"{optionStatType.ValueToShortString(statOptionsMin[statIndex])} ~ {optionStatType.ValueToShortString(statOptionsMax[statIndex])}",
                        count);

                    var tooltipContext = $"{optionStatType.ValueToString(statOptionsMin[statIndex])} ~ {optionStatType.ValueToString(statOptionsMax[statIndex])}<sprite name=icon_Arrow>";
                    statView.SetDescriptionButton(() =>
                    {
                        statTooltip.transform.position = statView.DescriptionPosition;;
                        statTooltip.Set("", tooltipContext);
                        statTooltip.gameObject.SetActive(true);
                    });
                }

                for (var skillIndex = 0; skillIndex < itemOptionInfo.SkillOptions.Count; skillIndex++)
                {
                    var (skillRow, power, chance, statPowerRatio, refStatType) = itemOptionInfo.SkillOptions[skillIndex];
                    var skillView = skillViews[skillIndex];
                    skillView.gameObject.SetActive(true);

                    var currentEffect = SkillExtensions.EffectToString(
                        skillRow.Id,
                        skillRow.SkillType,
                        power,
                        statPowerRatio,
                        refStatType);
                    var targetEffectMin = SkillExtensions.EffectToString(
                        skillRow.Id,
                        skillRow.SkillType,
                        skillPowersMin[skillIndex],
                        skillStatPowerRatioMin[skillIndex],
                        refStatType);
                    var targetEffectMax = SkillExtensions.EffectToString(
                        skillRow.Id,
                        skillRow.SkillType,
                        skillPowersMax[skillIndex],
                        skillStatPowerRatioMax[skillIndex],
                        refStatType);

                    var valueText = $"<color=#FBF0B8>({currentEffect} > <color=#E3C32C>{targetEffectMin.Replace("%", "")}~{targetEffectMax}</color><sprite name=icon_Arrow>)</color>";
                    var chanceText = $"<color=#FBF0B8>({chance}% > <color=#E3C32C>{skillChancesMin[skillIndex]}~{skillChancesMax[skillIndex]}%</color><sprite name=icon_Arrow>)</color>";
                    skillView.Set(skillRow.GetLocalizedName(), skillRow.SkillType, skillRow.Id, skillRow.Cooldown, chanceText, valueText);
                }

                // Update next CP text
                var nextCp = CPHelper.GetStatCP(mainStatType, baseStatMax);
                for (var statIndex = 0; statIndex < itemOptionInfo.StatOptions.Count; statIndex++)
                {
                    nextCp += CPHelper.GetStatCP(itemOptionInfo.StatOptions[statIndex].type, statOptionsMax[statIndex]);
                }

                nextCp *= CPHelper.GetSkillsMultiplier(itemOptionInfo.SkillOptions.Count);
                nextEquipmentCP.text = CPHelper.DecimalToInt(nextCp).ToString();
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
            enhancementInventory.DeselectBaseItem();
        }
    }
}
