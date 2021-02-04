using System.Collections;
using Coffee.UISoftMask;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Nekoyume.UI
{
    public class GuideBackground : TutorialItem
    {
        [SerializeField] private float fadeDuration = 1.0f;
        [SerializeField] private AnimationCurve fadeCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        [SerializeField] private float alpha = 0.8f;
        [SerializeField] private SoftMask mask;
        [SerializeField] private Image background;

        private Coroutine _coroutine;

        public override void Play<T>(T data, System.Action callback)
        {
            if (data is GuideBackgroundData d)
            {
                if (_coroutine != null)
                {
                    StopCoroutine(_coroutine);
                }
                _coroutine = StartCoroutine(LatePlay(d, callback));
            }
        }

        public override void Stop(System.Action callback)
        {
            SetFade(false, fadeDuration, callback);
        }

        private IEnumerator LatePlay(GuideBackgroundData data, System.Action callback)
        {
            yield return new WaitForSeconds(predelay);
            SetMask(data.isEnableMask, data.target);
            SetFade(true, data.isExistFadeIn ? fadeDuration : 0.0f, callback);
        }

        private void SetFade(bool isIn, float duration, System.Action action)
        {
            background.DOKill();
            var color = background.color;
            color.a = isIn ? 0 : alpha;
            background.color = color;
            background.DOFade(isIn ? alpha : 0.0f, duration)
                .SetEase(fadeCurve)
                .OnComplete(() => action?.Invoke());
        }

        private void SetMask(bool isEnable, RectTransform target)
        {
            mask.alpha = isEnable ? alpha : 0.0f;
            if (isEnable)
            {
                mask.rectTransform.position = target? target.position : Vector3.zero;
            }
        }
    }
}
