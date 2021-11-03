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
            Stop();
        }

        public override void Play()
        {
            Stop();

            currentTween = DOTween.To(
                () => beginValue,
                value => _text.text = value.ToString(),
                endValue,
                duration);

            SetEase().OnComplete(onComplete);
        }

        public void Play(int beginValue, int endValue)
        {
            this.beginValue = beginValue;
            this.endValue = endValue;

            Play();
        }
    }
}
