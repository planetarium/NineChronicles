using System;
using System.Collections.Generic;
using System.Linq;
using Coffee.UIEffects;
using Libplanet.Action;
using Nekoyume.Game.Controller;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.TableData;
using Nekoyume.UI.Model;
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

        [SerializeField]
        private Image runeImage;

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
        private UIHsvModifier optionTagBg = null;

        [SerializeField]
        private List<Image> optionTagImages = null;

        [SerializeField]
        private OptionTagDataScriptableObject optionTagData = null;

        [SerializeField]
        private Button closeButton;

        [SerializeField]
        private SpeechBubble speechBubble;

        [SerializeField]
        private RuneLevelBonusDiff runeLevelBonusDiff;

        [SerializeField]
        private TextMeshProUGUI speechBubbleBeforeText;

        [SerializeField]
        private TextMeshProUGUI speechBubbleAfterText;

        private static readonly int HashToSuccess =
            Animator.StringToHash("Success");

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() =>
            {
                speechBubble.Close();
                Close(true);
            });
            CloseWidget = () =>
            {
                speechBubble.Close();
                Close(true);
            };
        }

        public void Show(
            RuneItem runeItem,
            int tryCount,
            IRandom random)
        {
            base.Show(true);

            var isSuccess = RuneHelper.TryEnhancement(
                runeItem.Level,
                runeItem.CostRow,
                random,
                tryCount,
                out var tryResult);

            AudioController.instance.PlaySfx(isSuccess
                ? AudioController.SfxCode.Success
                : AudioController.SfxCode.Failed);

            var resultLevel = runeItem.Level + tryResult.LevelUpCount;

            speechBubble.Show();
            UpdateInformation(runeItem, resultLevel);
            currentLevelText.text = $"+{runeItem.Level}";
            nextLevelText.text = $"+{resultLevel}";

            var allRuneState = Game.Game.instance.States.AllRuneState;
            var runeListSheet = Game.Game.instance.TableSheets.RuneListSheet;
            var before = RuneFrontHelper.CalculateRuneLevelBonus(
                allRuneState, runeListSheet, (runeItem.Row.Id, runeItem.Level));
            var current = RuneFrontHelper.CalculateRuneLevelBonus(
                allRuneState, runeListSheet);
            runeLevelBonusDiff.beforeText.text = $"{before / 10000m:0.####}";
            runeLevelBonusDiff.afterText.text = $"{current / 10000m:0.####}";

            speechBubbleBeforeText.text = $"+{runeItem.Level}";
            speechBubbleAfterText.text = $"+{resultLevel}";

            animator.Play(HashToSuccess);
        }

        private void UpdateInformation(RuneItem item, int resultLevel)
        {
            if (RuneFrontHelper.TryGetRuneIcon(item.Row.Id, out var icon))
            {
                runeImage.sprite = icon;
            }

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
            optionTagBg.gameObject.SetActive(false);

            foreach (var image in optionTagImages)
            {
                image.gameObject.SetActive(false);
            }

            if (option.SkillId != 0)
            {
                var data = optionTagData.GetOptionTagData(grade);
                var image = optionTagImages.First();
                image.gameObject.SetActive(true);
                image.sprite = optionTagData.SkillOptionSprite;
                optionTagBg.range = data.GradeHsvRange;
                optionTagBg.hue = data.GradeHsvHue;
                optionTagBg.saturation = data.GradeHsvSaturation;
                optionTagBg.value = data.GradeHsvValue;
                optionTagBg.gameObject.SetActive(true);
            }
        }
    }
}
