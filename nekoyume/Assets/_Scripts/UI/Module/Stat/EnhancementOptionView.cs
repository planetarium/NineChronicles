using TMPro;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class EnhancementOptionView : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI typeText;

        [SerializeField]
        private TextMeshProUGUI contentText;

        [SerializeField]
        private TextMeshProUGUI currentText;

        [SerializeField]
        private TextMeshProUGUI currentExText;

        [SerializeField]
        private TextMeshProUGUI addText;

        [SerializeField]
        private TextMeshProUGUI addExText;

        [SerializeField]
        private TextMeshProUGUI etcText;

        [SerializeField]
        private TextMeshProUGUI etcExText;

        [SerializeField]
        private GameObject star;

        [SerializeField]
        private Button descriptionButton;

        private RectTransform _descriptionRect;

        public float3 DescriptionPosition => _descriptionRect.GetWorldPositionOfPivot(EnumType.PivotPresetType.MiddleLeft);

        private System.Action _onClickButton;

        private void Awake()
        {
            if (descriptionButton)
            {
                descriptionButton.onClick.AddListener(() => _onClickButton?.Invoke());
                _descriptionRect = descriptionButton.GetComponent<RectTransform>();
            }
        }

        public void Set(string type, string currentValue, string addValue, int count = 1)
        {
            typeText.text = type;
            currentText.text = currentValue;
            addText.text = addValue;
            if (star)
            {
                star.SetActive(count > 1);
            }
        }

        public void Set(string type, string currentValue, string addValue,
            string exValue, string addExValue)
        {
            typeText.text = type;

            currentText.text = currentValue;
            addText.text = addValue;

            currentExText.text = exValue;
            addExText.text = addExValue;
        }

        public void Set(string type, string currentValue, string addValue)
        {
            typeText.text = type;
            currentText.text = currentValue;
            addText.text = addValue;
        }

        public void Set(
            string type,
            string content,
            string currentValue,
            string currentExValue,
            string addValue,
            string addExValue,
            string etcValue,
            string etcExValue)
        {
            typeText.text = type;
            contentText.text = content;
            currentText.text = currentValue;
            currentExText.text = currentExValue;
            addText.text = addValue;
            addExText.text = addExValue;
            etcText.text = etcValue;
            etcExText.text = etcExValue;
        }

        public void SetDescriptionButton(System.Action action)
        {
            _onClickButton = action;
        }
    }
}
