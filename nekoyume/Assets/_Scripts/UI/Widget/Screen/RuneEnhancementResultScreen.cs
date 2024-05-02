using System;
using System.Collections.Generic;
using Libplanet.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
using Nekoyume.UI.Module;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RuneEnhancementResultScreen : ScreenWidget
    {
        [Serializable]
        private struct RuneLevelBonusDiff
        {
            public TextMeshProUGUI beforeText;
            public TextMeshProUGUI afterText;
        }

        [Serializable]
        private struct SpeechBubble
        {
            public GameObject container;
            public GameObject arrow;
            public TextMeshProUGUI beforeText;
            public TextMeshProUGUI afterText;
            public TextMeshProUGUI dialogText;
        }

        [SerializeField]
        private VanillaItemView vanillaItemView;

        [SerializeField]
        private ItemOptionTag itemOptionTag;

        [SerializeField]
        private TextMeshProUGUI runeText;

        [SerializeField]
        private TextMeshProUGUI currentLevelText;

        [SerializeField]
        private TextMeshProUGUI nextLevelText;

        [SerializeField]
        private TextMeshProUGUI skillNameText;

        [SerializeField]
        private TextMeshProUGUI skillDescText;

        [SerializeField]
        private List<TextMeshProUGUI> statTextList;

        [SerializeField]
        private List<TextMeshProUGUI> addStatTextList;

        [SerializeField]
        private GameObject skill;

        [SerializeField]
        private List<GameObject> stats;

        [SerializeField]
        private TextMeshProUGUI cpText;

        [SerializeField]
        private Animator animator;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private SpeechBubble speechBubble;

        [SerializeField]
        private RuneLevelBonusDiff runeLevelBonusDiff;

        private static readonly int HashToSuccess =
            Animator.StringToHash("Success");

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() =>
            {
                speechBubble.container.SetActive(false);
                Close(true);
            });
            CloseWidget = () =>
            {
                speechBubble.container.SetActive(false);
                Close(true);
            };
        }

        public void Show(
            RuneItem runeItem,
            int tryCount,
            IRandom random,
            (int previousCp, int currentCp) cp)
        {
            base.Show(true);

            RuneHelper.TryEnhancement(
                runeItem.Level,
                runeItem.CostRow,
                random,
                tryCount,
                out var tryResult);

            AudioController.instance.PlaySfx(AudioController.SfxCode.Success);

            var resultLevel = runeItem.Level + tryResult.LevelUpCount;

            UpdateInformation(runeItem, resultLevel);
            currentLevelText.text = $"+{runeItem.Level}";
            nextLevelText.text = $"+{resultLevel}";

            var allRuneState = Game.Game.instance.States.AllRuneState;
            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            var runeLevelBonusSheet = Game.Game.instance.TableSheets.RuneLevelBonusSheet;
            var beforeReward = RuneFrontHelper.CalculateRuneLevelBonusReward(
                allRuneState, runeListSheet, runeLevelBonusSheet,
                (runeItem.Row.Id, runeItem.Level));
            var currentReward = RuneHelper.CalculateRuneLevelBonus(
                allRuneState, runeListSheet, runeLevelBonusSheet);
            runeLevelBonusDiff.beforeText.text = $"+{beforeReward / 1000m:0.###}%";
            runeLevelBonusDiff.afterText.text = $"+{currentReward / 1000m:0.###}%";

            var isCombine = runeItem.Level == 0;
            speechBubble.arrow.SetActive(!isCombine);
            speechBubble.beforeText.gameObject.SetActive(!isCombine);
            speechBubble.beforeText.text = $"+{runeItem.Level}";
            speechBubble.afterText.text = $"+{resultLevel}";
            speechBubble.dialogText.text = isCombine ?
                L10nManager.Localize("UI_RUNE_COMBINE_COMPLETE") :
                L10nManager.Localize("UI_RUNE_UPGRADE_COMPLETE");
            speechBubble.container.SetActive(true);

            animator.Play(HashToSuccess);
            var (previousCp, currentCp) = cp;
            if (previousCp != currentCp)
            {
                Find<CPScreen>().Show(previousCp, currentCp);
            }
        }

        private void UpdateInformation(RuneItem item, int resultLevel)
        {
            vanillaItemView.SetData(item.RuneStone);
            runeText.text = L10nManager.Localize($"RUNE_NAME_{item.Row.Id}");

            if (!item.OptionRow.LevelOptionMap.TryGetValue(resultLevel, out var option))
            {
                return;
            }

            cpText.text = $"CP {option.Cp}";

            UpdateSkillInformation(option);
            UpdateStatInformation(item, option);
            UpdateOptionTag(option, item.Row.Grade);
        }

        private void UpdateSkillInformation(RuneOptionSheet.Row.RuneOptionInfo option)
        {
            skill.gameObject.SetActive(false);
            if (option.SkillId != 0)
            {
                skill.gameObject.SetActive(true);
                skillNameText.text = L10nManager.Localize($"SKILL_NAME_{option.SkillId}");
                var skillValueString = RuneFrontHelper.GetRuneValueString(option);

                skillDescText.text = L10nManager.Localize(
                    $"SKILL_DESCRIPTION_{option.SkillId}", option.SkillChance, option.BuffDuration, skillValueString);
            }
        }

        private void UpdateStatInformation(RuneItem item, RuneOptionSheet.Row.RuneOptionInfo nextOption)
        {
            foreach (var stat in stats)
            {
                stat.SetActive(false);
            }

            for (var i = 0; i < nextOption.Stats.Count; i++)
            {
                var info = nextOption.Stats[i];
                stats[i].gameObject.SetActive(true);
                statTextList[i].text = $"{info.stat.StatType.ToString()} {info.stat.StatType.ValueToString(info.stat.TotalValueAsLong)}";
            }

            if (item.Level > 0)
            {
                if (!item.OptionRow.LevelOptionMap.TryGetValue(item.Level, out var curOption))
                {
                    return;
                }

                for (var i = 0; i < nextOption.Stats.Count; i++)
                {
                    var next = nextOption.Stats[i];
                    var cur = curOption.Stats[i];
                    var result = next.stat.TotalValueAsLong - cur.stat.TotalValueAsLong;
                    addStatTextList[i].text = $"(+{cur.stat.StatType.ValueToString(result)})";
                }
            }
            else
            {
                for (var i = 0; i < nextOption.Stats.Count; i++)
                {
                    var info = nextOption.Stats[i];
                    addStatTextList[i].text = $"(+{info.stat.StatType.ValueToString(info.stat.TotalValueAsLong)})";
                }
            }
        }

        private void UpdateOptionTag(RuneOptionSheet.Row.RuneOptionInfo option, int grade)
        {
            var skillOption = option.SkillId != 0;
            itemOptionTag.gameObject.SetActive(skillOption);
            if (skillOption)
            {
                itemOptionTag.Set(grade);
            }
        }
    }
}
