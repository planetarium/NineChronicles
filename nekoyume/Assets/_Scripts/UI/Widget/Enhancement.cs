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

        private static readonly int HashToShow = Animator.StringToHash("Show");
        private static readonly int HashToRegisterBase = Animator.StringToHash("RegisterBase");

        private static readonly int HashToPostRegisterBase =
            Animator.StringToHash("PostRegisterBase");

        private static readonly int HashToPostRegisterMaterial =
            Animator.StringToHash("PostRegisterMaterial");

        private static readonly int HashToUnregisterMaterial =
            Animator.StringToHash("UnregisterMaterial");

        private static readonly int HashToClose = Animator.StringToHash("Close");


        private EnhancementCostSheetV2 _costSheet;
        private BigInteger _costNcg = 0;
        private string errorMessage;

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
                .Subscribe(_ => Action())
                .AddTo(gameObject);

            _costSheet = Game.Game.instance.TableSheets.EnhancementCostSheetV2;

            baseSlot.RemoveButton.onClick.AddListener(() => enhancementInventory.DeselectItem(true));
            materialSlot.RemoveButton.onClick.AddListener(() => enhancementInventory.DeselectItem());
        }

        public override void Show(bool ignoreShowAnimation = false)
        {
            Clear();
            HelpTooltip.HelpMe(100017, true);
            enhancementInventory.Set(ShowItemTooltip, UpdateInformation);
            animator.Play(HashToShow);
            base.Show(ignoreShowAnimation);
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
                    NotificationCell.NotificationType.Alert),
                target);
        }

        private void Action()
        {
            var (baseItem, materialItem) = enhancementInventory.GetSelectedModels();
            if (!IsInteractableButton(baseItem, materialItem))
            {
                NotificationSystem.Push(MailType.System, errorMessage,
                    NotificationCell.NotificationType.Alert);
                return;
            }

            if (States.Instance.GoldBalanceState.Gold.MajorUnit < _costNcg)
            {
                errorMessage = L10nManager.Localize("UI_NOT_ENOUGH_NCG");
                NotificationSystem.Push(MailType.System, errorMessage,
                    NotificationCell.NotificationType.Alert);
                return;
            }

            var slots = Find<CombinationSlotsPopup>();
            if (!slots.TryGetEmptyCombinationSlot(out var slotIndex))
            {
                return;
            }

            var sheet = Game.Game.instance.TableSheets.EnhancementCostSheetV2;
            if (ItemEnhancement.TryGetRow(baseItem, sheet, out var row))
            {
                slots.SetCaching(slotIndex, true, row.SuccessRequiredBlockIndex,
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
                errorMessage = L10nManager.Localize("UI_SELECT_MATERIAL_TO_UPGRADE");
                return false;
            }

            if (States.Instance.CurrentAvatarState.actionPoint < GameConfig.EnhanceEquipmentCostAP)
            {
                errorMessage = L10nManager.Localize("NOTIFICATION_NOT_ENOUGH_ACTION_POWER");
                return false;
            }

            if (!Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out _))
            {
                errorMessage = L10nManager.Localize("NOTIFICATION_NOT_ENOUGH_SLOTS");
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
            EnhancementInventoryItem materialModel)
        {
            if (baseModel is null)
            {
                baseSlot.RemoveMaterial();
                materialSlot.RemoveMaterial();
                noneContainer.SetActive(true);
                itemInformationContainer.SetActive(false);
                animator.Play(HashToRegisterBase);
                closeButton.interactable = true;
            }
            else
            {
                if (!baseSlot.IsExist)
                {
                    animator.Play(HashToPostRegisterBase);
                }

                baseSlot.AddMaterial(baseModel.ItemBase);

                if (materialModel is null)
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
                upgradeButton.SetCost(CostType.NCG, (int)row.Cost);
                var slots = Find<CombinationSlotsPopup>();
                upgradeButton.Interactable = slots.TryGetEmptyCombinationSlot(out var _);

                itemNameText.text = equipment.GetLocalizedName();
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
                    var mainAdd = Math.Max(1,
                        (int)(mainValue * row.BaseStatGrowthMax.NormalizeFromTenThousandths()));
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
                    var skillName = skills[i].skillRow.GetLocalizedName();
                    var power = skills[i].power;
                    var chance = skills[i].chance;

                    if (row.ExtraSkillDamageGrowthMin == 0 && row.ExtraSkillDamageGrowthMax == 0 &&
                        row.ExtraSkillChanceGrowthMin == 0 && row.ExtraSkillChanceGrowthMax == 0)
                    {
                        skillViews[i].Set(skillName,
                            $"{L10nManager.Localize("UI_SKILL_POWER")} : {power}",
                            string.Empty,
                            $"{L10nManager.Localize("UI_SKILL_CHANCE")} : {chance}",
                            string.Empty);
                    }
                    else
                    {
                        var powerAdd = Math.Max(1,
                            (int)(power *
                                  row.ExtraSkillDamageGrowthMax.NormalizeFromTenThousandths()));
                        var chanceAdd = Math.Max(1,
                            (int)(chance *
                                  row.ExtraSkillChanceGrowthMax.NormalizeFromTenThousandths()));

                        skillViews[i].Set(skillName,
                            $"{L10nManager.Localize("UI_SKILL_POWER")} : {power}",
                            $"(<size=80%>max</size> +{powerAdd})",
                            $"{L10nManager.Localize("UI_SKILL_CHANCE")} : {chance}",
                            $"(<size=80%>max</size> +{chanceAdd}%)");
                    }
                }
            }
        }
    }
}
