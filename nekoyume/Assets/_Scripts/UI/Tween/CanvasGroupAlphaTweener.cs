using System;
using DG.Tweening;
using NUnit.Framework;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(CanvasGroup))]
    public class CanvasGroupAlphaTweener : BaseTweener
    {
        public TweenCallback onComplete = null;
        public TweenCallback onReverseComplete = null;

        [SerializeField]
        private float delayForPlay = 0f;

        [SerializeField]
        private Ease easeForPlay = Ease.Linear;

        [SerializeField]
        private float delayForPlayReverse = 0f;

        [SerializeField]
        private Ease easeForPlayReverse = Ease.Linear;

        [SerializeField]
        private float end = 1f;

        [SerializeField]
        private float duration = 1f;

        [SerializeField]
        private bool isFrom = false;

        [SerializeField]
        private bool playOnAwake = false;

        [SerializeField]
        private bool playOnEnable = false;

        private CanvasGroup _canvasGroupCache;
        private float? _originAlphaCache;

        private CanvasGroup CanvasGroup => _canvasGroupCache is null
            ? _canvasGroupCache = GetComponent<CanvasGroup>()
            : _canvasGroupCache;

        private float OriginAlpha =>
            _originAlphaCache ?? (_originAlphaCache = CanvasGroup.alpha).Value;

        private void Awake()
        {
            Assert.NotNull(CanvasGroup);
            Assert.AreEqual(OriginAlpha, _originAlphaCache);

            if (playOnAwake)
            {
                PlayTween();
            }
        }

        private void OnEnable()
        {
            if (playOnEnable)
            {
                PlayTween();
            }
        }

        public override Tweener PlayTween()
        {
            KillTween();

            if (isFrom)
            {
                CanvasGroup.alpha = end;
                Tweener = DOTween
                    .To(() => CanvasGroup.alpha,
                        value => CanvasGroup.alpha = value,
                        OriginAlpha,
                        duration)
                    .SetDelay(delayForPlay)
                    .SetEase(easeForPlay);
            }
            else
            {
                CanvasGroup.alpha = OriginAlpha;
                Tweener = DOTween
                    .To(() => CanvasGroup.alpha,
                        value => CanvasGroup.alpha = value,
                        end,
                        duration)
                    .SetDelay(delayForPlay)
                    .SetEase(easeForPlay);
            }

            Tweener.onComplete = onComplete;
            return Tweener.Play();
        }

        public override Tweener PlayReverse()
        {
            KillTween();

            if (isFrom)
            {
                CanvasGroup.alpha = OriginAlpha;
                Tweener = DOTween
                    .To(() => CanvasGroup.alpha,
                        value => CanvasGroup.alpha = value,
                        end,
                        duration)
                    .SetDelay(delayForPlayReverse)
                    .SetEase(easeForPlayReverse);
            }
            else
            {
                CanvasGroup.alpha = end;
                Tweener = DOTween
                    .To(() => CanvasGroup.alpha,
                        value => CanvasGroup.alpha = value,
                        OriginAlpha,
                        duration)
                    .SetDelay(delayForPlayReverse)
                    .SetEase(easeForPlayReverse);
            }

            Tweener.onComplete = onReverseComplete;
            return Tweener.Play();
        }

        public override void ResetToOrigin()
        {
            KillTween();
            CanvasGroup.alpha = OriginAlpha;
        }
    }
}
