using DG.Tweening;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(RectTransform))]
    public class RectTransformShakeTweener : MonoBehaviour
    {
        [SerializeField]
        protected float duration = 1.0f;

        [SerializeField]
        private float magnitude = 2.5f;

        private RectTransform _rectTransform = null;

        protected Tweener Tweener { get; set; }

        public Tweener PlayLoop()
        {
            KillTween();

            if (!_rectTransform)
                _rectTransform = GetComponent<RectTransform>();

            var startValue = _rectTransform.anchoredPosition;

            // 10초에 한번씩 루프 (무한)
            Tweener = _rectTransform.DOShakeAnchorPos(
                10f, // 지속 시간
                magnitude,
                30, // 진동 수
                90, // 무작위성
                false,
                false);
            Tweener.SetLoops(-1);

            Tweener.onKill = () => _rectTransform.anchoredPosition = startValue;

            return Tweener;
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
