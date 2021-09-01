using System;
using System.Collections;
using Coffee.UISoftMask;
using DG.Tweening;
using Nekoyume.UI.Module;
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

        private Menu _menu;
        private Coroutine _coroutine;
        private Vector2 _cachedMaskSize;

        private void Awake()
        {
            _menu = Widget.Find<Menu>();
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
            FadeOut(fadeDuration, callback);
        }

        private IEnumerator LatePlay(GuideBackgroundData data, System.Action callback)
        {
            yield return new WaitForSeconds(predelay);
            FadeIn(data.isExistFadeIn ? fadeDuration : 0.0f);
            SetButton(data.fullScreenButton, data.buttonRectTransform, data.target);
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

        private void SetButton(bool isFullScreen, RectTransform buttonRectTransform, RectTransform target)
        {
            if (isFullScreen)
            {
                buttonRectTransform.position = Vector3.zero;
                buttonRectTransform.sizeDelta = Vector2.one * 2000;
            }
            else
            {
                buttonRectTransform.position = target ? target.position  : Vector3.zero;
                buttonRectTransform.sizeDelta = target ? target.sizeDelta : Vector2.one * 2000;
            }
        }

        private void FadeIn(float duration, System.Action callback = null)
        {
            if (duration > 0)
            {
                background.DOKill();
                var color = background.color;
                color.a = 0;
                background.color = color;
                background.DOFade(alpha, duration)
                    .SetEase(fadeCurve)
                    .OnComplete(() => callback?.Invoke());
            }
        }

        private void FadeOut(float duration, System.Action callback = null)
        {
            background.DOKill();
            var color = background.color;
            color.a = alpha;
            background.color = color;
            background.DOFade(0.0f, duration)
                .SetEase(fadeCurve)
                .OnComplete(() => callback?.Invoke());
        }

        private void SetMaskSize(RectTransform target)
        {
            mask.rectTransform.sizeDelta = _cachedMaskSize;

            if (target == null)
            {
                return;
            }

            var maskSize = mask.rectTransform.sizeDelta;
            maskSize.x = Mathf.Max(maskSize.x, target.sizeDelta.x * 1.5f);
            maskSize.y = Mathf.Max(maskSize.y, target.sizeDelta.y * 1.5f);
            mask.rectTransform.sizeDelta = maskSize;

            var menu = target.GetComponent<MainMenu>();
            if (menu != null && menu.type == MenuType.Combination)
            {
                mask.rectTransform.sizeDelta = new Vector2(400, 500);
            }
        }

        public override void Skip(System.Action callback)
        {
            // Do-nothing.
        }
    }
}
