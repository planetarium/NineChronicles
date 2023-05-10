using System;
using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
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

        [SerializeField] [Space]
        private GameObject skillTextGroupParent;

        [SerializeField]
        private TMP_Text skillName;

        [SerializeField]
        private TMP_Text skillPowerText;

        [SerializeField]
        private TMP_Text skillChanceText;

        [SerializeField] [Space]
        private GameObject noneRecipeTextParent;

        [SerializeField] [Space]
        private ToggleGroup normalRecipeToggleGroup;

        [SerializeField]
        private Toggle basicRecipeToggle;

        [SerializeField]
        private Toggle premiumRecipeToggle;

        [SerializeField] [Space]
        private ToggleGroup legendaryRecipeToggleGroup;

        [SerializeField]
        private Toggle[] legendaryRecipeToggles;

        private EquipmentItemRecipeSheet.Row _recipeRow;
        private int _subRecipeIndex;

        private const int SuperCraftIndex = 20;
        private const int BasicRecipeIndex = 0;
        private const int PremiumRecipeIndex = 1;

        public override void Initialize()
        {
            basicRecipeToggle.onValueChanged.AddListener(b =>
            {
                _subRecipeIndex = b ? BasicRecipeIndex : PremiumRecipeIndex;
                SetSkillInfoText(_recipeRow.SubRecipeIds[_subRecipeIndex]);
            });
            premiumRecipeToggle.onValueChanged.AddListener(b =>
            {
                _subRecipeIndex = b ? PremiumRecipeIndex : BasicRecipeIndex;
                SetSkillInfoText(_recipeRow.SubRecipeIds[_subRecipeIndex]);
            });
            for (int i = 0; i < legendaryRecipeToggles.Length; i++)
            {
                var index = i;
                legendaryRecipeToggles[index].onValueChanged.AddListener(b =>
                {
                    if (!b) return;
                    _subRecipeIndex = index;
                    SetSkillInfoText(_recipeRow.SubRecipeIds[_subRecipeIndex]);
                });
            }

            superCraftButton.OnSubmitSubject.Subscribe(_ =>
            {
                var craftInfo = new Craft.CraftInfo()
                {
                    RecipeID = _recipeRow.Id,
                    SubrecipeId = premiumRecipeToggle.isOn ? PremiumRecipeIndex : BasicRecipeIndex,
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
            superCraftButton.Interactable =
                Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out _) && canSuperCraft;
            var sheets = TableSheets.Instance;
            superCraftButton.SetCost(
                CostType.Crystal,
                sheets.CrystalHammerPointSheet[_recipeRow.Id].CRYSTAL);
            base.Show(ignoreAnimation);

            if (_recipeRow.GetResultEquipmentItemRow().Grade < 5)  // todo : 전설 등급 Grade 값 확인 필요
            {
                normalRecipeToggleGroup.gameObject.SetActive(true);
                legendaryRecipeToggleGroup.gameObject.SetActive(false);
                normalRecipeToggleGroup.SetAllTogglesOff();
            }
            else
            {
                normalRecipeToggleGroup.gameObject.SetActive(false);
                legendaryRecipeToggleGroup.gameObject.SetActive(true);
                legendaryRecipeToggleGroup.SetAllTogglesOff();

                var recipeCount = _recipeRow.SubRecipeIds.Count;
                for (int i = 0; i < legendaryRecipeToggles.Length; i++)
                {
                    legendaryRecipeToggles[i].gameObject.SetActive(i < recipeCount);
                    // todo : 레시피 별 탭 이름 가져와서 설정
                }
            }

            SetSkillInfoText(null);
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
                        ReplacedMaterials = null,
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
            if(subRecipeId is null)
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
            var buffRow = isBuffSkill
                ? sheets.StatBuffSheet[sheets.SkillBuffSheet[skillOptionRow.SkillId].BuffIds.First()]
                : null;

            skillName.text = L10nManager.Localize($"SKILL_NAME_{skillOptionRow.SkillId}");
            skillPowerText.text = isBuffSkill
                ? $"{L10nManager.Localize("UI_SKILL_EFFECT")}: {buffRow.EffectToString(skillOptionRow.SkillDamageMax)}"
                : $"{L10nManager.Localize("UI_SKILL_POWER")}: {skillOptionRow.SkillDamageMax.ToString()}";
            skillChanceText.text =
                $"{L10nManager.Localize("UI_SKILL_CHANCE")}: {skillOptionRow.SkillChanceMin.NormalizeFromTenThousandths() * 100:0%}";
        }

        private IEnumerator CoCombineNpcAnimation(ItemBase itemBase)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SetItemMaterial(new Item(itemBase));
            loadingScreen.SetCloseAction(null);
            loadingScreen.OnDisappear = () => Close();
            yield return new WaitForSeconds(.5f);

            var format = L10nManager.Localize("UI_COST_BLOCK");
            var quote = string.Format(format, SuperCraftIndex);
            loadingScreen.AnimateNPC(itemBase.ItemType, quote);
        }
    }
}
