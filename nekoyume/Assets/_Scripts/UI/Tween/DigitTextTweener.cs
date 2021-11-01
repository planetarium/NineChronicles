using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DigitTextTweener : DOTweenBase
    {
        public TweenCallback onComplete = null;

        public int beginValue = 0;

        public int endValue = 0;

        private TextMeshProUGUI _text = null;

        protected override void Awake()
        {
            base.Awake();
            _text = GetComponent<TextMeshProUGUI>();
        }

        private void OnDisable()
        {
            KillTween();
        }

        public new DG.Tweening.Tween Play()
        {
            KillTween();

            currentTween = DOTween.To(
                () => beginValue,
                value => _text.text = value.ToString(),
                endValue,
                duration);

            return SetEase().OnComplete(onComplete);
        }

        public DG.Tweening.Tween Play(int beginValue, int endValue)
        {
            this.beginValue = beginValue;
            this.endValue = endValue;

            return Play();
        }

        public void KillTween()
        {
            if (currentTween?.IsPlaying() ?? false)
            {
                currentTween?.Kill();
            }

            currentTween = null;
        }
    }
}
