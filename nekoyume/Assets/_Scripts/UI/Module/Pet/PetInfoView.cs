using Nekoyume.Game;
using Nekoyume.Helper;
using Nekoyume.L10n;
using Nekoyume.State;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module.Pet
{
    public class PetInfoView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI petNameText;

        [SerializeField]
        private TextMeshProUGUI gradeText;

        [SerializeField]
        private TextMeshProUGUI subTypeText;

        [SerializeField]
        private Image spacerImage;

        [SerializeField]
        private Image soulStoneImage;

        [SerializeField]
        private Slider soulStoneGaugeSlider;

        [SerializeField]
        private Image sliderFillRectImage;

        [SerializeField]
        private TextMeshProUGUI soulStoneText;

        public void Set(int id, int grade, bool useGradeColor = true)
        {
            var color = useGradeColor
                ? LocalizationExtensions.GetItemGradeColor(grade)
                : Color.white;
            petNameText.text = L10nManager.Localize($"PET_NAME_{id}");
            petNameText.color = color;
            if (gradeText)
            {
                gradeText.text = L10nManager.Localize($"UI_ITEM_GRADE_{grade}");
                gradeText.color = color;
            }

            if (spacerImage)
            {
                spacerImage.color = color;
            }

            if (subTypeText)
            {
                subTypeText.color = color;
            }

            if (soulStoneImage)
            {
                soulStoneImage.overrideSprite = PetFrontHelper.GetSoulStoneSprite(id);
            }

            if (soulStoneGaugeSlider)
            {
                var isOwn = States.Instance.PetStates.TryGetPetState(id, out var pet);
                var isMaxLevel = !TableSheets.Instance.PetCostSheet[id]
                    .TryGetCost((pet?.Level ?? 0) + 1, out var nextCost);
                soulStoneGaugeSlider.maxValue = 1;
                soulStoneGaugeSlider.minValue = 0;
                soulStoneGaugeSlider.value = 1;
                if (isMaxLevel)
                {
                    sliderFillRectImage.color = PetFrontHelper.GetUIColor(PetFrontHelper.SoulStoneGaugeMax);
                    soulStoneText.text = "MAX";
                }
                else
                {
                    var ticker = TableSheets.Instance.PetSheet[id].SoulStoneTicker;
                    var ownSoulStone = (float)States.Instance.AvatarBalance[ticker].MajorUnit;
                    var need = nextCost.SoulStoneQuantity;
                    soulStoneText.text = $"{ownSoulStone}/{need}";
                    sliderFillRectImage.color = PetFrontHelper.GetUIColor(isOwn
                        ? PetFrontHelper.SoulStoneGaugeLevelUp
                        : PetFrontHelper.SoulStoneGaugeSummon);
                    soulStoneGaugeSlider.value = ownSoulStone / need;
                }
            }
        }
    }
}
