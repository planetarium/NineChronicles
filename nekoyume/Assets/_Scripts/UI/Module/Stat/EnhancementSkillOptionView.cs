using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using Nekoyume.L10n;
using Nekoyume.Model.Skill;
using UnityEngine.UI;
using Nekoyume.Model.Stat;
using Nekoyume.Helper;
using Nekoyume.Game;
using System.Linq;

namespace Nekoyume.UI.Model
{
    public class EnhancementSkillOptionView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI titleText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private TextMeshProUGUI cooldownText;

        [SerializeField]
        private GameObject buffObject;

        [SerializeField]
        private Image buffIconImage;

        [SerializeField]
        protected TextMeshProUGUI buffStatTypeText;

        [SerializeField]
        private GameObject debuffObject;

        [SerializeField]
        private Image debuffIconImage;

        [SerializeField]
        private TextMeshProUGUI debuffStatTypeText;

        private const string VariableColorTag = "<color=#f5e3c0>";

        public void Set(string skillName, SkillType skillType, int skillId, int coolDown, string chanceText, string valueText)
        {
            titleText.text = skillName;
            var sheets = TableSheets.Instance;
            switch (skillType)
            {
                case SkillType.Attack:
                    buffObject.SetActive(false);
                    debuffObject.SetActive(false);
                    contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_ATTACK", chanceText, valueText);
                    break;
                case SkillType.Heal:
                    buffObject.SetActive(false);
                    debuffObject.SetActive(false);
                    contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_HEAL", chanceText, valueText);
                    break;
                case SkillType.Buff:
                    buffObject.SetActive(true);
                    debuffObject.SetActive(false);
                    var buffRow = sheets.StatBuffSheet[sheets.SkillBuffSheet[skillId].BuffIds.First()];
                    buffStatTypeText.text = buffRow.StatType.GetAcronym();
                    buffIconImage.overrideSprite = BuffHelper.GetStatBuffIcon(buffRow.StatType, false);
                    contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_STATBUFF", chanceText, buffRow.StatType, valueText);
                    break;
                case SkillType.Debuff:
                    buffObject.SetActive(false);
                    debuffObject.SetActive(true);
                    var debuffRow = sheets.StatBuffSheet[sheets.SkillBuffSheet[skillId].BuffIds.First()];
                    debuffStatTypeText.text = debuffRow.StatType.GetAcronym();
                    debuffIconImage.overrideSprite = BuffHelper.GetStatBuffIcon(debuffRow.StatType, true);
                    contentText.text = L10nManager.Localize("SKILL_DESCRIPTION_STATDEBUFF", chanceText, debuffRow.StatType, valueText);
                    break;
            }
            cooldownText.text = $"{L10nManager.Localize("UI_COOLDOWN")}: {coolDown}";
        }
    }
}
