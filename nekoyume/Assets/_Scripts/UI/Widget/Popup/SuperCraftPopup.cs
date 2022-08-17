using System;
using System.Collections;
using System.Linq;
using Nekoyume.BlockChain;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.Item;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UniRx;
using UnityEngine;
using ObservableExtensions = UniRx.ObservableExtensions;

namespace Nekoyume.UI
{
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

        private EquipmentItemOptionSheet.Row _skillOptionRow;
        private SubRecipeView.RecipeInfo _recipeInfo;

        public override void Initialize()
        {
            ObservableExtensions.Subscribe(superCraftButton.OnSubmitSubject, _ =>
            {
                if (Find<CombinationSlotsPopup>().TryGetEmptyCombinationSlot(out var slotIndex))
                {
                    ActionManager.Instance.CombinationEquipment(
                        _recipeInfo,
                        slotIndex,
                        false,
                        true);

                    var sheets = TableSheets.Instance;
                    var equipmentRow = sheets
                        .EquipmentItemRecipeSheet[_recipeInfo.RecipeId];
                    var equipment = (Equipment) ItemFactory.CreateItemUsable(
                        equipmentRow.GetResultEquipmentItemRow(),
                        Guid.Empty,
                        default);
                    var blockIndex = equipment.RequiredBlockIndex;
                    if (_recipeInfo.SubRecipeId != null &&
                        sheets.EquipmentItemSubRecipeSheetV2.TryGetValue(
                            _recipeInfo.SubRecipeId.Value, out var row))
                    {
                        blockIndex += row.RequiredBlockIndex;
                    }

                    StartCoroutine(CoCombineNPCAnimation(equipment, blockIndex));
                }
            }).AddTo(gameObject);
        }

        public void Show(
            EquipmentItemOptionSheet.Row row,
            SubRecipeView.RecipeInfo recipeInfo,
            int recipeId,
            bool ignoreAnimation = false)
        {
            _skillOptionRow = row;
            _recipeInfo = recipeInfo;
            skillName.text = L10nManager.Localize($"SKILL_NAME_{row.SkillId}");
            var sheets = TableSheets.Instance;
            var isBuffSkill = row.SkillDamageMax == 0;
            var buffRow = isBuffSkill
                ? sheets.BuffSheet[sheets.SkillBuffSheet[row.SkillId].BuffIds.First()]
                : null;
            skillPowerText.text = isBuffSkill
                ? $"{L10nManager.Localize("UI_SKILL_EFFECT")}: {buffRow.StatModifier}"
                : $"{L10nManager.Localize("UI_SKILL_POWER")}: {row.SkillDamageMax.ToString()}";
            skillChanceText.text =
                $"{L10nManager.Localize("UI_SKILL_CHANCE")}: {row.SkillChanceMin.NormalizeFromTenThousandths() * 100:0%}";
            superCraftButton.SetCost(
                CostType.Crystal,
                sheets.CrystalHammerPointSheet[recipeId].CRYSTAL);
            base.Show(ignoreAnimation);
        }

        private IEnumerator CoCombineNPCAnimation(ItemBase itemBase, long blockIndex)
        {
            var loadingScreen = Find<CombinationLoadingScreen>();
            loadingScreen.Show();
            loadingScreen.SetItemMaterial(new Item(itemBase));
            loadingScreen.SetCloseAction(null);
            loadingScreen.OnDisappear = Pop;
            Push();
            yield return new WaitForSeconds(.5f);

            var format = L10nManager.Localize("UI_COST_BLOCK");
            var quote = string.Format(format, blockIndex);
            loadingScreen.AnimateNPC(itemBase.ItemType, quote);
        }
    }
}
