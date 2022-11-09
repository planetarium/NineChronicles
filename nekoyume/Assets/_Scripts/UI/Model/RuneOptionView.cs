using System.Collections.Generic;
using Nekoyume.L10n;
using Nekoyume.Model.Stat;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class RuneOptionView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private List<EnhancementOptionView> stats;

        [SerializeField]
        private EnhancementOptionView skill;

        [SerializeField]
        private List<GameObject> deco = new();

        public void Hide()
        {
            levelText.text = string.Empty;

            foreach (var d in deco)
            {
                d.SetActive(false);
            }

            foreach (var stat in stats)
            {
                stat.gameObject.SetActive(false);
            }

            skill.gameObject.SetActive(false);
        }

        public void Set(int level, RuneOptionSheet.Row.RuneOptionInfo option)
        {
            levelText.text = $"+{level}";
            foreach (var d in deco)
            {
                d.SetActive(true);
            }

            foreach (var stat in stats)
            {
                stat.gameObject.SetActive(false);
            }

            for (var i = 0; i < option.Stats.Count; i++)
            {
                var info = option.Stats[i];
                stats[i].gameObject.SetActive(true);
                stats[i].Set(info.statMap.StatType.ToString(), info.statMap.ValueAsInt.ToString());
            }

            if (option.SkillId != 0)
            {
                skill.gameObject.SetActive(true);
                var skillValue = option.SkillValueType == StatModifier.OperationType.Percentage
                    ? $"{option.SkillValue * 100}%"
                    : $"{option.SkillValue}";

                skill.Set(L10nManager.Localize($"SKILL_NAME_{option.SkillId}"),
                    $"{L10nManager.Localize("UI_SKILL_POWER")} : {skillValue}",
                    $"{L10nManager.Localize("UI_SKILL_CHANCE")} : {option.SkillChance}%",
                    $"{L10nManager.Localize("UI_COOLDOWN")} : {option.SkillCooldown}");
            }
            else
            {
                skill.gameObject.SetActive(false);
            }
        }
    }
}
