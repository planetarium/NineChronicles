using DG.Tweening;
using TMPro;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class DigitTextTweener : MonoBehaviour
    {
        public TweenCallback onComplete = null;

        public int startValue = 0;

        public int endValue = 0;

        [SerializeField]
        protected float duration = 0.0f;

        private TextMeshProUGUI text = null;

        protected Tweener Tweener { get; set; }

        private void Awake()
        {
            text = GetComponent<TextMeshProUGUI>();
        }

        public Tweener Play()
        {
            KillTween();

            Tweener = DOTween.To(
                () => startValue,
                value => text.text = value.ToString(),
                endValue,
                duration);
            Tweener.onComplete = onComplete;
            return Tweener;
        }

        public Tweener Play(int startValue, int endValue)
        {
            this.startValue = startValue;
            this.endValue = endValue;

            return Play();
        }

        public void KillTween()
        {
            if (Tweener?.IsPlaying() ?? false)
            {
                Tweener?.Kill();
            }

            Tweener = null;
        }
    }
}
