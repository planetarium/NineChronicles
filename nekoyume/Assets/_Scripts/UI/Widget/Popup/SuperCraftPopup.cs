using System;
using System.Collections;
using System.Linq;
using Nekoyume.Blockchain;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI
{
    using Nekoyume.Model.Skill;
    using UniRx;
    using UnityEngine.UI;

    public class SuperCraftPopup : PopupWidget
    {
        [SerializeField]
        private ConditionalCostButton superCraftButton;

        [SerializeField][Space]
        private GameObject skillTextGroupParent;

        [SerializeField]
        private TMP_Text skillName;

        [SerializeField]
        private TMP_Text skillPowerText;

        [SerializeField]
        private TMP_Text skillChanceText;

        [SerializeField][Space]
        private GameObject noneRecipeTextParent;

        [SerializeField][Space]
        private ToggleGroup normalRecipeTabGroup;

        [SerializeField]
        private Toggle basicRecipeTab;

        [SerializeField]
        private Toggle premiumRecipeTab;

        [SerializeField][Space]
        private SubRecipeView.RecipeTabGroup legendaryRecipeTabGroup;

        private EquipmentItemRecipeSheet.Row _recipeRow;
        private int _subRecipeIndex;
        private bool _canSuperCraft;

        private const int SuperCraftIndex = 20;
        private const int BasicRecipeIndex = 0;
        private const int PremiumRecipeIndex = 1;

        public override void Initialize()
        {
            basicRecipeTab.onValueChanged.AddListener(b =>
            {
                _subRecipeIndex = b ? BasicRecipeIndex : PremiumRecipeIndex;
                SetSkillInfoText(_recipeRow.SubRecipeIds[_subRecipeIndex]);
            });
            premiumRecipeTab.onValueChanged.AddListener(b =>
            {
                _subRecipeIndex = b ? PremiumRecipeIndex : BasicRecipeIndex;
                SetSkillInfoText(_recipeRow.SubRecipeIds[_subRecipeIndex]);
            });
            for (var i = 0; i < legendaryRecipeTabGroup.recipeTabs.Count; i++)
            {
                var index = i;
                legendaryRecipeTabGroup.recipeTabs[index].toggle.onValueChanged.AddListener(b =>
                {
                    if (!b)
                    {
                        return;
                    }

                    _subRecipeIndex = index;
                    SetSkillInfoText(_recipeRow.SubRecipeIds[_subRecipeIndex]);
                    superCraftButton.Interactable =
                        superCraftButton.Interactable ||
                        (Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out _) && _canSuperCraft);
                });
            }

            superCraftButton.OnSubmitSubject.Subscribe(_ =>
            {
                var craftInfo = new Craft.CraftInfo()
                {
                    RecipeID = _recipeRow.Id,
                    SubrecipeId = _recipeRow.SubRecipeIds[_subRecipeIndex]
                };

                Find<PetSelectionPopup>().Show(craftInfo, SendAction);
            }).AddTo(gameObject);
        }

        public void Show(
            EquipmentItemRecipeSheet.Row recipeRow,
            bool canSuperCraft,
            bool ignoreAnimation = false)
        {
            _recipeRow = recipeRow;
            _canSuperCraft = canSuperCraft;
            superCraftButton.SetCost(
                CostType.Crystal,
                TableSheets.Instance.CrystalHammerPointSheet[_recipeRow.Id].CRYSTAL);
            base.Show(ignoreAnimation);

            if (_recipeRow.GetResultEquipmentItemRow().Grade < 5)
            {
                normalRecipeTabGroup.gameObject.SetActive(true);
                legendaryRecipeTabGroup.toggleGroup.gameObject.SetActive(false);
                superCraftButton.Interactable =
                    Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out _) &&
                    _canSuperCraft;

                premiumRecipeTab.isOn = true;
                SetSkillInfoText(_recipeRow.SubRecipeIds[PremiumRecipeIndex]);
            }
            else
            {
                normalRecipeTabGroup.gameObject.SetActive(false);
                legendaryRecipeTabGroup.toggleGroup.gameObject.SetActive(true);
                legendaryRecipeTabGroup.toggleGroup.SetAllTogglesOff();
                superCraftButton.Interactable = false;

                var tabNames = SubRecipeView.DefaultTabNames;
                var tab = Craft.SubRecipeTabs.FirstOrDefault(tab => tab.RecipeId == _recipeRow.Key);
                if (tab != null)
                {
                    tabNames = tab.TabNames;
                }

                for (var i = 0; i < legendaryRecipeTabGroup.recipeTabs.Count; i++)
                {
                    var recipeTab = legendaryRecipeTabGroup.recipeTabs[i];

                    recipeTab.toggle.gameObject.SetActive(i < tabNames.Length);
                    if (i < tabNames.Length)
                    {
                        recipeTab.disableText.text = tabNames[i];
                        recipeTab.enableText.text = tabNames[i];
                    }
                }

                SetSkillInfoText(null);
            }
        }

        private void SendAction(int? petId)
        {
            if (Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out var slotIndex))
            {
                ActionManager.Instance.CombinationEquipment(
                    new SubRecipeView.RecipeInfo
                    {
                        RecipeId = _recipeRow.Id,
                        SubRecipeId = _recipeRow.SubRecipeIds[_subRecipeIndex],
                        CostNCG = default,
                        CostCrystal = default,
                        CostAP = 0,
                        Materials = default,
                        ReplacedMaterials = null
                    },
                    slotIndex,
                    false,
                    true,
                    petId).Subscribe();
                var sheets = TableSheets.Instance;
                var equipmentRow = sheets
                    .EquipmentItemRecipeSheet[_recipeRow.Id];
                var equipment = (Equipment)ItemFactory.CreateItemUsable(
                    equipmentRow.GetResultEquipmentItemRow(),
                    Guid.Empty,
                    SuperCraftIndex);
                Find<CombinationSlotsPopup>().SetCaching(
                    States.Instance.CurrentAvatarState.address,
                    slotIndex,
                    true,
                    SuperCraftIndex,
                    itemUsable: equipment);
                StartCoroutine(CoCombineNpcAnimation(equipment));
            }
        }

        private void SetSkillInfoText(int? subRecipeId)
        {
            if (subRecipeId is null)
            {
                skillTextGroupParent.SetActive(false);
                noneRecipeTextParent.SetActive(true);
                return;
            }

            skillTextGroupParent.SetActive(true);
            noneRecipeTextParent.SetActive(false);

            var sheets = TableSheets.Instance;
            var subRecipeRow = sheets.EquipmentItemSubRecipeSheetV2[subRecipeId.Value];
            var optionSheet = sheets.EquipmentItemOptionSheet;
            var skillOptionRow = subRecipeRow.Options
                .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                .FirstOrDefault(tuple => tuple.option.SkillId != 0)
                .option;
            var skillRow = sheets.SkillSheet[skillOptionRow.SkillId];
            var isBuffSkill = skillRow.SkillType is SkillType.Buff or SkillType.Debuff;
            skillName.text = L10nManager.Localize($"SKILL_NAME_{skillOptionRow.SkillId}");

            var effectString = SkillExtensions.EffectToString(
                skillOptionRow.SkillId,
                skillRow.SkillType,
                skillOptionRow.SkillDamageMax,
                skillOptionRow.StatDamageRatioMax,
                skillOptionRow.ReferencedStatType);
            skillPowerText.text = isBuffSkill
                ? $"{L10nManager.Localize("UI_SKILL_EFFECT")}: {effectString}"
                : $"{L10nManager.Localize("UI_SKILL_POWER")}: {effectString}";
            skillChanceText.text =
                $"{L10nManager.Localize("UI_SKILL_CHANCE")}: {skillOptionRow.SkillChanceMin.NormalizeFromTenThousandths() * 100:0%}";
        }

        private IEnumerator CoCombineNpcAnimation(ItemBase itemBase)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SpeechBubbleWithItem.SetItemMaterial(new Item(itemBase));
            loadingScreen.SetCloseAction(null);
            loadingScreen.OnDisappear = () => Close();
            yield return new WaitForSeconds(.5f);

            var format = L10nManager.Localize("UI_COST_BLOCK");
            var quote = string.Format(format, SuperCraftIndex);
            loadingScreen.AnimateNPC(CombinationLoadingScreen.SpeechBubbleItemType.Equipment, quote);
        }

        // Invoke from TutorialController.PlayAction() by TutorialTargetType
        public void TutorialActionClickCombineEquipmentSuperCraftPopupClose()
        {
            Close();
        }
    }
}
