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
    using UniRx;

    public class Enhancement : Widget
    {
        [SerializeField]
        private EnhancementInventory enhancementInventory;

        [SerializeField]
        private ConditionalCostButton upgradeButton;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private UpgradeEquipmentSlot baseSlot;

        [SerializeField]
        private UpgradeEquipmentSlot materialSlot;

        [SerializeField]
        private TextMeshProUGUI successRatioText;

        [SerializeField]
        private TextMeshProUGUI requiredBlockIndexText;

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
        private List<EnhancementOptionView> skillViews;

        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private GameObject noneContainer;

        [SerializeField]
        private GameObject itemInformationContainer;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private SkillPositionTooltip skillTooltip;

        [SerializeField]
        private EnhancementSelectedMaterialItemScroll enhancementSelectedMaterialItemScroll;

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


        private EnhancementCostSheetV2 _costSheet;
        private BigInteger _costNcg = 0;
        private string _errorMessage;

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

            _costSheet = Game.Game.instance.TableSheets.EnhancementCostSheetV2;

            baseSlot.RemoveButton.onClick.AddListener(() => enhancementInventory.DeselectItem(true));
            materialSlot.RemoveButton.onClick.AddListener(() => enhancementInventory.DeselectItem());
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Clear();
            enhancementInventory.Set(ShowItemTooltip, UpdateInformation);
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

        private void ShowItemTooltip(EnhancementInventoryItem model, RectTransform target)
        {
            var tooltip = ItemTooltip.Find(model.ItemBase.ItemType);
            tooltip.Show(model, enhancementInventory.GetSubmitText(),
                !model.Disabled.Value,
                () => enhancementInventory.SelectItem(),
                () => enhancementInventory.ClearSelectedItem(),
                () => NotificationSystem.Push(MailType.System,
                    L10nManager.Localize("NOTIFICATION_MISMATCH_MATERIAL"),
                    NotificationCell.NotificationType.Alert));
        }

        private void OnSubmit()
        {
            var (baseItem, materialItem) = enhancementInventory.GetSelectedModels();

            //Equip Upgragd ToDO
/*            if (!IsInteractableButton(baseItem, materialItem))
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

            var sheet = Game.Game.instance.TableSheets.EnhancementCostSheetV2;
            EnhancementAction(baseItem, materialItem);*/
        }

        private void EnhancementAction(Equipment baseItem, Equipment materialItem)
        {
            var slots = Find<CombinationSlotsPopup>();
            if (!slots.TryGetEmptyCombinationSlot(out var slotIndex))
            {
                return;
            }

            var sheet = Game.Game.instance.TableSheets.EnhancementCostSheetV2;
            if (ItemEnhancement.TryGetRow(baseItem, sheet, out var row))
            {
                var avatarAddress = States.Instance.CurrentAvatarState.address;
                slots.SetCaching(avatarAddress, slotIndex, true, row.SuccessRequiredBlockIndex,
                    itemUsable: baseItem);
            }

            NotificationSystem.Push(MailType.Workshop,
                L10nManager.Localize("NOTIFICATION_ITEM_ENHANCEMENT_START"),
                NotificationCell.NotificationType.Information);

            Game.Game.instance.ActionManager
                .ItemEnhancement(baseItem, materialItem, slotIndex, _costNcg).Subscribe();

            enhancementInventory.DeselectItem(true);

            StartCoroutine(CoCombineNPCAnimation(baseItem, row.SuccessRequiredBlockIndex, Clear));
        }

        private void Clear()
        {
            ClearInformation();
            enhancementInventory.DeselectItem(true);
        }

        private bool IsInteractableButton(IItem item, IItem material)
        {
            if (item is null || material is null)
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
            successRatioText.text = "0%";
            requiredBlockIndexText.text = "0";

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

        private void UpdateInformation(EnhancementInventoryItem baseModel,
            List<EnhancementInventoryItem> materialModels)
        {
            if (baseModel is null)
            {
                baseSlot.RemoveMaterial();
                materialSlot.RemoveMaterial();
                noneContainer.SetActive(true);
                itemInformationContainer.SetActive(false);
                animator.Play(HashToRegisterBase);
                enhancementSelectedMaterialItemScroll.UpdateData(materialModels, true);
                closeButton.interactable = true;
            }
            else
            {
                if (!baseSlot.IsExist)
                {
                    animator.Play(HashToPostRegisterBase);
                }

                baseSlot.AddMaterial(baseModel.ItemBase);

                //Equip Upgragd ToDO
/*                if (materialModel is null)
                {
                    if (materialSlot.IsExist)
                    {
                        animator.Play(HashToUnregisterMaterial);
                    }

                    materialSlot.RemoveMaterial();
                }
                else
                {
                    if (!materialSlot.IsExist)
                    {
                        animator.Play(HashToPostRegisterMaterial);
                    }

                    materialSlot.AddMaterial(materialModel.ItemBase);
                }*/

                enhancementSelectedMaterialItemScroll.UpdateData(materialModels);
                if(materialModels.Count != 0)
                {
                    enhancementSelectedMaterialItemScroll.JumpTo(materialModels[materialModels.Count - 1]);
                }

                var equipment = baseModel.ItemBase as Equipment;
                if (!ItemEnhancement.TryGetRow(equipment, _costSheet, out var row))
                {
                    return;
                }

                noneContainer.SetActive(false);
                itemInformationContainer.SetActive(true);

                ClearInformation();
                _costNcg = row.Cost;
                upgradeButton.SetCost(CostType.NCG, (long)row.Cost);
                var slots = Find<CombinationSlotsPopup>();
                upgradeButton.Interactable = slots.TryGetEmptyCombinationSlot(out var _);

                itemNameText.text = equipment.GetLocalizedNonColoredName();
                currentLevelText.text = $"+{equipment.level}";
                nextLevelText.text = $"+{equipment.level + 1}";
                successRatioText.text =
                    ((row.GreatSuccessRatio + row.SuccessRatio).NormalizeFromTenThousandths())
                    .ToString("0%");
                requiredBlockIndexText.text = $"{row.SuccessRequiredBlockIndex}";

                var sheet = Game.Game.instance.TableSheets.ItemRequirementSheet;
                if (!sheet.TryGetValue(equipment.Id, out var requirementRow))
                {
                    levelText.enabled = false;
                }
                else
                {
                    levelText.text =
                        L10nManager.Localize("UI_REQUIRED_LEVEL", requirementRow.Level);
                    var hasEnoughLevel =
                        States.Instance.CurrentAvatarState.level >= requirementRow.Level;
                    levelText.color = hasEnoughLevel
                        ? Palette.GetColor(EnumType.ColorType.ButtonEnabled)
                        : Palette.GetColor(EnumType.ColorType.TextDenial);

                    levelText.enabled = true;
                }

                var itemOptionInfo = new ItemOptionInfo(equipment);

                if (row.BaseStatGrowthMin != 0 && row.BaseStatGrowthMax != 0)
                {
                    var (mainStatType, mainValue, _) = itemOptionInfo.MainStat;
                    var mainAdd = (int)Math.Max(1,
                        (mainValue * row.BaseStatGrowthMax.NormalizeFromTenThousandths()));
                    mainStatView.gameObject.SetActive(true);
                    mainStatView.Set(mainStatType.ToString(),
                        mainStatType.ValueToString(mainValue),
                        $"(<size=80%>max</size> +{mainStatType.ValueToString(mainAdd)})");
                }

                var stats = itemOptionInfo.StatOptions;
                for (var i = 0; i < stats.Count; i++)
                {
                    statViews[i].gameObject.SetActive(true);
                    var statType = stats[i].type;
                    var statValue = stats[i].value;
                    var count = stats[i].count;

                    if (row.ExtraStatGrowthMin == 0 && row.ExtraStatGrowthMax == 0)
                    {
                        statViews[i].Set(statType.ToString(),
                            statType.ValueToString(statValue),
                            string.Empty,
                            count);
                    }
                    else
                    {
                        var statAdd = Math.Max(1,
                            (int)(statValue *
                                  row.ExtraStatGrowthMax.NormalizeFromTenThousandths()));
                        statViews[i].Set(statType.ToString(),
                            statType.ValueToString(statValue),
                            $"(<size=80%>max</size> +{statType.ValueToString(statAdd)})",
                            count);
                    }
                }

                var skills = itemOptionInfo.SkillOptions;
                for (var i = 0; i < skills.Count; i++)
                {
                    skillViews[i].gameObject.SetActive(true);
                    var skill = skills[i];
                    var skillName = skill.skillRow.GetLocalizedName();
                    var power = skill.power;
                    var chance = skill.chance;
                    var ratio = skill.statPowerRatio;
                    var refStatType = skill.refStatType;
                    var effectString = SkillExtensions.EffectToString(
                        skill.skillRow.Id,
                        skill.skillRow.SkillType,
                        power,
                        ratio,
                        refStatType);
                    var isBuff =
                        skill.skillRow.SkillType == Nekoyume.Model.Skill.SkillType.Buff ||
                        skill.skillRow.SkillType == Nekoyume.Model.Skill.SkillType.Debuff;

                    if (row.ExtraSkillDamageGrowthMin == 0 && row.ExtraSkillDamageGrowthMax == 0 &&
                        row.ExtraSkillChanceGrowthMin == 0 && row.ExtraSkillChanceGrowthMax == 0)
                    {
                        var view = skillViews[i];
                        view.Set(skillName,
                            $"{L10nManager.Localize("UI_SKILL_POWER")} : {effectString}",
                            string.Empty,
                            $"{L10nManager.Localize("UI_SKILL_CHANCE")} : {chance}",
                            string.Empty);
                        var skillRow = skill.skillRow;
                        view.SetDescriptionButton(() =>
                        {
                            skillTooltip.Show(skillRow, chance, chance, power, power, ratio, ratio, refStatType);
                            skillTooltip.transform.position = view.DescriptionPosition;
                        });
                    }
                    else
                    {
                        var powerAdd = Math.Max(isBuff || power == 0 ? 0 : 1,
                            (int)(power *
                                  row.ExtraSkillDamageGrowthMax.NormalizeFromTenThousandths()));
                        var ratioAdd = Math.Max(0,
                            (int)(ratio *
                                  row.ExtraSkillDamageGrowthMax.NormalizeFromTenThousandths()));
                        var chanceAdd = Math.Max(1,
                            (int)(chance *
                                  row.ExtraSkillChanceGrowthMax.NormalizeFromTenThousandths()));
                        var totalPower = power + powerAdd;
                        var totalChance = chance + chanceAdd;
                        var totalRatio = ratio + ratioAdd;
                        var skillRow = skill.skillRow;

                        var powerString = SkillExtensions.EffectToString(
                            skillRow.Id,
                            skillRow.SkillType,
                            powerAdd,
                            ratioAdd,
                            skill.refStatType);

                        var view = skillViews[i];
                        view.Set(skillName,
                            $"{L10nManager.Localize("UI_SKILL_POWER")} : {effectString}",
                            $"(<size=80%>max</size> +{powerString})",
                            $"{L10nManager.Localize("UI_SKILL_CHANCE")} : {chance}",
                            $"(<size=80%>max</size> +{chanceAdd}%)");
                        view.SetDescriptionButton(() =>
                        {
                            skillTooltip.Show(
                                skillRow, totalChance, totalChance, totalPower, totalPower, totalRatio, totalRatio, refStatType);
                            skillTooltip.transform.position = view.DescriptionPosition;
                        });
                    }
                }
            }
        }
    }
}
