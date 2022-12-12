using System.Collections.Generic;
using System.Linq;
using Nekoyume.L10n;
using Nekoyume.Model.Stat;
using Nekoyume.State;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Model
{
    public class RuneOptionView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI levelText;

        [SerializeField]
        private TextMeshProUGUI nextLevelText;

        [SerializeField]
        private List<EnhancementOptionView> stats;

        [SerializeField]
        private EnhancementOptionView skill;

        [SerializeField]
        private List<GameObject> statArrows;

        [SerializeField]
        private List<GameObject> skillArrows;

        [SerializeField]
        private List<GameObject> skillObjects;

        [SerializeField]
        private GameObject levelArrow;

        [SerializeField]
        private PositionTooltip tooltip;

        public void Hide()
        {
            levelText.text = string.Empty;

            foreach (var stat in stats)
            {
                stat.gameObject.SetActive(false);
            }

            skill.gameObject.SetActive(false);
        }

        public void Set(
            int level,
            int nextLevel,
            RuneOptionSheet.Row.RuneOptionInfo option,
            RuneOptionSheet.Row.RuneOptionInfo nextOption)
        {
            levelText.text = $"+{level}";
            nextLevelText.text = $"+{nextLevel}";
            levelArrow.SetActive(true);
            stats.ForEach(x => x.gameObject.SetActive(false));
            statArrows.ForEach(x => x.SetActive(false));
            skillArrows.ForEach(x => x.SetActive(false));

            for (var i = 0; i < option.Stats.Count; i++)
            {
                var info = option.Stats[i];
                var nextInfo = nextOption.Stats[i];
                statArrows[i].gameObject.SetActive(true);
                stats[i].gameObject.SetActive(true);
                stats[i].Set(
                    info.statMap.StatType.ToString(),
                    info.statMap.ValueAsInt.ToString(),
                    nextInfo.statMap.ValueAsInt.ToString());
            }

            if (option.SkillId != 0)
            {
                skill.gameObject.SetActive(true);
                skillObjects.ForEach(x => x.SetActive(true));
                skillArrows.ForEach(x => x.SetActive(true));

                var isPercent = option.SkillValueType == StatModifier.OperationType.Percentage;
                var skillName = L10nManager.Localize($"SKILL_NAME_{option.SkillId}");
                var curPower = isPercent ? option.SkillValue * 100 : option.SkillValue;
                var nextPower = isPercent ? nextOption.SkillValue * 100 : nextOption.SkillValue;
                var curSkillValue = curPower == (int)curPower ? $"{(int)curPower}" : $"{curPower}";
                var nextSkillValue =
                    nextPower == (int)nextPower ? $"{(int)nextPower}" : $"{nextPower}";
                var curChance = $"{option.SkillChance}%";
                var nextChance = option.SkillChance == nextOption.SkillChance ? string.Empty : $"{nextOption.SkillChance}%";
                var curCooldown = $"{option.SkillCooldown}";
                var nextCooldown = option.SkillCooldown == nextOption.SkillCooldown ? string.Empty : $"{nextOption.SkillCooldown}";

                var tableSheets = Game.Game.instance.TableSheets;
                var currentValueString = curSkillValue;
                var nextValueString = nextSkillValue;
                if (tableSheets.SkillSheet.TryGetValue(option.SkillId, out var skillRow) &&
                    tableSheets.SkillBuffSheet.TryGetValue(skillRow.Id, out var skillBuffRow) &&
                    tableSheets.StatBuffSheet.TryGetValue(skillBuffRow.BuffIds.First(), out var buffRow))
                {
                    currentValueString =
                        $"{StatExtensions.ValueToString(buffRow.StatModifier.StatType, (int)curPower)}";
                    nextValueString =
                        $"{StatExtensions.ValueToString(buffRow.StatModifier.StatType, (int)nextPower)}";
                }
                else if (isPercent)
                {
                    currentValueString = $"{curSkillValue}%";
                    nextValueString = $"{nextSkillValue}%";
                }

                var skillDescription = L10nManager.Localize($"SKILL_DESCRIPTION_{option.SkillId}",
                    nextOption.SkillChance, nextOption.BuffDuration, nextValueString);
                skill.Set(skillName,
                    skillDescription,
                    currentValueString,
                    nextValueString,
                    curChance,
                    nextChance,
                    curCooldown,
                    nextCooldown);

                if (tooltip != null)
                {
                    tooltip.Set(skillName, skillDescription);
                    tooltip.gameObject.SetActive(false);
                }
            }
            else
            {
                skill.gameObject.SetActive(false);
                skillObjects.ForEach(x => x.SetActive(false));
                if (tooltip != null)
                {
                    tooltip.gameObject.SetActive(false);
                }
            }
        }

        public void Set(int level, RuneOptionSheet.Row.RuneOptionInfo option)
        {
            levelText.text = $"+{level}";
            nextLevelText.text = string.Empty;
            levelArrow.SetActive(false);
            stats.ForEach(x => x.gameObject.SetActive(false));
            statArrows.ForEach(x => x.SetActive(false));
            skillArrows.ForEach(x => x.SetActive(false));

            for (var i = 0; i < option.Stats.Count; i++)
            {
                var info = option.Stats[i];
                stats[i].gameObject.SetActive(true);
                stats[i].Set(
                    info.statMap.StatType.ToString(),
                    info.statMap.ValueAsInt.ToString(),
                    string.Empty);
            }

            if (option.SkillId != 0)
            {
                skill.gameObject.SetActive(true);
                skillObjects.ForEach(x => x.SetActive(true));
                var isPercent = option.SkillValueType == StatModifier.OperationType.Percentage;
                var skillName = L10nManager.Localize($"SKILL_NAME_{option.SkillId}");
                var power = isPercent ? option.SkillValue * 100 : option.SkillValue;
                var skillValue = power == (int)power ? $"{(int)power}" : $"{power}";

                var tableSheets = Game.Game.instance.TableSheets;
                var skillValueString = skillValue;
                if (tableSheets.SkillSheet.TryGetValue(option.SkillId, out var skillRow) &&
                    tableSheets.SkillBuffSheet.TryGetValue(skillRow.Id, out var skillBuffRow) &&
                    tableSheets.StatBuffSheet.TryGetValue(skillBuffRow.BuffIds.First(), out var buffRow))
                {
                    skillValueString =
                        $"{StatExtensions.ValueToString(buffRow.StatModifier.StatType, (int)power)}";
                }
                else if (isPercent)
                {
                    skillValueString = $"{power}%";
                }

                var skillDescription = L10nManager.Localize($"SKILL_DESCRIPTION_{option.SkillId}",
                    option.SkillChance, option.BuffDuration, skillValueString);
                skill.Set(skillName,
                    skillDescription,
                    skillValueString,
                    string.Empty,
                    $"{option.SkillChance}%",
                    string.Empty,
                    $"{option.SkillCooldown}",
                    string.Empty);

                if (tooltip != null)
                {
                    tooltip.Set(skillName, skillDescription);
                    tooltip.gameObject.SetActive(false);
                }
            }
            else
            {
                skill.gameObject.SetActive(false);
                skillObjects.ForEach(x => x.SetActive(false));
                if (tooltip != null)
                {
                    tooltip.gameObject.SetActive(false);
                }
            }
        }
    }
}
