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
        private TextMeshProUGUI sliderPercentText;

        [SerializeField]
        private TextMeshProUGUI levelStateText;

        [SerializeField]
        private EnhancementSelectedMaterialItemScroll enhancementSelectedMaterialItemScroll;

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

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(Close);
            CloseWidget = Close;
        }

        public override void Initialize()
        {
            base.Initialize();

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
            base.Show(ignoreShowAnimation);
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

        private void EnhancementAction(Equipment baseItem, List<Equipment> materialItems)
        {
            var slots = Find<CombinationSlotsPopup>();
            if (!slots.TryGetEmptyCombinationSlot(out var slotIndex))
            {
                return;
            }

            var sheet = Game.Game.instance.TableSheets.EnhancementCostSheetV3;

            var targetExp = baseItem.Exp + materialItems.Aggregate(0L, (total, m) => total + m.Exp);
            EnhancementCostSheetV3.Row targetRow;
            int requiredBlockIndex = 0;
            try
            {
                requiredBlockIndex = sheet.OrderedList
                    .Where(e =>
                        e.ItemSubType == baseItem.ItemSubType &&
                        e.Grade == baseItem.Grade &&
                        e.Exp > baseItem.Exp &&
                        e.Exp <= targetExp)
                .Aggregate(0, (blocks, row) => blocks + row.RequiredBlockIndex);
            }
            catch
            {
                requiredBlockIndex = 0;
            }

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

            if (States.Instance.CurrentAvatarState.actionPoint < GameConfig.EnhanceEquipmentCostAP)
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

        private IEnumerator CoCombineNPCAnimation(ItemBase itemBase,
            long blockIndex,
            System.Action action,
            bool isConsumable = false)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SetItemMaterial(new Item(itemBase), isConsumable);
            loadingScreen.SetCloseAction(action);
            Push();
            yield return new WaitForSeconds(.5f);

            var format = L10nManager.Localize("UI_COST_BLOCK");
            var quote = string.Format(format, blockIndex);
            loadingScreen.AnimateNPC(itemBase.ItemType, quote);
        }

        private void ClearInformation()
        {
            itemNameText.text = string.Empty;
            currentLevelText.text = string.Empty;
            nextLevelText.text = string.Empty;

            expSlider.value = 0;
            sliderPercentText.text = "0%";

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
            return Nekoyume.Battle.CPHelper.DecimalToInt(Math.Max(1.0m, baseStat * upgradeStat.NormalizeFromTenThousandths()));
        }

        private void UpdateInformation(EnhancementInventoryItem baseModel,
            List<EnhancementInventoryItem> materialModels)
        {
            _costNcg = 0;
            if (baseModel is null)
            {
                baseSlot.RemoveMaterial();
                //materialSlot.RemoveMaterial();
                noneContainer.SetActive(true);
                itemInformationContainer.SetActive(false);
                animator.Play(HashToRegisterBase);
                enhancementSelectedMaterialItemScroll.UpdateData(materialModels, true);
                closeButton.interactable = true;
                ClearInformation();
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
                    enhancementSelectedMaterialItemScroll.JumpTo(materialModels[materialModels.Count - 1]);
                    animator.Play(HashToPostRegisterMaterial);
                    noneContainer.SetActive(false);
                }
                else
                {
                    noneContainer.SetActive(true);
                }

                var equipment = baseModel.ItemBase as Equipment;
                if (!ItemEnhancement.TryGetRow(equipment, _costSheet, out var baseItemCostRow))
                {
                    baseItemCostRow = new EnhancementCostSheetV3.Row();
                }

                var targetExp = (baseModel.ItemBase as Equipment).Exp + materialModels.Aggregate(0L, (total, m) => total + (m.ItemBase as Equipment).Exp);

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
                    Debug.LogError("[Enhancement] Faild Get TargetRangeRows");
                }
                else
                {
                    var nextExp = targetRangeRows[targetRangeRows.Count - 1].Exp;
                    var prevExp = targetRangeRows[targetRangeRows.Count - 2].Exp;
                    var lerp = Mathf.InverseLerp(prevExp, nextExp, targetExp);
                    expSlider.value = lerp;
                    sliderPercentText.text = $"{(int)(lerp * 100)}%";
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
                    for (int i = 1; i < targetRangeRows.Count; i++)
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


                //StatView
                mainStatView.gameObject.SetActive(true);
                mainStatView.Set(itemOptionInfo.MainStat.type.ToString(),
                    itemOptionInfo.MainStat.type.ValueToString(itemOptionInfo.MainStat.baseValue),
                    $"{baseStatMin.ToCurrencyNotation()} ~ {baseStatMax.ToCurrencyNotation()}");
                mainStatView.SetDescriptionButton(() =>
                {
                    statTooltip.transform.position = mainStatView.DescriptionPosition;
                    statTooltip.Set("", $"{baseStatMin} ~ {baseStatMax}<sprite name=icon_Arrow>");
                    statTooltip.gameObject.SetActive(true);
                });

                for (int statIndex = 0; statIndex < itemOptionInfo.StatOptions.Count; statIndex++)
                {
                    statViews[statIndex].gameObject.SetActive(true);
                    statViews[statIndex].Set(itemOptionInfo.StatOptions[statIndex].type.ToString(),
                            itemOptionInfo.StatOptions[statIndex].type.ValueToString(itemOptionInfo.StatOptions[statIndex].value),
                            $"{statOptionsMin[statIndex].ToCurrencyNotation()} ~ {statOptionsMax[statIndex].ToCurrencyNotation()}",
                            itemOptionInfo.StatOptions[statIndex].count);
                    var tooltipContext = $"{statOptionsMin[statIndex]} ~ {statOptionsMax[statIndex]}<sprite name=icon_Arrow>";
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
                    switch (skillRow.SkillType)
                    {
                        case Nekoyume.Model.Skill.SkillType.Attack:
                        case Nekoyume.Model.Skill.SkillType.Heal:
                            valueText = $"<color=#FBF0B8>({itemOptionInfo.SkillOptions[skillIndex].power} > <color=#E3C32C>{skillPowersMin[skillIndex]}~{skillPowersMax[skillIndex]}</color><sprite name=icon_Arrow>)</color>";
                            break;
                        case Nekoyume.Model.Skill.SkillType.Buff:
                        case Nekoyume.Model.Skill.SkillType.Debuff:
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
                            break;
                        default:
                            break;
                    }
                    skillViews[skillIndex].Set(skillRow.GetLocalizedName(), skillRow.SkillType, skillRow.Id, skillRow.Cooldown, chanceText, valueText);
                }
            }
        }
    }
}
