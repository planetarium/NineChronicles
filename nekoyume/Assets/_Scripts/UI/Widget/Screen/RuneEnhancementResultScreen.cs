using System.Collections.Generic;
using System.Linq;
using Coffee.UIEffects;
using Libplanet.Action;
using Libplanet.Assets;
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
        [SerializeField]
        private Image runeImage;

        [SerializeField]
        private TextMeshProUGUI runeText;

        [SerializeField]
        private TextMeshProUGUI successText;

        [SerializeField]
        private TextMeshProUGUI failText;

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

        private static readonly int HashToSuccess =
            Animator.StringToHash("Success");

        private static readonly int HashToFail =
            Animator.StringToHash("Fail");

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
            FungibleAssetValue ncg,
            FungibleAssetValue crystal,
            int tryCount,
            IRandom random)
        {
            base.Show(true);

            var isSuccess = RuneHelper.TryEnhancement(
                ncg,
                crystal,
                runeItem.RuneStone,
                ncg.Currency,
                crystal.Currency,
                runeItem.RuneStone.Currency,
                runeItem.Cost,
                random,
                tryCount,
                 out var tryResult);

            var speech = string.Empty;

            AudioController.instance.PlaySfx(isSuccess
                ? AudioController.SfxCode.Success
                : AudioController.SfxCode.Failed);

            if (isSuccess)
            {
                speech = tryCount != tryResult
                    ? L10nManager.Localize("UI_RUNE_LEVEL_UP_SUCCESS_1", tryCount, tryResult)
                    : L10nManager.Localize("UI_RUNE_LEVEL_UP_SUCCESS_2", tryResult);
            }
            else
            {
                speech = L10nManager.Localize("UI_RUNE_LEVEL_UP_FAIL", tryResult);
            }

            speechBubble.Show();
            StartCoroutine(speechBubble.CoShowText(speech, true));

            UpdateInformation(runeItem, isSuccess);
            currentLevelText.text = $"+{runeItem.Level}";
            nextLevelText.text = $"+{runeItem.Level + 1}";
            successText.gameObject.SetActive(isSuccess);
            failText.gameObject.SetActive(!isSuccess);
            animator.Play(isSuccess ? HashToSuccess : HashToFail);
        }

        private void UpdateInformation(RuneItem item, bool isSuccess)
        {
            if (RuneFrontHelper.TryGetRuneIcon(item.Row.Id, out var icon))
            {
                runeImage.sprite = icon;
            }

            runeText.text = L10nManager.Localize($"RUNE_NAME_{item.Row.Id}");

            var level = isSuccess ? item.Level + 1 : item.Level;
            if (!item.OptionRow.LevelOptionMap.TryGetValue(level, out var option))
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
                statTextList[i].text = $"{info.statMap.StatType.ToString()} {info.statMap.ValueAsInt}";
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
                    var result = next.statMap.ValueAsInt - cur.statMap.ValueAsInt;
                    addStatTextList[i].text = $"(+{result})";
                }
            }
            else
            {
                for (var i = 0; i < nextOption.Stats.Count; i++)
                {
                    var info = nextOption.Stats[i];
                    addStatTextList[i].text = $"(+{info.statMap.ValueAsInt})";
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
