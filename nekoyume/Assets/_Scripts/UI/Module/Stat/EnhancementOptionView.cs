using System;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class EnhancementOptionView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI typeText;
        [SerializeField] private TextMeshProUGUI currentText;
        [SerializeField] private TextMeshProUGUI addText;
        [SerializeField] private TextMeshProUGUI currentExText;
        [SerializeField] private TextMeshProUGUI addExText;

        public void Initialize(string type, string currentValue, string addValue,
                                                string exValue, string addExValue)
        {
            typeText.text = type;

            currentText.text = currentValue;
            addText.text = addValue;

            currentExText.text = exValue;
            addExText.text = addExValue;
        }
    }
}
