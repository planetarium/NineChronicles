using UnityEngine;
using UnityEngine.UI;
using System;

namespace Nekoyume.UI.Module
{
    [Serializable]
    public class Toggle : UnityEngine.UI.Toggle
    {
        public GameObject offObject;
        public GameObject onObject;

        protected Toggle()
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
    }
}
