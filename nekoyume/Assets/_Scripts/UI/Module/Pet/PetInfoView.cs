using Nekoyume.L10n;
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

        public void Set(string petName, int grade, bool useGradeColor = true)
        {
            petNameText.text = petName;
            gradeText.text = L10nManager.Localize($"UI_ITEM_GRADE_{grade}");
            var color = useGradeColor
                ? LocalizationExtensions.GetItemGradeColor(grade)
                : Color.white;
            petNameText.color = color;
            gradeText.color = color;
            spacerImage.color = color;
            subTypeText.color = color;
        }
    }
}
