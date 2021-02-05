using System;
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
        [SerializeField] private float maskDuration = 1.0f;
        [SerializeField] private AnimationCurve maskCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(1, 1));
        [SerializeField] private float alpha = 0.8f;
        [SerializeField] private SoftMask mask;
        [SerializeField] private Image background;

        private Coroutine _coroutine;
        private Vector2 _cachedMaskSize;

        private void Awake()
        {
            _cachedMaskSize = mask.rectTransform.sizeDelta;
        }

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
            SetFade(true, data.isExistFadeIn ? fadeDuration : 0.0f);
            SetButton(data.buttonRectTransform, data.target);
            SetMaskSize(data.target);

            mask.rectTransform.position = data.target ? data.target.position : Vector3.zero;
            mask.alpha = data.isEnableMask ? alpha : 0.0f;

            float time = 0f;
            float tick = maskDuration / 60.0f;
            while (mask.alpha < alpha - 0.01f)
            {
                time += tick;
                mask.alpha = maskCurve.Evaluate(time) * alpha;
                yield return null;
            }
            callback?.Invoke();
        }

        private void SetButton(RectTransform buttonRectTransform, RectTransform target)
        {
            buttonRectTransform.position = target ? target.position  : Vector3.zero;
            buttonRectTransform.sizeDelta = target ? target.sizeDelta : Vector2.one * 2000;
        }

        private void SetFade(bool isIn, float duration, System.Action callback = null)
        {
            background.DOKill();
            var color = background.color;
            color.a = isIn ? 0 : alpha;
            background.color = color;
            background.DOFade(isIn ? alpha : 0.0f, duration)
                .SetEase(fadeCurve)
                .OnComplete(() => callback?.Invoke());
        }

        private void SetMaskSize(RectTransform target)
        {
            mask.rectTransform.sizeDelta = _cachedMaskSize;
            if (target && target.sizeDelta.sqrMagnitude > mask.rectTransform.sizeDelta.sqrMagnitude)
            {
                target.sizeDelta = mask.rectTransform.sizeDelta * 1.2f;
            }
        }
    }
}
