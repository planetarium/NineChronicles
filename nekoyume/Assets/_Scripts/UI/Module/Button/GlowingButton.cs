using System;
using Nekoyume.L10n;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class GlowingButton : ToggleableButton
    {
        public class Model : IDisposable
        {
            public readonly ReactiveProperty<bool> IsEnabled = new ReactiveProperty<bool>();

            public void Dispose()
            {
                IsEnabled.Dispose();
            }
        }

        public Image glowImage;
        public TextMeshProUGUI glowText;

        public readonly Model SharedModel = new Model();

        protected override void Awake()
        {
            base.Awake();

            SharedModel.IsEnabled.SubscribeTo(glowImage.gameObject).AddTo(gameObject);
        }

        protected override void SetText(string text)
        {
            base.SetText(text);
            if (!(glowText is null))
            {
                glowText.text = text;
            }
        }
    }
}
