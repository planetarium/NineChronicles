using System.Collections.Generic;
using Nekoyume.L10n;
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

        public void Set(int level, RuneOptionSheet.Row.RuneOptionInfo optionInfo)
        {
            levelText.text = $"+{level}";

            foreach (var stat in stats)
            {
                stat.gameObject.SetActive(false);
            }

            for (var i = 0; i < optionInfo.Stats.Count; i++)
            {
                var info = optionInfo.Stats[i];

                stats[i].gameObject.SetActive(true);
                stats[i].Set(info.statMap.StatType.ToString(), info.statMap.ValueAsInt.ToString());
            }

            if (optionInfo.SkillId != 0)
            {
                skill.gameObject.SetActive(true);
                skill.Set(L10nManager.Localize($"SKILL_NAME_{optionInfo.SkillId}"),
                    $"{L10nManager.Localize("UI_SKILL_POWER")} : {optionInfo.SkillValue}",
                    $"{L10nManager.Localize("UI_SKILL_CHANCE")} : {optionInfo.SkillChance}",
                    $"{L10nManager.Localize("UI_COOLDOWN")} : {optionInfo.SkillCooldown}");
            }
            else
            {
                skill.gameObject.SetActive(false);
            }
        }
    }
}
