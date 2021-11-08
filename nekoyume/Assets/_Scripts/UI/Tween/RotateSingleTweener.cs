using System;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;

namespace Nekoyume.UI.Tween
{
    [RequireComponent(typeof(RectTransform))]
    public class RotateSingleTweener : DOTweenBase
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

        [SerializeField]
        private float beginValue = 0f;

        [SerializeField]
        private float endValue = 0f;

        [SerializeField]
        private RotateMode rotateMode = RotateMode.Fast;

        [SerializeField]
        private bool isLoop = true;

        [SerializeField]
        private LoopType loopType = LoopType.Incremental;

        private RectTransform _rectTransform;

        protected override void Awake()
        {
            base.Awake();
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
                    fromValue.x = beginValue;
                    toValue.x = endValue;
                    break;
                case Single.Y:
                    fromValue.y = beginValue;
                    toValue.y = endValue;
                    break;
                case Single.Z:
                    fromValue.z = beginValue;
                    toValue.z = endValue;
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

            currentTween = _rectTransform.DOLocalRotate(toValue, duration, rotateMode);
            if (isLoop)
            {
                SetEase().SetLoops(-1, loopType);
            }
            else
            {
                SetEase();
            }
        }

        private void OnDisable()
        {
            Stop();
        }
    }
}
