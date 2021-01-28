using Coffee.UISoftMask;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GuideBackground : TutorialItem
    {
        [SerializeField] private SoftMask mask;
        [SerializeField] private Image background;

        [SerializeField] private float fadeDuration = 1.0f;
        [SerializeField] private float alpha = 0.8f;

        public override void Play<T>(T data, System.Action callback)
        {
            if (data is GuideBackgroundData d)
            {
                SetMask(d.IsEnableMask, d.Target);
                SetFade(true, d.IsExistFadeIn ? fadeDuration : 0.0f, callback);
            }
        }

        public override void Stop()
        {
            SetFade(false, fadeDuration);
        }

        private void SetFade(bool isIn, float duration, System.Action action = null)
        {
            background.DOKill();
            var color = background.color;
            color.a = isIn ? 0 : alpha;
            background.color = color;
            background.DOFade(isIn ? alpha : 0.0f, duration).OnComplete(() => action?.Invoke());
        }

        private void SetMask(bool isEnable, RectTransform target)
        {
            mask.alpha = isEnable ? alpha : 0.0f;
            if (isEnable)
            {
                mask.rectTransform.anchoredPosition  = target.anchoredPosition;
            }
        }
    }
}
