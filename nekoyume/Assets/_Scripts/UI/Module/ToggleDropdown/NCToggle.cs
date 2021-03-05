using UnityEngine;
using UnityEngine.UI;
using System;
using TMPro;

namespace Nekoyume.UI.Module
{
    [Serializable]
    public class NCToggle : Toggle
    {
        public GameObject onObject;
        public GameObject offObject;

        private TextMeshProUGUI _onText;
        private TextMeshProUGUI _offText;
        protected NCToggle()
        {
            onValueChanged.AddListener(UpdateObject);
        }

        protected virtual void UpdateObject(bool value)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }
#endif

            if (onObject)
            {
                onObject.SetActive(isOn);
            }

            if (offObject)
            {
                offObject.SetActive(!isOn);
            }
        }

        protected override void OnDestroy()
        {
            onValueChanged.RemoveAllListeners();
            base.OnDestroy();
        }

        public void SetText(string text)
        {
            onObject.GetComponentInChildren<TextMeshProUGUI>().text = text;
            offObject.GetComponentInChildren<TextMeshProUGUI>().text = text;
        }
    }
}
