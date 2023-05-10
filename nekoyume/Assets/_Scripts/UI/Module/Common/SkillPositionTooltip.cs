using TMPro;
using UnityEngine;
using Nekoyume.Game;
using Nekoyume.L10n;
using Nekoyume.Model.Skill;
using Nekoyume.TableData;
using System.Linq;
using UniRx;
using UnityEngine.UI;
using Nekoyume.Helper;
using Nekoyume.Model.Stat;

namespace Nekoyume.UI.Module.Common
{
    public class SkillPositionTooltip : PositionTooltip
    {
        [SerializeField]
        protected TextMeshProUGUI cooldownText;

        [SerializeField]
        protected GameObject buffObject;
        
        [SerializeField]
        protected GameObject debuffObject;

        [SerializeField]
        protected Image buffIconImage;

        [SerializeField]
        protected TextMeshProUGUI buffStatTypeText;

        [SerializeField]
        protected Image debuffIconImage;

        [SerializeField]
        protected TextMeshProUGUI debuffStatTypeText;

        private const string VariableColorTag = "<color=#f5e3c0>";

        public void Set(SkillSheet.Row skillRow, EquipmentItemOptionSheet.Row optionRow)
        {
            titleText.text = skillRow.GetLocalizedName();

            var key = $"SKILL_DESCRIPTION_{skillRow.Id}";
            if (L10nManager.ContainsKey(key))
            {
                contentText.text = L10nManager.Localize(key);
            }
            else
            {
                switch (skillRow.SkillType)
                {
                    case SkillType.Attack:
                        SetAttackSkillDescription(optionRow);
                        break;
                    case SkillType.Heal:
                        SetHealSkillDescription(optionRow);
                        break;
                    case SkillType.Buff:
                        SetBuffDescription(optionRow, false);
                        break;
                    case SkillType.Debuff:
                        SetBuffDescription(optionRow, true);
                        break;
                }
            }

            cooldownText.text = $"{L10nManager.Localize("UI_COOLDOWN")}: {skillRow.Cooldown}";
        }

        private void SetAttackSkillDescription(EquipmentItemOptionSheet.Row optionRow)
        {
            var chanceText = optionRow.SkillChanceMin == optionRow.SkillChanceMax ?
                $"{VariableColorTag}{optionRow.SkillChanceMin}%</color>" :
                $"{VariableColorTag}{optionRow.SkillChanceMin}-{optionRow.SkillChanceMax}%</color>";
            var value = optionRow.SkillDamageMin == optionRow.SkillDamageMax ?
                $"{VariableColorTag}{optionRow.SkillDamageMin}%</color>" :
                $"{VariableColorTag}{optionRow.SkillDamageMin}-{optionRow.SkillDamageMax}</color>";
            contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_ATTACK", chanceText, value);

            buffObject.SetActive(false);
            debuffObject.SetActive(false);
        }

        private void SetHealSkillDescription(EquipmentItemOptionSheet.Row optionRow)
        {
            var chanceText = optionRow.SkillChanceMin == optionRow.SkillChanceMax ?
                $"{VariableColorTag}{optionRow.SkillChanceMin}%</color>" :
                $"{VariableColorTag}{optionRow.SkillChanceMin}-{optionRow.SkillChanceMax}%</color>";
            var value = optionRow.SkillDamageMin == optionRow.SkillDamageMax ?
                $"{VariableColorTag}{optionRow.SkillDamageMin}%</color>" :
                $"{VariableColorTag}{optionRow.SkillDamageMin}-{optionRow.SkillDamageMax}</color>";
            contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_HEAL", chanceText, value);

            buffObject.SetActive(false);
            debuffObject.SetActive(false);
        }

        private void SetBuffDescription(EquipmentItemOptionSheet.Row optionRow, bool isDebuff)
        {
            var sheets = TableSheets.Instance;
            var buffRow = sheets.StatBuffSheet[sheets.SkillBuffSheet[optionRow.SkillId].BuffIds.First()];
            var chanceText = optionRow.SkillChanceMin == optionRow.SkillChanceMax ?
                $"{VariableColorTag}{optionRow.SkillChanceMin}%</color>" :
                $"{VariableColorTag}{optionRow.SkillChanceMin}-{optionRow.SkillChanceMax}%</color>";
            var statType = $"{VariableColorTag}{buffRow.StatType}</color>";
            var value = $"{VariableColorTag}{buffRow.EffectToString(optionRow.SkillDamageMax)}</color>";

            contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_STATBUFF", chanceText, statType, value);

            var icon = BuffHelper.GetStatBuffIcon(buffRow.StatType, isDebuff);
            if (isDebuff)
            {
                debuffStatTypeText.text = buffRow.StatType.GetAcronym();
                debuffIconImage.overrideSprite = icon;
            }
            else
            {
                buffStatTypeText.text = buffRow.StatType.GetAcronym();
                buffIconImage.overrideSprite = icon;
            }

            buffObject.SetActive(!isDebuff);
            debuffObject.SetActive(isDebuff);
        }
    }
}
