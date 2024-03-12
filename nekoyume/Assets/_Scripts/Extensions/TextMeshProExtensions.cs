using System;
using TMPro;
using UnityEngine;

namespace Nekoyume
{
    using UniRx;
    public static class TextMeshProExtensions
    {
        private static readonly Camera Camera;

        static TextMeshProExtensions()
        {
            Camera = Camera.main;
        }

        private static void OnClickTextWithLink(TMP_Text text, Action<TMP_LinkInfo> onClick)
        {
            if (text == null)
            {
                return;
            }

            var linkIndex = TMP_TextUtilities.FindIntersectingLink(text, Input.mousePosition, Camera);
            if (linkIndex != -1)
            {
                onClick.Invoke(text.textInfo.linkInfo[linkIndex]);
            }
        }

        public static IDisposable SubscribeForClickLink(this TextMeshProUGUI text,
            Action<TMP_LinkInfo> onClick)
        {
            return Observable.EveryUpdate().Where(_ => Input.GetKeyDown(KeyCode.Mouse0))
                .Subscribe(_ => OnClickTextWithLink(text, onClick));
        }
    }
}
