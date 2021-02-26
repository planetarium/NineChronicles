using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(RectTransform))]
    public class RotateSingleTweener : MonoBehaviour
    {
        public enum Single
        {
            X,
            Y,
            Z
        }

        [SerializeField]
        private bool isLocal = true;

        [SerializeField]
        private Single single = Single.Z;

        // todo: `from` -> `begin`
        [SerializeField]
        private float from = 0f;

        // todo: `to` -> `end`
        [SerializeField]
        private float to = 0f;

        [SerializeField]
        private float duration = 1f;

        [SerializeField]
        private RotateMode rotateMode = RotateMode.Fast;

        [SerializeField]
        private Ease ease = Ease.Linear;

        [SerializeField]
        private bool isLoop = true;

        [SerializeField]
        private LoopType loopType = LoopType.Incremental;

        private RectTransform _rectTransform;

        protected Tweener Tweener { get; set; }

        private void Awake()
        {
            _rectTransform = GetComponent<RectTransform>();
        }

        private void OnEnable()
        {
            var fromValue = isLocal
                ? _rectTransform.localRotation.eulerAngles
                : _rectTransform.rotation.eulerAngles;
            var toValue = fromValue;
            switch (single)
            {
                case Single.X:
                    fromValue.x = from;
                    toValue.x = to;
                    break;
                case Single.Y:
                    fromValue.y = from;
                    toValue.y = to;
                    break;
                case Single.Z:
                    fromValue.z = from;
                    toValue.z = to;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (isLocal)
            {
                _rectTransform.localRotation = Quaternion.Euler(fromValue);
            }
            else
            {
                _rectTransform.rotation = Quaternion.Euler(fromValue);
            }

            if (isLoop)
            {
                Tweener = _rectTransform.DOLocalRotate(toValue, duration, rotateMode)
                    .SetEase(ease)
                    .SetLoops(-1, loopType);
            }
            else
            {
                Tweener = _rectTransform.DOLocalRotate(toValue, duration, rotateMode)
                    .SetEase(ease);
            }
        }

        private void OnDisable()
        {
            KillTween();
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
