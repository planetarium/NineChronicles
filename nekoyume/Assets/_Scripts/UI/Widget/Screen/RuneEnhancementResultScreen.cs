using System.Collections.Generic;
using Coffee.UIEffects;
using Libplanet.Action;
using Libplanet.Assets;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.UI.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class RuneEnhancementResultScreen : PopupWidget
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
        private TextMeshProUGUI levelText;

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

        private static readonly int HashToSuccess =
            Animator.StringToHash("Success");

        private static readonly int HashToFail =
            Animator.StringToHash("Fail");

        protected override void Awake()
        {
            base.Awake();
            closeButton.onClick.AddListener(() =>
            {
                Close(true);
            });
            CloseWidget = () =>
            {
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
            UpdateInformation(runeItem);
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
                 out _);

            successText.gameObject.SetActive(isSuccess);
            failText.gameObject.SetActive(!isSuccess);
            animator.Play(isSuccess ? HashToSuccess : HashToFail);
        }

        private void UpdateInformation(RuneItem runeItem)
        {
            if (RuneFrontHelper.TryGetRuneIcon(runeItem.Row.Id, out var icon))
            {
                runeImage.sprite = icon;
            }

            runeText.text = L10nManager.Localize($"ITEM_NAME_{runeItem.Row.Id}");
            SetOptionTag();
        }

        private void SetOptionTag()
        {
            optionTagBg.gameObject.SetActive(false);

            // todo : 룬 옵션 갯수 체크

            var grade = 1;
            var data = optionTagData.GetOptionTagData(grade);
            foreach (var image in optionTagImages)
            {
                image.gameObject.SetActive(false);
            }

            optionTagBg.range = data.GradeHsvRange;
            optionTagBg.hue = data.GradeHsvHue;
            optionTagBg.saturation = data.GradeHsvSaturation;
            optionTagBg.value = data.GradeHsvValue;
            // var optionInfo = new ItemOptionInfo(equipment);

            // var optionCount = optionInfo.StatOptions.Sum(x => x.count);
            var optionCount = 0;
            var index = 0;
            for (var i = 0; i < optionCount; ++i)
            {
                var image = optionTagImages[index];
                image.gameObject.SetActive(true);
                image.sprite = optionTagData.StatOptionSprite;
                ++index;
            }

            // for (var i = 0; i < optionInfo.SkillOptions.Count; ++i)
            for (var i = 0; i < 0; ++i)
            {
                var image = optionTagImages[index];
                image.gameObject.SetActive(true);
                image.sprite = optionTagData.SkillOptionSprite;
                ++index;
            }

            if (optionCount > 0)
            {
                optionTagBg.gameObject.SetActive(true);
            }
        }
    }
}
