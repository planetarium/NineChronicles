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

            toggledOnText.text = L10nManager.Localize(string.IsNullOrEmpty(localizationKey) ? "null" : localizationKey);
            SharedModel.IsEnabled.SubscribeTo(glowImage.gameObject).AddTo(gameObject);
        }
    }
}
