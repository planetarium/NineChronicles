using System;
using System.Collections;
using System.Collections.Generic;
using Assets.SimpleLocalization;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Module
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class LocalizedText : MonoBehaviour
    {
        [SerializeField]
        private string localizationKey = null;

        private TextMeshProUGUI _text;
        private TextMeshProUGUI Text
        {
            get
            {
                if (!_text)
                {
                    _text = GetComponent<TextMeshProUGUI>();
                }

                return _text;
            }
        }

        private void Awake()
        {
            Text.text = LocalizationManager.Localize(localizationKey);
        }
    }
}
