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
                        SetHealDescription(optionRow);
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

        public void Set(SkillSheet.Row skillRow, int chanceMin, int chanceMax, int damageMin, int damageMax)
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
                        SetAttackSkillDescription(skillRow, chanceMin, chanceMax, damageMin, damageMax);
                        break;
                    case SkillType.Heal:
                        SetHealDescription(skillRow, chanceMin, chanceMax, damageMin, damageMax);
                        break;
                    case SkillType.Buff:
                        SetBuffDescription(skillRow, chanceMin, chanceMax, damageMax, false);
                        break;
                    case SkillType.Debuff:
                        SetBuffDescription(skillRow, chanceMin, chanceMax, damageMax, true);
                        break;
                }
            }

            cooldownText.text = $"{L10nManager.Localize("UI_COOLDOWN")}: {skillRow.Cooldown}";
        }

        private void SetAttackSkillDescription(EquipmentItemOptionSheet.Row optionRow)
        {
            var row = TableSheets.Instance.SkillSheet[optionRow.SkillId];
            SetAttackSkillDescription(
                row,
                optionRow.SkillChanceMin,
                optionRow.SkillChanceMax,
                optionRow.SkillDamageMin,
                optionRow.SkillDamageMax);
        }

        private void SetAttackSkillDescription(SkillSheet.Row row,
            int chanceMin, int chanceMax, int damageMin, int damageMax)
        {
            var chanceText = chanceMin == chanceMax ?
                $"{VariableColorTag}{chanceMin}%</color>" :
                $"{VariableColorTag}{chanceMin}-{chanceMax}%</color>";
            var value = damageMin == damageMax ?
                $"{VariableColorTag}{row.EffectToString(damageMin)}</color>" :
                $"{VariableColorTag}{row.EffectToString(damageMin)}-{row.EffectToString(damageMax)}</color>";
            contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_ATTACK", chanceText, value);

            buffObject.SetActive(false);
            debuffObject.SetActive(false);
        }

        private void SetHealDescription(EquipmentItemOptionSheet.Row optionRow)
        {
            var row = TableSheets.Instance.SkillSheet[optionRow.SkillId];
            SetHealDescription(
                row,
                optionRow.SkillChanceMin,
                optionRow.SkillChanceMax,
                optionRow.SkillDamageMin,
                optionRow.SkillDamageMax);
        }

        private void SetHealDescription(SkillSheet.Row row,
            int chanceMin, int chanceMax, int amountMin, int amountMax)
        {
            var chanceText = chanceMin == chanceMax ?
                $"{VariableColorTag}{chanceMin}%</color>" :
                $"{VariableColorTag}{chanceMin}-{chanceMax}%</color>";
            var value = amountMin == amountMax ?
                $"{VariableColorTag}{row.EffectToString(amountMin)}</color>" :
                $"{VariableColorTag}{row.EffectToString(amountMin)}-{row.EffectToString(amountMax)}</color>";
            contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_HEAL", chanceText, value);

            buffObject.SetActive(false);
            debuffObject.SetActive(false);
        }

        private void SetBuffDescription(EquipmentItemOptionSheet.Row optionRow, bool isDebuff)
        {
            var skillRow = TableSheets.Instance.SkillSheet[optionRow.SkillId];
            SetBuffDescription(
                skillRow,
                optionRow.SkillChanceMin,
                optionRow.SkillChanceMax,
                optionRow.SkillDamageMax,
                isDebuff);
        }

        private void SetBuffDescription(SkillSheet.Row skillRow, int chanceMin, int chanceMax, int damageMax, bool isDebuff)
        {
            var sheets = TableSheets.Instance;
            var buffRow = sheets.StatBuffSheet[sheets.SkillBuffSheet[skillRow.Id].BuffIds.First()];
            var chanceText = chanceMin == chanceMax ?
                $"{VariableColorTag}{chanceMin}%</color>" :
                $"{VariableColorTag}{chanceMin}-{chanceMax}%</color>";
            var statType = $"{VariableColorTag}{buffRow.StatType}</color>";
            var value = $"{VariableColorTag}{skillRow.EffectToString(damageMax)}</color>";

            var icon = BuffHelper.GetStatBuffIcon(buffRow.StatType, isDebuff);
            if (isDebuff)
            {
                debuffStatTypeText.text = buffRow.StatType.GetAcronym();
                debuffIconImage.overrideSprite = icon;
                contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_STATDEBUFF", chanceText, statType, value);
            }
            else
            {
                buffStatTypeText.text = buffRow.StatType.GetAcronym();
                buffIconImage.overrideSprite = icon;
                contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_STATBUFF", chanceText, statType, value);
            }

            buffObject.SetActive(!isDebuff);
            debuffObject.SetActive(isDebuff);
        }
    }
}
