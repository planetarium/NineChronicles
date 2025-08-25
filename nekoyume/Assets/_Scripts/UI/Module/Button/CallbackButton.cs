using System.Collections.Generic;
using System.Linq;
using Nekoyume.Helper;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    using System;

    public class CallbackButton : MonoBehaviour
    {
        [SerializeField]
        private Button button;

        public void Set(Action onClick)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() =>
            {
                onClick?.Invoke();
            });
        }
    }
}
