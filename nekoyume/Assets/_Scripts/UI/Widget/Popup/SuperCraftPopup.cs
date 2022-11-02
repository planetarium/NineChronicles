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
    using UniRx;
    public class SuperCraftPopup : PopupWidget
    {
        [SerializeField]
        private ConditionalCostButton superCraftButton;

        [SerializeField]
        private TMP_Text skillName;

        [SerializeField]
        private TMP_Text skillPowerText;

        [SerializeField]
        private TMP_Text skillChanceText;

        [SerializeField]
        private Toggle basicRecipeToggle;

        [SerializeField]
        private Toggle premiumRecipeToggle;

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
            superCraftButton.OnSubmitSubject.Subscribe(_ =>
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
                        true).Subscribe();
                    var sheets = TableSheets.Instance;
                    var equipmentRow = sheets
                        .EquipmentItemRecipeSheet[_recipeRow.Id];
                    var equipment = (Equipment) ItemFactory.CreateItemUsable(
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
            premiumRecipeToggle.isOn = true;
            SetSkillInfoText(_recipeRow.SubRecipeIds[PremiumRecipeIndex]);
        }

        private void SetSkillInfoText(int subRecipeId)
        {
            var sheets = TableSheets.Instance;
            var subRecipeRow = sheets.EquipmentItemSubRecipeSheetV2[subRecipeId];
            var optionSheet = sheets.EquipmentItemOptionSheet;
            var skillOptionRow = subRecipeRow.Options
                .Select(x => (ratio: x.Ratio, option: optionSheet[x.Id]))
                .FirstOrDefault(tuple => tuple.option.SkillId != 0)
                .option;
            var isBuffSkill = skillOptionRow.SkillDamageMax == 0;
            var buffRow = isBuffSkill
                ? sheets.StatBuffSheet[sheets.SkillBuffSheet[skillOptionRow.SkillId].BuffIds.First()]
                : null;
            skillName.text = L10nManager.Localize($"SKILL_NAME_{skillOptionRow.SkillId}");
            skillPowerText.text = isBuffSkill
                ? $"{L10nManager.Localize("UI_SKILL_EFFECT")}: {buffRow.StatModifier}"
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
