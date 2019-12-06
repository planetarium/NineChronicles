using Assets.SimpleLocalization;
using Nekoyume.UI.Tween;
using System;
using TMPro;
using UniRx;
using UnityEngine.UI;

namespace Nekoyume.UI.Module
{
    public class GlowingButton : NormalButton
    {
        public class Model : IDisposable
        {
            public readonly ReactiveProperty<bool> IsEnabled = new ReactiveProperty<bool>(false);

            public void Dispose()
            {
                IsEnabled.Dispose();
            }
        }

        public GraphicAlphaTweener imageTweener;
        public GraphicAlphaTweener textTweener;
        public TextMeshProUGUI glowText;

        public readonly Model SharedModel = new Model();

        protected override void Awake()
        {
            base.Awake();
            glowText.text = LocalizationManager.Localize(string.IsNullOrEmpty(localizationKey) ? "null" : localizationKey);
            SharedModel.IsEnabled.Subscribe(value =>
            {
                imageTweener.enabled = value;
                textTweener.enabled = value;
            }).AddTo(gameObject);
        }
    }
}
