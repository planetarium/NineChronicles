using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    public class InAppProductTab : MonoBehaviour
    {
        [SerializeField]
        private TextMeshProUGUI disabledText;

        [SerializeField]
        private TextMeshProUGUI enabledText;

        public void SetText(string productName)
        {
            disabledText.text = enabledText.text = productName;
        }
    }
}
