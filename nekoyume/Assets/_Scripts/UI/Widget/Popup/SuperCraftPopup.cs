using System.Linq;
using Nekoyume.Extensions;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

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

        public void Show(
            EquipmentItemOptionSheet.Row row,
            int recipeId,
            bool ignoreAnimation = false)
        {
            _skillOptionRow = row;
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
    }
}
