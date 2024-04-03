using System;
using System.Collections.Generic;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.Model.EnumType;
using Nekoyume.TableData;
using Nekoyume.UI.Module;
using Nekoyume.UI.Module.Common;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

        [SerializeField]
        private Image adventure;

        [SerializeField]
        private Image arena;

        [SerializeField]
        private Image raid;

        [SerializeField]
        private GameObject cpArrow;

        [SerializeField]
        private TextMeshProUGUI currentCp;

        [SerializeField]
        private TextMeshProUGUI nextCp;

        private RuneOptionSheet.Row _optionRow;
        private int _startLevel;
        private RuneUsePlace _runeUsePlace;

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
            RuneOptionSheet.Row optionRow,
            int startLevel,
            RuneUsePlace runeUsePlace)
        {
            _optionRow = optionRow;
            _startLevel = startLevel;
            _runeUsePlace = runeUsePlace;
        }

        public void UpdateTryCount(int tryCount)
        {
            if (_optionRow is null)
            {
                return;
            }

            if (_startLevel == 0)
            {
                if (!_optionRow.LevelOptionMap.TryGetValue(1, out var statInfo))
                {
                    return;
                }

                Set(1, statInfo, _runeUsePlace);
            }
            else
            {
                if (!_optionRow.LevelOptionMap.TryGetValue(_startLevel, out var statInfo))
                {
                    return;
                }

                var nextLevel = _startLevel + tryCount;
                if (_optionRow.LevelOptionMap.TryGetValue(nextLevel, out var nextStatInfo))
                {
                    Set(
                        _startLevel,
                        nextLevel,
                        statInfo,
                        nextStatInfo,
                        _runeUsePlace);
                }
                else // max level
                {
                    Set(_startLevel, statInfo, _runeUsePlace);
                }
            }
        }

        private void Set(
            int level,
            int nextLevel,
            RuneOptionSheet.Row.RuneOptionInfo option,
            RuneOptionSheet.Row.RuneOptionInfo nextOption,
            RuneUsePlace runeUsePlace)
        {
            UpdateAreaIcon(runeUsePlace);
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
                    info.stat.StatType.ToString(),
                    info.stat.StatType.ValueToString(info.stat.TotalValueAsLong),
                    nextInfo.stat.StatType.ValueToString(nextInfo.stat.TotalValueAsLong));
            }

            if (option.SkillId != 0)
            {
                skill.gameObject.SetActive(true);
                skillObjects.ForEach(x => x.SetActive(true));
                skillArrows.ForEach(x => x.SetActive(true));

                var skillName = L10nManager.Localize($"SKILL_NAME_{option.SkillId}");
                var curChance = $"{option.SkillChance}%";
                var nextChance = option.SkillChance == nextOption.SkillChance
                    ? $"<color=#FFF3D4>{option.SkillChance}%</color>"
                    : $"{nextOption.SkillChance}%";
                var curCooldown = $"{option.SkillCooldown}";
                var nextCooldown = option.SkillCooldown == nextOption.SkillCooldown
                    ? $"<color=#FFF3D4>{option.SkillCooldown}</color>"
                    : $"{nextOption.SkillCooldown}";
                var currentValueString = RuneFrontHelper.GetRuneValueString(option);
                var nextValueString = RuneFrontHelper.GetRuneValueString(nextOption);
                if (currentValueString.Equals(nextValueString))
                {
                    nextValueString = $"<color=#FFF3D4>{currentValueString}</color>";
                }
                var skillDescription = L10nManager.Localize($"SKILL_DESCRIPTION_{option.SkillId}",
                    option.SkillChance, option.BuffDuration, currentValueString);

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

            //cpSetting
            cpArrow.SetActive(true);
            currentCp.text = option.Cp.ToString();
            nextCp.text = nextOption.Cp.ToString();
            nextCp.gameObject.SetActive(true);
        }

        private void Set(int level, RuneOptionSheet.Row.RuneOptionInfo option, RuneUsePlace runeUsePlace)
        {
            UpdateAreaIcon(runeUsePlace);
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
                    info.stat.StatType.ToString(),
                    info.stat.StatType.ValueToString(info.stat.TotalValueAsLong),
                    string.Empty);
            }

            if (option.SkillId != 0)
            {
                skill.gameObject.SetActive(true);
                skillObjects.ForEach(x => x.SetActive(true));
                var skillName = L10nManager.Localize($"SKILL_NAME_{option.SkillId}");
                var skillValueString = RuneFrontHelper.GetRuneValueString(option);
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

            //cpSetting
            cpArrow.SetActive(false);
            currentCp.text = option.Cp.ToString();
            nextCp.gameObject.SetActive(false);
        }

        private void UpdateAreaIcon(RuneUsePlace runeUsePlace)
        {
            switch (runeUsePlace)
            {
                case RuneUsePlace.Adventure:
                    adventure.gameObject.SetActive(true);
                    arena.gameObject.SetActive(false);
                    raid.gameObject.SetActive(false);
                    break;
                case RuneUsePlace.Arena:
                    adventure.gameObject.SetActive(false);
                    arena.gameObject.SetActive(true);
                    raid.gameObject.SetActive(false);
                    break;
                case RuneUsePlace.AdventureAndArena:
                    adventure.gameObject.SetActive(true);
                    arena.gameObject.SetActive(true);
                    raid.gameObject.SetActive(false);
                    break;
                case RuneUsePlace.Raid:
                    adventure.gameObject.SetActive(false);
                    arena.gameObject.SetActive(false);
                    raid.gameObject.SetActive(true);
                    break;
                case RuneUsePlace.RaidAndAdventure:
                    adventure.gameObject.SetActive(true);
                    arena.gameObject.SetActive(false);
                    raid.gameObject.SetActive(true);
                    break;
                case RuneUsePlace.RaidAndArena:
                    adventure.gameObject.SetActive(false);
                    arena.gameObject.SetActive(true);
                    raid.gameObject.SetActive(true);
                    break;
                case RuneUsePlace.All:
                    adventure.gameObject.SetActive(true);
                    arena.gameObject.SetActive(true);
                    raid.gameObject.SetActive(true);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(runeUsePlace), runeUsePlace, null);
            }
        }
    }
}
